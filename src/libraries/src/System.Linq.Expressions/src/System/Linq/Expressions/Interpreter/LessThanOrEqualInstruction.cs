// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class LessThanOrEqualInstruction : Instruction
    {
        private readonly object _nullValue;
        private static Instruction s_SByte, s_Int16, s_Char, s_Int32, s_Int64, s_Byte, s_UInt16, s_UInt32, s_UInt64, s_Single, s_Double;
        private static Instruction s_liftedToNullSByte, s_liftedToNullInt16, s_liftedToNullChar, s_liftedToNullInt32, s_liftedToNullInt64, s_liftedToNullByte, s_liftedToNullUInt16, s_liftedToNullUInt32, s_liftedToNullUInt64, s_liftedToNullSingle, s_liftedToNullDouble;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "LessThanOrEqual";

        private LessThanOrEqualInstruction(object nullValue)
        {
            _nullValue = nullValue;
        }

        private sealed class LessThanOrEqualSByte : LessThanOrEqualInstruction
        {
            public LessThanOrEqualSByte(object nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((sbyte)left <= (sbyte)right);
                }
                return 1;
            }
        }

        private sealed class LessThanOrEqualInt16 : LessThanOrEqualInstruction
        {
            public LessThanOrEqualInt16(object nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((short)left <= (short)right);
                }
                return 1;
            }
        }

        private sealed class LessThanOrEqualChar : LessThanOrEqualInstruction
        {
            public LessThanOrEqualChar(object nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((char)left <= (char)right);
                }
                return 1;
            }
        }

        private sealed class LessThanOrEqualInt32 : LessThanOrEqualInstruction
        {
            public LessThanOrEqualInt32(object nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((int)left <= (int)right);
                }
                return 1;
            }
        }

        private sealed class LessThanOrEqualInt64 : LessThanOrEqualInstruction
        {
            public LessThanOrEqualInt64(object nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((long)left <= (long)right);
                }
                return 1;
            }
        }

        private sealed class LessThanOrEqualByte : LessThanOrEqualInstruction
        {
            public LessThanOrEqualByte(object nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((byte)left <= (byte)right);
                }
                return 1;
            }
        }

        private sealed class LessThanOrEqualUInt16 : LessThanOrEqualInstruction
        {
            public LessThanOrEqualUInt16(object nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((ushort)left <= (ushort)right);
                }
                return 1;
            }
        }

        private sealed class LessThanOrEqualUInt32 : LessThanOrEqualInstruction
        {
            public LessThanOrEqualUInt32(object nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((uint)left <= (uint)right);
                }
                return 1;
            }
        }

        private sealed class LessThanOrEqualUInt64 : LessThanOrEqualInstruction
        {
            public LessThanOrEqualUInt64(object nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((ulong)left <= (ulong)right);
                }
                return 1;
            }
        }

        private sealed class LessThanOrEqualSingle : LessThanOrEqualInstruction
        {
            public LessThanOrEqualSingle(object nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((float)left <= (float)right);
                }
                return 1;
            }
        }

        private sealed class LessThanOrEqualDouble : LessThanOrEqualInstruction
        {
            public LessThanOrEqualDouble(object nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object right = frame.Pop();
                object left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((double)left <= (double)right);
                }
                return 1;
            }
        }

        public static Instruction Create(Type type, bool liftedToNull = false)
        {
            Debug.Assert(!type.IsEnum);
            if (liftedToNull)
            {
                switch (type.GetNonNullableType().GetTypeCode())
                {
                    case TypeCode.SByte: return s_liftedToNullSByte ?? (s_liftedToNullSByte = new LessThanOrEqualSByte(null));
                    case TypeCode.Int16: return s_liftedToNullInt16 ?? (s_liftedToNullInt16 = new LessThanOrEqualInt16(null));
                    case TypeCode.Char: return s_liftedToNullChar ?? (s_liftedToNullChar = new LessThanOrEqualChar(null));
                    case TypeCode.Int32: return s_liftedToNullInt32 ?? (s_liftedToNullInt32 = new LessThanOrEqualInt32(null));
                    case TypeCode.Int64: return s_liftedToNullInt64 ?? (s_liftedToNullInt64 = new LessThanOrEqualInt64(null));
                    case TypeCode.Byte: return s_liftedToNullByte ?? (s_liftedToNullByte = new LessThanOrEqualByte(null));
                    case TypeCode.UInt16: return s_liftedToNullUInt16 ?? (s_liftedToNullUInt16 = new LessThanOrEqualUInt16(null));
                    case TypeCode.UInt32: return s_liftedToNullUInt32 ?? (s_liftedToNullUInt32 = new LessThanOrEqualUInt32(null));
                    case TypeCode.UInt64: return s_liftedToNullUInt64 ?? (s_liftedToNullUInt64 = new LessThanOrEqualUInt64(null));
                    case TypeCode.Single: return s_liftedToNullSingle ?? (s_liftedToNullSingle = new LessThanOrEqualSingle(null));
                    case TypeCode.Double: return s_liftedToNullDouble ?? (s_liftedToNullDouble = new LessThanOrEqualDouble(null));
                    default:
                        throw ContractUtils.Unreachable;
                }
            }
            else
            {
                switch (type.GetNonNullableType().GetTypeCode())
                {
                    case TypeCode.SByte: return s_SByte ?? (s_SByte = new LessThanOrEqualSByte(Utils.BoxedFalse));
                    case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new LessThanOrEqualInt16(Utils.BoxedFalse));
                    case TypeCode.Char: return s_Char ?? (s_Char = new LessThanOrEqualChar(Utils.BoxedFalse));
                    case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new LessThanOrEqualInt32(Utils.BoxedFalse));
                    case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new LessThanOrEqualInt64(Utils.BoxedFalse));
                    case TypeCode.Byte: return s_Byte ?? (s_Byte = new LessThanOrEqualByte(Utils.BoxedFalse));
                    case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new LessThanOrEqualUInt16(Utils.BoxedFalse));
                    case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new LessThanOrEqualUInt32(Utils.BoxedFalse));
                    case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new LessThanOrEqualUInt64(Utils.BoxedFalse));
                    case TypeCode.Single: return s_Single ?? (s_Single = new LessThanOrEqualSingle(Utils.BoxedFalse));
                    case TypeCode.Double: return s_Double ?? (s_Double = new LessThanOrEqualDouble(Utils.BoxedFalse));
                    default:
                        throw ContractUtils.Unreachable;
                }
            }
        }
    }
}
