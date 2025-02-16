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

            uint carry = 0;
            LeftShiftSelf(bits, smallShift, ref carry);
            bits[0] |= carry;

            if (digitShift == 0)
                return;

            SwapUpperAndLower(bits, bits.Length - digitShift);
        }

        public static void RotateRight(Span<uint> bits, int digitShift, int smallShift)
        {
            Debug.Assert(bits.Length > 0);

            uint carry = 0;
            RightShiftSelf(bits, smallShift, ref carry);
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

        public static void LeftShiftSelf(Span<uint> bits, int shift, ref uint carry)
        {
            Debug.Assert((uint)shift < 32);
            if (shift == 0)
                return;

            int back = 32 - shift;
            for (int i = 0; i < bits.Length; i++)
            {
                uint value = carry | bits[i] << shift;
                carry = bits[i] >> back;
                bits[i] = value;
            }
        }
        public static void RightShiftSelf(Span<uint> bits, int shift, ref uint carry)
        {
            Debug.Assert((uint)shift < 32);
            if (shift == 0)
                return;

            int back = 32 - shift;
            for (int i = bits.Length - 1; i >= 0; i--)
            {
                uint value = carry | bits[i] >> shift;
                carry = bits[i] << back;
                bits[i] = value;
            }
        }
    }
}
