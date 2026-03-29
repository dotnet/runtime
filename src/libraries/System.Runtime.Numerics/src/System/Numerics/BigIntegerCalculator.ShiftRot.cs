// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        /// <summary>
        /// Rotates a span of 32-bit words left by the specified amount.
        /// This provides platform-independent 32-bit word rotation semantics.
        /// </summary>
        public static void RotateLeft32(Span<uint> bits, long rotateLeftAmount)
        {
            Debug.Assert(Math.Abs(rotateLeftAmount) <= 0x80000000);

            const int BitsPerWord = 32;
            int digitShiftMax = (int)(0x80000000 / BitsPerWord);

            int digitShift = digitShiftMax;
            int smallShift = 0;

            if (rotateLeftAmount < 0)
            {
                if (rotateLeftAmount != -0x80000000)
                {
                    (digitShift, smallShift) = Math.DivRem(-(int)rotateLeftAmount, BitsPerWord);
                }

                RotateRight32(bits, digitShift % bits.Length, smallShift);
            }
            else
            {
                if (rotateLeftAmount != 0x80000000)
                {
                    (digitShift, smallShift) = Math.DivRem((int)rotateLeftAmount, BitsPerWord);
                }

                RotateLeft32(bits, digitShift % bits.Length, smallShift);
            }
        }

        private static void RotateLeft32(Span<uint> bits, int digitShift, int smallShift)
        {
            Debug.Assert(bits.Length > 0);

            LeftShiftSelf32(bits, smallShift, out uint carry);
            bits[0] |= carry;

            if (digitShift == 0)
            {
                return;
            }

            SwapUpperAndLower32(bits, bits.Length - digitShift);
        }

        private static void RotateRight32(Span<uint> bits, int digitShift, int smallShift)
        {
            Debug.Assert(bits.Length > 0);

            RightShiftSelf32(bits, smallShift, out uint carry);
            bits[^1] |= carry;

            if (digitShift == 0)
            {
                return;
            }

            SwapUpperAndLower32(bits, digitShift);
        }

        private static void SwapUpperAndLower32(Span<uint> bits, int lowerLength)
        {
            Debug.Assert(lowerLength > 0);
            Debug.Assert(lowerLength < bits.Length);

            int upperLength = bits.Length - lowerLength;

            Span<uint> lower = bits.Slice(0, lowerLength);
            Span<uint> upper = bits.Slice(lowerLength);

            Span<uint> lowerDst = bits.Slice(upperLength);

            int tmpLength = Math.Min(lowerLength, upperLength);
            uint[] tmpArray = System.Buffers.ArrayPool<uint>.Shared.Rent(tmpLength);
            Span<uint> tmp = tmpArray.AsSpan(0, tmpLength);

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

            System.Buffers.ArrayPool<uint>.Shared.Return(tmpArray);
        }

        private static void LeftShiftSelf32(Span<uint> bits, int shift, out uint carry)
        {
            Debug.Assert((uint)shift < 32);

            carry = 0;
            if (shift == 0 || bits.IsEmpty)
            {
                return;
            }

            int back = 32 - shift;
            carry = bits[^1] >> back;

            for (int i = bits.Length - 1; i > 0; i--)
            {
                bits[i] = (bits[i] << shift) | (bits[i - 1] >> back);
            }
            bits[0] <<= shift;
        }

        private static void RightShiftSelf32(Span<uint> bits, int shift, out uint carry)
        {
            Debug.Assert((uint)shift < 32);

            carry = 0;
            if (shift == 0 || bits.IsEmpty)
            {
                return;
            }

            int back = 32 - shift;
            carry = bits[0] << back;

            for (int i = 0; i < bits.Length - 1; i++)
            {
                bits[i] = (bits[i] >> shift) | (bits[i + 1] << back);
            }
            bits[^1] >>= shift;
        }

        public static void LeftShiftSelf(Span<nuint> bits, int shift, out nuint carry)
        {
            Debug.Assert((uint)shift < BitsPerLimb);

            carry = 0;
            if (shift == 0 || bits.IsEmpty)
            {
                return;
            }

            int back = BitsPerLimb - shift;

            if (Vector128.IsHardwareAccelerated)
            {
                carry = bits[^1] >> back;

                ref nuint start = ref MemoryMarshal.GetReference(bits);
                int offset = bits.Length;

                // Each vector load needs one extra element below it to source the carry bits
                // that shift in from the lower limbs, hence the +1 minimum.
                while (Vector512.IsHardwareAccelerated && offset >= Vector512<nuint>.Count + 1)
                {
                    Vector512<nuint> current = Vector512.LoadUnsafe(ref start, (nuint)(offset - Vector512<nuint>.Count)) << shift;
                    Vector512<nuint> carries = Vector512.LoadUnsafe(ref start, (nuint)(offset - (Vector512<nuint>.Count + 1))) >> back;

                    Vector512<nuint> newValue = current | carries;

                    Vector512.StoreUnsafe(newValue, ref start, (nuint)(offset - Vector512<nuint>.Count));
                    offset -= Vector512<nuint>.Count;
                }

                while (Vector256.IsHardwareAccelerated && offset >= Vector256<nuint>.Count + 1)
                {
                    Vector256<nuint> current = Vector256.LoadUnsafe(ref start, (nuint)(offset - Vector256<nuint>.Count)) << shift;
                    Vector256<nuint> carries = Vector256.LoadUnsafe(ref start, (nuint)(offset - (Vector256<nuint>.Count + 1))) >> back;

                    Vector256<nuint> newValue = current | carries;

                    Vector256.StoreUnsafe(newValue, ref start, (nuint)(offset - Vector256<nuint>.Count));
                    offset -= Vector256<nuint>.Count;
                }

                while (Vector128.IsHardwareAccelerated && offset >= Vector128<nuint>.Count + 1)
                {
                    Vector128<nuint> current = Vector128.LoadUnsafe(ref start, (nuint)(offset - Vector128<nuint>.Count)) << shift;
                    Vector128<nuint> carries = Vector128.LoadUnsafe(ref start, (nuint)(offset - (Vector128<nuint>.Count + 1))) >> back;

                    Vector128<nuint> newValue = current | carries;

                    Vector128.StoreUnsafe(newValue, ref start, (nuint)(offset - Vector128<nuint>.Count));
                    offset -= Vector128<nuint>.Count;
                }

                nuint carry2 = 0;
                for (int i = 0; i < offset; i++)
                {
                    nuint value = carry2 | bits[i] << shift;
                    carry2 = bits[i] >> back;
                    bits[i] = value;
                }
            }
            else
            {
                carry = 0;
                for (int i = 0; i < bits.Length; i++)
                {
                    nuint value = carry | bits[i] << shift;
                    carry = bits[i] >> back;
                    bits[i] = value;
                }
            }
        }

        public static void RightShiftSelf(Span<nuint> bits, int shift, out nuint carry)
        {
            Debug.Assert((uint)shift < BitsPerLimb);

            carry = 0;
            if (shift == 0 || bits.IsEmpty)
            {
                return;
            }

            int back = BitsPerLimb - shift;

            if (Vector128.IsHardwareAccelerated)
            {
                carry = bits[0] << back;

                ref nuint start = ref MemoryMarshal.GetReference(bits);
                int offset = 0;

                while (Vector512.IsHardwareAccelerated && bits.Length - offset >= Vector512<nuint>.Count + 1)
                {
                    Vector512<nuint> current = Vector512.LoadUnsafe(ref start, (nuint)offset) >> shift;
                    Vector512<nuint> carries = Vector512.LoadUnsafe(ref start, (nuint)(offset + 1)) << back;

                    Vector512<nuint> newValue = current | carries;

                    Vector512.StoreUnsafe(newValue, ref start, (nuint)offset);
                    offset += Vector512<nuint>.Count;
                }

                while (Vector256.IsHardwareAccelerated && bits.Length - offset >= Vector256<nuint>.Count + 1)
                {
                    Vector256<nuint> current = Vector256.LoadUnsafe(ref start, (nuint)offset) >> shift;
                    Vector256<nuint> carries = Vector256.LoadUnsafe(ref start, (nuint)(offset + 1)) << back;

                    Vector256<nuint> newValue = current | carries;

                    Vector256.StoreUnsafe(newValue, ref start, (nuint)offset);
                    offset += Vector256<nuint>.Count;
                }

                while (Vector128.IsHardwareAccelerated && bits.Length - offset >= Vector128<nuint>.Count + 1)
                {
                    Vector128<nuint> current = Vector128.LoadUnsafe(ref start, (nuint)offset) >> shift;
                    Vector128<nuint> carries = Vector128.LoadUnsafe(ref start, (nuint)(offset + 1)) << back;

                    Vector128<nuint> newValue = current | carries;

                    Vector128.StoreUnsafe(newValue, ref start, (nuint)offset);
                    offset += Vector128<nuint>.Count;
                }

                nuint carry2 = 0;
                for (int i = bits.Length - 1; i >= offset; i--)
                {
                    nuint value = carry2 | bits[i] >> shift;
                    carry2 = bits[i] << back;
                    bits[i] = value;
                }
            }
            else
            {
                carry = 0;
                for (int i = bits.Length - 1; i >= 0; i--)
                {
                    nuint value = carry | bits[i] >> shift;
                    carry = bits[i] << back;
                    bits[i] = value;
                }
            }
        }
    }
}
