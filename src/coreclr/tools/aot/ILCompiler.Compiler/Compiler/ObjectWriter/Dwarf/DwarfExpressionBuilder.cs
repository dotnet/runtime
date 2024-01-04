// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using Internal.TypeSystem;
using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    internal ref struct DwarfExpressionBuilder
    {
        private readonly TargetArchitecture _architecture;
        private readonly byte _targetPointerSize;
        private readonly IBufferWriter<byte> _writer;

        public DwarfExpressionBuilder(TargetArchitecture architecture, byte targetPointerSize, IBufferWriter<byte> writer)
        {
            _architecture = architecture;
            _targetPointerSize = targetPointerSize;
            _writer = writer;
        }

        public void OpReg(int register) => OpDwarfReg(DwarfRegNum(_architecture, register));

        public void OpBReg(int register, int offset = 0) => OpBDwarfReg(DwarfRegNum(_architecture, register), offset);

        public void OpDwarfReg(int register)
        {
            if (register <= 31)
            {
                OpCode((byte)(DW_OP_reg0 + register));
            }
            else
            {
                OpCode(DW_OP_regx);
                AppendULEB128((ulong)register);
            }
        }

        public void OpBDwarfReg(int register, int offset = 0)
        {
            if (register <= 31)
            {
                OpCode((byte)(DW_OP_breg0 + register));
            }
            else
            {
                OpCode(DW_OP_bregx);
                AppendULEB128((ulong)register);
            }
            AppendSLEB128(offset);
        }

        public void OpDeref() => OpCode(DW_OP_deref);

        public void OpPiece(uint size = 0)
        {
            OpCode(DW_OP_piece);
            AppendULEB128(size == 0 ? (uint)_targetPointerSize : size);
        }

        private void OpCode(byte opcode)
        {
            var b = _writer.GetSpan(1);
            b[0] = opcode;
            _writer.Advance(1);
        }

        private void AppendULEB128(ulong value) => DwarfHelper.WriteULEB128(_writer, value);

        private void AppendSLEB128(long value) => DwarfHelper.WriteSLEB128(_writer, value);

        private enum RegNumX86 : int
        {
            REGNUM_EAX,
            REGNUM_ECX,
            REGNUM_EDX,
            REGNUM_EBX,
            REGNUM_ESP,
            REGNUM_EBP,
            REGNUM_ESI,
            REGNUM_EDI,
            REGNUM_COUNT,
            REGNUM_FP = REGNUM_EBP,
            REGNUM_SP = REGNUM_ESP
        };

        private enum RegNumAmd64 : int
        {
            REGNUM_RAX,
            REGNUM_RCX,
            REGNUM_RDX,
            REGNUM_RBX,
            REGNUM_RSP,
            REGNUM_RBP,
            REGNUM_RSI,
            REGNUM_RDI,
            REGNUM_R8,
            REGNUM_R9,
            REGNUM_R10,
            REGNUM_R11,
            REGNUM_R12,
            REGNUM_R13,
            REGNUM_R14,
            REGNUM_R15,
            REGNUM_COUNT,
            REGNUM_SP = REGNUM_RSP,
            REGNUM_FP = REGNUM_RBP
        };

        public static int DwarfRegNum(TargetArchitecture architecture, int regNum)
        {
            switch (architecture)
            {
                case TargetArchitecture.ARM64:
                    // Normal registers are directly mapped
                    if (regNum >= 33)
                        regNum = regNum - 33 + 64; // FP
                    return regNum;

                case TargetArchitecture.ARM:
                    // Normal registers are directly mapped
                    if (regNum >= 16)
                        regNum = ((regNum - 16) / 2) + 256; // FP
                    return regNum;

                case TargetArchitecture.X64:
                    return (RegNumAmd64)regNum switch
                    {
                        RegNumAmd64.REGNUM_RAX => 0,
                        RegNumAmd64.REGNUM_RDX => 1,
                        RegNumAmd64.REGNUM_RCX => 2,
                        RegNumAmd64.REGNUM_RBX => 3,
                        RegNumAmd64.REGNUM_RSI => 4,
                        RegNumAmd64.REGNUM_RDI => 5,
                        RegNumAmd64.REGNUM_RBP => 6,
                        RegNumAmd64.REGNUM_RSP => 7,
                        RegNumAmd64.REGNUM_R8 => 8,
                        RegNumAmd64.REGNUM_R9 => 9,
                        RegNumAmd64.REGNUM_R10 => 10,
                        RegNumAmd64.REGNUM_R11 => 11,
                        RegNumAmd64.REGNUM_R12 => 12,
                        RegNumAmd64.REGNUM_R13 => 13,
                        RegNumAmd64.REGNUM_R14 => 14,
                        RegNumAmd64.REGNUM_R15 => 15,
                        _ => regNum - (int)RegNumAmd64.REGNUM_COUNT + 17 // FP registers
                    };

                case TargetArchitecture.X86:
                    return (RegNumX86)regNum switch
                    {
                        RegNumX86.REGNUM_EAX => 0,
                        RegNumX86.REGNUM_ECX => 1,
                        RegNumX86.REGNUM_EDX => 2,
                        RegNumX86.REGNUM_EBX => 3,
                        RegNumX86.REGNUM_ESP => 4,
                        RegNumX86.REGNUM_EBP => 5,
                        RegNumX86.REGNUM_ESI => 6,
                        RegNumX86.REGNUM_EDI => 7,
                        _ => regNum - (int)RegNumX86.REGNUM_COUNT + 32 // FP registers
                    };

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
