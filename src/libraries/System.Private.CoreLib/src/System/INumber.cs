// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;

#if !FEATURE_GENERIC_MATH
#error FEATURE_GENERIC_MATH is not defined
#endif

namespace System
{
    /// <summary>Defines a number type.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    [RequiresPreviewFeatures]
    public interface INumber<TSelf>
        : IAdditionOperators<TSelf, TSelf, TSelf>,
          IAdditiveIdentity<TSelf, TSelf>,
          IComparisonOperators<TSelf, TSelf>,   // implies IEquatableOperators<TSelf, TSelf>
          IDecrementOperators<TSelf>,
          IDivisionOperators<TSelf, TSelf, TSelf>,
          IIncrementOperators<TSelf>,
          IModulusOperators<TSelf, TSelf, TSelf>,
          IMultiplicativeIdentity<TSelf, TSelf>,
          IMultiplyOperators<TSelf, TSelf, TSelf>,
          ISpanFormattable,                     // implies IFormattable
          ISpanParseable<TSelf>,                // implies IParseable<TSelf>
          ISubtractionOperators<TSelf, TSelf, TSelf>,
          IUnaryNegationOperators<TSelf, TSelf>,
          IUnaryPlusOperators<TSelf, TSelf>
        where TSelf : INumber<TSelf>
    {
        /// <summary>Gets the value <c>1</c> for the type.</summary>
        static abstract TSelf One { get; }

        /// <summary>Gets the value <c>0</c> for the type.</summary>
        static abstract TSelf Zero { get; }

        /// <summary>Computes the absolute of a value.</summary>
        /// <param name="value">The value for which to get its absolute.</param>
        /// <returns>The absolute of <paramref name="value" />.</returns>
        /// <exception cref="OverflowException">The absolute of <paramref name="value" /> is not representable by <typeparamref name="TSelf" />.</exception>
        static abstract TSelf Abs(TSelf value);

        /// <summary>Clamps a value to an inclusive minimum and maximum value.</summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The inclusive minimum to which <paramref name="value" /> should clamp.</param>
        /// <param name="max">The inclusive maximum to which <paramref name="value" /> should clamp.</param>
        /// <returns>The result of clamping <paramref name="value" /> to the inclusive range of <paramref name="min" /> and <paramref name="max" />.</returns>
        /// <exception cref="ArgumentException"><paramref name="min" /> is greater than <paramref name="max" />.</exception>
        static abstract TSelf Clamp(TSelf value, TSelf min, TSelf max);

        /// <summary>Creates an instance of the current type from a value.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
        /// <returns>An instance of <typeparamref name="TSelf" /> created from <paramref name="value" />.</returns>
        /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <typeparamref name="TSelf" />.</exception>
        static abstract TSelf Create<TOther>(TOther value)
            where TOther : INumber<TOther>;

        /// <summary>Creates an instance of the current type from a value, saturating any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
        /// <returns>An instance of <typeparamref name="TSelf" /> created from <paramref name="value" />, saturating if <paramref name="value" /> falls outside the representable range of <typeparamref name="TSelf" />.</returns>
        /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
        static abstract TSelf CreateSaturating<TOther>(TOther value)
            where TOther : INumber<TOther>;

        /// <summary>Creates an instance of the current type from a value, truncating any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
        /// <returns>An instance of <typeparamref name="TSelf" /> created from <paramref name="value" />, truncating if <paramref name="value" /> falls outside the representable range of <typeparamref name="TSelf" />.</returns>
        /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
        static abstract TSelf CreateTruncating<TOther>(TOther value)
            where TOther : INumber<TOther>;

        /// <summary>Computes the quotient and remainder of two values.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The quotient and remainder of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        static abstract (TSelf Quotient, TSelf Remainder) DivRem(TSelf left, TSelf right);

        /// <summary>Compares two values to compute which is greater.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPoint{TSelf}" /> this method matches the IEEE 754:2019 <c>maximum</c> function. This requires NaN inputs to be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static abstract TSelf Max(TSelf x, TSelf y);

        /// <summary>Compares two values to compute which is lesser.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPoint{TSelf}" /> this method matches the IEEE 754:2019 <c>minimum</c> function. This requires NaN inputs to be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static abstract TSelf Min(TSelf x, TSelf y);

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

        /// <summary>Computes the sign of a value.</summary>
        /// <param name="value">The value whose sign is to be computed.</param>
        /// <returns>A positive value if <paramref name="value" /> is positive, <see cref="Zero" /> if <paramref name="value" /> is zero, and a negative value if <paramref name="value" /> is negative.</returns>
        /// <remarks>It is recommended that a function return <c>1</c>, <c>0</c>, and <c>-1</c>, respectively.</remarks>
        static abstract TSelf Sign(TSelf value);

        /// <summary>Tries to create an instance of the current type from a value.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
        /// <param name="result">On return, contains the result of succesfully creating an instance of <typeparamref name="TSelf" /> from <paramref name="value" /> or an undefined value on failure.</param>
        /// <returns><c>true</c> if <paramref name="value" /> an instance of the current type was succesfully created from <paramref name="value" />; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
        static abstract bool TryCreate<TOther>(TOther value, out TSelf result)
            where TOther : INumber<TOther>;

        /// <summary>Tries to parses a string into a value.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">A bitwise combination of number styles that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <param name="result">On return, contains the result of succesfully parsing <paramref name="s" /> or an undefined value on failure.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="style" /> is not a supported <see cref="NumberStyles" /> value.</exception>
        static abstract bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out TSelf result);

        /// <summary>Tries to parses a span of characters into a value.</summary>
        /// <param name="s">The span of characters to parse.</param>
        /// <param name="style">A bitwise combination of number styles that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <param name="result">On return, contains the result of succesfully parsing <paramref name="s" /> or an undefined value on failure.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="style" /> is not a supported <see cref="NumberStyles" /> value.</exception>
        static abstract bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out TSelf result);
    }

    /// <summary>Defines a number that is represented in a base-2 format.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    [RequiresPreviewFeatures]
    public interface IBinaryNumber<TSelf>
        : IBitwiseOperators<TSelf, TSelf, TSelf>,
          INumber<TSelf>
        where TSelf : IBinaryNumber<TSelf>
    {
        /// <summary>Determines if a value is a power of two.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is a power of two; otherwise, <c>false</c>.</returns>
        static abstract bool IsPow2(TSelf value);

        /// <summary>Computes the log2 of a value.</summary>
        /// <param name="value">The value whose log2 is to be computed.</param>
        /// <returns>The log2 of <paramref name="value" />.</returns>
        static abstract TSelf Log2(TSelf value);
    }

    /// <summary>Defines a number type which can represent both positive and negative values.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    [RequiresPreviewFeatures]
    public interface ISignedNumber<TSelf>
        : INumber<TSelf>
        where TSelf : ISignedNumber<TSelf>
    {
        /// <summary>Gets the value <c>-1</c> for the type.</summary>
        static abstract TSelf NegativeOne { get; }
    }

    /// <summary>Defines a number type which can only represent positive values, that is it cannot represent negative values.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    [RequiresPreviewFeatures]
    public interface IUnsignedNumber<TSelf>
        : INumber<TSelf>
        where TSelf : IUnsignedNumber<TSelf>
    {
    }
}
