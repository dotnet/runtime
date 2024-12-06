// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    internal abstract partial class OrdinalStringFrozenSet
    {
        // We want to avoid having to implement FindItemIndex for each of the multiple types
        // that derive from this one, but each of those needs to supply its own notion of Equals/GetHashCode.
        // To avoid lots of virtual calls, we have every derived type override FindItemIndex and
        // call to that span-based method that's aggressively inlined. That then exposes the implementation
        // to the sealed Equals/GetHashCodes on each derived type, allowing them to be devirtualized and inlined
        // into each unique copy of the code.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override int FindItemIndex<TAlternate>(TAlternate alternate)
        {
            Debug.Assert(typeof(TAlternate) == typeof(ReadOnlySpan<char>));
            ReadOnlySpan<char> item = Unsafe.As<TAlternate, ReadOnlySpan<char>>(ref alternate);

            if ((uint)(item.Length - _minimumLength) <= (uint)_maximumLengthDiff)
            {
                if (CheckLengthQuick((uint)item.Length))
                {
                    int hashCode = GetHashCode(item);
                    _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

                    while (index <= endIndex)
                    {
                        if (hashCode == _hashTable.HashCodes[index] && Equals(item, _items[index]))
                        {
                            return index;
                        }

                        index++;
                    }
                }
            }

            return -1;
        }
    }
}
