// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
#if !NET
        [ThreadStatic]
        private static Random? t_random;
#endif

        /// <summary>Shuffles the order of the elements of a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to shuffle.</param>
        /// <returns>A sequence whose elements correspond to those of the input sequence in randomized order.</returns>
        /// <remarks>Randomization is performed using a non-cryptographically-secure random number generator.</remarks>
        public static IAsyncEnumerable<TSource> Shuffle<TSource>(
            this IAsyncEnumerable<TSource> source)
        {
            ThrowHelper.ThrowIfNull(source);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                Impl(source, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                TSource[] array = await source.ToArrayAsync(cancellationToken).ConfigureAwait(false);

#if NET
                Random.Shared.Shuffle(array);
#else
                Random random = t_random ??= new Random(Environment.TickCount ^ Environment.CurrentManagedThreadId);
                int n = array.Length;
                for (int i = 0; i < n - 1; i++)
                {
                    int j = random.Next(i, n);
                    if (j != i)
                    {
                        TSource temp = array[i];
                        array[i] = array[j];
                        array[j] = temp;
                    }
                }
#endif

                for (int i = 0; i < array.Length; i++)
                {
                    yield return array[i];
                }
            }
        }
    }
}
