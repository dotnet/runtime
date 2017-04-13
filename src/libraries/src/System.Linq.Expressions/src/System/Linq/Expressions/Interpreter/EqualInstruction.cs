// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class EqualInstruction : Instruction
    {
        // Perf: EqualityComparer<T> but is 3/2 to 2 times slower.
        private static Instruction s_reference, s_Boolean, s_SByte, s_Int16, s_Char, s_Int32, s_Int64, s_Byte, s_UInt16, s_UInt32, s_UInt64, s_Single, s_Double;
        private static Instruction s_BooleanLiftedToNull, s_SByteLiftedToNull, s_Int16LiftedToNull, s_CharLiftedToNull, s_Int32LiftedToNull, s_Int64LiftedToNull, s_ByteLiftedToNull, s_UInt16LiftedToNull, s_UInt32LiftedToNull, s_UInt64LiftedToNull, s_SingleLiftedToNull, s_DoubleLiftedToNull;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "Equal";

        private EqualInstruction() { }

        private sealed class EqualBoolean : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right == null);
                }
                else if (right == null)
                {
                    frame.Push(false);
                }
                else
                {
                    frame.Push((bool)left == (bool)right);
                }
                return 1;
            }
        }

        private sealed class EqualSByte : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right == null);
                }
                else if (right == null)
                {
                    frame.Push(false);
                }
                else
                {
                    frame.Push((sbyte)left == (sbyte)right);
                }
                return 1;
            }
        }

        private sealed class EqualInt16 : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right == null);
                }
                else if (right == null)
                {
                    frame.Push(false);
                }
                else
                {
                    frame.Push((short)left == (short)right);
                }
                return 1;
            }
        }

        private sealed class EqualChar : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right == null);
                }
                else if (right == null)
                {
                    frame.Push(false);
                }
                else
                {
                    frame.Push((char)left == (char)right);
                }
                return 1;
            }
        }

        private sealed class EqualInt32 : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right == null);
                }
                else if (right == null)
                {
                    frame.Push(false);
                }
                else
                {
                    frame.Push((int)left == (int)right);
                }
                return 1;
            }
        }

        private sealed class EqualInt64 : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right == null);
                }
                else if (right == null)
                {
                    frame.Push(false);
                }
                else
                {
                    frame.Push((long)left == (long)right);
                }
                return 1;
            }
        }

        private sealed class EqualByte : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right == null);
                }
                else if (right == null)
                {
                    frame.Push(false);
                }
                else
                {
                    frame.Push((byte)left == (byte)right);
                }
                return 1;
            }
        }

        private sealed class EqualUInt16 : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right == null);
                }
                else if (right == null)
                {
                    frame.Push(false);
                }
                else
                {
                    frame.Push((ushort)left == (ushort)right);
                }
                return 1;
            }
        }

        private sealed class EqualUInt32 : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right == null);
                }
                else if (right == null)
                {
                    frame.Push(false);
                }
                else
                {
                    frame.Push((uint)left == (uint)right);
                }
                return 1;
            }
        }

        private sealed class EqualUInt64 : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right == null);
                }
                else if (right == null)
                {
                    frame.Push(false);
                }
                else
                {
                    frame.Push((ulong)left == (ulong)right);
                }
                return 1;
            }
        }

        private sealed class EqualSingle : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right == null);
                }
                else if (right == null)
                {
                    frame.Push(false);
                }
                else
                {
                    frame.Push((float)left == (float)right);
                }
                return 1;
            }
        }

        private sealed class EqualDouble : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null)
                {
                    frame.Push(right == null);
                }
                else if (right == null)
                {
                    frame.Push(false);
                }
                else
                {
                    frame.Push((double)left == (double)right);
                }
                return 1;
            }
        }

        private sealed class EqualReference : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(frame.Pop() == frame.Pop());
                return 1;
            }
        }

        private sealed class EqualBooleanLiftedToNull : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((bool)left == (bool)right);
                }
                return 1;
            }
        }

        private sealed class EqualSByteLiftedToNull : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((sbyte)left == (sbyte)right);
                }
                return 1;
            }
        }

        private sealed class EqualInt16LiftedToNull : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((short)left == (short)right);
                }
                return 1;
            }
        }

        private sealed class EqualCharLiftedToNull : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((char)left == (char)right);
                }
                return 1;
            }
        }

        private sealed class EqualInt32LiftedToNull : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((int)left == (int)right);
                }
                return 1;
            }
        }

        private sealed class EqualInt64LiftedToNull : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((long)left == (long)right);
                }
                return 1;
            }
        }

        private sealed class EqualByteLiftedToNull : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((byte)left == (byte)right);
                }
                return 1;
            }
        }

        private sealed class EqualUInt16LiftedToNull : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((ushort)left == (ushort)right);
                }
                return 1;
            }
        }

        private sealed class EqualUInt32LiftedToNull : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((uint)left == (uint)right);
                }
                return 1;
            }
        }

        private sealed class EqualUInt64LiftedToNull : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((ulong)left == (ulong)right);
                }
                return 1;
            }
        }

        private sealed class EqualSingleLiftedToNull : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((float)left == (float)right);
                }
                return 1;
            }
        }

        private sealed class EqualDoubleLiftedToNull : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((double)left == (double)right);
                }
                return 1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type, bool liftedToNull)
        {
            if (liftedToNull)
            {
                switch (type.GetNonNullableType().GetTypeCode())
                {
                    case TypeCode.Boolean: return s_BooleanLiftedToNull ?? (s_BooleanLiftedToNull = new EqualBooleanLiftedToNull());
                    case TypeCode.SByte: return s_SByteLiftedToNull ?? (s_SByteLiftedToNull = new EqualSByteLiftedToNull());
                    case TypeCode.Int16: return s_Int16LiftedToNull ?? (s_Int16LiftedToNull = new EqualInt16LiftedToNull());
                    case TypeCode.Char: return s_CharLiftedToNull ?? (s_CharLiftedToNull = new EqualCharLiftedToNull());
                    case TypeCode.Int32: return s_Int32LiftedToNull ?? (s_Int32LiftedToNull = new EqualInt32LiftedToNull());
                    case TypeCode.Int64: return s_Int64LiftedToNull ?? (s_Int64LiftedToNull = new EqualInt64LiftedToNull());
                    case TypeCode.Byte: return s_ByteLiftedToNull ?? (s_ByteLiftedToNull = new EqualByteLiftedToNull());
                    case TypeCode.UInt16: return s_UInt16LiftedToNull ?? (s_UInt16LiftedToNull = new EqualUInt16LiftedToNull());
                    case TypeCode.UInt32: return s_UInt32LiftedToNull ?? (s_UInt32LiftedToNull = new EqualUInt32LiftedToNull());
                    case TypeCode.UInt64: return s_UInt64LiftedToNull ?? (s_UInt64LiftedToNull = new EqualUInt64LiftedToNull());
                    case TypeCode.Single: return s_SingleLiftedToNull ?? (s_SingleLiftedToNull = new EqualSingleLiftedToNull());
                    default:
                        Debug.Assert(type.GetNonNullableType().GetTypeCode() == TypeCode.Double);
                        return s_DoubleLiftedToNull ?? (s_DoubleLiftedToNull = new EqualDoubleLiftedToNull());
                }
            }
            else
            {
                switch (type.GetNonNullableType().GetTypeCode())
                {
                    case TypeCode.Boolean: return s_Boolean ?? (s_Boolean = new EqualBoolean());
                    case TypeCode.SByte: return s_SByte ?? (s_SByte = new EqualSByte());
                    case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new EqualInt16());
                    case TypeCode.Char: return s_Char ?? (s_Char = new EqualChar());
                    case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new EqualInt32());
                    case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new EqualInt64());
                    case TypeCode.Byte: return s_Byte ?? (s_Byte = new EqualByte());
                    case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new EqualUInt16());
                    case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new EqualUInt32());
                    case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new EqualUInt64());
                    case TypeCode.Single: return s_Single ?? (s_Single = new EqualSingle());
                    case TypeCode.Double: return s_Double ?? (s_Double = new EqualDouble());
                    default:
                        // Nullable only valid if one operand is constant null, so this assert is slightly too broad.
                        Debug.Assert(type.IsNullableOrReferenceType());
                        return s_reference ?? (s_reference = new EqualReference());
                }
            }
        }
    }
}
