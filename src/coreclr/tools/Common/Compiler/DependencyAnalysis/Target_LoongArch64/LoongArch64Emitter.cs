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

        public void EmitMOV(Register regDst, ushort imm16)
        {
            Debug.Assert((uint)regDst <= 0x1f);
            Debug.Assert(imm16 <= 0xfff);
            uint instruction = 0x03800000u | (uint)((imm16 & 0xfff) << 10) | (uint)regDst;
            Builder.EmitUInt(instruction);
        }

        // pcaddi regDst, 0
        public void EmitPC(Register regDst)
        {
            Debug.Assert((uint)regDst > 0 && (uint)regDst < 32);
            Builder.EmitUInt(0x18000000 | (uint)regDst);
        }

        // ld_d regDst, regAddr, offset
        public void EmitLD(Register regDst, Register regSrc, int offset)
        {
            Debug.Assert(offset >= -2048 && offset <= 2047);

            Builder.EmitUInt((uint)(0x28c00000 | (uint)((offset & 0xfff) << 10) | ((uint)regSrc << 5) | (uint)regDst));
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
    }
}
