// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis.X86
{
    public struct X86Emitter
    {
        public X86Emitter(NodeFactory factory, bool relocsOnly)
        {
            Builder = new ObjectDataBuilder(factory, relocsOnly);
            TargetRegister = new TargetRegisterMap(factory.Target.OperatingSystem);
        }

        public ObjectDataBuilder Builder;
        public TargetRegisterMap TargetRegister;

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
                Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_HIGHLOW);
            }
            else
            {
                Builder.EmitByte(0xE9);
                Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_REL32);
            }
        }

        public void EmitXOR(Register register1, Register register2)
        {
            Builder.EmitByte(0x33);
            Builder.EmitByte((byte)(0xC0 | ((byte)register1 << 3) | (byte)register2));
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
                // push [node address]
                Builder.EmitByte(0xFF);
                Builder.EmitByte(0x35);
            }
            else
            {
                // push <node address>
                Builder.EmitByte(0x68);
            }
            Builder.EmitReloc(node, RelocType.IMAGE_REL_BASED_HIGHLOW);
        }

        public void EmitMOV(Register regDst, Register regSrc)
        {
            Builder.EmitByte(0x8B);
            Builder.EmitByte((byte)(0xC0 | (((int)regDst & 0x07) << 3) | (((int)regSrc & 0x07))));
        }

        public void EmitMOV(Register register, ISymbolNode node, int delta = 0)
        {
            if (node.RepresentsIndirectionCell)
            {
                // mov register, [node address]
                Builder.EmitByte(0x8B);
                Builder.EmitByte((byte)(0x00 | ((byte)register << 3) | 0x5));
            }
            else
            {
                // mov register, immediate
                Builder.EmitByte((byte)(0xB8 + (byte)register));
            }
            Builder.EmitReloc(node, RelocType.IMAGE_REL_BASED_HIGHLOW, delta);
        }

        public void EmitINT3()
        {
            Builder.EmitByte(0xCC);
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
                if (addrMode.Offset == 0 && (lowOrderBitsOfBaseReg != (byte)Register.EBP))
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
                    emitSibByte = (addrMode.IndexReg != Register.NoIndex);
                    modRM &= 0x38;    // set Mod bits to 00 and clear out base reg
                    offsetSize = 4;   // this forces 32-bit displacement

                    if (emitSibByte)
                    {
                        // EBP in SIB byte means no base
                        // ModRM base register forced to ESP in SIB code below
                        sibByteBaseRegister = Register.EBP;
                    }
                    else
                    {
                        // EBP in ModRM means no base
                        modRM |= (byte)(Register.EBP);
                    }
                }
                else if (lowOrderBitsOfBaseReg == (byte)Register.ESP || addrMode.IndexReg.HasValue)
                {
                    emitSibByte = true;
                }

                if (!emitSibByte)
                {
                    Builder.EmitByte(modRM);
                }
                else
                {
                    modRM = (byte)((modRM & 0xF8) | (int)Register.ESP);
                    Builder.EmitByte(modRM);
                    int indexRegAsInt = (int)(addrMode.IndexReg ?? Register.ESP);
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

        private void EmitIndirInstruction(int opcode, byte subOpcode, ref AddrMode addrMode)
        {
            if ((opcode >> 8) != 0)
            {
                EmitExtendedOpcode(opcode);
            }
            Builder.EmitByte((byte)opcode);
            EmitModRM(subOpcode, ref addrMode);
        }
    }
}
