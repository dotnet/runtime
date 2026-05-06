// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        public static void RotateLeft(Span<nuint> bits, long rotateLeftAmount)
        {
            Debug.Assert(Math.Abs(rotateLeftAmount) <= 0x80000000);

            int digitShiftMax = (int)(0x80000000 / BitsPerLimb);

            int digitShift = digitShiftMax;
            int smallShift = 0;

            if (rotateLeftAmount < 0)
            {
                if (rotateLeftAmount != -0x80000000)
                {
                    (digitShift, smallShift) = Math.DivRem(-(int)rotateLeftAmount, BitsPerLimb);
                }

                RotateRight(bits, digitShift % bits.Length, smallShift);
            }
            else
            {
                if (rotateLeftAmount != 0x80000000)
                {
                    (digitShift, smallShift) = Math.DivRem((int)rotateLeftAmount, BitsPerLimb);
                }

                RotateLeft(bits, digitShift % bits.Length, smallShift);
            }
        }

        private static void RotateLeft(Span<nuint> bits, int digitShift, int smallShift)
        {
            Debug.Assert(bits.Length > 0);

            LeftShiftSelf(bits, smallShift, out nuint carry);
            bits[0] |= carry;

            if (digitShift == 0)
            {
                return;
            }

            SwapUpperAndLower(bits, bits.Length - digitShift);
        }

        private static void RotateRight(Span<nuint> bits, int digitShift, int smallShift)
        {
            Debug.Assert(bits.Length > 0);

            RightShiftSelf(bits, smallShift, out nuint carry);
            bits[^1] |= carry;

            if (digitShift == 0)
            {
                return;
            }

            SwapUpperAndLower(bits, digitShift);
        }

        public static void SwapUpperAndLower(Span<nuint> bits, int lowerLength)
        {
            Debug.Assert(lowerLength > 0);
            Debug.Assert(lowerLength < bits.Length);

            int upperLength = bits.Length - lowerLength;

            Span<nuint> lower = bits.Slice(0, lowerLength);
            Span<nuint> upper = bits.Slice(lowerLength);

            Span<nuint> lowerDst = bits.Slice(upperLength);

            int tmpLength = Math.Min(lowerLength, upperLength);
            Span<nuint> tmp = BigInteger.RentedBuffer.Create(tmpLength, out BigInteger.RentedBuffer tmpBuffer);

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

            tmpBuffer.Dispose();
        }

        // 32-bit word digit-swap for the partial-limb edge case on 64-bit.
        // When the rotation ring has an odd number of 32-bit words, the digit-swap
        // boundary may fall mid-nuint, so the swap must operate at uint granularity.

        public static void SwapUpperAndLower(Span<uint> bits, int lowerLength)
        {
            Debug.Assert(lowerLength > 0);
            Debug.Assert(lowerLength < bits.Length);

            int upperLength = bits.Length - lowerLength;

            Span<uint> lower = bits.Slice(0, lowerLength);
            Span<uint> upper = bits.Slice(lowerLength);

            Span<uint> lowerDst = bits.Slice(upperLength);

            int tmpLength = Math.Min(lowerLength, upperLength);
            int wordsPerLimb = nint.Size / sizeof(uint);
            int nuintCount = (tmpLength + wordsPerLimb - 1) / wordsPerLimb;
            Span<nuint> tmpNuint = BigInteger.RentedBuffer.Create(nuintCount, out BigInteger.RentedBuffer tmpBuffer);
            Span<uint> tmp = MemoryMarshal.Cast<nuint, uint>(tmpNuint).Slice(0, tmpLength);

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

            tmpBuffer.Dispose();
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

                Span<nuint> data = bits;

                // Each vector load needs one extra element below it to source the carry bits
                // that shift in from the lower limbs, hence data.Length > Count (not >=).
                while (Vector512.IsHardwareAccelerated && data.Length > Vector512<nuint>.Count)
                {
                    int tailStart = data.Length - Vector512<nuint>.Count;
                    Vector512<nuint> current = Vector512.Create((ReadOnlySpan<nuint>)data.Slice(tailStart)) << shift;
                    Vector512<nuint> carries = Vector512.Create((ReadOnlySpan<nuint>)data.Slice(tailStart - 1, Vector512<nuint>.Count)) >> back;

                    Vector512<nuint> newValue = current | carries;

                    newValue.CopyTo(data.Slice(tailStart));
                    data = data.Slice(0, tailStart);
                }

                while (Vector256.IsHardwareAccelerated && data.Length > Vector256<nuint>.Count)
                {
                    int tailStart = data.Length - Vector256<nuint>.Count;
                    Vector256<nuint> current = Vector256.Create((ReadOnlySpan<nuint>)data.Slice(tailStart)) << shift;
                    Vector256<nuint> carries = Vector256.Create((ReadOnlySpan<nuint>)data.Slice(tailStart - 1, Vector256<nuint>.Count)) >> back;

                    Vector256<nuint> newValue = current | carries;

                    newValue.CopyTo(data.Slice(tailStart));
                    data = data.Slice(0, tailStart);
                }

                while (Vector128.IsHardwareAccelerated && data.Length > Vector128<nuint>.Count)
                {
                    int tailStart = data.Length - Vector128<nuint>.Count;
                    Vector128<nuint> current = Vector128.Create((ReadOnlySpan<nuint>)data.Slice(tailStart)) << shift;
                    Vector128<nuint> carries = Vector128.Create((ReadOnlySpan<nuint>)data.Slice(tailStart - 1, Vector128<nuint>.Count)) >> back;

                    Vector128<nuint> newValue = current | carries;

                    newValue.CopyTo(data.Slice(tailStart));
                    data = data.Slice(0, tailStart);
                }

                nuint carry2 = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    nuint value = carry2 | data[i] << shift;
                    carry2 = data[i] >> back;
                    data[i] = value;
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

            carry = bits[0] << back;

            Span<nuint> remaining = bits;

            while (Vector512.IsHardwareAccelerated && remaining.Length > Vector512<nuint>.Count)
            {
                Vector512<nuint> current = Vector512.Create((ReadOnlySpan<nuint>)remaining) >> shift;
                Vector512<nuint> carries = Vector512.Create((ReadOnlySpan<nuint>)remaining.Slice(1)) << back;

                Vector512<nuint> newValue = current | carries;

                newValue.CopyTo(remaining);
                remaining = remaining.Slice(Vector512<nuint>.Count);
            }

            while (Vector256.IsHardwareAccelerated && remaining.Length > Vector256<nuint>.Count)
            {
                Vector256<nuint> current = Vector256.Create((ReadOnlySpan<nuint>)remaining) >> shift;
                Vector256<nuint> carries = Vector256.Create((ReadOnlySpan<nuint>)remaining.Slice(1)) << back;

                Vector256<nuint> newValue = current | carries;

                newValue.CopyTo(remaining);
                remaining = remaining.Slice(Vector256<nuint>.Count);
            }

            while (Vector128.IsHardwareAccelerated && remaining.Length > Vector128<nuint>.Count)
            {
                Vector128<nuint> current = Vector128.Create((ReadOnlySpan<nuint>)remaining) >> shift;
                Vector128<nuint> carries = Vector128.Create((ReadOnlySpan<nuint>)remaining.Slice(1)) << back;

                Vector128<nuint> newValue = current | carries;

                newValue.CopyTo(remaining);
                remaining = remaining.Slice(Vector128<nuint>.Count);
            }

            for (int i = 0; i < remaining.Length - 1; i++)
            {
                remaining[i] = (remaining[i] >> shift) | (remaining[i + 1] << back);
            }
            remaining[remaining.Length - 1] >>= shift;
        }
    }
}
