// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    /// <summary>
    /// A lightweight hash set.
    /// </summary>
    /// <typeparam name="TElement">The type of the set's items.</typeparam>
    internal sealed class Set<TElement>
    {
        /// <summary>
        /// The comparer used to hash and compare items in the set.
        /// </summary>
        private readonly IEqualityComparer<TElement> _comparer;

        /// <summary>
        /// The hash buckets, which are used to index into the slots.
        /// </summary>
        private int[] _buckets;

        /// <summary>
        /// The slots, each of which store an item and its hash code.
        /// </summary>
        private Slot[] _slots;

        /// <summary>
        /// The number of items in this set.
        /// </summary>
        private int _count;

#if DEBUG
        /// <summary>
        /// Whether <see cref="Remove"/> has been called on this set.
        /// </summary>
        /// <remarks>
        /// When <see cref="Remove"/> runs in debug builds, this flag is set to <c>true</c>.
        /// Other methods assert that this flag is <c>false</c> in debug builds, because
        /// they make optimizations that may not be correct if <see cref="Remove"/> is called
        /// beforehand.
        /// </remarks>
        private bool _haveRemoved;
#endif

        /// <summary>
        /// Constructs a set that compares items with the specified comparer.
        /// </summary>
        /// <param name="comparer">
        /// The comparer. If this is <c>null</c>, it defaults to <see cref="EqualityComparer{TElement}.Default"/>.
        /// </param>
        public Set(IEqualityComparer<TElement> comparer)
        {
            _comparer = comparer ?? EqualityComparer<TElement>.Default;
            _buckets = new int[7];
            _slots = new Slot[7];
        }

        /// <summary>
        /// Attempts to add an item to this set.
        /// </summary>
        /// <param name="value">The item to add.</param>
        /// <returns>
        /// <c>true</c> if the item was not in the set; otherwise, <c>false</c>.
        /// </returns>
        public bool Add(TElement value)
        {
#if DEBUG
            Debug.Assert(!_haveRemoved, "This class is optimised for never calling Add after Remove. If your changes need to do so, undo that optimization.");
#endif
            int hashCode = InternalGetHashCode(value);
            for (int i = _buckets[hashCode % _buckets.Length] - 1; i >= 0; i = _slots[i]._next)
            {
                if (_slots[i]._hashCode == hashCode && _comparer.Equals(_slots[i]._value, value))
                {
                    return false;
                }
            }

            if (_count == _slots.Length)
            {
                Resize();
            }

            int index = _count;
            _count++;
            int bucket = hashCode % _buckets.Length;
            _slots[index]._hashCode = hashCode;
            _slots[index]._value = value;
            _slots[index]._next = _buckets[bucket] - 1;
            _buckets[bucket] = index + 1;
            return true;
        }

        /// <summary>
        /// Attempts to remove an item from this set.
        /// </summary>
        /// <param name="value">The item to remove.</param>
        /// <returns>
        /// <c>true</c> if the item was in the set; otherwise, <c>false</c>.
        /// </returns>
        public bool Remove(TElement value)
        {
#if DEBUG
            _haveRemoved = true;
#endif
            int hashCode = InternalGetHashCode(value);
            int bucket = hashCode % _buckets.Length;
            int last = -1;
            for (int i = _buckets[bucket] - 1; i >= 0; last = i, i = _slots[i]._next)
            {
                if (_slots[i]._hashCode == hashCode && _comparer.Equals(_slots[i]._value, value))
                {
                    if (last < 0)
                    {
                        _buckets[bucket] = _slots[i]._next + 1;
                    }
                    else
                    {
                        _slots[last]._next = _slots[i]._next;
                    }

                    _slots[i]._hashCode = -1;
                    _slots[i]._value = default(TElement);
                    _slots[i]._next = -1;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Expands the capacity of this set to double the current capacity, plus one.
        /// </summary>
        private void Resize()
        {
            int newSize = checked((_count * 2) + 1);
            int[] newBuckets = new int[newSize];
            Slot[] newSlots = new Slot[newSize];
            Array.Copy(_slots, 0, newSlots, 0, _count);
            for (int i = 0; i < _count; i++)
            {
                int bucket = newSlots[i]._hashCode % newSize;
                newSlots[i]._next = newBuckets[bucket] - 1;
                newBuckets[bucket] = i + 1;
            }

            _buckets = newBuckets;
            _slots = newSlots;
        }

        /// <summary>
        /// Creates an array from the items in this set.
        /// </summary>
        /// <returns>An array of the items in this set.</returns>
        public TElement[] ToArray()
        {
#if DEBUG
            Debug.Assert(!_haveRemoved, "Optimised ToArray cannot be called if Remove has been called.");
#endif
            TElement[] array = new TElement[_count];
            for (int i = 0; i != array.Length; ++i)
            {
                array[i] = _slots[i]._value;
            }

            return array;
        }

        /// <summary>
        /// Creates a list from the items in this set.
        /// </summary>
        /// <returns>A list of the items in this set.</returns>
        public List<TElement> ToList()
        {
#if DEBUG
            Debug.Assert(!_haveRemoved, "Optimised ToList cannot be called if Remove has been called.");
#endif
            int count = _count;
            List<TElement> list = new List<TElement>(count);
            for (int i = 0; i != count; ++i)
            {
                list.Add(_slots[i]._value);
            }

            return list;
        }

        /// <summary>
        /// The number of items in this set.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Unions this set with an enumerable.
        /// </summary>
        /// <param name="other">The enumerable.</param>
        public void UnionWith(IEnumerable<TElement> other)
        {
            Debug.Assert(other != null);

            foreach (TElement item in other)
            {
                Add(item);
            }
        }

        /// <summary>
        /// Gets the hash code of the provided value with its sign bit zeroed out, so that modulo has a positive result.
        /// </summary>
        /// <param name="value">The value to hash.</param>
        /// <returns>The lower 31 bits of the value's hash code.</returns>
        private int InternalGetHashCode(TElement value) => value == null ? 0 : _comparer.GetHashCode(value) & 0x7FFFFFFF;

        /// <summary>
        /// An entry in the hash set.
        /// </summary>
        private struct Slot
        {
            /// <summary>
            /// The hash code of the item.
            /// </summary>
            internal int _hashCode;

            /// <summary>
            /// In the case of a hash collision, the index of the next slot to probe.
            /// </summary>
            internal int _next;

            /// <summary>
            /// The item held by this slot.
            /// </summary>
            internal TElement _value;
        }
    }
}
