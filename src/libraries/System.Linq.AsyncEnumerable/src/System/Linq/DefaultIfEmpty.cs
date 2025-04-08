// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Returns the elements of the specified sequence or the type parameter's default if the sequence is empty.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The sequence to return a default value for if it is empty.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> object that contains the default value for
        /// the TSource type if source is empty; otherwise, source.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TSource?> DefaultIfEmpty<TSource>(
            this IAsyncEnumerable<TSource> source) =>
            DefaultIfEmpty(source, default);

        /// <summary>Returns the elements of the specified sequence or the specified value if the sequence is empty.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The sequence to return a default value for if it is empty.</param>
        /// <param name="defaultValue">The value to return if the sequence is empty.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> object that contains the default value for
        /// the TSource type if source is empty; otherwise, source.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TSource> DefaultIfEmpty<TSource>(
            this IAsyncEnumerable<TSource> source, TSource defaultValue)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source, defaultValue, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                TSource defaultValue,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        do
                        {
                            yield return e.Current;
                        }
                        while (await e.MoveNextAsync().ConfigureAwait(false));
                    }
                    else
                    {
                        yield return defaultValue;
                    }
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
