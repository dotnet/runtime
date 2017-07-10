// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>
    /// Contains helper methods for System.Linq. Please put enumerable-related methods in <see cref="EnumerableHelpers"/>.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// Decides if two equality comparers are equivalent.
        /// </summary>
        /// <typeparam name="TSource">The type of each comparer.</typeparam>
        /// <param name="left">The first comparer.</param>
        /// <param name="right">The second comparer.</param>
        /// <returns><c>true</c> if the equality comparers are equal; otherwise, <c>false</c>.</returns>
        public static bool AreEqualityComparersEqual<TSource>(IEqualityComparer<TSource> left, IEqualityComparer<TSource> right)
        {
            if (left == right)
            {
                return true;
            }

            var defaultComparer = EqualityComparer<TSource>.Default;

            if (left == null)
            {
                // Micro-opt: Typically it's impossible to get a different instance
                // of the default comparer without reflection/serialization.
                // Save a virtual method call to Equals in the common case.
                return right == defaultComparer || right.Equals(defaultComparer);
            }

            if (right == null)
            {
                return left == defaultComparer || left.Equals(defaultComparer);
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Combines two predicates.
        /// </summary>
        /// <typeparam name="TSource">The type of the predicate argument.</typeparam>
        /// <param name="predicate1">The first predicate to run.</param>
        /// <param name="predicate2">The second predicate to run.</param>
        /// <returns>
        /// A new predicate that will evaluate to <c>true</c> only if both the first and
        /// second predicates return true. If the first predicate returns <c>false</c>,
        /// the second predicate will not be run.
        /// </returns>
        public static Func<TSource, bool> CombinePredicates<TSource>(Func<TSource, bool> predicate1, Func<TSource, bool> predicate2) =>
            x => predicate1(x) && predicate2(x);

        /// <summary>
        /// Combines two selectors.
        /// </summary>
        /// <typeparam name="TSource">The type of the first selector's argument.</typeparam>
        /// <typeparam name="TMiddle">The type of the second selector's argument.</typeparam>
        /// <typeparam name="TResult">The type of the second selector's return value.</typeparam>
        /// <param name="selector1">The first selector to run.</param>
        /// <param name="selector2">The second selector to run.</param>
        /// <returns>
        /// A new selector that represents the composition of the first selector with the second selector.
        /// </returns>
        public static Func<TSource, TResult> CombineSelectors<TSource, TMiddle, TResult>(Func<TSource, TMiddle> selector1, Func<TMiddle, TResult> selector2) =>
            x => selector2(selector1(x));
    }
}
