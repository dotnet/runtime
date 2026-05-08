// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>
        /// Filters the elements of a <see cref="IAsyncEnumerable{Object}"/> based on a specified type <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> whose elements to filter.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements from the input sequence of type <typeparamref name="TResult"/>.</returns>
        public static IAsyncEnumerable<TResult> OfType<TResult>(
            this IAsyncEnumerable<object?> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<object?> source,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (object? item in source.WithCancellation(cancellationToken))
                {
                    if (item is TResult target)
                    {
                        yield return target;
                    }
                }
            }
        }
    }
}
