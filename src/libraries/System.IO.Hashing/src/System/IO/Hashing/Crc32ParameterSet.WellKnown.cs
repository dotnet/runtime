// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO.Hashing
{
    public partial class Crc32ParameterSet
    {
        /// <summary>
        ///   Gets the parameter set for the variant of CRC-32 as used in
        ///   ITU-T V.42 and IEEE 802.3.
        /// </summary>
        /// <value>
        ///   The parameter set for the variant of CRC-32 as used in
        ///   ITU-T V.42 and IEEE 802.3.
        /// </value>
        public static Crc32ParameterSet Crc32 =>
            field ??= new Ieee8023ParameterSet();

        /// <summary>
        ///   Gets the parameter set for the CRC-32C variant of CRC-32.
        /// </summary>
        /// <value>
        ///   The parameter set for the CRC-32C variant of CRC-32.
        /// </value>
        public static Crc32ParameterSet Crc32C =>
            field ??= MakeCrc32CParameterSet();

        private static Crc32ParameterSet MakeCrc32CParameterSet()
        {
#if NET
            if (System.Runtime.Intrinsics.X86.Sse42.IsSupported || System.Runtime.Intrinsics.Arm.Crc32.IsSupported)
            {
                return new Crc32CParameterSet();
            }
#endif

            return Create(
                polynomial: 0x1edc6f41,
                initialValue: 0xffffffff,
                finalXorValue: 0xffffffff,
                reflectValues: true);
        }

        private sealed class Ieee8023ParameterSet : Crc32ParameterSet
        {
            public Ieee8023ParameterSet()
                : base(0x04c11db7, 0xffffffff, 0xffffffff, reflectValues: true)
            {
            }

            internal override uint Update(uint value, ReadOnlySpan<byte> source) => Hashing.Crc32.Update(value, source);
        }

#if NET
        private sealed class Crc32CParameterSet : Crc32ParameterSet
        {
            public Crc32CParameterSet()
                : base(0x1edc6f41, 0xffffffff, 0xffffffff, reflectValues: true)
            {
            }

            internal override uint Update(uint value, ReadOnlySpan<byte> source) => UpdateIntrinsic(value, source);

            private static uint UpdateIntrinsic(uint crc, ReadOnlySpan<byte> source)
            {
                if (System.Runtime.Intrinsics.X86.Sse42.IsSupported)
                {
                    if (System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported)
                    {
                        ReadOnlySpan<ulong> ulongData = MemoryMarshal.Cast<byte, ulong>(source);
                        ulong crc64 = crc;

                        foreach (ulong value in ulongData)
                        {
                            crc64 = System.Runtime.Intrinsics.X86.Sse42.X64.Crc32(crc64, value);
                        }

                        crc = (uint)crc64;
                        source = source.Slice(ulongData.Length * sizeof(ulong));
                    }

                    ReadOnlySpan<uint> uintData = MemoryMarshal.Cast<byte, uint>(source);

                    foreach (uint value in uintData)
                    {
                        crc = System.Runtime.Intrinsics.X86.Sse42.Crc32(crc, value);
                    }

                    // SSE 4.2 defines a ushort version as well, but that will only save us one byte,
                    // so not worth the branch and cast.

                    ReadOnlySpan<byte> remainingBytes = source.Slice(uintData.Length * sizeof(uint));

                    foreach (byte value in remainingBytes)
                    {
                        crc = System.Runtime.Intrinsics.X86.Sse42.Crc32(crc, value);
                    }
                }
                else
                {
                    Debug.Assert(System.Runtime.Intrinsics.Arm.Crc32.IsSupported);
                    ref byte ptr = ref MemoryMarshal.GetReference(source);
                    int offset = 0;

                    if (System.Runtime.Intrinsics.Arm.Crc32.Arm64.IsSupported)
                    {
                        int longLength = source.Length & ~0x7; // Exclude trailing bytes not a multiple of 8

                        for (; offset < longLength; offset += sizeof(ulong))
                        {
                            crc = System.Runtime.Intrinsics.Arm.Crc32.Arm64.ComputeCrc32C(
                                crc,
                                Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref ptr, offset)));
                        }
                    }

                    int intLength = source.Length & ~0x3; // Exclude trailing bytes not a multiple of 4

                    for (; offset < intLength; offset += sizeof(uint))
                    {
                        crc = System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32C(
                            crc,
                            Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref ptr, offset)));
                    }

                    ReadOnlySpan<byte> remainingBytes = source.Slice(offset);

                    foreach (byte value in remainingBytes)
                    {
                        crc = System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32C(crc, value);
                    }
                }

                return crc;
            }
        }
#endif
    }
}
