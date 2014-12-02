// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Linq.Parallel
{
    internal class JaggedArray<TElement>
    {
        public static TElement[][] Allocate(int size1, int size2)
        {
            TElement[][] ret = new TElement[size1][];
            for (int i = 0; i < size1; i++)
                ret[i] = new TElement[size2];

            return ret;
        }
    }

    // Copied from Linq.
    internal class Set<TElement>
    {
        private int[] _buckets;
        private Slot[] _slots;
        private int _count;
        private int _freeList;
        private IEqualityComparer<TElement> _comparer;

        public Set() : this(null) { }

        public Set(IEqualityComparer<TElement> comparer)
        {
            if (comparer == null) comparer = EqualityComparer<TElement>.Default;
            _comparer = comparer;
            _buckets = new int[7];
            _slots = new Slot[7];
            _freeList = -1;
        }

        // If value is not in set, add it and return true; otherwise return false
        public bool Add(TElement value)
        {
            return !Find(value, true);
        }

        // Check whether value is in set
        public bool Contains(TElement value)
        {
            return Find(value, false);
        }

        // If value is in set, remove it and return true; otherwise return false
        public bool Remove(TElement value)
        {
            int hashCode = InternalGetHashCode(value);
            int bucket = hashCode % _buckets.Length;
            int last = -1;
            for (int i = _buckets[bucket] - 1; i >= 0; last = i, i = _slots[i].next)
            {
                if (_slots[i].hashCode == hashCode && _comparer.Equals(_slots[i].value, value))
                {
                    if (last < 0)
                    {
                        _buckets[bucket] = _slots[i].next + 1;
                    }
                    else
                    {
                        _slots[last].next = _slots[i].next;
                    }
                    _slots[i].hashCode = -1;
                    _slots[i].value = default(TElement);
                    _slots[i].next = _freeList;
                    _freeList = i;
                    return true;
                }
            }
            return false;
        }

        bool Find(TElement value, bool add)
        {
            int hashCode = InternalGetHashCode(value);
            for (int i = _buckets[hashCode % _buckets.Length] - 1; i >= 0; i = _slots[i].next)
            {
                if (_slots[i].hashCode == hashCode && _comparer.Equals(_slots[i].value, value)) return true;
            }
            if (add)
            {
                int index;
                if (_freeList >= 0)
                {
                    index = _freeList;
                    _freeList = _slots[index].next;
                }
                else
                {
                    if (_count == _slots.Length) Resize();
                    index = _count;
                    _count++;
                }
                int bucket = hashCode % _buckets.Length;
                _slots[index].hashCode = hashCode;
                _slots[index].value = value;
                _slots[index].next = _buckets[bucket] - 1;
                _buckets[bucket] = index + 1;
            }
            return false;
        }

        void Resize()
        {
            int newSize = checked(_count * 2 + 1);
            int[] newBuckets = new int[newSize];
            Slot[] newSlots = ArrayT<Slot>.Resize(_slots, newSize, _count);
            for (int i = 0; i < _count; i++)
            {
                int bucket = newSlots[i].hashCode % newSize;
                newSlots[i].next = newBuckets[bucket] - 1;
                newBuckets[bucket] = i + 1;
            }
            _buckets = newBuckets;
            _slots = newSlots;
        }

        internal int InternalGetHashCode(TElement value)
        {
            // Work around comparer implementations that throw when passed null
            return (value == null) ? 0 : _comparer.GetHashCode(value) & 0x7FFFFFFF;
        }

        internal struct Slot
        {
            internal int hashCode;
            internal TElement value;
            internal int next;
        }
    }
}