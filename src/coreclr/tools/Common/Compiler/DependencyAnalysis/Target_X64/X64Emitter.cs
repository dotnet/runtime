// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis.X64
{
    public struct X64Emitter
    {
        public X64Emitter(NodeFactory factory, bool relocsOnly)
        {
            Builder = new ObjectDataBuilder(factory, relocsOnly);
            TargetRegister = new TargetRegisterMap(factory.Target.OperatingSystem);
        }

        public ObjectDataBuilder Builder;
        public TargetRegisterMap TargetRegister;

        // Assembly stub creation api. TBD, actually make this general purpose
        public void EmitMOV(Register regDst, ref AddrMode memory)
        {
            EmitIndirInstructionSize(0x8a, regDst, ref memory);
        }

        public void EmitMOV(Register regDst, Register regSrc)
        {
            AddrMode rexAddrMode = new AddrMode(regSrc, null, 0, 0, AddrModeSize.Int64);
            EmitRexPrefix(regDst, ref rexAddrMode);
            Builder.EmitByte(0x8B);
            Builder.EmitByte((byte)(0xC0 | (((int)regDst & 0x07) << 3) | (((int)regSrc & 0x07))));
        }

        public void EmitMOV(Register regDst, int imm32)
        {
            AddrMode rexAddrMode = new AddrMode(regDst, null, 0, 0, AddrModeSize.Int32);
            EmitRexPrefix(regDst, ref rexAddrMode);
            Builder.EmitByte((byte)(0xB8 | ((int)regDst & 0x07)));
            Builder.EmitInt(imm32);
        }

        public void EmitMOV(Register regDst, ISymbolNode node)
        {
            if (node.RepresentsIndirectionCell)
            {
                Builder.EmitByte(0x67);
                Builder.EmitByte(0x48);
                Builder.EmitByte(0x8B);
                Builder.EmitByte((byte)(0x00 | ((byte)regDst << 3) | 0x05));
                Builder.EmitReloc(node, RelocType.IMAGE_REL_BASED_REL32);
            }
            else
            {
                EmitLEAQ(regDst, node, delta: 0);
            }
        }

        public void EmitLEAQ(Register reg, ISymbolNode symbol, int delta = 0)
        {
            AddrMode rexAddrMode = new AddrMode(Register.RAX, null, 0, 0, AddrModeSize.Int64);
            EmitRexPrefix(reg, ref rexAddrMode);
            Builder.EmitByte(0x8D);
            Builder.EmitByte((byte)(0x05 | (((int)reg) & 0x07) << 3));
            Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_REL32, delta);
        }

        public void EmitLEA(Register reg, ref AddrMode addrMode)
        {
            Debug.Assert(addrMode.Size != AddrModeSize.Int8 &&
                addrMode.Size != AddrModeSize.Int16);
            EmitIndirInstruction(0x8D, reg, ref addrMode);
        }

        public void EmitCMP(ref AddrMode addrMode, sbyte immediate)
        {
            if (addrMode.Size == AddrModeSize.Int16)
                Builder.EmitByte(0x66);
            EmitIndirInstruction((byte)((addrMode.Size != AddrModeSize.Int8) ? 0x83 : 0x80), 0x7, ref addrMode);
            Builder.EmitByte((byte)immediate);
        }

        public void EmitADD(ref AddrMode addrMode, sbyte immediate)
        {
            if (addrMode.Size == AddrModeSize.Int16)
                Builder.EmitByte(0x66);
            EmitIndirInstruction((byte)((addrMode.Size != AddrModeSize.Int8) ? 0x83 : 0x80), (byte)0, ref addrMode);
            Builder.EmitByte((byte)immediate);
        }

        public void EmitJMP(ISymbolNode symbol)
        {
            if (symbol.RepresentsIndirectionCell)
            {
                Builder.EmitByte(0xff);
                Builder.EmitByte(0x25);
                Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_REL32);
            }
            else
            {
                Builder.EmitByte(0xE9);
                Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_REL32);

            }
        }

        public void EmitJE(ISymbolNode symbol)
        {
            if (symbol.RepresentsIndirectionCell)
            {
                throw new NotImplementedException();
            }
            else
            {
                Builder.EmitByte(0x0f);
                Builder.EmitByte(0x84);
                Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_REL32);
            }
        }

        public void EmitINT3()
        {
            Builder.EmitByte(0xCC);
        }

        public void EmitJmpToAddrMode(ref AddrMode addrMode)
        {
            EmitIndirInstruction(0xFF, 0x4, ref addrMode);
        }

        public void EmitPUSH(sbyte imm8)
        {
            Builder.EmitByte(0x6A);
            Builder.EmitByte(unchecked((byte)imm8));
        }

        public void EmitPUSH(ISymbolNode node)
        {
            if (node.RepresentsIndirectionCell)
            {
                // push [rip + relative node offset]
                Builder.EmitByte(0xFF);
                Builder.EmitByte(0x35);
                Builder.EmitReloc(node, RelocType.IMAGE_REL_BASED_REL32);
            }
            else
            {
                // push rax (arbitrary value)
                Builder.EmitByte(0x50);
                // lea rax, [rip + relative node offset]
                Builder.EmitByte(0x48);
                Builder.EmitByte(0x8D);
                Builder.EmitByte(0x05);
                Builder.EmitReloc(node, RelocType.IMAGE_REL_BASED_REL32);
                // xchg [rsp], rax; this also restores the previous value of rax
                Builder.EmitByte(0x48);
                Builder.EmitByte(0x87);
                Builder.EmitByte(0x04);
                Builder.EmitByte(0x24);
            }
        }

        public void EmitRET()
        {
            Builder.EmitByte(0xC3);
        }

        public void EmitRETIfEqual()
        {
            // jne @+1
            Builder.EmitByte(0x75);
            Builder.EmitByte(0x01);

            // ret
            Builder.EmitByte(0xC3);
        }

        public void EmitCompareToZero(Register reg)
        {
            AddrMode rexAddrMode = new AddrMode(Register.RegDirect | reg, null, 0, 0, AddrModeSize.Int64);
            EmitIndirInstructionSize(0x84, reg, ref rexAddrMode);
        }

        public void EmitZeroReg(Register reg)
        {
            // High 32 bits get cleared automatically when using 32bit registers
            AddrMode rexAddrMode = new AddrMode(reg, null, 0, 0, AddrModeSize.Int32);
            EmitRexPrefix(reg, ref rexAddrMode);
            Builder.EmitByte(0x33);
            Builder.EmitByte((byte)(0xC0 | (((int)reg & 0x07) << 3) | ((int)reg & 0x07)));
        }

        private static bool InSignedByteRange(int i)
        {
            return i == (int)(sbyte)i;
        }

        private void EmitImmediate(int immediate, int size)
        {
            switch (size)
            {
                case 0:
                    break;
                case 1:
                    Builder.EmitByte((byte)immediate);
                    break;
                case 2:
                    Builder.EmitShort((short)immediate);
                    break;
                case 4:
                    Builder.EmitInt(immediate);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void EmitModRM(byte subOpcode, ref AddrMode addrMode)
        {
            byte modRM = (byte)((subOpcode & 0x07) << 3);
            if (addrMode.BaseReg > Register.None)
            {
                Debug.Assert(addrMode.BaseReg >= Register.RegDirect);

                Register reg = (Register)(addrMode.BaseReg - Register.RegDirect);
                Builder.EmitByte((byte)(0xC0 | modRM | ((int)reg & 0x07)));
            }
            else
            {
                byte lowOrderBitsOfBaseReg = (byte)((int)addrMode.BaseReg & 0x07);
                modRM |= lowOrderBitsOfBaseReg;
                int offsetSize;
                if (addrMode.Offset == 0 && (lowOrderBitsOfBaseReg != (byte)Register.RBP))
                {
                    offsetSize = 0;
                }
                else if (InSignedByteRange(addrMode.Offset))
                {
                    offsetSize = 1;
                    modRM |= 0x40;
                }
                else
                {
                    offsetSize = 4;
                    modRM |= 0x80;
                }

                bool emitSibByte = false;
                Register sibByteBaseRegister = addrMode.BaseReg;

                if (addrMode.BaseReg == Register.None)
                {
                    //# ifdef _TARGET_AMD64_
                    // x64 requires SIB to avoid RIP relative address
                    emitSibByte = true;
                    //#else
                    //                    emitSibByte = (addrMode.m_indexReg != MDIL_REG_NO_INDEX);
                    //#endif

                    modRM &= 0x38;    // set Mod bits to 00 and clear out base reg
                    offsetSize = 4;   // this forces 32-bit displacement

                    if (emitSibByte)
                    {
                        // EBP in SIB byte means no base
                        // ModRM base register forced to ESP in SIB code below
                        sibByteBaseRegister = Register.RBP;
                    }
                    else
                    {
                        // EBP in ModRM means no base
                        modRM |= (byte)(Register.RBP);
                    }
                }
                else if (lowOrderBitsOfBaseReg == (byte)Register.RSP || addrMode.IndexReg.HasValue)
                {
                    emitSibByte = true;
                }

                if (!emitSibByte)
                {
                    Builder.EmitByte(modRM);
                }
                else
                {
                    // MDIL_REG_ESP as the base is the marker that there is a SIB byte
                    modRM = (byte)((modRM & 0xF8) | (int)Register.RSP);
                    Builder.EmitByte(modRM);

                    int indexRegAsInt = (int)(addrMode.IndexReg ?? Register.RSP);

                    Builder.EmitByte((byte)((addrMode.Scale << 6) + ((indexRegAsInt & 0x07) << 3) + ((int)sibByteBaseRegister & 0x07)));
                }
                EmitImmediate(addrMode.Offset, offsetSize);
            }
        }

        private void EmitExtendedOpcode(int opcode)
        {
            if ((opcode >> 16) != 0)
            {
                if ((opcode >> 24) != 0)
                {
                    Builder.EmitByte((byte)(opcode >> 24));
                }
                Builder.EmitByte((byte)(opcode >> 16));
            }
            Builder.EmitByte((byte)(opcode >> 8));
        }

        private void EmitRexPrefix(Register reg, ref AddrMode addrMode)
        {
            byte rexPrefix = 0;

            // Check the situations where a REX prefix is needed

            // Are we accessing a byte register that wasn't byte accessible in x86?
            if (addrMode.Size == AddrModeSize.Int8 && reg >= Register.RSP)
            {
                rexPrefix |= 0x40; // REX - access to new 8-bit registers
            }

            // Is this a 64 bit instruction?
            if (addrMode.Size == AddrModeSize.Int64)
            {
                rexPrefix |= 0x48; // REX.W - 64-bit data operand
            }

            // Is the destination register one of the new ones?
            if (reg >= Register.R8)
            {
                rexPrefix |= 0x44; // REX.R - extension of the register field
            }

            // Is the index register one of the new ones?
            if (addrMode.IndexReg.HasValue && addrMode.IndexReg.Value >= Register.R8 && addrMode.IndexReg.Value <= Register.R15)
            {
                rexPrefix |= 0x42; // REX.X - extension of the SIB index field
            }

            // Is the base register one of the new ones?
            if (addrMode.BaseReg >= Register.R8 && addrMode.BaseReg <= Register.R15
               || addrMode.BaseReg >= (int)Register.R8 + Register.RegDirect && addrMode.BaseReg <= (int)Register.R15 + Register.RegDirect)
            {
                rexPrefix |= 0x41; // REX.WB (Wide, extended Base)
            }

            // If we have anything so far, emit it.
            if (rexPrefix != 0)
            {
                Builder.EmitByte(rexPrefix);
            }
        }

        private void EmitIndirInstruction(int opcode, byte subOpcode, ref AddrMode addrMode)
        {
            EmitRexPrefix(Register.RAX, ref addrMode);
            if ((opcode >> 8) != 0)
            {
                EmitExtendedOpcode(opcode);
            }
            Builder.EmitByte((byte)opcode);
            EmitModRM(subOpcode, ref addrMode);
        }

        private void EmitIndirInstruction(int opcode, Register dstReg, ref AddrMode addrMode)
        {
            EmitRexPrefix(dstReg, ref addrMode);
            if ((opcode >> 8) != 0)
            {
                EmitExtendedOpcode(opcode);
            }
            Builder.EmitByte((byte)opcode);
            EmitModRM((byte)((int)dstReg & 0x07), ref addrMode);
        }

        private void EmitIndirInstructionSize(int opcode, Register dstReg, ref AddrMode addrMode)
        {
            //# ifndef _TARGET_AMD64_
            // assert that ESP, EBP, ESI, EDI are not accessed as bytes in 32-bit mode
            //            Debug.Assert(!(addrMode.Size == AddrModeSize.Int8 && dstReg > Register.RBX));
            //#endif
            Debug.Assert(addrMode.Size != 0);
            if (addrMode.Size == AddrModeSize.Int16)
                Builder.EmitByte(0x66);
            EmitIndirInstruction(opcode + ((addrMode.Size != AddrModeSize.Int8) ? 1 : 0), dstReg, ref addrMode);
        }
    }
}
