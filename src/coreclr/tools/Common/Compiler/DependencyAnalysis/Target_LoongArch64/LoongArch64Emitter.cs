// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis.LoongArch64
{
    public struct LoongArch64Emitter
    {
        public LoongArch64Emitter(NodeFactory factory, bool relocsOnly)
        {
            Builder = new ObjectDataBuilder(factory, relocsOnly);
            TargetRegister = new TargetRegisterMap(factory.Target.OperatingSystem);
        }

        public ObjectDataBuilder Builder;
        public TargetRegisterMap TargetRegister;

        // Assembly stub creation api. TBD, actually make this general purpose

        public void EmitBreak()
        {
            Builder.EmitUInt(0x002a0005);
        }

        public void EmitMOV(Register regDst, ushort imm16)
        {
            Debug.Assert((uint)regDst <= 0x1f);
            Debug.Assert(imm16 <= 0xfff);
            uint instruction = 0x03800000u | (uint)((imm16 & 0xfff) << 10) | (uint)regDst;
            Builder.EmitUInt(instruction);
        }

        public void EmitMOV(Register regDst, Register regSrc)
        {
            Builder.EmitUInt((uint)(0x03800000 | ((uint)regSrc << 5) | (uint)regDst));
        }

        public void EmitMOV(Register regDst, ISymbolNode symbol)
        {
            Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_LOONGARCH64_PC);
            // pcaddu12i  reg, off-hi-20bits
            Builder.EmitUInt(0x1c000000u | (uint)regDst);

            // addi_d  reg, reg, off-lo-12bits
            Builder.EmitUInt(0x02c00000u | (uint)(((uint)regDst << 5) | (uint)regDst));
        }

        // pcaddi regDst, 0
        public void EmitPC(Register regDst)
        {
            Debug.Assert((uint)regDst > 0 && (uint)regDst < 32);
            Builder.EmitUInt(0x18000000 | (uint)regDst);
        }

        // addi.d regDst, regSrc, imm12
        public void EmitADD(Register regDst, Register regSrc, int imm)
        {
            Debug.Assert((imm >= -2048) && (imm <= 2047));

            Builder.EmitUInt((uint)(0x02c00000 | (uint)((imm & 0xfff) << 10) | ((uint)regSrc << 5) | (uint)regDst));
        }

        // xori regDst, regSrc, imm12
        public void EmitXOR(Register regDst, Register regSrc, int imm)
        {
            Debug.Assert((imm >= 0) && (imm <= 0xfff));

            Builder.EmitUInt((uint)(0x03c00000 | (uint)((imm & 0xfff) << 10) | ((uint)regSrc << 5) | (uint)regDst));
        }

        // ld_d regDst, regAddr, offset
        public void EmitLD(Register regDst, Register regSrc, int offset)
        {
            Debug.Assert((offset >= -2048) && (offset <= 2047));

            Builder.EmitUInt((uint)(0x28c00000 | (uint)((offset & 0xfff) << 10) | ((uint)regSrc << 5) | (uint)regDst));
        }

        public void EmitRET()
        {
            // jirl R0,R1,0
            Builder.EmitUInt(0x4c000020);
        }

        public void EmitJMP(Register reg)
        {
            Builder.EmitUInt(0x4c000000u | ((uint)reg << 5));
        }

        public void EmitJMP(ISymbolNode symbol)
        {
            if (symbol.RepresentsIndirectionCell)
            {
                // pcaddi R21, 0
                EmitPC(Register.R21);

                EmitLD(Register.R21, Register.R21, 0x10);

                // ld_d R21, R21, 0
                EmitLD(Register.R21, Register.R21, 0);

                // jirl R0,R21,0
                Builder.EmitUInt(0x4c0002a0);

                Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_DIR64);
            }
            else
            {
                //Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_LOONGARCH64_PC);
                Builder.EmitUInt(0xffffffff); // bad code.
                throw new NotImplementedException();
            }
        }

        public void EmitRETIfEqual(Register regSrc)
        {
            // BNEZ regSrc, 8
            Builder.EmitUInt((uint)(0x44000000 | (2 << 10) | ((uint)regSrc << 5)));
            EmitRET();
        }

        public void EmitJE(Register regSrc, ISymbolNode symbol)
        {
            uint offset = symbol.RepresentsIndirectionCell ? 7u : 2u;

            // BNEZ regSrc, offset
            Builder.EmitUInt((uint)(0x44000000 | (offset << 10) | ((uint)regSrc << 5)));

            EmitJMP(symbol);
        }
    }
}
