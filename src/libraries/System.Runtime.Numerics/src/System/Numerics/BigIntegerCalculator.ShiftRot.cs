// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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

        public static void RotateLeft(Span<nuint> bits, int digitShift, int smallShift)
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

        public static void RotateRight(Span<nuint> bits, int digitShift, int smallShift)
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

        private static void SwapUpperAndLower(Span<nuint> bits, int lowerLength)
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

                Span<nuint> remaining = bits;

                // Each vector load needs one extra element below it to source the carry bits
                // that shift in from the lower limbs, hence the +1 minimum.
                while (Vector512.IsHardwareAccelerated && remaining.Length >= Vector512<nuint>.Count + 1)
                {
                    int offset = remaining.Length - Vector512<nuint>.Count;
                    Vector512<nuint> current = Vector512.Create(remaining.Slice(offset)) << shift;
                    Vector512<nuint> carries = Vector512.Create(remaining.Slice(offset - 1)) >> back;

                    Vector512<nuint> newValue = current | carries;

                    newValue.CopyTo(remaining.Slice(offset));
                    remaining = remaining.Slice(0, offset);
                }

                while (Vector256.IsHardwareAccelerated && remaining.Length >= Vector256<nuint>.Count + 1)
                {
                    int offset = remaining.Length - Vector256<nuint>.Count;
                    Vector256<nuint> current = Vector256.Create(remaining.Slice(offset)) << shift;
                    Vector256<nuint> carries = Vector256.Create(remaining.Slice(offset - 1)) >> back;

                    Vector256<nuint> newValue = current | carries;

                    newValue.CopyTo(remaining.Slice(offset));
                    remaining = remaining.Slice(0, offset);
                }

                while (Vector128.IsHardwareAccelerated && remaining.Length >= Vector128<nuint>.Count + 1)
                {
                    int offset = remaining.Length - Vector128<nuint>.Count;
                    Vector128<nuint> current = Vector128.Create(remaining.Slice(offset)) << shift;
                    Vector128<nuint> carries = Vector128.Create(remaining.Slice(offset - 1)) >> back;

                    Vector128<nuint> newValue = current | carries;

                    newValue.CopyTo(remaining.Slice(offset));
                    remaining = remaining.Slice(0, offset);
                }

                nuint carry2 = 0;
                for (int i = 0; i < remaining.Length; i++)
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

                Span<nuint> remaining = bits;

                while (Vector512.IsHardwareAccelerated && remaining.Length >= Vector512<nuint>.Count + 1)
                {
                    Vector512<nuint> current = Vector512.Create(remaining) >> shift;
                    Vector512<nuint> carries = Vector512.Create(remaining.Slice(1)) << back;

                    Vector512<nuint> newValue = current | carries;

                    newValue.CopyTo(remaining);
                    remaining = remaining.Slice(Vector512<nuint>.Count);
                }

                while (Vector256.IsHardwareAccelerated && remaining.Length >= Vector256<nuint>.Count + 1)
                {
                    Vector256<nuint> current = Vector256.Create(remaining) >> shift;
                    Vector256<nuint> carries = Vector256.Create(remaining.Slice(1)) << back;

                    Vector256<nuint> newValue = current | carries;

                    newValue.CopyTo(remaining);
                    remaining = remaining.Slice(Vector256<nuint>.Count);
                }

                while (Vector128.IsHardwareAccelerated && remaining.Length >= Vector128<nuint>.Count + 1)
                {
                    Vector128<nuint> current = Vector128.Create(remaining) >> shift;
                    Vector128<nuint> carries = Vector128.Create(remaining.Slice(1)) << back;

                    Vector128<nuint> newValue = current | carries;

                    newValue.CopyTo(remaining);
                    remaining = remaining.Slice(Vector128<nuint>.Count);
                }

                nuint carry2 = 0;
                int offset = bits.Length - remaining.Length;
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
