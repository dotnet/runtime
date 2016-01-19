// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace System.Reflection.Internal
{
    internal static class EnumerableExtensions
    {
        private class ComparisonComparer<T> : Comparer<T>
        {
            private readonly Comparison<T> _compare;

            public ComparisonComparer(Comparison<T> compare)
            {
                _compare = compare;
            }

            public override int Compare(T x, T y)
            {
                return _compare(x, y);
            }
        }

        private static class Functions<T>
        {
            public static readonly Func<T, T> Identity = t => t;
        }

        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, IComparer<T> comparer)
        {
            return source.OrderBy(Functions<T>.Identity, comparer);
        }

        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, Comparison<T> compare)
        {
            return source.OrderBy(new ComparisonComparer<T>(compare));
        }
    }
}
