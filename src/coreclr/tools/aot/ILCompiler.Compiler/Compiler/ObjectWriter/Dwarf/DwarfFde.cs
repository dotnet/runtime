// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Buffers;
using System.IO;
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

        /// <summary>
        /// Convert JIT version of CFI blob into the the DWARF byte code form.
        /// </summary>
        internal static byte[] CfiCodeToInstructions(DwarfCie cie, byte[] blobData)
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

        // Get the CFI data in the same shape as clang/LLVM generated one. This improves the compatibility with libunwind and other unwind solutions
        // - Combine in one single block for the whole prolog instead of one CFI block per assembler instruction
        // - Store CFA definition first
        // - Store all used registers in ascending order
        internal static byte[] CompressARM64CFI(byte[] blobData)
        {
            if (blobData == null || blobData.Length == 0)
            {
                return blobData;
            }

            Debug.Assert(blobData.Length % 8 == 0);

            short spReg = -1;

            int codeOffset = 0;
            short cfaRegister = spReg;
            int cfaOffset = 0;
            int spOffset = 0;

            int[] registerOffset = new int[96];

            for (int i = 0; i < registerOffset.Length; i++)
            {
                registerOffset[i] = int.MinValue;
            }

            int offset = 0;
            while (offset < blobData.Length)
            {
                codeOffset = Math.Max(codeOffset, blobData[offset++]);
                CFI_OPCODE opcode = (CFI_OPCODE)blobData[offset++];
                short dwarfReg = BitConverter.ToInt16(blobData, offset);
                offset += sizeof(short);
                int cfiOffset = BitConverter.ToInt32(blobData, offset);
                offset += sizeof(int);

                switch (opcode)
                {
                    case CFI_OPCODE.CFI_DEF_CFA_REGISTER:
                        cfaRegister = dwarfReg;

                        if (spOffset != 0)
                        {
                            for (int i = 0; i < registerOffset.Length; i++)
                            {
                                if (registerOffset[i] != int.MinValue)
                                {
                                    registerOffset[i] -= spOffset;
                                }
                            }

                            cfaOffset += spOffset;
                            spOffset = 0;
                        }

                        break;

                    case CFI_OPCODE.CFI_REL_OFFSET:
                        Debug.Assert(cfaRegister == spReg);
                        registerOffset[dwarfReg] = cfiOffset;
                        break;

                    case CFI_OPCODE.CFI_ADJUST_CFA_OFFSET:
                        if (cfaRegister != spReg)
                        {
                            cfaOffset += cfiOffset;
                        }
                        else
                        {
                            spOffset += cfiOffset;

                            for (int i = 0; i < registerOffset.Length; i++)
                            {
                                if (registerOffset[i] != int.MinValue)
                                {
                                    registerOffset[i] += cfiOffset;
                                }
                            }
                        }
                        break;
                }
            }

            using (MemoryStream cfiStream = new MemoryStream())
            {
                int storeOffset = 0;

                using (BinaryWriter cfiWriter = new BinaryWriter(cfiStream))
                {
                    if (cfaRegister != -1)
                    {
                        cfiWriter.Write((byte)codeOffset);
                        cfiWriter.Write(cfaOffset != 0 ? (byte)CFI_OPCODE.CFI_DEF_CFA : (byte)CFI_OPCODE.CFI_DEF_CFA_REGISTER);
                        cfiWriter.Write(cfaRegister);
                        cfiWriter.Write(cfaOffset);
                        storeOffset = cfaOffset;
                    }
                    else
                    {
                        if (cfaOffset != 0)
                        {
                            cfiWriter.Write((byte)codeOffset);
                            cfiWriter.Write((byte)CFI_OPCODE.CFI_ADJUST_CFA_OFFSET);
                            cfiWriter.Write((short)-1);
                            cfiWriter.Write(cfaOffset);
                        }

                        if (spOffset != 0)
                        {
                            cfiWriter.Write((byte)codeOffset);
                            cfiWriter.Write((byte)CFI_OPCODE.CFI_DEF_CFA);
                            cfiWriter.Write((short)31);
                            cfiWriter.Write(spOffset);
                        }
                    }

                    for (int i = registerOffset.Length - 1; i >= 0; i--)
                    {
                        if (registerOffset[i] != int.MinValue)
                        {
                            cfiWriter.Write((byte)codeOffset);
                            cfiWriter.Write((byte)CFI_OPCODE.CFI_REL_OFFSET);
                            cfiWriter.Write((short)i);
                            cfiWriter.Write(registerOffset[i] + storeOffset);
                        }
                    }
                }

                return cfiStream.ToArray();
            }
        }
    }
}
