using System;

namespace ByteMsg233
{
    public sealed class ByteMsgByteBuffer : IByteMsgResettable
    {
        private byte[] _buffer;

        public ByteMsgByteBuffer(int capacity = 0)
        {
            _buffer = capacity > 0 ? new byte[capacity] : Array.Empty<byte>();
        }

        public int Length { get; private set; }
        public int Capacity => _buffer.Length;
        public byte[] Buffer => _buffer;

        public ReadOnlySpan<byte> Span => _buffer.AsSpan(0, Length);

        public void EnsureCapacity(int capacity)
        {
            if (_buffer.Length >= capacity)
            {
                return;
            }

            var next = Math.Max(capacity, Math.Max(16, _buffer.Length * 2));
            Array.Resize(ref _buffer, next);
        }

        public void SetLength(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            EnsureCapacity(length);
            Length = length;
        }

        public void Set(ReadOnlySpan<byte> value)
        {
            SetLength(value.Length);
            value.CopyTo(_buffer);
        }

        public void Reset()
        {
            Length = 0;
        }
    }
}
