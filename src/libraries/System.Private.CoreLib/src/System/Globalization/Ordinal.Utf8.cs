// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;

namespace System.Globalization
{
    internal static partial class Ordinal
    {
        // Not optimized for large inputs: the only callers (number parsing) pass short sign/NaN/Infinity symbols.
        internal static bool EqualsIgnoreCaseUtf8(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) =>
            MatchIgnoreCaseUtf8(left, right, prefixOnly: false);

        internal static bool StartsWithIgnoreCaseUtf8(ReadOnlySpan<byte> source, ReadOnlySpan<byte> prefix) =>
            MatchIgnoreCaseUtf8(source, prefix, prefixOnly: true);

        private static bool MatchIgnoreCaseUtf8(ReadOnlySpan<byte> source, ReadOnlySpan<byte> prefix, bool prefixOnly)
        {
            // ASCII-only loop without calls, so it stays cheap when inlined into callers
            for (int i = 0; i < prefix.Length; i++)
            {
                if (i >= source.Length)
                {
                    // The source ended before the prefix
                    return false;
                }

                uint a = source[i];
                uint b = prefix[i];

                if ((a | b) > 0x7F)
                {
                    return MatchIgnoreCaseNonAsciiUtf8(source.Slice(i), prefix.Slice(i), prefixOnly);
                }

                // Ordinal equals or lowercase equals if the result ends up in the a-z range
                if ((a != b) && (((a | 0x20) != (b | 0x20)) || !char.IsAsciiLetter((char)a)))
                {
                    return false;
                }
            }

            return prefixOnly || (source.Length == prefix.Length);
        }

        private static bool MatchIgnoreCaseNonAsciiUtf8(ReadOnlySpan<byte> source, ReadOnlySpan<byte> prefix, bool prefixOnly)
        {
            // NOTE: Two UTF-8 inputs of different length might compare as equal under
            // the OrdinalIgnoreCase comparer. This is distinct from UTF-16, where the
            // inputs being different length will mean that they can never compare as
            // equal under an OrdinalIgnoreCase comparer.

            while (!prefix.IsEmpty)
            {
                if (source.IsEmpty)
                {
                    // The source ended before the prefix
                    return false;
                }

                uint a = source[0];
                uint b = prefix[0];

                if ((a | b) <= 0x7F)
                {
                    // Ordinal equals or lowercase equals if the result ends up in the a-z range
                    if ((a != b) && (((a | 0x20) != (b | 0x20)) || !char.IsAsciiLetter((char)a)))
                    {
                        return false;
                    }

                    source = source.Slice(1);
                    prefix = prefix.Slice(1);
                    continue;
                }

                if ((a ^ b) > 0x7F)
                {
                    // No non-ASCII scalar is equal to an ASCII one under ordinal casing
                    return false;
                }

                // NLS/ICU doesn't provide native UTF-8 support so we need to do our own corresponding ordinal comparison
                OperationStatus statusA = Rune.DecodeFromUtf8(source, out Rune runeA, out int bytesConsumedA);
                OperationStatus statusB = Rune.DecodeFromUtf8(prefix, out Rune runeB, out int bytesConsumedB);

                if (statusA != statusB)
                {
                    return false;
                }

                if (statusA == OperationStatus.Done)
                {
                    if ((runeA != runeB) && (Rune.ToUpperOrdinal(runeA) != Rune.ToUpperOrdinal(runeB)))
                    {
                        return false;
                    }
                }
                else if (!source.Slice(0, bytesConsumedA).SequenceEqual(prefix.Slice(0, bytesConsumedB)))
                {
                    // Invalid sequences must match exactly
                    return false;
                }

                source = source.Slice(bytesConsumedA);
                prefix = prefix.Slice(bytesConsumedB);
            }

            return prefixOnly || source.IsEmpty;
        }
    }
}
