// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ArmCrc = System.Runtime.Intrinsics.Arm.Crc32;

namespace System.IO.Hashing
{
    public partial class Crc32
    {
        private static uint UpdateScalarArm64(uint crc, ReadOnlySpan<byte> source)
        {
            Debug.Assert(ArmCrc.Arm64.IsSupported, "ARM CRC support is required.");

            // Compute in 8 byte chunks
            if (source.Length >= sizeof(ulong))
            {
                int longLength = source.Length & ~0x7; // Exclude trailing bytes not a multiple of 8
                foreach (ulong l in MemoryMarshal.Cast<byte, ulong>(source.Slice(0, longLength)))
                {
                    crc = ArmCrc.Arm64.ComputeCrc32(crc, l);
                }
                source = source.Slice(longLength);
            }

            // Compute remaining bytes
            foreach (byte b in source)
            {
                crc = ArmCrc.ComputeCrc32(crc, b);
            }

            return crc;
        }

        private static uint UpdateScalarArm32(uint crc, ReadOnlySpan<byte> source)
        {
            Debug.Assert(ArmCrc.IsSupported, "ARM CRC support is required.");

            // Compute in 4 byte chunks
            if (source.Length >= sizeof(uint))
            {
                int intLength = source.Length & ~0x3; // Exclude trailing bytes not a multiple of 4
                foreach (uint i in MemoryMarshal.Cast<byte, uint>(source.Slice(0, intLength)))
                {
                    crc = ArmCrc.ComputeCrc32(crc, i);
                }
                source = source.Slice(intLength);
            }

            // Compute remaining bytes
            foreach (byte b in source)
            {
                crc = ArmCrc.ComputeCrc32(crc, b);
            }

            return crc;
        }
    }
}
