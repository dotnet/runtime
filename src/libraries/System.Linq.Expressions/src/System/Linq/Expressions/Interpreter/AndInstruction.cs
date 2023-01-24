// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class AndInstruction : Instruction
    {
        private static Instruction? s_SByte, s_Int16, s_Int32, s_Int64, s_Byte, s_UInt16, s_UInt32, s_UInt64, s_Boolean;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "And";

        private AndInstruction() { }

        private sealed class AndSByte : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? left = frame.Pop();
                object? right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((sbyte)((sbyte)left & (sbyte)right));
                return 1;
            }
        }

        private sealed class AndInt16 : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? left = frame.Pop();
                object? right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((short)((short)left & (short)right));
                return 1;
            }
        }

        private sealed class AndInt32 : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? left = frame.Pop();
                object? right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((int)left & (int)right);
                return 1;
            }
        }

        private sealed class AndInt64 : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? left = frame.Pop();
                object? right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((long)left & (long)right);
                return 1;
            }
        }

        private sealed class AndByte : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? left = frame.Pop();
                object? right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((byte)((byte)left & (byte)right));
                return 1;
            }
        }

        private sealed class AndUInt16 : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? left = frame.Pop();
                object? right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((ushort)((ushort)left & (ushort)right));
                return 1;
            }
        }

        private sealed class AndUInt32 : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? left = frame.Pop();
                object? right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((uint)left & (uint)right);
                return 1;
            }
        }

        private sealed class AndUInt64 : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? left = frame.Pop();
                object? right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((ulong)left & (ulong)right);
                return 1;
            }
        }

        private sealed class AndBoolean : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    if (right == null)
                    {
                        frame.Push(null);
                    }
                    else
                    {
                        frame.Push((bool)right ? null : Utils.BoxedFalse);
                    }
                    return 1;
                }
                else if (right == null)
                {
                    frame.Push((bool)left ? null : Utils.BoxedFalse);
                    return 1;
                }
                frame.Push((bool)left & (bool)right);
                return 1;
            }
        }

        public static Instruction Create(Type type) =>
            type.GetNonNullableType().GetTypeCode() switch
            {
                TypeCode.SByte => s_SByte ??= new AndSByte(),
                TypeCode.Int16 => s_Int16 ??= new AndInt16(),
                TypeCode.Int32 => s_Int32 ??= new AndInt32(),
                TypeCode.Int64 => s_Int64 ??= new AndInt64(),
                TypeCode.Byte => s_Byte ??= new AndByte(),
                TypeCode.UInt16 => s_UInt16 ??= new AndUInt16(),
                TypeCode.UInt32 => s_UInt32 ??= new AndUInt32(),
                TypeCode.UInt64 => s_UInt64 ??= new AndUInt64(),
                TypeCode.Boolean => s_Boolean ??= new AndBoolean(),
                _ => throw ContractUtils.Unreachable,
            };
    }
}
