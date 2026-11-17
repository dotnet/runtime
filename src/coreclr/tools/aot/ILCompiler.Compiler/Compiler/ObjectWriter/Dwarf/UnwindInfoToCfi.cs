// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Internal.TypeSystem;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Converts the Windows style unwind information the JIT produces for a frame into the CFI codes the JIT reports
    /// for the same prolog operations, for the tools that read .debug_frame or the platform unwind information.
    /// The unwind information is the only description of managed frames, so this is the only source of CFI.
    /// </summary>
    internal static class UnwindInfoToCfi
    {
        public static byte[] Convert(TargetArchitecture architecture, TargetOS os, byte[] unwindInfo)
        {
            if (unwindInfo is null || unwindInfo.Length < 4)
                return [];

            var writer = new CfiWriter();
            switch (architecture)
            {
                case TargetArchitecture.X64:
                    ConvertX64(unwindInfo, writer);
                    break;
                case TargetArchitecture.ARM64:
                    ConvertArm64(unwindInfo, writer);
                    break;
                case TargetArchitecture.ARM:
                    ConvertArm(unwindInfo, writer);
                    break;
                case TargetArchitecture.LoongArch64:
                    ConvertLoongArch64OrRiscV64(unwindInfo, writer, fpRegister: 22, firstFloatRegister: 32 + 24, wideFloatIndex: false);
                    break;
                case TargetArchitecture.RiscV64:
                    ConvertLoongArch64OrRiscV64(unwindInfo, writer, fpRegister: 8, firstFloatRegister: 32 + 8, wideFloatIndex: true);
                    break;
                default:
                    throw new NotSupportedException(architecture.ToString());
            }

            byte[] cfi = writer.ToArray();

            // libunwind and other DWARF unwinders expect the arm64 prolog in the shape clang produces
            if (architecture == TargetArchitecture.ARM64 && os == TargetOS.Linux)
                cfi = CompressArm64(cfi);

            return cfi;
        }

        private sealed class CfiWriter
        {
            private readonly List<byte> _bytes = new();

            // CFA - SP, from the CIE, tracked the way DwarfFde tracks it
            public int CfaOffset;

            public void Add(int codeOffset, CFI_OPCODE opcode, int dwarfReg, int offset)
            {
                _bytes.Add(checked((byte)codeOffset));
                _bytes.Add((byte)opcode);
                _bytes.Add((byte)dwarfReg);
                _bytes.Add((byte)(dwarfReg >> 8));
                for (int b = 0; b < 32; b += 8)
                    _bytes.Add((byte)(offset >> b));
                if (opcode == CFI_OPCODE.CFI_ADJUST_CFA_OFFSET)
                    CfaOffset += offset;
            }

            public void Alloc(int codeOffset, int size) => Add(codeOffset, CFI_OPCODE.CFI_ADJUST_CFA_OFFSET, -1, size);

            public void Save(int codeOffset, int dwarfReg, int spOffset) => Add(codeOffset, CFI_OPCODE.CFI_REL_OFFSET, dwarfReg, spOffset);

            public void SetFrame(int codeOffset, int dwarfReg, int spOffset)
            {
                Add(codeOffset, CFI_OPCODE.CFI_DEF_CFA_REGISTER, dwarfReg, 0);
                if (spOffset != 0)
                    Alloc(codeOffset, -spOffset);
            }

            public byte[] ToArray() => _bytes.ToArray();
        }

        private static void ConvertX64(byte[] unwindInfo, CfiWriter writer)
        {
            // UWOP_* and the extra slots each of them uses, see win64unwind.h
            const int UWOP_PUSH_NONVOL = 0, UWOP_ALLOC_LARGE = 1, UWOP_ALLOC_SMALL = 2, UWOP_SET_FPREG = 3,
                UWOP_SAVE_NONVOL = 4, UWOP_SAVE_NONVOL_FAR = 5, UWOP_SAVE_XMM128 = 8, UWOP_SAVE_XMM128_FAR = 9,
                UWOP_SET_FPREG_LARGE = 11;
            ReadOnlySpan<byte> extraSlots = [0, 1, 0, 0, 1, 2, 1, 2, 1, 2, 0, 2];
            // DWARF numbers of RAX, RCX, RDX, RBX, RSP, RBP, RSI, RDI, R8-R15
            ReadOnlySpan<byte> dwarfRegisters = [0, 2, 1, 3, 7, 6, 4, 5, 8, 9, 10, 11, 12, 13, 14, 15];
            const int DwarfXmm0 = 17;

            int count = unwindInfo[2];
            int frameOffset = unwindInfo[3] >> 4;
            int Op(int index) => unwindInfo[4 + 2 * index + 1] & 0xF;
            int Info(int index) => unwindInfo[4 + 2 * index + 1] >> 4;
            int Slot(int index) => BinaryPrimitives.ReadUInt16LittleEndian(unwindInfo.AsSpan(4 + 2 * index));
            int Slot32(int index) => Slot(index) | (Slot(index + 1) << 16);

            // The codes are in reverse order of the prolog, and multi-slot codes keep their operands after them
            var ops = new List<int>();
            for (int i = 0; i < count; i += 1 + extraSlots[Op(i)] + (Op(i) == UWOP_ALLOC_LARGE && Info(i) != 0 ? 1 : 0))
                ops.Add(i);
            ops.Reverse();

            // Saves with a displacement are relative to the stack pointer at the end of the fixed allocation
            int frameSize = 0;
            foreach (int i in ops)
            {
                frameSize += Op(i) switch
                {
                    UWOP_PUSH_NONVOL => 8,
                    UWOP_ALLOC_SMALL => Info(i) * 8 + 8,
                    UWOP_ALLOC_LARGE => Info(i) == 0 ? Slot(i + 1) * 8 : Slot32(i + 1),
                    _ => 0,
                };
            }

            foreach (int i in ops)
            {
                int codeOffset = unwindInfo[4 + 2 * i];
                int info = Info(i);
                switch (Op(i))
                {
                    case UWOP_PUSH_NONVOL:
                        writer.Alloc(codeOffset, 8);
                        writer.Save(codeOffset, dwarfRegisters[info], 0);
                        break;
                    case UWOP_ALLOC_SMALL:
                        writer.Alloc(codeOffset, info * 8 + 8);
                        break;
                    case UWOP_ALLOC_LARGE:
                        writer.Alloc(codeOffset, info == 0 ? Slot(i + 1) * 8 : Slot32(i + 1));
                        break;
                    case UWOP_SET_FPREG:
                        writer.SetFrame(codeOffset, dwarfRegisters[unwindInfo[3] & 0xF], 16 * frameOffset);
                        break;
                    case UWOP_SET_FPREG_LARGE:
                        writer.SetFrame(codeOffset, dwarfRegisters[unwindInfo[3] & 0xF], 16 * Slot32(i + 1));
                        break;
                    case UWOP_SAVE_NONVOL:
                    case UWOP_SAVE_NONVOL_FAR:
                    case UWOP_SAVE_XMM128:
                    case UWOP_SAVE_XMM128_FAR:
                        bool far = Op(i) is UWOP_SAVE_NONVOL_FAR or UWOP_SAVE_XMM128_FAR;
                        bool xmm = Op(i) is UWOP_SAVE_XMM128 or UWOP_SAVE_XMM128_FAR;
                        int displacement = far ? Slot32(i + 1) : Slot(i + 1) * (xmm ? 16 : 8);
                        writer.Save(codeOffset, xmm ? DwarfXmm0 + info : dwarfRegisters[info], displacement - frameSize + writer.CfaOffset);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected unwind code {Op(i)}.");
                }
            }
        }

        /// <summary>
        /// Returns the unwind codes of the prolog of an arm64, loongarch64 or riscv64 style unwind information, whose
        /// header has the number of code words in bits 27-31 and the number of epilog scopes in bits 22-26, or of an
        /// arm one, with the number of code words in bits 28-31 and the number of epilog scopes in bits 23-27.
        /// </summary>
        private static ReadOnlySpan<byte> GetUnwindCodes(byte[] unwindInfo, bool arm)
        {
            uint header = BinaryPrimitives.ReadUInt32LittleEndian(unwindInfo);
            int codeWords = (int)(arm ? header >> 28 : header >> 27);
            int epilogCount = (int)((header >> (arm ? 23 : 22)) & 0x1F);
            bool singleEpilog = ((header >> 21) & 1) != 0;
            int offset = 4;
            if (codeWords == 0 && epilogCount == 0)
            {
                uint extension = BinaryPrimitives.ReadUInt32LittleEndian(unwindInfo.AsSpan(4));
                codeWords = (int)((extension >> 16) & 0xFF);
                epilogCount = (int)(extension & 0xFFFF);
                offset = 8;
            }

            if (!singleEpilog)
                offset += 4 * epilogCount;

            return unwindInfo.AsSpan(offset, 4 * codeWords);
        }

        private static void ConvertArm64(byte[] unwindInfo, CfiWriter writer)
        {
            const int DwarfFp = 29, DwarfLr = 30, DwarfD8 = 72;
            ReadOnlySpan<byte> codes = GetUnwindCodes(unwindInfo, arm: false);
            ReadOnlySpan<byte> sizeOfE0toFF = [4, 1, 2, 1, 1, 1, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 1, 1, 1, 1];

            // The prolog codes are in reverse order of the prolog instructions, one code per instruction
            var ops = new List<int>();
            for (int i = 0; i < codes.Length && (codes[i] & 0xFE) != 0xE4;)
            {
                ops.Add(i);
                byte b = codes[i];
                i += b < 0xC0 ? 1 : b < 0xE0 ? 2 : sizeOfE0toFF[b - 0xE0];
            }
            ops.Reverse();

            // The register pair that save_next continues from
            int lastPair = -1, lastPairOffset = 0;

            for (int k = 0; k < ops.Count; k++)
            {
                int codeOffset = 4 * (k + 1);
                int i = ops[k];
                int b1 = codes[i];
                int b2 = i + 1 < codes.Length ? codes[i + 1] : 0;
                int x, z;

                void SavePair(int first, int second, int spOffset)
                {
                    writer.Save(codeOffset, first, spOffset);
                    writer.Save(codeOffset, second, spOffset + 8);
                    lastPair = first;
                    lastPairOffset = spOffset;
                }

                if ((b1 & 0xE0) == 0x00)
                {
                    writer.Alloc(codeOffset, (b1 & 0x1F) * 16);
                }
                else if ((b1 & 0xE0) == 0x20)
                {
                    writer.Alloc(codeOffset, (b1 & 0x1F) * 8);
                    SavePair(19, 20, 0);
                }
                else if ((b1 & 0xC0) == 0x40)
                {
                    SavePair(DwarfFp, DwarfLr, (b1 & 0x3F) * 8);
                }
                else if ((b1 & 0xC0) == 0x80)
                {
                    writer.Alloc(codeOffset, ((b1 & 0x3F) + 1) * 8);
                    SavePair(DwarfFp, DwarfLr, 0);
                }
                else if ((b1 & 0xF8) == 0xC0)
                {
                    writer.Alloc(codeOffset, (((b1 & 0x7) << 8) | b2) * 16);
                }
                else if ((b1 & 0xF8) == 0xC8)
                {
                    // save_regp and save_regp_x
                    x = ((b1 & 0x3) << 2) | (b2 >> 6);
                    z = b2 & 0x3F;
                    bool preIndexed = (b1 & 0xFC) == 0xCC;
                    if (preIndexed)
                        writer.Alloc(codeOffset, (z + 1) * 8);
                    SavePair(19 + x, 20 + x, preIndexed ? 0 : z * 8);
                }
                else if ((b1 & 0xFC) == 0xD0)
                {
                    writer.Save(codeOffset, 19 + (((b1 & 0x3) << 2) | (b2 >> 6)), (b2 & 0x3F) * 8);
                }
                else if ((b1 & 0xFE) == 0xD4)
                {
                    writer.Alloc(codeOffset, ((b2 & 0x1F) + 1) * 8);
                    writer.Save(codeOffset, 19 + (((b1 & 0x1) << 3) | (b2 >> 5)), 0);
                }
                else if ((b1 & 0xFE) == 0xD6)
                {
                    x = ((b1 & 0x1) << 2) | (b2 >> 6);
                    writer.Save(codeOffset, 19 + 2 * x, (b2 & 0x3F) * 8);
                    writer.Save(codeOffset, DwarfLr, (b2 & 0x3F) * 8 + 8);
                }
                else if ((b1 & 0xFC) == 0xD8)
                {
                    // save_fregp and save_fregp_x
                    x = ((b1 & 0x1) << 2) | (b2 >> 6);
                    z = b2 & 0x3F;
                    bool preIndexed = (b1 & 0xFE) == 0xDA;
                    if (preIndexed)
                        writer.Alloc(codeOffset, (z + 1) * 8);
                    SavePair(DwarfD8 + x, DwarfD8 + x + 1, preIndexed ? 0 : z * 8);
                }
                else if ((b1 & 0xFE) == 0xDC)
                {
                    writer.Save(codeOffset, DwarfD8 + (((b1 & 0x1) << 2) | (b2 >> 6)), (b2 & 0x3F) * 8);
                }
                else if (b1 == 0xDE)
                {
                    writer.Alloc(codeOffset, ((b2 & 0x1F) + 1) * 8);
                    writer.Save(codeOffset, DwarfD8 + (b2 >> 5), 0);
                }
                else if (b1 == 0xE0)
                {
                    writer.Alloc(codeOffset, ((b2 << 16) | (codes[i + 2] << 8) | codes[i + 3]) * 16);
                }
                else if (b1 == 0xE1)
                {
                    writer.SetFrame(codeOffset, DwarfFp, 0);
                }
                else if (b1 == 0xE2)
                {
                    writer.SetFrame(codeOffset, DwarfFp, b2 * 8);
                }
                else if (b1 == 0xE6)
                {
                    if (lastPair < 0)
                        throw new InvalidOperationException("save_next without a preceding register pair.");
                    SavePair(lastPair + 2, lastPair + 3, lastPairOffset + 16);
                }
                else if (b1 == 0xF9)
                {
                    // save_regp_rev, a NativeAOT extension: the pair <reg + 1, reg> in that order
                    int first = ((b2 & 0x40) != 0 ? DwarfD8 : 19) + (b2 & 0x1F);
                    z = codes[i + 2] & 0x3F;
                    bool preIndexed = (b2 & 0x80) != 0;
                    if (preIndexed)
                        writer.Alloc(codeOffset, (z + 1) * 8);
                    writer.Save(codeOffset, first + 1, preIndexed ? 0 : z * 8);
                    writer.Save(codeOffset, first, (preIndexed ? 0 : z * 8) + 8);
                    lastPair = -1;
                }
                else if (b1 == 0xFC)
                {
                    writer.Add(codeOffset, CFI_OPCODE.CFI_NEGATE_RA_STATE, -1, 0);
                }
                else if (b1 != 0xE3)
                {
                    throw new InvalidOperationException($"Unexpected unwind code {b1:X2}.");
                }
            }
        }

        /// <summary>
        /// Returns the packed unwind data (the Flag = 1 form of the arm64 .pdata) that describes the same frame as the
        /// arm64 unwind information, when the unwinder expands the packed form back to exactly that unwind information.
        /// </summary>
        public static bool TryPackArm64(byte[] unwindInfo, out uint packed)
        {
            packed = 0;
            if (unwindInfo is null || unwindInfo.Length < 8)
                return false;

            uint header = BinaryPrimitives.ReadUInt32LittleEndian(unwindInfo);
            uint functionLength = header & 0x3FFFF;
            if (functionLength > 0x7FF || (header & (0x3u << 18 | 1u << 20)) != 0 || (header >> 27) == 0)
                return false;

            // The packed form has a single epilog at the end of the function, which the JIT describes with the E bit
            // when its codes are all the prolog codes, and otherwise with a single epilog scope
            if (((header >> 21) & 1) == 0)
            {
                if (((header >> 22) & 0x1F) != 1)
                    return false;

                uint scope = BinaryPrimitives.ReadUInt32LittleEndian(unwindInfo.AsSpan(4));
                uint index = scope >> 22;
                byte[] scopeCodes = unwindInfo.AsSpan(8).ToArray();
                if (index >= scopeCodes.Length || (scope & 0x3FFFF) + CountInstructionsArm64(scopeCodes.AsSpan((int)index)) + 1 != functionLength)
                    return false;

                // The same unwind information with the E bit
                header = (header & ~(0x1Fu << 22)) | (1u << 21) | (index << 22);
                var normalized = new byte[4 + scopeCodes.Length];
                BinaryPrimitives.WriteUInt32LittleEndian(normalized, header);
                scopeCodes.CopyTo(normalized, 4);
                unwindInfo = normalized;
            }

            // Derive the parameters from the prolog codes, then check that they expand to the same unwind information
            ReadOnlySpan<byte> codes = GetUnwindCodes(unwindInfo, arm: false);
            int intRegs = 0, floatRegs = 0, frameSize = 0, lastPair = 0;
            bool lrWithInts = false, chained = false, pac = false;
            for (int i = 0; i < codes.Length && (codes[i] & 0xFE) != 0xE4;)
            {
                int b1 = codes[i], b2 = i + 1 < codes.Length ? codes[i + 1] : 0;
                if (b1 < 0x20) { frameSize += (b1 & 0x1F) * 16; i++; }
                else if (b1 < 0x40) { frameSize += (b1 & 0x1F) * 8; intRegs += 2; lastPair = 0; i++; }
                else if (b1 < 0x80) { chained = true; i++; }
                else if (b1 < 0xC0) { frameSize += ((b1 & 0x3F) + 1) * 8; chained = true; i++; }
                else if (b1 < 0xC8) { frameSize += (((b1 & 0x7) << 8) | b2) * 16; i += 2; }
                else if (b1 < 0xCC) { intRegs += 2; lastPair = 0; i += 2; }
                else if (b1 < 0xD0) { frameSize += ((b2 & 0x3F) + 1) * 8; intRegs += 2; lastPair = 0; i += 2; }
                else if (b1 < 0xD4) { if ((((b1 & 0x3) << 2) | (b2 >> 6)) == 11) lrWithInts = true; else intRegs++; i += 2; }
                else if (b1 < 0xD6) { frameSize += ((b2 & 0x1F) + 1) * 8; intRegs++; i += 2; }
                else if (b1 < 0xD8) { intRegs++; lrWithInts = true; i += 2; }
                else if (b1 < 0xDA) { floatRegs += 2; lastPair = 1; i += 2; }
                else if (b1 < 0xDC) { frameSize += ((b2 & 0x3F) + 1) * 8; floatRegs += 2; lastPair = 1; i += 2; }
                else if (b1 < 0xDE) { floatRegs++; i += 2; }
                else if (b1 == 0xDE) { frameSize += ((b2 & 0x1F) + 1) * 8; floatRegs++; i += 2; }
                else if (b1 == 0xE0) { frameSize += ((b2 << 16) | (codes[i + 2] << 8) | codes[i + 3]) * 16; i += 4; }
                else if (b1 == 0xE1) { i++; }
                else if (b1 == 0xE6) { if (lastPair == 0) intRegs += 2; else floatRegs += 2; i++; }
                else if (b1 == 0xFC) { pac = true; i++; }
                else return false;
            }

            if (intRegs > 10 || floatRegs == 1 || floatRegs > 8 || frameSize % 16 != 0 || frameSize / 16 > 0x1FF)
                return false;

            uint cr = chained ? (pac ? 2u : 3u) : lrWithInts ? 1u : 0u;
            uint regF = floatRegs == 0 ? 0u : (uint)floatRegs - 1;
            packed = 1 | (functionLength << 2) | (regF << 13) | ((uint)intRegs << 16) | (cr << 21) | ((uint)frameSize / 16 << 23);

            // The codes after the end code are padding
            byte[] expanded = ExpandArm64Packed(packed);
            int used = Array.LastIndexOf(expanded, (byte)0xE4) + 1;
            return expanded.Length == unwindInfo.Length && expanded.AsSpan(0, used).SequenceEqual(unwindInfo.AsSpan(0, used));
        }

        /// <summary>
        /// Number of instructions the arm64 unwind codes up to the end code describe.
        /// </summary>
        private static int CountInstructionsArm64(ReadOnlySpan<byte> codes)
        {
            ReadOnlySpan<byte> sizeOfE0toFF = [4, 1, 2, 1, 1, 1, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 1, 1, 1, 1];
            ReadOnlySpan<byte> instructionsOfE0toFF = [1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1];
            int count = 0;
            for (int i = 0; i < codes.Length && (codes[i] & 0xFE) != 0xE4;)
            {
                byte b = codes[i];
                if (b < 0xE0)
                {
                    count++;
                    i += b < 0xC0 ? 1 : 2;
                }
                else
                {
                    count += instructionsOfE0toFF[b - 0xE0];
                    i += sizeOfE0toFF[b - 0xE0];
                }
            }
            return count;
        }

        /// <summary>
        /// Expands packed arm64 unwind data the way the arm64 unwinder does (RtlpExpandCompactToFull), for packed data
        /// without homed parameters.
        /// </summary>
        public static byte[] ExpandArm64Packed(uint packed)
        {
            const int CrUnchainedSavedLr = 1, CrChainedWithPac = 2, CrChained = 3;
            int functionLength = (int)((packed >> 2) & 0x7FF);
            int regF = (int)((packed >> 13) & 0x7);
            int regI = (int)((packed >> 16) & 0xF);
            int h = (int)((packed >> 20) & 0x1);
            int cr = (int)((packed >> 21) & 0x3);
            int famsz = (int)(packed >> 23) * 2;
            if (h != 0)
                throw new NotSupportedException("Packed unwind data with homed parameters.");

            int intsz = regI + (cr == CrUnchainedSavedLr ? 1 : 0);
            int fpsz = regF != 0 ? regF + 1 : 0;
            int savsz = (intsz + fpsz + 1) & ~1;
            int locsz = famsz - savsz;

            // The codes in prolog order, each in its final byte order
            var ops = new List<byte[]>();
            ops.Add([0xE4]);
            if (cr == CrChainedWithPac)
                ops.Add([0xFC]);

            bool predecDone = false;
            int slot = 0;
            void Op(byte[] op) => ops.Add(op);
            void Alloc(int size)
            {
                if (size >= 16384)
                    Op([0xE0, (byte)(size / 16 >> 16), (byte)(size / 16 >> 8), (byte)(size / 16)]);
                else if (size >= 512)
                    Op([(byte)(0xC0 | (size / 16 >> 8)), (byte)(size / 16)]);
                else
                    Op([(byte)(size / 16)]);
            }
            void Pair(int fixedBits, int xBits, int reg, int z) => Op(Encode16((fixedBits << (6 + xBits)) | (reg << 6) | z));
            static byte[] Encode16(int v) => [(byte)(v >> 8), (byte)v];

            if (intsz != 0)
            {
                if (regI == 1 && cr == CrUnchainedSavedLr)
                {
                    Alloc(savsz * 8);
                    predecDone = true;
                }

                int intreg = 0;
                for (; intreg < regI / 2 * 2; intreg += 2)
                {
                    if (!predecDone)
                    {
                        Pair(0x33, 4, intreg, savsz - 1);
                        predecDone = true;
                    }
                    else
                    {
                        Pair(0x32, 4, intreg, slot);
                    }
                    slot += 2;
                }

                if (regI % 2 == 1)
                {
                    if (cr == CrUnchainedSavedLr)
                    {
                        Pair(0x6B, 3, intreg / 2, slot);
                        slot += 2;
                    }
                    else if (!predecDone)
                    {
                        Op(Encode16((0x6A << 9) | (intreg << 5) | (savsz - 1)));
                        slot++;
                        predecDone = true;
                    }
                    else
                    {
                        Pair(0x34, 4, intreg, slot);
                        slot++;
                    }
                }
                else if (cr == CrUnchainedSavedLr)
                {
                    if (!predecDone)
                    {
                        Op(Encode16((0x6A << 9) | (11 << 5) | (savsz - 1)));
                        predecDone = true;
                    }
                    else
                    {
                        Pair(0x34, 4, 11, slot);
                    }
                    slot++;
                }
            }

            if (fpsz != 0)
            {
                int fpreg = 0;
                for (; fpreg < fpsz / 2 * 2; fpreg += 2)
                {
                    if (!predecDone)
                    {
                        Pair(0x6D, 3, fpreg, savsz - 1);
                        predecDone = true;
                    }
                    else
                    {
                        Pair(0x6C, 3, fpreg, slot);
                    }
                    slot += 2;
                }

                if (fpsz % 2 == 1)
                {
                    if (!predecDone)
                    {
                        Op(Encode16((0xDE << 8) | (fpreg << 5) | (savsz - 1)));
                        predecDone = true;
                    }
                    else
                    {
                        Pair(0x6E, 3, fpreg, slot);
                    }
                    slot++;
                }
            }

            bool fpSet = false;
            if (locsz > 0)
            {
                if (cr == CrChained || cr == CrChainedWithPac)
                {
                    if (locsz <= 512 / 8)
                    {
                        Op([(byte)(0x80 | (locsz - 1))]);
                    }
                    else
                    {
                        Alloc(locsz * 8);
                        Op([0x40]);
                    }
                    Op([0xE1]);
                    fpSet = true;
                }
                else
                {
                    Alloc(locsz * 8);
                }
            }

            var bytes = new List<byte>();
            for (int i = ops.Count - 1; i >= 0; i--)
                bytes.AddRange(ops[i]);
            int codeWords = (bytes.Count + 3) / 4;
            while (bytes.Count < codeWords * 4)
                bytes.Add(0);

            uint header = (uint)functionLength | (1u << 21) | ((fpSet ? 1u : 0u) << 22) | ((uint)codeWords << 27);
            var result = new byte[4 + bytes.Count];
            BinaryPrimitives.WriteUInt32LittleEndian(result, header);
            bytes.CopyTo(result, 4);
            return result;
        }

        private static void ConvertArm(byte[] unwindInfo, CfiWriter writer)
        {
            const int DwarfLr = 14, DwarfD0 = 256;
            ReadOnlySpan<byte> codes = GetUnwindCodes(unwindInfo, arm: true);

            // Prolog codes for a phantom prolog describe the state at the start of the code
            bool phantomProlog = ((BinaryPrimitives.ReadUInt32LittleEndian(unwindInfo) >> 22) & 1) != 0;

            // The prolog codes are in reverse order of the prolog instructions, one code per instruction
            var ops = new List<int>();
            for (int i = 0; i < codes.Length && codes[i] < 0xFD;)
            {
                ops.Add(i);
                byte b = codes[i];
                i += b switch
                {
                    < 0x80 => 1,
                    < 0xC0 => 2,
                    < 0xE8 => 1,
                    < 0xF0 => 2,
                    < 0xF5 => 1,
                    0xF5 or 0xF6 => 2,
                    0xF7 or 0xF9 => 3,
                    0xF8 or 0xFA => 4,
                    _ => 1,
                };
            }
            ops.Reverse();

            int position = 0;
            foreach (int i in ops)
            {
                int b1 = codes[i];
                int b2 = i + 1 < codes.Length ? codes[i + 1] : 0;
                bool wide = b1 switch
                {
                    < 0x80 => false,
                    < 0xC0 => true,
                    < 0xD8 => false,
                    < 0xEC => true,
                    0xEC or 0xED or 0xEE or 0xF7 or 0xF8 or 0xFB => false,
                    _ => true,
                };
                position += wide ? 4 : 2;
                int codeOffset = phantomProlog ? 0 : position;

                void PushIntegers(int mask)
                {
                    int count = System.Numerics.BitOperations.PopCount((uint)mask);
                    writer.Alloc(codeOffset, 4 * count);
                    int slot = 0;
                    for (int reg = 0; reg < 16; reg++)
                    {
                        if ((mask & (1 << reg)) == 0)
                            continue;
                        // Only the callee-saved registers and the return address are restored
                        if (reg is >= 4 and <= 11 or DwarfLr)
                            writer.Save(codeOffset, reg, 4 * slot);
                        slot++;
                    }
                }

                void PushDoubles(int first, int last)
                {
                    writer.Alloc(codeOffset, 8 * (last - first + 1));
                    for (int reg = first; reg <= last; reg++)
                    {
                        if (reg is >= 8 and <= 15)
                            writer.Save(codeOffset, DwarfD0 + reg, 8 * (reg - first));
                    }
                }

                if (b1 < 0x80)
                {
                    writer.Alloc(codeOffset, b1 * 4);
                }
                else if (b1 < 0xC0)
                {
                    PushIntegers((((b1 & 0x1F) << 8) | b2) | ((b1 & 0x20) != 0 ? 1 << DwarfLr : 0));
                }
                else if (b1 < 0xD0)
                {
                    writer.SetFrame(codeOffset, b1 & 0xF, 0);
                }
                else if (b1 < 0xE0)
                {
                    int last = (b1 & 0x3) + ((b1 & 0x8) != 0 ? 8 : 4);
                    PushIntegers(((1 << (last + 1)) - (1 << 4)) | ((b1 & 0x4) != 0 ? 1 << DwarfLr : 0));
                }
                else if (b1 < 0xE8)
                {
                    PushDoubles(8, 8 + (b1 & 0x7));
                }
                else if (b1 < 0xEC)
                {
                    writer.Alloc(codeOffset, (((b1 & 0x3) << 8) | b2) * 4);
                }
                else if (b1 < 0xEE)
                {
                    PushIntegers(b2 | ((b1 & 0x1) != 0 ? 1 << DwarfLr : 0));
                }
                else if (b1 == 0xEF && (b2 & 0xF0) == 0)
                {
                    writer.Alloc(codeOffset, (b2 & 0xF) * 4);
                    writer.Save(codeOffset, DwarfLr, 0);
                }
                else if (b1 is 0xF5 or 0xF6)
                {
                    int bias = b1 == 0xF6 ? 16 : 0;
                    PushDoubles((b2 >> 4) + bias, (b2 & 0xF) + bias);
                }
                else if (b1 is 0xF7 or 0xF9)
                {
                    writer.Alloc(codeOffset, ((b2 << 8) | codes[i + 2]) * 4);
                }
                else if (b1 is 0xF8 or 0xFA)
                {
                    writer.Alloc(codeOffset, ((b2 << 16) | (codes[i + 2] << 8) | codes[i + 3]) * 4);
                }
                else if (b1 is not (0xFB or 0xFC))
                {
                    throw new InvalidOperationException($"Unexpected unwind code {b1:X2}.");
                }
            }
        }

        private static void ConvertLoongArch64OrRiscV64(byte[] unwindInfo, CfiWriter writer, int fpRegister, int firstFloatRegister, bool wideFloatIndex)
        {
            ReadOnlySpan<byte> codes = GetUnwindCodes(unwindInfo, arm: false);

            // The prolog codes are in reverse order of the prolog instructions, one code per instruction
            var ops = new List<int>();
            for (int i = 0; i < codes.Length && (codes[i] & 0xFE) != 0xE4;)
            {
                ops.Add(i);
                byte b = codes[i];
                i += b switch
                {
                    < 0xC0 => 1,
                    < 0xC8 => 2,
                    0xD0 or 0xDC or 0xDD or 0xE2 => 3,
                    0xE0 => 4,
                    _ => 1,
                };
            }
            ops.Reverse();

            for (int k = 0; k < ops.Count; k++)
            {
                int codeOffset = 4 * (k + 1);
                int i = ops[k];
                int b1 = codes[i];
                int b2 = i + 1 < codes.Length ? codes[i + 1] : 0;
                int b3 = i + 2 < codes.Length ? codes[i + 2] : 0;

                if (b1 < 0x20)
                {
                    writer.Alloc(codeOffset, b1 * 16);
                }
                else if ((b1 & 0xF8) == 0xC0)
                {
                    writer.Alloc(codeOffset, (((b1 & 0x7) << 8) | b2) * 16);
                }
                else if (b1 == 0xE0)
                {
                    writer.Alloc(codeOffset, ((b2 << 16) | (b3 << 8) | codes[i + 3]) * 16);
                }
                else if (b1 == 0xD0)
                {
                    // The register is the number of the register after the return address register
                    writer.Save(codeOffset, 1 + b2, b3 * 8);
                }
                else if (b1 == 0xDC || (wideFloatIndex && b1 == 0xDD))
                {
                    int x = ((b1 & 0x1) << 4) | (b2 >> 4);
                    writer.Save(codeOffset, firstFloatRegister + x, (((b2 & 0xF) << 8) | b3) * 8);
                }
                else if (b1 == 0xE1)
                {
                    writer.SetFrame(codeOffset, fpRegister, 0);
                }
                else if (b1 == 0xE2)
                {
                    writer.SetFrame(codeOffset, fpRegister, ((b2 << 8) | b3) * 8);
                }
                else if (b1 != 0xE3)
                {
                    throw new InvalidOperationException($"Unexpected unwind code {b1:X2}.");
                }
            }
        }

        // Get the CFI data in the same shape as clang/LLVM generated one. This improves the compatibility with libunwind and other unwind solutions
        // - Combine in one single block for the whole prolog instead of one CFI block per assembler instruction
        // - Store CFA definition first
        // - Store all used registers in ascending order
        private static byte[] CompressArm64(byte[] blobData)
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
            bool shouldAddPacOpCode = false;
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

                    case CFI_OPCODE.CFI_NEGATE_RA_STATE:
                        // Sign before frame setup so signing SP == CFA.
                        Debug.Assert(cfaRegister == spReg);
                        Debug.Assert(cfaOffset == 0);
                        Debug.Assert(spOffset == 0);
                        shouldAddPacOpCode = true;
                        break;
                }
            }

            using (MemoryStream cfiStream = new MemoryStream())
            {
                int storeOffset = 0;

                using (BinaryWriter cfiWriter = new BinaryWriter(cfiStream))
                {
                    if (shouldAddPacOpCode)
                    {
                        cfiWriter.Write((byte)codeOffset);
                        cfiWriter.Write((byte)CFI_OPCODE.CFI_NEGATE_RA_STATE);
                        cfiWriter.Write((short)-1);
                        cfiWriter.Write(0);
                    }

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
