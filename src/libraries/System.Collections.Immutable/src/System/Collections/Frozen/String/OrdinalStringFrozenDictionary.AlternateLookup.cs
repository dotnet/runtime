// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    internal abstract partial class OrdinalStringFrozenDictionary<TValue> : FrozenDictionary<string, TValue>
    {
        /// <summary>
        /// Invokes <see cref="GetValueRefOrNullRefCoreAlternate(ReadOnlySpan{char})"/>
        /// on instances known to be of type <see cref="OrdinalStringFrozenDictionary{TValue}"/>.
        /// </summary>
        private static readonly AlternateLookupDelegate<ReadOnlySpan<char>> s_alternateLookup = (dictionary, key)
            => ref ((OrdinalStringFrozenDictionary<TValue>)dictionary).GetValueRefOrNullRefCoreAlternate(key);

        /// <inheritdoc/>
        private protected override AlternateLookupDelegate<TAlternateKey> GetAlternateLookupDelegate<TAlternateKey>()
        {
            Debug.Assert(typeof(TAlternateKey) == typeof(ReadOnlySpan<char>));
            return (AlternateLookupDelegate<TAlternateKey>)(object)s_alternateLookup;
        }

        // We want to avoid having to implement GetValueRefOrNullRefCoreAlternate for each of the multiple types
        // that derive from this one, but each of those needs to supply its own notion of Equals/GetHashCode.
        // To avoid lots of virtual calls, we have every derived type override GetValueRefOrNullRefCoreAlternate and
        // call to that span-based method that's aggressively inlined. That then exposes the implementation
        // to the sealed Equals/GetHashCodes on each derived type, allowing them to be devirtualized and inlined
        // into each unique copy of the code.
        /// <inheritdoc cref="GetValueRefOrNullRefCore(string)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected virtual ref readonly TValue GetValueRefOrNullRefCoreAlternate(ReadOnlySpan<char> key)
        {
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

    // See comment above for why these overrides exist. Do not remove.

    internal sealed partial class OrdinalStringFrozenDictionary_Full<TValue>
    {
        private protected override ref readonly TValue GetValueRefOrNullRefCoreAlternate(ReadOnlySpan<char> key) => ref base.GetValueRefOrNullRefCoreAlternate(key);
    }

    internal sealed partial class OrdinalStringFrozenDictionary_FullCaseInsensitive<TValue>
    {
        private protected override ref readonly TValue GetValueRefOrNullRefCoreAlternate(ReadOnlySpan<char> key) => ref base.GetValueRefOrNullRefCoreAlternate(key);
    }

    internal sealed partial class OrdinalStringFrozenDictionary_FullCaseInsensitiveAscii<TValue>
    {
        private protected override ref readonly TValue GetValueRefOrNullRefCoreAlternate(ReadOnlySpan<char> key) => ref base.GetValueRefOrNullRefCoreAlternate(key);
    }

    internal sealed partial class OrdinalStringFrozenDictionary_LeftJustifiedCaseInsensitiveAsciiSubstring<TValue>
    {
        private protected override ref readonly TValue GetValueRefOrNullRefCoreAlternate(ReadOnlySpan<char> key) => ref base.GetValueRefOrNullRefCoreAlternate(key);
    }

    internal sealed partial class OrdinalStringFrozenDictionary_LeftJustifiedCaseInsensitiveSubstring<TValue>
    {
        private protected override ref readonly TValue GetValueRefOrNullRefCoreAlternate(ReadOnlySpan<char> key) => ref base.GetValueRefOrNullRefCoreAlternate(key);
    }

    internal sealed partial class OrdinalStringFrozenDictionary_LeftJustifiedSingleChar<TValue>
    {
        private protected override ref readonly TValue GetValueRefOrNullRefCoreAlternate(ReadOnlySpan<char> key) => ref base.GetValueRefOrNullRefCoreAlternate(key);
    }

    internal sealed partial class OrdinalStringFrozenDictionary_LeftJustifiedSubstring<TValue>
    {
        private protected override ref readonly TValue GetValueRefOrNullRefCoreAlternate(ReadOnlySpan<char> key) => ref base.GetValueRefOrNullRefCoreAlternate(key);
    }

    internal sealed partial class OrdinalStringFrozenDictionary_RightJustifiedCaseInsensitiveAsciiSubstring<TValue>
    {
        private protected override ref readonly TValue GetValueRefOrNullRefCoreAlternate(ReadOnlySpan<char> key) => ref base.GetValueRefOrNullRefCoreAlternate(key);
    }

    internal sealed partial class OrdinalStringFrozenDictionary_RightJustifiedCaseInsensitiveSubstring<TValue>
    {
        private protected override ref readonly TValue GetValueRefOrNullRefCoreAlternate(ReadOnlySpan<char> key) => ref base.GetValueRefOrNullRefCoreAlternate(key);
    }

    internal sealed partial class OrdinalStringFrozenDictionary_RightJustifiedSingleChar<TValue>
    {
        private protected override ref readonly TValue GetValueRefOrNullRefCoreAlternate(ReadOnlySpan<char> key) => ref base.GetValueRefOrNullRefCoreAlternate(key);
    }

    internal sealed partial class OrdinalStringFrozenDictionary_RightJustifiedSubstring<TValue>
    {
        private protected override ref readonly TValue GetValueRefOrNullRefCoreAlternate(ReadOnlySpan<char> key) => ref base.GetValueRefOrNullRefCoreAlternate(key);
    }
}
