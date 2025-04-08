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
        /// <summary>Returns the maximum value in a generic sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}" /> to compare values.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No object in <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="IComparable{T}" /> interface (via the returned task).</exception>
        /// <remarks>
        /// <para>If type <typeparamref name="TSource" /> implements <see cref="IComparable{T}" />, the <see cref="MaxAsync{TSource}(IAsyncEnumerable{TSource}, IComparer{TSource}?, CancellationToken)" /> method uses that implementation to compare values. Otherwise, if type <typeparamref name="TSource" /> implements <see cref="IComparable" />, that implementation is used to compare values.</para>
        /// <para>If <typeparamref name="TSource" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
        /// </remarks>
        public static ValueTask<TSource?> MaxAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            IComparer<TSource>? comparer = null,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            comparer ??= Comparer<TSource>.Default;

            // Special-case float/double/float?/double? to maintain compatibility
            // with System.Linq.Enumerable implementations.
#pragma warning disable CA2012 // Use ValueTasks correctly
            if (typeof(TSource) == typeof(float) && comparer == Comparer<TSource>.Default)
            {
                return (ValueTask<TSource?>)(object)MaxAsync((IAsyncEnumerable<float>)(object)source, cancellationToken);
            }

            if (typeof(TSource) == typeof(double) && comparer == Comparer<TSource>.Default)
            {
                return (ValueTask<TSource?>)(object)MaxAsync((IAsyncEnumerable<double>)(object)source, cancellationToken);
            }

            if (typeof(TSource) == typeof(float?) && comparer == Comparer<TSource>.Default)
            {
                return (ValueTask<TSource?>)(object)MaxAsync((IAsyncEnumerable<float?>)(object)source, cancellationToken);
            }

            if (typeof(TSource) == typeof(double?) && comparer == Comparer<TSource>.Default)
            {
                return (ValueTask<TSource?>)(object)MaxAsync((IAsyncEnumerable<double?>)(object)source, cancellationToken);
            }
#pragma warning restore CA2012

            return Impl(source, comparer, cancellationToken);

            static async ValueTask<TSource?> Impl(
                IAsyncEnumerable<TSource> source,
                IComparer<TSource> comparer,
                CancellationToken cancellationToken)
            {
                TSource? value = default;
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (default(TSource) is null)
                    {
                        do
                        {
                            if (!await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                return value;
                            }

                            value = e.Current;
                        }
                        while (value is null);

                        while (await e.MoveNextAsync().ConfigureAwait(false))
                        {
                            TSource next = e.Current;
                            if (next is not null && comparer.Compare(next, value) > 0)
                            {
                                value = next;
                            }
                        }
                    }
                    else
                    {
                        if (!await e.MoveNextAsync().ConfigureAwait(false))
                        {
                            ThrowHelper.ThrowNoElementsException();
                        }

                        value = e.Current;
                        if (comparer == Comparer<TSource>.Default)
                        {
                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                TSource next = e.Current;
                                if (Comparer<TSource>.Default.Compare(next, value) > 0)
                                {
                                    value = next;
                                }
                            }
                        }
                        else
                        {
                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                TSource next = e.Current;
                                if (comparer.Compare(next, value) > 0)
                                {
                                    value = next;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }

                return value;
            }
        }

        /// <summary>Returns the maximum value in a sequence of values.</summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The maximum value in the sequence.</returns>
        private static async ValueTask<float> MaxAsync(
            this IAsyncEnumerable<float> source,
            CancellationToken cancellationToken)
        {
            IAsyncEnumerator<float> e = source.GetAsyncEnumerator(cancellationToken);
            try
            {
                if (!await e.MoveNextAsync().ConfigureAwait(false))
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                // NaN is ordered less than all other values. We need to do explicit checks to ensure this,
                // but once we've found a value that is not NaN we need no longer worry about it,
                // so first loop until such a value is found (or not, as the case may be).
                float value = e.Current;
                while (float.IsNaN(value))
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        return value;
                    }

                    value = e.Current;
                }

                while (await e.MoveNextAsync().ConfigureAwait(false))
                {
                    float x = e.Current;
                    if (x > value)
                    {
                        value = x;
                    }
                }

                return value;
            }
            finally
            {
                await e.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>Returns the maximum value in a sequence of values.</summary>
        /// <param name="source">A sequence of values to determine the maximum value of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The maximum value in the sequence.</returns>
        private static async ValueTask<double> MaxAsync(
            this IAsyncEnumerable<double> source,
            CancellationToken cancellationToken)
        {
            IAsyncEnumerator<double> e = source.GetAsyncEnumerator(cancellationToken);
            try
            {
                if (!await e.MoveNextAsync().ConfigureAwait(false))
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                // NaN is ordered less than all other values. We need to do explicit checks to ensure this,
                // but once we've found a value that is not NaN we need no longer worry about it,
                // so first loop until such a value is found (or not, as the case may be).
                double value = e.Current;
                while (double.IsNaN(value))
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        return value;
                    }

                    value = e.Current;
                }

                while (await e.MoveNextAsync().ConfigureAwait(false))
                {
                    double x = e.Current;
                    if (x > value)
                    {
                        value = x;
                    }
                }

                return value;
            }
            finally
            {
                await e.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>Returns the maximum value in a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to determine the maximum value of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The maximum value in the sequence.</returns>
        private static async ValueTask<float?> MaxAsync(IAsyncEnumerable<float?> source, CancellationToken cancellationToken)
        {
            float? value = null;
            await foreach (float? x in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (x is null)
                {
                    continue;
                }

                if (value is null || x > value || float.IsNaN((float)value))
                {
                    value = x;
                }
            }

            return value;
        }

        /// <summary>Returns the maximum value in a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to determine the maximum value of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The maximum value in the sequence.</returns>
        private static async ValueTask<double?> MaxAsync(IAsyncEnumerable<double?> source, CancellationToken cancellationToken)
        {
            double? value = null;
            await foreach (double? x in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (x is null)
                {
                    continue;
                }

                if (value is null || x > value || double.IsNaN((double)value))
                {
                    value = x;
                }
            }

            return value;
        }
    }
}
