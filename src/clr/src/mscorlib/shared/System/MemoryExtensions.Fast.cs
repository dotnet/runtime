// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Extension methods for Span{T}, Memory{T}, and friends.
    /// </summary>
    public static partial class MemoryExtensions
    {
        /// <summary>
        /// Returns a value indicating whether the specified <paramref name="value"/> occurs within the <paramref name="span"/>.
        /// <param name="span">The source span.</param>
        /// <param name="value">The value to seek within the source span.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="value"/> are compared.</param>
        /// </summary>
        public static bool Contains(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
        {
            return (IndexOf(span, value, comparisonType) >= 0);
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
                    return (CultureInfo.CurrentCulture.CompareInfo.CompareOptionNone(span, other) == 0);

                case StringComparison.CurrentCultureIgnoreCase:
                    return (CultureInfo.CurrentCulture.CompareInfo.CompareOptionIgnoreCase(span, other) == 0);

                case StringComparison.InvariantCulture:
                    return (CompareInfo.Invariant.CompareOptionNone(span, other) == 0);

                case StringComparison.InvariantCultureIgnoreCase:
                    return (CompareInfo.Invariant.CompareOptionIgnoreCase(span, other) == 0);

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
            return span.SequenceEqual(value); //TODO: Optimize - https://github.com/dotnet/corefx/issues/27487
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsOrdinalIgnoreCase(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
        {
            if (span.Length != value.Length)
                return false;
            if (value.Length == 0)  // span.Length == value.Length == 0
                return true;
            return (CompareInfo.CompareOrdinalIgnoreCase(span, value) == 0);
        }

        // TODO https://github.com/dotnet/corefx/issues/27526
        internal static bool Contains(this ReadOnlySpan<char> source, char value)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == value)
                {
                    return true;
                }
            }

            return false;
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

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return SpanHelpers.IndexOfCultureHelper(span, value, CultureInfo.CurrentCulture.CompareInfo);

                case StringComparison.CurrentCultureIgnoreCase:
                    return SpanHelpers.IndexOfCultureIgnoreCaseHelper(span, value, CultureInfo.CurrentCulture.CompareInfo);

                case StringComparison.InvariantCulture:
                    return SpanHelpers.IndexOfCultureHelper(span, value, CompareInfo.Invariant);

                case StringComparison.InvariantCultureIgnoreCase:
                    return SpanHelpers.IndexOfCultureIgnoreCaseHelper(span, value, CompareInfo.Invariant);

                case StringComparison.Ordinal:
                    return SpanHelpers.IndexOfOrdinalHelper(span, value, ignoreCase: false);

                case StringComparison.OrdinalIgnoreCase:
                    return SpanHelpers.IndexOfOrdinalHelper(span, value, ignoreCase: true);
            }

            Debug.Fail("StringComparison outside range");
            return -1;
        }

        /// <summary>
        /// Copies the characters from the source span into the destination, converting each character to lowercase,
        /// using the casing rules of the specified culture.
        /// </summary>
        /// <param name="source">The source span.</param>
        /// <param name="destination">The destination span which contains the transformed characters.</param>
        /// <param name="culture">An object that supplies culture-specific casing rules.</param>
        /// <remarks>If the source and destinations overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.</remarks>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="culture"/> is null.
        /// </exception>
        public static int ToLower(this ReadOnlySpan<char> source, Span<char> destination, CultureInfo culture)
        {
            if (culture == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);

            // Assuming that changing case does not affect length
            if (destination.Length < source.Length)
                return -1;

            if (GlobalizationMode.Invariant)
                culture.TextInfo.ToLowerAsciiInvariant(source, destination);
            else
                culture.TextInfo.ChangeCase(source, destination, toUpper: false);
            return source.Length;
        }

        /// <summary>
        /// Copies the characters from the source span into the destination, converting each character to lowercase,
        /// using the casing rules of the invariant culture.
        /// </summary>
        /// <param name="source">The source span.</param>
        /// <param name="destination">The destination span which contains the transformed characters.</param>
        /// <remarks>If the source and destinations overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.</remarks>
        public static int ToLowerInvariant(this ReadOnlySpan<char> source, Span<char> destination)
        {
            // Assuming that changing case does not affect length
            if (destination.Length < source.Length)
                return -1;

            if (GlobalizationMode.Invariant)
                CultureInfo.InvariantCulture.TextInfo.ToLowerAsciiInvariant(source, destination);
            else
                CultureInfo.InvariantCulture.TextInfo.ChangeCase(source, destination, toUpper: false);
            return source.Length;
        }

        /// <summary>
        /// Copies the characters from the source span into the destination, converting each character to uppercase,
        /// using the casing rules of the specified culture.
        /// </summary>
        /// <param name="source">The source span.</param>
        /// <param name="destination">The destination span which contains the transformed characters.</param>
        /// <param name="culture">An object that supplies culture-specific casing rules.</param>
        /// <remarks>If the source and destinations overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.</remarks>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="culture"/> is null.
        /// </exception>
        public static int ToUpper(this ReadOnlySpan<char> source, Span<char> destination, CultureInfo culture)
        {
            if (culture == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);

            // Assuming that changing case does not affect length
            if (destination.Length < source.Length)
                return -1;

            if (GlobalizationMode.Invariant)
                culture.TextInfo.ToUpperAsciiInvariant(source, destination);
            else
                culture.TextInfo.ChangeCase(source, destination, toUpper: true);
            return source.Length;
        }

        /// <summary>
        /// Copies the characters from the source span into the destination, converting each character to uppercase
        /// using the casing rules of the invariant culture.
        /// </summary>
        /// <param name="source">The source span.</param>
        /// <param name="destination">The destination span which contains the transformed characters.</param>
        /// <remarks>If the source and destinations overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.</remarks>
        public static int ToUpperInvariant(this ReadOnlySpan<char> source, Span<char> destination)
        {
            // Assuming that changing case does not affect length
            if (destination.Length < source.Length)
                return -1;

            if (GlobalizationMode.Invariant)
                CultureInfo.InvariantCulture.TextInfo.ToUpperAsciiInvariant(source, destination);
            else
                CultureInfo.InvariantCulture.TextInfo.ChangeCase(source, destination, toUpper: true);
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
            if (value.Length == 0)
            {
                string.CheckStringComparison(comparisonType);
                return true;
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return SpanHelpers.EndsWithCultureHelper(span, value, CultureInfo.CurrentCulture.CompareInfo);

                case StringComparison.CurrentCultureIgnoreCase:
                    return SpanHelpers.EndsWithCultureIgnoreCaseHelper(span, value, CultureInfo.CurrentCulture.CompareInfo);

                case StringComparison.InvariantCulture:
                    return SpanHelpers.EndsWithCultureHelper(span, value, CompareInfo.Invariant);

                case StringComparison.InvariantCultureIgnoreCase:
                    return SpanHelpers.EndsWithCultureIgnoreCaseHelper(span, value, CompareInfo.Invariant);

                case StringComparison.Ordinal:
                    return span.EndsWith(value); //TODO: Optimize - https://github.com/dotnet/corefx/issues/27487

                case StringComparison.OrdinalIgnoreCase:
                    return SpanHelpers.EndsWithOrdinalIgnoreCaseHelper(span, value);

                default:
                    throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }

        /// <summary>
        /// Determines whether the beginning of the <paramref name="span"/> matches the specified <paramref name="value"/> when compared using the specified <paramref name="comparisonType"/> option.
        /// </summary>
        /// <param name="span">The source span.</param>
        /// <param name="value">The sequence to compare to the beginning of the source span.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how the <paramref name="span"/> and <paramref name="value"/> are compared.</param>
        public static bool StartsWith(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
        {
            if (value.Length == 0)
            {
                string.CheckStringComparison(comparisonType);
                return true;
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return SpanHelpers.StartsWithCultureHelper(span, value, CultureInfo.CurrentCulture.CompareInfo);

                case StringComparison.CurrentCultureIgnoreCase:
                    return SpanHelpers.StartsWithCultureIgnoreCaseHelper(span, value, CultureInfo.CurrentCulture.CompareInfo);

                case StringComparison.InvariantCulture:
                    return SpanHelpers.StartsWithCultureHelper(span, value, CompareInfo.Invariant);

                case StringComparison.InvariantCultureIgnoreCase:
                    return SpanHelpers.StartsWithCultureIgnoreCaseHelper(span, value, CompareInfo.Invariant);

                case StringComparison.Ordinal:
                    return span.StartsWith(value); //TODO: Optimize - https://github.com/dotnet/corefx/issues/27487

                case StringComparison.OrdinalIgnoreCase:
                    return SpanHelpers.StartsWithOrdinalIgnoreCaseHelper(span, value);

                default:
                    throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }

        /// <summary>
        /// Creates a new span over the portion of the target array.
        /// </summary>
        public static Span<T> AsSpan<T>(this T[] array, int start)
        {
            if (array == null)
            {
                if (start != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                return default;
            }
            if (default(T) == null && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();
            if ((uint)start > (uint)array.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new Span<T>(ref Unsafe.Add(ref Unsafe.As<byte, T>(ref array.GetRawSzArrayData()), start), array.Length - start);
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        public static ReadOnlySpan<char> AsSpan(this string text)
        {
            if (text == null)
                return default;

            return new ReadOnlySpan<char>(ref text.GetRawStringData(), text.Length);
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;text.Length).
        /// </exception>
        public static ReadOnlySpan<char> AsSpan(this string text, int start)
        {
            if (text == null)
            {
                if (start != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }

            if ((uint)start > (uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            return new ReadOnlySpan<char>(ref Unsafe.Add(ref text.GetRawStringData(), start), text.Length - start);
        }

        /// <summary>
        /// Creates a new readonly span over the portion of the target string.
        /// </summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index or <paramref name="length"/> is not in range.
        /// </exception>
        public static ReadOnlySpan<char> AsSpan(this string text, int start, int length)
        {
            if (text == null)
            {
                if (start != 0 || length != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }

            if ((uint)start > (uint)text.Length || (uint)length > (uint)(text.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            return new ReadOnlySpan<char>(ref Unsafe.Add(ref text.GetRawStringData(), start), length);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.</summary>
        /// <param name="text">The target string.</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        public static ReadOnlyMemory<char> AsMemory(this string text)
        {
            if (text == null)
                return default;

            return new ReadOnlyMemory<char>(text, 0, text.Length);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.</summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;text.Length).
        /// </exception>
        public static ReadOnlyMemory<char> AsMemory(this string text, int start)
        {
            if (text == null)
            {
                if (start != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }

            if ((uint)start > (uint)text.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            return new ReadOnlyMemory<char>(text, start, text.Length - start);
        }

        /// <summary>Creates a new <see cref="ReadOnlyMemory{T}"/> over the portion of the target string.</summary>
        /// <param name="text">The target string.</param>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <remarks>Returns default when <paramref name="text"/> is null.</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index or <paramref name="length"/> is not in range.
        /// </exception>
        public static ReadOnlyMemory<char> AsMemory(this string text, int start, int length)
        {
            if (text == null)
            {
                if (start != 0 || length != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
                return default;
            }

            if ((uint)start > (uint)text.Length || (uint)length > (uint)(text.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);

            return new ReadOnlyMemory<char>(text, start, length);
        }
    }
}
