// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>
        /// Returns the count of elements in the source sequence grouped by key.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence that contains elements to be counted.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> to compare keys with.</param>
        /// <returns>An enumerable containing the frequencies of each key occurrence in <paramref name="source"/>.</returns>
        public static IEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null) where TKey : notnull
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            using IEnumerator<TSource> enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                return [];
            }

            Dictionary<TKey, int> countsBy = new(keyComparer);

            do
            {
                TSource value = enumerator.Current;
                TKey key = keySelector(value);

                ref int currentCount = ref CollectionsMarshal.GetValueRefOrAddDefault(countsBy, key, out _);
                checked
                {
                    currentCount++;
                }
            }
            while (enumerator.MoveNext());

            return countsBy;
        }
    }
}
