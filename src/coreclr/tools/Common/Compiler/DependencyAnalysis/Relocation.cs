// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis
{
    public enum RelocType
    {
        IMAGE_REL_BASED_ABSOLUTE             = 0x00,   // No relocation required
        IMAGE_REL_BASED_ADDR32NB             = 0x02,   // The 32-bit address without an image base (RVA)
        IMAGE_REL_BASED_HIGHLOW              = 0x03,   // 32 bit address base
        IMAGE_REL_BASED_THUMB_MOV32          = 0x07,   // Thumb2: based MOVW/MOVT
        IMAGE_REL_BASED_DIR64                = 0x0A,   // 64 bit address base
        IMAGE_REL_BASED_REL32                = 0x10,   // 32-bit relative address from byte following reloc
        IMAGE_REL_BASED_THUMB_BRANCH24       = 0x13,   // Thumb2: based B, BL
        IMAGE_REL_BASED_THUMB_MOV32_PCREL    = 0x14,   // Thumb2: based MOVW/MOVT
        IMAGE_REL_BASED_ARM64_BRANCH26       = 0x15,   // Arm64: B, BL
        IMAGE_REL_BASED_LOONGARCH64_PC       = 0x16,   // LoongArch64: pcaddu12i+imm12
        IMAGE_REL_BASED_LOONGARCH64_JIR      = 0x17,   // LoongArch64: pcaddu18i+jirl
        IMAGE_REL_BASED_RISCV64_PC           = 0x18,   // RiscV64: auipc
        IMAGE_REL_BASED_RELPTR32             = 0x7C,   // 32-bit relative address from byte starting reloc
                                                       // This is a special NGEN-specific relocation type
                                                       // for relative pointer (used to make NGen relocation
                                                       // section smaller)
        IMAGE_REL_SECTION                    = 0x79,   // 16 bit section index containing target

        IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 = 0x81,   // ADRP
        IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A = 0x82,   // ADD/ADDS (immediate) with zero shift, for page offset
        IMAGE_REL_BASED_ARM64_PAGEOFFSET_12L = 0x83,   // LDR (indexed, unsigned immediate), for page offset

        //
        // Relocation operators related to TLS access
        //

        // Windows x64
        IMAGE_REL_SECREL                     = 0x104,

        // Linux x64
        // GD model
        IMAGE_REL_TLSGD                      = 0x105,
        // LE model
        IMAGE_REL_TPOFF                      = 0x106,

        // Linux arm64
        //    TLSDESC  (dynamic)
        IMAGE_REL_AARCH64_TLSDESC_ADR_PAGE21 = 0x107,
        IMAGE_REL_AARCH64_TLSDESC_LD64_LO12  = 0x108,
        IMAGE_REL_AARCH64_TLSDESC_ADD_LO12   = 0x109,
        IMAGE_REL_AARCH64_TLSDESC_CALL       = 0x10A,
        //    LE model
        IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_HI12    = 0x10B,
        IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_LO12_NC = 0x10C,

        //
        // Relocations for R2R image production
        //
        IMAGE_REL_SYMBOL_SIZE                = 0x1000, // The size of data in the image represented by the target symbol node
        IMAGE_REL_FILE_ABSOLUTE              = 0x1001, // 32 bit offset from beginning of image
    }

    public struct Relocation
    {
        public readonly RelocType RelocType;
        public readonly int Offset;
        public readonly ISymbolNode Target;

        //*****************************************************************************
        //  Extract the 16-bit immediate from ARM Thumb2 Instruction (format T2_N)
        //*****************************************************************************
        private static unsafe ushort GetThumb2Imm16(ushort* p)
        {
            uint Opcode0 = (uint)p[0];
            uint Opcode1 = (uint)p[1];
            uint Result =
                ((Opcode0 << 12) & 0xf000) |
                ((Opcode0 <<  1) & 0x0800) |
                ((Opcode1 >>  4) & 0x0700) |
                ((Opcode1 >>  0) & 0x00ff);
            return (ushort)Result;
        }

        //*****************************************************************************
        //  Deposit the 16-bit immediate into ARM Thumb2 Instruction (format T2_N)
        //*****************************************************************************
        private static unsafe void PutThumb2Imm16(ushort* p, ushort imm16)
        {
            uint Opcode0 = (uint)p[0];
            uint Opcode1 = (uint)p[1];
            int val0 = (0xf000 >> 12);
            int val1 = (0x0800 >>  1);
            Opcode0 &= ~((uint)val0 | (uint)val1);
            int val3 = (0x0700 <<  4);
            Opcode1 &= ~((uint)val3 | (0x00ff << 0));
            Opcode0 |= ((uint)imm16 & 0xf000) >> 12;
            Opcode0 |= ((uint)imm16 & 0x0800) >>  1;
            Opcode1 |= ((uint)imm16 & 0x0700) <<  4;
            Opcode1 |= ((uint)imm16 & 0x00ff) <<  0;
            p[0] = (ushort)Opcode0;
            p[1] = (ushort)Opcode1;
        }

        //*****************************************************************************
        //  Extract the 32-bit immediate from movw/movt sequence
        //*****************************************************************************
        private static unsafe int GetThumb2Mov32(ushort* p)
        {
            // Make sure we are decoding movw/movt sequence
            ushort Opcode0 = *(p + 0);
            ushort Opcode1 = *(p + 2);
            Debug.Assert(((uint)Opcode0 & 0xFBF0) == 0xF240);
            Debug.Assert(((uint)Opcode1 & 0xFBF0) == 0xF2C0);

            return (int)GetThumb2Imm16(p) + ((int)(GetThumb2Imm16(p + 2) << 16));
        }

        //*****************************************************************************
        //  Deposit the 32-bit immediate into movw/movt Thumb2 sequence
        //*****************************************************************************
        private static unsafe void PutThumb2Mov32(ushort* p, uint imm32)
        {
            // Make sure we are decoding movw/movt sequence
            ushort Opcode0 = *(p + 0);
            ushort Opcode1 = *(p + 2);
            Debug.Assert(((uint)Opcode0 & 0xFBF0) == 0xF240);
            Debug.Assert(((uint)Opcode1 & 0xFBF0) == 0xF2C0);

            ushort imm16 = (ushort)(imm32 & 0xffff);
            PutThumb2Imm16(p, imm16);
            imm16 = (ushort)(imm32 >> 16);
            PutThumb2Imm16(p + 2, imm16);

            Debug.Assert((uint)GetThumb2Mov32(p) == imm32);
        }

        //*****************************************************************************
        //  Extract the 24-bit rel offset from bl instruction
        //*****************************************************************************
        private static unsafe int GetThumb2BlRel24(ushort* p)
        {
            uint Opcode0 = (uint)p[0];
            uint Opcode1 = (uint)p[1];

            uint S  = Opcode0 >> 10;
            uint J2 = Opcode1 >> 11;
            uint J1 = Opcode1 >> 13;

            uint ret =
                ((S << 24)              & 0x1000000) |
                (((J1 ^ S ^ 1) << 23)   & 0x0800000) |
                (((J2 ^ S ^ 1) << 22)   & 0x0400000) |
                ((Opcode0 << 12)        & 0x03FF000) |
                ((Opcode1 <<  1)        & 0x0000FFE);

            // Sign-extend and return
            return (int)(ret << 7) >> 7;
        }

        //*****************************************************************************
        // Returns whether the offset fits into bl instruction
        //*****************************************************************************
        private static bool FitsInThumb2BlRel24(int imm24)
        {
            return ((imm24 << 7) >> 7) == imm24;
        }

        //*****************************************************************************
        //  Deposit the 24-bit rel offset into bl instruction
        //*****************************************************************************
        private static unsafe void PutThumb2BlRel24(ushort* p, int imm24)
        {
            // Verify that we got a valid offset
            Debug.Assert(FitsInThumb2BlRel24(imm24));

            // Ensure that the ThumbBit is not set on the offset
            // as it cannot be encoded.
            Debug.Assert((imm24 & 1/*THUMB_CODE*/) == 0);

            uint Opcode0 = (uint)p[0];
            uint Opcode1 = (uint)p[1];
            Opcode0 &= 0xF800;
            Opcode1 &= 0xD000;

            uint S  =  ((uint)imm24 & 0x1000000) >> 24;
            uint J1 = (((uint)imm24 & 0x0800000) >> 23) ^ S ^ 1;
            uint J2 = (((uint)imm24 & 0x0400000) >> 22) ^ S ^ 1;

            Opcode0 |=  (((uint)imm24 & 0x03FF000) >> 12) | (S << 10);
            Opcode1 |=  (((uint)imm24 & 0x0000FFE) >>  1) | (J1 << 13) | (J2 << 11);

            p[0] = (ushort)Opcode0;
            p[1] = (ushort)Opcode1;

            Debug.Assert(GetThumb2BlRel24(p) == imm24);
        }

        //*****************************************************************************
        //  Extract the PC-Relative offset from an adrp instruction
        //*****************************************************************************
        private static unsafe int GetArm64Rel21(uint* pCode)
        {
            int adrpInstr = (int)*pCode;

            // 23-5 bits for the high part. Shift it by 5.
            int immhi = (adrpInstr & 0xFFFFE0) >> 5;
            // 30,29 bits for the lower part. Shift it by 29.
            int immlo = (adrpInstr & 0x60000000) >> 29;

            // Merge them
            int imm21 = (immhi << 2) | immlo;

            return imm21;
        }

        //*****************************************************************************
        // Returns whether the offset fits into an Arm64 adrp instruction
        //*****************************************************************************
        private static bool FitsInRel21(int val32)
        {
            return (val32 >= 0) && (val32 <= 0x001FFFFF);
        }

        //*****************************************************************************
        //  Deposit the PC-Relative offset 'imm21' into an adrp instruction
        //*****************************************************************************
        private static unsafe void PutArm64Rel21(uint* pCode, int imm21)
        {
            // Verify that we got a valid offset
            Debug.Assert(FitsInRel21(imm21));

            uint adrpInstr = *pCode;
            // Check adrp opcode 1ii1 0000 ...
            Debug.Assert((adrpInstr & 0x9F000000) == 0x90000000);

            adrpInstr &= 0x9F00001F;             // keep bits 31, 28-24, 4-0.
            int immlo = imm21 & 0x03;            // Extract low 2 bits which will occupy 30-29 bits.
            int immhi = (imm21 & 0x1FFFFC) >> 2; // Extract high 19 bits which will occupy 23-5 bits.
            adrpInstr |= (uint)((immlo << 29) | (immhi << 5));

            *pCode = adrpInstr;                  // write the assembled instruction

            Debug.Assert(GetArm64Rel21(pCode) == imm21);
        }

        //*****************************************************************************
        //  Extract the PC-Relative offset from an add instruction
        //*****************************************************************************
        private static unsafe int GetArm64Rel12(uint* pCode)
        {
            uint addInstr = *pCode;

            // 21-10 contains value. Mask 12 bits and shift by 10 bits.
            int imm12 = (int)(addInstr & 0x003FFC00) >> 10;

            return imm12;
        }

        //*****************************************************************************
        // Returns whether the offset fits into an Arm64 add instruction
        //*****************************************************************************
        private static bool FitsInRel12(int val32)
        {
            return (val32 >= 0) && (val32 <= 0x00000FFF);
        }

        //*****************************************************************************
        //  Deposit the PC-Relative offset 'imm12' into an add instruction
        //*****************************************************************************
        private static unsafe void PutArm64Rel12(uint* pCode, int imm12)
        {
            // Verify that we got a valid offset
            Debug.Assert(FitsInRel12(imm12));

            uint addInstr = *pCode;
            // Check add opcode 1001 0001 00...
            Debug.Assert((addInstr & 0xFFC00000) == 0x91000000);

            addInstr &= 0xFFC003FF;          // keep bits 31-22, 9-0
            addInstr |= (uint)(imm12 << 10); // Occupy 21-10.

            *pCode = addInstr;               // write the assembled instruction

            Debug.Assert(GetArm64Rel12(pCode) == imm12);
        }

        private static unsafe int GetArm64Rel28(uint* pCode)
        {
            uint branchInstr = *pCode;

            // first shift 6 bits left to set the sign bit,
            // then arithmetic shift right by 4 bits
            int imm28 = (((int)(branchInstr & 0x03FFFFFF)) << 6) >> 4;

            return imm28;
        }

        private static bool FitsInArm64Rel28(long imm28)
        {
            return (imm28 >= -0x08000000L) && (imm28 < 0x08000000L);
        }

        private static unsafe void PutArm64Rel28(uint* pCode, long imm28)
        {
            // Verify that we got a valid offset
            Debug.Assert(FitsInArm64Rel28(imm28));

            Debug.Assert((imm28 & 0x3) == 0);    // the low two bits must be zero

            uint branchInstr = *pCode;

            branchInstr &= 0xFC000000;       // keep bits 31-26

            Debug.Assert((branchInstr & 0x7FFFFFFF) == 0x14000000);  // Must be B or BL

            // Assemble the pc-relative delta 'imm28' into the branch instruction
            branchInstr |= (uint)(((imm28 >> 2) & 0x03FFFFFFU));

            *pCode = branchInstr;          // write the assembled instruction

            Debug.Assert(GetArm64Rel28(pCode) == imm28);
        }

        private static unsafe int GetLoongArch64PC12(uint* pCode)
        {
            uint pcInstr = *pCode;

            // first get the high 20 bits,
            int imm = (int)(((pcInstr >> 5) & 0xFFFFF) << 12);

            // then get the low 12 bits,
            pcInstr = *(pCode + 1);
            imm += ((int)(((pcInstr >> 10) & 0xFFF) << 20)) >> 20;

            return imm;
        }

        //  case:EA_HANDLE_CNS_RELOC
        //   pcaddu12i  reg, off-hi-20bits
        //   addi_d  reg, reg, off-lo-12bits
        //  case:EA_PTR_DSP_RELOC
        //   pcaddu12i  reg, off-hi-20bits
        //   ld_d  reg, reg, off-lo-12bits
        private static unsafe void PutLoongArch64PC12(uint* pCode, long imm32)
        {
            // Verify that we got a valid offset
            Debug.Assert((int)imm32 == imm32);

            uint pcInstr = *pCode;

            Debug.Assert((pcInstr & 0xFE000000) == 0x1c000000);  // Must be pcaddu12i

            int relOff = (int)imm32 & 0x800;
            int imm = (int)imm32 + relOff;
            relOff = ((imm & 0x7ff) - relOff) & 0xfff;

            // Assemble the pc-relative high 20 bits of 'imm32' into the pcaddu12i instruction
            pcInstr |= (uint)(((imm >> 12) & 0xFFFFF) << 5);

            *pCode = pcInstr;          // write the assembled instruction

            pcInstr = *(pCode + 1);

            // Assemble the pc-relative low 12 bits of 'imm32' into the addid or ld instruction
            pcInstr |= (uint)(relOff << 10);

            *(pCode + 1) = pcInstr;          // write the assembled instruction

            Debug.Assert(GetLoongArch64PC12(pCode) == imm32);
        }

        private static unsafe long GetLoongArch64JIR(uint* pCode)
        {
            uint pcInstr = *pCode;

            // first get the high 20 bits,
            long imm = ((long)((pcInstr >> 5) & 0xFFFFF) << 18);

            // then get the low 18 bits
            pcInstr = *(pCode + 1);
            imm += ((long)((short)((pcInstr >> 10) & 0xFFFF))) << 2;

            return imm;
        }

        private static unsafe void PutLoongArch64JIR(uint* pCode, long imm38)
        {
            // Verify that we got a valid offset
            Debug.Assert((imm38 >= -0x2000000000L) && (imm38 < 0x2000000000L));

            Debug.Assert((imm38 & 0x3) == 0);    // the low two bits must be zero

            uint pcInstr = *pCode;

            Debug.Assert(pcInstr == 0x1e00000e);  // Must be pcaddu18i R14, 0

            long relOff = imm38 & 0x20000;
            long imm = imm38 + relOff;
            relOff = (((imm & 0x1ffff) - relOff) >> 2) & 0xffff;

            // Assemble the pc-relative high 20 bits of 'imm38' into the pcaddu12i instruction
            pcInstr |= (uint)(((imm >> 18) & 0xFFFFF) << 5);

            *pCode = pcInstr;          // write the assembled instruction

            pcInstr = *(pCode + 1);

            // Assemble the pc-relative low 18 bits of 'imm38' into the addid or ld instruction
            pcInstr |= (uint)(relOff << 10);

            *(pCode + 1) = pcInstr;          // write the assembled instruction

            Debug.Assert(GetLoongArch64JIR(pCode) == imm38);
        }

        private static unsafe int GetRiscV64PC(uint* pCode)
        {
            uint auipcInstr = *pCode;
            Debug.Assert((auipcInstr & 0x7f) == 0x00000017);
            // first get the high 20 bits,
            int imm = (int)((auipcInstr & 0xfffff000));
            // then get the low 12 bits,
            uint addiInstr = *(pCode + 1);
            Debug.Assert((addiInstr & 0x707f) == 0x00000013);
            imm += ((int)(addiInstr)) >> 20;

            return imm;
        }

        // INS_OPTS_RELOC: placeholders.  2-ins:
        //  case:EA_HANDLE_CNS_RELOC
        //   auipc  reg, off-hi-20bits
        //   addi   reg, reg, off-lo-12bits
        //  case:EA_PTR_DSP_RELOC
        //   auipc  reg, off-hi-20bits
        //   ld     reg, reg, off-lo-12bits
        private static unsafe void PutRiscV64PC(uint* pCode, long imm32)
        {
            // Verify that we got a valid offset
            Debug.Assert((int)imm32 == imm32);

            int doff = (int)(imm32 & 0xfff);
            uint auipcInstr = *pCode;
            Debug.Assert((auipcInstr & 0x7f) == 0x00000017);

            auipcInstr |= (uint)((imm32 + 0x800) & 0xfffff000);
            *pCode = auipcInstr;

            uint addiInstr = *(pCode + 1);
            Debug.Assert((addiInstr & 0x707f) == 0x00000013);
            addiInstr |= (uint)((doff & 0xfff) << 20);
            *(pCode + 1) = addiInstr;

            Debug.Assert(GetRiscV64PC(pCode) == imm32);
        }

        public Relocation(RelocType relocType, int offset, ISymbolNode target)
        {
            RelocType = relocType;
            Offset = offset;
            Target = target;
        }

        public static unsafe void WriteValue(RelocType relocType, void* location, long value)
        {
            switch (relocType)
            {
                case RelocType.IMAGE_REL_BASED_ABSOLUTE:
                case RelocType.IMAGE_REL_BASED_ADDR32NB:
                case RelocType.IMAGE_REL_BASED_HIGHLOW:
                case RelocType.IMAGE_REL_BASED_REL32:
                case RelocType.IMAGE_REL_BASED_RELPTR32:
                case RelocType.IMAGE_REL_SECREL:
                case RelocType.IMAGE_REL_TLSGD:
                case RelocType.IMAGE_REL_TPOFF:
                case RelocType.IMAGE_REL_SYMBOL_SIZE:
                case RelocType.IMAGE_REL_FILE_ABSOLUTE:
                case RelocType.IMAGE_REL_AARCH64_TLSDESC_LD64_LO12:
                    *(int*)location = (int)value;
                    break;
                case RelocType.IMAGE_REL_BASED_DIR64:
                    *(long*)location = value;
                    break;
                case RelocType.IMAGE_REL_BASED_THUMB_MOV32:
                case RelocType.IMAGE_REL_BASED_THUMB_MOV32_PCREL:
                    PutThumb2Mov32((ushort*)location, (uint)value);
                    break;
                case RelocType.IMAGE_REL_BASED_THUMB_BRANCH24:
                    PutThumb2BlRel24((ushort*)location, (int)value);
                    break;
                case RelocType.IMAGE_REL_BASED_ARM64_BRANCH26:
                    PutArm64Rel28((uint*)location, value);
                    break;
                case RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21:
                case RelocType.IMAGE_REL_AARCH64_TLSDESC_ADR_PAGE21:
                    PutArm64Rel21((uint*)location, (int)value);
                    break;
                case RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A:
                case RelocType.IMAGE_REL_AARCH64_TLSDESC_ADD_LO12:
                    PutArm64Rel12((uint*)location, (int)value);
                    break;
                case RelocType.IMAGE_REL_BASED_LOONGARCH64_PC:
                    PutLoongArch64PC12((uint*)location, value);
                    break;
                case RelocType.IMAGE_REL_BASED_LOONGARCH64_JIR:
                    PutLoongArch64JIR((uint*)location, value);
                    break;
                case RelocType.IMAGE_REL_BASED_RISCV64_PC:
                    PutRiscV64PC((uint*)location, value);
                    break;
                default:
                    Debug.Fail("Invalid RelocType: " + relocType);
                    break;
            }
        }

        public static int GetSize(RelocType relocType)
        {
            return relocType switch
            {
                RelocType.IMAGE_REL_BASED_DIR64 => 8,
                RelocType.IMAGE_REL_BASED_HIGHLOW => 4,
                RelocType.IMAGE_REL_BASED_RELPTR32 => 4,
                _ => throw new NotSupportedException(),
            };
        }

        public static unsafe long ReadValue(RelocType relocType, void* location)
        {
            switch (relocType)
            {
                case RelocType.IMAGE_REL_BASED_ABSOLUTE:
                case RelocType.IMAGE_REL_BASED_ADDR32NB:
                case RelocType.IMAGE_REL_BASED_HIGHLOW:
                case RelocType.IMAGE_REL_BASED_REL32:
                case RelocType.IMAGE_REL_BASED_RELPTR32:
                case RelocType.IMAGE_REL_SECREL:
                case RelocType.IMAGE_REL_TLSGD:
                case RelocType.IMAGE_REL_TPOFF:
                case RelocType.IMAGE_REL_FILE_ABSOLUTE:
                case RelocType.IMAGE_REL_SYMBOL_SIZE:
                case RelocType.IMAGE_REL_AARCH64_TLSDESC_LD64_LO12:
                    return *(int*)location;
                case RelocType.IMAGE_REL_BASED_DIR64:
                    return *(long*)location;
                case RelocType.IMAGE_REL_BASED_THUMB_MOV32:
                case RelocType.IMAGE_REL_BASED_THUMB_MOV32_PCREL:
                    return (long)GetThumb2Mov32((ushort*)location);
                case RelocType.IMAGE_REL_BASED_THUMB_BRANCH24:
                    return (long)GetThumb2BlRel24((ushort*)location);
                case RelocType.IMAGE_REL_BASED_ARM64_BRANCH26:
                    return (long)GetArm64Rel28((uint*)location);
                case RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21:
                    return GetArm64Rel21((uint*)location);
                case RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A:
                    return GetArm64Rel12((uint*)location);
                case RelocType.IMAGE_REL_AARCH64_TLSDESC_ADD_LO12:
                case RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_HI12:
                case RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_LO12_NC:
                    // TLS relocs do not have offsets
                    Debug.Assert((GetArm64Rel12((uint*)location) & 0xFF) == 0);
                    return 0;
                case RelocType.IMAGE_REL_AARCH64_TLSDESC_ADR_PAGE21:
                    // TLS relocs do not have offsets
                    Debug.Assert((GetArm64Rel21((uint*)location) & 0xFF) == 0);
                    return 0;
                case RelocType.IMAGE_REL_AARCH64_TLSDESC_CALL:
                    // TLS relocs do not have offsets
                    return 0;
                case RelocType.IMAGE_REL_BASED_LOONGARCH64_PC:
                    return (long)GetLoongArch64PC12((uint*)location);
                case RelocType.IMAGE_REL_BASED_LOONGARCH64_JIR:
                    return (long)GetLoongArch64JIR((uint*)location);
                case RelocType.IMAGE_REL_BASED_RISCV64_PC:
                    return (long)GetRiscV64PC((uint*)location);
                default:
                    Debug.Fail("Invalid RelocType: " + relocType);
                    return 0;
            }
        }

        /// <summary>
        /// Return file relocation type for the given relocation type. If the relocation
        /// doesn't require a file-level relocation entry in the .reloc section, 0 is returned
        /// corresponding to the IMAGE_REL_BASED_ABSOLUTE no-op relocation record.
        /// </summary>
        /// <param name="relocationType">Relocation type</param>
        /// <returns>File-level relocation type or 0 (IMAGE_REL_BASED_ABSOLUTE) if none is required</returns>
        public static RelocType GetFileRelocationType(RelocType relocationType)
        {
            switch (relocationType)
            {
                case RelocType.IMAGE_REL_BASED_HIGHLOW:
                case RelocType.IMAGE_REL_BASED_DIR64:
                case RelocType.IMAGE_REL_BASED_THUMB_MOV32:
                    return relocationType;

                default:
                    return RelocType.IMAGE_REL_BASED_ABSOLUTE;
            }
        }

        public override string ToString()
        {
            return $"{Target} ({RelocType}, 0x{Offset:X})";
        }
    }
}
