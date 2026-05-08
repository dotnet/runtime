// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Converts an <see cref="IEnumerable{T}"/> to an <see cref="IAsyncEnumerable{T}"/>.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> of the elements to enumerate.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> containing the sequence of elements from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If <paramref name="source"/> already implements <see cref="IAsyncEnumerable{T}"/>, it is returned directly.
        /// Otherwise, each iteration through the resulting <see cref="IAsyncEnumerable{T}"/> will iterate through the <paramref name="source"/>.
        /// </remarks>
        public static IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(
            this IEnumerable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source switch
            {
                IAsyncEnumerable<TSource> asyncEnumerable => asyncEnumerable,
                TSource[] array => array.Length == 0 ? Empty<TSource>() : FromArray(array),
                List<TSource> list => FromList(list),
                IList<TSource> list => FromIList(list),
                _ when source == Enumerable.Empty<TSource>() => Empty<TSource>(),
                _ => FromIterator(source),
            };

            static async IAsyncEnumerable<TSource> FromArray(TSource[] source)
            {
                for (int i = 0; ; i++)
                {
                    int localI = i;
                    TSource[] localSource = source;
                    if ((uint)localI >= (uint)localSource.Length)
                    {
                        break;
                    }
                    yield return localSource[localI];
                }
            }

            static async IAsyncEnumerable<TSource> FromList(List<TSource> source)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    yield return source[i];
                }
            }

            static async IAsyncEnumerable<TSource> FromIList(IList<TSource> source)
            {
                int count = source.Count;
                for (int i = 0; i < count; i++)
                {
                    yield return source[i];
                }
            }

            static async IAsyncEnumerable<TSource> FromIterator(IEnumerable<TSource> source)
            {
                foreach (TSource element in source)
                {
                    yield return element;
                }
            }
        }
    }
}
