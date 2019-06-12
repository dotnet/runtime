// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    /// <summary>
    /// This is a constant map aimed to efficiently support a Split operation (map decomposition).
    /// A Split operation returns two non-overlapping, non-empty views of the existing map (or both
    /// values are set to NULL). The two views returned should contain roughly the same number of elements.
    /// This map is backed by a sorted array. Thus, split operations are O(1) and enumerations are fast;
    /// however, look-up in the map are O(log n).
    /// </summary>
    /// <typeparam name="TKey">Type of objects that act as keys.</typeparam>    
    /// <typeparam name="TValue">Type of objects that act as entries / values.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class ConstantSplittableMap<TKey, TValue> : IMapView<TKey, TValue> where TKey : notnull
    {
        private class KeyValuePairComparator : IComparer<KeyValuePair<TKey, TValue>>
        {
            private static readonly IComparer<TKey> keyComparator = Comparer<TKey>.Default;

            public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            {
                return keyComparator.Compare(x.Key, y.Key);
            }
        }  // private class KeyValuePairComparator


        private static readonly KeyValuePairComparator keyValuePairComparator = new KeyValuePairComparator();

        private readonly KeyValuePair<TKey, TValue>[] items;
        private readonly int firstItemIndex;
        private readonly int lastItemIndex;

        internal ConstantSplittableMap(IReadOnlyDictionary<TKey, TValue> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            firstItemIndex = 0;
            lastItemIndex = data.Count - 1;
            items = CreateKeyValueArray(data.Count, data.GetEnumerator());
        }


        private ConstantSplittableMap(KeyValuePair<TKey, TValue>[] items, int firstItemIndex, int lastItemIndex)
        {
            this.items = items;
            this.firstItemIndex = firstItemIndex;
            this.lastItemIndex = lastItemIndex;
        }


        private KeyValuePair<TKey, TValue>[] CreateKeyValueArray(int count, IEnumerator<KeyValuePair<TKey, TValue>> data)
        {
            KeyValuePair<TKey, TValue>[] kvArray = new KeyValuePair<TKey, TValue>[count];

            int i = 0;
            while (data.MoveNext())
                kvArray[i++] = data.Current;

            Array.Sort(kvArray, keyValuePairComparator);

            return kvArray;
        }


        public int Count
        {
            get
            {
                return lastItemIndex - firstItemIndex + 1;
            }
        }


        // [CLSCompliant(false)]
        public uint Size
        {
            get
            {
                return (uint)(lastItemIndex - firstItemIndex + 1);
            }
        }


        public TValue Lookup(TKey key)
        {
            TValue value;
            bool found = TryGetValue(key, out value);

            if (!found)
            {
                Debug.Assert(key != null);
                Exception e = new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
                e.HResult = HResults.E_BOUNDS;
                throw e;
            }

            return value;
        }


        public bool HasKey(TKey key)
        {
            TValue value;
            bool hasKey = TryGetValue(key, out value);
            return hasKey;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IKeyValuePair<TKey, TValue>>)this).GetEnumerator();
        }

        public IIterator<IKeyValuePair<TKey, TValue>> First()
        {
            return new EnumeratorToIteratorAdapter<IKeyValuePair<TKey, TValue>>(GetEnumerator());
        }

        public IEnumerator<IKeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new IKeyValuePairEnumerator(items, firstItemIndex, lastItemIndex);
        }

        public void Split(out IMapView<TKey, TValue>? firstPartition, out IMapView<TKey, TValue>? secondPartition)
        {
            if (Count < 2)
            {
                firstPartition = null;
                secondPartition = null;
                return;
            }

            int pivot = (int)(((long)firstItemIndex + (long)lastItemIndex) / (long)2);

            firstPartition = new ConstantSplittableMap<TKey, TValue>(items, firstItemIndex, pivot);
            secondPartition = new ConstantSplittableMap<TKey, TValue>(items, pivot + 1, lastItemIndex);
        }

        #region IReadOnlyDictionary members

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            KeyValuePair<TKey, TValue> searchKey = new KeyValuePair<TKey, TValue>(key, default!);
            int index = Array.BinarySearch(items, firstItemIndex, Count, searchKey, keyValuePairComparator);

            if (index < 0)
            {
                value = default!;
                return false;
            }

            value = items[index].Value;
            return true;
        }

        #endregion IReadOnlyDictionary members

        #region IKeyValuePair Enumerator

        internal struct IKeyValuePairEnumerator : IEnumerator<IKeyValuePair<TKey, TValue>>
        {
            private KeyValuePair<TKey, TValue>[] _array;
            private int _start;
            private int _end;
            private int _current;

            internal IKeyValuePairEnumerator(KeyValuePair<TKey, TValue>[] items, int first, int end)
            {
                Debug.Assert(items != null);
                Debug.Assert(first >= 0);
                Debug.Assert(end >= 0);
                Debug.Assert(first < items.Length);
                Debug.Assert(end < items.Length);

                _array = items;
                _start = first;
                _end = end;
                _current = _start - 1;
            }

            public bool MoveNext()
            {
                if (_current < _end)
                {
                    _current++;
                    return true;
                }
                return false;
            }

            public IKeyValuePair<TKey, TValue> Current
            {
                get
                {
                    if (_current < _start) throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                    if (_current > _end) throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                    return new CLRIKeyValuePairImpl<TKey, TValue>(ref _array[_current]);
                }
            }

            object? IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                _current = _start - 1;
            }

            public void Dispose()
            {
            }
        }

        #endregion IKeyValuePair Enumerator
    }  // internal ConstantSplittableMap<TKey, TValue>
}  // namespace System.Runtime.InteropServices.WindowsRuntime
