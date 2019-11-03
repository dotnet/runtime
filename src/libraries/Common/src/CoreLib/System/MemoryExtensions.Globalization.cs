// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public static partial class MemoryExtensions
    {
        /// <summary>
        /// Indicates whether the specified span contains only white-space characters.
        /// </summary>
        public static bool IsWhiteSpace(this ReadOnlySpan<char> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (!char.IsWhiteSpace(span[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns a value indicating whether the specified <paramref name="value"/> occurs within the <paramref name="span"/>.
        /// <param name="span">The source span.</param>
        /// <param name="value">The value to seek within the source span.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="value"/> are compared.</param>
        /// </summary>
        public static bool Contains(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
        {
            return IndexOf(span, value, comparisonType) >= 0;
        }

        /// <summary>
        /// Determines whether this <paramref name="span"/> and the specified <paramref name="other"/> span have the same characters
        /// when compared using the specified <paramref name="comparisonType"/> option.
        /// <param name="span">The source span.</param>
        /// <param name="other">The value to compare with the source span.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="other"/> are compared.</param>
        /// </summary>
        public static bool Equals(this ReadOnlySpan<char> span, ReadOnlySpan<char> other, StringComparison comparisonType)
        {
            string.CheckStringComparison(comparisonType);

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.CompareOptionNone(span, other) == 0;

                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.CompareOptionIgnoreCase(span, other) == 0;

                case StringComparison.InvariantCulture:
                    return CompareInfo.Invariant.CompareOptionNone(span, other) == 0;

                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.CompareOptionIgnoreCase(span, other) == 0;

                case StringComparison.Ordinal:
                    return EqualsOrdinal(span, other);

                case StringComparison.OrdinalIgnoreCase:
                    return EqualsOrdinalIgnoreCase(span, other);
            }

            Debug.Fail("StringComparison outside range");
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsOrdinal(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
        {
            if (span.Length != value.Length)
                return false;
            if (value.Length == 0)  // span.Length == value.Length == 0
                return true;
            return span.SequenceEqual(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsOrdinalIgnoreCase(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
        {
            if (span.Length != value.Length)
                return false;
            if (value.Length == 0)  // span.Length == value.Length == 0
                return true;
            return CompareInfo.EqualsOrdinalIgnoreCase(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(value), span.Length);
        }

        /// <summary>
        /// Compares the specified <paramref name="span"/> and <paramref name="other"/> using the specified <paramref name="comparisonType"/>,
        /// and returns an integer that indicates their relative position in the sort order.
        /// <param name="span">The source span.</param>
        /// <param name="other">The value to compare with the source span.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="other"/> are compared.</param>
        /// </summary>
        public static int CompareTo(this ReadOnlySpan<char> span, ReadOnlySpan<char> other, StringComparison comparisonType)
        {
            string.CheckStringComparison(comparisonType);

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.CompareOptionNone(span, other);

                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.CompareOptionIgnoreCase(span, other);

                case StringComparison.InvariantCulture:
                    return CompareInfo.Invariant.CompareOptionNone(span, other);

                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.CompareOptionIgnoreCase(span, other);

                case StringComparison.Ordinal:
                    if (span.Length == 0 || other.Length == 0)
                        return span.Length - other.Length;
                    return string.CompareOrdinal(span, other);

                case StringComparison.OrdinalIgnoreCase:
                    return CompareInfo.CompareOrdinalIgnoreCase(span, other);
            }

            Debug.Fail("StringComparison outside range");
            return 0;
        }

        /// <summary>
        /// Reports the zero-based index of the first occurrence of the specified <paramref name="value"/> in the current <paramref name="span"/>.
        /// <param name="span">The source span.</param>
        /// <param name="value">The value to seek within the source span.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="value"/> are compared.</param>
        /// </summary>
        public static int IndexOf(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
        {
            string.CheckStringComparison(comparisonType);

            if (value.Length == 0)
            {
                return 0;
            }

            if (span.Length == 0)
            {
                return -1;
            }

            if (comparisonType == StringComparison.Ordinal)
            {
                return SpanHelpers.IndexOf(
                    ref MemoryMarshal.GetReference(span),
                    span.Length,
                    ref MemoryMarshal.GetReference(value),
                    value.Length);
            }

            if (GlobalizationMode.Invariant)
            {
                return CompareInfo.InvariantIndexOf(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType) != CompareOptions.None);
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.IndexOf(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.IndexOf(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));

                default:
                    Debug.Assert(comparisonType == StringComparison.OrdinalIgnoreCase);
                    return CompareInfo.Invariant.IndexOfOrdinalIgnoreCase(span, value);
            }
        }

        /// <summary>
        /// Reports the zero-based index of the last occurrence of the specified <paramref name="value"/> in the current <paramref name="span"/>.
        /// <param name="span">The source span.</param>
        /// <param name="value">The value to seek within the source span.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="value"/> are compared.</param>
        /// </summary>
        public static int LastIndexOf(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
        {
            string.CheckStringComparison(comparisonType);

            if (value.Length == 0)
            {
                return span.Length > 0 ? span.Length - 1 : 0;
            }

            if (span.Length == 0)
            {
                return -1;
            }

            if (GlobalizationMode.Invariant)
            {
                return CompareInfo.InvariantIndexOf(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType) != CompareOptions.None, fromBeginning: false);
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.LastIndexOf(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));

                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return CompareInfo.Invariant.LastIndexOf(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));

                default:
                    Debug.Assert(comparisonType == StringComparison.Ordinal || comparisonType == StringComparison.OrdinalIgnoreCase);
                    return CompareInfo.Invariant.LastIndexOfOrdinal(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType) != CompareOptions.None);
            }
        }

        /// <summary>
        /// Copies the characters from the source span into the destination, converting each character to lowercase,
        /// using the casing rules of the specified culture.
        /// </summary>
        /// <param name="source">The source span.</param>
        /// <param name="destination">The destination span which contains the transformed characters.</param>
        /// <param name="culture">An object that supplies culture-specific casing rules.</param>
        /// <remarks>If <paramref name="culture"/> is null, <see cref="System.Globalization.CultureInfo.CurrentCulture"/> will be used.</remarks>
        /// <returns>The number of characters written into the destination span. If the destination is too small, returns -1.</returns>
        /// <exception cref="InvalidOperationException">The source and destination buffers overlap.</exception>
        public static int ToLower(this ReadOnlySpan<char> source, Span<char> destination, CultureInfo? culture)
        {
            if (source.Overlaps(destination))
                throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);

            culture ??= CultureInfo.CurrentCulture;

            // Assuming that changing case does not affect length
            if (destination.Length < source.Length)
                return -1;

            if (GlobalizationMode.Invariant)
                TextInfo.ToLowerAsciiInvariant(source, destination);
            else
                culture.TextInfo.ChangeCaseToLower(source, destination);
            return source.Length;
        }

        /// <summary>
        /// Copies the characters from the source span into the destination, converting each character to lowercase,
        /// using the casing rules of the invariant culture.
        /// </summary>
        /// <param name="source">The source span.</param>
        /// <param name="destination">The destination span which contains the transformed characters.</param>
        /// <returns>The number of characters written into the destination span. If the destination is too small, returns -1.</returns>
        /// <exception cref="InvalidOperationException">The source and destination buffers overlap.</exception>
        public static int ToLowerInvariant(this ReadOnlySpan<char> source, Span<char> destination)
        {
            if (source.Overlaps(destination))
                throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);

            // Assuming that changing case does not affect length
            if (destination.Length < source.Length)
                return -1;

            if (GlobalizationMode.Invariant)
                TextInfo.ToLowerAsciiInvariant(source, destination);
            else
                CultureInfo.InvariantCulture.TextInfo.ChangeCaseToLower(source, destination);
            return source.Length;
        }

        /// <summary>
        /// Copies the characters from the source span into the destination, converting each character to uppercase,
        /// using the casing rules of the specified culture.
        /// </summary>
        /// <param name="source">The source span.</param>
        /// <param name="destination">The destination span which contains the transformed characters.</param>
        /// <param name="culture">An object that supplies culture-specific casing rules.</param>
        /// <remarks>If <paramref name="culture"/> is null, <see cref="System.Globalization.CultureInfo.CurrentCulture"/> will be used.</remarks>
        /// <returns>The number of characters written into the destination span. If the destination is too small, returns -1.</returns>
        /// <exception cref="InvalidOperationException">The source and destination buffers overlap.</exception>
        public static int ToUpper(this ReadOnlySpan<char> source, Span<char> destination, CultureInfo? culture)
        {
            if (source.Overlaps(destination))
                throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);

            culture ??= CultureInfo.CurrentCulture;

            // Assuming that changing case does not affect length
            if (destination.Length < source.Length)
                return -1;

            if (GlobalizationMode.Invariant)
                TextInfo.ToUpperAsciiInvariant(source, destination);
            else
                culture.TextInfo.ChangeCaseToUpper(source, destination);
            return source.Length;
        }

        /// <summary>
        /// Copies the characters from the source span into the destination, converting each character to uppercase
        /// using the casing rules of the invariant culture.
        /// </summary>
        /// <param name="source">The source span.</param>
        /// <param name="destination">The destination span which contains the transformed characters.</param>
        /// <returns>The number of characters written into the destination span. If the destination is too small, returns -1.</returns>
        /// <exception cref="InvalidOperationException">The source and destination buffers overlap.</exception>
        public static int ToUpperInvariant(this ReadOnlySpan<char> source, Span<char> destination)
        {
            if (source.Overlaps(destination))
                throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);

            // Assuming that changing case does not affect length
            if (destination.Length < source.Length)
                return -1;

            if (GlobalizationMode.Invariant)
                TextInfo.ToUpperAsciiInvariant(source, destination);
            else
                CultureInfo.InvariantCulture.TextInfo.ChangeCaseToUpper(source, destination);
            return source.Length;
        }

        /// <summary>
        /// Determines whether the end of the <paramref name="span"/> matches the specified <paramref name="value"/> when compared using the specified <paramref name="comparisonType"/> option.
        /// </summary>
        /// <param name="span">The source span.</param>
        /// <param name="value">The sequence to compare to the end of the source span.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="value"/> are compared.</param>
        public static bool EndsWith(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
        {
            string.CheckStringComparison(comparisonType);

            if (value.Length == 0)
            {
                return true;
            }

            if (comparisonType >= StringComparison.Ordinal || GlobalizationMode.Invariant)
            {
                if (string.GetCaseCompareOfComparisonCulture(comparisonType) == CompareOptions.None)
                    return span.EndsWith(value);

                return (span.Length >= value.Length) ? (CompareInfo.CompareOrdinalIgnoreCase(span.Slice(span.Length - value.Length), value) == 0) : false;
            }

            if (span.Length == 0)
            {
                return false;
            }

            return (comparisonType >= StringComparison.InvariantCulture) ?
                CompareInfo.Invariant.IsSuffix(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType)) :
                    CultureInfo.CurrentCulture.CompareInfo.IsSuffix(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));
        }

        /// <summary>
        /// Determines whether the beginning of the <paramref name="span"/> matches the specified <paramref name="value"/> when compared using the specified <paramref name="comparisonType"/> option.
        /// </summary>
        /// <param name="span">The source span.</param>
        /// <param name="value">The sequence to compare to the beginning of the source span.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="value"/> are compared.</param>
        public static bool StartsWith(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
        {
            string.CheckStringComparison(comparisonType);

            if (value.Length == 0)
            {
                return true;
            }

            if (comparisonType >= StringComparison.Ordinal || GlobalizationMode.Invariant)
            {
                if (string.GetCaseCompareOfComparisonCulture(comparisonType) == CompareOptions.None)
                    return span.StartsWith(value);

                return (span.Length >= value.Length) ? (CompareInfo.CompareOrdinalIgnoreCase(span.Slice(0, value.Length), value) == 0) : false;
            }

            if (span.Length == 0)
            {
                return false;
            }

            return (comparisonType >= StringComparison.InvariantCulture) ?
                CompareInfo.Invariant.IsPrefix(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType)) :
                    CultureInfo.CurrentCulture.CompareInfo.IsPrefix(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));
        }

        /// <summary>
        /// Returns an enumeration of <see cref="Rune"/> from the provided span.
        /// </summary>
        /// <remarks>
        /// Invalid sequences will be represented in the enumeration by <see cref="Rune.ReplacementChar"/>.
        /// </remarks>
        public static SpanRuneEnumerator EnumerateRunes(this ReadOnlySpan<char> span)
        {
            return new SpanRuneEnumerator(span);
        }

        /// <summary>
        /// Returns an enumeration of <see cref="Rune"/> from the provided span.
        /// </summary>
        /// <remarks>
        /// Invalid sequences will be represented in the enumeration by <see cref="Rune.ReplacementChar"/>.
        /// </remarks>
        public static SpanRuneEnumerator EnumerateRunes(this Span<char> span)
        {
            return new SpanRuneEnumerator(span);
        }
    }
}
