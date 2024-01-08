// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            IEqualityComparer<TKey>? keyComparer = null) where TKey : notnull
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }
            if (func is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.func);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return AggregateByIterator(source, keySelector, seed, func, keyComparer);
        }

        public static IEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> func,
            IEqualityComparer<TKey>? keyComparer = null) where TKey : notnull
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }
            if (seedSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.seedSelector);
            }
            if (func is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.func);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return AggregateByIterator(source, keySelector, seedSelector, func, keyComparer);
        }

        private static IEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateByIterator<TSource, TKey, TAccumulate>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                yield break;
            }

            foreach (KeyValuePair<TKey, TAccumulate> countBy in PopulateDictionary(enumerator, keySelector, seed, func, keyComparer))
            {
                yield return countBy;
            }

            static Dictionary<TKey, TAccumulate> PopulateDictionary(IEnumerator<TSource> enumerator, Func<TSource, TKey> keySelector, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, IEqualityComparer<TKey>? keyComparer)
            {
                Dictionary<TKey, TAccumulate> dict = new(keyComparer);

                do
                {
                    TSource value = enumerator.Current;
                    TKey key = keySelector(value);

                    ref TAccumulate? acc = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out bool exists);
                    acc = func(exists ? acc! : seed, value);
                }
                while (enumerator.MoveNext());

                return dict;
            }
        }

        private static IEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateByIterator<TSource, TKey, TAccumulate>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TAccumulate> seedSelector, Func<TAccumulate, TSource, TAccumulate> func, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                yield break;
            }

            foreach (KeyValuePair<TKey, TAccumulate> countBy in PopulateDictionary(enumerator, keySelector, seedSelector, func, keyComparer))
            {
                yield return countBy;
            }

            static Dictionary<TKey, TAccumulate> PopulateDictionary(IEnumerator<TSource> enumerator, Func<TSource, TKey> keySelector, Func<TKey, TAccumulate> seedSelector, Func<TAccumulate, TSource, TAccumulate> func, IEqualityComparer<TKey>? keyComparer)
            {
                Dictionary<TKey, TAccumulate> dict = new(keyComparer);

                do
                {
                    TSource value = enumerator.Current;
                    TKey key = keySelector(value);

                    ref TAccumulate? acc = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out bool exists);
                    acc = func(exists ? acc! : seedSelector(key), value);
                }
                while (enumerator.MoveNext());

                return dict;
            }
        }
    }
}
