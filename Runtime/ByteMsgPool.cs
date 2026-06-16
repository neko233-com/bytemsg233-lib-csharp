using System;
using System.Collections.Generic;

namespace ByteMsg233
{
    public sealed class ByteMsgPool<T> where T : class
    {
        private readonly Stack<T> _items = new Stack<T>();
        private readonly Func<T> _factory;
        private readonly Action<T>? _reset;

        public ByteMsgPool(Func<T> factory, Action<T>? reset = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset;
        }

        public T Rent()
        {
            if (_items.Count > 0)
            {
                return _items.Pop();
            }

            return _factory();
        }

        public void Return(T? value)
        {
            if (value == null)
            {
                return;
            }

            if (_reset != null)
            {
                _reset(value);
            }
            else if (value is IByteMsgResettable resettable)
            {
                resettable.Reset();
            }

            _items.Push(value);
        }

        public int Count => _items.Count;
    }
}
