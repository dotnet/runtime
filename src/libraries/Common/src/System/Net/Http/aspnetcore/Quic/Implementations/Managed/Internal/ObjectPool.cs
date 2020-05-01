using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Quic.Implementations.Managed.Internal
{

    internal interface IPoolableObject
    {
        void Reset();
    }

    internal sealed class ObjectPool<T> where T : IPoolableObject, new()
    {
        private readonly int _maxItems;

        private readonly Stack<T> _items;

        public ObjectPool(int maxItems)
        {
            _maxItems = maxItems;
            _items = new Stack<T>(maxItems);
        }

        public T Rent()
        {
            if (!_items.TryPop(out var item))
                item = new T();

            return item;
        }

        public void Return(T item)
        {
            item.Reset();
            if (_items.Count < _maxItems)
            {
                _items.Push(item);
            }
        }
    }
}
