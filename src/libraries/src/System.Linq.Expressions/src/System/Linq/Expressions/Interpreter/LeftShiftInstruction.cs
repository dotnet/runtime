// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class LeftShiftInstruction : Instruction
    {
        private static Instruction s_SByte, s_Int16, s_Int32, s_Int64, s_Byte, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "LeftShift";

        private LeftShiftInstruction() { }

        private sealed class LeftShiftSByte : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((sbyte)((sbyte)value << (int)shift)));
                }
                return 1;
            }
        }

        private sealed class LeftShiftInt16 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((short)((short)value << (int)shift)));
                }
                return 1;
            }
        }

        private sealed class LeftShiftInt32 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((int)value << (int)shift);
                }
                return 1;
            }
        }

        private sealed class LeftShiftInt64 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((long)value << (int)shift);
                }
                return 1;
            }
        }

        private sealed class LeftShiftByte : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((byte)((byte)value << (int)shift)));
                }
                return 1;
            }
        }

        private sealed class LeftShiftUInt16 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((ushort)((ushort)value << (int)shift)));
                }
                return 1;
            }
        }

        private sealed class LeftShiftUInt32 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((uint)value << (int)shift);
                }
                return 1;
            }
        }

        private sealed class LeftShiftUInt64 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((ulong)value << (int)shift);
                }
                return 1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type)
        {
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new LeftShiftSByte());
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new LeftShiftInt16());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new LeftShiftInt32());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new LeftShiftInt64());
                case TypeCode.Byte: return s_Byte ?? (s_Byte = new LeftShiftByte());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new LeftShiftUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new LeftShiftUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new LeftShiftUInt64());
                default:
                    throw ContractUtils.Unreachable;
            }
        }
    }
}
