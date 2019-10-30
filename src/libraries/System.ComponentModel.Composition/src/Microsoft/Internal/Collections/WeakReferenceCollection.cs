// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Internal.Collections
{
    internal class WeakReferenceCollection<T> where T : class
    {
        private readonly List<WeakReference> _items = new List<WeakReference>();

        public void Add(T item)
        {
            // Only cleanup right before we need to reallocate space.
            if (_items.Capacity == _items.Count)
            {
                CleanupDeadReferences();
            }

            _items.Add(new WeakReference(item));
        }

        public void Remove(T item)
        {
            int index = IndexOf(item);

            if (index != -1)
            {
                _items.RemoveAt(index);
            }
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void Clear()
        {
            _items.Clear();
        }

        // Should be executed under at least a read lock.
        private int IndexOf(T item)
        {
            int count = _items.Count;
            for (int i = 0; i < count; i++)
            {
                if (object.ReferenceEquals(_items[i].Target, item))
                {
                    return i;
                }
            }
            return -1;
        }

        // Should be executed under a write lock
        private void CleanupDeadReferences()
        {
            _items.RemoveAll(w => !w.IsAlive);
        }

        public List<T> AliveItemsToList()
        {
            List<T> aliveItems = new List<T>();

            foreach (var weakItem in _items)
            {
                T item = weakItem.Target as T;

                if (item != null)
                {
                    aliveItems.Add(item);
                }
            }

            return aliveItems;
        }
    }
}
