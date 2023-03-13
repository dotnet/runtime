// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;

namespace System.IO.Hashing
{
    public partial class Crc32
    {
        private const int X86BlockSize = 64;

        private const byte CarrylessMultiplyLower = 0x00;
        private const byte CarrylessMultiplyUpper = 0x11;
        private const byte CarrylessMultiplyLeftLowerRightUpper = 0x10;

        // Processes the bytes in source in X86BlockSize chunks using x86 intrinsics, followed by processing 16
        // byte chunks, and then processing remaining bytes individually. Requires support for Sse2 and Pclmulqdq intrinsics.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint UpdateX86(uint crc, ReadOnlySpan<byte> source)
        {
            if (source.Length < X86BlockSize)
            {
                return UpdateSlowPath(crc, source);
            }

            // Work with a reference to where we're at in the ReadOnlySpan and a local length
            // to avoid extraneous range checks.
            ref byte srcRef = ref MemoryMarshal.GetReference(source);
            int length = source.Length;

            Vector128<ulong> x1 = Vector128.LoadUnsafe(ref srcRef).AsUInt64();
            Vector128<ulong> x2 = Vector128.LoadUnsafe(ref srcRef, 16).AsUInt64();
            Vector128<ulong> x3 = Vector128.LoadUnsafe(ref srcRef, 32).AsUInt64();
            Vector128<ulong> x4 = Vector128.LoadUnsafe(ref srcRef, 48).AsUInt64();
            Vector128<ulong> x5;

            x1 ^= Vector128.CreateScalar((ulong)crc);
            Vector128<ulong> x0 = Vector128.Create(0x0154442bd4, 0x01c6e41596).AsUInt64(); // k1, k2

            srcRef = ref Unsafe.Add(ref srcRef, X86BlockSize);
            length -= X86BlockSize;

            // Parallel fold blocks of 64, if any.
            while (length >= X86BlockSize)
            {
                x5 = Pclmulqdq.CarrylessMultiply(x1, x0, CarrylessMultiplyLower);
                Vector128<ulong> x6 = Pclmulqdq.CarrylessMultiply(x2, x0, CarrylessMultiplyLower);
                Vector128<ulong> x7 = Pclmulqdq.CarrylessMultiply(x3, x0, CarrylessMultiplyLower);
                Vector128<ulong> x8 = Pclmulqdq.CarrylessMultiply(x4, x0, CarrylessMultiplyLower);

                x1 = Pclmulqdq.CarrylessMultiply(x1, x0, CarrylessMultiplyUpper);
                x2 = Pclmulqdq.CarrylessMultiply(x2, x0, CarrylessMultiplyUpper);
                x3 = Pclmulqdq.CarrylessMultiply(x3, x0, CarrylessMultiplyUpper);
                x4 = Pclmulqdq.CarrylessMultiply(x4, x0, CarrylessMultiplyUpper);

                Vector128<ulong> y5 = Vector128.LoadUnsafe(ref srcRef).AsUInt64();
                Vector128<ulong> y6 = Vector128.LoadUnsafe(ref srcRef, 16).AsUInt64();
                Vector128<ulong> y7 = Vector128.LoadUnsafe(ref srcRef, 32).AsUInt64();
                Vector128<ulong> y8 = Vector128.LoadUnsafe(ref srcRef, 48).AsUInt64();

                x1 ^= x5;
                x2 ^= x6;
                x3 ^= x7;
                x4 ^= x8;

                x1 ^= y5;
                x2 ^= y6;
                x3 ^= y7;
                x4 ^= y8;

                srcRef = ref Unsafe.Add(ref srcRef, X86BlockSize);
                length -= X86BlockSize;
            }

            // Fold into 128-bits.
            x0 = Vector128.Create(0x01751997d0, 0x00ccaa009e).AsUInt64(); // k3, k4

            x5 = Pclmulqdq.CarrylessMultiply(x1, x0, CarrylessMultiplyLower);
            x1 = Pclmulqdq.CarrylessMultiply(x1, x0, CarrylessMultiplyUpper);
            x1 ^= x2;
            x1 ^= x5;

            x5 = Pclmulqdq.CarrylessMultiply(x1, x0, CarrylessMultiplyLower);
            x1 = Pclmulqdq.CarrylessMultiply(x1, x0, CarrylessMultiplyUpper);
            x1 ^= x3;
            x1 ^= x5;

            x5 = Pclmulqdq.CarrylessMultiply(x1, x0, CarrylessMultiplyLower);
            x1 = Pclmulqdq.CarrylessMultiply(x1, x0, CarrylessMultiplyUpper);
            x1 ^= x4;
            x1 ^= x5;

            // Single fold blocks of 16, if any.
            while (length >= 16)
            {
                x2 = Vector128.LoadUnsafe(ref srcRef).AsUInt64();

                x5 = Pclmulqdq.CarrylessMultiply(x1, x0, CarrylessMultiplyLower);
                x1 = Pclmulqdq.CarrylessMultiply(x1, x0, CarrylessMultiplyUpper);
                x1 ^= x2;
                x1 ^= x5;

                srcRef = ref Unsafe.Add(ref srcRef, 16);
                length -= 16;
            }

            // Fold 128 bits to 64 bits.
            x2 = Pclmulqdq.CarrylessMultiply(x1, x0, CarrylessMultiplyLeftLowerRightUpper);
            x3 = Vector128.Create(~0, 0, ~0, 0).AsUInt64();
            x1 = Sse2.ShiftRightLogical128BitLane(x1, 8);
            x1 ^= x2;

            x0 = Vector128.CreateScalar(0x0163cd6124).AsUInt64(); // k5, k0

            x2 = Sse2.ShiftRightLogical128BitLane(x1, 4);
            x1 &= x3;
            x1 = Pclmulqdq.CarrylessMultiply(x1, x0, CarrylessMultiplyLower);
            x1 ^= x2;

            // Reduce to 32 bits.
            x0 = Vector128.Create(0x01db710641, 0x01f7011641).AsUInt64(); // polynomial

            x2 = x1 & x3;
            x2 = Pclmulqdq.CarrylessMultiply(x2, x0, CarrylessMultiplyLeftLowerRightUpper);
            x2 &= x3;
            x2 = Pclmulqdq.CarrylessMultiply(x2, x0, CarrylessMultiplyLower);
            x1 ^= x2;

            // Process the remaining bytes, if any
            return length > 0
                ? UpdateSlowPath(x1.AsUInt32().GetElement(1), MemoryMarshal.CreateReadOnlySpan(ref srcRef, length))
                : x1.AsUInt32().GetElement(1);
        }
    }
}
