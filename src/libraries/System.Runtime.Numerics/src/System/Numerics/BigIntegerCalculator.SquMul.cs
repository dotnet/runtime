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
        internal static int SquareKaratsubaThreshold = 48;
        internal static int SquareToom3Threshold = 384;
#else
        internal const int MultiplyKaratsubaThreshold = 32;
        internal const int MultiplyToom3Threshold = 256;
        internal const int SquareKaratsubaThreshold = 48;
        internal const int SquareToom3Threshold = 384;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Square(ReadOnlySpan<nuint> value, Span<nuint> bits)
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

            if (value.Length < SquareKaratsubaThreshold)
            {
                Naive(value, bits);
            }
            else if (value.Length < SquareToom3Threshold)
            {
                Karatsuba(value, bits);
            }
            else
            {
                Toom3(value, bits);
            }

            static void Toom3(ReadOnlySpan<nuint> value, Span<nuint> bits)
            {
                Debug.Assert(value.Length >= 3);
                Debug.Assert(bits.Length >= value.Length + value.Length);
                Debug.Assert(bits.Trim((nuint)0).IsEmpty);

                // Based on the Toom-Cook multiplication we split left/right
                // into some smaller values, doing recursive multiplication.
                // Replace m in Wikipedia with left and n in Wikipedia with right.
                // https://en.wikipedia.org/wiki/Toom-Cook_multiplication

                int n = (value.Length + 2) / 3;

                int pLength = n + 1;

                // The threshold for Toom-3 is expected to be greater than
                // StackAllocThreshold, so ArrayPool is always used.

                int pAndQAllLength = pLength * 3;
                nuint[] pAndQAllFromPool = ArrayPool<nuint>.Shared.Rent(pAndQAllLength);
                Span<nuint> pAndQAll = pAndQAllFromPool.AsSpan(0, pAndQAllLength);
                pAndQAll.Clear();

                Toom3Data p = Toom3Data.Build(value, n, pAndQAll.Slice(0, 3 * pLength));

                // Replace r_n in Wikipedia with z_n
                int rLength = pLength + pLength + 1;
                int rAndZAllLength = rLength * 3;
                nuint[] rAndZAllFromPool = ArrayPool<nuint>.Shared.Rent(rAndZAllLength);
                Span<nuint> rAndZAll = rAndZAllFromPool.AsSpan(0, rAndZAllLength);
                rAndZAll.Clear();

                p.Square(n, bits, rAndZAll);

                ArrayPool<nuint>.Shared.Return(pAndQAllFromPool);
                ArrayPool<nuint>.Shared.Return(rAndZAllFromPool);
            }

            static void Karatsuba(ReadOnlySpan<nuint> value, Span<nuint> bits)
            {
                Debug.Assert(bits.Length == value.Length + value.Length);
                Debug.Assert(bits.Trim((nuint)0).IsEmpty);

                // The special form of the Toom-Cook multiplication, where we
                // split both operands into two operands, is also known
                // as the Karatsuba algorithm...
                // https://en.wikipedia.org/wiki/Karatsuba_algorithm

                // Say we want to compute z = a * a ...

                // ... we need to determine our new length (just the half)
                int n = value.Length >> 1;
                int n2 = n << 1;

                // ... split value like a = (a_1 << n) + a_0
                ReadOnlySpan<nuint> valueLow = value.Slice(0, n);
                ReadOnlySpan<nuint> valueHigh = value.Slice(n);

                // ... prepare our result array (to reuse its memory)
                Span<nuint> bitsLow = bits.Slice(0, n2);
                Span<nuint> bitsHigh = bits.Slice(n2);

                // ... compute z_0 = a_0 * a_0 (squaring again!)
                Square(valueLow, bitsLow);

                // ... compute z_2 = a_1 * a_1 (squaring again!)
                Square(valueHigh, bitsHigh);

                int foldLength = valueHigh.Length + 1;
                Span<nuint> fold = BigInteger.RentedBuffer.Create(foldLength, out BigInteger.RentedBuffer foldBuffer);

                int coreLength = foldLength + foldLength;
                Span<nuint> core = BigInteger.RentedBuffer.Create(coreLength, out BigInteger.RentedBuffer coreBuffer);

                // ... compute z_a = a_1 + a_0 (call it fold...)
                Add(valueHigh, valueLow, fold);

                // ... compute z_1 = z_a * z_a - z_0 - z_2
                Square(fold, core);

                foldBuffer.Dispose();

                SubtractCore(bitsHigh, bitsLow, core);

                // ... and finally merge the result! :-)
                AddSelf(bits.Slice(n), core);

                coreBuffer.Dispose();
            }

            static void Naive(ReadOnlySpan<nuint> value, Span<nuint> bits)
            {
                Debug.Assert(bits.Length == value.Length + value.Length);
                Debug.Assert(bits.Trim((nuint)0).IsEmpty);

                // Squares the bits using the "grammar-school" method.
                // Envisioning the "rhombus" of a pen-and-paper calculation
                // we see that computing z_i+j += a_j * a_i can be optimized
                // since a_j * a_i = a_i * a_j (we're squaring after all!).
                // Thus, we directly get z_i+j += 2 * a_j * a_i + c.

                // ATTENTION: an ordinary multiplication is safe, because
                // z_i+j + a_j * a_i + c <= 2(2^n - 1) + (2^n - 1)^2 =
                // = 2^(2n) - 1, where n = BitsPerLimb. But here we would need
                // one extra bit... Hence, we split these operation and do some
                // extra shifts.
                if (nint.Size == 8)
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        UInt128 carry = 0;
                        nuint v = value[i];
                        for (int j = 0; j < i; j++)
                        {
                            UInt128 digit1 = (UInt128)(ulong)bits[i + j] + carry;
                            UInt128 digit2 = (UInt128)(ulong)value[j] * (ulong)v;
                            bits[i + j] = (nuint)(ulong)(digit1 + (digit2 << 1));
                            // We need digit1 + 2*digit2, but that could overflow UInt128.
                            // Instead, compute (digit2 + digit1/2) >> 63 which gives the
                            // same carry without needing an extra bit of precision.
                            carry = (digit2 + (digit1 >> 1)) >> 63;
                        }

                        UInt128 digits = (UInt128)(ulong)v * (ulong)v + carry;
                        bits[i + i] = (nuint)(ulong)digits;
                        bits[i + i + 1] = (nuint)(ulong)(digits >> 64);
                    }
                }
                else
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        ulong carry = 0;
                        nuint v = value[i];
                        for (int j = 0; j < i; j++)
                        {
                            ulong digit1 = bits[i + j] + carry;
                            ulong digit2 = (ulong)value[j] * v;
                            bits[i + j] = (uint)(digit1 + (digit2 << 1));
                            carry = (digit2 + (digit1 >> 1)) >> 31;
                        }

                        ulong digits = (ulong)v * v + carry;
                        bits[i + i] = (uint)digits;
                        bits[i + i + 1] = (uint)(digits >> 32);
                    }
                }
            }
        }

        public static void Multiply(ReadOnlySpan<nuint> left, nuint right, Span<nuint> bits)
        {
            Debug.Assert(bits.Length == left.Length + 1);

            nuint carry = Mul1(bits, left, right);
            bits[left.Length] = carry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
        {
            if (left.Length < right.Length)
            {
                ReadOnlySpan<nuint> tmp = right;
                right = left;
                left = tmp;
            }

            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(right.Length >= 0);
            Debug.Assert(right.IsEmpty || bits.Length >= left.Length + right.Length);
            Debug.Assert(bits.Trim((nuint)0).IsEmpty);
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
            {
                Naive(left, right, bits);
            }
            else if ((left.Length + 1) >> 1 is int n && right.Length <= n)
            {
                RightSmall(left, right, bits, n);
            }
            else if (right.Length < MultiplyToom3Threshold)
            {
                Karatsuba(left, right, bits, n);
            }
            else
            {
                Toom3(left, right, bits);
            }

            static void Toom3(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
            {
                Debug.Assert(left.Length >= 3);
                Debug.Assert(left.Length >= right.Length);
                Debug.Assert(bits.Length >= left.Length + right.Length);
                Debug.Assert(bits.Trim((nuint)0).IsEmpty);

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
                nuint[] pAndQAllFromPool = ArrayPool<nuint>.Shared.Rent(pAndQAllLength);
                Span<nuint> pAndQAll = pAndQAllFromPool.AsSpan(0, pAndQAllLength);
                pAndQAll.Clear();

                Toom3Data p = Toom3Data.Build(left, n, pAndQAll.Slice(0, 3 * pLength));
                Toom3Data q = Toom3Data.Build(right, n, pAndQAll.Slice(3 * pLength));

                // Replace r_n in Wikipedia with z_n
                int rLength = pLength + pLength + 1;
                int rAndZAllLength = rLength * 3;
                nuint[] rAndZAllFromPool = ArrayPool<nuint>.Shared.Rent(rAndZAllLength);
                Span<nuint> rAndZAll = rAndZAllFromPool.AsSpan(0, rAndZAllLength);
                rAndZAll.Clear();

                p.MultiplyOther(q, n, bits, rAndZAll);

                ArrayPool<nuint>.Shared.Return(pAndQAllFromPool);
                ArrayPool<nuint>.Shared.Return(rAndZAllFromPool);
            }

            static void Toom25(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits, int n)
            {
                // Toom 2.5

                Debug.Assert(3 * n - left.Length is 0 or 1 or 2);
                Debug.Assert(right.Length > n);
                Debug.Assert(right.Length <= 2 * n);
                Debug.Assert(bits.Length >= left.Length + right.Length);
                Debug.Assert(bits.Trim((nuint)0).IsEmpty);

                ReadOnlySpan<nuint> left0 = left.Slice(0, n).TrimEnd((nuint)0);
                ReadOnlySpan<nuint> left1 = left.Slice(n, n).TrimEnd((nuint)0);
                ReadOnlySpan<nuint> left2 = left.Slice(n + n);

                ReadOnlySpan<nuint> right0 = right.Slice(0, n).TrimEnd((nuint)0);
                ReadOnlySpan<nuint> right1 = right.Slice(n);

                Span<nuint> z0 = bits.Slice(0, left0.Length + right0.Length);
                Span<nuint> z3 = bits.Slice(n * 3);
                Multiply(left0, right0, z0);
                Multiply(left2, right1, z3);

                int pLength = n + 1;
                int pAndQAllLength = pLength * 4;

                // The threshold for Toom-3 is expected to be greater than
                // StackAllocThreshold, so ArrayPool is always used.
                nuint[] pAndQAllFromPool = ArrayPool<nuint>.Shared.Rent(pAndQAllLength);
                Span<nuint> pAndQAll = pAndQAllFromPool.AsSpan(0, pAndQAllLength);
                pAndQAll.Clear();

                Span<nuint> p1 = pAndQAll.Slice(0, pLength);
                Span<nuint> pm1 = pAndQAll.Slice(pLength, pLength);
                Span<nuint> q1 = pAndQAll.Slice(pLength * 2, pLength);
                Span<nuint> qm1 = pAndQAll.Slice(pLength * 3, pLength);

                int pm1Sign = 1;
                int qm1Sign = 1;

                if (left0.Length < left2.Length)
                {
                    Add(left2, left0, pm1);
                }
                else
                {
                    Add(left0, left2, pm1);
                }

                pm1.CopyTo(p1);
                AddSelf(p1, left1);
                SubtractSelf(pm1, ref pm1Sign, left1);
                p1 = p1.TrimEnd((nuint)0);
                pm1 = pm1.TrimEnd((nuint)0);

                right0.CopyTo(q1);
                right0.CopyTo(qm1);
                AddSelf(q1, right1);
                SubtractSelf(qm1, ref qm1Sign, right1);
                q1 = q1.TrimEnd((nuint)0);
                qm1 = qm1.TrimEnd((nuint)0);

                int cLength = pLength * 2 + 1;
                int cAllLength = cLength * 3;
                nuint[] cAllFromPool = ArrayPool<nuint>.Shared.Rent(cAllLength);
                Span<nuint> cAll = cAllFromPool.AsSpan(0, cAllLength);
                cAll.Clear();

                Span<nuint> z1 = cAll.Slice(0, cLength);
                Span<nuint> c1 = z1.Slice(0, p1.Length + q1.Length);

                Span<nuint> z2 = cAll.Slice(cLength, cLength);
                Span<nuint> cm1 = cAll.Slice(cLength * 2, pm1.Length + qm1.Length);

                Multiply(p1, q1, c1);
                Multiply(pm1, qm1, cm1);

                int cm1Sign = pm1Sign * qm1Sign;
                int z2Sign = c1.IsEmpty ? 0 : 1;
                c1.CopyTo(z2);

                AddSelf(z2, ref z2Sign, cm1, -cm1Sign);
                Debug.Assert(z2Sign >= 0);
                RightShiftOne(z2);
                SubtractSelf(z2, z3.TrimEnd((nuint)0));

                AddSelf(z1, cm1);
                RightShiftOne(z1);
                AddSelf(z1, z0.TrimEnd((nuint)0));

                ArrayPool<nuint>.Shared.Return(pAndQAllFromPool);

                AddSelf(bits.Slice(n), z1.TrimEnd((nuint)0));
                AddSelf(bits.Slice(n * 2), z2.TrimEnd((nuint)0));

                ArrayPool<nuint>.Shared.Return(cAllFromPool);
            }

            static void Karatsuba(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits, int n)
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
                Debug.Assert(bits.Trim((nuint)0).IsEmpty);

                // ... we need to determine our new length (just the half)
                Debug.Assert(2 * n - left.Length is 0 or 1);
                Debug.Assert(right.Length > n);

                // ... split left like a = (a_1 << n) + a_0
                ReadOnlySpan<nuint> leftLow = left.Slice(0, n);
                ReadOnlySpan<nuint> leftHigh = left.Slice(n);

                // ... split right like b = (b_1 << n) + b_0
                ReadOnlySpan<nuint> rightLow = right.Slice(0, n);
                ReadOnlySpan<nuint> rightHigh = right.Slice(n);

                // ... prepare our result array (to reuse its memory)
                Span<nuint> bitsLow = bits.Slice(0, n + n);
                Span<nuint> bitsHigh = bits.Slice(n + n);

                Debug.Assert(leftLow.Length >= leftHigh.Length);
                Debug.Assert(rightLow.Length >= rightHigh.Length);
                Debug.Assert(bitsLow.Length >= bitsHigh.Length);

                // ... compute z_0 = a_0 * b_0 (multiply again)
                Multiply(leftLow, rightLow, bitsLow);

                // ... compute z_2 = a_1 * b_1 (multiply again)
                Multiply(leftHigh, rightHigh, bitsHigh);

                int foldLength = n + 1;
                Span<nuint> leftFold = BigInteger.RentedBuffer.Create(foldLength, out BigInteger.RentedBuffer leftFoldBuffer);

                Span<nuint> rightFold = BigInteger.RentedBuffer.Create(foldLength, out BigInteger.RentedBuffer rightFoldBuffer);

                // ... compute z_a = a_1 + a_0 (call it fold...)
                Add(leftLow, leftHigh, leftFold);

                // ... compute z_b = b_1 + b_0 (call it fold...)
                Add(rightLow, rightHigh, rightFold);

                int coreLength = foldLength + foldLength;
                Span<nuint> core = BigInteger.RentedBuffer.Create(coreLength, out BigInteger.RentedBuffer coreBuffer);

                // ... compute z_ab = z_a * z_b
                Multiply(leftFold, rightFold, core);

                leftFoldBuffer.Dispose();

                rightFoldBuffer.Dispose();

                // ... compute z_1 = z_a * z_b - z_0 - z_2 = a_0 * b_1 + a_1 * b_0
                SubtractCore(bitsLow, bitsHigh, core);

                Debug.Assert(ActualLength(core) <= left.Length + 1);

                // ... and finally merge the result! :-)
                AddSelf(bits.Slice(n), core.TrimEnd((nuint)0));

                coreBuffer.Dispose();
            }

            static void RightSmall(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits, int n)
            {
                Debug.Assert(left.Length >= right.Length);
                Debug.Assert(2 * n - left.Length is 0 or 1);
                Debug.Assert(right.Length <= n);
                Debug.Assert(bits.Length >= left.Length + right.Length);
                Debug.Assert(bits.Trim((nuint)0).IsEmpty);

                // ... split left like a = (a_1 << n) + a_0
                ReadOnlySpan<nuint> leftLow = left.Slice(0, n);
                ReadOnlySpan<nuint> leftHigh = left.Slice(n);
                Debug.Assert(leftLow.Length >= leftHigh.Length);

                // ... prepare our result array (to reuse its memory)
                Span<nuint> bitsLow = bits.Slice(0, n + right.Length);
                Span<nuint> bitsHigh = bits.Slice(n);

                // ... compute low
                Multiply(leftLow, right, bitsLow);

                int carryLength = right.Length;
                Span<nuint> carry = BigInteger.RentedBuffer.Create(carryLength, out BigInteger.RentedBuffer carryBuffer);

                Span<nuint> carryOrig = bitsHigh.Slice(0, right.Length);
                carryOrig.CopyTo(carry);
                carryOrig.Clear();

                // ... compute high
                Multiply(leftHigh, right, bitsHigh.Slice(0, leftHigh.Length + right.Length));

                AddSelf(bitsHigh, carry);

                carryBuffer.Dispose();
            }

            static void Naive(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
            {
                Debug.Assert(right.Length < MultiplyKaratsubaThreshold);

                // Multiplies the bits using the "grammar-school" method.
                // Envisioning the "rhombus" of a pen-and-paper calculation
                // should help getting the idea of these two loops...
                // The inner multiplication operations are safe, because
                // z_i+j + a_j * b_i + c <= 2(2^n - 1) + (2^n - 1)^2 =
                // = 2^(2n) - 1, where n = BitsPerLimb.

                for (int i = 0; i < right.Length; i++)
                {
                    nuint carry = MulAdd1(bits.Slice(i), left, right[i]);
                    bits[i + left.Length] = carry;
                }
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly ref struct Toom3Data(
            ReadOnlySpan<nuint> c0,
            ReadOnlySpan<nuint> cInf,
            ReadOnlySpan<nuint> c1,
            ReadOnlySpan<nuint> cm1,
            int cm1Sign,
            ReadOnlySpan<nuint> cm2,
            int cm2Sign)
        {
            private readonly ReadOnlySpan<nuint> c0 = c0;
            private readonly ReadOnlySpan<nuint> c1 = c1;
            private readonly ReadOnlySpan<nuint> cInf = cInf;
            private readonly ReadOnlySpan<nuint> cm1 = cm1;
            private readonly ReadOnlySpan<nuint> cm2 = cm2;
            private readonly int cm1Sign = cm1Sign;
            private readonly int cm2Sign = cm2Sign;

            public static Toom3Data Build(ReadOnlySpan<nuint> value, int n, Span<nuint> buffer)
            {
                Debug.Assert(!buffer.ContainsAnyExcept(0u));
                Debug.Assert(buffer.Length == 3 * (n + 1));
                Debug.Assert(value.Length > n);
                Debug.Assert(value[^1] != 0);

                int pLength = n + 1;

                ReadOnlySpan<nuint> v0, v1, v2;

                v0 = value.Slice(0, n).TrimEnd((nuint)0);
                if (value.Length <= n + n)
                {
                    v1 = value.Slice(n);
                    v2 = default;
                }
                else
                {
                    v1 = value.Slice(n, n).TrimEnd((nuint)0);
                    v2 = value.Slice(n + n);
                }

                Span<nuint> p1 = buffer.Slice(0, pLength);
                Span<nuint> pm1 = buffer.Slice(pLength, pLength);

                // Calculate p(1) = p_0 + m_1, p(-1) = p_0 - m_1
                int pm1Sign = 1;
                {
                    v0.CopyTo(p1);
                    AddSelf(p1, v2);

                    p1.CopyTo(pm1);
                    AddSelf(p1, v1);

                    SubtractSelf(pm1, ref pm1Sign, v1);

                    pm1 = pm1Sign != 0 ? pm1.TrimEnd((nuint)0) : default;
                }

                // Calculate p(-2) = (p(-1) + m_2)*2 - m_0
                int pm2Sign = pm1Sign;
                Span<nuint> pm2 = buffer.Slice(pLength + pLength, pLength);
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
                        Debug.Assert(pm2[^1] < ((nuint)1 << (BitsPerLimb - 1)));
                        LeftShiftOne(pm2);
                    }

                    Debug.Assert(pm2[^1] != nuint.MaxValue);

                    // Calculate p(-2) = (p(-1) + m_2)*2 - m_0
                    SubtractSelf(pm2, ref pm2Sign, v0);

                    pm2 = pm2.TrimEnd((nuint)0);
                }

                return new Toom3Data(
                    c0: v0,
                    c1: p1.TrimEnd((nuint)0),
                    cInf: v2,
                    cm1: pm1.TrimEnd((nuint)0),
                    cm2: pm2,
                    cm1Sign: pm1Sign,
                    cm2Sign: pm2Sign
                );
            }

            public void MultiplyOther(in Toom3Data right, int n, Span<nuint> bits, Span<nuint> buffer)
            {
                Debug.Assert(!buffer.ContainsAnyExcept(0u));
                Debug.Assert(cInf.Length >= right.cInf.Length);

                int rLength = n + n + 3;

                ReadOnlySpan<nuint> p0 = c0;
                ReadOnlySpan<nuint> q0 = right.c0;

                ReadOnlySpan<nuint> p1 = c1;
                ReadOnlySpan<nuint> q1 = right.c1;

                ReadOnlySpan<nuint> pm1 = cm1;
                ReadOnlySpan<nuint> qm1 = right.cm1;

                ReadOnlySpan<nuint> pm2 = cm2;
                ReadOnlySpan<nuint> qm2 = right.cm2;

                ReadOnlySpan<nuint> pInf = cInf;
                ReadOnlySpan<nuint> qInf = right.cInf;


                Span<nuint> r0 = bits.Slice(0, p0.Length + q0.Length);
                Span<nuint> rInf =
                    !qInf.IsEmpty
                        ? bits.Slice(4 * n, pInf.Length + qInf.Length)
                        : default;

                Span<nuint> r1 = buffer.Slice(0, p1.Length + q1.Length);
                Span<nuint> rm1 = buffer.Slice(rLength, pm1.Length + qm1.Length);
                Span<nuint> rm2 = buffer.Slice(rLength * 2, pm2.Length + qm2.Length);

                Multiply(p0, q0, r0);
                Multiply(p1, q1, r1);
                Multiply(pm1, qm1, rm1);
                Multiply(pm2, qm2, rm2);
                Multiply(pInf, qInf, rInf);

                Toom3CalcResult(
                    n,
                    r0: r0.TrimEnd((nuint)0),
                    rInf: rInf.TrimEnd((nuint)0),
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

            public void Square(int n, Span<nuint> bits, Span<nuint> buffer)
            {
                Debug.Assert(!buffer.ContainsAnyExcept(0u));
                Debug.Assert(!cInf.IsEmpty);

                int rLength = n + n + 3;

                ReadOnlySpan<nuint> p0 = c0;
                ReadOnlySpan<nuint> p1 = c1;
                ReadOnlySpan<nuint> pm1 = cm1;
                ReadOnlySpan<nuint> pm2 = cm2;
                ReadOnlySpan<nuint> pInf = cInf;

                Span<nuint> r0 = bits.Slice(0, p0.Length << 1);
                Span<nuint> rInf = bits.Slice(4 * n, pInf.Length << 1);

                Span<nuint> r1 = buffer.Slice(0, p1.Length << 1);
                Span<nuint> rm1 = buffer.Slice(rLength, pm1.Length << 1);
                Span<nuint> rm2 = buffer.Slice(rLength * 2, pm2.Length << 1);

                BigIntegerCalculator.Square(p0, r0);
                BigIntegerCalculator.Square(p1, r1);
                BigIntegerCalculator.Square(pm1, rm1);
                BigIntegerCalculator.Square(pm2, rm2);
                BigIntegerCalculator.Square(pInf, rInf);

                Toom3CalcResult(
                    n,
                    r0: r0.TrimEnd((nuint)0),
                    rInf: rInf.TrimEnd((nuint)0),
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
                ReadOnlySpan<nuint> r0,
                ReadOnlySpan<nuint> rInf,
                Span<nuint> z1,
                int r1Length,
                Span<nuint> z2,
                int z2Sign,
                int rm1Length,
                Span<nuint> z3,
                int z3Sign,
                Span<nuint> bits)
            {
                int z1Sign = Math.Sign(r1Length);

                // Calc z_3 = (r(-2) - r(1))/3
                {
                    // Calc r(-2) - r(1)
                    SubtractSelf(z3, ref z3Sign, z1.Slice(0, r1Length));

                    // Calc (r(-2) - r(1))/3
                    DivideThreeSelf(z3.TrimEnd((nuint)0));
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
                    AddSelf(z2, ref z2Sign, z1.TrimEnd((nuint)0));
                    SubtractSelf(z2, ref z2Sign, rInf);
                }

                // Calc z_1 = z_1 - z_3
                SubtractSelf(z1, ref z1Sign, z3.TrimEnd((nuint)0));

                Debug.Assert(z1Sign >= 0);
                Debug.Assert(z2Sign >= 0);
                Debug.Assert(z3Sign >= 0);

                AddSelf(bits.Slice(n), z1.TrimEnd((nuint)0));
                AddSelf(bits.Slice(2 * n), z2.TrimEnd((nuint)0));

                if (bits.Length >= 3 * n)
                {
                    AddSelf(bits.Slice(3 * n), z3.TrimEnd((nuint)0));
                }
            }
        }

        private static void DivideThreeSelf(Span<nuint> bits)
        {
            nuint oneThird, twoThirds;
            if (nint.Size == 8)
            {
                ulong oneThird64 = 0x5555_5555_5555_5555;
                ulong twoThirds64 = 0xAAAA_AAAA_AAAA_AAAA;
                oneThird = (nuint)oneThird64;
                twoThirds = (nuint)twoThirds64;
            }
            else
            {
                oneThird = 0x5555_5555;
                twoThirds = 0xAAAA_AAAA;
            }

            nuint carry = 0;
            for (int i = bits.Length - 1; i >= 0; i--)
            {
                nuint quo = bits[i] / 3;
                nuint rem = bits[i] - quo * 3;

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
                    {
                        ++quo;
                    }
                    else
                    {
                        rem = 2;
                    }

                    bits[i] = twoThirds + quo;
                    carry = rem;
                }
            }

            Debug.Assert(carry == 0);
        }

        private static void SubtractCore(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> core)
        {
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(core.Length >= left.Length);

            // Executes a special subtraction algorithm for the multiplication,
            // which needs to subtract two different values from a core value,
            // while core is always bigger than the sum of these values.

            // NOTE: we could do an ordinary subtraction of course, but we spare
            // one "run", if we do this computation within a single one...

            int i = 0;

            if (right.Length != 0)
            {
                _ = left[right.Length - 1];
                _ = core[left.Length - 1];
            }

            if (nint.Size == 8)
            {
                Int128 carry = 0;

                for (; i < right.Length; i++)
                {
                    Int128 digit = (Int128)(ulong)core[i] + carry - (ulong)left[i] - (ulong)right[i];
                    core[i] = (nuint)(ulong)digit;
                    carry = digit >> 64;
                }

                for (; i < left.Length; i++)
                {
                    Int128 digit = (Int128)(ulong)core[i] + carry - (ulong)left[i];
                    core[i] = (nuint)(ulong)digit;
                    carry = digit >> 64;
                }

                for (; carry != 0 && i < core.Length; i++)
                {
                    Int128 digit = (Int128)(ulong)core[i] + carry;
                    core[i] = (nuint)(ulong)digit;
                    carry = digit >> 64;
                }
            }
            else
            {
                long carry = 0L;

                for (; i < right.Length; i++)
                {
                    long digit = ((uint)core[i] + carry) - (uint)left[i] - (uint)right[i];
                    core[i] = (uint)digit;
                    carry = digit >> 32;
                }

                for (; i < left.Length; i++)
                {
                    long digit = ((uint)core[i] + carry) - (uint)left[i];
                    core[i] = (uint)digit;
                    carry = digit >> 32;
                }

                for (; carry != 0 && i < core.Length; i++)
                {
                    long digit = (uint)core[i] + carry;
                    core[i] = (uint)digit;
                    carry = digit >> 32;
                }
            }
        }


        private static void AddSelf(Span<nuint> left, ref int leftSign, ReadOnlySpan<nuint> right, int rightSign)
        {
            Debug.Assert(left.Length >= right.Length);

            if (rightSign == 0)
            {
                return;
            }
            else if (rightSign > 0)
            {
                AddSelf(left, ref leftSign, right);
            }
            else
            {
                SubtractSelf(left, ref leftSign, right);
            }
        }

        private static void AddSelf(Span<nuint> left, ref int leftSign, ReadOnlySpan<nuint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            right = right.TrimEnd((nuint)0);

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
                    // right > left: compute right - left directly
                    left = left.Slice(0, right.Length);
                    nuint borrow = 0;
                    for (int j = 0; j < left.Length; j++)
                    {
                        left[j] = SubWithBorrow(right[j], left[j], borrow, out borrow);
                    }

                    Debug.Assert(borrow == 0);
                }
            }
        }

        private static void SubtractSelf(Span<nuint> left, ref int leftSign, ReadOnlySpan<nuint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            right = right.TrimEnd((nuint)0);

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
                    // right > left: compute right - left directly
                    left = left.Slice(0, right.Length);
                    nuint borrow = 0;
                    for (int j = 0; j < left.Length; j++)
                    {
                        left[j] = SubWithBorrow(right[j], left[j], borrow, out borrow);
                    }

                    Debug.Assert(borrow == 0);
                }
            }
        }

        private static void LeftShiftOne(Span<nuint> bits)
        {
            nuint carry = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                nuint value = carry | bits[i] << 1;
                carry = bits[i] >> (BitsPerLimb - 1);
                bits[i] = value;
            }
        }

        private static void RightShiftOne(Span<nuint> bits)
        {
            nuint carry = 0;
            for (int i = bits.Length - 1; i >= 0; i--)
            {
                nuint value = carry | bits[i] >> 1;
                carry = bits[i] << (BitsPerLimb - 1);
                bits[i] = value;
            }
        }

    }
}
