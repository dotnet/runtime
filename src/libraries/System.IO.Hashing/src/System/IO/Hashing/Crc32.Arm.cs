// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using ArmCrc = System.Runtime.Intrinsics.Arm.Crc32;

namespace System.IO.Hashing
{
    public partial class Crc32
    {
        private static uint UpdateScalarArm64(uint crc, ReadOnlySpan<byte> source)
        {
            Debug.Assert(ArmCrc.Arm64.IsSupported,
                "ARM CRC support is required.");

            // Compute in 8 byte chunks
            if (source.Length >= sizeof(ulong))
            {
                ReadOnlySpan<ulong> longSource = MemoryMarshal.Cast<byte, ulong>(source);
                for (int i = 0; i < longSource.Length; i++)
                {
                    crc = ArmCrc.Arm64.ComputeCrc32(crc, longSource[i]);
                }

                source = source.Slice(longSource.Length * sizeof(ulong));
            }

            // Compute remaining bytes
            for (int i = 0; i < source.Length; i++)
            {
                crc = ArmCrc.ComputeCrc32(crc, source[i]);
            }

            return crc;
        }

        private static uint UpdateScalarArm32(uint crc, ReadOnlySpan<byte> source)
        {
            Debug.Assert(ArmCrc.IsSupported,
                "ARM CRC support is required.");

            // Compute in 4 byte chunks
            if (source.Length >= sizeof(uint))
            {
                ReadOnlySpan<uint> intSource = MemoryMarshal.Cast<byte, uint>(source);
                for (int i = 0; i < intSource.Length; i++)
                {
                    crc = ArmCrc.ComputeCrc32(crc, intSource[i]);
                }

                source = source.Slice(intSource.Length * sizeof(uint));
            }

            // Compute remaining bytes
            for (int i = 0; i < source.Length; i++)
            {
                crc = ArmCrc.ComputeCrc32(crc, source[i]);
            }

            return crc;
        }
    }
}
