// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Numerics
{
    /// <summary>
    /// Represents a comparison operation that compares floating-point numbers
    /// with IEEE754 totalOrder semantic.
    /// </summary>
    /// <typeparam name="T">The type of the numbers to be compared, must be an IEEE754 floating-point type.</typeparam>
    public class TotalOrderIeee754Comparer<T> : IComparer<T>, IEqualityComparer<T>
        where T : IFloatingPointIeee754<T>?
    {
        /// <summary>
        /// Compares two numbers with IEEE754 totalOrder semantic and returns
        /// a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first number to compare.</param>
        /// <param name="y">The second number to compare.</param>
        /// <returns>
        /// A signed integer that indicates the relative
        /// values of <paramref name="x"/> and <paramref name="y"/>.
        /// <list type="bullet">
        ///   <item>
        ///     <description>If less than 0, <paramref name="x" /> is less than <paramref name="y" />.</desciption>
        ///   </item>
        ///   <item>
        ///     <description>If 0, <paramref name="x" /> equals <paramref name="y" />.</desciption>
        ///   </item>
        ///   <item>
        ///     <description>If greater than 0, <paramref name="x" /> is greater than <paramref name="y" />.</desciption>
        ///   </item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// IEEE754 specification defines totalOrder as a &lt;= semantic.
        /// totalOrder(x,y) is true corresponds to the result of this method &lt;= 0.
        /// </remarks>
        public int Compare(T? x, T? y)
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
                if (T.IsZero(x) && T.IsZero(y))
                {
                    if (T.IsNegative(x) && T.IsPositive(y))
                    {
                        return -1;
                    }
                    else if (T.IsPositive(x) && T.IsNegative(y))
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                // One or two of the values are NaN
                // totalOrder defines that -qNaN < -sNaN < x < +sNaN < + qNaN

                if (T.IsNaN(x) && T.IsNaN(y))
                {
                    if (T.IsNegative(x) && T.IsPositive(y))
                    {
                        return -1;
                    }
                    else if (T.IsPositive(x) && T.IsNegative(y))
                    {
                        return 1;
                    }
                    else
                    {
                        // The order of same category of NaN is undefined
                        return 0;
                    }
                }
                else if (T.IsNaN(x))
                {
                    return T.IsPositive(x) ? 1 : -1;
                }
                else if (T.IsNaN(y))
                {
                    return T.IsPositive(y) ? -1 : 1;
                }
                else
                {
                    // T does not correctly implement IEEE754 semantics
                    // return 0 for this case
                    return 0;
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
        /// There is no corresponding equals semantic with totalOrder defined by IEEE754 specification.
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
    }
}
