// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Generates a sequence that begins with <paramref name="start"/> and yields additional values each incremented by <paramref name="step"/> until <paramref name="endInclusive"/> is reached.</summary>
        /// <typeparam name="T">The type of the value to be yielded in the result sequence.</typeparam>
        /// <param name="start">The starting value. This value will always be included in the resulting sequence.</param>
        /// <param name="endInclusive">The ending bound beyond which values will not be included in the sequence.</param>
        /// <param name="step">The amount by which the next value in the sequence should be incremented from the previous value.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="start"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="endInclusive"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="step"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is NaN.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="endInclusive"/> is NaN.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is NaN.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is greater than zero but <paramref name="endInclusive"/> is less than <paramref name="start"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is less than zero but <paramref name="endInclusive"/> is greater than <paramref name="start"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is zero and <paramref name="endInclusive"/> does not equal <paramref name="start"/>.</exception>
        public static IAsyncEnumerable<T> Sequence<T>(T start, T endInclusive, T step) where T : INumber<T>
        {
            if (start is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(start));
            }

            if (T.IsNaN(start))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(start));
            }

            if (endInclusive is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(endInclusive));
            }

            if (T.IsNaN(endInclusive))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(endInclusive));
            }

            if (step is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(step));
            }

            if (T.IsNaN(step))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(step));
            }

            if (T.IsZero(step))
            {
                // step == 0. If start != endInclusive, then the sequence would be infinite. As such, we validate
                // that they're equal, and if they are, we return a sequence that yields the start/endInclusive value once.
                if (start != endInclusive)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(step));
                }

                return Repeat(start, 1);
            }
            else if (T.IsPositive(step))
            {
                // Presumed to be the most common case, step > 0. Validate that endInclusive >= start, as otherwise we can't easily
                // guarantee that the sequence will terminate.
                if (endInclusive < start)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(endInclusive));
                }

                // Otherwise, just produce an incrementing sequence.
                return IncrementingIterator(start, endInclusive, step);
            }
            else
            {
                // step < 0. Validate that endInclusive <= start, as otherwise we can't easily guarantee that the sequence will terminate.
                if (endInclusive > start)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(endInclusive));
                }

                // Then produce the decrementing sequence.
                return DecrementingIterator(start, endInclusive, step);
            }

            static async IAsyncEnumerable<T> IncrementingIterator(T current, T endInclusive, T step)
            {
                Debug.Assert(step > T.Zero);

                yield return current;

                while (true)
                {
                    T next = current + step;

                    if (next >= endInclusive || next <= current) // handle overflow and saturation
                    {
                        if (next == endInclusive && current != next)
                        {
                            yield return next;
                        }

                        yield break;
                    }

                    yield return next;
                    current = next;
                }
            }


            static async IAsyncEnumerable<T> DecrementingIterator(T current, T endInclusive, T step)
            {
                Debug.Assert(step < T.Zero);

                yield return current;

                while (true)
                {
                    T next = current + step;

                    if (next <= endInclusive || next >= current) // handle overflow and saturation
                    {
                        if (next == endInclusive && current != next)
                        {
                            yield return next;
                        }

                        yield break;
                    }

                    yield return next;
                    current = next;
                }
            }
        }
    }
}
