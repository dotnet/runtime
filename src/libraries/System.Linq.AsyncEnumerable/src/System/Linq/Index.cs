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
        /// <summary>Returns an enumerable that incorporates the element's index into a tuple.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The source enumerable providing the elements.</param>
        /// <returns>An enumerable that incorporates each element index into a tuple.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<(int Index, TSource Item)> Index<TSource>(
            this IAsyncEnumerable<TSource> source)
        {
            ThrowHelper.ThrowIfNull(source);

            return
                source.IsKnownEmpty() ? Empty<(int Index, TSource Item)>() :
                Impl(source, default);

            static async IAsyncEnumerable<(int Index, TSource Item)> Impl(
                IAsyncEnumerable<TSource> source,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                int index = -1;
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return (checked(++index), element);
                }
            }
        }
    }
}
