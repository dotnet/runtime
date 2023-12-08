// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Buffers;
using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    public class DwarfFde
    {
        public DwarfCie Cie;
        public string PcStartSymbolName;
        public ulong PcLength;
        public string LsdaSymbolName;
        public string PersonalitySymbolName;
        public byte[] Instructions;

        public DwarfFde(DwarfCie cie, byte[] instructions)
        {
            Cie = cie;
            Instructions = instructions;
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
        public static byte[] CfiCodeToInstructions(DwarfCie cie, byte[] blobData)
        {
            int cfaOffset = cie.InitialCFAOffset;
            var cfiCode = ArrayPool<byte>.Shared.Rent(4096);
            int cfiCodeOffset = 0;
            byte codeOffset = 0;
            byte lastCodeOffset = 0;
            int offset = 0;
            uint temp;
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
                    if (diff <= 0x3f)
                    {
                        cfiCode[cfiCodeOffset++] = (byte)((byte)DW_CFA_advance_loc | diff);
                    }
                    else
                    {
                        Debug.Assert(diff <= 0xff);
                        cfiCode[cfiCodeOffset++] = (byte)DW_CFA_advance_loc1;
                        cfiCode[cfiCodeOffset++] = (byte)diff;
                    }
                    lastCodeOffset = codeOffset;
                }

                switch (opcode)
                {
                    case CFI_OPCODE.CFI_DEF_CFA_REGISTER:
                        cfiCode[cfiCodeOffset++] = (byte)DW_CFA_def_cfa_register;
                        cfiCode[cfiCodeOffset++] = (byte)dwarfReg;
                        break;

                    case CFI_OPCODE.CFI_REL_OFFSET:
                        if (dwarfReg <= 0x3f)
                        {
                            cfiCode[cfiCodeOffset++] = (byte)((byte)DW_CFA_offset | (byte)dwarfReg);
                        }
                        else
                        {
                            cfiCode[cfiCodeOffset++] = DW_CFA_offset_extended;
                            temp = (uint)dwarfReg;
                            do
                            {
                                cfiCode[cfiCodeOffset++] = (byte)((temp & 0x7f) | ((temp >= 0x80) ? 0x80u : 0));
                                temp >>= 7;
                            }
                            while (temp > 0);
                        }
                        temp = (uint)((cfiOffset - cfaOffset) / cie.DataAlignFactor);
                        do
                        {
                            cfiCode[cfiCodeOffset++] = (byte)((temp & 0x7f) | ((temp >= 0x80) ? 0x80u : 0));
                            temp >>= 7;
                        }
                        while (temp > 0);
                        break;

                    case CFI_OPCODE.CFI_ADJUST_CFA_OFFSET:
                        cfiCode[cfiCodeOffset++] = (byte)DW_CFA_def_cfa_offset;
                        cfaOffset += cfiOffset;
                        temp = (uint)(cfaOffset);
                        do
                        {
                            cfiCode[cfiCodeOffset++] = (byte)((temp & 0x7f) | ((temp >= 0x80) ? 0x80u : 0));
                            temp >>= 7;
                        }
                        while (temp > 0);
                        break;

                    case CFI_OPCODE.CFI_DEF_CFA:
                        cfiCode[cfiCodeOffset++] = (byte)DW_CFA_def_cfa;
                        cfiCode[cfiCodeOffset++] = (byte)dwarfReg;
                        cfaOffset = cfiOffset;
                        temp = (uint)(cfaOffset);
                        do
                        {
                            cfiCode[cfiCodeOffset++] = (byte)((temp & 0x7f) | ((temp >= 0x80) ? 0x80u : 0));
                            temp >>= 7;
                        }
                        while (temp > 0);
                        break;
                }
            }

            var result = cfiCode[0..cfiCodeOffset];
            ArrayPool<byte>.Shared.Return(cfiCode);
            return result;
        }
    }
}
