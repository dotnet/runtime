// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
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

            return CountByIterator(source, keySelector, keyComparer);
        }

#pragma warning disable CA1859
        private static IEnumerable<KeyValuePair<TKey, int>> CountByIterator<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
        {
            return new LazyEnumerable<KeyValuePair<TKey, int>>(() =>
                {
                    using IEnumerator<TSource> enumerator = source.GetEnumerator();

                    if (!enumerator.MoveNext())
                    {
                        return ((IEnumerable<KeyValuePair<TKey, int>>)[]).GetEnumerator();
                    }

                    return BuildCountDictionary(enumerator, keySelector, keyComparer).GetEnumerator();
                });
        }
#pragma warning restore CA1859

        private sealed class LazyEnumerable<T>(Func<IEnumerator<T>> getEnumerator) : IEnumerable<T>
        {
            public IEnumerator<T> GetEnumerator() => getEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static Dictionary<TKey, int> BuildCountDictionary<TSource, TKey>(IEnumerator<TSource> enumerator, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
        {
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
