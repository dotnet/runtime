// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class LeftShiftInstruction : Instruction
    {
        private static Instruction? s_SByte, s_Int16, s_Int32, s_Int64, s_Byte, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "LeftShift";

        private LeftShiftInstruction() { }

        private sealed class LeftShiftSByte : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? shift = frame.Pop();
                object? value = frame.Pop();
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
                object? shift = frame.Pop();
                object? value = frame.Pop();
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
                object? shift = frame.Pop();
                object? value = frame.Pop();
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
                object? shift = frame.Pop();
                object? value = frame.Pop();
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
                object? shift = frame.Pop();
                object? value = frame.Pop();
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
                object? shift = frame.Pop();
                object? value = frame.Pop();
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
                object? shift = frame.Pop();
                object? value = frame.Pop();
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
                object? shift = frame.Pop();
                object? value = frame.Pop();
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

        public static Instruction Create(Type type) =>
            type.GetNonNullableType().GetTypeCode() switch
            {
                TypeCode.SByte => s_SByte ??= new LeftShiftSByte(),
                TypeCode.Int16 => s_Int16 ??= new LeftShiftInt16(),
                TypeCode.Int32 => s_Int32 ??= new LeftShiftInt32(),
                TypeCode.Int64 => s_Int64 ??= new LeftShiftInt64(),
                TypeCode.Byte => s_Byte ??= new LeftShiftByte(),
                TypeCode.UInt16 => s_UInt16 ??= new LeftShiftUInt16(),
                TypeCode.UInt32 => s_UInt32 ??= new LeftShiftUInt32(),
                TypeCode.UInt64 => s_UInt64 ??= new LeftShiftUInt64(),
                _ => throw ContractUtils.Unreachable,
            };
    }
}
