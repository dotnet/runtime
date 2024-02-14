// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Numerics
{
    /// <summary>Defines a number type.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface INumber<TSelf>
        : IComparable,
          IComparable<TSelf>,
          IComparisonOperators<TSelf, TSelf, bool>,
          IModulusOperators<TSelf, TSelf, TSelf>,
          INumberBase<TSelf>
        where TSelf : INumber<TSelf>?
    {
        /// <summary>Clamps a value to an inclusive minimum and maximum value.</summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The inclusive minimum to which <paramref name="value" /> should clamp.</param>
        /// <param name="max">The inclusive maximum to which <paramref name="value" /> should clamp.</param>
        /// <returns>The result of clamping <paramref name="value" /> to the inclusive range of <paramref name="min" /> and <paramref name="max" />.</returns>
        /// <exception cref="ArgumentException"><paramref name="min" /> is greater than <paramref name="max" />.</exception>
        static virtual TSelf Clamp(TSelf value, TSelf min, TSelf max)
        {
            if (min > max)
            {
                Math.ThrowMinMaxException(min, max);
            }

            TSelf result = value;

            result = TSelf.Max(result, min);
            result = TSelf.Min(result, max);

            return result;
        }

        /// <summary>Copies the sign of a value to the sign of another value.</summary>
        /// <param name="value">The value whose magnitude is used in the result.</param>
        /// <param name="sign">The value whose sign is used in the result.</param>
        /// <returns>A value with the magnitude of <paramref name="value" /> and the sign of <paramref name="sign" />.</returns>
        static virtual TSelf CopySign(TSelf value, TSelf sign)
        {
            TSelf result = value;

            if (TSelf.IsNegative(value) != TSelf.IsNegative(sign))
            {
                result = checked(-result);
            }

            return result;
        }

        /// <summary>Compares two values to compute which is greater.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPoint{TSelf}" /> this method matches the IEEE 754:2019 <c>maximum</c> function. This requires NaN inputs to be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static virtual TSelf Max(TSelf x, TSelf y)
        {
            // This matches the IEEE 754:2019 `maximum` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the larger of the inputs. It
            // treats +0 as larger than -0 as per the specification.

            if (x != y)
            {
                if (!TSelf.IsNaN(x))
                {
                    return y < x ? x : y;
                }

                return x;
            }

            return TSelf.IsNegative(y) ? x : y;
        }

        /// <summary>Compares two values to compute which is greater and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPoint{TSelf}" /> this method matches the IEEE 754:2019 <c>maximumNumber</c> function. This requires NaN inputs to not be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static virtual TSelf MaxNumber(TSelf x, TSelf y)
        {
            // This matches the IEEE 754:2019 `maximumNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the larger of the inputs. It
            // treats +0 as larger than -0 as per the specification.

            if (x != y)
            {
                if (!TSelf.IsNaN(y))
                {
                    return y < x ? x : y;
                }

                return x;
            }

            return TSelf.IsNegative(y) ? x : y;
        }

        /// <summary>Compares two values to compute which is lesser.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPoint{TSelf}" /> this method matches the IEEE 754:2019 <c>minimum</c> function. This requires NaN inputs to be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static virtual TSelf Min(TSelf x, TSelf y)
        {
            // This matches the IEEE 754:2019 `minimum` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the larger of the inputs. It
            // treats +0 as larger than -0 as per the specification.

            if ((x != y) && !TSelf.IsNaN(x))
            {
                return x < y ? x : y;
            }

            return TSelf.IsNegative(x) ? x : y;
        }

        /// <summary>Compares two values to compute which is lesser and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPoint{TSelf}" /> this method matches the IEEE 754:2019 <c>minimumNumber</c> function. This requires NaN inputs to not be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static virtual TSelf MinNumber(TSelf x, TSelf y)
        {
            // This matches the IEEE 754:2019 `minimumNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the larger of the inputs. It
            // treats +0 as larger than -0 as per the specification.

            if (x != y)
            {
                if (!TSelf.IsNaN(y))
                {
                    return x < y ? x : y;
                }

                return x;
            }

            return TSelf.IsNegative(x) ? x : y;
        }

        /// <summary>Computes the sign of a value.</summary>
        /// <param name="value">The value whose sign is to be computed.</param>
        /// <returns>A positive value if <paramref name="value" /> is positive, 0 if <paramref name="value" /> is zero, and a negative value if <paramref name="value" /> is negative.</returns>
        /// <remarks>It is recommended that a function return <c>1</c>, <c>0</c>, and <c>-1</c>, respectively.</remarks>
        static virtual int Sign(TSelf value)
        {
            if (value != TSelf.Zero)
            {
                return TSelf.IsNegative(value) ? -1 : +1;
            }
            return 0;
        }
    }
}
