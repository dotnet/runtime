// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    internal class Set<TElement>
    {
        private int[] _buckets;
        private Slot[] _slots;
        private int _count;
        private readonly IEqualityComparer<TElement> _comparer;
#if DEBUG
        private bool _haveRemoved;
#endif

        public Set(IEqualityComparer<TElement> comparer)
        {
            if (comparer == null) comparer = EqualityComparer<TElement>.Default;
            _comparer = comparer;
            _buckets = new int[7];
            _slots = new Slot[7];
        }

        // If value is not in set, add it and return true; otherwise return false
        public bool Add(TElement value)
        {
#if DEBUG
            Debug.Assert(!_haveRemoved, "This class is optimised for never calling Add after Remove. If your changes need to do so, undo that optimization.");
#endif
            int hashCode = InternalGetHashCode(value);
            for (int i = _buckets[hashCode % _buckets.Length] - 1; i >= 0; i = _slots[i].next)
            {
                if (_slots[i].hashCode == hashCode && _comparer.Equals(_slots[i].value, value)) return false;
            }
            if (_count == _slots.Length) Resize();
            int index = _count;
            _count++;
            int bucket = hashCode % _buckets.Length;
            _slots[index].hashCode = hashCode;
            _slots[index].value = value;
            _slots[index].next = _buckets[bucket] - 1;
            _buckets[bucket] = index + 1;
            return true;
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

        private void Resize()
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

        internal TElement[] ToArray()
        {
#if DEBUG
            Debug.Assert(!_haveRemoved, "Optimised ToArray cannot be called if Remove has been called.");
#endif
            TElement[] array = new TElement[_count];
            for (int i = 0; i != array.Length; ++i)
                array[i] = _slots[i].value;
            return array;
        }

        internal List<TElement> ToList()
        {
#if DEBUG
            Debug.Assert(!_haveRemoved, "Optimised ToList cannot be called if Remove has been called.");
#endif
            int count = _count;
            List<TElement> list = new List<TElement>(count);
            for (int i = 0; i != count; ++i)
                list.Add(_slots[i].value);
            return list;
        }

        internal int Count
        {
            get { return _count; }
        }

        internal int InternalGetHashCode(TElement value)
        {
            // Handle comparer implementations that throw when passed null
            return (value == null) ? 0 : _comparer.GetHashCode(value) & 0x7FFFFFFF;
        }

        internal struct Slot
        {
            internal int hashCode;
            internal int next;
            internal TElement value;
        }
    }
}
