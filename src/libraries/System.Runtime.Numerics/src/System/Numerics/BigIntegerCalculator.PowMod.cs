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
            // Executes the big pow algorithm.

            bits[0] = 1;

            if (modulus.Length < ReducerThreshold)
            {
                Span<nuint> result = PowCore(value, valueLength, power, modulus, bits, 1, temp);
                result.CopyTo(bits);
                bits.Slice(result.Length).Clear();
            }
            else
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
        }

        private static void PowCore(Span<nuint> value, int valueLength,
                                    nuint power, ReadOnlySpan<nuint> modulus,
                                    Span<nuint> temp, Span<nuint> bits)
        {
            // Executes the big pow algorithm.
            bits[0] = 1;

            if (modulus.Length < ReducerThreshold)
            {
                Span<nuint> result = PowCore(value, valueLength, power, modulus, bits, 1, temp);
                result.CopyTo(bits);
                bits.Slice(result.Length).Clear();
            }
            else
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
