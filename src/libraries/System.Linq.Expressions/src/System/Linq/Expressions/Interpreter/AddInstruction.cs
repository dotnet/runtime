// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class AddInstruction : Instruction
    {
        private static Instruction s_Int16, s_Int32, s_Int64, s_UInt16, s_UInt32, s_UInt64, s_Single, s_Double;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "Add";

        private AddInstruction() { }

        private sealed class AddInt16 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((short)((short)left + (short)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddInt32 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : ScriptingRuntimeHelpers.Int32ToObject(unchecked((int)left + (int)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddInt64 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((long)left + (long)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddUInt16 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((ushort)((ushort)left + (ushort)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddUInt32 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((uint)left + (uint)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddUInt64 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((ulong)left + (ulong)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddSingle : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((float)left + (float)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddDouble : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((double)left + (double)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(type.IsArithmetic());
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new AddInt16());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new AddInt32());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new AddInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new AddUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new AddUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new AddUInt64());
                case TypeCode.Single: return s_Single ?? (s_Single = new AddSingle());
                case TypeCode.Double: return s_Double ?? (s_Double = new AddDouble());
                default:
                    throw ContractUtils.Unreachable;
            }
        }
    }

    internal abstract class AddOvfInstruction : Instruction
    {
        private static Instruction s_Int16, s_Int32, s_Int64, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "AddOvf";

        private AddOvfInstruction() { }

        private sealed class AddOvfInt16 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((short)((short)left + (short)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddOvfInt32 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : ScriptingRuntimeHelpers.Int32ToObject(checked((int)left + (int)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddOvfInt64 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((long)left + (long)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddOvfUInt16 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((ushort)((ushort)left + (ushort)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddOvfUInt32 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((uint)left + (uint)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class AddOvfUInt64 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)checked((ulong)left + (ulong)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(type.IsArithmetic());
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new AddOvfInt16());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new AddOvfInt32());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new AddOvfInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new AddOvfUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new AddOvfUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new AddOvfUInt64());
                default:
                    return AddInstruction.Create(type);
            }
        }
    }
}
