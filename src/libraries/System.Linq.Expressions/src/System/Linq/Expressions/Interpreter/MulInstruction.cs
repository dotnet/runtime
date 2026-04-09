// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class MulInstruction : Instruction
    {
        private static Instruction? s_Int16, s_Int32, s_Int64, s_UInt16, s_UInt32, s_UInt64, s_Single, s_Double;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "Mul";

        private MulInstruction() { }

        private sealed class MulInt16 : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((short)((short)left * (short)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class MulInt32 : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : ScriptingRuntimeHelpers.Int32ToObject(unchecked((int)left * (int)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class MulInt64 : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((long)left * (long)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class MulUInt16 : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((ushort)((ushort)left * (ushort)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class MulUInt32 : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((uint)left * (uint)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class MulUInt64 : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((ulong)left * (ulong)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class MulSingle : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((float)left * (float)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class MulDouble : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((double)left * (double)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(type.IsArithmetic());
            return type.GetNonNullableType().GetTypeCode() switch
            {
                TypeCode.Int16 => s_Int16 ??= new MulInt16(),
                TypeCode.Int32 => s_Int32 ??= new MulInt32(),
                TypeCode.Int64 => s_Int64 ??= new MulInt64(),
                TypeCode.UInt16 => s_UInt16 ??= new MulUInt16(),
                TypeCode.UInt32 => s_UInt32 ??= new MulUInt32(),
                TypeCode.UInt64 => s_UInt64 ??= new MulUInt64(),
                TypeCode.Single => s_Single ??= new MulSingle(),
                TypeCode.Double => s_Double ??= new MulDouble(),

                _ => throw ContractUtils.Unreachable,
            };
        }
    }

    internal abstract class MulOvfInstruction : Instruction
    {
        private static Instruction? s_Int16, s_Int32, s_Int64, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "MulOvf";

        private MulOvfInstruction() { }

        private sealed class MulOvfInt16 : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((short)((short)left * (short)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class MulOvfInt32 : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : ScriptingRuntimeHelpers.Int32ToObject(checked((int)left * (int)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class MulOvfInt64 : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((long)left * (long)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class MulOvfUInt16 : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((ushort)((ushort)left * (ushort)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class MulOvfUInt32 : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((uint)left * (uint)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class MulOvfUInt64 : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((ulong)left * (ulong)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(type.IsArithmetic());
            return type.GetNonNullableType().GetTypeCode() switch
            {
                TypeCode.Int16 => s_Int16 ??= new MulOvfInt16(),
                TypeCode.Int32 => s_Int32 ??= new MulOvfInt32(),
                TypeCode.Int64 => s_Int64 ??= new MulOvfInt64(),
                TypeCode.UInt16 => s_UInt16 ??= new MulOvfUInt16(),
                TypeCode.UInt32 => s_UInt32 ??= new MulOvfUInt32(),
                TypeCode.UInt64 => s_UInt64 ??= new MulOvfUInt64(),
                _ => MulInstruction.Create(type),
            };
        }
    }
}
