// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    internal sealed class ChildKeysAggregator : IEnumerable<string>
    {
        // A section rarely has more children than this, and below it a scan of the keys already held beats the set
        // that would replace it, which costs several hundred bytes before it holds anything.
        private const int LinearLimit = 8;

        private string[] _items = Array.Empty<string>();
        private int _count;
#if NET
        private HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _lookup;
#else
        private HashSet<string>? _set;
#endif

        internal ChildKeysAggregator() { }

        internal ChildKeysAggregator(IEnumerable<string> keys)
        {
            foreach (string key in keys)
            {
                Add(key);
            }
        }

        internal string[] Items => _items;

        /// <summary>Gets the number of distinct child keys accumulated so far.</summary>
        internal int Count => _count;

#if NET
        private HashSet<string>? Set => _lookup.Set;
#else
        private HashSet<string>? Set => _set;
#endif

        internal void Add(ReadOnlySpan<char> key)
        {
            HashSet<string>? set = Set;
            if (set is null)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (key.Equals(_items[i].AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                string item = key.ToString();
                if (_count == LinearLimit)
                {
                    Promote().Add(item);
                }

                Append(item);
                return;
            }

#if NET
            if (!_lookup.Contains(key))
            {
                string item = key.ToString();
                set.Add(item);
                Append(item);
            }
#else
            string materialized = key.ToString();
            if (set.Add(materialized))
            {
                Append(materialized);
            }
#endif
        }

        internal void Add(ChildKeysAggregator other)
        {
            for (int i = 0; i < other._count; i++)
            {
                Add(other._items[i]);
            }
        }

        /// <summary>
        /// Replaces the contents with <paramref name="keys"/>, de-duplicated. The keys come from a provider that
        /// returned a sequence other than this accumulator, in whatever order it chose; the order is established
        /// later, when the accumulator is handed to a consumer.
        /// </summary>
        /// <param name="keys">The keys to replace the current contents with.</param>
        internal void Overwrite(IEnumerable<string> keys)
        {
            // The keys may be a lazy view over this accumulator, so they are staged past the current contents and
            // only compacted down over them once the sequence is exhausted. The array and set are reused throughout.
            int start = _count;
            int end = start;

            if (keys is ChildKeysAggregator aggregator)
            {
                end = Stage(aggregator.Count);
                aggregator.CopyTo(_items, start);
            }
            else if (keys is ICollection<string> collection)
            {
                end = Stage(collection.Count);
                collection.CopyTo(_items, start);
            }
            else
            {
                foreach (string key in keys)
                {
                    if (end == _items.Length)
                    {
                        Array.Resize(ref _items, _items.Length == 0 ? LinearLimit : _items.Length * 2);
                    }

                    _items[end++] = key;
                }
            }

            _count = 0;
            Set?.Clear();

            for (int i = start; i < end; i++)
            {
                Add(_items[i]);
            }

            Array.Clear(_items, _count, end - _count);

            int Stage(int incoming)
            {
                int total = start + incoming;
                if (total > _items.Length)
                {
                    Array.Resize(ref _items, total);
                }

                return total;
            }
        }

        internal void CopyTo(string[] array, int arrayIndex) => Array.Copy(_items, 0, array, arrayIndex, _count);

        public Enumerator GetEnumerator() => new Enumerator(_items, _count);

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal void Add(string item)
        {
            HashSet<string>? set = Set;
            if (set is null)
            {
                if (IndexOf(item) >= 0)
                {
                    return;
                }

                if (_count == LinearLimit)
                {
                    Promote().Add(item);
                }

                Append(item);
                return;
            }

            if (set.Add(item))
            {
                Append(item);
            }
        }

        private int IndexOf(string item)
        {
            for (int i = 0; i < _count; i++)
            {
                if (string.Equals(_items[i], item, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private HashSet<string> Promote()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _count; i++)
            {
                set.Add(_items[i]);
            }

#if NET
            _lookup = set.GetAlternateLookup<ReadOnlySpan<char>>();
#else
            _set = set;
#endif
            return set;
        }

        private void Append(string item)
        {
            if (_count == _items.Length)
            {
                Array.Resize(ref _items, _items.Length == 0 ? LinearLimit : _items.Length * 2);
            }

            _items[_count++] = item;
        }

        // Reads the array it was handed, so a later Overwrite publishing a new one cannot disturb it.
        internal struct Enumerator : IEnumerator<string>
        {
            private readonly string[] _items;
            private readonly int _count;
            private int _index;

            internal Enumerator(string[] items, int count)
            {
                _items = items;
                _count = count;
                _index = -1;
            }

            public string Current => _items[_index];

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index < _count;

            public void Reset() => _index = -1;

            public void Dispose()
            {
            }
        }
    }
}
