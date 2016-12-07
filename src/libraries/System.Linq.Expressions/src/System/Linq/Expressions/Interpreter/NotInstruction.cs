// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class NotInstruction : Instruction
    {
        public static Instruction s_Boolean, s_Int64, s_Int32, s_Int16, s_UInt64, s_UInt32, s_UInt16, s_Byte, s_SByte;

        private NotInstruction() { }

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "Not";

        private sealed class BoolNot : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(!(bool)value);
                }
                return 1;
            }
        }

        private sealed class Int64Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(~(long)value);
                }
                return 1;
            }
        }

        private sealed class Int32Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(~(int)value);
                }
                return 1;
            }
        }

        private sealed class Int16Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((short)(~(short)value));
                }
                return 1;
            }
        }

        private sealed class UInt64Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(~(ulong)value);
                }
                return 1;
            }
        }

        private sealed class UInt32Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(~(uint)value);
                }
                return 1;
            }
        }

        private sealed class UInt16Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((ushort)(~(ushort)value));
                }
                return 1;
            }
        }

        private sealed class ByteNot : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((byte)(~(byte)value));
                }
                return 1;
            }
        }

        private sealed class SByteNot : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((sbyte)(~(sbyte)value));
                }
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.Boolean: return s_Boolean ?? (s_Boolean = new BoolNot());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new Int64Not());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new Int32Not());
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new Int16Not());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new UInt64Not());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new UInt32Not());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new UInt16Not());
                case TypeCode.Byte: return s_Byte ?? (s_Byte = new ByteNot());
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new SByteNot());

                default:
                    throw Error.ExpressionNotSupportedForType("Not", type);
            }
        }
    }
}
