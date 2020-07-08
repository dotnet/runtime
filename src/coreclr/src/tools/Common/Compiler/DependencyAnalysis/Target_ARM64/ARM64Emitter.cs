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
        public void EmitMOV(Register regDst, ref AddrMode memory)
        {
            throw new NotImplementedException();
        }

        public void EmitMOV(Register regDst, Register regSrc)
        {
            throw new NotImplementedException();
        }

        public void EmitMOV(Register regDst, ushort imm16)
        {
            Debug.Assert((uint)regDst <= 0x1f);
            uint instruction = 0xd2800000u | ((uint)imm16 << 5) | (uint)regDst;
            Builder.EmitUInt(instruction);
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

        public void EmitLEAQ(Register reg, ISymbolNode symbol, int delta = 0)
        {
            throw new NotImplementedException();
        }

        public void EmitLEA(Register reg, ref AddrMode addrMode)
        {
            throw new NotImplementedException();
        }

        public void EmitCMP(ref AddrMode addrMode, sbyte immediate)
        {
            throw new NotImplementedException();
        }

        // add reg, immediate
        public void EmitADD(Register reg, byte immediate)
        {
            Builder.EmitInt((int)(0x91 << 24) | (immediate << 10) | ((byte)reg << 5) | (byte) reg);
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
                Builder.EmitByte(0);
                Builder.EmitByte(0);
                Builder.EmitByte(0);
                Builder.EmitByte(0x14);
            }
        }

        public void EmitINT3()
        {
            throw new NotImplementedException();
        }

        public void EmitJmpToAddrMode(ref AddrMode addrMode)
        {
            throw new NotImplementedException();
        }

        public void EmitRET()
        {
            throw new NotImplementedException();
        }

        public void EmitRETIfEqual()
        {
            throw new NotImplementedException();
        }

        private bool InSignedByteRange(int i)
        {
            return i == (int)(sbyte)i;
        }
    }
}
