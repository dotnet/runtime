// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis.Riscv64
{
    public struct Riscv64Emitter
    {
        public Riscv64Emitter(NodeFactory factory, bool relocsOnly)
        {
            Builder = new ObjectDataBuilder(factory, relocsOnly);
            TargetRegister = new TargetRegisterMap(factory.Target.OperatingSystem);
        }

        public ObjectDataBuilder Builder;
        public TargetRegisterMap TargetRegister;

        // Assembly stub creation api. TBD, actually make this general purpose

        //ebreak
        public void EmitBreak()
        {
            Builder.EmitUInt(0x00100073);
        }

        public void EmitMOV(Register regDst, ushort imm16)
        {
            Debug.Assert((uint)regDst <= 0x1f);
            Debug.Assert(imm16 <= 0xfff);
            uint instruction = 0x00000013u | (uint)((imm16 & 0xfff) << 20) | (uint)((uint)regDst << 7);
            Builder.EmitUInt(instruction);
        }

        public void EmitMOV(Register regDst, Register regSrc)
        {
            Builder.EmitUInt((uint)(0x00000013 | ((uint)regSrc << 15) | (uint)regDst << 7 ));
        }

        public void EmitMOV(Register regDst, ISymbolNode symbol)
        {
            Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_RISCV64_PC);
            //auipc reg, off-hi-20bits
            Builder.EmitUInt(0x00000017u | (uint)regDst << 7);
            //addi reg, reg, off-lo-12bits
            Builder.EmitUInt(0x00000013u | ((uint)regDst << 7) | ((uint)regDst << 15));
        }

        // auipc regDst, 0
        public void EmitPC(Register regDst)
        {
            Debug.Assert((uint)regDst > 0 && (uint)regDst < 32);
            Builder.EmitUInt(0x00000017 | (uint)regDst << 7 );
        }

        // addi regDst, regSrc, imm12
        public void EmitADD(Register regDst, Register regSrc, int imm)
        {
            Debug.Assert((imm >= -2048) && (imm <= 2047));

            Builder.EmitUInt((uint)(0x00000013 | ((uint)regSrc << 15) | ((uint)regDst << 7) | (uint)((imm & 0xfff) << 20)));
        }

        // xori regDst, regSrc, imm12
        public void EmitXOR(Register regDst, Register regSrc, int imm)
        {
            Debug.Assert((imm >= 0) && (imm <= 0xfff));

            Builder.EmitUInt((uint)(0x00004013  | ((uint)regSrc << 15) | ((uint)regDst << 7) | (uint)((imm & 0xfff) << 20)));
        }

        // ld regDst, offset(regSrc)
        public void EmitLD(Register regDst, Register regSrc, int offset)
        {
            Debug.Assert((offset >= -2048) && (offset <= 2047));

            Builder.EmitUInt((uint)(0x00003003  | ((uint)regSrc << 15) | ((uint)regDst << 7) | (uint)((offset & 0xfff) << 20)));
        }

        public void EmitRET()
        {
            // jalr x0,x1,0
            Builder.EmitUInt(0x00008067);
        }

        public void EmitJMP(Register reg)
        {
            //jalr x0, reg, 0
            Builder.EmitUInt(0x00000067 | ((uint)reg << 15));
        }

        public void EmitJMP(ISymbolNode symbol)
        {
            if (symbol.RepresentsIndirectionCell)
            {
                // auipc t0, 0
                EmitPC(Register.X5);
                // ld a0, 16(t0)
                EmitLD(Register.X10, Register.X5, 16);
                // ld t0, 24(t0)
                EmitLD(Register.X5, Register.X5, 24);
                // jalr x0,x5,0
                Builder.EmitUInt(0x00028067);

                Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_DIR64);
            }
            else
            {
                //Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_RISCV64_PC);
                Builder.EmitUInt(0xffffffff); // bad code.
                throw new NotImplementedException();
            }
        }

        public void EmitRETIfEqual(Register regSrc)
        {
            // bne regSrc, x0, 8
            Builder.EmitUInt((uint)(0x00001463 | ((uint)regSrc << 15)));
            EmitRET();
        }

        public void EmitJE(Register regSrc, ISymbolNode symbol)
        {
            uint offset = symbol.RepresentsIndirectionCell ? 7u : 2u;

            // bne regSrc, x0, offset
            Builder.EmitUInt((uint)(0x00001063 |  ((uint)regSrc << 15) | (offset << 31)));

            EmitJMP(symbol);
        }
    }
}
