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
        public static byte Xor(this IEnumerable<byte> source) => Xor<byte, byte>(source);

        public static char Xor(this IEnumerable<char> source) => Xor<char, char>(source);

        public static short Xor(this IEnumerable<short> source) => Xor<short, short>(source);

        public static int Xor(this IEnumerable<int> source) => Xor<int, int>(source);

        public static long Xor(this IEnumerable<long> source) => Xor<long, long>(source);

        private static TResult Xor<TSource, TResult>(this IEnumerable<TSource> source)
            where TSource : struct, IBinaryNumber<TSource>
            where TResult : struct, IBinaryNumber<TResult>
        {
            if (source.TryGetSpan(out ReadOnlySpan<TSource> span))
            {
                return Xor<TSource, TResult>(span);
            }

            TResult Xor = TResult.Zero;
            foreach (TSource value in source)
            {
                checked { Xor ^= TResult.CreateChecked(value); }
            }

            return Xor;
        }

        private static TResult Xor<T, TResult>(ReadOnlySpan<T> span)
            where T : struct, IBinaryNumber<T>
            where TResult : struct, IBinaryNumber<TResult>
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
                    return (TResult) (object) XorSignedIntegersVectorized(MemoryMarshal.Cast<T, long>(span));
                }
                if (typeof(T) == typeof(int))
                {
                    return (TResult) (object) XorSignedIntegersVectorized(MemoryMarshal.Cast<T, int>(span));
                }
            }

            TResult Xor = TResult.Zero;
            foreach (T value in span)
            {
                checked { Xor ^= TResult.CreateChecked(value); }
            }

            return Xor;
        }

        private static T XorSignedIntegersVectorized<T>(ReadOnlySpan<T> span)
            where T : struct, IBinaryInteger<T>, ISignedNumber<T>, IMinMaxValue<T>
        {
            Debug.Assert(span.Length >= Vector<T>.Count * 4);
            Debug.Assert(Vector<T>.Count > 2);
            Debug.Assert(Vector.IsHardwareAccelerated);

            ref T ptr = ref MemoryMarshal.GetReference(span);
            nuint length = (nuint)span.Length;

            // Overflow testing for vectors is based on setting the sign bit of the overflowTracking
            // vector for an element if the following are all true:
            //   - The two elements being Xormed have the same sign bit. If one element is positive
            //     and the other is negative then an overflow is not possible.
            //   - The sign bit of the Xor is not the same as the sign bit of the previous accumulator.
            //     This indicates that the new Xor wrapped around to the opposite sign.
            //
            // This is done by:
            //   overflowTracking |= (result ^ input1) & (result ^ input2);
            //
            // The general premise here is that we're doing signof(result) ^ signof(input1). This will produce
            // a sign-bit of 1 if they differ and 0 if they are the same. We do the same with
            // signof(result) ^ signof(input2), then combine both results together with a logical &.
            //
            // Thus, if we had a sign swap compared to both inputs, then signof(input1) == signof(input2) and
            // we must have overflowed.
            //
            // By bitwise or-ing the overflowTracking vector for each step we can save cycles by testing
            // the sign bits less often. If any iteration has the sign bit set in any element it indicates
            // there was an overflow.
            //
            // Note: The overflow checking in this algorithm is only correct for signed integers.
            // If support is ever added for unsigned integers then the overflow check should be:
            //   overflowTracking |= (input1 & input2) | Vector.AndNot(input1 | input2, result);

            Vector<T> accumulator = Vector<T>.Zero;

            // Build a test vector with only the sign bit set in each element.
            Vector<T> overflowTestVector = new(T.MinValue);

            // Unroll the loop to Xor 4 vectors per iteration. This reduces range check
            // and overflow check frequency, allows us to eliminate move operations swapping
            // accumulators, and may have pipelining benefits.
            nuint index = 0;
            nuint limit = length - (nuint)Vector<T>.Count * 4;
            do
            {
                // Switch accumulators with each step to avoid an additional move operation
                Vector<T> data = Vector.LoadUnsafe(ref ptr, index);
                Vector<T> accumulator2 = accumulator + data;
                Vector<T> overflowTracking = (accumulator2 ^ accumulator) & (accumulator2 ^ data);

                data = Vector.LoadUnsafe(ref ptr, index + (nuint)Vector<T>.Count);
                accumulator = accumulator2 + data;
                overflowTracking |= (accumulator ^ accumulator2) & (accumulator ^ data);

                data = Vector.LoadUnsafe(ref ptr, index + (nuint)Vector<T>.Count * 2);
                accumulator2 = accumulator + data;
                overflowTracking |= (accumulator2 ^ accumulator) & (accumulator2 ^ data);

                data = Vector.LoadUnsafe(ref ptr, index + (nuint)Vector<T>.Count * 3);
                accumulator = accumulator2 + data;
                overflowTracking |= (accumulator ^ accumulator2) & (accumulator ^ data);

                if ((overflowTracking & overflowTestVector) != Vector<T>.Zero)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                index += (nuint)Vector<T>.Count * 4;
            } while (index < limit);

            // Process remaining vectors, if any, without unrolling
            limit = length - (nuint)Vector<T>.Count;
            if (index < limit)
            {
                Vector<T> overflowTracking = Vector<T>.Zero;

                do
                {
                    Vector<T> data = Vector.LoadUnsafe(ref ptr, index);
                    Vector<T> accumulator2 = accumulator + data;
                    overflowTracking |= (accumulator2 ^ accumulator) & (accumulator2 ^ data);
                    accumulator = accumulator2;

                    index += (nuint)Vector<T>.Count;
                } while (index < limit);

                if ((overflowTracking & overflowTestVector) != Vector<T>.Zero)
                {
                    ThrowHelper.ThrowOverflowException();
                }
            }

            // Add the elements in the vector horizontally.
            // Vector.Xor doesn't perform overflow checking, instead add elements individually.
            T result = T.Zero;
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                checked { result ^= accumulator[i]; }
            }

            // Add any remaining elements
            while (index < length)
            {
                checked { result += Unsafe.Add(ref ptr, index); }

                index++;
            }

            return result;
        }

        public static byte? Xor(this IEnumerable<byte?> source) => Xor<byte, byte>(source);

        public static char? Xor(this IEnumerable<char?> source) => Xor<char, char>(source);

        public static short? Xor(this IEnumerable<short?> source) => Xor<short, short>(source);

        public static int? Xor(this IEnumerable<int?> source) => Xor<int, int>(source);

        public static long? Xor(this IEnumerable<long?> source) => Xor<long, long>(source);

        private static TSource? Xor<TSource, TAccumulator>(this IEnumerable<TSource?> source)
            where TSource : struct, IBinaryNumber<TSource>
            where TAccumulator : struct, IBinaryNumber<TAccumulator>
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            TAccumulator Xor = TAccumulator.Zero;
            foreach (TSource? value in source)
            {
                if (value is not null)
                {
                    checked { Xor ^= TAccumulator.CreateChecked(value.GetValueOrDefault()); }
                }
            }

            return TSource.CreateTruncating(Xor);
        }

        public static int Xor<TSource>(this IEnumerable<TSource> source, Func<TSource, byte> selector) => Xor<TSource, byte, byte>(source, selector);

        public static int Xor<TSource>(this IEnumerable<TSource> source, Func<TSource, char> selector) => Xor<TSource, char, char>(source, selector);

        public static int Xor<TSource>(this IEnumerable<TSource> source, Func<TSource, short> selector) => Xor<TSource, short, short>(source, selector);

        public static int Xor<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector) => Xor<TSource, int, int>(source, selector);

        public static long Xor<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector) => Xor<TSource, long, long>(source, selector);

        private static TResult Xor<TSource, TResult, TAccumulator>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
            where TResult : struct, IBinaryNumber<TResult>
            where TAccumulator : struct, IBinaryNumber<TAccumulator>
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            TAccumulator Xor = TAccumulator.Zero;
            foreach (TSource value in source)
            {
                checked { Xor ^= TAccumulator.CreateChecked(selector(value)); }
            }

            return TResult.CreateTruncating(Xor);
        }

        public static byte? Xor<TSource>(this IEnumerable<TSource> source, Func<TSource, byte?> selector) => Xor<TSource, byte, byte>(source, selector);

        public static char? Xor<TSource>(this IEnumerable<TSource> source, Func<TSource, char?> selector) => Xor<TSource, char, char>(source, selector);

        public static short? Xor<TSource>(this IEnumerable<TSource> source, Func<TSource, short?> selector) => Xor<TSource, short, short>(source, selector);

        public static int? Xor<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector) => Xor<TSource, int, int>(source, selector);

        public static long? Xor<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector) => Xor<TSource, long, long>(source, selector);


        private static TResult? Xor<TSource, TResult, TAccumulator>(this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
            where TResult : struct, IBinaryNumber<TResult>
            where TAccumulator : struct, IBinaryNumber<TAccumulator>
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            TAccumulator Xor = TAccumulator.Zero;
            foreach (TSource item in source)
            {
                if (selector(item) is TResult selectedValue)
                {
                    checked { Xor ^= TAccumulator.CreateChecked(selectedValue); }
                }
            }

            return TResult.CreateTruncating(Xor);
        }
    }
}
