// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Buffers;
using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    internal readonly struct DwarfFde
    {
        public readonly DwarfCie Cie;
        public readonly byte[] Instructions;
        public readonly string PcStartSymbolName;
        public readonly long PcStartSymbolOffset;
        public readonly ulong PcLength;
        public readonly string LsdaSymbolName;
        public readonly string PersonalitySymbolName;

        public DwarfFde(
            DwarfCie cie,
            byte[] blobData,
            string pcStartSymbolName,
            long pcStartSymbolOffset,
            ulong pcLength,
            string lsdaSymbolName,
            string personalitySymbolName)
        {
            Cie = cie;
            Instructions = CfiCodeToInstructions(cie, blobData);
            PcStartSymbolName = pcStartSymbolName;
            PcStartSymbolOffset = pcStartSymbolOffset;
            PcLength = pcLength;
            LsdaSymbolName = lsdaSymbolName;
            PersonalitySymbolName = personalitySymbolName;
        }

        private enum CFI_OPCODE
        {
            CFI_ADJUST_CFA_OFFSET,    // Offset is adjusted relative to the current one.
            CFI_DEF_CFA_REGISTER,     // New register is used to compute CFA
            CFI_REL_OFFSET,           // Register is saved at offset from the current CFA
            CFI_DEF_CFA               // Take address from register and add offset to it.
        }

        /// <summary>
        /// Convert JIT version of CFI blob into the the DWARF byte code form.
        /// </summary>
        private static byte[] CfiCodeToInstructions(DwarfCie cie, byte[] blobData)
        {
            int cfaOffset = cie.InitialCFAOffset;
            var cfiCode = ArrayPool<byte>.Shared.Rent(4096);
            int cfiCodeOffset = 0;
            byte codeOffset = 0;
            byte lastCodeOffset = 0;
            int offset = 0;
            while (offset < blobData.Length)
            {
                codeOffset = Math.Max(codeOffset, blobData[offset++]);
                CFI_OPCODE opcode = (CFI_OPCODE)blobData[offset++];
                short dwarfReg = BitConverter.ToInt16(blobData, offset);
                offset += sizeof(short);
                int cfiOffset = BitConverter.ToInt32(blobData, offset);
                offset += sizeof(int);

                if (codeOffset != lastCodeOffset)
                {
                    // Advance
                    int diff = (int)((codeOffset - lastCodeOffset) / cie.CodeAlignFactor);
                    if (diff <= 0x3F)
                    {
                        cfiCode[cfiCodeOffset++] = (byte)(DW_CFA_advance_loc | diff);
                    }
                    else
                    {
                        Debug.Assert(diff <= 0xFF);
                        cfiCode[cfiCodeOffset++] = DW_CFA_advance_loc1;
                        cfiCode[cfiCodeOffset++] = (byte)diff;
                    }
                    lastCodeOffset = codeOffset;
                }

                switch (opcode)
                {
                    case CFI_OPCODE.CFI_DEF_CFA_REGISTER:
                        cfiCode[cfiCodeOffset++] = DW_CFA_def_cfa_register;
                        cfiCode[cfiCodeOffset++] = (byte)dwarfReg;
                        break;

                    case CFI_OPCODE.CFI_REL_OFFSET:
                        int absOffset = ((cfiOffset - cfaOffset) / cie.DataAlignFactor);
                        if (absOffset < 0)
                        {
                            cfiCode[cfiCodeOffset++] = DW_CFA_offset_extended_sf;
                            cfiCodeOffset += DwarfHelper.WriteULEB128(cfiCode.AsSpan(cfiCodeOffset), (uint)dwarfReg);
                            cfiCodeOffset += DwarfHelper.WriteSLEB128(cfiCode.AsSpan(cfiCodeOffset), absOffset);
                        }
                        else if (dwarfReg <= 0x3F)
                        {
                            cfiCode[cfiCodeOffset++] = (byte)(DW_CFA_offset | (byte)dwarfReg);
                            cfiCodeOffset += DwarfHelper.WriteULEB128(cfiCode.AsSpan(cfiCodeOffset), (uint)absOffset);
                        }
                        else
                        {
                            cfiCode[cfiCodeOffset++] = DW_CFA_offset_extended;
                            cfiCodeOffset += DwarfHelper.WriteULEB128(cfiCode.AsSpan(cfiCodeOffset), (uint)dwarfReg);
                            cfiCodeOffset += DwarfHelper.WriteULEB128(cfiCode.AsSpan(cfiCodeOffset), (uint)absOffset);
                        }
                        break;

                    case CFI_OPCODE.CFI_ADJUST_CFA_OFFSET:
                        cfiCode[cfiCodeOffset++] = DW_CFA_def_cfa_offset;
                        cfaOffset += cfiOffset;
                        cfiCodeOffset += DwarfHelper.WriteULEB128(cfiCode.AsSpan(cfiCodeOffset), (uint)cfaOffset);
                        break;

                    case CFI_OPCODE.CFI_DEF_CFA:
                        cfiCode[cfiCodeOffset++] = DW_CFA_def_cfa;
                        cfiCode[cfiCodeOffset++] = (byte)dwarfReg;
                        cfaOffset = cfiOffset;
                        cfiCodeOffset += DwarfHelper.WriteULEB128(cfiCode.AsSpan(cfiCodeOffset), (uint)cfaOffset);
                        break;
                }
            }

            var result = cfiCode[0..cfiCodeOffset];
            ArrayPool<byte>.Shared.Return(cfiCode);
            return result;
        }
    }
}
