// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

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
        private readonly IEqualityComparer<TElement> _comparer;
#if DEBUG
        private bool _haveRemoved;
#endif

        private const int InitialSize = 7;
        private const int HashCodeMask = 0x7FFFFFFF;

        public Set(IEqualityComparer<TElement> comparer)
        {
            if (comparer == null) comparer = EqualityComparer<TElement>.Default;
            _comparer = comparer;
            _buckets = new int[InitialSize];
            _slots = new Slot[InitialSize];
        }

        // If value is not in set, add it and return true; otherwise return false
        public bool Add(TElement value)
        {
#if DEBUG
            Debug.Assert(!_haveRemoved, "This class is optimised for never calling Add after Remove. If your changes need to do so, undo that optimization.");
#endif
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
#if DEBUG
            _haveRemoved = true;
#endif
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
                    _slots[i].next = -1;
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
                if (_count == _slots.Length) Resize();
                int index = _count;
                _count++;
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
            Slot[] newSlots = new Slot[newSize];
            Array.Copy(_slots, 0, newSlots, 0, _count);
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
            return (value == null) ? 0 : _comparer.GetHashCode(value) & HashCodeMask;
        }

        internal struct Slot
        {
            internal int hashCode;
            internal int next;
            internal TElement value;
        }
    }
}
