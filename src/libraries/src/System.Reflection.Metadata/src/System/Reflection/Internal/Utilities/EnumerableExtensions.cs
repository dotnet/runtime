// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Replacements for System.Linq to avoid an unnecessary dependency. 
    /// Parameter and return types strengthened to actual internal usage as an optimization.
    /// </summary>
    internal static class EnumerableExtensions
    {
        public static T FirstOrDefault<T>(this ImmutableArray<T> collection, Func<T, bool> predicate)
        {
            foreach (var item in collection)
            {
                if (predicate(item))
                {
                    return item;
                }
            }

            return default(T);
        }

        // used only in debugger display so we needn't get fancy with optimizations.
        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            foreach (var item in source)
            {
                yield return selector(item);
            }
        }

        public static T Last<T>(this ImmutableArray<T>.Builder source)
        {
            return source[source.Count - 1];
        }

        public static List<T> OrderBy<T>(this List<T> source, Comparison<T> comparison)
        {
            var list = new List<T>(source);
            list.Sort(comparison);
            return list;
        }
    }
}
