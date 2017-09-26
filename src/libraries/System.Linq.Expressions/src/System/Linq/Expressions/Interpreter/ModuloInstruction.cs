// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class ModuloInstruction : Instruction
    {
        private static Instruction s_Int16, s_Int32, s_Int64, s_UInt16, s_UInt32, s_UInt64, s_Single, s_Double;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "Modulo";

        private ModuloInstruction() { }

        private sealed class ModuloInt16 : ModuloInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((short)((short)left % (short)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class ModuloInt32 : ModuloInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : ScriptingRuntimeHelpers.Int32ToObject((int)left % (int)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class ModuloInt64 : ModuloInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((long)left % (long)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class ModuloUInt16 : ModuloInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)unchecked((ushort)((ushort)left % (ushort)right));
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class ModuloUInt32 : ModuloInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((uint)left % (uint)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class ModuloUInt64 : ModuloInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((ulong)left % (ulong)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class ModuloSingle : ModuloInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((float)left % (float)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        private sealed class ModuloDouble : ModuloInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                int index = frame.StackIndex;
                object[] stack = frame.Data;
                object left = stack[index - 2];
                if (left != null)
                {
                    object right = stack[index - 1];
                    stack[index - 2] = right == null ? null : (object)((double)left % (double)right);
                }

                frame.StackIndex = index - 1;
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.IsEnum);
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new ModuloInt16());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new ModuloInt32());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new ModuloInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new ModuloUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new ModuloUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new ModuloUInt64());
                case TypeCode.Single: return s_Single ?? (s_Single = new ModuloSingle());
                case TypeCode.Double: return s_Double ?? (s_Double = new ModuloDouble());
                default:
                    throw ContractUtils.Unreachable;
            }
        }
    }
}
