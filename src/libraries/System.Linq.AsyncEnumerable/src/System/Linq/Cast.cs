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
        // Right now it's limited because you can't cast an IAsyncEnumerable<TValueType> to IAsyncEnumerable<object>. But the method with this
        // shape is necessary to support query comprehensions with explicit types, e.g. `from string s in asyncEnumerable`.

        /// <summary>
        /// Casts the elements of an <see cref="IAsyncEnumerable{Object}"/> to the specified type.
        /// </summary>
        /// <typeparam name="TResult">The type to cast the elements of source to.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{Object}"/> that contains the elements to be cast to type <typeparamref name="TResult"/>.</param>
        /// <returns>An <see cref="IAsyncEnumerable{TResult}"/> that contains each element of the source sequence cast to the <typeparamref name="TResult"/> type.</returns>
        public static IAsyncEnumerable<TResult> Cast<TResult>( // satisfies the C# query-expression pattern
            this IAsyncEnumerable<object?> source)
        {
            ThrowHelper.ThrowIfNull(source);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                source as IAsyncEnumerable<TResult> ??
                Impl(source, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<object?> source,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (object? item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return (TResult)item!;
                }
            }
        }
    }
}
