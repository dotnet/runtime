// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;

namespace Microsoft.Internal.Collections
{
    internal sealed class WeakReferenceCollection<T> where T : class
    {
        private readonly List<WeakReference> _items = new List<WeakReference>();
        private readonly CompositionLock _lock;

        internal WeakReferenceCollection(CompositionLock @lock)
        {
            _lock = @lock;
        }

        public void Add(T item)
        {
            // Only cleanup right before we need to reallocate space.
            if (_items.Capacity == _items.Count)
            {
                CleanupDeadReferences();
            }

            using (_lock.LockStateForWrite())
            {
                _items.Add(new WeakReference(item));
            }
        }

        public void Remove(T item)
        {
            int index = IndexOf(item);

            if (index != -1)
            {
                using (_lock.LockStateForWrite())
                {
                    _items.RemoveAt(index);
                }
            }
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void Clear()
        {
            using (_lock.LockStateForWrite())
            {
                _items.Clear();
            }
        }

        // Should be executed under at least a read lock.
        private int IndexOf(T item)
        {
            using (_lock.LockStateForRead())
            {
                int count = _items.Count;
                for (int i = 0; i < count; i++)
                {
                    if (object.ReferenceEquals(_items[i].Target, item))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        // Should be executed under a write lock
        private void CleanupDeadReferences()
        {
            using (_lock.LockStateForWrite())
            {
                _items.RemoveAll(w => !w.IsAlive);
            }
        }

        public List<T> AliveItemsToList()
        {
            List<T> aliveItems = new List<T>();

            using (_lock.LockStateForRead())
            {
                foreach (var weakItem in _items)
                {
                    if (weakItem.Target is T item)
                    {
                        aliveItems.Add(item);
                    }
                }
            }

            return aliveItems;
        }
    }
}
