// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis
{
    public enum RelocType
    {
        IMAGE_REL_BASED_ABSOLUTE        = 0x00,     // No relocation required
        IMAGE_REL_BASED_ADDR32NB        = 0x02,     // The 32-bit address without an image base (RVA)
        IMAGE_REL_BASED_HIGHLOW         = 0x03,     // 32 bit address base
        IMAGE_REL_BASED_THUMB_MOV32     = 0x07,     // Thumb2: based MOVW/MOVT
        IMAGE_REL_BASED_DIR64           = 0x0A,     // 64 bit address base
        IMAGE_REL_BASED_REL32           = 0x10,     // 32-bit relative address from byte following reloc
        IMAGE_REL_BASED_THUMB_BRANCH24  = 0x13,     // Thumb2: based B, BL
        IMAGE_REL_BASED_ARM64_BRANCH26  = 0x14,     // Arm64: B, BL
        IMAGE_REL_BASED_RELPTR32        = 0x7C,     // 32-bit relative address from byte starting reloc
                                                    // This is a special NGEN-specific relocation type 
                                                    // for relative pointer (used to make NGen relocation 
                                                    // section smaller)    
        IMAGE_REL_SECREL                = 0x80,     // 32 bit offset from base of section containing target

        IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 = 0x81,   // ADRP
        IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A = 0x82,   // ADD/ADDS (immediate) with zero shift, for page offset
        IMAGE_REL_BASED_ARM64_PAGEOFFSET_12L = 0x83,   // LDR (indexed, unsigned immediate), for page offset
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
            return (int)((ret << 7) >> 7);
        }

        //*****************************************************************************
        // Returns whether the offset fits into bl instruction
        //*****************************************************************************
        private static bool FitsInThumb2BlRel24(uint imm24)
        {
            return ((imm24 << 7) >> 7) == imm24;
        }

        //*****************************************************************************
        //  Deposit the 24-bit rel offset into bl instruction
        //*****************************************************************************
        private static unsafe void PutThumb2BlRel24(ushort* p, uint imm24)
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

            uint S  =  (imm24 & 0x1000000) >> 24;
            uint J1 = ((imm24 & 0x0800000) >> 23) ^ S ^ 1;
            uint J2 = ((imm24 & 0x0400000) >> 22) ^ S ^ 1;

            Opcode0 |=  ((imm24 & 0x03FF000) >> 12) | (S << 10);
            Opcode1 |=  ((imm24 & 0x0000FFE) >>  1) | (J1 << 13) | (J2 << 11);

            p[0] = (ushort)Opcode0;
            p[1] = (ushort)Opcode1;

            Debug.Assert((uint)GetThumb2BlRel24(p) == imm24);
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
                case RelocType.IMAGE_REL_BASED_HIGHLOW:
                case RelocType.IMAGE_REL_BASED_REL32:
                case RelocType.IMAGE_REL_BASED_ADDR32NB:
                    *(int*)location = (int)value;
                    break;
                case RelocType.IMAGE_REL_BASED_DIR64:
                    *(long*)location = value;
                    break;
                case RelocType.IMAGE_REL_BASED_THUMB_MOV32:
                    PutThumb2Mov32((ushort*)location, (uint)value);
                    break;
                case RelocType.IMAGE_REL_BASED_THUMB_BRANCH24:
                    PutThumb2BlRel24((ushort*)location, (uint)value);
                    break;
                default:
                    Debug.Fail("Invalid RelocType: " + relocType);
                    break;
            }
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
                    return *(int*)location;
                case RelocType.IMAGE_REL_BASED_DIR64:
                    return *(long*)location;
                case RelocType.IMAGE_REL_BASED_THUMB_MOV32:
                    return (long)GetThumb2Mov32((ushort*)location);
                case RelocType.IMAGE_REL_BASED_THUMB_BRANCH24:
                    return (long)GetThumb2BlRel24((ushort*)location);
                default:
                    Debug.Fail("Invalid RelocType: " + relocType);
                    return 0;
            }
        }

        public override string ToString()
        {
            return $"{Target} ({RelocType}, 0x{Offset:X})";
        }
    }
}
