// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis.RiscV64
{
    public struct RiscV64Emitter
    {
        public RiscV64Emitter(NodeFactory factory, bool relocsOnly)
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

        public void EmitLI(Register regDst, int offset)
        {
            Debug.Assert((offset >= -2048) && (offset <= 2047));
            EmitADDI(regDst, Register.X0, offset);
        }

        public void EmitMOV(Register regDst, Register regSrc)
        {
            EmitADDI(regDst, regSrc, 0);
        }

        public void EmitMOV(Register regDst, ISymbolNode symbol)
        {
            Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_RISCV64_PC);
            //auipc reg, off-hi-20bits
            EmitPC(regDst);
            //addi reg, reg, off-lo-12bits
            EmitADDI(regDst, regDst, 0);
        }

        // auipc regDst, 0
        public void EmitPC(Register regDst)
        {
            Debug.Assert((uint)regDst > 0 && (uint)regDst < 32);
            Builder.EmitUInt(0x00000017u | (uint)regDst << 7);
        }

        // addi regDst, regSrc, offset
        public void EmitADDI(Register regDst, Register regSrc, int offset)
        {
            Debug.Assert((uint)regDst <= 0x1f);
            Debug.Assert((uint)regSrc <= 0x1f);
            Debug.Assert((offset >= -2048) && (offset <= 2047));
            Builder.EmitUInt((uint)(0x00000013u | ((uint)regSrc << 15) | ((uint)regDst << 7) | (uint)((offset & 0xfff) << 20)));
        }

        // xori regDst, regSrc, offset
        public void EmitXORI(Register regDst, Register regSrc, int offset)
        {
            Debug.Assert((offset >= -2048) && (offset <= 2047));
            Builder.EmitUInt((uint)(0x00004013u | ((uint)regSrc << 15) | ((uint)regDst << 7) | (uint)((offset & 0xfff) << 20)));
        }

        // ld regDst, offset(regSrc)
        public void EmitLD(Register regDst, Register regSrc, int offset)
        {
            Debug.Assert((offset >= -2048) && (offset <= 2047));
            Builder.EmitUInt((uint)(0x00003003u | ((uint)regSrc << 15) | ((uint)regDst << 7) | (uint)((offset & 0xfff) << 20)));
        }

        // jalr regDst, offset(regSrc)
        public void EmitJALR(Register regDst, Register regSrc, int offset)
        {
            Debug.Assert((offset >= -2048) && (offset <= 2047));
            Builder.EmitUInt((uint)(0x00000067u | ((uint)regSrc << 15) | ((uint)regDst << 7) | (uint)((offset & 0xfff) << 20)));
        }

        public void EmitRET()
        {
            // jalr x0,0(x1)
            EmitJALR(Register.X0, Register.X1, 0);
        }

        public void EmitJMP(Register reg)
        {
            //jalr x0, 0(reg)
            EmitJALR(Register.X0, reg, 0);
        }

        public void EmitJMP(ISymbolNode symbol)
        {
            if (symbol.RepresentsIndirectionCell)
            {
                //auipc x29, 0
                EmitPC(Register.X29);
                //ld x29,16(x29)
                EmitLD(Register.X29, Register.X29, 16);
                //ld x29,0(x29)
                EmitLD(Register.X29, Register.X29, 0);
                //jalr x0,0(x29)
                EmitJALR(Register.X0, Register.X29, 0);

                Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_DIR64);
            }
            else
            {
                Builder.EmitUInt(0x00000000); // bad code.
                throw new NotImplementedException();
            }
        }

        public void EmitRETIfZero(Register regSrc)
        {
            // bne regSrc, x0, 8
            Builder.EmitUInt((uint)(0x00001463 | ((uint)regSrc << 15)));
            EmitRET();
        }

        public void EmitJMPIfZero(Register regSrc, ISymbolNode symbol)
        {
            uint offset = symbol.RepresentsIndirectionCell ? 28u : 8u;
            uint encodedOffset = ((offset & 0x1e) << 7) | ((offset & 0x7e0) << 20) | ((offset & 0x800) >> 4) | ((offset & 0x1000) << 19);
            // bne regSrc, x0, offset
            Builder.EmitUInt((uint)(0x00001063 | ((uint)regSrc << 15) | encodedOffset));
            EmitJMP(symbol);
        }
    }
}
