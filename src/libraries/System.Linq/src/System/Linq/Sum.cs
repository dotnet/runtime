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
        public static int Sum(this IEnumerable<int> source) => Sum<int, int>(source);

        public static long Sum(this IEnumerable<long> source) => Sum<long, long>(source);

        public static float Sum(this IEnumerable<float> source) => (float)Sum<float, double>(source);

        public static double Sum(this IEnumerable<double> source) => Sum<double, double>(source);

        public static decimal Sum(this IEnumerable<decimal> source) => Sum<decimal, decimal>(source);

        private static TResult Sum<TSource, TResult>(this IEnumerable<TSource> source)
            where TSource : struct, INumber<TSource>
            where TResult : struct, INumber<TResult>
        {
            if (source.TryGetSpan(out ReadOnlySpan<TSource> span))
            {
                return Sum<TSource, TResult>(span);
            }

            TResult sum = TResult.Zero;
            foreach (TSource value in source)
            {
                checked { sum += TResult.CreateChecked(value); }
            }

            return sum;
        }

        private static TResult Sum<T, TResult>(ReadOnlySpan<T> span)
            where T : struct, INumber<T>
            where TResult : struct, INumber<TResult>
        {
            if (typeof(T) == typeof(TResult)
                && Vector<T>.IsSupported
                && Vector.IsHardwareAccelerated
                && Vector<T>.Count > 2
                && span.Length >= Vector<T>.Count * 4)
            {
                // For cases where the vector may only contain two elements vectorization doesn't add any benefit
                // due to the expense of overflow checking. This means that architectures where Vector<T> is 128 bit,
                // such as ARM or Intel without AVX, will only vectorize spans of ints and not longs.

                if (typeof(T) == typeof(long))
                {
                    return (TResult) (object) SumSignedIntegersVectorized(MemoryMarshal.Cast<T, long>(span));
                }
                if (typeof(T) == typeof(int))
                {
                    return (TResult) (object) SumSignedIntegersVectorized(MemoryMarshal.Cast<T, int>(span));
                }
            }

            TResult sum = TResult.Zero;
            foreach (T value in span)
            {
                checked { sum += TResult.CreateChecked(value); }
            }

            return sum;
        }

        private static T SumSignedIntegersVectorized<T>(ReadOnlySpan<T> span)
            where T : struct, IBinaryInteger<T>
        {
            Debug.Assert(span.Length >= Vector<T>.Count * 4);

            ref T ptr = ref MemoryMarshal.GetReference(span);
            int length = span.Length;

            // Overflow testing for vectors is based on setting the sign bit of the overflowTracking
            // vector for an element if the following are all true:
            //   - The two elements being summed have the same sign bit. If one element is positive
            //     and the other is negative then an overflow is not possible.
            //   - The sign bit of the sum is not the same as the sign bit of the previous accumulator.
            //     This indicates that the new sum wrapped around to the opposite sign.
            //
            // By bitwise or-ing the overflowTracking vector for each step we can save cycles by testing
            // the sign bits less often. If any iteration has the sign bit set in any element it indicates
            // there was an overflow.
            //
            // Note: The overflow checking in this algorithm is only correct for signed integers.
            // If support is ever added for unsigned integers then the overflow check should be:
            //   overflowTracking |= (accumulator & data) | Vector.AndNot(accumulator | data, sum);

            Vector<T> accumulator = Vector<T>.Zero;

            // Build a test vector with only the sign bit set in each element. JIT will fold this into a constant.
            Vector<T> overflowTestVector = new(T.RotateRight(T.MultiplicativeIdentity, 1));

            // Unroll the loop to sum 4 vectors per iteration
            do
            {
                // Switch accumulators with each step to avoid an additional move operation
                Vector<T> data = Vector.LoadUnsafe(ref ptr);
                Vector<T> accumulator2 = accumulator + data;
                Vector<T> overflowTracking = (accumulator2 ^ accumulator) & (accumulator2 ^ data);

                data = Vector.LoadUnsafe(ref ptr, (nuint)Vector<T>.Count);
                accumulator = accumulator2 + data;
                overflowTracking |= (accumulator ^ accumulator2) & (accumulator ^ data);

                data = Vector.LoadUnsafe(ref ptr, (nuint)Vector<T>.Count * 2);
                accumulator2 = accumulator + data;
                overflowTracking |= (accumulator2 ^ accumulator) & (accumulator2 ^ data);

                data = Vector.LoadUnsafe(ref ptr, (nuint)Vector<T>.Count * 3);
                accumulator = accumulator2 + data;
                overflowTracking |= (accumulator ^ accumulator2) & (accumulator ^ data);

                if ((overflowTracking & overflowTestVector) != Vector<T>.Zero)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                ptr = ref Unsafe.Add(ref ptr, Vector<T>.Count * 4);
                length -= Vector<T>.Count * 4;
            } while (length >= Vector<T>.Count * 4);

            // Process remaining vectors, if any, without unrolling
            if (length >= Vector<T>.Count)
            {
                Vector<T> overflowTracking = Vector<T>.Zero;

                do
                {
                    Vector<T> data = Vector.LoadUnsafe(ref ptr);
                    Vector<T> accumulator2 = accumulator + data;
                    overflowTracking |= (accumulator2 ^ accumulator) & (accumulator2 ^ data);
                    accumulator = accumulator2;

                    ptr = ref Unsafe.Add(ref ptr, Vector<T>.Count);
                    length -= Vector<T>.Count;
                } while (length >= Vector<T>.Count);

                if ((overflowTracking & overflowTestVector) != Vector<T>.Zero)
                {
                    ThrowHelper.ThrowOverflowException();
                }
            }

            // Add the elements in the vector horizontally.
            // Vector.Sum doesn't perform overflow checking, instead add elements individually.
            T result = T.Zero;
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                checked { result += accumulator[i]; }
            }

            // Add any remaining elements
            for (int i = 0; i < length; i++)
            {
                checked { result += Unsafe.Add(ref ptr, i); }
            }

            return result;
        }

        public static int? Sum(this IEnumerable<int?> source) => Sum<int, int>(source);

        public static long? Sum(this IEnumerable<long?> source) => Sum<long, long>(source);

        public static float? Sum(this IEnumerable<float?> source) => Sum<float, double>(source);

        public static double? Sum(this IEnumerable<double?> source) => Sum<double, double>(source);

        public static decimal? Sum(this IEnumerable<decimal?> source) => Sum<decimal, decimal>(source);

        private static TSource? Sum<TSource, TAccumulator>(this IEnumerable<TSource?> source)
            where TSource : struct, INumber<TSource>
            where TAccumulator : struct, INumber<TAccumulator>
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            TAccumulator sum = TAccumulator.Zero;
            foreach (TSource? value in source)
            {
                if (value is not null)
                {
                    checked { sum += TAccumulator.CreateChecked(value.GetValueOrDefault()); }
                }
            }

            return TSource.CreateTruncating(sum);
        }


        public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector) => Sum<TSource, int, int>(source, selector);

        public static long Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector) => Sum<TSource, long, long>(source, selector);

        public static float Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector) => Sum<TSource, float, double>(source, selector);

        public static double Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector) => Sum<TSource, double, double>(source, selector);

        public static decimal Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector) => Sum<TSource, decimal, decimal>(source, selector);

        private static TResult Sum<TSource, TResult, TAccumulator>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
            where TResult : struct, INumber<TResult>
            where TAccumulator : struct, INumber<TAccumulator>
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            TAccumulator sum = TAccumulator.Zero;
            foreach (TSource value in source)
            {
                checked { sum += TAccumulator.CreateChecked(selector(value)); }
            }

            return TResult.CreateTruncating(sum);
        }


        public static int? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector) => Sum<TSource, int, int>(source, selector);

        public static long? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector) => Sum<TSource, long, long>(source, selector);

        public static float? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector) => Sum<TSource, float, double>(source, selector);

        public static double? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector) => Sum<TSource, double, double>(source, selector);

        public static decimal? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector) => Sum<TSource, decimal, decimal>(source, selector);

        private static TResult? Sum<TSource, TResult, TAccumulator>(this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
            where TResult : struct, INumber<TResult>
            where TAccumulator : struct, INumber<TAccumulator>
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            TAccumulator sum = TAccumulator.Zero;
            foreach (TSource item in source)
            {
                if (selector(item) is TResult selectedValue)
                {
                    checked { sum += TAccumulator.CreateChecked(selectedValue); }
                }
            }

            return TResult.CreateTruncating(sum);
        }
    }
}
