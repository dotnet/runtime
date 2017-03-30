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

        private sealed class NotBoolean : NotInstruction
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

        private sealed class NotInt64 : NotInstruction
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

        private sealed class NotInt32 : NotInstruction
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

        private sealed class NotInt16 : NotInstruction
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

        private sealed class NotUInt64 : NotInstruction
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

        private sealed class NotUInt32 : NotInstruction
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

        private sealed class NotUInt16 : NotInstruction
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
                    frame.Push(unchecked((ushort)(~(ushort)value)));
                }
                return 1;
            }
        }

        private sealed class NotByte : NotInstruction
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
                    frame.Push(unchecked((byte)(~(byte)value)));
                }
                return 1;
            }
        }

        private sealed class NotSByte : NotInstruction
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
                case TypeCode.Boolean: return s_Boolean ?? (s_Boolean = new NotBoolean());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new NotInt64());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new NotInt32());
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new NotInt16());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new NotUInt64());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new NotUInt32());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new NotUInt16());
                case TypeCode.Byte: return s_Byte ?? (s_Byte = new NotByte());
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new NotSByte());
                default:
                    throw ContractUtils.Unreachable;
            }
        }
    }
}
