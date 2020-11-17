// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Replacements for System.Linq to avoid an unnecessary dependency.
    /// Parameter and return types strengthened to actual internal usage as an optimization.
    /// </summary>
    internal static class EnumerableExtensions
    {
        public static T? FirstOrDefault<T>(this ImmutableArray<T> collection, Func<T, bool> predicate)
        {
            foreach (var item in collection)
            {
                if (predicate(item))
                {
                    return item;
                }
            }

            return default;
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

        public static IEnumerable<T> OrderBy<T>(this List<T> source, Comparison<T> comparison)
        {
            // Produce an iterator that represents a stable sort of source.
            // Implement by creating an int array that represents the initial ordering of elements in
            // the source list, then sort those integers with a sort function that sorts by the values
            // in source, but for cases where the values are equivalent, sort by initial index in
            // the source array
            int[] map = new int[source.Count];
            for (int i = 0; i < map.Length; i++)
                map[i] = i;

            Array.Sort(map, (int left, int right) =>
            {
                if (left == right)
                    return 0;

                int result = comparison(source[left], source[right]);
                if (result == 0)
                {
                    return left - right;
                }
                return result;
            });

            foreach (int index in map)
            {
                yield return source[index];
            }
        }
    }
}
