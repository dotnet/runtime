// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
#if DEBUG
        // Mutable for unit testing...
        internal static int MultiplyKaratsubaThreshold = 32;
        internal static int MultiplyToom3Threshold = 256;
#else
        internal const int MultiplyKaratsubaThreshold = 32;
        internal const int MultiplyToom3Threshold = 256;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Square(ReadOnlySpan<uint> value, Span<uint> bits)
        {
            Debug.Assert(bits.Length == value.Length + value.Length);
            Debug.Assert(!bits.ContainsAnyExcept(0u));

            // Executes different algorithms for computing z = a * a
            // based on the actual length of a. If a is "small" enough
            // we stick to the classic "grammar-school" method; for the
            // rest we switch to implementations with less complexity
            // albeit more overhead (which needs to pay off!).

            // NOTE: useful thresholds needs some "empirical" testing,
            // which are smaller in DEBUG mode for testing purpose.

            if (value.Length < MultiplyKaratsubaThreshold)
            {
                Naive(value, bits);
            }
            else if (value.Length < MultiplyToom3Threshold)
            {
                Karatsuba(value, bits);
            }
            else
            {
                Toom3(value, bits);
            }

            static void Toom3(ReadOnlySpan<uint> value, Span<uint> bits)
            {
                Debug.Assert(value.Length >= 3);
                Debug.Assert(bits.Length >= value.Length + value.Length);
                Debug.Assert(bits.Trim(0u).IsEmpty);

                // Based on the Toom-Cook multiplication we split left/right
                // into some smaller values, doing recursive multiplication.
                // Replace m in Wikipedia with left and n in Wikipedia with right.
                // https://en.wikipedia.org/wiki/Toom-Cook_multiplication

                int n = (value.Length + 2) / 3;

                int pLength = n + 1;

                // The threshold for Toom-3 is expected to be greater than
                // StackAllocThreshold, so ArrayPool is always used.

                int pAndQAllLength = pLength * 3;
                uint[] pAndQAllFromPool = ArrayPool<uint>.Shared.Rent(pAndQAllLength);
                Span<uint> pAndQAll = pAndQAllFromPool.AsSpan(0, pAndQAllLength);
                pAndQAll.Clear();

                Toom3Data p = Toom3Data.Build(value, n, pAndQAll.Slice(0, 3 * pLength));

                // Replace r_n in Wikipedia with z_n
                int rLength = pLength + pLength + 1;
                int rAndZAllLength = rLength * 3;
                uint[] rAndZAllFromPool = ArrayPool<uint>.Shared.Rent(rAndZAllLength);
                Span<uint> rAndZAll = rAndZAllFromPool.AsSpan(0, rAndZAllLength);
                rAndZAll.Clear();

                p.Square(n, bits, rAndZAll);

                ArrayPool<uint>.Shared.Return(pAndQAllFromPool);
                ArrayPool<uint>.Shared.Return(rAndZAllFromPool);
            }

            static void Karatsuba(ReadOnlySpan<uint> value, Span<uint> bits)
            {
                Debug.Assert(bits.Length == value.Length + value.Length);
                Debug.Assert(bits.Trim(0u).IsEmpty);

                // The special form of the Toom-Cook multiplication, where we
                // split both operands into two operands, is also known
                // as the Karatsuba algorithm...
                // https://en.wikipedia.org/wiki/Karatsuba_algorithm

                // Say we want to compute z = a * a ...

                // ... we need to determine our new length (just the half)
                int n = value.Length >> 1;
                int n2 = n << 1;

                // ... split value like a = (a_1 << n) + a_0
                ReadOnlySpan<uint> valueLow = value.Slice(0, n);
                ReadOnlySpan<uint> valueHigh = value.Slice(n);

                // ... prepare our result array (to reuse its memory)
                Span<uint> bitsLow = bits.Slice(0, n2);
                Span<uint> bitsHigh = bits.Slice(n2);

                // ... compute z_0 = a_0 * a_0 (squaring again!)
                Square(valueLow, bitsLow);

                // ... compute z_2 = a_1 * a_1 (squaring again!)
                Square(valueHigh, bitsHigh);

                int foldLength = valueHigh.Length + 1;
                uint[]? foldFromPool = null;
                Span<uint> fold = ((uint)foldLength <= StackAllocThreshold
                    ? stackalloc uint[StackAllocThreshold]
                    : foldFromPool = ArrayPool<uint>.Shared.Rent(foldLength)).Slice(0, foldLength);
                fold.Clear();

                int coreLength = foldLength + foldLength;
                uint[]? coreFromPool = null;
                Span<uint> core = ((uint)coreLength <= StackAllocThreshold
                    ? stackalloc uint[StackAllocThreshold]
                    : coreFromPool = ArrayPool<uint>.Shared.Rent(coreLength)).Slice(0, coreLength);
                core.Clear();

                // ... compute z_a = a_1 + a_0 (call it fold...)
                Add(valueHigh, valueLow, fold);

                // ... compute z_1 = z_a * z_a - z_0 - z_2
                Square(fold, core);

                if (foldFromPool != null)
                    ArrayPool<uint>.Shared.Return(foldFromPool);

                SubtractCore(bitsHigh, bitsLow, core);

                // ... and finally merge the result! :-)
                AddSelf(bits.Slice(n), core);

                if (coreFromPool != null)
                    ArrayPool<uint>.Shared.Return(coreFromPool);
            }

            static void Naive(ReadOnlySpan<uint> value, Span<uint> bits)
            {
                Debug.Assert(bits.Length == value.Length + value.Length);
                Debug.Assert(bits.Trim(0u).IsEmpty);

                // Switching to managed references helps eliminating
                // index bounds check...
                ref uint resultPtr = ref MemoryMarshal.GetReference(bits);

                // Squares the bits using the "grammar-school" method.
                // Envisioning the "rhombus" of a pen-and-paper calculation
                // we see that computing z_i+j += a_j * a_i can be optimized
                // since a_j * a_i = a_i * a_j (we're squaring after all!).
                // Thus, we directly get z_i+j += 2 * a_j * a_i + c.

                // ATTENTION: an ordinary multiplication is safe, because
                // z_i+j + a_j * a_i + c <= 2(2^32 - 1) + (2^32 - 1)^2 =
                // = 2^64 - 1 (which perfectly matches with ulong!). But
                // here we would need an UInt65... Hence, we split these
                // operation and do some extra shifts.
                for (int i = 0; i < value.Length; i++)
                {
                    ulong carry = 0UL;
                    uint v = value[i];
                    for (int j = 0; j < i; j++)
                    {
                        ulong digit1 = Unsafe.Add(ref resultPtr, i + j) + carry;
                        ulong digit2 = (ulong)value[j] * v;
                        Unsafe.Add(ref resultPtr, i + j) = unchecked((uint)(digit1 + (digit2 << 1)));
                        carry = (digit2 + (digit1 >> 1)) >> 31;
                    }
                    ulong digits = (ulong)v * v + carry;
                    Unsafe.Add(ref resultPtr, i + i) = unchecked((uint)digits);
                    Unsafe.Add(ref resultPtr, i + i + 1) = (uint)(digits >> 32);
                }
            }
        }

        public static void Multiply(ReadOnlySpan<uint> left, uint right, Span<uint> bits)
        {
            Debug.Assert(bits.Length == left.Length + 1);

            // Executes the multiplication for one big and one 32-bit integer.
            // Since every step holds the already slightly familiar equation
            // a_i * b + c <= 2^32 - 1 + (2^32 - 1)^2 < 2^64 - 1,
            // we are safe regarding to overflows.

            int i = 0;
            ulong carry = 0UL;

            for (; i < left.Length; i++)
            {
                ulong digits = (ulong)left[i] * right + carry;
                bits[i] = unchecked((uint)digits);
                carry = digits >> 32;
            }
            bits[i] = (uint)carry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
        {
            if (left.Length < right.Length)
            {
                ReadOnlySpan<uint> tmp = right;
                right = left;
                left = tmp;
            }

            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(right.Length >= 0);
            Debug.Assert(right.IsEmpty || bits.Length >= left.Length + right.Length);
            Debug.Assert(bits.Trim(0u).IsEmpty);
            Debug.Assert(MultiplyKaratsubaThreshold >= 2);
            Debug.Assert(MultiplyToom3Threshold >= 9);
            Debug.Assert(MultiplyKaratsubaThreshold <= MultiplyToom3Threshold);

            // Executes different algorithms for computing z = a * b
            // based on the actual length of b. If b is "small" enough
            // we stick to the classic "grammar-school" method; for the
            // rest we switch to implementations with less complexity
            // albeit more overhead (which needs to pay off!).

            // NOTE: useful thresholds needs some "empirical" testing,
            // which are smaller in DEBUG mode for testing purpose.

            if (right.Length < MultiplyKaratsubaThreshold)
                Naive(left, right, bits);
            else if ((left.Length + 1) >> 1 is int n && right.Length <= n)
                RightSmall(left, right, bits, n);
            else if (right.Length < MultiplyToom3Threshold)
                Karatsuba(left, right, bits, n);
            else
                Toom3(left, right, bits);

            static void Toom3(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
            {
                Debug.Assert(left.Length >= 3);
                Debug.Assert(left.Length >= right.Length);
                Debug.Assert(bits.Length >= left.Length + right.Length);
                Debug.Assert(bits.Trim(0u).IsEmpty);

                // Based on the Toom-Cook multiplication we split left/right
                // into some smaller values, doing recursive multiplication.
                // Replace m in Wikipedia with left and n in Wikipedia with right.
                // https://en.wikipedia.org/wiki/Toom-Cook_multiplication

                int n = (left.Length + 2) / 3;

                Debug.Assert(right.Length > n);
                if (((uint)right.Length << 1) <= (uint)n)
                {
                    Toom25(left, right, bits, n);
                    return;
                }

                int pLength = n + 1;
                int pAndQAllLength = pLength * 6;

                // The threshold for Toom-3 is expected to be greater than
                // StackAllocThreshold, so ArrayPool is always used.
                uint[] pAndQAllFromPool = ArrayPool<uint>.Shared.Rent(pAndQAllLength);
                Span<uint> pAndQAll = pAndQAllFromPool.AsSpan(0, pAndQAllLength);
                pAndQAll.Clear();

                Toom3Data p = Toom3Data.Build(left, n, pAndQAll.Slice(0, 3 * pLength));
                Toom3Data q = Toom3Data.Build(right, n, pAndQAll.Slice(3 * pLength));

                // Replace r_n in Wikipedia with z_n
                int rLength = pLength + pLength + 1;
                int rAndZAllLength = rLength * 3;
                uint[] rAndZAllFromPool = ArrayPool<uint>.Shared.Rent(rAndZAllLength);
                Span<uint> rAndZAll = rAndZAllFromPool.AsSpan(0, rAndZAllLength);
                rAndZAll.Clear();

                p.MultiplyOther(q, n, bits, rAndZAll);

                ArrayPool<uint>.Shared.Return(pAndQAllFromPool);
                ArrayPool<uint>.Shared.Return(rAndZAllFromPool);
            }

            static void Toom25(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits, int n)
            {
                // Toom 2.5

                Debug.Assert(3 * n - left.Length is 0 or 1 or 2);
                Debug.Assert(right.Length > n);
                Debug.Assert(right.Length <= 2 * n);
                Debug.Assert(bits.Length >= left.Length + right.Length);
                Debug.Assert(bits.Trim(0u).IsEmpty);

                ReadOnlySpan<uint> left0 = left.Slice(0, n).TrimEnd(0u);
                ReadOnlySpan<uint> left1 = left.Slice(n, n).TrimEnd(0u);
                ReadOnlySpan<uint> left2 = left.Slice(n + n);

                ReadOnlySpan<uint> right0 = right.Slice(0, n).TrimEnd(0u);
                ReadOnlySpan<uint> right1 = right.Slice(n);

                Span<uint> z0 = bits.Slice(0, left0.Length + right0.Length);
                Span<uint> z3 = bits.Slice(n * 3);
                Multiply(left0, right0, z0);
                Multiply(left2, right1, z3);

                int pLength = n + 1;
                int pAndQAllLength = pLength * 4;

                // The threshold for Toom-3 is expected to be greater than
                // StackAllocThreshold, so ArrayPool is always used.
                uint[] pAndQAllFromPool = ArrayPool<uint>.Shared.Rent(pAndQAllLength);
                Span<uint> pAndQAll = pAndQAllFromPool.AsSpan(0, pAndQAllLength);
                pAndQAll.Clear();

                Span<uint> p1 = pAndQAll.Slice(0, pLength);
                Span<uint> pm1 = pAndQAll.Slice(pLength, pLength);
                Span<uint> q1 = pAndQAll.Slice(pLength * 2, pLength);
                Span<uint> qm1 = pAndQAll.Slice(pLength * 3, pLength);

                int pm1Sign = 1;
                int qm1Sign = 1;

                if (left0.Length < left2.Length)
                    Add(left2, left0, pm1);
                else
                    Add(left0, left2, pm1);

                pm1.CopyTo(p1);
                AddSelf(p1, left1);
                SubtractSelf(pm1, ref pm1Sign, left1);
                p1 = p1.TrimEnd(0u);
                pm1 = pm1.TrimEnd(0u);

                right0.CopyTo(q1);
                right0.CopyTo(qm1);
                AddSelf(q1, right1);
                SubtractSelf(qm1, ref qm1Sign, right1);
                q1 = q1.TrimEnd(0u);
                qm1 = qm1.TrimEnd(0u);

                int cLength = pLength * 2 + 1;
                int cAllLength = cLength * 3;
                uint[] cAllFromPool = ArrayPool<uint>.Shared.Rent(cAllLength);
                Span<uint> cAll = cAllFromPool.AsSpan(0, cAllLength);
                cAll.Clear();

                Span<uint> z1 = cAll.Slice(0, cLength);
                Span<uint> c1 = z1.Slice(0, p1.Length + q1.Length);

                Span<uint> z2 = cAll.Slice(cLength, cLength);
                Span<uint> cm1 = cAll.Slice(cLength * 2, pm1.Length + qm1.Length);

                Multiply(p1, q1, c1);
                Multiply(pm1, qm1, cm1);

                int cm1Sign = pm1Sign * qm1Sign;
                int z2Sign = c1.IsEmpty ? 0 : 1;
                c1.CopyTo(z2);

                AddSelf(z2, ref z2Sign, cm1, -cm1Sign);
                Debug.Assert(z2Sign >= 0);
                RightShiftOne(z2);
                SubtractSelf(z2, z3.TrimEnd(0u));

                AddSelf(z1, cm1);
                RightShiftOne(z1);
                AddSelf(z1, z0.TrimEnd(0u));

                ArrayPool<uint>.Shared.Return(pAndQAllFromPool);

                AddSelf(bits.Slice(n), z1.TrimEnd(0u));
                AddSelf(bits.Slice(n * 2), z2.TrimEnd(0u));

                ArrayPool<uint>.Shared.Return(cAllFromPool);
            }

            static void Karatsuba(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits, int n)
            {
                //                                            upper           lower
                // A=   |               |               | a1 = a[n..2n] | a0 = a[0..n] |
                // B=   |               |               | b1 = b[n..2n] | b0 = b[0..n] |

                // Result
                // z0=  |               |               |            a0 * b0            |
                // z1=  |               |       a1 * b0 + a0 * b1       |               |
                // z2=  |            a1 * b1            |               |               |

                // z1 = a1 * b0 + a0 * b1
                //    = (a0 + a1) * (b0 + b1) - a0 * b0 - a1 * b1
                //    = (a0 + a1) * (b0 + b1) - z0 - z2

                // The special form of the Toom-Cook multiplication, where we
                // split both operands into two operands, is also known
                // as the Karatsuba algorithm...
                // https://en.wikipedia.org/wiki/Karatsuba_algorithm

                // Say we want to compute z = a * b ...

                Debug.Assert(left.Length >= right.Length);
                Debug.Assert(bits.Length >= left.Length + right.Length);
                Debug.Assert(bits.Trim(0u).IsEmpty);

                // ... we need to determine our new length (just the half)
                Debug.Assert(2 * n - left.Length is 0 or 1);
                Debug.Assert(right.Length > n);

                // ... split left like a = (a_1 << n) + a_0
                ReadOnlySpan<uint> leftLow = left.Slice(0, n);
                ReadOnlySpan<uint> leftHigh = left.Slice(n);

                // ... split right like b = (b_1 << n) + b_0
                ReadOnlySpan<uint> rightLow = right.Slice(0, n);
                ReadOnlySpan<uint> rightHigh = right.Slice(n);

                // ... prepare our result array (to reuse its memory)
                Span<uint> bitsLow = bits.Slice(0, n + n);
                Span<uint> bitsHigh = bits.Slice(n + n);

                Debug.Assert(leftLow.Length >= leftHigh.Length);
                Debug.Assert(rightLow.Length >= rightHigh.Length);
                Debug.Assert(bitsLow.Length >= bitsHigh.Length);

                // ... compute z_0 = a_0 * b_0 (multiply again)
                Multiply(leftLow, rightLow, bitsLow);

                // ... compute z_2 = a_1 * b_1 (multiply again)
                Multiply(leftHigh, rightHigh, bitsHigh);

                int foldLength = n + 1;
                uint[]? leftFoldFromPool = null;
                Span<uint> leftFold = ((uint)foldLength <= StackAllocThreshold
                    ? stackalloc uint[StackAllocThreshold]
                    : leftFoldFromPool = ArrayPool<uint>.Shared.Rent(foldLength)).Slice(0, foldLength);
                leftFold.Clear();

                uint[]? rightFoldFromPool = null;
                Span<uint> rightFold = ((uint)foldLength <= StackAllocThreshold
                    ? stackalloc uint[StackAllocThreshold]
                    : rightFoldFromPool = ArrayPool<uint>.Shared.Rent(foldLength)).Slice(0, foldLength);
                rightFold.Clear();

                // ... compute z_a = a_1 + a_0 (call it fold...)
                Add(leftLow, leftHigh, leftFold);

                // ... compute z_b = b_1 + b_0 (call it fold...)
                Add(rightLow, rightHigh, rightFold);

                int coreLength = foldLength + foldLength;
                uint[]? coreFromPool = null;
                Span<uint> core = ((uint)coreLength <= StackAllocThreshold
                    ? stackalloc uint[StackAllocThreshold]
                    : coreFromPool = ArrayPool<uint>.Shared.Rent(coreLength)).Slice(0, coreLength);
                core.Clear();

                // ... compute z_ab = z_a * z_b
                Multiply(leftFold, rightFold, core);

                if (leftFoldFromPool != null)
                    ArrayPool<uint>.Shared.Return(leftFoldFromPool);

                if (rightFoldFromPool != null)
                    ArrayPool<uint>.Shared.Return(rightFoldFromPool);

                // ... compute z_1 = z_a * z_b - z_0 - z_2 = a_0 * b_1 + a_1 * b_0
                SubtractCore(bitsLow, bitsHigh, core);

                Debug.Assert(ActualLength(core) <= left.Length + 1);

                // ... and finally merge the result! :-)
                AddSelf(bits.Slice(n), core.TrimEnd(0u));

                if (coreFromPool != null)
                    ArrayPool<uint>.Shared.Return(coreFromPool);
            }

            static void RightSmall(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits, int n)
            {
                Debug.Assert(left.Length >= right.Length);
                Debug.Assert(2 * n - left.Length is 0 or 1);
                Debug.Assert(right.Length <= n);
                Debug.Assert(bits.Length >= left.Length + right.Length);
                Debug.Assert(bits.Trim(0u).IsEmpty);

                // ... split left like a = (a_1 << n) + a_0
                ReadOnlySpan<uint> leftLow = left.Slice(0, n);
                ReadOnlySpan<uint> leftHigh = left.Slice(n);
                Debug.Assert(leftLow.Length >= leftHigh.Length);

                // ... prepare our result array (to reuse its memory)
                Span<uint> bitsLow = bits.Slice(0, n + right.Length);
                Span<uint> bitsHigh = bits.Slice(n);

                // ... compute low
                Multiply(leftLow, right, bitsLow);

                int carryLength = right.Length;
                uint[]? carryFromPool = null;
                Span<uint> carry = ((uint)carryLength <= StackAllocThreshold
                    ? stackalloc uint[StackAllocThreshold]
                    : carryFromPool = ArrayPool<uint>.Shared.Rent(carryLength)).Slice(0, carryLength);

                Span<uint> carryOrig = bitsHigh.Slice(0, right.Length);
                carryOrig.CopyTo(carry);
                carryOrig.Clear();

                // ... compute high
                Multiply(leftHigh, right, bitsHigh.Slice(0, leftHigh.Length + right.Length));

                AddSelf(bitsHigh, carry);

                if (carryFromPool != null)
                    ArrayPool<uint>.Shared.Return(carryFromPool);
            }

            static void Naive(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
            {
                Debug.Assert(right.Length < MultiplyKaratsubaThreshold);

                // Switching to managed references helps eliminating
                // index bounds check...
                ref uint resultPtr = ref MemoryMarshal.GetReference(bits);

                // Multiplies the bits using the "grammar-school" method.
                // Envisioning the "rhombus" of a pen-and-paper calculation
                // should help getting the idea of these two loops...
                // The inner multiplication operations are safe, because
                // z_i+j + a_j * b_i + c <= 2(2^32 - 1) + (2^32 - 1)^2 =
                // = 2^64 - 1 (which perfectly matches with ulong!).

                for (int i = 0; i < right.Length; i++)
                {
                    uint rv = right[i];
                    ulong carry = 0UL;
                    for (int j = 0; j < left.Length; j++)
                    {
                        ref uint elementPtr = ref Unsafe.Add(ref resultPtr, i + j);
                        ulong digits = elementPtr + carry + (ulong)left[j] * rv;
                        elementPtr = unchecked((uint)digits);
                        carry = digits >> 32;
                    }
                    Unsafe.Add(ref resultPtr, i + left.Length) = (uint)carry;
                }
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly ref struct Toom3Data(
            ReadOnlySpan<uint> c0,
            ReadOnlySpan<uint> cInf,
            ReadOnlySpan<uint> c1,
            ReadOnlySpan<uint> cm1,
            int cm1Sign,
            ReadOnlySpan<uint> cm2,
            int cm2Sign)
        {
            private readonly ReadOnlySpan<uint> c0 = c0;
            private readonly ReadOnlySpan<uint> c1 = c1;
            private readonly ReadOnlySpan<uint> cInf = cInf;
            private readonly ReadOnlySpan<uint> cm1 = cm1;
            private readonly ReadOnlySpan<uint> cm2 = cm2;
            private readonly int cm1Sign = cm1Sign;
            private readonly int cm2Sign = cm2Sign;

            public static Toom3Data Build(ReadOnlySpan<uint> value, int n, Span<uint> buffer)
            {
                Debug.Assert(!buffer.ContainsAnyExcept(0u));
                Debug.Assert(buffer.Length == 3 * (n + 1));
                Debug.Assert(value.Length > n);
                Debug.Assert(value[^1] != 0);

                int pLength = n + 1;

                ReadOnlySpan<uint> v0, v1, v2;

                v0 = value.Slice(0, n).TrimEnd(0u);
                if (value.Length <= n + n)
                {
                    v1 = value.Slice(n);
                    v2 = default;
                }
                else
                {
                    v1 = value.Slice(n, n).TrimEnd(0u);
                    v2 = value.Slice(n + n);
                }

                Span<uint> p1 = buffer.Slice(0, pLength);
                Span<uint> pm1 = buffer.Slice(pLength, pLength);

                // Calculate p(1) = p_0 + m_1, p(-1) = p_0 - m_1
                int pm1Sign = 1;
                {
                    v0.CopyTo(p1);
                    AddSelf(p1, v2);

                    p1.CopyTo(pm1);
                    AddSelf(p1, v1);

                    SubtractSelf(pm1, ref pm1Sign, v1);

                    pm1 = pm1Sign != 0 ? pm1.TrimEnd(0u) : default;
                }

                // Calculate p(-2) = (p(-1) + m_2)*2 - m_0
                int pm2Sign = pm1Sign;
                Span<uint> pm2 = buffer.Slice(pLength + pLength, pLength);
                {
                    Debug.Assert(!pm2.ContainsAnyExcept(0u));
                    Debug.Assert(pm1.IsEmpty || pm1[^1] != 0);
                    Debug.Assert(v0.IsEmpty || v0[^1] != 0);
                    Debug.Assert(v2.IsEmpty || v2[^1] != 0);

                    pm1.CopyTo(pm2);

                    // Calclate p(-1) + m_2
                    AddSelf(pm2, ref pm2Sign, v2);

                    // Calculate p(-2) = (p(-1) + m_2)*2
                    {
                        Debug.Assert(pm2[^1] < 0x8000_0000);
                        LeftShiftOne(pm2);
                    }

                    Debug.Assert(pm2[^1] != uint.MaxValue);

                    // Calculate p(-2) = (p(-1) + m_2)*2 - m_0
                    SubtractSelf(pm2, ref pm2Sign, v0);

                    pm2 = pm2.TrimEnd(0u);
                }

                return new Toom3Data(
                    c0: v0,
                    c1: p1.TrimEnd(0u),
                    cInf: v2,
                    cm1: pm1.TrimEnd(0u),
                    cm2: pm2,
                    cm1Sign: pm1Sign,
                    cm2Sign: pm2Sign
                );
            }

            public void MultiplyOther(in Toom3Data right, int n, Span<uint> bits, Span<uint> buffer)
            {
                Debug.Assert(!buffer.ContainsAnyExcept(0u));
                Debug.Assert(cInf.Length >= right.cInf.Length);

                int rLength = n + n + 3;

                ReadOnlySpan<uint> p0 = c0;
                ReadOnlySpan<uint> q0 = right.c0;

                ReadOnlySpan<uint> p1 = c1;
                ReadOnlySpan<uint> q1 = right.c1;

                ReadOnlySpan<uint> pm1 = cm1;
                ReadOnlySpan<uint> qm1 = right.cm1;

                ReadOnlySpan<uint> pm2 = cm2;
                ReadOnlySpan<uint> qm2 = right.cm2;

                ReadOnlySpan<uint> pInf = cInf;
                ReadOnlySpan<uint> qInf = right.cInf;


                Span<uint> r0 = bits.Slice(0, p0.Length + q0.Length);
                Span<uint> rInf =
                    !qInf.IsEmpty
                    ? bits.Slice(4 * n, pInf.Length + qInf.Length)
                    : default;

                Span<uint> r1 = buffer.Slice(0, p1.Length + q1.Length);
                Span<uint> rm1 = buffer.Slice(rLength, pm1.Length + qm1.Length);
                Span<uint> rm2 = buffer.Slice(rLength * 2, pm2.Length + qm2.Length);

                Multiply(p0, q0, r0);
                Multiply(p1, q1, r1);
                Multiply(pm1, qm1, rm1);
                Multiply(pm2, qm2, rm2);
                Multiply(pInf, qInf, rInf);

                Toom3CalcResult(
                    n,
                    r0: r0.TrimEnd(0u),
                    rInf: rInf.TrimEnd(0u),
                    z1: buffer.Slice(0, rLength),
                    r1Length: ActualLength(r1),
                    z2: buffer.Slice(rLength, rLength),
                    z2Sign: cm1Sign * right.cm1Sign,
                    rm1Length: ActualLength(rm1),
                    z3: buffer.Slice(rLength * 2, rLength),
                    z3Sign: cm2Sign * right.cm2Sign,
                    bits
                );
            }
            public void Square(int n, Span<uint> bits, Span<uint> buffer)
            {
                Debug.Assert(!buffer.ContainsAnyExcept(0u));
                Debug.Assert(!cInf.IsEmpty);

                int rLength = n + n + 3;

                ReadOnlySpan<uint> p0 = c0;
                ReadOnlySpan<uint> p1 = c1;
                ReadOnlySpan<uint> pm1 = cm1;
                ReadOnlySpan<uint> pm2 = cm2;
                ReadOnlySpan<uint> pInf = cInf;

                Span<uint> r0 = bits.Slice(0, p0.Length << 1);
                Span<uint> rInf = bits.Slice(4 * n, pInf.Length << 1);

                Span<uint> r1 = buffer.Slice(0, p1.Length << 1);
                Span<uint> rm1 = buffer.Slice(rLength, pm1.Length << 1);
                Span<uint> rm2 = buffer.Slice(rLength * 2, pm2.Length << 1);

                BigIntegerCalculator.Square(p0, r0);
                BigIntegerCalculator.Square(p1, r1);
                BigIntegerCalculator.Square(pm1, rm1);
                BigIntegerCalculator.Square(pm2, rm2);
                BigIntegerCalculator.Square(pInf, rInf);

                Toom3CalcResult(
                    n,
                    r0: r0.TrimEnd(0u),
                    rInf: rInf.TrimEnd(0u),
                    z1: buffer.Slice(0, rLength),
                    r1Length: ActualLength(r1),
                    z2: buffer.Slice(rLength, rLength),
                    z2Sign: cm1Sign & 1,
                    rm1Length: ActualLength(rm1),
                    z3: buffer.Slice(rLength * 2, rLength),
                    z3Sign: cm2Sign & 1,
                    bits
                );
            }

            private static void Toom3CalcResult(
                int n,
                ReadOnlySpan<uint> r0,
                ReadOnlySpan<uint> rInf,
                Span<uint> z1,
                int r1Length,
                Span<uint> z2,
                int z2Sign,
                int rm1Length,
                Span<uint> z3,
                int z3Sign,
                Span<uint> bits)
            {
                int z1Sign = Math.Sign(r1Length);

                // Calc z_3 = (r(-2) - r(1))/3
                {
                    // Calc r(-2) - r(1)
                    SubtractSelf(z3, ref z3Sign, z1.Slice(0, r1Length));

                    // Calc (r(-2) - r(1))/3
                    DivideThreeSelf(z3.TrimEnd(0u));
                }

                // Calc z_1 = (r(1) - r(-1))/2
                {
                    AddSelf(z1, ref z1Sign, z2.Slice(0, rm1Length), -z2Sign);
                    Debug.Assert(z1.IsEmpty || (z1[0] & 1) == 0);

                    RightShiftOne(z1);
                }

                // Calc z_2 = r(-1) - r(0)
                SubtractSelf(z2, ref z2Sign, r0);

                // Calc z_3 = (z_2 - z_3)/2 + 2r(Inf)
                {
                    // Calc z_2 - z_3
                    AddSelf(z3, ref z3Sign, z2, -z2Sign);
                    z3Sign = -z3Sign;

                    Debug.Assert(z3.IsEmpty || (z3[0] & 1) == 0);


                    // Calc (z_2 - z_3)/2
                    RightShiftOne(z3);

                    // Calc (z_2 - z_3)/2 + 2r(Inf)
                    AddSelf(z3, ref z3Sign, rInf);
                    AddSelf(z3, ref z3Sign, rInf);
                }

                // Calc z_2 = z_2 + z_1 - r(Inf)
                {
                    AddSelf(z2, ref z2Sign, z1.TrimEnd(0u));
                    SubtractSelf(z2, ref z2Sign, rInf);
                }

                // Calc z_1 = z_1 - z_3
                SubtractSelf(z1, ref z1Sign, z3.TrimEnd(0u));

                Debug.Assert(z1Sign >= 0);
                Debug.Assert(z2Sign >= 0);
                Debug.Assert(z3Sign >= 0);

                AddSelf(bits.Slice(n), z1.TrimEnd(0u));
                AddSelf(bits.Slice(2 * n), z2.TrimEnd(0u));

                if (bits.Length >= 3 * n)
                    AddSelf(bits.Slice(3 * n), z3.TrimEnd(0u));
            }
        }

        private static void DivideThreeSelf(Span<uint> bits)
        {
            const uint oneThird = (uint)((1ul << 32) / 3);
            const uint twoThirds = (uint)((2ul << 32) / 3);

            uint carry = 0;
            for (int i = bits.Length - 1; i >= 0; i--)
            {
                (uint quo, uint rem) = Math.DivRem(bits[i], 3);

                Debug.Assert(carry < 3);

                if (carry == 0)
                {
                    bits[i] = quo;
                    carry = rem;
                }
                else if (carry == 1)
                {
                    if (++rem == 3)
                    {
                        rem = 0;
                        ++quo;
                    }

                    bits[i] = oneThird + quo;
                    carry = rem;
                }
                else
                {
                    if (--rem < 3)
                        ++quo;
                    else
                        rem = 2;

                    bits[i] = twoThirds + quo;
                    carry = rem;
                }
            }

            Debug.Assert(carry == 0);
        }
        private static void SubtractCore(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> core)
        {
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(core.Length >= left.Length);

            // Executes a special subtraction algorithm for the multiplication,
            // which needs to subtract two different values from a core value,
            // while core is always bigger than the sum of these values.

            // NOTE: we could do an ordinary subtraction of course, but we spare
            // one "run", if we do this computation within a single one...

            int i = 0;
            long carry = 0L;

            // Switching to managed references helps eliminating
            // index bounds check...
            ref uint leftPtr = ref MemoryMarshal.GetReference(left);
            ref uint corePtr = ref MemoryMarshal.GetReference(core);

            for (; i < right.Length; i++)
            {
                long digit = (Unsafe.Add(ref corePtr, i) + carry) - Unsafe.Add(ref leftPtr, i) - right[i];
                Unsafe.Add(ref corePtr, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }

            for (; i < left.Length; i++)
            {
                long digit = (Unsafe.Add(ref corePtr, i) + carry) - left[i];
                Unsafe.Add(ref corePtr, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }

            for (; carry != 0 && i < core.Length; i++)
            {
                long digit = core[i] + carry;
                core[i] = (uint)digit;
                carry = digit >> 32;
            }
        }


        private static void AddSelf(Span<uint> left, ref int leftSign, ReadOnlySpan<uint> right, int rightSign)
        {
            Debug.Assert(left.Length >= right.Length);

            if (rightSign == 0)
                return;
            else if (rightSign > 0)
                AddSelf(left, ref leftSign, right);
            else
                SubtractSelf(left, ref leftSign, right);
        }

        private static void AddSelf(Span<uint> left, ref int leftSign, ReadOnlySpan<uint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            right = right.TrimEnd(0u);

            if (leftSign == 0)
            {
                Debug.Assert(!left.ContainsAnyExcept(0u));

                if (!right.IsEmpty)
                {
                    leftSign = 1;
                    right.CopyTo(left);
                }
            }
            else if (leftSign > 0)
            {
                AddSelf(left, right);
            }
            else
            {
                leftSign = CompareActual(right, left);
                if (leftSign == 0)
                {
                    left.Clear();
                }
                else if (leftSign < 0)
                {
                    SubtractSelf(left, right);
                }
                else
                {
                    left = left.Slice(0, right.Length);
                    SubtractSelf(left, right);
                    NumericsHelpers.DangerousMakeTwosComplement(left);
                }
            }
        }
        private static void SubtractSelf(Span<uint> left, ref int leftSign, ReadOnlySpan<uint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            right = right.TrimEnd(0u);

            if (leftSign == 0)
            {
                if (!right.IsEmpty)
                {
                    leftSign = -1;
                    right.CopyTo(left);
                }
            }
            else if (leftSign < 0)
            {
                AddSelf(left, right);
            }
            else
            {
                leftSign = CompareActual(left, right);
                if (leftSign == 0)
                {
                    left.Clear();
                }
                else if (leftSign > 0)
                {
                    SubtractSelf(left, right);
                }
                else
                {
                    left = left.Slice(0, right.Length);
                    SubtractSelf(left, right);
                    NumericsHelpers.DangerousMakeTwosComplement(left);
                }
            }
        }

        private static void LeftShiftOne(Span<uint> bits)
        {
            uint carry = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                uint value = carry | bits[i] << 1;
                carry = bits[i] >> 31;
                bits[i] = value;
            }
        }
        private static void RightShiftOne(Span<uint> bits)
        {
            uint carry = 0;
            for (int i = bits.Length - 1; i >= 0; i--)
            {
                uint value = carry | bits[i] >> 1;
                carry = bits[i] << 31;
                bits[i] = value;
            }
        }
    }
}
