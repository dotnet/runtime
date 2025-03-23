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
        // TODO https://github.com/dotnet/runtime/issues/111717:
        // Consider before shipping .NET 10 whether this can instead use extension everything to support any IAsyncEnumerable<T> source.
        // Right now it's limited because you can't cast an IAsyncEnumerable<TValueType> to IAsyncEnumerable<object>, but this keeps it in
        // sync with Cast<T>, which needs its shape in support of query comprehensions.

        /// <summary>
        /// Filters the elements of a <see cref="IAsyncEnumerable{Object}"/> based on a specified type <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> whose elements to filter.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements from the input sequence of type <typeparamref name="TResult"/>.</returns>
        public static IAsyncEnumerable<TResult> OfType<TResult>(
            this IAsyncEnumerable<object?> source)
        {
            ThrowHelper.ThrowIfNull(source);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<object?> source,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (object? item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
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
