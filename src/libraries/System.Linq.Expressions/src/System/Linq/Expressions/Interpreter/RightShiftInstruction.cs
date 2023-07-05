// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class RightShiftInstruction : Instruction
    {
        private static Instruction? s_SByte, s_Int16, s_Int32, s_Int64, s_Byte, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "RightShift";

        private RightShiftInstruction() { }

        private sealed class RightShiftSByte : RightShiftInstruction
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
                    frame.Push((sbyte)((sbyte)value >> (int)shift));
                }
                return 1;
            }
        }

        private sealed class RightShiftInt16 : RightShiftInstruction
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
                    frame.Push((short)((short)value >> (int)shift));
                }
                return 1;
            }
        }

        private sealed class RightShiftInt32 : RightShiftInstruction
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
                    frame.Push((int)value >> (int)shift);
                }
                return 1;
            }
        }

        private sealed class RightShiftInt64 : RightShiftInstruction
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
                    frame.Push((long)value >> (int)shift);
                }
                return 1;
            }
        }

        private sealed class RightShiftByte : RightShiftInstruction
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
                    frame.Push((byte)((byte)value >> (int)shift));
                }
                return 1;
            }
        }

        private sealed class RightShiftUInt16 : RightShiftInstruction
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
                    frame.Push((ushort)((ushort)value >> (int)shift));
                }
                return 1;
            }
        }

        private sealed class RightShiftUInt32 : RightShiftInstruction
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
                    frame.Push((uint)value >> (int)shift);
                }
                return 1;
            }
        }

        private sealed class RightShiftUInt64 : RightShiftInstruction
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
                    frame.Push((ulong)value >> (int)shift);
                }
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            return type.GetNonNullableType().GetTypeCode() switch
            {
                TypeCode.SByte => s_SByte ??= new RightShiftSByte(),
                TypeCode.Int16 => s_Int16 ??= new RightShiftInt16(),
                TypeCode.Int32 => s_Int32 ??= new RightShiftInt32(),
                TypeCode.Int64 => s_Int64 ??= new RightShiftInt64(),
                TypeCode.Byte => s_Byte ??= new RightShiftByte(),
                TypeCode.UInt16 => s_UInt16 ??= new RightShiftUInt16(),
                TypeCode.UInt32 => s_UInt32 ??= new RightShiftUInt32(),
                TypeCode.UInt64 => s_UInt64 ??= new RightShiftUInt64(),
                _ => throw ContractUtils.Unreachable,
            };
        }
    }
}
