namespace System;

/// <summary> Number with total Order => Min, Max and Sign are defined </summary>
public interface IScalarNumber<TSelf, That>
	: ISignedNumber<TSelf, That>, IComparisonOperators<TSelf, That> //, IConvertible
	where TSelf : IScalarNumber<TSelf, That>
{
    static abstract double AsDouble(TSelf value);

    ///// <summary>Gets the mathematical constant <c>pi</c>.</summary>
    //static abstract TSelf Pi { get; }

    ///// <summary>Gets the mathematical constant <c>e</c>.</summary>
    //static abstract TSelf E { get; }

    static abstract bool IsNegative(TSelf value);

    /// <summary>Compares two values to compute which is greater.</summary>
    /// <param name="x">The value to compare with <paramref name="y" />.</param>
    /// <param name="y">The value to compare with <paramref name="x" />.</param>
    /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
    /// <remarks>For <see cref="IFloatingPoint{TSelf}" /> this method matches the IEEE 754:2019 <c>maximum</c> function. This requires NaN inputs to be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
    static abstract That Max(TSelf x, That y);

    /// <summary>Compares two values to compute which is lesser.</summary>
    /// <param name="x">The value to compare with <paramref name="y" />.</param>
    /// <param name="y">The value to compare with <paramref name="x" />.</param>
    /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
    /// <remarks>For <see cref="IFloatingPoint{TSelf}" /> this method matches the IEEE 754:2019 <c>minimum</c> function. This requires NaN inputs to be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
    static abstract That Min(TSelf x, That y);

    /// <summary>Computes the absolute of a value.</summary>
    /// <param name="value">The value for which to get its absolute.</param>
    /// <returns>The absolute of <paramref name="value" />.</returns>
    /// <exception cref="OverflowException">The absolute of <paramref name="value" /> is not representable by <typeparamref name="TSelf" />.</exception>
    static abstract TSelf Abs(TSelf value);

    /// <summary>Computes the sign of a value.</summary>
    /// <param name="value">The value whose sign is to be computed.</param>
    /// <returns>A positive value if <paramref name="value" /> is positive, <see cref="INumber{T,S}.Zero" /> if <paramref name="value" /> is zero, and a negative value if <paramref name="value" /> is negative.</returns>
    /// <remarks>It is recommended that a function return <c>1</c>, <c>0</c>, and <c>-1</c>, respectively.</remarks>
    static abstract That Sign(TSelf value);


    /// <summary>Clamps a value to an inclusive minimum and maximum value.</summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The inclusive minimum to which <paramref name="value" /> should clamp.</param>
    /// <param name="max">The inclusive maximum to which <paramref name="value" /> should clamp.</param>
    /// <returns>The result of clamping <paramref name="value" /> to the inclusive range of <paramref name="min" /> and <paramref name="max" />.</returns>
    /// <exception cref="ArgumentException"><paramref name="min" /> is greater than <paramref name="max" />.</exception>
    static abstract TSelf Clamp(TSelf value, TSelf min, TSelf max);

    /// <summary>Creates an instance of the current type from a value, saturating any values that fall outside the representable range of the current type.</summary>
    /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
    /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
    /// <returns>An instance of <typeparamref name="TSelf" /> created from <paramref name="value" />, saturating if <paramref name="value" /> falls outside the representable range of <typeparamref name="TSelf" />.</returns>
    /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
    static abstract TSelf CreateSaturating<TOther>(TOther value);// where TOther : INumber<TOther>;

    /// <summary>Creates an instance of the current type from a value, truncating any values that fall outside the representable range of the current type.</summary>
    /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
    /// <param name="value">The value which is used to create the instance of <typeparamref name="TSelf" />.</param>
    /// <returns>An instance of <typeparamref name="TSelf" /> created from <paramref name="value" />, truncating if <paramref name="value" /> falls outside the representable range of <typeparamref name="TSelf" />.</returns>
    /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
    static abstract TSelf CreateTruncating<TOther>(TOther value);// where TOther : INumber<TOther>;

}
