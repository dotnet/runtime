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
        /// <summary>Computes the sum of a sequence of values.</summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="OverflowException">The sum is larger than <see cref="int.MaxValue"/>.</exception>
        public static ValueTask<int> SumAsync(
            this IAsyncEnumerable<int> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<int> Impl(
                ConfiguredCancelableAsyncEnumerable<int> source)
            {
                int sum = 0;
                await foreach (int item in source)
                {
                    checked { sum += item; }
                }
                return sum;
            }
        }

        /// <summary>Computes the sum of a sequence of values.</summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="OverflowException">The sum is larger than <see cref="long.MaxValue"/>.</exception>
        public static ValueTask<long> SumAsync(
            this IAsyncEnumerable<long> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<long> Impl(
                ConfiguredCancelableAsyncEnumerable<long> source)
            {
                long sum = 0;
                await foreach (long item in source)
                {
                    checked { sum += item; }
                }
                return sum;
            }
        }

        /// <summary>Computes the sum of a sequence of values.</summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static ValueTask<float> SumAsync(
            this IAsyncEnumerable<float> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<float> Impl(
                ConfiguredCancelableAsyncEnumerable<float> source)
            {
                double sum = 0;
                await foreach (float item in source)
                {
                    sum += item;
                }
                return (float)sum;
            }
        }

        /// <summary>Computes the sum of a sequence of values.</summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static ValueTask<double> SumAsync(
            this IAsyncEnumerable<double> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<double> Impl(
                ConfiguredCancelableAsyncEnumerable<double> source)
            {
                double sum = 0;
                await foreach (double item in source)
                {
                    sum += item;
                }
                return sum;
            }
        }

        /// <summary>Computes the sum of a sequence of values.</summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static ValueTask<decimal> SumAsync(
            this IAsyncEnumerable<decimal> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<decimal> Impl(
                ConfiguredCancelableAsyncEnumerable<decimal> source)
            {
                decimal sum = 0;
                await foreach (decimal item in source)
                {
                    sum += item;
                }
                return sum;
            }
        }

        /// <summary>Computes the sum of a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to calculate the sum of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="OverflowException">The sum is larger than <see cref="int.MaxValue"/>.</exception>
        public static ValueTask<int?> SumAsync(
            this IAsyncEnumerable<int?> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<int?> Impl(
                ConfiguredCancelableAsyncEnumerable<int?> source)
            {
                int sum = 0;
                await foreach (int? item in source)
                {
                    if (item is not null)
                    {
                        checked { sum += item.GetValueOrDefault(); }
                    }
                }
                return sum;
            }
        }

        /// <summary>Computes the sum of a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to calculate the sum of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="OverflowException">The sum is larger than <see cref="long.MaxValue"/>.</exception>
        public static ValueTask<long?> SumAsync(
            this IAsyncEnumerable<long?> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<long?> Impl(
                ConfiguredCancelableAsyncEnumerable<long?> source)
            {
                long sum = 0;
                await foreach (long? item in source)
                {
                    if (item is not null)
                    {
                        checked { sum += item.GetValueOrDefault(); }
                    }
                }
                return sum;
            }
        }

        /// <summary>Computes the sum of a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to calculate the sum of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static ValueTask<float?> SumAsync(
            this IAsyncEnumerable<float?> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<float?> Impl(
                ConfiguredCancelableAsyncEnumerable<float?> source)
            {
                double sum = 0;
                await foreach (float? item in source)
                {
                    if (item is not null)
                    {
                        sum += item.GetValueOrDefault();
                    }
                }
                return (float)sum;
            }
        }

        /// <summary>Computes the sum of a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to calculate the sum of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static ValueTask<double?> SumAsync(
            this IAsyncEnumerable<double?> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<double?> Impl(
                ConfiguredCancelableAsyncEnumerable<double?> source)
            {
                double sum = 0;
                await foreach (double? item in source)
                {
                    if (item is not null)
                    {
                        sum += item.GetValueOrDefault();
                    }
                }
                return sum;
            }
        }

        /// <summary>Computes the sum of a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to calculate the sum of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static ValueTask<decimal?> SumAsync(
            this IAsyncEnumerable<decimal?> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<decimal?> Impl(
                ConfiguredCancelableAsyncEnumerable<decimal?> source)
            {
                decimal sum = 0;
                await foreach (decimal? item in source)
                {
                    if (item is not null)
                    {
                        sum += item.GetValueOrDefault();
                    }
                }
                return sum;
            }
        }
    }
}
