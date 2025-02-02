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
        /// <summary>Returns the minimum value in a generic sequence.</summary>
         /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
         /// <param name="source">A sequence of values to determine the minimum value of.</param>
         /// <param name="comparer">The <see cref="IComparer{T}" /> to compare values.</param>
         /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
         /// <returns>The minimum value in the sequence.</returns>
         /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
         /// <exception cref="ArgumentException">No object in <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="IComparable{T}" /> interface.</exception>
         /// <remarks>
         /// <para>If type <typeparamref name="TSource" /> implements <see cref="IComparable{T}" />, the <see cref="MinAsync{TSource}(IAsyncEnumerable{TSource}, IComparer{TSource}?, CancellationToken)" /> method uses that implementation to compare values. Otherwise, if type <typeparamref name="TSource" /> implements <see cref="IComparable" />, that implementation is used to compare values.</para>
         /// <para>If <typeparamref name="TSource" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
         /// </remarks>
        public static ValueTask<TSource?> MinAsync<TSource>(
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
                return (ValueTask<TSource?>)(object)MinAsync((IAsyncEnumerable<float>)(object)source, cancellationToken);
            }

            if (typeof(TSource) == typeof(double) && comparer == Comparer<TSource>.Default)
            {
                return (ValueTask<TSource?>)(object)MinAsync((IAsyncEnumerable<double>)(object)source, cancellationToken);
            }

            if (typeof(TSource) == typeof(float?) && comparer == Comparer<TSource>.Default)
            {
                return (ValueTask<TSource?>)(object)MinAsync((IAsyncEnumerable<float?>)(object)source, cancellationToken);
            }

            if (typeof(TSource) == typeof(double?) && comparer == Comparer<TSource>.Default)
            {
                return (ValueTask<TSource?>)(object)MinAsync((IAsyncEnumerable<double?>)(object)source, cancellationToken);
            }
#pragma warning restore CA2012

            return Impl(source, comparer, cancellationToken);

            static async ValueTask<TSource?> Impl(IAsyncEnumerable<TSource> source, IComparer<TSource> comparer, CancellationToken cancellationToken)
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
                            if (next is not null && comparer.Compare(next, value) < 0)
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
                                if (Comparer<TSource>.Default.Compare(next, value) < 0)
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
                                if (comparer.Compare(next, value) < 0)
                                {
                                    value = next;
                                }
                            }
                        }
                    }

                    return value;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the minimum value in a sequence of values.</summary>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The minimum value in the sequence.</returns>
        private static async ValueTask<float> MinAsync(
            IAsyncEnumerable<float> source,
            CancellationToken cancellationToken)
        {
            IAsyncEnumerator<float> e = source.GetAsyncEnumerator(cancellationToken);
            try
            {
                if (!await e.MoveNextAsync().ConfigureAwait(false))
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                float value = e.Current;
                if (float.IsNaN(value))
                {
                    return value;
                }

                while (await e.MoveNextAsync().ConfigureAwait(false))
                {
                    float x = e.Current;
                    if (x < value)
                    {
                        value = x;
                    }

                    // Normally NaN < anything is false, as is anything < NaN
                    // However, this leads to some irksome outcomes in Min and Max.
                    // If we use those semantics then Min(NaN, 5.0) is NaN, but
                    // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
                    // ordering where NaN is smaller than every value, including
                    // negative infinity. Not testing for NaN therefore isn't an option, but since we
                    // can't find a smaller value, we can short-circuit.
                    else if (float.IsNaN(x))
                    {
                        return x;
                    }
                }

                return value;

            }
            finally
            {
                await e.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>Returns the minimum value in a sequence of values.</summary>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The minimum value in the sequence.</returns>
        private static async ValueTask<double> MinAsync(
            IAsyncEnumerable<double> source,
            CancellationToken cancellationToken)
        {
            IAsyncEnumerator<double> e = source.GetAsyncEnumerator(cancellationToken);
            try
            {
                if (!await e.MoveNextAsync().ConfigureAwait(false))
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                double value = e.Current;
                if (double.IsNaN(value))
                {
                    return value;
                }

                while (await e.MoveNextAsync().ConfigureAwait(false))
                {
                    double x = e.Current;
                    if (x < value)
                    {
                        value = x;
                    }

                    // Normally NaN < anything is false, as is anything < NaN
                    // However, this leads to some irksome outcomes in Min and Max.
                    // If we use those semantics then Min(NaN, 5.0) is NaN, but
                    // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
                    // ordering where NaN is smaller than every value, including
                    // negative infinity. Not testing for NaN therefore isn't an option, but since we
                    // can't find a smaller value, we can short-circuit.
                    else if (double.IsNaN(x))
                    {
                        return x;
                    }
                }

                return value;

            }
            finally
            {
                await e.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>Returns the minimum value in a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to determine the minimum value of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The minimum value in the sequence.</returns>
        private static async ValueTask<float?> MinAsync(
            IAsyncEnumerable<float?> source,
            CancellationToken cancellationToken)
        {
            float? value = null;
            await foreach (float? x in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (x is null)
                {
                    continue;
                }

                if (value == null || x < value || float.IsNaN(x.GetValueOrDefault()))
                {
                    value = x;
                }
            }

            return value;
        }

        /// <summary>Returns the minimum value in a sequence of nullable values.</summary>
        /// <param name="source">A sequence of nullable values to determine the minimum value of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The minimum value in the sequence.</returns>
        private static async ValueTask<double?> MinAsync(
            IAsyncEnumerable<double?> source,
            CancellationToken cancellationToken)
        {
            double? value = null;
            await foreach (double? x in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (x is null)
                {
                    continue;
                }

                if (value == null || x < value || double.IsNaN(x.GetValueOrDefault()))
                {
                    value = x;
                }
            }

            return value;
        }
    }
}
