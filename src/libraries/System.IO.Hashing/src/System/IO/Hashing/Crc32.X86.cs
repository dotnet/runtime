// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;

namespace System.IO.Hashing
{
    public partial class Crc32
    {
        private const int X86MinBufferSize = 64;

        // Processes the bytes in source in X86MinBufferSize chunks using x86 intrinsics. After completion source is updated
        // to refer to any remaining bytes (at most X86MinBufferSize-1). Requires support for Sse2 and Pclmulqdq intrinsics.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint UpdateX86(uint crc, ref ReadOnlySpan<byte> source)
        {
            if (source.Length < X86MinBufferSize)
            {
                return crc;
            }

            // There's at least one block of 64.
            Vector128<ulong> x1 = Vector128.Create(MemoryMarshal.Cast<byte, ulong>(source));
            Vector128<ulong> x2 = Vector128.Create(MemoryMarshal.Cast<byte, ulong>(source.Slice(16)));
            Vector128<ulong> x3 = Vector128.Create(MemoryMarshal.Cast<byte, ulong>(source.Slice(32)));
            Vector128<ulong> x4 = Vector128.Create(MemoryMarshal.Cast<byte, ulong>(source.Slice(48)));
            Vector128<ulong> x5;

            x1 = Vector128.Xor(x1, Vector128.CreateScalar((ulong) crc));
            Vector128<ulong> x0 = Vector128.Create(0x0154442bd4, 0x01c6e41596).AsUInt64(); // k1, k2

            source = source.Slice(64);

            // Parallel fold blocks of 64, if any.
            while (source.Length >= 64)
            {
                x5 = Pclmulqdq.CarrylessMultiply(x1, x0, 0x00);
                Vector128<ulong> x6 = Pclmulqdq.CarrylessMultiply(x2, x0, 0x00);
                Vector128<ulong> x7 = Pclmulqdq.CarrylessMultiply(x3, x0, 0x00);
                Vector128<ulong> x8 = Pclmulqdq.CarrylessMultiply(x4, x0, 0x00);

                x1 = Pclmulqdq.CarrylessMultiply(x1, x0, 0x11);
                x2 = Pclmulqdq.CarrylessMultiply(x2, x0, 0x11);
                x3 = Pclmulqdq.CarrylessMultiply(x3, x0, 0x11);
                x4 = Pclmulqdq.CarrylessMultiply(x4, x0, 0x11);

                Vector128<ulong> y5 = Vector128.Create(MemoryMarshal.Cast<byte, ulong>(source));
                Vector128<ulong> y6 = Vector128.Create(MemoryMarshal.Cast<byte, ulong>(source.Slice(16)));
                Vector128<ulong> y7 = Vector128.Create(MemoryMarshal.Cast<byte, ulong>(source.Slice(32)));
                Vector128<ulong> y8 = Vector128.Create(MemoryMarshal.Cast<byte, ulong>(source.Slice(48)));

                x1 = Vector128.Xor(x1, x5);
                x2 = Vector128.Xor(x2, x6);
                x3 = Vector128.Xor(x3, x7);
                x4 = Vector128.Xor(x4, x8);

                x1 = Vector128.Xor(x1, y5);
                x2 = Vector128.Xor(x2, y6);
                x3 = Vector128.Xor(x3, y7);
                x4 = Vector128.Xor(x4, y8);

                source = source.Slice(64);
            }

            // Fold into 128-bits.
            x0 = Vector128.Create(0x01751997d0, 0x00ccaa009e).AsUInt64(); // k3, k4

            x5 = Pclmulqdq.CarrylessMultiply(x1, x0, 0x00);
            x1 = Pclmulqdq.CarrylessMultiply(x1, x0, 0x11);
            x1 = Vector128.Xor(x1, x2);
            x1 = Vector128.Xor(x1, x5);

            x5 = Pclmulqdq.CarrylessMultiply(x1, x0, 0x00);
            x1 = Pclmulqdq.CarrylessMultiply(x1, x0, 0x11);
            x1 = Vector128.Xor(x1, x3);
            x1 = Vector128.Xor(x1, x5);

            x5 = Pclmulqdq.CarrylessMultiply(x1, x0, 0x00);
            x1 = Pclmulqdq.CarrylessMultiply(x1, x0, 0x11);
            x1 = Vector128.Xor(x1, x4);
            x1 = Vector128.Xor(x1, x5);

            // Single fold blocks of 16, if any.
            while (source.Length >= 16)
            {
                x2 = Vector128.Create(MemoryMarshal.Cast<byte, ulong>(source));

                x5 = Pclmulqdq.CarrylessMultiply(x1, x0, 0x00);
                x1 = Pclmulqdq.CarrylessMultiply(x1, x0, 0x11);
                x1 = Vector128.Xor(x1, x2);
                x1 = Vector128.Xor(x1, x5);

                source = source.Slice(16);
            }

            // Fold 128 - bits to 64 - bits.
            x2 = Pclmulqdq.CarrylessMultiply(x1, x0, 0x10);
            x3 = Vector128.Create(~0, 0, ~0, 0).AsUInt64();
            x1 = Sse2.ShiftRightLogical128BitLane(x1, 8);
            x1 = Vector128.Xor(x1, x2);

            x0 = Vector128.CreateScalar(0x0163cd6124).AsUInt64(); // k5, k0

            x2 = Sse2.ShiftRightLogical128BitLane(x1, 4);
            x1 = Vector128.BitwiseAnd(x1, x3);
            x1 = Pclmulqdq.CarrylessMultiply(x1, x0, 0x00);
            x1 = Vector128.Xor(x1, x2);

            // Reduce to 32-bits.
            x0 = Vector128.Create(0x01db710641, 0x01f7011641).AsUInt64(); // polynomial

            x2 = Vector128.BitwiseAnd(x1, x3);
            x2 = Pclmulqdq.CarrylessMultiply(x2, x0, 0x10);
            x2 = Vector128.BitwiseAnd(x2, x3);
            x2 = Pclmulqdq.CarrylessMultiply(x2, x0, 0x00);
            x1 = Vector128.Xor(x1, x2);

            return x1.AsUInt32().GetElement(1);
        }
    }
}
