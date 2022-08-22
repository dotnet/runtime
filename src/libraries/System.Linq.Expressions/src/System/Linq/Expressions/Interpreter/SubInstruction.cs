// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class SubInstruction : Instruction
    {
        private static Instruction? s_Int16, s_Int32, s_Int64, s_UInt16, s_UInt32, s_UInt64, s_Single, s_Double;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "Sub";

        private SubInstruction() { }

        private sealed class SubInt16 : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((short)((short)left - (short)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class SubInt32 : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : ScriptingRuntimeHelpers.Int32ToObject(unchecked((int)left - (int)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class SubInt64 : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((long)left - (long)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class SubUInt16 : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((ushort)((ushort)left - (ushort)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class SubUInt32 : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((uint)left - (uint)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class SubUInt64 : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((ulong)left - (ulong)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class SubSingle : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((float)left - (float)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class SubDouble : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((double)left - (double)right);
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
                TypeCode.Int16 => s_Int16 ??= new SubInt16(),
                TypeCode.Int32 => s_Int32 ??= new SubInt32(),
                TypeCode.Int64 => s_Int64 ??= new SubInt64(),
                TypeCode.UInt16 => s_UInt16 ??= new SubUInt16(),
                TypeCode.UInt32 => s_UInt32 ??= new SubUInt32(),
                TypeCode.UInt64 => s_UInt64 ??= new SubUInt64(),
                TypeCode.Single => s_Single ??= new SubSingle(),
                TypeCode.Double => s_Double ??= new SubDouble(),
                _ => throw ContractUtils.Unreachable,
            };
        }
    }

    internal abstract class SubOvfInstruction : Instruction
    {
        private static Instruction? s_Int16, s_Int32, s_Int64, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "SubOvf";

        private SubOvfInstruction() { }

        private sealed class SubOvfInt16 : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((short)((short)left - (short)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class SubOvfInt32 : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : ScriptingRuntimeHelpers.Int32ToObject(checked((int)left - (int)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class SubOvfInt64 : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((long)left - (long)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class SubOvfUInt16 : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((ushort)((ushort)left - (ushort)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class SubOvfUInt32 : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((uint)left - (uint)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class SubOvfUInt64 : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object?[] stack = frame.Data;
                object? left = stack[index - 2];
                if (left != null)
                {
                    object? right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((ulong)left - (ulong)right);
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
                TypeCode.Int16 => s_Int16 ??= new SubOvfInt16(),
                TypeCode.Int32 => s_Int32 ??= new SubOvfInt32(),
                TypeCode.Int64 => s_Int64 ??= new SubOvfInt64(),
                TypeCode.UInt16 => s_UInt16 ??= new SubOvfUInt16(),
                TypeCode.UInt32 => s_UInt32 ??= new SubOvfUInt32(),
                TypeCode.UInt64 => s_UInt64 ??= new SubOvfUInt64(),
                _ => SubInstruction.Create(type),
            };
        }
    }
}
