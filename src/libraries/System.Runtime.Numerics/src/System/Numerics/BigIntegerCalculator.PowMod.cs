// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        // Executes different exponentiation algorithms, which are
        // based on the classic square-and-multiply method.

        // https://en.wikipedia.org/wiki/Exponentiation_by_squaring

        public static void Pow(nuint value, nuint power, Span<nuint> bits)
        {
            Pow(value != 0 ? new ReadOnlySpan<nuint>(in value) : default, power, bits);
        }

        public static void Pow(ReadOnlySpan<nuint> value, nuint power, Span<nuint> bits)
        {
            Debug.Assert(bits.Length == PowBound(power, value.Length));

            nuint[]? tempFromPool = null;
            Span<nuint> temp = (bits.Length <= StackAllocThreshold ?
                              stackalloc nuint[StackAllocThreshold]
                              : tempFromPool = ArrayPool<nuint>.Shared.Rent(bits.Length)).Slice(0, bits.Length);
            temp.Clear();

            nuint[]? valueCopyFromPool = null;
            Span<nuint> valueCopy = (bits.Length <= StackAllocThreshold ?
                                   stackalloc nuint[StackAllocThreshold]
                                   : valueCopyFromPool = ArrayPool<nuint>.Shared.Rent(bits.Length)).Slice(0, bits.Length);
            value.CopyTo(valueCopy);
            valueCopy.Slice(value.Length).Clear();

            Span<nuint> result = PowCore(valueCopy, value.Length, temp, power, bits);
            result.CopyTo(bits);
            bits.Slice(result.Length).Clear();

            if (tempFromPool != null)
                ArrayPool<nuint>.Shared.Return(tempFromPool);
            if (valueCopyFromPool != null)
                ArrayPool<nuint>.Shared.Return(valueCopyFromPool);
        }

        private static Span<nuint> PowCore(Span<nuint> value, int valueLength, Span<nuint> temp, nuint power, Span<nuint> result)
        {
            Debug.Assert(value.Length >= valueLength);
            Debug.Assert(temp.Length == result.Length);
            Debug.Assert(value.Length == temp.Length);

            result[0] = 1;
            int resultLength = 1;

            // The basic pow algorithm using square-and-multiply.
            while (power != 0)
            {
                if ((power & 1) == 1)
                    resultLength = MultiplySelf(ref result, resultLength, value.Slice(0, valueLength), ref temp);
                if (power != 1)
                    valueLength = SquareSelf(ref value, valueLength, ref temp);
                power >>= 1;
            }

            return result.Slice(0, resultLength);
        }

        private static int MultiplySelf(ref Span<nuint> left, int leftLength, ReadOnlySpan<nuint> right, ref Span<nuint> temp)
        {
            Debug.Assert(leftLength <= left.Length);

            int resultLength = leftLength + right.Length;

            Multiply(left.Slice(0, leftLength), right, temp.Slice(0, resultLength));

            left.Clear();
            //switch buffers
            Span<nuint> t = left;
            left = temp;
            temp = t;
            return ActualLength(left.Slice(0, resultLength));
        }

        private static int SquareSelf(ref Span<nuint> value, int valueLength, ref Span<nuint> temp)
        {
            Debug.Assert(valueLength <= value.Length);
            Debug.Assert(temp.Length >= valueLength + valueLength);

            int resultLength = valueLength + valueLength;

            Square(value.Slice(0, valueLength), temp.Slice(0, resultLength));

            value.Clear();
            //switch buffers
            Span<nuint> t = value;
            value = temp;
            temp = t;
            return ActualLength(value.Slice(0, resultLength));
        }

        public static int PowBound(nuint power, int valueLength)
        {
            // The basic pow algorithm, but instead of squaring
            // and multiplying we just sum up the lengths.

            int resultLength = 1;
            while (power != 0)
            {
                checked
                {
                    if ((power & 1) == 1)
                        resultLength += valueLength;
                    if (power != 1)
                        valueLength += valueLength;
                }
                power >>= 1;
            }

            return resultLength;
        }

        public static nuint Pow(nuint value, nuint power, nuint modulus)
        {
            // The single-limb modulus pow method for a single-limb integer
            // raised by a 32-bit integer...

            return PowCore(value, power, modulus, 1);
        }

        public static nuint Pow(ReadOnlySpan<nuint> value, nuint power, nuint modulus)
        {
            // The single-limb modulus pow method for a big integer
            // raised by a 32-bit integer...

            nuint v = Remainder(value, modulus);
            return PowCore(v, power, modulus, 1);
        }

        public static nuint Pow(nuint value, ReadOnlySpan<nuint> power, nuint modulus)
        {
            // The single-limb modulus pow method for a single-limb integer
            // raised by a big integer...

            return PowCore(value, power, modulus, 1);
        }

        public static nuint Pow(ReadOnlySpan<nuint> value, ReadOnlySpan<nuint> power, nuint modulus)
        {
            // The single-limb modulus pow method for a big integer
            // raised by a big integer...

            nuint v = Remainder(value, modulus);
            return PowCore(v, power, modulus, 1);
        }

        private static nuint PowCore(nuint value, ReadOnlySpan<nuint> power, nuint modulus, nuint result)
        {
            // The single-limb modulus pow algorithm for all but
            // the last power limb using square-and-multiply.

            for (int i = 0; i < power.Length - 1; i++)
            {
                nuint p = power[i];
                for (int j = 0; j < kcbitNuint; j++)
                {
                    if (nint.Size == 8)
                    {
                        if ((p & 1) == 1)
                        {
                            UInt128 prod = (UInt128)(ulong)result * (ulong)value;
                            result = (nuint)(ulong)(prod % (ulong)modulus);
                        }
                        {
                            UInt128 sq = (UInt128)(ulong)value * (ulong)value;
                            value = (nuint)(ulong)(sq % (ulong)modulus);
                        }
                    }
                    else
                    {
                        if ((p & 1) == 1)
                            result = (nuint)(uint)(((ulong)result * value) % modulus);
                        value = (nuint)(uint)(((ulong)value * value) % modulus);
                    }
                    p >>= 1;
                }
            }

            return PowCore(value, power[power.Length - 1], modulus, result);
        }

        private static nuint PowCore(nuint value, nuint power, nuint modulus, nuint result)
        {
            // The single-limb modulus pow algorithm for the last or
            // the only power limb using square-and-multiply.

            while (power != 0)
            {
                if (nint.Size == 8)
                {
                    if ((power & 1) == 1)
                    {
                        UInt128 prod = (UInt128)(ulong)result * (ulong)value;
                        result = (nuint)(ulong)(prod % (ulong)modulus);
                    }
                    if (power != 1)
                    {
                        UInt128 sq = (UInt128)(ulong)value * (ulong)value;
                        value = (nuint)(ulong)(sq % (ulong)modulus);
                    }
                }
                else
                {
                    if ((power & 1) == 1)
                        result = (nuint)(uint)(((ulong)result * value) % modulus);
                    if (power != 1)
                        value = (nuint)(uint)(((ulong)value * value) % modulus);
                }
                power >>= 1;
            }

            return result % modulus;
        }

        public static void Pow(nuint value, nuint power,
                               ReadOnlySpan<nuint> modulus, Span<nuint> bits)
        {
            Pow(value != 0 ? new ReadOnlySpan<nuint>(in value) : default, power, modulus, bits);
        }

        public static void Pow(ReadOnlySpan<nuint> value, nuint power,
                               ReadOnlySpan<nuint> modulus, Span<nuint> bits)
        {
            Debug.Assert(!modulus.IsEmpty);
            Debug.Assert(bits.Length == modulus.Length + modulus.Length);

            // The big modulus pow method for a big integer
            // raised by a 32-bit integer...

            nuint[]? valueCopyFromPool = null;
            int size = Math.Max(value.Length, bits.Length);
            Span<nuint> valueCopy = (size <= StackAllocThreshold ?
                                   stackalloc nuint[StackAllocThreshold]
                                   : valueCopyFromPool = ArrayPool<nuint>.Shared.Rent(size)).Slice(0, size);

            // smallish optimization here:
            // subsequent operations will copy the elements to the beginning of the buffer,
            // no need to clear everything
            valueCopy.Slice(value.Length).Clear();

            if (value.Length > modulus.Length)
            {
                Remainder(value, modulus, valueCopy.Slice(0, value.Length));
            }
            else
            {
                value.CopyTo(valueCopy);
            }

            nuint[]? tempFromPool = null;
            Span<nuint> temp = (bits.Length <= StackAllocThreshold ?
                              stackalloc nuint[StackAllocThreshold]
                              : tempFromPool = ArrayPool<nuint>.Shared.Rent(bits.Length)).Slice(0, bits.Length);
            temp.Clear();

            PowCore(valueCopy, ActualLength(valueCopy), power, modulus, temp, bits);

            if (valueCopyFromPool != null)
                ArrayPool<nuint>.Shared.Return(valueCopyFromPool);
            if (tempFromPool != null)
                ArrayPool<nuint>.Shared.Return(tempFromPool);
        }

        public static void Pow(nuint value, ReadOnlySpan<nuint> power,
                               ReadOnlySpan<nuint> modulus, Span<nuint> bits)
        {
            Pow(value != 0 ? new ReadOnlySpan<nuint>(in value) : default, power, modulus, bits);
        }

        public static void Pow(ReadOnlySpan<nuint> value, ReadOnlySpan<nuint> power,
                               ReadOnlySpan<nuint> modulus, Span<nuint> bits)
        {
            Debug.Assert(!modulus.IsEmpty);
            Debug.Assert(bits.Length == modulus.Length + modulus.Length);

            // The big modulus pow method for a big integer
            // raised by a big integer...

            int size = Math.Max(value.Length, bits.Length);
            nuint[]? valueCopyFromPool = null;
            Span<nuint> valueCopy = (size <= StackAllocThreshold ?
                                   stackalloc nuint[StackAllocThreshold]
                                   : valueCopyFromPool = ArrayPool<nuint>.Shared.Rent(size)).Slice(0, size);

            // smallish optimization here:
            // subsequent operations will copy the elements to the beginning of the buffer,
            // no need to clear everything
            valueCopy.Slice(value.Length).Clear();

            if (value.Length > modulus.Length)
            {
                Remainder(value, modulus, valueCopy.Slice(0, value.Length));
            }
            else
            {
                value.CopyTo(valueCopy);
            }

            nuint[]? tempFromPool = null;
            Span<nuint> temp = (bits.Length <= StackAllocThreshold ?
                              stackalloc nuint[StackAllocThreshold]
                              : tempFromPool = ArrayPool<nuint>.Shared.Rent(bits.Length)).Slice(0, bits.Length);
            temp.Clear();

            PowCore(valueCopy, ActualLength(valueCopy), power, modulus, temp, bits);

            if (valueCopyFromPool != null)
                ArrayPool<nuint>.Shared.Return(valueCopyFromPool);
            if (tempFromPool != null)
                ArrayPool<nuint>.Shared.Return(tempFromPool);
        }

#if DEBUG
        // Mutable for unit testing...
        internal static
#else
        internal const
#endif
        int ReducerThreshold = 32;

        private static void PowCore(Span<nuint> value, int valueLength,
                                     ReadOnlySpan<nuint> power, ReadOnlySpan<nuint> modulus,
                                     Span<nuint> temp, Span<nuint> bits)
        {
            if ((modulus[0] & 1) != 0)
            {
                PowCoreMontgomery(value, valueLength, power, modulus, temp, bits);
                return;
            }

            bits[0] = 1;

            if (modulus.Length < ReducerThreshold)
            {
                Span<nuint> result = PowCore(value, valueLength, power, modulus, bits, 1, temp);
                result.CopyTo(bits);
                bits.Slice(result.Length).Clear();
            }
            else
            {
                PowCoreBarrett(value, valueLength, power, modulus, temp, bits);
            }
        }

        private static void PowCore(Span<nuint> value, int valueLength,
                                     nuint power, ReadOnlySpan<nuint> modulus,
                                     Span<nuint> temp, Span<nuint> bits)
        {
            if ((modulus[0] & 1) != 0)
            {
                PowCoreMontgomery(value, valueLength, new ReadOnlySpan<nuint>(in power), modulus, temp, bits);
                return;
            }

            bits[0] = 1;

            if (modulus.Length < ReducerThreshold)
            {
                Span<nuint> result = PowCore(value, valueLength, power, modulus, bits, 1, temp);
                result.CopyTo(bits);
                bits.Slice(result.Length).Clear();
            }
            else
            {
                PowCoreBarrett(value, valueLength, new ReadOnlySpan<nuint>(in power), modulus, temp, bits);
            }
        }

        private static void PowCoreBarrett(Span<nuint> value, int valueLength,
                                            ReadOnlySpan<nuint> power, ReadOnlySpan<nuint> modulus,
                                            Span<nuint> temp, Span<nuint> bits)
        {
            int size = modulus.Length * 2 + 1;
            nuint[]? rFromPool = null;
            Span<nuint> r = ((uint)size <= StackAllocThreshold ?
                           stackalloc nuint[StackAllocThreshold]
                           : rFromPool = ArrayPool<nuint>.Shared.Rent(size)).Slice(0, size);
            r.Clear();

            size = r.Length - modulus.Length + 1;
            nuint[]? muFromPool = null;
            Span<nuint> mu = ((uint)size <= StackAllocThreshold ?
                            stackalloc nuint[StackAllocThreshold]
                            : muFromPool = ArrayPool<nuint>.Shared.Rent(size)).Slice(0, size);
            mu.Clear();

            size = modulus.Length * 2 + 2;
            nuint[]? q1FromPool = null;
            Span<nuint> q1 = ((uint)size <= StackAllocThreshold ?
                            stackalloc nuint[StackAllocThreshold]
                            : q1FromPool = ArrayPool<nuint>.Shared.Rent(size)).Slice(0, size);
            q1.Clear();

            nuint[]? q2FromPool = null;
            Span<nuint> q2 = ((uint)size <= StackAllocThreshold ?
                            stackalloc nuint[StackAllocThreshold]
                            : q2FromPool = ArrayPool<nuint>.Shared.Rent(size)).Slice(0, size);
            q2.Clear();

            FastReducer reducer = new FastReducer(modulus, r, mu, q1, q2);

            if (rFromPool != null)
                ArrayPool<nuint>.Shared.Return(rFromPool);

            Span<nuint> result = PowCore(value, valueLength, power, reducer, bits, 1, temp);
            result.CopyTo(bits);
            bits.Slice(result.Length).Clear();

            if (muFromPool != null)
                ArrayPool<nuint>.Shared.Return(muFromPool);
            if (q1FromPool != null)
                ArrayPool<nuint>.Shared.Return(q1FromPool);
            if (q2FromPool != null)
                ArrayPool<nuint>.Shared.Return(q2FromPool);
        }

        private static void PowCoreMontgomery(Span<nuint> value, int valueLength,
                                               ReadOnlySpan<nuint> power, ReadOnlySpan<nuint> modulus,
                                               Span<nuint> temp, Span<nuint> bits)
        {
            Debug.Assert((modulus[0] & 1) != 0);
            Debug.Assert(bits.Length >= modulus.Length * 2);

            // Save a reference to the original output buffer. MultiplySelf/SquareSelf
            // swap their ref parameters, so 'bits' may point to a different buffer
            // by the time we're done. We'll copy the result back at the end.
            Span<nuint> originalBits = bits;

            int k = modulus.Length;
            nuint n0inv = ComputeMontgomeryInverse(modulus[0]);

            // Convert value to Montgomery form: montValue = (value << k*wordBits) mod n
            int shiftLen = k + valueLength;
            nuint[]? shiftPool = null;
            Span<nuint> shifted = ((uint)shiftLen <= StackAllocThreshold
                ? stackalloc nuint[StackAllocThreshold]
                : shiftPool = ArrayPool<nuint>.Shared.Rent(shiftLen)).Slice(0, shiftLen);
            shifted.Clear();
            value.Slice(0, valueLength).CopyTo(shifted.Slice(k));

            if (shifted.Length >= modulus.Length)
                DivRem(shifted, modulus, default);

            shifted.Slice(0, k).CopyTo(value);
            value.Slice(k).Clear();
            valueLength = ActualLength(value.Slice(0, k));

            if (shiftPool is not null)
                ArrayPool<nuint>.Shared.Return(shiftPool);

            // Initialize result to Montgomery form of 1: R mod n
            int oneShiftLen = k + 1;
            nuint[]? oneShiftPool = null;
            Span<nuint> oneShifted = ((uint)oneShiftLen <= StackAllocThreshold
                ? stackalloc nuint[StackAllocThreshold]
                : oneShiftPool = ArrayPool<nuint>.Shared.Rent(oneShiftLen)).Slice(0, oneShiftLen);
            oneShifted.Clear();
            oneShifted[k] = 1;

            DivRem(oneShifted, modulus, default);

            bits.Clear();
            oneShifted.Slice(0, k).CopyTo(bits);
            int resultLength = ActualLength(bits.Slice(0, k));

            if (oneShiftPool is not null)
                ArrayPool<nuint>.Shared.Return(oneShiftPool);

            // Square-and-multiply loop processing exponent bits right-to-left
            for (int i = 0; i < power.Length - 1; i++)
            {
                nuint p = power[i];
                for (int j = 0; j < kcbitNuint; j++)
                {
                    if ((p & 1) == 1)
                    {
                        resultLength = MultiplySelf(ref bits, resultLength, value.Slice(0, valueLength), ref temp);
                        resultLength = MontgomeryReduce(bits, modulus, n0inv);
                    }
                    valueLength = SquareSelf(ref value, valueLength, ref temp);
                    valueLength = MontgomeryReduce(value, modulus, n0inv);
                    p >>= 1;
                }
            }

            // Last exponent limb
            {
                nuint p = power[power.Length - 1];
                while (p != 0)
                {
                    if ((p & 1) == 1)
                    {
                        resultLength = MultiplySelf(ref bits, resultLength, value.Slice(0, valueLength), ref temp);
                        resultLength = MontgomeryReduce(bits, modulus, n0inv);
                    }
                    if (p != 1)
                    {
                        valueLength = SquareSelf(ref value, valueLength, ref temp);
                        valueLength = MontgomeryReduce(value, modulus, n0inv);
                    }
                    p >>= 1;
                }
            }

            // Convert result from Montgomery form: REDC(montResult)
            bits.Slice(resultLength).Clear();
            resultLength = MontgomeryReduce(bits, modulus, n0inv);

            // Copy result back to the original output buffer
            bits.Slice(0, resultLength).CopyTo(originalBits);
            originalBits.Slice(resultLength).Clear();
        }

        /// <summary>
        /// Computes -n[0]^{-1} mod 2^wordsize using Newton's method with quadratic convergence.
        /// </summary>
        private static nuint ComputeMontgomeryInverse(nuint n0)
        {
            Debug.Assert((n0 & 1) != 0);

            nuint x = 1;
            int iterations = nint.Size == 8 ? 6 : 5;
            for (int i = 0; i < iterations; i++)
            {
                x *= 2 - n0 * x;
            }

            return unchecked((nuint)0 - x);
        }

        /// <summary>
        /// Montgomery reduction (REDC): computes T * R^{-1} mod n in-place.
        /// T is a 2k-limb value, n is the k-limb odd modulus.
        /// Result is placed in value[0..k-1]; returns actual length.
        /// </summary>
        private static int MontgomeryReduce(Span<nuint> value, ReadOnlySpan<nuint> modulus, nuint n0inv)
        {
            int k = modulus.Length;
            Debug.Assert(value.Length >= 2 * k);

            nuint overflow = 0;

            for (int i = 0; i < k; i++)
            {
                nuint m = unchecked(value[i] * n0inv);
                nuint carry = 0;

                for (int j = 0; j < k; j++)
                {
                    if (nint.Size == 8)
                    {
                        UInt128 p = (UInt128)m * modulus[j] + value[i + j] + carry;
                        value[i + j] = (nuint)(ulong)p;
                        carry = (nuint)(ulong)(p >> 64);
                    }
                    else
                    {
                        ulong p = (ulong)m * modulus[j] + value[i + j] + carry;
                        value[i + j] = (nuint)(uint)p;
                        carry = (nuint)(uint)(p >> 32);
                    }
                }

                for (int idx = i + k; carry != 0 && idx < 2 * k; idx++)
                {
                    nuint sum = value[idx] + carry;
                    carry = (sum < value[idx]) ? (nuint)1 : 0;
                    value[idx] = sum;
                }
                overflow += carry;
            }

            // The mathematical bound guarantees T' < 2*n*R, so T'/R < 2n,
            // meaning overflow past the 2k-limb buffer is at most 1.
            Debug.Assert(overflow <= 1);

            Span<nuint> upper = value.Slice(k, k);

            if (overflow != 0 || Compare(upper, modulus) >= 0)
            {
                SubtractSelf(upper, modulus);
            }

            upper.CopyTo(value);
            value.Slice(k).Clear();

            return ActualLength(value.Slice(0, k));
        }

        private static Span<nuint> PowCore(Span<nuint> value, int valueLength,
                                          ReadOnlySpan<nuint> power, ReadOnlySpan<nuint> modulus,
                                          Span<nuint> result, int resultLength,
                                          Span<nuint> temp)
        {
            // The big modulus pow algorithm for all but
            // the last power limb using square-and-multiply.

            // NOTE: we're using an ordinary remainder here,
            // since the reducer overhead doesn't pay off.

            for (int i = 0; i < power.Length - 1; i++)
            {
                nuint p = power[i];
                for (int j = 0; j < kcbitNuint; j++)
                {
                    if ((p & 1) == 1)
                    {
                        resultLength = MultiplySelf(ref result, resultLength, value.Slice(0, valueLength), ref temp);
                        resultLength = Reduce(result.Slice(0, resultLength), modulus);
                    }
                    valueLength = SquareSelf(ref value, valueLength, ref temp);
                    valueLength = Reduce(value.Slice(0, valueLength), modulus);
                    p >>= 1;
                }
            }

            return PowCore(value, valueLength, power[power.Length - 1], modulus, result, resultLength, temp);
        }

        private static Span<nuint> PowCore(Span<nuint> value, int valueLength,
                                          nuint power, ReadOnlySpan<nuint> modulus,
                                          Span<nuint> result, int resultLength,
                                          Span<nuint> temp)
        {
            // The big modulus pow algorithm for the last or
            // the only power limb using square-and-multiply.

            // NOTE: we're using an ordinary remainder here,
            // since the reducer overhead doesn't pay off.

            while (power != 0)
            {
                if ((power & 1) == 1)
                {
                    resultLength = MultiplySelf(ref result, resultLength, value.Slice(0, valueLength), ref temp);
                    resultLength = Reduce(result.Slice(0, resultLength), modulus);
                }
                if (power != 1)
                {
                    valueLength = SquareSelf(ref value, valueLength, ref temp);
                    valueLength = Reduce(value.Slice(0, valueLength), modulus);
                }
                power >>= 1;
            }

            return result.Slice(0, resultLength);
        }

        private static Span<nuint> PowCore(Span<nuint> value, int valueLength,
                                          ReadOnlySpan<nuint> power, in FastReducer reducer,
                                          Span<nuint> result, int resultLength,
                                          Span<nuint> temp)
        {
            // The big modulus pow algorithm for all but
            // the last power limb using square-and-multiply.

            // NOTE: we're using a special reducer here,
            // since it's additional overhead does pay off.

            for (int i = 0; i < power.Length - 1; i++)
            {
                nuint p = power[i];
                for (int j = 0; j < kcbitNuint; j++)
                {
                    if ((p & 1) == 1)
                    {
                        resultLength = MultiplySelf(ref result, resultLength, value.Slice(0, valueLength), ref temp);
                        resultLength = reducer.Reduce(result.Slice(0, resultLength));
                    }
                    valueLength = SquareSelf(ref value, valueLength, ref temp);
                    valueLength = reducer.Reduce(value.Slice(0, valueLength));
                    p >>= 1;
                }
            }

            return PowCore(value, valueLength, power[power.Length - 1], reducer, result, resultLength, temp);
        }

        private static Span<nuint> PowCore(Span<nuint> value, int valueLength,
                                          nuint power, in FastReducer reducer,
                                          Span<nuint> result, int resultLength,
                                          Span<nuint> temp)
        {
            // The big modulus pow algorithm for the last or
            // the only power limb using square-and-multiply.

            // NOTE: we're using a special reducer here,
            // since it's additional overhead does pay off.

            while (power != 0)
            {
                if ((power & 1) == 1)
                {
                    resultLength = MultiplySelf(ref result, resultLength, value.Slice(0, valueLength), ref temp);
                    resultLength = reducer.Reduce(result.Slice(0, resultLength));
                }
                if (power != 1)
                {
                    valueLength = SquareSelf(ref value, valueLength, ref temp);
                    valueLength = reducer.Reduce(value.Slice(0, valueLength));
                }
                power >>= 1;
            }

            return result.Slice(0, resultLength);
        }
    }
}
