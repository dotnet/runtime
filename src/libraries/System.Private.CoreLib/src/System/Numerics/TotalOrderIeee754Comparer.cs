// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    /// <summary>
    /// Represents a comparison operation that compares floating-point numbers
    /// with IEEE 754 totalOrder semantic.
    /// </summary>
    /// <typeparam name="T">The type of the numbers to be compared, must be an IEEE 754 floating-point type.</typeparam>
    public readonly struct TotalOrderIeee754Comparer<T> : IComparer<T>, IEqualityComparer<T>, IEquatable<TotalOrderIeee754Comparer<T>>
        where T : IFloatingPointIeee754<T>?
    {
        /// <summary>
        /// Compares two numbers with IEEE 754 totalOrder semantic and returns
        /// a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first number to compare.</param>
        /// <param name="y">The second number to compare.</param>
        /// <returns>
        /// A signed integer that indicates the relative
        /// values of <paramref name="x"/> and <paramref name="y"/>, as shown in the following table.
        /// <list type="table">
        ///   <listheader>
        ///     <term> Value</term>
        ///     <description> Meaning</description>
        ///   </listheader>
        ///   <item>
        ///     <term> Less than zero</term>
        ///     <description><paramref name = "x" /> is less than <paramref name="y" /></description>
        ///   </item>
        ///   <item>
        ///     <term> Zero</term>
        ///     <description><paramref name = "x" /> equals <paramref name="y" /></description>
        ///   </item>
        ///   <item>
        ///     <term> Greater than zero</term>
        ///     <description><paramref name = "x" /> is greater than <paramref name="y" /></description>
        ///   </item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// IEEE 754 specification defines totalOrder as &lt;= semantic.
        /// totalOrder(x,y) is <see langword="true"/> when the result of this method is less than or equal to 0.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T? x, T? y)
        {
            if (typeof(T) == typeof(float))
            {
                return CompareIntegerSemantic(BitConverter.SingleToInt32Bits((float)(object)x!), BitConverter.SingleToInt32Bits((float)(object)y!));
            }
            else if (typeof(T) == typeof(double))
            {
                return CompareIntegerSemantic(BitConverter.DoubleToInt64Bits((double)(object)x!), BitConverter.DoubleToInt64Bits((double)(object)y!));
            }
            else if (typeof(T) == typeof(Half))
            {
                return CompareIntegerSemantic(BitConverter.HalfToInt16Bits((Half)(object)x!), BitConverter.HalfToInt16Bits((Half)(object)y!));
            }
            else if (typeof(T) == typeof(BFloat16))
            {
                return CompareIntegerSemantic(Unsafe.BitCast<BFloat16, short>((BFloat16)(object)x!), Unsafe.BitCast<BFloat16, short>((BFloat16)(object)y!));
            }
            else if (typeof(T) == typeof(NFloat))
            {
                return CompareIntegerSemantic(Unsafe.BitCast<NFloat, nint>((NFloat)(object)x!), Unsafe.BitCast<NFloat, nint>((NFloat)(object)y!));
            }
            else if (typeof(T) == typeof(Decimal32))
            {
                return CompareDecimal<Decimal32, uint>(((Decimal32)(object)x!)._value, ((Decimal32)(object)y!)._value);
            }
            else if (typeof(T) == typeof(Decimal64))
            {
                return CompareDecimal<Decimal64, ulong>(((Decimal64)(object)x!)._value, ((Decimal64)(object)y!)._value);
            }
            else if (typeof(T) == typeof(Decimal128))
            {
                Decimal128 xValue = (Decimal128)(object)x!;
                Decimal128 yValue = (Decimal128)(object)y!;
                return CompareDecimal<Decimal128, UInt128>(new UInt128(xValue._upper, xValue._lower), new UInt128(yValue._upper, yValue._lower));
            }
            else
            {
                return CompareGeneric(x, y);
            }

            static int CompareIntegerSemantic<TInteger>(TInteger x, TInteger y)
                where TInteger : struct, IBinaryInteger<TInteger>, ISignedNumber<TInteger>
            {
                // In IEEE 754 binary floating-point representation, a number is represented as Sign|Exponent|Significand
                // Normal numbers has an implicit 1. in front of the significand, so value with larger exponent will have larger absolute value
                // Inf and NaN are defined as Exponent=All 1s, while Inf has Significand=0, sNaN has Significand=0xxx and qNaN has Significand=1xxx
                // This also satisfies totalOrder definition which is +x < +Inf < +sNaN < +qNaN

                // The order of NaNs of same category and same sign is implementation defined,
                // here we define it as the order of exponent bits to simplify comparison

                // Negative values are represented in sign-magnitude, instead of two's complement like integers
                // Just negating the comparison result when both numbers are negative is enough

                return (TInteger.IsNegative(x) && TInteger.IsNegative(y)) ? y.CompareTo(x) : x.CompareTo(y);
            }

            static int CompareGeneric(T? x, T? y)
            {
                // IComparer contract is null < value

                if (x is null)
                {
                    return (y is null) ? 0 : -1;
                }
                else if (y is null)
                {
                    return 1;
                }

                static int CompareExponent(T x, T y)
                {
                    // Equal values with differing representations only differ by their exponent, so the
                    // unbiased (quantum) exponents are read and compared using signed semantics. For a
                    // given type both exponents share the same byte count.

                    // Prevent stack overflow for huge numbers
                    const int StackAllocThreshold = 256;

                    int xExponentLength = x!.GetExponentByteCount();
                    int yExponentLength = y!.GetExponentByteCount();

                    Span<byte> exponentX = (uint)xExponentLength <= StackAllocThreshold ? stackalloc byte[xExponentLength] : new byte[xExponentLength];
                    Span<byte> exponentY = (uint)yExponentLength <= StackAllocThreshold ? stackalloc byte[yExponentLength] : new byte[yExponentLength];

                    x.WriteExponentBigEndian(exponentX);
                    y.WriteExponentBigEndian(exponentY);

                    // The exponents are two's complement big-endian. Flipping the sign bit of the most
                    // significant byte maps the signed ordering onto an unsigned byte-wise comparison.
                    exponentX[0] ^= 0x80;
                    exponentY[0] ^= 0x80;

                    return exponentX.SequenceCompareTo(exponentY);
                }

                // If < or > returns true, the result satisfies definition of totalOrder too

                if (x < y)
                {
                    return -1;
                }
                else if (x > y)
                {
                    return 1;
                }
                else if (x == y)
                {
                    // Only zeros are equal to zeros, and totalOrder places -0 before +0.
                    if (T.IsZero(x) && (T.IsNegative(x) != T.IsNegative(y)))
                    {
                        return T.IsNegative(x) ? -1 : 1;
                    }

                    // Values that are equal but have differing representations are IEEE 754 decimal cohort
                    // members (e.g. 1.0 and 1.00, or same-signed zeros with differing exponents). These are
                    // ordered by exponent: the value with the smaller exponent is closer to zero for positive
                    // values, and the ordering is reversed for negative values. Binary types have a unique
                    // representation per value, so their exponents already match and this returns 0.

                    int exponentComparison = CompareExponent(x, y);
                    return T.IsNegative(x) ? -exponentComparison : exponentComparison;
                }
                else
                {
                    // One or two of the values are NaN
                    // totalOrder defines that -qNaN < -sNaN < x < +sNaN < + qNaN

                    static unsafe int CompareSignificand(T x, T y)
                    {
                        // IEEE 754 totalOrder only defines the order of NaN type bit (the first bit of significand)
                        // To match the integer semantic comparison above, here we compare all the significand bits

                        // Leave the space for custom floating-point type that has variable significand length

                        int xSignificandBits = x!.GetSignificandBitLength();
                        int ySignificandBits = y!.GetSignificandBitLength();

                        if (xSignificandBits == ySignificandBits)
                        {
                            // Prevent stack overflow for huge numbers
                            const int StackAllocThreshold = 256;

                            int xSignificandLength = x.GetSignificandByteCount();
                            int ySignificandLength = y.GetSignificandByteCount();

                            Span<byte> significandX = (uint)xSignificandLength <= StackAllocThreshold ? stackalloc byte[xSignificandLength] : new byte[xSignificandLength];
                            Span<byte> significandY = (uint)ySignificandLength <= StackAllocThreshold ? stackalloc byte[ySignificandLength] : new byte[ySignificandLength];

                            x.WriteSignificandBigEndian(significandX);
                            y.WriteSignificandBigEndian(significandY);

                            return significandX.SequenceCompareTo(significandY);
                        }
                        else
                        {
                            return xSignificandBits.CompareTo(ySignificandBits);
                        }
                    }

                    if (T.IsNaN(x))
                    {
                        if (T.IsNaN(y))
                        {
                            if (T.IsNegative(x))
                            {
                                return T.IsPositive(y) ? -1 : CompareSignificand(y, x);
                            }
                            else
                            {
                                return T.IsNegative(y) ? 1 : CompareSignificand(x, y);
                            }
                        }
                        else
                        {
                            return T.IsPositive(x) ? 1 : -1;
                        }
                    }
                    else if (T.IsNaN(y))
                    {
                        return T.IsPositive(y) ? -1 : 1;
                    }
                    else
                    {
                        // T does not correctly implement IEEE754 semantics
                        ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
                        return 0; // unreachable
                    }
                }
            }
        }

        // Fast path for the built-in IEEE 754 decimal types. Decimal bit patterns are not monotonic
        // in totalOrder (unlike binary formats), so this replicates the generic totalOrder while
        // unpacking each operand at most twice instead of the four-plus unpacks the generic path incurs.
        private static int CompareDecimal<TDecimal, TValue>(TValue xBits, TValue yBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            bool xIsNaN = TDecimal.IsNaN(xBits);
            bool yIsNaN = TDecimal.IsNaN(yBits);

            if (xIsNaN || yIsNaN)
            {
                // totalOrder places NaNs at the extremes ordered by sign: -qNaN < -sNaN < finite < +sNaN < +qNaN.
                bool xIsNegative = TDecimal.IsNegative(xBits);

                if (xIsNaN && yIsNaN)
                {
                    if (xIsNegative != TDecimal.IsNegative(yBits))
                    {
                        return xIsNegative ? -1 : 1;
                    }

                    // Same-signed NaNs follow totalOrder's +sNaN < +qNaN ordering, then break ties on
                    // the raw payload. The binary formats get this for free because their quiet bit is
                    // the significand's most significant bit; the decimal signaling bit sits above the
                    // payload but is set for signaling (not quiet), so it is folded in inverted to rank
                    // quiet above signaling. Using the raw payload rather than the decoded coefficient
                    // keeps every width consistent (including Decimal128, whose NaN decode is non-canonical).
                    TValue signalingMask = TDecimal.SNaNMask ^ TDecimal.NaNMask;
                    TValue xKey = (xBits & TDecimal.NaNPayloadMask) | ((xBits & signalingMask) ^ signalingMask);
                    TValue yKey = (yBits & TDecimal.NaNPayloadMask) | ((yBits & signalingMask) ^ signalingMask);

                    int payload = xKey.CompareTo(yKey);
                    return xIsNegative ? -payload : payload;
                }

                if (xIsNaN)
                {
                    return xIsNegative ? -1 : 1;
                }

                return TDecimal.IsNegative(yBits) ? 1 : -1;
            }

            int result = Number.CompareDecimalIeee754<TDecimal, TValue>(xBits, yBits);

            if (result != 0)
            {
                return result;
            }

            // The values are numerically equal but may have differing representations (decimal cohort
            // members such as 1.0 and 1.00, or same-signed zeros with differing exponents).
            Number.DecodedDecimalIeee754<TValue> x = Number.UnpackDecimalIeee754<TDecimal, TValue>(xBits);
            Number.DecodedDecimalIeee754<TValue> y = Number.UnpackDecimalIeee754<TDecimal, TValue>(yBits);

            // totalOrder places -0 before +0.
            if ((x.Significand == TValue.Zero) && (x.Signed != y.Signed))
            {
                return x.Signed ? -1 : 1;
            }

            // Cohort members differ only by exponent: the value with the smaller exponent is closer to
            // zero for positive values, and the ordering is reversed for negative values.
            int exponentComparison = x.UnbiasedExponent.CompareTo(y.UnbiasedExponent);
            return x.Signed ? -exponentComparison : exponentComparison;
        }

        /// <summary>
        /// Determines whether the specified numbers are equal.
        /// </summary>
        /// <param name="x">The first number of type <typeparamref name="T"/> to compare.</param>
        /// <param name="y">The second number of type <typeparamref name="T"/> to compare.</param>
        /// <returns><see langword="true"/> if the specified numbers are equal; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// There is no corresponding equals semantic with totalOrder defined by IEEE 754 specification.
        /// This method returns <see langword="true"/> when <see cref="Compare(T?, T?)"/> returns 0.
        /// </remarks>
        public bool Equals(T? x, T? y) => Compare(x, y) == 0;

        /// <summary>
        /// Returns a hash code for the specified number.
        /// </summary>
        /// <param name="obj">The number for which a hash code is to be returned.</param>
        /// <returns>A hash code for the specified number.</returns>
        public int GetHashCode([DisallowNull] T obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            return obj.GetHashCode();
        }

        public bool Equals(TotalOrderIeee754Comparer<T> other) => true;

        /// <summary>Determines whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if the current instance and <paramref name="obj" /> are equal; otherwise, <c>false</c>. If <paramref name="obj" /> is <c>null</c>, the method returns <c>false</c>.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is TotalOrderIeee754Comparer<T>;

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode();
    }
}
