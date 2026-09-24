// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    // Decodes the GCInfo v5 fields needed by the current Wasm folding policy. This follows
    // GcInfoDecoder/GcSlotDecoder and the Wasm constants in gcinfotypes.h.
    internal static class WasmGcInfo
    {
        private const uint HasSecurityObject = 0x2;
        private const uint HasGsCookie = 0x4;
        private const uint HasPspSym = 0x8;
        private const uint HasGenericsContextMask = 0x30;
        private const uint HasStackBaseRegister = 0x40;
        private const uint HasEditAndContinueInfo = 0x100;
        private const uint HasReversePInvokeFrame = 0x200;

        public static bool HasNoSafePointsInterruptibleRangesOrGcSlots(ReadOnlySpan<byte> gcInfo)
        {
            var reader = new BitReader(gcInfo);
            bool isSlimHeader = reader.ReadBits(1) == 0;
            uint headerFlags = isSlimHeader
                ? (reader.ReadBits(1) != 0 ? HasStackBaseRegister : 0)
                : reader.ReadBits(10);

            // Decode all fields preceding the transition and slot counts. Code length and stack-base
            // register are intentionally not eligibility constraints.
            reader.DecodeVarLengthUnsigned(6);

            if ((headerFlags & HasGsCookie) != 0)
            {
                reader.DecodeVarLengthUnsigned(4);
                reader.DecodeVarLengthUnsigned(3);
            }
            else if ((headerFlags & (HasSecurityObject | HasGenericsContextMask)) != 0)
            {
                reader.DecodeVarLengthUnsigned(4);
            }

            if ((headerFlags & HasSecurityObject) != 0)
            {
                reader.DecodeVarLengthSigned(6);
            }
            if ((headerFlags & HasGsCookie) != 0)
            {
                reader.DecodeVarLengthSigned(6);
            }
            if ((headerFlags & HasPspSym) != 0)
            {
                reader.DecodeVarLengthSigned(6);
            }
            if ((headerFlags & HasGenericsContextMask) != 0)
            {
                reader.DecodeVarLengthSigned(6);
            }
            if ((headerFlags & HasStackBaseRegister) != 0 && !isSlimHeader)
            {
                reader.DecodeVarLengthUnsigned(3);
            }
            if ((headerFlags & HasEditAndContinueInfo) != 0)
            {
                reader.DecodeVarLengthUnsigned(3);
            }
            if ((headerFlags & HasReversePInvokeFrame) != 0)
            {
                reader.DecodeVarLengthSigned(6);
            }

            uint safePointCount = reader.DecodeVarLengthUnsigned(4);
            uint interruptibleRangeCount = isSlimHeader ? 0 : reader.DecodeVarLengthUnsigned(1);
            if (safePointCount != 0 || interruptibleRangeCount != 0)
            {
                return false;
            }

            uint registerCount = reader.ReadBits(1) != 0
                ? reader.DecodeVarLengthUnsigned(3)
                : 0;
            uint stackSlotCount = 0;
            uint untrackedSlotCount = 0;
            if (reader.ReadBits(1) != 0)
            {
                stackSlotCount = reader.DecodeVarLengthUnsigned(5);
                untrackedSlotCount = reader.DecodeVarLengthUnsigned(5);
            }

            return registerCount == 0 && stackSlotCount == 0 && untrackedSlotCount == 0;
        }

        private ref struct BitReader
        {
            private readonly ReadOnlySpan<byte> _data;
            private int _bitOffset;

            public BitReader(ReadOnlySpan<byte> data)
            {
                _data = data;
            }

            public uint ReadBits(int bitCount)
            {
                if ((uint)bitCount > 32 || (uint)_bitOffset + (uint)bitCount > (uint)_data.Length * 8)
                {
                    throw new BadImageFormatException("Invalid Wasm GC information.");
                }

                uint value = 0;
                for (int bit = 0; bit < bitCount; bit++)
                {
                    value |= (uint)((_data[_bitOffset >> 3] >> (_bitOffset & 7)) & 1) << bit;
                    _bitOffset++;
                }

                return value;
            }

            public uint DecodeVarLengthUnsigned(int chunkBitCount)
            {
                uint extensionBit = 1u << chunkBitCount;
                uint valueMask = extensionBit - 1;
                uint value = 0;

                for (int shift = 0; ; shift += chunkBitCount)
                {
                    uint chunk = ReadBits(chunkBitCount + 1);
                    value |= (chunk & valueMask) << shift;
                    if ((chunk & extensionBit) == 0)
                    {
                        return value;
                    }
                }
            }

            public int DecodeVarLengthSigned(int chunkBitCount)
            {
                uint extensionBit = 1u << chunkBitCount;
                uint valueMask = extensionBit - 1;
                uint value = 0;

                for (int shift = 0; ; shift += chunkBitCount)
                {
                    uint chunk = ReadBits(chunkBitCount + 1);
                    value |= (chunk & valueMask) << shift;
                    if ((chunk & extensionBit) == 0)
                    {
                        int valueBitCount = shift + chunkBitCount;
                        int signExtension = 32 - valueBitCount;
                        return (int)value << signExtension >> signExtension;
                    }
                }
            }
        }
    }
}
