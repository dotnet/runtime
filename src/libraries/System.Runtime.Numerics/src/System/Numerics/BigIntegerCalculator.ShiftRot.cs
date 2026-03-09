// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        public static void RotateLeft(Span<uint> bits, long rotateLeftAmount)
        {
            Debug.Assert(Math.Abs(rotateLeftAmount) <= 0x80000000);

            const int digitShiftMax = (int)(0x80000000 / 32);

            int digitShift = digitShiftMax;
            int smallShift = 0;

            if (rotateLeftAmount < 0)
            {
                if (rotateLeftAmount != -0x80000000)
                    (digitShift, smallShift) = Math.DivRem(-(int)rotateLeftAmount, 32);

                RotateRight(bits, digitShift % bits.Length, smallShift);
            }
            else
            {
                if (rotateLeftAmount != 0x80000000)
                    (digitShift, smallShift) = Math.DivRem((int)rotateLeftAmount, 32);

                RotateLeft(bits, digitShift % bits.Length, smallShift);
            }
        }

        public static void RotateLeft(Span<uint> bits, int digitShift, int smallShift)
        {
            Debug.Assert(bits.Length > 0);

            LeftShiftSelf(bits, smallShift, out uint carry);
            bits[0] |= carry;

            if (digitShift == 0)
                return;

            SwapUpperAndLower(bits, bits.Length - digitShift);
        }

        public static void RotateRight(Span<uint> bits, int digitShift, int smallShift)
        {
            Debug.Assert(bits.Length > 0);

            RightShiftSelf(bits, smallShift, out uint carry);
            bits[^1] |= carry;

            if (digitShift == 0)
                return;

            SwapUpperAndLower(bits, digitShift);
        }

        private static void SwapUpperAndLower(Span<uint> bits, int lowerLength)
        {
            Debug.Assert(lowerLength > 0);
            Debug.Assert(lowerLength < bits.Length);

            int upperLength = bits.Length - lowerLength;

            Span<uint> lower = bits.Slice(0, lowerLength);
            Span<uint> upper = bits.Slice(lowerLength);

            Span<uint> lowerDst = bits.Slice(upperLength);

            int tmpLength = Math.Min(lowerLength, upperLength);
            uint[]? tmpFromPool = null;
            Span<uint> tmp = ((uint)tmpLength <= StackAllocThreshold ?
                                  stackalloc uint[StackAllocThreshold]
                                  : tmpFromPool = ArrayPool<uint>.Shared.Rent(tmpLength)).Slice(0, tmpLength);

            if (upperLength < lowerLength)
            {
                upper.CopyTo(tmp);
                lower.CopyTo(lowerDst);
                tmp.CopyTo(bits);
            }
            else
            {
                lower.CopyTo(tmp);
                upper.CopyTo(bits);
                tmp.CopyTo(lowerDst);
            }

            if (tmpFromPool != null)
                ArrayPool<uint>.Shared.Return(tmpFromPool);
        }

        public static void LeftShiftSelf(Span<uint> bits, int shift, out uint carry)
        {
            Debug.Assert((uint)shift < 32);

            carry = 0;
            if (shift == 0 || bits.IsEmpty)
                return;

            int back = 32 - shift;

            if (Vector128.IsHardwareAccelerated)
            {
                carry = bits[^1] >> back;

                ref uint start = ref MemoryMarshal.GetReference(bits);
                int offset = bits.Length;

                while (Vector512.IsHardwareAccelerated && offset >= Vector512<uint>.Count + 1)
                {
                    Vector512<uint> current = Vector512.LoadUnsafe(ref start, (nuint)(offset - Vector512<uint>.Count)) << shift;
                    Vector512<uint> carries = Vector512.LoadUnsafe(ref start, (nuint)(offset - (Vector512<uint>.Count + 1))) >> back;

                    Vector512<uint> newValue = current | carries;

                    Vector512.StoreUnsafe(newValue, ref start, (nuint)(offset - Vector512<uint>.Count));
                    offset -= Vector512<uint>.Count;
                }

                while (Vector256.IsHardwareAccelerated && offset >= Vector256<uint>.Count + 1)
                {
                    Vector256<uint> current = Vector256.LoadUnsafe(ref start, (nuint)(offset - Vector256<uint>.Count)) << shift;
                    Vector256<uint> carries = Vector256.LoadUnsafe(ref start, (nuint)(offset - (Vector256<uint>.Count + 1))) >> back;

                    Vector256<uint> newValue = current | carries;

                    Vector256.StoreUnsafe(newValue, ref start, (nuint)(offset - Vector256<uint>.Count));
                    offset -= Vector256<uint>.Count;
                }

                while (Vector128.IsHardwareAccelerated && offset >= Vector128<uint>.Count + 1)
                {
                    Vector128<uint> current = Vector128.LoadUnsafe(ref start, (nuint)(offset - Vector128<uint>.Count)) << shift;
                    Vector128<uint> carries = Vector128.LoadUnsafe(ref start, (nuint)(offset - (Vector128<uint>.Count + 1))) >> back;

                    Vector128<uint> newValue = current | carries;

                    Vector128.StoreUnsafe(newValue, ref start, (nuint)(offset - Vector128<uint>.Count));
                    offset -= Vector128<uint>.Count;
                }

                uint carry2 = 0;
                for (int i = 0; i < offset; i++)
                {
                    uint value = carry2 | bits[i] << shift;
                    carry2 = bits[i] >> back;
                    bits[i] = value;
                }
            }
            else
            {
                carry = 0;
                for (int i = 0; i < bits.Length; i++)
                {
                    uint value = carry | bits[i] << shift;
                    carry = bits[i] >> back;
                    bits[i] = value;
                }
            }
        }
        public static void RightShiftSelf(Span<uint> bits, int shift, out uint carry)
        {
            Debug.Assert((uint)shift < 32);

            carry = 0;
            if (shift == 0 || bits.IsEmpty)
                return;

            int back = 32 - shift;

            if (Vector128.IsHardwareAccelerated)
            {
                carry = bits[0] << back;

                ref uint start = ref MemoryMarshal.GetReference(bits);
                int offset = 0;

                while (Vector512.IsHardwareAccelerated && bits.Length - offset >= Vector512<uint>.Count + 1)
                {
                    Vector512<uint> current = Vector512.LoadUnsafe(ref start, (nuint)offset) >> shift;
                    Vector512<uint> carries = Vector512.LoadUnsafe(ref start, (nuint)(offset + 1)) << back;

                    Vector512<uint> newValue = current | carries;

                    Vector512.StoreUnsafe(newValue, ref start, (nuint)offset);
                    offset += Vector512<uint>.Count;
                }

                while (Vector256.IsHardwareAccelerated && bits.Length - offset >= Vector256<uint>.Count + 1)
                {
                    Vector256<uint> current = Vector256.LoadUnsafe(ref start, (nuint)offset) >> shift;
                    Vector256<uint> carries = Vector256.LoadUnsafe(ref start, (nuint)(offset + 1)) << back;

                    Vector256<uint> newValue = current | carries;

                    Vector256.StoreUnsafe(newValue, ref start, (nuint)offset);
                    offset += Vector256<uint>.Count;
                }

                while (Vector128.IsHardwareAccelerated && bits.Length - offset >= Vector128<uint>.Count + 1)
                {
                    Vector128<uint> current = Vector128.LoadUnsafe(ref start, (nuint)offset) >> shift;
                    Vector128<uint> carries = Vector128.LoadUnsafe(ref start, (nuint)(offset + 1)) << back;

                    Vector128<uint> newValue = current | carries;

                    Vector128.StoreUnsafe(newValue, ref start, (nuint)offset);
                    offset += Vector128<uint>.Count;
                }

                uint carry2 = 0;
                for (int i = bits.Length - 1; i >= offset; i--)
                {
                    uint value = carry2 | bits[i] >> shift;
                    carry2 = bits[i] << back;
                    bits[i] = value;
                }
            }
            else
            {
                carry = 0;
                for (int i = bits.Length - 1; i >= 0; i--)
                {
                    uint value = carry | bits[i] >> shift;
                    carry = bits[i] << back;
                    bits[i] = value;
                }
            }
        }
    }
}
