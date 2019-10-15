// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code adapted from https://blogs.msdn.microsoft.com/haibo_luo/2010/04/19/ilvisualizer-2010-solution

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Linq.Expressions.Tests
{
    public sealed class ILReader : IEnumerable<ILInstruction>, IEnumerable
    {
        private static readonly Type s_runtimeMethodInfoType = Type.GetType("System.Reflection.RuntimeMethodInfo");
        private static readonly Type s_runtimeConstructorInfoType = Type.GetType("System.Reflection.RuntimeConstructorInfo");

        private static readonly OpCode[] s_OneByteOpCodes;
        private static readonly OpCode[] s_TwoByteOpCodes;

        static ILReader()
        {
            s_OneByteOpCodes = new OpCode[0x100];
            s_TwoByteOpCodes = new OpCode[0x100];

            foreach (FieldInfo fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                OpCode opCode = (OpCode)fi.GetValue(null);
                ushort value = unchecked((ushort)opCode.Value);
                if (value < 0x100)
                {
                    s_OneByteOpCodes[value] = opCode;
                }
                else if ((value & 0xff00) == 0xfe00)
                {
                    s_TwoByteOpCodes[value & 0xff] = opCode;
                }
            }
        }

        private int _position;
        private readonly byte[] _byteArray;

        public ILReader(IILProvider ilProvider, ITokenResolver tokenResolver)
        {
            if (ilProvider == null)
            {
                throw new ArgumentNullException(nameof(ilProvider));
            }

            Resolver = tokenResolver;
            ILProvider = ilProvider;
            _byteArray = ILProvider.GetByteArray();
            _position = 0;
        }

        public IILProvider ILProvider { get; }
        public ITokenResolver Resolver { get; }

        public IEnumerator<ILInstruction> GetEnumerator()
        {
            while (_position < _byteArray.Length)
                yield return Next();

            _position = 0;
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private ILInstruction Next()
        {
            int offset = _position;
            OpCode opCode = OpCodes.Nop;
            int token = 0;

            // read first 1 or 2 bytes as opCode
            byte code = ReadByte();
            if (code != 0xFE)
            {
                opCode = s_OneByteOpCodes[code];
            }
            else
            {
                code = ReadByte();
                opCode = s_TwoByteOpCodes[code];
            }

            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    return new InlineNoneInstruction(offset, opCode);

                //The operand is an 8-bit integer branch target.
                case OperandType.ShortInlineBrTarget:
                    sbyte shortDelta = ReadSByte();
                    return new ShortInlineBrTargetInstruction(offset, opCode, shortDelta);

                //The operand is a 32-bit integer branch target.
                case OperandType.InlineBrTarget:
                    int delta = ReadInt32();
                    return new InlineBrTargetInstruction(offset, opCode, delta);

                //The operand is an 8-bit integer: 001F  ldc.i4.s, FE12  unaligned.
                case OperandType.ShortInlineI:
                    sbyte int8 = ReadSByte();
                    return new ShortInlineIInstruction(offset, opCode, int8);

                //The operand is a 32-bit integer.
                case OperandType.InlineI:
                    int int32 = ReadInt32();
                    return new InlineIInstruction(offset, opCode, int32);

                //The operand is a 64-bit integer.
                case OperandType.InlineI8:
                    long int64 = ReadInt64();
                    return new InlineI8Instruction(offset, opCode, int64);

                //The operand is a 32-bit IEEE floating point number.
                case OperandType.ShortInlineR:
                    float float32 = ReadSingle();
                    return new ShortInlineRInstruction(offset, opCode, float32);

                //The operand is a 64-bit IEEE floating point number.
                case OperandType.InlineR:
                    double float64 = ReadDouble();
                    return new InlineRInstruction(offset, opCode, float64);

                //The operand is an 8-bit integer containing the ordinal of a local variable or an argument
                case OperandType.ShortInlineVar:
                    byte index8 = ReadByte();
                    return new ShortInlineVarInstruction(offset, opCode, index8);

                //The operand is 16-bit integer containing the ordinal of a local variable or an argument.
                case OperandType.InlineVar:
                    ushort index16 = ReadUInt16();
                    return new InlineVarInstruction(offset, opCode, index16);

                //The operand is a 32-bit metadata string token.
                case OperandType.InlineString:
                    token = ReadInt32();
                    return new InlineStringInstruction(offset, opCode, token, Resolver);

                //The operand is a 32-bit metadata signature token.
                case OperandType.InlineSig:
                    token = ReadInt32();
                    return new InlineSigInstruction(offset, opCode, token, Resolver);

                //The operand is a 32-bit metadata token.
                case OperandType.InlineMethod:
                    token = ReadInt32();
                    return new InlineMethodInstruction(offset, opCode, token, Resolver);

                //The operand is a 32-bit metadata token.
                case OperandType.InlineField:
                    token = ReadInt32();
                    return new InlineFieldInstruction(Resolver, offset, opCode, token);

                //The operand is a 32-bit metadata token.
                case OperandType.InlineType:
                    token = ReadInt32();
                    return new InlineTypeInstruction(offset, opCode, token, Resolver);

                //The operand is a FieldRef, MethodRef, or TypeRef token.
                case OperandType.InlineTok:
                    token = ReadInt32();
                    return new InlineTokInstruction(offset, opCode, token, Resolver);

                //The operand is the 32-bit integer argument to a switch instruction.
                case OperandType.InlineSwitch:
                    int cases = ReadInt32();
                    int[] deltas = new int[cases];
                    for (int i = 0; i < cases; i++)
                        deltas[i] = ReadInt32();
                    return new InlineSwitchInstruction(offset, opCode, deltas);

                default:
                    throw new BadImageFormatException("unexpected OperandType " + opCode.OperandType);
            }
        }

        public void Accept(ILInstructionVisitor visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException("argument 'visitor' can not be null");

            foreach (ILInstruction instruction in this)
            {
                instruction.Accept(visitor);
            }
        }

        private byte ReadByte() => _byteArray[_position++];
        private sbyte ReadSByte() => (sbyte)ReadByte();

        private ushort ReadUInt16()
        {
            int pos = _position;
            _position += 2;
            return BitConverter.ToUInt16(_byteArray, pos);
        }

        private uint ReadUInt32()
        {
            int pos = _position;
            _position += 4;
            return BitConverter.ToUInt32(_byteArray, pos);
        }

        private ulong ReadUInt64()
        {
            int pos = _position;
            _position += 8;
            return BitConverter.ToUInt64(_byteArray, pos);
        }

        private int ReadInt32()
        {
            int pos = _position;
            _position += 4;
            return BitConverter.ToInt32(_byteArray, pos);
        }

        private long ReadInt64()
        {
            int pos = _position;
            _position += 8;
            return BitConverter.ToInt64(_byteArray, pos);
        }

        private float ReadSingle()
        {
            int pos = _position;
            _position += 4;
            return BitConverter.ToSingle(_byteArray, pos);
        }

        private double ReadDouble()
        {
            int pos = _position;
            _position += 8;
            return BitConverter.ToDouble(_byteArray, pos);
        }
    }
}
