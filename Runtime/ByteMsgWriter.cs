using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ByteMsg233
{
    public sealed class ByteMsgWriter
    {
        private readonly MemoryStream _stream;
        private readonly byte[] _scratch = new byte[10];

        public ByteMsgWriter(int capacity = 128)
        {
            _stream = new MemoryStream(capacity);
        }

        public int Length => (int)_stream.Length;

        public void Reset()
        {
            _stream.SetLength(0);
        }

        public byte[] ToArray()
        {
            return _stream.ToArray();
        }

        public ArraySegment<byte> ToArraySegment()
        {
            return _stream.TryGetBuffer(out var segment) ? segment : new ArraySegment<byte>(ToArray());
        }

        public void WriteRaw(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
            {
                return;
            }

            _stream.Write(bytes);
        }

        public void WriteVarint(ulong value)
        {
            var index = 0;
            while (value >= 0x80)
            {
                _scratch[index++] = (byte)(value | 0x80);
                value >>= 7;
            }

            _scratch[index++] = (byte)value;
            _stream.Write(_scratch, 0, index);
        }

        public void WriteUInt(uint value)
        {
            WriteVarint(value);
        }

        public void WriteULong(ulong value)
        {
            WriteVarint(value);
        }

        public void WriteZigZag(long value)
        {
            WriteVarint(ZigZagEncode(value));
        }

        public void WriteBool(bool value)
        {
            WriteVarint(value ? 1UL : 0UL);
        }

        public void WriteEnum(int value)
        {
            WriteVarint((ulong)value);
        }

        public void WriteString(string value)
        {
            value ??= string.Empty;
            var bytes = Encoding.UTF8.GetBytes(value);
            WriteBytes(bytes);
        }

        public void WriteBytes(byte[]? value)
        {
            value ??= Array.Empty<byte>();
            WriteVarint((ulong)value.Length);
            _stream.Write(value, 0, value.Length);
        }

        public void WriteFieldHeader(int tag, ByteMsgWireType wireType)
        {
            if (tag <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tag), tag, "Field tag must be positive.");
            }

            WriteVarint((ulong)((tag << 3) | (int)wireType));
        }

        public void WriteUIntField(int tag, uint value)
        {
            WriteFieldHeader(tag, ByteMsgWireType.Varint);
            WriteUInt(value);
        }

        public void WriteULongField(int tag, ulong value)
        {
            WriteFieldHeader(tag, ByteMsgWireType.Varint);
            WriteULong(value);
        }

        public void WriteZigZagField(int tag, long value)
        {
            WriteFieldHeader(tag, ByteMsgWireType.Varint);
            WriteZigZag(value);
        }

        public void WriteBoolField(int tag, bool value)
        {
            WriteFieldHeader(tag, ByteMsgWireType.Varint);
            WriteBool(value);
        }

        public void WriteEnumField(int tag, int value)
        {
            WriteFieldHeader(tag, ByteMsgWireType.Varint);
            WriteEnum(value);
        }

        public void WriteStringField(int tag, string value)
        {
            WriteFieldHeader(tag, ByteMsgWireType.LengthDelimited);
            WriteString(value);
        }

        public void WriteBytesField(int tag, byte[] value)
        {
            WriteFieldHeader(tag, ByteMsgWireType.LengthDelimited);
            WriteBytes(value);
        }

        public void WriteMessage(Action<ByteMsgWriter> encode)
        {
            if (encode == null)
            {
                throw new ArgumentNullException(nameof(encode));
            }

            var nested = new ByteMsgWriter();
            encode(nested);
            var bytes = nested.ToArray();
            WriteBytes(bytes);
        }

        public void WriteMessageField(int tag, Action<ByteMsgWriter> encode)
        {
            WriteFieldHeader(tag, ByteMsgWireType.LengthDelimited);
            WriteMessage(encode);
        }

        public void WriteListField<T>(int tag, IReadOnlyList<T>? values, Action<ByteMsgWriter, T> writeItem)
        {
            if (values == null || values.Count == 0)
            {
                return;
            }

            WriteMessageField(tag, writer =>
            {
                writer.WriteVarint((ulong)values.Count);
                for (var i = 0; i < values.Count; i++)
                {
                    writeItem(writer, values[i]);
                }
            });
        }

        public void WriteMapField<TKey, TValue>(
            int tag,
            IEnumerable<KeyValuePair<TKey, TValue>>? values,
            Action<ByteMsgWriter, TKey> writeKey,
            Action<ByteMsgWriter, TValue> writeValue)
        {
            if (values == null)
            {
                return;
            }

            var entries = values as ICollection<KeyValuePair<TKey, TValue>> ?? new List<KeyValuePair<TKey, TValue>>(values);
            if (entries.Count == 0)
            {
                return;
            }

            WriteMessageField(tag, writer =>
            {
                writer.WriteVarint((ulong)entries.Count);
                foreach (var entry in entries)
                {
                    writeKey(writer, entry.Key);
                    writeValue(writer, entry.Value);
                }
            });
        }

        public static ulong ZigZagEncode(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }

        public static long ZigZagDecode(ulong value)
        {
            return (long)((value >> 1) ^ (ulong)-(long)(value & 1));
        }
    }
}
