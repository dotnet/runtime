// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    internal class MultiSet<T>
    {
        private readonly IEqualityComparer<T> _equalityComparer;
        private int[] _bucketHeaders; // initialized as zeroed array so its valid item value is 1-based index of _entries
        private Entry[] _entries;
        private int _startOffset;

        public MultiSet(int capacity, IEqualityComparer<T>? equalityComparer)
        {
            _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

            _bucketHeaders = new int[capacity];
            _entries = new Entry[capacity];
        }

        public MultiSet(IEqualityComparer<T>? equalityComparer)
        {
            _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

            _bucketHeaders = new int[1];
            _entries = new Entry[1];
        }

        public void Add(T item)
        {
            ref int bucketHeader = ref GetBucketHeaderIndexRef(item);
            int entryIndex = bucketHeader - 1;
            while (entryIndex != -1)
            {
                ref Entry entry = ref _entries[entryIndex];
                if (_equalityComparer.Equals(entry.Key, item))
                {
                    entry.Count++;
                    return;
                }

                entryIndex = entry.Next;
            }

            if (_startOffset == _bucketHeaders.Length)
            {
                EnsureCapacity();
                bucketHeader = ref GetBucketHeaderIndexRef(item);
            }

            _entries[_startOffset].SetInfo(item, bucketHeader - 1, 1);
            bucketHeader = ++_startOffset; // 1-based bucketHeader
        }

        public bool TryRemove(T item)
        {
            ref int bucketHeader = ref GetBucketHeaderIndexRef(item);
            int entryIndex = bucketHeader - 1;
            while (entryIndex != -1)
            {
                ref Entry entry = ref _entries[entryIndex];
                if (_equalityComparer.Equals(entry.Key, item))
                {
                    // Did not remove related entry from bucket when item's count was to 0
                    if (entry.Count == 0)
                    {
                        return false;
                    }

                    entry.Count--;
                    return true;
                }

                entryIndex = entry.Next;
            }

            return false;
        }

        private void EnsureCapacity()
        {
            const uint arrayMaxLength = 0X7FFFFFC7;

            int expectedSize = _bucketHeaders.Length + 1;
            uint newSize = (uint)_bucketHeaders.Length << 1;
            if (newSize > arrayMaxLength)
            {
                newSize = arrayMaxLength;
            }

            if (newSize < expectedSize)
            {
                newSize = (uint)expectedSize;
            }

            var entries = new Entry[newSize];
            Array.Copy(_entries, entries, _entries.Length);
            _bucketHeaders = new int[newSize];
            for (int i = 0; i < _entries.Length; i++)
            {
                ref Entry entry = ref _entries[i];
                if (entry.Count != 0)
                {
                    ref int bucketHeader = ref GetBucketHeaderIndexRef(entry.Key);
                    entry.Next = bucketHeader - 1;
                    bucketHeader = i + 1;
                }
            }

            _entries = entries;
        }

        private ref int GetBucketHeaderIndexRef(T item)
        {
            int hashCode = item == null ? 0 : _equalityComparer.GetHashCode(item);
            return ref _bucketHeaders[(uint)hashCode % _bucketHeaders.Length];
        }

        private struct Entry
        {
            public void SetInfo(T key, int next, int count)
            {
                Key = key;
                Next = next;
                Count = count;
            }

            public T Key;
            public int Next; // 0 based index; -1 means current entry is the last one
            public int Count;
        }
    }
}