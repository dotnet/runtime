// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        // Executes different exponentiation algorithms, which are
        // based on the classic square-and-multiply method.

        // https://en.wikipedia.org/wiki/Exponentiation_by_squaring

        public static void Pow(uint value, uint power, Span<uint> bits)
        {
            Pow(value != 0U ? MemoryMarshal.CreateReadOnlySpan(ref value, 1) : default, power, bits);
        }

        public static void Pow(ReadOnlySpan<uint> value, uint power, Span<uint> bits)
        {
            Debug.Assert(bits.Length == PowBound(power, value.Length));

            // The basic pow method for a big integer.
            uint[]? tempFromPool = null;
            Span<uint> temp = bits.Length <= AllocationThreshold ?
                              stackalloc uint[bits.Length]
                              : (tempFromPool = ArrayPool<uint>.Shared.Rent(bits.Length)).AsSpan(0, bits.Length);
            temp.Clear();

            uint[]? valueCopyFromPool = null;
            Span<uint> valueCopy = bits.Length <= AllocationThreshold ?
                                   stackalloc uint[bits.Length]
                                   : (valueCopyFromPool = ArrayPool<uint>.Shared.Rent(bits.Length)).AsSpan(0, bits.Length);
            valueCopy.Clear();
            value.CopyTo(valueCopy);

            PowCore(valueCopy, value.Length, temp, power, bits);

            if (tempFromPool != null)
                ArrayPool<uint>.Shared.Return(tempFromPool);
            if (valueCopyFromPool != null)
                ArrayPool<uint>.Shared.Return(valueCopyFromPool);
        }

        private static void PowCore(Span<uint> value, int valueLength, Span<uint> temp, uint power, Span<uint> bits)
        {
            Debug.Assert(value.Length >= valueLength);
            Debug.Assert(temp.Length == bits.Length);
            Debug.Assert(value.Length == temp.Length);

            //save the result buffer to temp variable because bits buffer will change during buffer switch later
            Span<uint> result = bits;

            bits[0] = 1;
            int bitsLength = 1;

            // The basic pow algorithm using square-and-multiply.
            while (power != 0)
            {
                if ((power & 1) == 1)
                    bitsLength = MultiplySelf(ref bits, bitsLength, value.Slice(0, valueLength), ref temp);
                if (power != 1)
                    valueLength = SquareSelf(ref value, valueLength, ref temp);
                power = power >> 1;
            }

            bits.CopyTo(result);
        }

        private static int MultiplySelf(ref Span<uint> left, int leftLength, ReadOnlySpan<uint> right, ref Span<uint> temp)
        {
            Debug.Assert(leftLength <= left.Length);

            int resultLength = leftLength + right.Length;

            if (leftLength >= right.Length)
            {
                Multiply(left.Slice(0, leftLength), right, temp.Slice(0, resultLength));
            }
            else
            {
                Multiply(right, left.Slice(0, leftLength), temp.Slice(0, resultLength));
            }

            left.Clear();
            //switch buffers
            Span<uint> t = left;
            left = temp;
            temp = t;
            return ActualLength(left.Slice(0, resultLength));
        }

        private static int SquareSelf(ref Span<uint> value, int valueLength, ref Span<uint> temp)
        {
            Debug.Assert(valueLength <= value.Length);
            Debug.Assert(temp.Length >= valueLength + valueLength);

            int resultLength = valueLength + valueLength;

            Square(value.Slice(0, valueLength), temp.Slice(0, resultLength));

            value.Clear();
            //switch buffers
            Span<uint> t = value;
            value = temp;
            temp = t;
            return ActualLength(value.Slice(0, resultLength));
        }

        public static int PowBound(uint power, int valueLength)
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
                power = power >> 1;
            }

            return resultLength;
        }

        public static uint Pow(uint value, uint power, uint modulus)
        {
            // The 32-bit modulus pow method for a 32-bit integer
            // raised by a 32-bit integer...

            return PowCore(power, modulus, value, 1);
        }

        public static uint Pow(ReadOnlySpan<uint> value, uint power, uint modulus)
        {
            // The 32-bit modulus pow method for a big integer
            // raised by a 32-bit integer...

            uint v = Remainder(value, modulus);
            return PowCore(power, modulus, v, 1);
        }

        public static uint Pow(uint value, ReadOnlySpan<uint> power, uint modulus)
        {
            // The 32-bit modulus pow method for a 32-bit integer
            // raised by a big integer...

            return PowCore(power, modulus, value, 1);
        }

        public static uint Pow(ReadOnlySpan<uint> value, ReadOnlySpan<uint> power, uint modulus)
        {
            // The 32-bit modulus pow method for a big integer
            // raised by a big integer...

            uint v = Remainder(value, modulus);
            return PowCore(power, modulus, v, 1);
        }

        private static uint PowCore(ReadOnlySpan<uint> power, uint modulus,
                                    ulong value, ulong result)
        {
            // The 32-bit modulus pow algorithm for all but
            // the last power limb using square-and-multiply.

            for (int i = 0; i < power.Length - 1; i++)
            {
                uint p = power[i];
                for (int j = 0; j < 32; j++)
                {
                    if ((p & 1) == 1)
                        result = (result * value) % modulus;
                    value = (value * value) % modulus;
                    p = p >> 1;
                }
            }

            return PowCore(power[power.Length - 1], modulus, value, result);
        }

        private static uint PowCore(uint power, uint modulus,
                                    ulong value, ulong result)
        {
            // The 32-bit modulus pow algorithm for the last or
            // the only power limb using square-and-multiply.

            while (power != 0)
            {
                if ((power & 1) == 1)
                    result = (result * value) % modulus;
                if (power != 1)
                    value = (value * value) % modulus;
                power = power >> 1;
            }

            return (uint)(result % modulus);
        }

        public static uint[] Pow(uint value, uint power, uint[] modulus)
        {
            Debug.Assert(modulus != null);

            // The big modulus pow method for a 32-bit integer
            // raised by a 32-bit integer...

            int size = modulus.Length + modulus.Length;
            BitsBuffer v = new BitsBuffer(size, value);
            return PowCore(power, modulus, ref v);
        }

        public static uint[] Pow(uint[] value, uint power, uint[] modulus)
        {
            Debug.Assert(value != null);
            Debug.Assert(modulus != null);

            // The big modulus pow method for a big integer
            // raised by a 32-bit integer...

            if (value.Length > modulus.Length)
                value = Remainder(value, modulus);

            int size = modulus.Length + modulus.Length;
            BitsBuffer v = new BitsBuffer(size, value);
            return PowCore(power, modulus, ref v);
        }

        public static uint[] Pow(uint value, uint[] power, uint[] modulus)
        {
            Debug.Assert(power != null);
            Debug.Assert(modulus != null);

            // The big modulus pow method for a 32-bit integer
            // raised by a big integer...

            int size = modulus.Length + modulus.Length;
            BitsBuffer v = new BitsBuffer(size, value);
            return PowCore(power, modulus, ref v);
        }

        public static uint[] Pow(uint[] value, uint[] power, uint[] modulus)
        {
            Debug.Assert(value != null);
            Debug.Assert(power != null);
            Debug.Assert(modulus != null);

            // The big modulus pow method for a big integer
            // raised by a big integer...

            if (value.Length > modulus.Length)
                value = Remainder(value, modulus);

            int size = modulus.Length + modulus.Length;
            BitsBuffer v = new BitsBuffer(size, value);
            return PowCore(power, modulus, ref v);
        }

        // Mutable for unit testing...
        private static int ReducerThreshold = 32;

        private static uint[] PowCore(uint[] power, uint[] modulus,
                                      ref BitsBuffer value)
        {
            // Executes the big pow algorithm.

            int size = value.GetSize();

            BitsBuffer temp = new BitsBuffer(size, 0);
            BitsBuffer result = new BitsBuffer(size, 1);

            if (modulus.Length < ReducerThreshold)
            {
                PowCore(power, modulus, ref value, ref result, ref temp);
            }
            else
            {
                size = modulus.Length * 2 + 1;
                uint[]? rFromPool = null;
                Span<uint> r = size <= AllocationThreshold ?
                               stackalloc uint[size]
                               : (rFromPool = ArrayPool<uint>.Shared.Rent(size)).AsSpan(0, size);
                r.Clear();

                size = r.Length - modulus.Length + 1;
                uint[]? muFromPool = null;
                Span<uint> mu = size <= AllocationThreshold ?
                                stackalloc uint[size]
                                : (muFromPool = ArrayPool<uint>.Shared.Rent(size)).AsSpan(0, size);
                mu.Clear();

                size = modulus.Length * 2 + 2;
                uint[]? q1FromPool = null;
                Span<uint> q1 = size <= AllocationThreshold ?
                                stackalloc uint[size]
                                : (q1FromPool = ArrayPool<uint>.Shared.Rent(size)).AsSpan(0, size);
                q1.Clear();

                uint[]? q2FromPool = null;
                Span<uint> q2 = size <= AllocationThreshold ?
                                stackalloc uint[size]
                                : (q2FromPool = ArrayPool<uint>.Shared.Rent(size)).AsSpan(0, size);
                q2.Clear();

                FastReducer reducer = new FastReducer(modulus, r, mu, q1, q2);

                if (rFromPool != null)
                    ArrayPool<uint>.Shared.Return(rFromPool);

                PowCore(power, reducer, ref value, ref result, ref temp);

                if (muFromPool != null)
                    ArrayPool<uint>.Shared.Return(muFromPool);
                if (q1FromPool != null)
                    ArrayPool<uint>.Shared.Return(q1FromPool);
                if (q2FromPool != null)
                    ArrayPool<uint>.Shared.Return(q2FromPool);
            }

            return result.GetBits();
        }

        private static uint[] PowCore(uint power, uint[] modulus,
                                      ref BitsBuffer value)
        {
            // Executes the big pow algorithm.

            int size = value.GetSize();

            BitsBuffer temp = new BitsBuffer(size, 0);
            BitsBuffer result = new BitsBuffer(size, 1);

            if (modulus.Length < ReducerThreshold)
            {
                PowCore(power, modulus, ref value, ref result, ref temp);
            }
            else
            {
                size = modulus.Length * 2 + 1;
                uint[]? rFromPool = null;
                Span<uint> r = size <= AllocationThreshold ?
                               stackalloc uint[size]
                               : (rFromPool = ArrayPool<uint>.Shared.Rent(size)).AsSpan(0, size);
                r.Clear();

                size = r.Length - modulus.Length + 1;
                uint[]? muFromPool = null;
                Span<uint> mu = size <= AllocationThreshold ?
                                stackalloc uint[size]
                                : (muFromPool = ArrayPool<uint>.Shared.Rent(size)).AsSpan(0, size);
                mu.Clear();

                size = modulus.Length * 2 + 2;
                uint[]? q1FromPool = null;
                Span<uint> q1 = size <= AllocationThreshold ?
                                stackalloc uint[size]
                                : (q1FromPool = ArrayPool<uint>.Shared.Rent(size)).AsSpan(0, size);
                q1.Clear();

                uint[]? q2FromPool = null;
                Span<uint> q2 = size <= AllocationThreshold ?
                                stackalloc uint[size]
                                : (q2FromPool = ArrayPool<uint>.Shared.Rent(size)).AsSpan(0, size);
                q2.Clear();

                FastReducer reducer = new FastReducer(modulus, r, mu, q1, q2);

                if (rFromPool != null)
                    ArrayPool<uint>.Shared.Return(rFromPool);

                PowCore(power, reducer, ref value, ref result, ref temp);

                if (muFromPool != null)
                    ArrayPool<uint>.Shared.Return(muFromPool);
                if (q1FromPool != null)
                    ArrayPool<uint>.Shared.Return(q1FromPool);
                if (q2FromPool != null)
                    ArrayPool<uint>.Shared.Return(q2FromPool);
            }

            return result.GetBits();
        }

        private static void PowCore(uint[] power, uint[] modulus,
                                    ref BitsBuffer value, ref BitsBuffer result,
                                    ref BitsBuffer temp)
        {
            // The big modulus pow algorithm for all but
            // the last power limb using square-and-multiply.

            // NOTE: we're using an ordinary remainder here,
            // since the reducer overhead doesn't pay off.

            for (int i = 0; i < power.Length - 1; i++)
            {
                uint p = power[i];
                for (int j = 0; j < 32; j++)
                {
                    if ((p & 1) == 1)
                    {
                        result.MultiplySelf(ref value, ref temp);
                        result.Reduce(modulus);
                    }
                    value.SquareSelf(ref temp);
                    value.Reduce(modulus);
                    p = p >> 1;
                }
            }

            PowCore(power[power.Length - 1], modulus, ref value, ref result,
                ref temp);
        }

        private static void PowCore(uint power, uint[] modulus,
                                    ref BitsBuffer value, ref BitsBuffer result,
                                    ref BitsBuffer temp)
        {
            // The big modulus pow algorithm for the last or
            // the only power limb using square-and-multiply.

            // NOTE: we're using an ordinary remainder here,
            // since the reducer overhead doesn't pay off.

            while (power != 0)
            {
                if ((power & 1) == 1)
                {
                    result.MultiplySelf(ref value, ref temp);
                    result.Reduce(modulus);
                }
                if (power != 1)
                {
                    value.SquareSelf(ref temp);
                    value.Reduce(modulus);
                }
                power = power >> 1;
            }
        }

        private static void PowCore(uint[] power, in FastReducer reducer,
                                    ref BitsBuffer value, ref BitsBuffer result,
                                    ref BitsBuffer temp)
        {
            // The big modulus pow algorithm for all but
            // the last power limb using square-and-multiply.

            // NOTE: we're using a special reducer here,
            // since it's additional overhead does pay off.

            for (int i = 0; i < power.Length - 1; i++)
            {
                uint p = power[i];
                for (int j = 0; j < 32; j++)
                {
                    if ((p & 1) == 1)
                    {
                        result.MultiplySelf(ref value, ref temp);
                        result.Reduce(reducer);
                    }
                    value.SquareSelf(ref temp);
                    value.Reduce(reducer);
                    p = p >> 1;
                }
            }

            PowCore(power[power.Length - 1], reducer, ref value, ref result,
                ref temp);
        }

        private static void PowCore(uint power, in FastReducer reducer,
                                    ref BitsBuffer value, ref BitsBuffer result,
                                    ref BitsBuffer temp)
        {
            // The big modulus pow algorithm for the last or
            // the only power limb using square-and-multiply.

            // NOTE: we're using a special reducer here,
            // since it's additional overhead does pay off.

            while (power != 0)
            {
                if ((power & 1) == 1)
                {
                    result.MultiplySelf(ref value, ref temp);
                    result.Reduce(reducer);
                }
                if (power != 1)
                {
                    value.SquareSelf(ref temp);
                    value.Reduce(reducer);
                }
                power = power >> 1;
            }
        }
    }
}
