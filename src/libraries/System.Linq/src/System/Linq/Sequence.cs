// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Generates a sequence that begins with <paramref name="start"/> and yields additional values each incremented by <paramref name="step"/> until <paramref name="endInclusive"/> is reached.</summary>
        /// <typeparam name="T">The type of the value to be yielded in the result sequence.</typeparam>
        /// <param name="start">The starting value. This value will always be included in the resulting sequence.</param>
        /// <param name="endInclusive">The ending bound beyond which values will not be included in the sequence.</param>
        /// <param name="step">The amount by which the next value in the sequence should be incremented from the previous value.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that contains the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="start"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="endInclusive"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="step"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is NaN.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="endInclusive"/> is NaN.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is NaN.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is greater than zero but <paramref name="endInclusive"/> is less than <paramref name="start"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is less than zero but <paramref name="endInclusive"/> is greater than <paramref name="start"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is zero and <paramref name="endInclusive"/> does not equal <paramref name="start"/>.</exception>
        public static IEnumerable<T> Sequence<T>(T start, T endInclusive, T step) where T : INumber<T>
        {
            if (start is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.start);
            }

            if (T.IsNaN(start))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
            }

            if (endInclusive is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.endInclusive);
            }

            if (T.IsNaN(endInclusive))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.endInclusive);
            }

            if (step is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.step);
            }

            if (T.IsNaN(step))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.step);
            }

            if (T.IsZero(step))
            {
                // If start != endInclusive, then the sequence would be infinite. As such, we validate
                // that they're equal, and if they are, we return a sequence that yields the start/endInclusive value once.
                if (start != endInclusive)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.step);
                }

                return Repeat(start, 1);
            }
            else if (T.IsPositive(step))
            {
                // Presumed to be the most common case, step > 0. Validate that endInclusive >= start, as otherwise we can't easily
                // guarantee that the sequence will terminate.
                if (endInclusive < start)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.endInclusive);
                }

                // If we're dealing with a known primitive integer type and the step is 1 and the number of elements to yield is within the bounds of that type,
                // we can reuse Range's iterator, which provides better performance, e.g. it implements IList<T>, has optimizations for Select, etc.
                RangeIterator<T>? range;
                if (typeof(T) == typeof(byte) && (range = TryUseRange<ushort>(start, endInclusive, step, byte.MaxValue)) is not null) return range;
                if (typeof(T) == typeof(sbyte) && (range = TryUseRange<short>(start, endInclusive, step, sbyte.MaxValue)) is not null) return range;
                if (typeof(T) == typeof(ushort) && (range = TryUseRange<uint>(start, endInclusive, step, ushort.MaxValue)) is not null) return range;
                if (typeof(T) == typeof(char) && (range = TryUseRange<uint>(start, endInclusive, step, char.MaxValue)) is not null) return range;
                if (typeof(T) == typeof(short) && (range = TryUseRange<int>(start, endInclusive, step, short.MaxValue)) is not null) return range;
                if (typeof(T) == typeof(uint) && (range = TryUseRange<ulong>(start, endInclusive, step, uint.MaxValue)) is not null) return range;
                if (typeof(T) == typeof(int) && (range = TryUseRange<long>(start, endInclusive, step, int.MaxValue)) is not null) return range;
                if (typeof(T) == typeof(ulong) && (range = TryUseRange<UInt128>(start, endInclusive, step, ulong.MaxValue)) is not null) return range;
                if (typeof(T) == typeof(long) && (range = TryUseRange<Int128>(start, endInclusive, step, long.MaxValue)) is not null) return range;
                if (typeof(T) == typeof(nuint) && (range = TryUseRange<UInt128>(start, endInclusive, step, nuint.MaxValue)) is not null) return range;
                if (typeof(T) == typeof(nint) && (range = TryUseRange<Int128>(start, endInclusive, step, nint.MaxValue)) is not null) return range;

                // Otherwise, just produce an incrementing sequence.
                return IncrementingIterator(start, endInclusive, step);
            }
            else
            {
                // step < 0. Validate that endInclusive <= start, as otherwise we can't easily guarantee that the sequence will terminate.
                if (endInclusive > start)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.endInclusive);
                }

                // Then produce the decrementing sequence.
                return DecrementingIterator(start, endInclusive, step);
            }

            static RangeIterator<T>? TryUseRange<TLarger>(T start, T endInclusive, T step, TLarger maxValue) where TLarger : INumber<TLarger>
            {
                if (step == T.One &&
                    TLarger.CreateTruncating(endInclusive) - TLarger.CreateTruncating(start) + TLarger.One <= maxValue)
                {
                    return new RangeIterator<T>(start, endInclusive + T.One);
                }

                return null;
            }

            static IEnumerable<T> IncrementingIterator(T current, T endInclusive, T step)
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


            static IEnumerable<T> DecrementingIterator(T current, T endInclusive, T step)
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
