using System;
using System.Collections.Generic;
using System.Text;

namespace ByteMsg233
{
    public sealed class ByteMsgReader
    {
        private readonly byte[] _data;
        private int _offset;

        public ByteMsgReader(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public int Offset => _offset;
        public int Length => _data.Length;
        public bool IsEof => _offset >= _data.Length;
        public int Remaining => _data.Length - _offset;

        public byte ReadByte()
        {
            if (IsEof)
            {
                throw new InvalidOperationException("Unexpected end of ByteMsg233 buffer.");
            }

            return _data[_offset++];
        }

        public ulong ReadVarint()
        {
            ulong result = 0;
            var shift = 0;

            while (shift < 64)
            {
                var current = ReadByte();
                result |= (ulong)(current & 0x7F) << shift;
                if ((current & 0x80) == 0)
                {
                    return result;
                }

                shift += 7;
            }

            throw new FormatException("Invalid varint encoding.");
        }

        public long ReadZigZag()
        {
            return ByteMsgWriter.ZigZagDecode(ReadVarint());
        }

        public bool ReadBool()
        {
            return ReadVarint() != 0;
        }

        public byte[] ReadBytes()
        {
            var length = checked((int)ReadVarint());
            if (length < 0 || length > Remaining)
            {
                throw new InvalidOperationException("ByteMsg233 length-delimited field exceeds remaining buffer.");
            }

            var bytes = new byte[length];
            Buffer.BlockCopy(_data, _offset, bytes, 0, length);
            _offset += length;
            return bytes;
        }

        public string ReadString()
        {
            return Encoding.UTF8.GetString(ReadBytes());
        }

        public List<ulong> ReadPackedVarints(List<ulong>? values = null)
        {
            var count = checked((int)ReadVarint());
            values ??= new List<ulong>(count);
            values.Clear();
            if (values.Capacity < count)
            {
                values.Capacity = count;
            }

            for (var i = 0; i < count; i++)
            {
                values.Add(ReadVarint());
            }

            return values;
        }

        public List<ulong> ReadDeltaVarints(List<ulong>? values = null)
        {
            var count = checked((int)ReadVarint());
            values ??= new List<ulong>(count);
            values.Clear();
            if (values.Capacity < count)
            {
                values.Capacity = count;
            }

            if (count == 0)
            {
                return values;
            }

            var current = ReadVarint();
            values.Add(current);
            for (var i = 1; i < count; i++)
            {
                current = unchecked((ulong)((long)current + ByteMsgWriter.ZigZagDecode(ReadVarint())));
                values.Add(current);
            }

            return values;
        }

        public List<bool> ReadBoolBitset(List<bool>? values = null)
        {
            var count = checked((int)ReadVarint());
            values ??= new List<bool>(count);
            values.Clear();
            if (values.Capacity < count)
            {
                values.Capacity = count;
            }

            for (var i = 0; i < count; i += 8)
            {
                var current = ReadByte();
                var limit = Math.Min(8, count - i);
                for (var bit = 0; bit < limit; bit++)
                {
                    values.Add((current & (1 << bit)) != 0);
                }
            }

            return values;
        }

        public List<string> ReadStringList(List<string>? values = null)
        {
            var count = checked((int)ReadVarint());
            values ??= new List<string>(count);
            values.Clear();
            if (values.Capacity < count)
            {
                values.Capacity = count;
            }

            for (var i = 0; i < count; i++)
            {
                values.Add(ReadString());
            }

            return values;
        }

        public ByteMsgFieldHeader ReadFieldHeader()
        {
            var raw = checked((int)ReadVarint());
            return new ByteMsgFieldHeader(raw >> 3, (ByteMsgWireType)(raw & 0x7));
        }

        public ByteMsgReader ReadSubReader()
        {
            return new ByteMsgReader(ReadBytes());
        }

        public T ReadMessage<T>(Func<ByteMsgReader, T> readValue)
        {
            return readValue(ReadSubReader());
        }

        public List<T> ReadList<T>(Func<ByteMsgReader, T> readItem)
        {
            var nested = ReadSubReader();
            var count = checked((int)nested.ReadVarint());
            var values = new List<T>(count);

            for (var i = 0; i < count; i++)
            {
                values.Add(readItem(nested));
            }

            return values;
        }

        public Dictionary<TKey, TValue> ReadMap<TKey, TValue>(
            Func<ByteMsgReader, TKey> readKey,
            Func<ByteMsgReader, TValue> readValue)
            where TKey : notnull
        {
            var nested = ReadSubReader();
            var count = checked((int)nested.ReadVarint());
            var values = new Dictionary<TKey, TValue>(count);

            for (var i = 0; i < count; i++)
            {
                values[readKey(nested)] = readValue(nested);
            }

            return values;
        }

        public void SkipField(ByteMsgWireType wireType)
        {
            switch (wireType)
            {
                case ByteMsgWireType.Varint:
                    ReadVarint();
                    return;
                case ByteMsgWireType.LengthDelimited:
                    ReadBytes();
                    return;
                default:
                    throw new NotSupportedException($"Unsupported ByteMsg233 wire type: {wireType}.");
            }
        }
    }
}
