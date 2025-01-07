// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public static partial class MemoryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsOrdinalIgnoreCaseUtf8(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> value)
        {
            // For UTF-8 ist is possible for two spans of different byte length
            // to compare as equal under an OrdinalIgnoreCase comparison.

            if ((span.Length | value.Length) == 0)  // span.Length == value.Length == 0
            {
                return true;
            }

            return Ordinal.EqualsIgnoreCaseUtf8(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
        }

        /// <summary>
        /// Determines whether the beginning of the <paramref name="span"/> matches the specified <paramref name="value"/> when compared using the specified <paramref name="comparisonType"/> option.
        /// </summary>
        /// <param name="span">The source span.</param>
        /// <param name="value">The sequence to compare to the beginning of the source span.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="value"/> are compared.</param>
        internal static bool StartsWithUtf8(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> value, StringComparison comparisonType)
        {
            string.CheckStringComparison(comparisonType);

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                {
                    return CultureInfo.CurrentCulture.CompareInfo.IsPrefixUtf8(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));
                }

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                {
                    return CompareInfo.Invariant.IsPrefixUtf8(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));
                }

                case StringComparison.Ordinal:
                {
                    return span.StartsWith(value);
                }

                default:
                {
                    Debug.Assert(comparisonType == StringComparison.OrdinalIgnoreCase);
                    return span.StartsWithOrdinalIgnoreCaseUtf8(value);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool StartsWithOrdinalIgnoreCaseUtf8(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> value)
        {
            // For UTF-8 ist is possible for two spans of different byte length
            // to compare as equal under an OrdinalIgnoreCase comparison.

            if ((span.Length | value.Length) == 0)  // span.Length == value.Length == 0
            {
                return true;
            }

            return Ordinal.StartsWithIgnoreCaseUtf8(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
        }
    }
}
