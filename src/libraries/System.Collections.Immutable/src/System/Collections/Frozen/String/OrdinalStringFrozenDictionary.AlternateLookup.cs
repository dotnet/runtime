// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    internal abstract partial class OrdinalStringFrozenDictionary<TValue> : FrozenDictionary<string, TValue>
    {
        // We want to avoid having to implement GetValueRefOrNullRefCore for each of the multiple types
        // that derive from this one, but each of those needs to supply its own notion of Equals/GetHashCode.
        // To avoid lots of virtual calls, we have every derived type override GetValueRefOrNullRefCore and
        // call to that span-based method that's aggressively inlined. That then exposes the implementation
        // to the sealed Equals/GetHashCodes on each derived type, allowing them to be devirtualized and inlined
        // into each unique copy of the code.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override ref readonly TValue GetValueRefOrNullRefCore<TAlternateKey>(TAlternateKey alternate)
        {
            Debug.Assert(typeof(TAlternateKey) == typeof(ReadOnlySpan<char>));
            ReadOnlySpan<char> key = Unsafe.As<TAlternateKey, ReadOnlySpan<char>>(ref alternate);

            if ((uint)(key.Length - _minimumLength) <= (uint)_maximumLengthDiff)
            {
                if (CheckLengthQuick((uint)key.Length))
                {
                    int hashCode = GetHashCode(key);
                    _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

                    while (index <= endIndex)
                    {
                        if (hashCode == _hashTable.HashCodes[index] && Equals(key, _keys[index]))
                        {
                            return ref _values[index];
                        }

                        index++;
                    }
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
