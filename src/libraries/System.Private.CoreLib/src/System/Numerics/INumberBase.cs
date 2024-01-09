// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

namespace System.Numerics
{
    /// <summary>Defines the base of other number types.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface INumberBase<TSelf>
        : IAdditionOperators<TSelf, TSelf, TSelf>,
          IAdditiveIdentity<TSelf, TSelf>,
          IDecrementOperators<TSelf>,
          IDivisionOperators<TSelf, TSelf, TSelf>,
          IEquatable<TSelf>,
          IEqualityOperators<TSelf, TSelf, bool>,
          IIncrementOperators<TSelf>,
          IMultiplicativeIdentity<TSelf, TSelf>,
          IMultiplyOperators<TSelf, TSelf, TSelf>,
          ISpanFormattable,
          ISpanParsable<TSelf>,
          ISubtractionOperators<TSelf, TSelf, TSelf>,
          IUnaryPlusOperators<TSelf, TSelf>,
          IUnaryNegationOperators<TSelf, TSelf>,
          IUtf8SpanFormattable,
          IUtf8SpanParsable<TSelf>
        where TSelf : INumberBase<TSelf>?
    {
        /// <summary>Gets the value <c>1</c> for the type.</summary>
        static abstract TSelf One { get; }

        /// <summary>Gets the radix, or base, for the type.</summary>
        static abstract int Radix { get; }

        /// <summary>Gets the value <c>0</c> for the type.</summary>
        static abstract TSelf Zero { get; }

        /// <summary>Computes the absolute of a value.</summary>
        /// <param name="value">The value for which to get its absolute.</param>
        /// <returns>The absolute of <paramref name="value" />.</returns>
        /// <exception cref="OverflowException">The absolute of <paramref name="value" /> is not representable by <typeparamref name="TSelf" />.</exception>
        static abstract TSelf Abs(TSelf value);

        /// <summary>Creates an instance of the current type from a value, throwing an overflow exception for any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
        /// <returns>An instance of <typeparamref name="TSelf" /> created from <paramref name="value" />.</returns>
        /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <typeparamref name="TSelf" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static virtual TSelf CreateChecked<TOther>(TOther value)
#nullable disable
            where TOther : INumberBase<TOther>
#nullable restore
        {
            TSelf? result;

            if (typeof(TOther) == typeof(TSelf))
            {
                result = (TSelf)(object)value;
            }
            else if (!TSelf.TryConvertFromChecked(value, out result) && !TOther.TryConvertToChecked<TSelf>(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <summary>Creates an instance of the current type from a value, saturating any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
        /// <returns>An instance of <typeparamref name="TSelf" /> created from <paramref name="value" />, saturating if <paramref name="value" /> falls outside the representable range of <typeparamref name="TSelf" />.</returns>
        /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static virtual TSelf CreateSaturating<TOther>(TOther value)
#nullable disable
            where TOther : INumberBase<TOther>
#nullable restore
        {
            TSelf? result;

            if (typeof(TOther) == typeof(TSelf))
            {
                result = (TSelf)(object)value;
            }
            else if (!TSelf.TryConvertFromSaturating(value, out result) && !TOther.TryConvertToSaturating<TSelf>(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <summary>Creates an instance of the current type from a value, truncating any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
        /// <returns>An instance of <typeparamref name="TSelf" /> created from <paramref name="value" />, truncating if <paramref name="value" /> falls outside the representable range of <typeparamref name="TSelf" />.</returns>
        /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static virtual TSelf CreateTruncating<TOther>(TOther value)
#nullable disable
            where TOther : INumberBase<TOther>
#nullable restore
        {
            TSelf? result;

            if (typeof(TOther) == typeof(TSelf))
            {
                result = (TSelf)(object)value;
            }
            else if (!TSelf.TryConvertFromTruncating(value, out result) && !TOther.TryConvertToTruncating<TSelf>(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <summary>Determines if a value is in its canonical representation.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is in its canonical representation; otherwise, <c>false</c>.</returns>
        static abstract bool IsCanonical(TSelf value);

        /// <summary>Determines if a value represents a complex value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is a complex number; otherwise, <c>false</c>.</returns>
        /// <remarks>This function returns <c>false</c> for a complex number <c>a + bi</c> where either <c>a</c> or <c>b</c> is zero. In other words, it excludes real numbers and pure imaginary numbers.</remarks>
        static abstract bool IsComplexNumber(TSelf value);

        /// <summary>Determines if a value represents an even integral value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is an even integer; otherwise, <c>false</c>.</returns>
        /// <remarks>
        ///     <para>This correctly handles floating-point values and so <c>2.0</c> will return <c>true</c> while <c>2.2</c> will return <c>false</c>.</para>
        ///     <para>This functioning returning <c>false</c> does not imply that <see cref="IsOddInteger(TSelf)" /> will return <c>true</c>. A number with a fractional portion, <c>3.3</c>, is not even nor odd.</para>
        /// </remarks>
        static abstract bool IsEvenInteger(TSelf value);

        /// <summary>Determines if a value is finite.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is finite; otherwise, <c>false</c>.</returns>
        /// <remarks>This function returning <c>false</c> does not imply that <see cref="IsInfinity(TSelf)" /> will return <c>true</c>. <c>NaN</c> is not finite nor infinite.</remarks>
        static abstract bool IsFinite(TSelf value);

        /// <summary>Determines if a value represents a pure imaginary value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is a pure imaginary number; otherwise, <c>false</c>.</returns>
        /// <remarks>This function returns <c>false</c> for a complex number <c>a + bi</c> where <c>a</c> is non-zero, as that number is not purely imaginary.</remarks>
        static abstract bool IsImaginaryNumber(TSelf value);

        /// <summary>Determines if a value is infinite.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is infinite; otherwise, <c>false</c>.</returns>
        /// <remarks>This function returning <c>false</c> does not imply that <see cref="IsFinite(TSelf)" /> will return <c>true</c>. <c>NaN</c> is not finite nor infinite.</remarks>
        static abstract bool IsInfinity(TSelf value);

        /// <summary>Determines if a value represents an integral value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is an integer; otherwise, <c>false</c>.</returns>
        /// <remarks>This correctly handles floating-point values and so <c>2.0</c> and <c>3.0</c> will return <c>true</c> while <c>2.2</c> and <c>3.3</c> will return <c>false</c>.</remarks>
        static abstract bool IsInteger(TSelf value);

        /// <summary>Determines if a value is NaN.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is NaN; otherwise, <c>false</c>.</returns>
        static abstract bool IsNaN(TSelf value);

        /// <summary>Determines if a value represents a negative real number.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> represents negative zero or a negative real number; otherwise, <c>false</c>.</returns>
        /// <remarks>
        ///     <para>If this type has signed zero, then <c>-0</c> is also considered negative.</para>
        ///     <para>This function returning <c>false</c> does not imply that <see cref="IsPositive(TSelf)" /> will return <c>true</c>. A complex number, <c>a + bi</c> for non-zero <c>b</c>, is not positive nor negative</para>
        /// </remarks>
        static abstract bool IsNegative(TSelf value);

        /// <summary>Determines if a value is negative infinity.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is negative infinity; otherwise, <c>false</c>.</returns>
        static abstract bool IsNegativeInfinity(TSelf value);

        /// <summary>Determines if a value is normal.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is normal; otherwise, <c>false</c>.</returns>
        static abstract bool IsNormal(TSelf value);

        /// <summary>Determines if a value represents an odd integral value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is an odd integer; otherwise, <c>false</c>.</returns>
        /// <remarks>
        ///     <para>This correctly handles floating-point values and so <c>3.0</c> will return <c>true</c> while <c>3.3</c> will return <c>false</c>.</para>
        ///     <para>This functioning returning <c>false</c> does not imply that <see cref="IsOddInteger(TSelf)" /> will return <c>true</c>. A number with a fractional portion, <c>3.3</c>, is neither even nor odd.</para>
        /// </remarks>
        static abstract bool IsOddInteger(TSelf value);

        /// <summary>Determines if a value represents zero or a positive real number.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> represents (positive) zero or a positive real number; otherwise, <c>false</c>.</returns>
        /// <remarks>
        ///     <para>If this type has signed zero, then <c>-0</c> is not considered positive, but <c>+0</c> is.</para>
        ///     <para>This function returning <c>false</c> does not imply that <see cref="IsNegative(TSelf)" /> will return <c>true</c>. A complex number, <c>a + bi</c> for non-zero <c>b</c>, is not positive nor negative</para>
        /// </remarks>
        static abstract bool IsPositive(TSelf value);

        /// <summary>Determines if a value is positive infinity.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is positive infinity; otherwise, <c>false</c>.</returns>
        static abstract bool IsPositiveInfinity(TSelf value);

        /// <summary>Determines if a value represents a real value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is a real number; otherwise, <c>false</c>.</returns>
        /// <remarks>This function returns <c>true</c> for a complex number <c>a + bi</c> where <c>b</c> is zero.</remarks>
        static abstract bool IsRealNumber(TSelf value);

        /// <summary>Determines if a value is subnormal.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is subnormal; otherwise, <c>false</c>.</returns>
        static abstract bool IsSubnormal(TSelf value);

        /// <summary>Determines if a value is zero.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is zero; otherwise, <c>false</c>.</returns>
        /// <remarks>This function treats both positive and negative zero as zero and so will return <c>true</c> for <c>+0.0</c> and <c>-0.0</c>.</remarks>
        static abstract bool IsZero(TSelf value);

        /// <summary>Compares two values to compute which has the greater magnitude.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a greater magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPointIeee754{TSelf}" /> this method matches the IEEE 754:2019 <c>maximumMagnitude</c> function. This requires NaN inputs to be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static abstract TSelf MaxMagnitude(TSelf x, TSelf y);

        /// <summary>Compares two values to compute which has the greater magnitude and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a greater magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPointIeee754{TSelf}" /> this method matches the IEEE 754:2019 <c>maximumMagnitudeNumber</c> function. This requires NaN inputs to not be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static abstract TSelf MaxMagnitudeNumber(TSelf x, TSelf y);

        /// <summary>Compares two values to compute which has the lesser magnitude.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a lesser magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPointIeee754{TSelf}" /> this method matches the IEEE 754:2019 <c>minimumMagnitude</c> function. This requires NaN inputs to be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static abstract TSelf MinMagnitude(TSelf x, TSelf y);

        /// <summary>Compares two values to compute which has the lesser magnitude and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a lesser magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPointIeee754{TSelf}" /> this method matches the IEEE 754:2019 <c>minimumMagnitudeNumber</c> function. This requires NaN inputs to not be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static abstract TSelf MinMagnitudeNumber(TSelf x, TSelf y);

        /// <summary>Parses a string into a value.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">A bitwise combination of number styles that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <returns>The result of parsing <paramref name="s" />.</returns>
        /// <exception cref="ArgumentException"><paramref name="style" /> is not a supported <see cref="NumberStyles" /> value.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> is not in the correct format.</exception>
        /// <exception cref="OverflowException"><paramref name="s" /> is not representable by <typeparamref name="TSelf" />.</exception>
        static abstract TSelf Parse(string s, NumberStyles style, IFormatProvider? provider);

        /// <summary>Parses a span of characters into a value.</summary>
        /// <param name="s">The span of characters to parse.</param>
        /// <param name="style">A bitwise combination of number styles that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <returns>The result of parsing <paramref name="s" />.</returns>
        /// <exception cref="ArgumentException"><paramref name="style" /> is not a supported <see cref="NumberStyles" /> value.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> is not in the correct format.</exception>
        /// <exception cref="OverflowException"><paramref name="s" /> is not representable by <typeparamref name="TSelf" />.</exception>
        static abstract TSelf Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider);

        /// <summary>Parses a span of UTF-8 characters into a value.</summary>
        /// <param name="utf8Text">The span of UTF-8 characters to parse.</param>
        /// <param name="style">A bitwise combination of number styles that can be present in <paramref name="utf8Text" />.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="utf8Text" />.</param>
        /// <returns>The result of parsing <paramref name="utf8Text" />.</returns>
        /// <exception cref="ArgumentException"><paramref name="style" /> is not a supported <see cref="NumberStyles" /> value.</exception>
        /// <exception cref="FormatException"><paramref name="utf8Text" /> is not in the correct format.</exception>
        /// <exception cref="OverflowException"><paramref name="utf8Text" /> is not representable by <typeparamref name="TSelf" />.</exception>
        static virtual TSelf Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider)
        {
            // Convert text using stackalloc for <= 256 characters and ArrayPool otherwise

            char[]? utf16TextArray;
            scoped Span<char> utf16Text;
            int textMaxCharCount = Encoding.UTF8.GetMaxCharCount(utf8Text.Length);

            if (textMaxCharCount < 256)
            {
                utf16TextArray = null;
                utf16Text = stackalloc char[256];
            }
            else
            {
                utf16TextArray = ArrayPool<char>.Shared.Rent(textMaxCharCount);
                utf16Text = utf16TextArray.AsSpan(0, textMaxCharCount);
            }

            OperationStatus utf8TextStatus = Utf8.ToUtf16(utf8Text, utf16Text, out _, out int utf16TextLength, replaceInvalidSequences: false);

            if (utf8TextStatus != OperationStatus.Done)
            {
                if (utf16TextArray != null)
                {
                    // Return rented buffers if necessary
                    ArrayPool<char>.Shared.Return(utf16TextArray);
                }

                ThrowHelper.ThrowFormatInvalidString();
            }
            utf16Text = utf16Text.Slice(0, utf16TextLength);

            // Actual operation

            TSelf result = TSelf.Parse(utf16Text, style, provider);

            // Return rented buffers if necessary

            if (utf16TextArray != null)
            {
                ArrayPool<char>.Shared.Return(utf16TextArray);
            }

            return result;
        }

        /// <summary>Tries to convert a value to an instance of the current type, throwing an overflow exception for any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
        /// <param name="result">On return, contains an instance of <typeparamref name="TSelf" /> converted from <paramref name="value" />.</param>
        /// <returns><c>false</c> if <typeparamref name="TOther" /> is not supported; otherwise, <c>true</c>.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <typeparamref name="TSelf" />.</exception>
        protected static abstract bool TryConvertFromChecked<TOther>(TOther value, [MaybeNullWhen(false)] out TSelf result)
#nullable disable
            where TOther : INumberBase<TOther>;
#nullable restore

        /// <summary>Tries to convert a value to an instance of the current type, saturating any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
        /// <param name="result">On return, contains an instance of <typeparamref name="TSelf" /> converted from <paramref name="value" />.</param>
        /// <returns><c>false</c> if <typeparamref name="TOther" /> is not supported; otherwise, <c>true</c>.</returns>
        protected static abstract bool TryConvertFromSaturating<TOther>(TOther value, [MaybeNullWhen(false)] out TSelf result)
#nullable disable
            where TOther : INumberBase<TOther>;
#nullable restore

        /// <summary>Tries to convert a value to an instance of the current type, truncating any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
        /// <param name="result">On return, contains an instance of <typeparamref name="TSelf" /> converted from <paramref name="value" />.</param>
        /// <returns><c>false</c> if <typeparamref name="TOther" /> is not supported; otherwise, <c>true</c>.</returns>
        protected static abstract bool TryConvertFromTruncating<TOther>(TOther value, [MaybeNullWhen(false)] out TSelf result)
#nullable disable
            where TOther : INumberBase<TOther>;
#nullable restore

        /// <summary>Tries to convert an instance of the current type to another type, throwing an overflow exception for any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type to which <paramref name="value" /> should be converted.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TOther" />.</param>
        /// <param name="result">On return, contains an instance of <typeparamref name="TOther" /> converted from <paramref name="value" />.</param>
        /// <returns><c>false</c> if <typeparamref name="TOther" /> is not supported; otherwise, <c>true</c>.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <typeparamref name="TOther" />.</exception>
        protected static abstract bool TryConvertToChecked<TOther>(TSelf value, [MaybeNullWhen(false)] out TOther result)
#nullable disable
            where TOther : INumberBase<TOther>;
#nullable restore

        /// <summary>Tries to convert an instance of the current type to another type, saturating any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type to which <paramref name="value" /> should be converted.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TOther" />.</param>
        /// <param name="result">On return, contains an instance of <typeparamref name="TOther" /> converted from <paramref name="value" />.</param>
        /// <returns><c>false</c> if <typeparamref name="TOther" /> is not supported; otherwise, <c>true</c>.</returns>
        protected static abstract bool TryConvertToSaturating<TOther>(TSelf value, [MaybeNullWhen(false)] out TOther result)
#nullable disable
            where TOther : INumberBase<TOther>;
#nullable restore

        /// <summary>Tries to convert an instance of the current type to another type, truncating any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type to which <paramref name="value" /> should be converted.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TOther" />.</param>
        /// <param name="result">On return, contains an instance of <typeparamref name="TOther" /> converted from <paramref name="value" />.</param>
        /// <returns><c>false</c> if <typeparamref name="TOther" /> is not supported; otherwise, <c>true</c>.</returns>
        protected static abstract bool TryConvertToTruncating<TOther>(TSelf value, [MaybeNullWhen(false)] out TOther result)
#nullable disable
            where TOther : INumberBase<TOther>;
#nullable restore

        /// <summary>Tries to parse a string into a value.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">A bitwise combination of number styles that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <param name="result">On return, contains the result of successfully parsing <paramref name="s" /> or an undefined value on failure.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="style" /> is not a supported <see cref="NumberStyles" /> value.</exception>
        static abstract bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out TSelf result);

        /// <summary>Tries to parse a span of characters into a value.</summary>
        /// <param name="s">The span of characters to parse.</param>
        /// <param name="style">A bitwise combination of number styles that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <param name="result">On return, contains the result of successfully parsing <paramref name="s" /> or an undefined value on failure.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="style" /> is not a supported <see cref="NumberStyles" /> value.</exception>
        static abstract bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out TSelf result);

        /// <summary>Tries to parse a span of UTF-8 characters into a value.</summary>
        /// <param name="utf8Text">The span of UTF-8 characters to parse.</param>
        /// <param name="style">A bitwise combination of number styles that can be present in <paramref name="utf8Text" />.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="utf8Text" />.</param>
        /// <param name="result">On return, contains the result of successfully parsing <paramref name="utf8Text" /> or an undefined value on failure.</param>
        /// <returns><c>true</c> if <paramref name="utf8Text" /> was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="style" /> is not a supported <see cref="NumberStyles" /> value.</exception>
        static virtual bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out TSelf result)
        {
            // Convert text using stackalloc for <= 256 characters and ArrayPool otherwise

            char[]? utf16TextArray;
            scoped Span<char> utf16Text;
            int textMaxCharCount = Encoding.UTF8.GetMaxCharCount(utf8Text.Length);

            if (textMaxCharCount < 256)
            {
                utf16TextArray = null;
                utf16Text = stackalloc char[256];
            }
            else
            {
                utf16TextArray = ArrayPool<char>.Shared.Rent(textMaxCharCount);
                utf16Text = utf16TextArray.AsSpan(0, textMaxCharCount);
            }

            OperationStatus utf8TextStatus = Utf8.ToUtf16(utf8Text, utf16Text, out _, out int utf16TextLength, replaceInvalidSequences: false);

            if (utf8TextStatus != OperationStatus.Done)
            {
                if (utf16TextArray != null)
                {
                    // Return rented buffers if necessary
                    ArrayPool<char>.Shared.Return(utf16TextArray);
                }

                result = default;
                return false;
            }
            utf16Text = utf16Text.Slice(0, utf16TextLength);

            // Actual operation

            bool succeeded = TSelf.TryParse(utf16Text, style, provider, out result);

            // Return rented buffers if necessary

            if (utf16TextArray != null)
            {
                ArrayPool<char>.Shared.Return(utf16TextArray);
            }

            return succeeded;
        }

        bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            char[]? utf16DestinationArray;
            scoped Span<char> utf16Destination;
            int destinationMaxCharCount = Encoding.UTF8.GetMaxCharCount(utf8Destination.Length);

            if (destinationMaxCharCount < 256)
            {
                utf16DestinationArray = null;
                utf16Destination = stackalloc char[256];
            }
            else
            {
                utf16DestinationArray = ArrayPool<char>.Shared.Rent(destinationMaxCharCount);
                utf16Destination = utf16DestinationArray.AsSpan(0, destinationMaxCharCount);
            }

            if (!TryFormat(utf16Destination, out int charsWritten, format, provider))
            {
                if (utf16DestinationArray != null)
                {
                    // Return rented buffers if necessary
                    ArrayPool<char>.Shared.Return(utf16DestinationArray);
                }

                bytesWritten = 0;
                return false;
            }

            // Make sure we slice the buffer to just the characters written
            utf16Destination = utf16Destination.Slice(0, charsWritten);

            OperationStatus utf8DestinationStatus = Utf8.FromUtf16(utf16Destination, utf8Destination, out _, out bytesWritten, replaceInvalidSequences: false);

            if (utf16DestinationArray != null)
            {
                // Return rented buffers if necessary
                ArrayPool<char>.Shared.Return(utf16DestinationArray);
            }

            if (utf8DestinationStatus == OperationStatus.Done)
            {
                return true;
            }

            if (utf8DestinationStatus != OperationStatus.DestinationTooSmall)
            {
                ThrowHelper.ThrowInvalidOperationException_InvalidUtf8();
            }

            bytesWritten = 0;
            return false;
        }

        static TSelf IUtf8SpanParsable<TSelf>.Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
        {
            // Convert text using stackalloc for <= 256 characters and ArrayPool otherwise

            char[]? utf16TextArray;
            scoped Span<char> utf16Text;
            int textMaxCharCount = Encoding.UTF8.GetMaxCharCount(utf8Text.Length);

            if (textMaxCharCount < 256)
            {
                utf16TextArray = null;
                utf16Text = stackalloc char[256];
            }
            else
            {
                utf16TextArray = ArrayPool<char>.Shared.Rent(textMaxCharCount);
                utf16Text = utf16TextArray.AsSpan(0, textMaxCharCount);
            }

            OperationStatus utf8TextStatus = Utf8.ToUtf16(utf8Text, utf16Text, out _, out int utf16TextLength, replaceInvalidSequences: false);

            if (utf8TextStatus != OperationStatus.Done)
            {
                if (utf16TextArray != null)
                {
                    // Return rented buffers if necessary
                    ArrayPool<char>.Shared.Return(utf16TextArray);
                }

                ThrowHelper.ThrowFormatInvalidString();
            }
            utf16Text = utf16Text.Slice(0, utf16TextLength);

            // Actual operation

            TSelf result = TSelf.Parse(utf16Text, provider);

            // Return rented buffers if necessary

            if (utf16TextArray != null)
            {
                ArrayPool<char>.Shared.Return(utf16TextArray);
            }

            return result;
        }

        static bool IUtf8SpanParsable<TSelf>.TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(returnValue: false)] out TSelf result)
        {
            // Convert text using stackalloc for <= 256 characters and ArrayPool otherwise

            char[]? utf16TextArray;
            scoped Span<char> utf16Text;
            int textMaxCharCount = Encoding.UTF8.GetMaxCharCount(utf8Text.Length);

            if (textMaxCharCount < 256)
            {
                utf16TextArray = null;
                utf16Text = stackalloc char[256];
            }
            else
            {
                utf16TextArray = ArrayPool<char>.Shared.Rent(textMaxCharCount);
                utf16Text = utf16TextArray.AsSpan(0, textMaxCharCount);
            }

            OperationStatus utf8TextStatus = Utf8.ToUtf16(utf8Text, utf16Text, out _, out int utf16TextLength, replaceInvalidSequences: false);

            if (utf8TextStatus != OperationStatus.Done)
            {
                if (utf16TextArray != null)
                {
                    // Return rented buffers if necessary
                    ArrayPool<char>.Shared.Return(utf16TextArray);
                }

                result = default;
                return false;
            }
            utf16Text = utf16Text.Slice(0, utf16TextLength);

            // Actual operation

            bool succeeded = TSelf.TryParse(utf16Text, provider, out result);

            // Return rented buffers if necessary

            if (utf16TextArray != null)
            {
                ArrayPool<char>.Shared.Return(utf16TextArray);
            }

            return succeeded;
        }
    }
}
