// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class OnesComplementInstruction : Instruction
    {
        private static Instruction s_SByte, s_Int16, s_Int32, s_Int64, s_Byte, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "OnesComplement";

        private OnesComplementInstruction() { }

        private sealed class OnesComplementInt16 : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((short)(~(short)obj));
                }
                return 1;
            }
        }

        private sealed class OnesComplementInt32 : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(~(int)obj);
                }
                return 1;
            }
        }

        private sealed class OnesComplementInt64 : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(~(long)obj);
                }
                return 1;
            }
        }

        private sealed class OnesComplementUInt16 : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((ushort)(~(ushort)obj));
                }
                return 1;
            }
        }

        private sealed class OnesComplementUInt32 : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(~(uint)obj);
                }
                return 1;
            }
        }

        private sealed class OnesComplementUInt64 : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(~(ulong)obj);
                }
                return 1;
            }
        }

        private sealed class OnesComplementByte : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((byte)(~(byte)obj));
                }
                return 1;
            }
        }

        private sealed class OnesComplementSByte : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((sbyte)(~(sbyte)obj));
                }
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new OnesComplementSByte());
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new OnesComplementInt16());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new OnesComplementInt32());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new OnesComplementInt64());
                case TypeCode.Byte: return s_Byte ?? (s_Byte = new OnesComplementByte());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new OnesComplementUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new OnesComplementUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new OnesComplementUInt64());
                default:
                    throw Error.ExpressionNotSupportedForType("OnesComplement", type);
            }
        }
    }
}