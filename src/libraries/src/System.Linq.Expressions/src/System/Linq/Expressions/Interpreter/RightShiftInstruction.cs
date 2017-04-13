// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class RightShiftInstruction : Instruction
    {
        private static Instruction s_SByte, s_Int16, s_Int32, s_Int64, s_Byte, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "RightShift";

        private RightShiftInstruction() { }

        private sealed class RightShiftSByte : RightShiftInstruction
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
                    frame.Push((sbyte)((sbyte)value >> (int)shift));
                }
                return 1;
            }
        }

        private sealed class RightShiftInt16 : RightShiftInstruction
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
                    frame.Push((short)((short)value >> (int)shift));
                }
                return 1;
            }
        }

        private sealed class RightShiftInt32 : RightShiftInstruction
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
                    frame.Push((int)value >> (int)shift);
                }
                return 1;
            }
        }

        private sealed class RightShiftInt64 : RightShiftInstruction
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
                    frame.Push((long)value >> (int)shift);
                }
                return 1;
            }
        }

        private sealed class RightShiftByte : RightShiftInstruction
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
                    frame.Push((byte)((byte)value >> (int)shift));
                }
                return 1;
            }
        }

        private sealed class RightShiftUInt16 : RightShiftInstruction
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
                    frame.Push((ushort)((ushort)value >> (int)shift));
                }
                return 1;
            }
        }

        private sealed class RightShiftUInt32 : RightShiftInstruction
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
                    frame.Push((uint)value >> (int)shift);
                }
                return 1;
            }
        }

        private sealed class RightShiftUInt64 : RightShiftInstruction
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
                    frame.Push((ulong)value >> (int)shift);
                }
                return 1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type)
        {
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new RightShiftSByte());
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new RightShiftInt16());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new RightShiftInt32());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new RightShiftInt64());
                case TypeCode.Byte: return s_Byte ?? (s_Byte = new RightShiftByte());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new RightShiftUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new RightShiftUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new RightShiftUInt64());
                default:
                    throw ContractUtils.Unreachable;
            }
        }
    }
}
