// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Aes = System.Runtime.Intrinsics.Arm.Aes;

namespace System.IO.Hashing
{
    // Helpers which provide equivalent intrinsics for Intel and ARM architectures. Should only be used
    // if the intrinsics are available.
    internal static class VectorHelper
    {
        // Pclmulqdq implies support for SSE2
        public static bool IsSupported => Pclmulqdq.IsSupported || (Aes.IsSupported && AdvSimd.IsSupported);

        // Performs carryless multiplication of the upper pairs of source and constants and the lower pairs of source and constants,
        // then folds them into target using carryless addition.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> FoldPolynomialPair(Vector128<ulong> target, Vector128<ulong> source, Vector128<ulong> constants)
        {
            target ^= CarrylessMultiplyUpper(source, constants);
            target ^= CarrylessMultiplyLower(source, constants);

            return target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> CarrylessMultiplyLower(Vector128<ulong> left, Vector128<ulong> right)
        {
            if (Pclmulqdq.IsSupported)
            {
                return Pclmulqdq.CarrylessMultiply(left, right, 0x00);
            }

            if (Aes.IsSupported)
            {
                return Aes.PolynomialMultiplyWideningLower(left.GetLower(), right.GetLower());
            }

            ThrowHelper.ThrowUnreachableException();
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> CarrylessMultiplyUpper(Vector128<ulong> left, Vector128<ulong> right)
        {
            if (Pclmulqdq.IsSupported)
            {
                return Pclmulqdq.CarrylessMultiply(left, right, 0x11);
            }

            if (Aes.IsSupported)
            {
                return Aes.PolynomialMultiplyWideningUpper(left, right);
            }

            ThrowHelper.ThrowUnreachableException();
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> CarrylessMultiplyLeftUpperRightLower(Vector128<ulong> left, Vector128<ulong> right)
        {
            if (Pclmulqdq.IsSupported)
            {
                return Pclmulqdq.CarrylessMultiply(left, right, 0x01);
            }

            if (Aes.IsSupported)
            {
                return Aes.PolynomialMultiplyWideningLower(left.GetUpper(), right.GetLower());
            }

            ThrowHelper.ThrowUnreachableException();
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> CarrylessMultiplyLeftLowerRightUpper(Vector128<ulong> left, Vector128<ulong> right)
        {
            if (Pclmulqdq.IsSupported)
            {
                return Pclmulqdq.CarrylessMultiply(left, right, 0x10);
            }

            if (Aes.IsSupported)
            {
                return Aes.PolynomialMultiplyWideningLower(left.GetLower(), right.GetUpper());
            }

            ThrowHelper.ThrowUnreachableException();
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> ShiftRightBytesInVector(Vector128<ulong> operand,
            [ConstantExpected(Max = (byte)15)] byte numBytesToShift)
        {
            if (Sse2.IsSupported)
            {
                return Sse2.ShiftRightLogical128BitLane(operand, numBytesToShift);
            }

            if (AdvSimd.IsSupported)
            {
                return AdvSimd.ExtractVector128(operand.AsByte(), Vector128<byte>.Zero, numBytesToShift).AsUInt64();
            }

            ThrowHelper.ThrowUnreachableException();
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> ShiftLowerToUpper(Vector128<ulong> operand)
        {
            if (Sse2.IsSupported)
            {
                return Sse2.ShiftLeftLogical128BitLane(operand, 8);
            }

            if (AdvSimd.IsSupported)
            {
                return AdvSimd.ExtractVector128(Vector128<byte>.Zero, operand.AsByte(), 8).AsUInt64();
            }

            ThrowHelper.ThrowUnreachableException();
            return default;
        }
    }
}
