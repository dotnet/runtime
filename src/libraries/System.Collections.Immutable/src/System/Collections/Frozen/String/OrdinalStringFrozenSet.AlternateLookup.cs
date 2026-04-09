// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    internal abstract partial class OrdinalStringFrozenSet
    {
        /// <summary>
        /// Invokes <see cref="FindItemIndex(string)"/>
        /// on instances known to be of type <see cref="OrdinalStringFrozenSet"/>.
        /// </summary>
        private static readonly AlternateLookupDelegate<ReadOnlySpan<char>> s_alternateLookup = (set, key)
            => ((OrdinalStringFrozenSet)set).FindItemIndexAlternate(key);

        /// <inheritdoc/>
        private protected override AlternateLookupDelegate<TAlternateKey> GetAlternateLookupDelegate<TAlternateKey>()
        {
            Debug.Assert(typeof(TAlternateKey) == typeof(ReadOnlySpan<char>));
            return (AlternateLookupDelegate<TAlternateKey>)(object)s_alternateLookup;
        }

        // We want to avoid having to implement FindItemIndex for each of the multiple types
        // that derive from this one, but each of those needs to supply its own notion of Equals/GetHashCode.
        // To avoid lots of virtual calls, we have every derived type override FindItemIndex and
        // call to that span-based method that's aggressively inlined. That then exposes the implementation
        // to the sealed Equals/GetHashCodes on each derived type, allowing them to be devirtualized and inlined
        // into each unique copy of the code.
        /// <inheritdoc cref="FindItemIndex(string)" />

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected virtual int FindItemIndexAlternate(ReadOnlySpan<char> item)
        {
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

    // See comment above for why these overrides exist. Do not remove.

    internal sealed partial class OrdinalStringFrozenSet_Full
    {
        private protected override int FindItemIndexAlternate(ReadOnlySpan<char> item) => base.FindItemIndexAlternate(item);
    }

    internal sealed partial class OrdinalStringFrozenSet_FullCaseInsensitive
    {
        private protected override int FindItemIndexAlternate(ReadOnlySpan<char> item) => base.FindItemIndexAlternate(item);
    }

    internal sealed partial class OrdinalStringFrozenSet_FullCaseInsensitiveAscii
    {
        private protected override int FindItemIndexAlternate(ReadOnlySpan<char> item) => base.FindItemIndexAlternate(item);
    }

    internal sealed partial class OrdinalStringFrozenSet_LeftJustifiedCaseInsensitiveAsciiSubstring
    {
        private protected override int FindItemIndexAlternate(ReadOnlySpan<char> item) => base.FindItemIndexAlternate(item);
    }

    internal sealed partial class OrdinalStringFrozenSet_LeftJustifiedCaseInsensitiveSubstring
    {
        private protected override int FindItemIndexAlternate(ReadOnlySpan<char> item) => base.FindItemIndexAlternate(item);
    }

    internal sealed partial class OrdinalStringFrozenSet_LeftJustifiedSingleChar
    {
        private protected override int FindItemIndexAlternate(ReadOnlySpan<char> item) => base.FindItemIndexAlternate(item);
    }

    internal sealed partial class OrdinalStringFrozenSet_LeftJustifiedSubstring
    {
        private protected override int FindItemIndexAlternate(ReadOnlySpan<char> item) => base.FindItemIndexAlternate(item);
    }

    internal sealed partial class OrdinalStringFrozenSet_RightJustifiedCaseInsensitiveAsciiSubstring
    {
        private protected override int FindItemIndexAlternate(ReadOnlySpan<char> item) => base.FindItemIndexAlternate(item);
    }

    internal sealed partial class OrdinalStringFrozenSet_RightJustifiedCaseInsensitiveSubstring
    {
        private protected override int FindItemIndexAlternate(ReadOnlySpan<char> item) => base.FindItemIndexAlternate(item);
    }

    internal sealed partial class OrdinalStringFrozenSet_RightJustifiedSingleChar
    {
        private protected override int FindItemIndexAlternate(ReadOnlySpan<char> item) => base.FindItemIndexAlternate(item);
    }

    internal sealed partial class OrdinalStringFrozenSet_RightJustifiedSubstring
    {
        private protected override int FindItemIndexAlternate(ReadOnlySpan<char> item) => base.FindItemIndexAlternate(item);
    }
}
