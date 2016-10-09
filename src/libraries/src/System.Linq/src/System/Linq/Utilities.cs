// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    internal static class Utilities
    {
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

        public static Func<TSource, bool> CombinePredicates<TSource>(Func<TSource, bool> predicate1, Func<TSource, bool> predicate2)
        {
            return x => predicate1(x) && predicate2(x);
        }

        public static Func<TSource, TResult> CombineSelectors<TSource, TMiddle, TResult>(Func<TSource, TMiddle> selector1, Func<TMiddle, TResult> selector2)
        {
            return x => selector2(selector1(x));
        }
    }
}
