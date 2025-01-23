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
        /// <summary>Computes the average of a sequence of values.</summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="OverflowException">The sum of the elements in the sequence is larger than <see cref="long.MaxValue"/> (via the returned task).</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements (via the returned task).</exception>
        public static ValueTask<double> AverageAsync(
            this IAsyncEnumerable<int> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<double> Impl(
                ConfiguredCancelableAsyncEnumerable<int> source)
            {
                long sum = 0;
                long count = 0;
                await foreach (int item in source)
                {
                    checked { sum += item; }
                    count++;
                }

                if (count == 0)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                return (double)sum / count;
            }
        }

        /// <summary>Computes the average of a sequence of values.</summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="OverflowException">The sum of the elements in the sequence is larger than <see cref="long.MaxValue"/> (via the returned task).</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements (via the returned task).</exception>
        public static ValueTask<double> AverageAsync(
            this IAsyncEnumerable<long> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<double> Impl(
                ConfiguredCancelableAsyncEnumerable<long> source)
            {
                long sum = 0;
                long count = 0;
                await foreach (long item in source)
                {
                    checked { sum += item; }
                    count++;
                }

                if (count == 0)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                return (double)sum / count;
            }
        }

        /// <summary>Computes the average of a sequence of values.</summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements (via the returned task).</exception>
        public static ValueTask<float> AverageAsync(
            this IAsyncEnumerable<float> source, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<float> Impl(
                ConfiguredCancelableAsyncEnumerable<float> source)
            {
                double sum = 0;
                long count = 0;
                await foreach (double item in source)
                {
                    sum += item;
                    count++;
                }

                if (count == 0)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                return (float)(sum / count);
            }
        }

        /// <summary>Computes the average of a sequence of values.</summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements (via the returned task).</exception>
        public static ValueTask<double> AverageAsync(
            this IAsyncEnumerable<double> source, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<double> Impl(
                ConfiguredCancelableAsyncEnumerable<double> source)
            {
                double sum = 0;
                long count = 0;
                await foreach (double item in source)
                {
                    sum += item;
                    count++;
                }

                if (count == 0)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                return (double)sum / count;
            }
        }

        /// <summary>Computes the average of a sequence of values.</summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The average of the sequence of values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements (via the returned task).</exception>
        public static ValueTask<decimal> AverageAsync(
            this IAsyncEnumerable<decimal> source, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<decimal> Impl(
                ConfiguredCancelableAsyncEnumerable<decimal> source)
            {
                decimal sum = 0;
                long count = 0;
                await foreach (decimal item in source)
                {
                    sum += item;
                    count++;
                }

                if (count == 0)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                return sum / count;
            }
        }

        /// <summary>Computes the average of a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to calculate the average of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence is empty or contains only values that are null.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="OverflowException">The sum of the elements in the sequence is larger than <see cref="long.MaxValue"/> (via the returned task).</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements (via the returned task).</exception>
        public static ValueTask<double?> AverageAsync(
            this IAsyncEnumerable<int?> source, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<double?> Impl(
                ConfiguredCancelableAsyncEnumerable<int?> source)
            {
                long sum = 0;
                long count = 0;
                await foreach (int? item in source)
                {
                    if (item is int value)
                    {
                        checked { sum += value; }
                        count++;
                    }
                }

                return count != 0 ? (double)sum / count : null;
            }
        }

        /// <summary>Computes the average of a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to calculate the average of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence is empty or contains only values that are null.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="OverflowException">The sum of the elements in the sequence is larger than <see cref="long.MaxValue"/> (via the returned task).</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements (via the returned task).</exception>
        public static ValueTask<double?> AverageAsync(
            this IAsyncEnumerable<long?> source, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<double?> Impl(
                ConfiguredCancelableAsyncEnumerable<long?> source)
            {
                long sum = 0;
                long count = 0;
                await foreach (long? item in source)
                {
                    if (item is long value)
                    {
                        checked { sum += value; }
                        count++;
                    }
                }

                return count != 0 ? (double)sum / count : null;
            }
        }

        /// <summary>Computes the average of a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to calculate the average of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence is empty or contains only values that are null.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements (via the returned task).</exception>
        public static ValueTask<float?> AverageAsync(
            this IAsyncEnumerable<float?> source, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<float?> Impl(
                ConfiguredCancelableAsyncEnumerable<float?> source)
            {
                double sum = 0;
                long count = 0;
                await foreach (float? item in source)
                {
                    if (item is float value)
                    {
                        sum += value;
                        count++;
                    }
                }

                return count != 0 ? (float)(sum / count) : null;
            }
        }

        /// <summary>Computes the average of a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to calculate the average of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence is empty or contains only values that are null.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements (via the returned task).</exception>
        public static ValueTask<double?> AverageAsync(
            this IAsyncEnumerable<double?> source, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<double?> Impl(
                ConfiguredCancelableAsyncEnumerable<double?> source)
            {
                double sum = 0;
                long count = 0;
                await foreach (double? item in source)
                {
                    if (item is double value)
                    {
                        sum += value;
                        count++;
                    }
                }

                return count != 0 ? sum / count : null;
            }
        }

        /// <summary>Computes the average of a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to calculate the average of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence is empty or contains only values that are null.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements (via the returned task).</exception>
        public static ValueTask<decimal?> AverageAsync(
            this IAsyncEnumerable<decimal?> source, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<decimal?> Impl(
                ConfiguredCancelableAsyncEnumerable<decimal?> source)
            {
                decimal sum = 0;
                long count = 0;
                await foreach (decimal? item in source)
                {
                    if (item is decimal value)
                    {
                        sum += value;
                        count++;
                    }
                }

                return count != 0 ? sum / count : null;
            }
        }
    }
}
