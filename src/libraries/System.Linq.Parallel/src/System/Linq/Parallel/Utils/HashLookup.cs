// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// HashLookup.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A simple hash map data structure, derived from the LINQ set we also use.
    /// </summary>
    /// <typeparam name="TKey">The kind of keys contained within.</typeparam>
    /// <typeparam name="TValue">The kind of values contained within.</typeparam>
    internal sealed class HashLookup<TKey, TValue>
    {
        private int[] buckets;
        private Slot[] slots;
        private int count;
        private int freeList;
        private readonly IEqualityComparer<TKey>? comparer;

        private const int HashCodeMask = 0x7fffffff;

        internal HashLookup() : this(null)
        {
        }

        internal HashLookup(IEqualityComparer<TKey>? comparer)
        {
            this.comparer = comparer;
            buckets = new int[7];
            slots = new Slot[7];
            freeList = -1;
        }

        // If value is not in set, add it and return true; otherwise return false
        internal bool Add(TKey key, TValue value)
        {
            return !Find(key, true, false, ref value!);
        }

        // Check whether value is in set
        internal bool TryGetValue(TKey key, [MaybeNullWhen(false), AllowNull] ref TValue value)
        {
            return Find(key, false, false, ref value!);
        }

        internal TValue this[TKey key]
        {
            set
            {
                TValue? v = value;
                Find(key, false, true, ref v);
            }
        }

        private int GetKeyHashCode(TKey key)
        {
            return HashCodeMask &
                (key == null ? 0 : (comparer?.GetHashCode(key) ?? key.GetHashCode()));
        }

        private bool AreKeysEqual(TKey key1, TKey key2)
        {
            return
                (comparer == null ?
                    ((key1 == null && key2 == null) || (key1 != null && key1.Equals(key2))) :
                    comparer.Equals(key1, key2));
        }

        private bool Find(TKey key, bool add, bool set, [MaybeNullWhen(false)] ref TValue value)
        {
            int hashCode = GetKeyHashCode(key);

            for (int i = buckets[(uint)hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
            {
                if (slots[i].hashCode == hashCode && AreKeysEqual(slots[i].key, key))
                {
                    if (set)
                    {
                        slots[i].value = value;
                        return true;
                    }
                    else
                    {
                        value = slots[i].value;
                        return true;
                    }
                }
            }

            if (add)
            {
                int index;
                if (freeList >= 0)
                {
                    index = freeList;
                    freeList = slots[index].next;
                }
                else
                {
                    if (count == slots.Length) Resize();
                    index = count;
                    count++;
                }

                int bucket = hashCode % buckets.Length;
                slots[index].hashCode = hashCode;
                slots[index].key = key;
                slots[index].value = value;
                slots[index].next = buckets[bucket] - 1;
                buckets[bucket] = index + 1;
            }

            return false;
        }

        private void Resize()
        {
            int newSize = checked(count * 2 + 1);
            int[] newBuckets = new int[newSize];
            Slot[] newSlots = new Slot[newSize];
            Array.Copy(slots, newSlots, count);
            for (int i = 0; i < count; i++)
            {
                int bucket = newSlots[i].hashCode % newSize;
                newSlots[i].next = newBuckets[bucket] - 1;
                newBuckets[bucket] = i + 1;
            }
            buckets = newBuckets;
            slots = newSlots;
        }

        internal int Count
        {
            get { return count; }
        }

        internal KeyValuePair<TKey, TValue> this[int index]
        {
            get { return new KeyValuePair<TKey, TValue>(slots[index].key, slots[index].value); }
        }

        internal struct Slot
        {
            internal int hashCode;
            internal int next;
            internal TKey key;
            internal TValue value;
        }
    }
}
