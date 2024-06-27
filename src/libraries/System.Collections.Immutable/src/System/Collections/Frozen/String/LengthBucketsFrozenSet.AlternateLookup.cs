// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    internal sealed partial class LengthBucketsFrozenSet
    {
        private protected override int FindItemIndex<TAlternate>(TAlternate alternate)
        {
            Debug.Assert(typeof(TAlternate) == typeof(ReadOnlySpan<char>));
            ReadOnlySpan<char> item = Unsafe.As<TAlternate, ReadOnlySpan<char>>(ref alternate);

            IAlternateEqualityComparer<ReadOnlySpan<char>, string> comparer = GetAlternateEqualityComparer<ReadOnlySpan<char>>();

            // If the length doesn't have an associated bucket, the key isn't in the dictionary.
            int bucketIndex = (item.Length - _minLength) * LengthBuckets.MaxPerLength;
            int bucketEndIndex = bucketIndex + LengthBuckets.MaxPerLength;
            int[] lengthBuckets = _lengthBuckets;
            if (bucketIndex >= 0 && bucketEndIndex <= lengthBuckets.Length)
            {
                string[] items = _items;

                for (; bucketIndex < bucketEndIndex; bucketIndex++)
                {
                    int index = lengthBuckets[bucketIndex];
                    if ((uint)index < (uint)items.Length)
                    {
                        if (comparer.Equals(item, items[index]))
                        {
                            return index;
                        }
                    }
                    else
                    {
                        // -1 is used to indicate a null, when it's casted to unit it becomes > items.Length
                        break;
                    }
                }
            }

            return -1;
        }
    }
}
