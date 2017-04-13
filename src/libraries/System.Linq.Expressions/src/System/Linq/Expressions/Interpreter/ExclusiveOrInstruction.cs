// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class ExclusiveOrInstruction : Instruction
    {
        private static Instruction s_SByte, s_Int16, s_Int32, s_Int64, s_Byte, s_UInt16, s_UInt32, s_UInt64, s_Boolean;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "ExclusiveOr";

        private ExclusiveOrInstruction() { }

        private sealed class ExclusiveOrSByte : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((sbyte)((sbyte)left ^ (sbyte)right));
                return 1;
            }
        }

        private sealed class ExclusiveOrInt16 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((short)((short)left ^ (short)right));
                return 1;
            }
        }

        private sealed class ExclusiveOrInt32 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((int)left ^ (int)right);
                return 1;
            }
        }

        private sealed class ExclusiveOrInt64 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((long)left ^ (long)right);
                return 1;
            }
        }

        private sealed class ExclusiveOrByte : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((byte)((byte)left ^ (byte)right));
                return 1;
            }
        }

        private sealed class ExclusiveOrUInt16 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((ushort)((ushort)left ^ (ushort)right));
                return 1;
            }
        }

        private sealed class ExclusiveOrUInt32 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((uint)left ^ (uint)right);
                return 1;
            }
        }

        private sealed class ExclusiveOrUInt64 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((ulong)left ^ (ulong)right);
                return 1;
            }
        }

        private sealed class ExclusiveOrBoolean : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return 1;
                }
                frame.Push((bool)left ^ (bool)right);
                return 1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type)
        {
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new ExclusiveOrSByte());
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new ExclusiveOrInt16());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new ExclusiveOrInt32());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new ExclusiveOrInt64());
                case TypeCode.Byte: return s_Byte ?? (s_Byte = new ExclusiveOrByte());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new ExclusiveOrUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new ExclusiveOrUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new ExclusiveOrUInt64());
                case TypeCode.Boolean: return s_Boolean ?? (s_Boolean = new ExclusiveOrBoolean());
                default:
                    throw ContractUtils.Unreachable;
            }
        }
    }
}