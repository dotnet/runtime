// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class NotEqualInstruction : Instruction
    {
        // Perf: EqualityComparer<T> but is 3/2 to 2 times slower.
        private static Instruction? s_reference, s_Boolean, s_SByte, s_Int16, s_Char, s_Int32, s_Int64, s_Byte, s_UInt16, s_UInt32, s_UInt64, s_Single, s_Double;
        private static Instruction? s_SByteLiftedToNull, s_Int16LiftedToNull, s_CharLiftedToNull, s_Int32LiftedToNull, s_Int64LiftedToNull, s_ByteLiftedToNull, s_UInt16LiftedToNull, s_UInt32LiftedToNull, s_UInt64LiftedToNull, s_SingleLiftedToNull, s_DoubleLiftedToNull;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "NotEqual";

        private NotEqualInstruction() { }

        private sealed class NotEqualBoolean : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right != null);
                }
                else if (right == null)
                {
                    frame.Push(true);
                }
                else
                {
                    frame.Push((bool)left != (bool)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualSByte : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right != null);
                }
                else if (right == null)
                {
                    frame.Push(true);
                }
                else
                {
                    frame.Push((sbyte)left != (sbyte)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualInt16 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right != null);
                }
                else if (right == null)
                {
                    frame.Push(true);
                }
                else
                {
                    frame.Push((short)left != (short)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualChar : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right != null);
                }
                else if (right == null)
                {
                    frame.Push(true);
                }
                else
                {
                    frame.Push((char)left != (char)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualInt32 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right != null);
                }
                else if (right == null)
                {
                    frame.Push(true);
                }
                else
                {
                    frame.Push((int)left != (int)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualInt64 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right != null);
                }
                else if (right == null)
                {
                    frame.Push(true);
                }
                else
                {
                    frame.Push((long)left != (long)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualByte : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right != null);
                }
                else if (right == null)
                {
                    frame.Push(true);
                }
                else
                {
                    frame.Push((byte)left != (byte)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualUInt16 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right != null);
                }
                else if (right == null)
                {
                    frame.Push(true);
                }
                else
                {
                    frame.Push((ushort)left != (ushort)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualUInt32 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right != null);
                }
                else if (right == null)
                {
                    frame.Push(true);
                }
                else
                {
                    frame.Push((uint)left != (uint)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualUInt64 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right != null);
                }
                else if (right == null)
                {
                    frame.Push(true);
                }
                else
                {
                    frame.Push((ulong)left != (ulong)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualSingle : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right != null);
                }
                else if (right == null)
                {
                    frame.Push(true);
                }
                else
                {
                    frame.Push((float)left != (float)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualDouble : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right != null);
                }
                else if (right == null)
                {
                    frame.Push(true);
                }
                else
                {
                    frame.Push((double)left != (double)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualReference : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(frame.Pop() != frame.Pop());
                return 1;
            }
        }

        private sealed class NotEqualSByteLiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((sbyte)left != (sbyte)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualInt16LiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((short)left != (short)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualCharLiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((char)left != (char)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualInt32LiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((int)left != (int)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualInt64LiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((long)left != (long)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualByteLiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((byte)left != (byte)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualUInt16LiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((ushort)left != (ushort)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualUInt32LiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((uint)left != (uint)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualUInt64LiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((ulong)left != (ulong)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualSingleLiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((float)left != (float)right);
                }
                return 1;
            }
        }

        private sealed class NotEqualDoubleLiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((double)left != (double)right);
                }
                return 1;
            }
        }

        public static Instruction Create(Type type, bool liftedToNull)
        {
            if (liftedToNull)
            {
                switch (type.GetNonNullableType().GetTypeCode())
                {
                    case TypeCode.Boolean: return ExclusiveOrInstruction.Create(type);
                    case TypeCode.SByte: return s_SByteLiftedToNull ??= new NotEqualSByteLiftedToNull();
                    case TypeCode.Int16: return s_Int16LiftedToNull ??= new NotEqualInt16LiftedToNull();
                    case TypeCode.Char: return s_CharLiftedToNull ??= new NotEqualCharLiftedToNull();
                    case TypeCode.Int32: return s_Int32LiftedToNull ??= new NotEqualInt32LiftedToNull();
                    case TypeCode.Int64: return s_Int64LiftedToNull ??= new NotEqualInt64LiftedToNull();
                    case TypeCode.Byte: return s_ByteLiftedToNull ??= new NotEqualByteLiftedToNull();
                    case TypeCode.UInt16: return s_UInt16LiftedToNull ??= new NotEqualUInt16LiftedToNull();
                    case TypeCode.UInt32: return s_UInt32LiftedToNull ??= new NotEqualUInt32LiftedToNull();
                    case TypeCode.UInt64: return s_UInt64LiftedToNull ??= new NotEqualUInt64LiftedToNull();
                    case TypeCode.Single: return s_SingleLiftedToNull ??= new NotEqualSingleLiftedToNull();
                    default:
                        Debug.Assert(type.GetNonNullableType().GetTypeCode() == TypeCode.Double);
                        return s_DoubleLiftedToNull ??= new NotEqualDoubleLiftedToNull();
                }
            }
            else
            {
                switch (type.GetNonNullableType().GetTypeCode())
                {
                    case TypeCode.Boolean: return s_Boolean ??= new NotEqualBoolean();
                    case TypeCode.SByte: return s_SByte ??= new NotEqualSByte();
                    case TypeCode.Int16: return s_Int16 ??= new NotEqualInt16();
                    case TypeCode.Char: return s_Char ??= new NotEqualChar();
                    case TypeCode.Int32: return s_Int32 ??= new NotEqualInt32();
                    case TypeCode.Int64: return s_Int64 ??= new NotEqualInt64();
                    case TypeCode.Byte: return s_Byte ??= new NotEqualByte();
                    case TypeCode.UInt16: return s_UInt16 ??= new NotEqualUInt16();
                    case TypeCode.UInt32: return s_UInt32 ??= new NotEqualUInt32();
                    case TypeCode.UInt64: return s_UInt64 ??= new NotEqualUInt64();
                    case TypeCode.Single: return s_Single ??= new NotEqualSingle();
                    case TypeCode.Double: return s_Double ??= new NotEqualDouble();
                    default:
                        // Nullable only valid if one operand is constant null, so this assert is slightly too broad.
                        Debug.Assert(type.IsNullableOrReferenceType());
                        return s_reference ??= new NotEqualReference();
                }
            }
        }
    }
}
