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

        public static void Pow(uint value, uint power, Span<uint> bits)
        {
            Pow(value != 0U ? new ReadOnlySpan<uint>(in value) : default, power, bits);
        }

        public static void Pow(ReadOnlySpan<uint> value, uint power, Span<uint> bits)
        {
            Debug.Assert(bits.Length == PowBound(power, value.Length));

            uint[]? tempFromPool = null;
            Span<uint> temp = (bits.Length <= StackAllocThreshold ?
                              stackalloc uint[StackAllocThreshold]
                              : tempFromPool = ArrayPool<uint>.Shared.Rent(bits.Length)).Slice(0, bits.Length);
            temp.Clear();

            uint[]? valueCopyFromPool = null;
            Span<uint> valueCopy = (bits.Length <= StackAllocThreshold ?
                                   stackalloc uint[StackAllocThreshold]
                                   : valueCopyFromPool = ArrayPool<uint>.Shared.Rent(bits.Length)).Slice(0, bits.Length);
            value.CopyTo(valueCopy);
            valueCopy.Slice(value.Length).Clear();

            PowCore(valueCopy, value.Length, temp, power, bits).CopyTo(bits);

            if (tempFromPool != null)
                ArrayPool<uint>.Shared.Return(tempFromPool);
            if (valueCopyFromPool != null)
                ArrayPool<uint>.Shared.Return(valueCopyFromPool);
        }

        private static Span<uint> PowCore(Span<uint> value, int valueLength, Span<uint> temp, uint power, Span<uint> result)
        {
            Debug.Assert(value.Length >= valueLength);
            Debug.Assert(temp.Length == result.Length);
            Debug.Assert(value.Length == temp.Length);

            result[0] = 1;
            int bitsLength = 1;

            // The basic pow algorithm using square-and-multiply.
            while (power != 0)
            {
                if ((power & 1) == 1)
                    bitsLength = MultiplySelf(ref result, bitsLength, value.Slice(0, valueLength), ref temp);
                if (power != 1)
                    valueLength = SquareSelf(ref value, valueLength, ref temp);
                power >>= 1;
            }

            return result;
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
                power >>= 1;
            }

            return resultLength;
        }

        public static uint Pow(uint value, uint power, uint modulus)
        {
            // The 32-bit modulus pow method for a 32-bit integer
            // raised by a 32-bit integer...

            return PowCore(value, power, modulus, 1);
        }

        public static uint Pow(ReadOnlySpan<uint> value, uint power, uint modulus)
        {
            // The 32-bit modulus pow method for a big integer
            // raised by a 32-bit integer...

            uint v = Remainder(value, modulus);
            return PowCore(v, power, modulus, 1);
        }

        public static uint Pow(uint value, ReadOnlySpan<uint> power, uint modulus)
        {
            // The 32-bit modulus pow method for a 32-bit integer
            // raised by a big integer...

            return PowCore(value, power, modulus, 1);
        }

        public static uint Pow(ReadOnlySpan<uint> value, ReadOnlySpan<uint> power, uint modulus)
        {
            // The 32-bit modulus pow method for a big integer
            // raised by a big integer...

            uint v = Remainder(value, modulus);
            return PowCore(v, power, modulus, 1);
        }

        private static uint PowCore(ulong value, ReadOnlySpan<uint> power, uint modulus, ulong result)
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
                    p >>= 1;
                }
            }

            return PowCore(value, power[power.Length - 1], modulus, result);
        }

        private static uint PowCore(ulong value, uint power, uint modulus, ulong result)
        {
            // The 32-bit modulus pow algorithm for the last or
            // the only power limb using square-and-multiply.

            while (power != 0)
            {
                if ((power & 1) == 1)
                    result = (result * value) % modulus;
                if (power != 1)
                    value = (value * value) % modulus;
                power >>= 1;
            }

            return (uint)(result % modulus);
        }

        public static void Pow(uint value, uint power,
                               ReadOnlySpan<uint> modulus, Span<uint> bits)
        {
            Pow(value != 0U ? new ReadOnlySpan<uint>(in value) : default, power, modulus, bits);
        }

        public static void Pow(ReadOnlySpan<uint> value, uint power,
                               ReadOnlySpan<uint> modulus, Span<uint> bits)
        {
            Debug.Assert(!modulus.IsEmpty);
            Debug.Assert(bits.Length == modulus.Length + modulus.Length);

            // The big modulus pow method for a big integer
            // raised by a 32-bit integer...

            uint[]? valueCopyFromPool = null;
            int size = Math.Max(value.Length, bits.Length);
            Span<uint> valueCopy = (size <= StackAllocThreshold ?
                                   stackalloc uint[StackAllocThreshold]
                                   : valueCopyFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
            valueCopy.Clear();

            if (value.Length > modulus.Length)
            {
                Remainder(value, modulus, valueCopy);
            }
            else
            {
                value.CopyTo(valueCopy);
            }

            uint[]? tempFromPool = null;
            Span<uint> temp = (bits.Length <= StackAllocThreshold ?
                              stackalloc uint[StackAllocThreshold]
                              : tempFromPool = ArrayPool<uint>.Shared.Rent(bits.Length)).Slice(0, bits.Length);
            temp.Clear();

            PowCore(valueCopy, ActualLength(valueCopy), power, modulus, temp, bits);

            if (valueCopyFromPool != null)
                ArrayPool<uint>.Shared.Return(valueCopyFromPool);
            if (tempFromPool != null)
                ArrayPool<uint>.Shared.Return(tempFromPool);
        }

        public static void Pow(uint value, ReadOnlySpan<uint> power,
                               ReadOnlySpan<uint> modulus, Span<uint> bits)
        {
            Pow(value != 0U ? new ReadOnlySpan<uint>(in value) : default, power, modulus, bits);
        }

        public static void Pow(ReadOnlySpan<uint> value, ReadOnlySpan<uint> power,
                               ReadOnlySpan<uint> modulus, Span<uint> bits)
        {
            Debug.Assert(!modulus.IsEmpty);
            Debug.Assert(bits.Length == modulus.Length + modulus.Length);

            // The big modulus pow method for a big integer
            // raised by a big integer...

            int size = Math.Max(value.Length, bits.Length);
            uint[]? valueCopyFromPool = null;
            Span<uint> valueCopy = (size <= StackAllocThreshold ?
                                   stackalloc uint[StackAllocThreshold]
                                   : valueCopyFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
            valueCopy.Clear();

            if (value.Length > modulus.Length)
            {
                Remainder(value, modulus, valueCopy);
            }
            else
            {
                value.CopyTo(valueCopy);
            }

            uint[]? tempFromPool = null;
            Span<uint> temp = (bits.Length <= StackAllocThreshold ?
                              stackalloc uint[StackAllocThreshold]
                              : tempFromPool = ArrayPool<uint>.Shared.Rent(bits.Length)).Slice(0, bits.Length);
            temp.Clear();

            PowCore(valueCopy, ActualLength(valueCopy), power, modulus, temp, bits);

            if (valueCopyFromPool != null)
                ArrayPool<uint>.Shared.Return(valueCopyFromPool);
            if (tempFromPool != null)
                ArrayPool<uint>.Shared.Return(tempFromPool);
        }

#if DEBUG
        // Mutable for unit testing...
        private static
#else
        private const
#endif
        int ReducerThreshold = 32;

        private static void PowCore(Span<uint> value, int valueLength,
                                    ReadOnlySpan<uint> power, ReadOnlySpan<uint> modulus,
                                    Span<uint> temp, Span<uint> bits)
        {
            // Executes the big pow algorithm.

            bits[0] = 1;

            if (modulus.Length < ReducerThreshold)
            {
                PowCore(value, valueLength, power, modulus, bits, 1, temp).CopyTo(bits);
            }
            else
            {
                int size = modulus.Length * 2 + 1;
                uint[]? rFromPool = null;
                Span<uint> r = ((uint)size <= StackAllocThreshold ?
                               stackalloc uint[StackAllocThreshold]
                               : rFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                r.Clear();

                size = r.Length - modulus.Length + 1;
                uint[]? muFromPool = null;
                Span<uint> mu = ((uint)size <= StackAllocThreshold ?
                                stackalloc uint[StackAllocThreshold]
                                : muFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                mu.Clear();

                size = modulus.Length * 2 + 2;
                uint[]? q1FromPool = null;
                Span<uint> q1 = ((uint)size <= StackAllocThreshold ?
                                stackalloc uint[StackAllocThreshold]
                                : q1FromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                q1.Clear();

                uint[]? q2FromPool = null;
                Span<uint> q2 = ((uint)size <= StackAllocThreshold ?
                                stackalloc uint[StackAllocThreshold]
                                : q2FromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                q2.Clear();

                FastReducer reducer = new FastReducer(modulus, r, mu, q1, q2);

                if (rFromPool != null)
                    ArrayPool<uint>.Shared.Return(rFromPool);

                PowCore(value, valueLength, power, reducer, bits, 1, temp).CopyTo(bits);

                if (muFromPool != null)
                    ArrayPool<uint>.Shared.Return(muFromPool);
                if (q1FromPool != null)
                    ArrayPool<uint>.Shared.Return(q1FromPool);
                if (q2FromPool != null)
                    ArrayPool<uint>.Shared.Return(q2FromPool);
            }
        }

        private static void PowCore(Span<uint> value, int valueLength,
                                    uint power, ReadOnlySpan<uint> modulus,
                                    Span<uint> temp, Span<uint> bits)
        {
            // Executes the big pow algorithm.
            bits[0] = 1;

            if (modulus.Length < ReducerThreshold)
            {
                PowCore(value, valueLength, power, modulus, bits, 1, temp).CopyTo(bits);
            }
            else
            {
                int size = modulus.Length * 2 + 1;
                uint[]? rFromPool = null;
                Span<uint> r = ((uint)size <= StackAllocThreshold ?
                               stackalloc uint[StackAllocThreshold]
                               : rFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                r.Clear();

                size = r.Length - modulus.Length + 1;
                uint[]? muFromPool = null;
                Span<uint> mu = ((uint)size <= StackAllocThreshold ?
                                stackalloc uint[StackAllocThreshold]
                                : muFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                mu.Clear();

                size = modulus.Length * 2 + 2;
                uint[]? q1FromPool = null;
                Span<uint> q1 = ((uint)size <= StackAllocThreshold ?
                                stackalloc uint[StackAllocThreshold]
                                : q1FromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                q1.Clear();

                uint[]? q2FromPool = null;
                Span<uint> q2 = ((uint)size <= StackAllocThreshold ?
                                stackalloc uint[StackAllocThreshold]
                                : q2FromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                q2.Clear();

                FastReducer reducer = new FastReducer(modulus, r, mu, q1, q2);

                if (rFromPool != null)
                    ArrayPool<uint>.Shared.Return(rFromPool);

                PowCore(value, valueLength, power, reducer, bits, 1, temp).CopyTo(bits);

                if (muFromPool != null)
                    ArrayPool<uint>.Shared.Return(muFromPool);
                if (q1FromPool != null)
                    ArrayPool<uint>.Shared.Return(q1FromPool);
                if (q2FromPool != null)
                    ArrayPool<uint>.Shared.Return(q2FromPool);
            }
        }

        private static Span<uint> PowCore(Span<uint> value, int valueLength,
                                          ReadOnlySpan<uint> power, ReadOnlySpan<uint> modulus,
                                          Span<uint> result, int resultLength,
                                          Span<uint> temp)
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

        private static Span<uint> PowCore(Span<uint> value, int valueLength,
                                          uint power, ReadOnlySpan<uint> modulus,
                                          Span<uint> result, int resultLength,
                                          Span<uint> temp)
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

        private static Span<uint> PowCore(Span<uint> value, int valueLength,
                                          ReadOnlySpan<uint> power, in FastReducer reducer,
                                          Span<uint> result, int resultLength,
                                          Span<uint> temp)
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

        private static Span<uint> PowCore(Span<uint> value, int valueLength,
                                          uint power, in FastReducer reducer,
                                          Span<uint> result, int resultLength,
                                          Span<uint> temp)
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

            return result;
        }
    }
}
