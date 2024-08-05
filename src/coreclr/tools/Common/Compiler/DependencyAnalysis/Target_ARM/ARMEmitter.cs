// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis.ARM
{
    public struct ARMEmitter
    {
        public ARMEmitter(NodeFactory factory, bool relocsOnly)
        {
            Builder = new ObjectDataBuilder(factory, relocsOnly);
            TargetRegister = new TargetRegisterMap(factory.Target.OperatingSystem);
        }

        public ObjectDataBuilder Builder;
        public TargetRegisterMap TargetRegister;

        // check the value length
        private static bool IsBitNumOverflow(int value, byte numBits)
        {
            return (value >> numBits) != 0;
        }

        private static bool IsLowReg(Register reg)
        {
            return !IsBitNumOverflow((int)reg, 3);
        }

        private static bool IsValidReg(Register reg)
        {
            return !IsBitNumOverflow((int)reg, 4);
        }

        // mov reg, immediate
        // reg range: [0..7]
        // immediage range: [0..255]
        public void EmitMOV(Register reg, int immediate)
        {
            Debug.Assert(IsLowReg(reg) && !IsBitNumOverflow(immediate, 8));
            Builder.EmitShort((short)(0x2000 + ((byte)reg << 8) + immediate));
        }

        // cmp reg, immediate
        // reg range: [0..7]
        // immediage range: [0..255]
        public void EmitCMP(Register reg, int immediate)
        {
            Debug.Assert(IsLowReg(reg) && !IsBitNumOverflow(immediate, 8));
            Builder.EmitShort((short)(0x2800 + ((byte)reg << 8) + immediate));
        }

        // add reg, immediate
        // reg range: [0..7]
        // immediage range: [0..255]
        public void EmitADD(Register reg, int immediate)
        {
            Debug.Assert(IsLowReg(reg) && !IsBitNumOverflow(immediate, 8));
            Builder.EmitShort((short)(0x3000 + ((byte)reg << 8) + immediate));
        }

        // sub reg, immediate
        // reg range: [0..7]
        // immediage range: [0..255]
        public void EmitSUB(Register reg, int immediate)
        {
            Debug.Assert(IsLowReg(reg) && !IsBitNumOverflow(immediate, 8));
            Builder.EmitShort((short)(0x3800 + ((byte)reg << 8) + immediate));
        }

        // nop
        public void EmitNOP()
        {
            Builder.EmitByte(0x00);
            Builder.EmitByte(0xbf);
        }

        // __debugbreak
        public void EmitDebugBreak()
        {
            Builder.EmitByte(0xde);
            Builder.EmitByte(0xfe);
        }

        // push reg
        // reg range: [0..12, LR]
        public void EmitPUSH(Register reg)
        {
            Debug.Assert(reg >= Register.R0 && (reg <= Register.R12 || reg == TargetRegister.LR));
            Builder.EmitByte(0x4d);
            Builder.EmitByte(0xf8);
            Builder.EmitShort((short)(0x0d04 + ((byte)reg << 12)));
        }

        // pop reg
        // reg range: [0..12, LR, PC]
        public void EmitPOP(Register reg)
        {
            Debug.Assert(IsValidReg(reg) && reg != TargetRegister.SP);
            Builder.EmitByte(0x5d);
            Builder.EmitByte(0xf8);
            Builder.EmitShort((short)(0x0b04 + ((byte)reg << 12)));
        }

        // mov reg, reg
        // reg range: [0..PC]
        public void EmitMOV(Register destination, Register source)
        {
            Debug.Assert(IsValidReg(destination) && IsValidReg(source));
            Builder.EmitShort((short)(0x4600 + (((byte)destination & 0x8) << 4) + (((byte)source & 0x8) << 3) + (((byte)source & 0x7) << 3) + ((byte)destination & 0x7)));
        }

        // ldr reg, [reg]
        // reg range: [0..7]
        public void EmitLDR(Register destination, Register source)
        {
            Debug.Assert(IsLowReg(destination) && IsLowReg(source));
            Builder.EmitShort((short)(0x6800 + (((byte)source & 0x7) << 3) + ((byte)destination & 0x7)));
        }

        // ldr.w reg, [reg, #offset]
        // reg range: [0..PC]
        // offset range: [-255..4095]
        //
        // for offset >= 4096 we do an expansion into:
        // add.w destination, source, #const
        // ldr.w destination, [destination, #offset]
        public void EmitLDR(Register destination, Register source, int offset)
        {
            Debug.Assert(IsValidReg(destination) && IsValidReg(source));

            if (offset >= 0x1000)
            {
                uint constVal = (uint)offset & ~0xfffu;
                uint mask32 = 0xff;
                uint imm8 = 0;
                int encode = 31; // 11111

                do
                {
                    mask32 <<= 1;
                    if ((constVal & ~mask32) == 0)
                    {
                        imm8 = (constVal & mask32) >> (32 - encode);
                        break;
                    }
                    encode--;
                } while (encode >= 8);

                Debug.Assert(encode >= 8);
                Debug.Assert((imm8 & 0x80) > 0);
                Builder.EmitShort((short)(0xF100 + (byte)source + (((byte)encode & 0x10) << 6)));
                Builder.EmitShort((short)((((byte)encode & 0xE) << 11) + ((byte)destination << 8) + (((byte)encode & 1) << 7) + (imm8 & 0x7f)));

                offset = (int)(offset & 0xfffu);
                source = destination;
            }

            Debug.Assert(offset >= -255 && offset <= 4095);
            if (offset >= 0)
            {
                Builder.EmitShort((short)(0xf8d0 + ((byte)(source))));
                Builder.EmitShort((short)(offset + (((byte)destination) << 12)));
            }
            else
            {
                Builder.EmitShort((short)(0xf850 + ((byte)(source))));
                Builder.EmitShort((short)(-offset + (((byte)destination) << 12) + (((byte)12) << 8)));
            }
        }

        // movw  reg, [reloc] & 0x0000FFFF
        // movt  reg, [reloc] & 0xFFFF0000
        // add   reg, pc
        // reg range: [0..12, LR]
        public void EmitMOV(Register destination, ISymbolNode symbol)
        {
            Debug.Assert(destination >= Register.R0 && (destination <= Register.R12 || destination == TargetRegister.LR));
            Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_THUMB_MOV32_PCREL); // 12-byte offset is part of the relocation
            Builder.EmitShort(unchecked((short)0xf240));
            Builder.EmitShort((short)((byte)destination << 8));
            Builder.EmitShort(unchecked((short)0xf2c0));
            Builder.EmitShort((short)((byte)destination << 8));
            if (destination <= Register.R7)
            {
                Builder.EmitShort(unchecked((short)(0x4478u + (byte)destination)));
            }
            else
            {
                Builder.EmitShort(unchecked((short)(0x44f0u + (byte)destination)));
            }
        }

        // b.w symbol
        public void EmitJMP(ISymbolNode symbol)
        {
            Debug.Assert(!symbol.RepresentsIndirectionCell);
            Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_THUMB_BRANCH24);
            Builder.EmitByte(0);
            Builder.EmitByte(0xF0);
            Builder.EmitByte(0);
            Builder.EmitByte(0xB8);
        }

        // bx reg
        // reg range: [0..PC]
        public void EmitJMP(Register destination)
        {
            Debug.Assert(IsValidReg(destination));
            Builder.EmitShort((short)(0x4700 + ((byte)destination << 3)));
        }

        // bx lr
        public void EmitRET()
        {
            EmitJMP(TargetRegister.LR);
        }

        // bne #offset
        // offset range: [-256..254]
        public void EmitBNE(int immediate)
        {
            Debug.Assert(immediate >= -256 && immediate <= 254);
            // considering the pipeline with PC
            immediate -= 4;

            Builder.EmitByte((byte)(immediate >> 1));
            Builder.EmitByte(0xD1);
        }

        // beq #offset
        // offset range: [-256..254]
        public void EmitBEQ(int immediate)
        {
            Debug.Assert(immediate >= -256 && immediate <= 254);
            // considering the pipeline with PC
            immediate -= 4;

            Builder.EmitByte((byte)(immediate >> 1));
            Builder.EmitByte(0xD0);
        }

        // bne label(+4): ret(2) + next(2)
        // bx lr
        // label: ...
        public void EmitRETIfEqual()
        {
            EmitBNE(4);
            EmitRET();
        }

        // beq label(+4): ret(2) + next(2)
        // bx lr
        // label: ...
        public void EmitRETIfNotEqual()
        {
            EmitBEQ(4);
            EmitRET();
        }
    }
}
