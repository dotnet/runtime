// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Immutable.Tests
{
    internal sealed class CopyToTrackingCollection<T> : ICollection<T>
    {
        private readonly T[] _items;

        public CopyToTrackingCollection(params T[] items)
        {
            _items = items;
        }

        public int CopyToCallCount { get; private set; }

        public int Count => _items.Length;

        public bool IsReadOnly => true;

        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyToCallCount++;
            Array.Copy(_items, 0, array, arrayIndex, _items.Length);
        }

        public IEnumerator<T> GetEnumerator() => throw new ShouldNotBeInvokedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new ShouldNotBeInvokedException();

        public bool Contains(T item) => throw new ShouldNotBeInvokedException();

        public void Add(T item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Remove(T item) => throw new NotSupportedException();
    }
}
