// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
                    if (T.IsZero(x)) // only zeros are equal to zeros
                    {
                        // IEEE 754 numbers are either positive or negative. Skip check for the opposite.

                        if (T.IsNegative(x))
                        {
                            return T.IsNegative(y) ? 0 : -1;
                        }
                        else
                        {
                            return T.IsPositive(y) ? 0 : 1;
                        }
                    }
                    else
                    {
                        // Equivalant values are compared by their exponent parts,
                        // and the value with smaller exponent is considered closer to zero.

                        // This only applies to IEEE 754 decimals. Consider to add support if decimals are added into .NET.
                        return 0;
                    }
                }
                else
                {
                    // One or two of the values are NaN
                    // totalOrder defines that -qNaN < -sNaN < x < +sNaN < + qNaN

                    static int CompareSignificand(T x, T y)
                    {
                        // IEEE 754 totalOrder only defines the order of NaN type bit (the first bit of significand)
                        // To match the integer semantic comparison above, here we compare all the significand bits
                        // Revisit this if decimals are added

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
            ArgumentNullException.ThrowIfNull(obj, nameof(obj));
            return obj.GetHashCode();
        }

        public bool Equals(TotalOrderIeee754Comparer<T> other) => true;

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is TotalOrderIeee754Comparer<T>;

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode();
    }
}
