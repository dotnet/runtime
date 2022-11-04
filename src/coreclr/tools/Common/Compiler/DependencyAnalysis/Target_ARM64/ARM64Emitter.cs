// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis.ARM64
{
    public struct ARM64Emitter
    {
        public ARM64Emitter(NodeFactory factory, bool relocsOnly)
        {
            Builder = new ObjectDataBuilder(factory, relocsOnly);
            TargetRegister = new TargetRegisterMap(factory.Target.OperatingSystem);
        }

        public ObjectDataBuilder Builder;
        public TargetRegisterMap TargetRegister;

        // Assembly stub creation api. TBD, actually make this general purpose

        public void EmitMOV(Register regDst, Register regSrc)
        {
            Builder.EmitUInt((uint)(0b1_0_1_01010_000_00000_000000_11111_00000u | ((uint)regSrc << 16) | (uint)regDst));
        }

        public void EmitMOV(Register regDst, ushort imm16)
        {
            Debug.Assert((uint)regDst <= 0x1f);
            uint instruction = 0xd2800000u | ((uint)imm16 << 5) | (uint)regDst;
            Builder.EmitUInt(instruction);
        }

        public void EmitMOV(Register regDst, ISymbolNode symbol)
        {
            // ADRP regDst, [symbol (21bit ADRP thing)]
            Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21);
            Builder.EmitUInt(0x90000000u | (byte)regDst);

            // Add regDst, (12bit LDR page offset reloc)
            Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A);
            Builder.EmitUInt((uint)(0b1_0_0_100010_0_000000000000_00000_00000 | ((byte)regDst << 5) | (byte)regDst));
        }

        // ldr regDst, [PC + imm19]
        public void EmitLDR(Register regDst, short offset)
        {
            Debug.Assert((uint)regDst <= 0x1f);
            Debug.Assert((offset & 3) == 0);
            // Sign-extend offset and take 19 bits
            uint instruction = 0x58000000 | ((uint)((int)offset & 0x1ffffc) << 3) | (uint)regDst;
            Builder.EmitUInt(instruction);
        }

        // ldr regDst, [regAddr]
        public void EmitLDR(Register regDst, Register regAddr)
        {
            Debug.Assert((uint)regDst <= 0x1f);
            Debug.Assert((uint)regAddr <= 0x1f);
            uint instruction = 0xf9400000 | ((uint)regAddr << 5) | (uint)regDst;
            Builder.EmitUInt(instruction);
        }

        public void EmitLDR(Register regDst, Register regSrc, int offset)
        {
            if (offset >= 0 && offset <= 32760)
            {
                Debug.Assert(offset % 8 == 0);

                offset /= 8;

                Builder.EmitUInt((uint)(0b11_1110_0_1_0_1_000000000000_00000_00000u | ((uint)offset << 10) | ((uint)regSrc << 5) | (uint)regDst));
            }
            else if (offset >= -255 && offset < 0)
            {
                uint o = (uint)offset & 0x1FF;

                Builder.EmitUInt((uint)(0b11_1110_0_0_010_000000000_1_1_00000_00000u | (o << 12) | ((uint)regSrc << 5) | (uint)regDst));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void EmitCMP(Register reg, sbyte immediate)
        {
            if (immediate >= 0)
            {
                Builder.EmitUInt((uint)(0b1_1_1_100010_0_000000000000_00000_11111u | immediate << 10) | ((uint)reg << 5));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        // add reg, immediate
        public void EmitADD(Register reg, byte immediate)
        {
            Builder.EmitInt((int)(0x91 << 24) | (immediate << 10) | ((byte)reg << 5) | (byte) reg);
        }

        public void EmitSUB(Register reg, int immediate)
        {
            if (immediate >= 0)
            {
                Debug.Assert(immediate % 4 == 0);

                Builder.EmitUInt((uint)(0b1_1_0_100010_0_000000000000_00000_00000u | immediate << 10) | ((uint)reg << 5) | (uint)reg);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void EmitSUB(Register regDst, Register regSrc, int immediate)
        {
            if (immediate >= 0)
            {
                Debug.Assert(immediate % 4 == 0);

                Builder.EmitUInt((uint)(0b1_1_0_100010_0_000000000000_00000_00000u | immediate << 10) | ((uint)regSrc << 5) | (uint)regDst);
            }
            else
            {
                throw new NotImplementedException();
            }
        }


        public void EmitJMP(ISymbolNode symbol)
        {
            if (symbol.RepresentsIndirectionCell)
            {
                // ldr x12, [PC+0xc]
                EmitLDR(Register.X12, 0xc);

                // ldr x12, [x12]
                EmitLDR(Register.X12, Register.X12);

                // br x12
                Builder.EmitUInt(0xd61f0180);

                Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_DIR64);
            }
            else
            {
                Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_ARM64_BRANCH26);
                Builder.EmitUInt(0x14000000);
            }
        }

        public void EmitJMP(Register reg)
        {
            Builder.EmitUInt((uint)(0b11010110_0_0_0_11111_00000_0_00000_00000u | ((uint)reg << 5)));
        }

        public void EmitINT3()
        {
            Builder.EmitUInt(0b11010100_001_1111111111111111_000_0_0);
        }

        public void EmitRET()
        {
            Builder.EmitUInt(0b11010110_0_1_0_11111_00000_0_11110_00000);
        }

        public void EmitRETIfEqual()
        {
            Builder.EmitUInt(0b01010100_0000000000000000010_0_0001u);
            EmitRET();
        }

        public void EmitJE(ISymbolNode symbol)
        {
            uint offset = symbol.RepresentsIndirectionCell ? 6u : 2u;

            Builder.EmitUInt(0b01010100_0000000000000000000_0_0001u | offset << 5);

            EmitJMP(symbol);
        }

        private static bool InSignedByteRange(int i)
        {
            return i == (int)(sbyte)i;
        }
    }
}
