// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class LessThanInstruction : Instruction
    {
        private readonly object? _nullValue;
        private static Instruction? s_SByte, s_Int16, s_Char, s_Int32, s_Int64, s_Byte, s_UInt16, s_UInt32, s_UInt64, s_Single, s_Double;
        private static Instruction? s_liftedToNullSByte, s_liftedToNullInt16, s_liftedToNullChar, s_liftedToNullInt32, s_liftedToNullInt64, s_liftedToNullByte, s_liftedToNullUInt16, s_liftedToNullUInt32, s_liftedToNullUInt64, s_liftedToNullSingle, s_liftedToNullDouble;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "LessThan";

        private LessThanInstruction(object? nullValue)
        {
            _nullValue = nullValue;
        }

        private sealed class LessThanSByte : LessThanInstruction
        {
            public LessThanSByte(object? nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((sbyte)left < (sbyte)right);
                }
                return 1;
            }
        }

        private sealed class LessThanInt16 : LessThanInstruction
        {
            public LessThanInt16(object? nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((short)left < (short)right);
                }
                return 1;
            }
        }

        private sealed class LessThanChar : LessThanInstruction
        {
            public LessThanChar(object? nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((char)left < (char)right);
                }
                return 1;
            }
        }

        private sealed class LessThanInt32 : LessThanInstruction
        {
            public LessThanInt32(object? nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((int)left < (int)right);
                }
                return 1;
            }
        }

        private sealed class LessThanInt64 : LessThanInstruction
        {
            public LessThanInt64(object? nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((long)left < (long)right);
                }
                return 1;
            }
        }

        private sealed class LessThanByte : LessThanInstruction
        {
            public LessThanByte(object? nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((byte)left < (byte)right);
                }
                return 1;
            }
        }

        private sealed class LessThanUInt16 : LessThanInstruction
        {
            public LessThanUInt16(object? nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((ushort)left < (ushort)right);
                }
                return 1;
            }
        }

        private sealed class LessThanUInt32 : LessThanInstruction
        {
            public LessThanUInt32(object? nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((uint)left < (uint)right);
                }
                return 1;
            }
        }

        private sealed class LessThanUInt64 : LessThanInstruction
        {
            public LessThanUInt64(object? nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((ulong)left < (ulong)right);
                }
                return 1;
            }
        }

        private sealed class LessThanSingle : LessThanInstruction
        {
            public LessThanSingle(object? nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((float)left < (float)right);
                }
                return 1;
            }
        }

        private sealed class LessThanDouble : LessThanInstruction
        {
            public LessThanDouble(object? nullValue)
                : base(nullValue)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                object? right = frame.Pop();
                object? left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(_nullValue);
                }
                else
                {
                    frame.Push((double)left < (double)right);
                }
                return 1;
            }
        }
        public static Instruction Create(Type type, bool liftedToNull = false)
        {
            Debug.Assert(!type.IsEnum);
            if (liftedToNull)
            {
                return type.GetNonNullableType().GetTypeCode() switch
                {
                    TypeCode.SByte => s_liftedToNullSByte ??= new LessThanSByte(null),
                    TypeCode.Int16 => s_liftedToNullInt16 ??= new LessThanInt16(null),
                    TypeCode.Char => s_liftedToNullChar ??= new LessThanChar(null),
                    TypeCode.Int32 => s_liftedToNullInt32 ??= new LessThanInt32(null),
                    TypeCode.Int64 => s_liftedToNullInt64 ??= new LessThanInt64(null),
                    TypeCode.Byte => s_liftedToNullByte ??= new LessThanByte(null),
                    TypeCode.UInt16 => s_liftedToNullUInt16 ??= new LessThanUInt16(null),
                    TypeCode.UInt32 => s_liftedToNullUInt32 ??= new LessThanUInt32(null),
                    TypeCode.UInt64 => s_liftedToNullUInt64 ??= new LessThanUInt64(null),
                    TypeCode.Single => s_liftedToNullSingle ??= new LessThanSingle(null),
                    TypeCode.Double => s_liftedToNullDouble ??= new LessThanDouble(null),
                    _ => throw ContractUtils.Unreachable,
                };
            }
            else
            {
                return type.GetNonNullableType().GetTypeCode() switch
                {
                    TypeCode.SByte => s_SByte ??= new LessThanSByte(Utils.BoxedFalse),
                    TypeCode.Int16 => s_Int16 ??= new LessThanInt16(Utils.BoxedFalse),
                    TypeCode.Char => s_Char ??= new LessThanChar(Utils.BoxedFalse),
                    TypeCode.Int32 => s_Int32 ??= new LessThanInt32(Utils.BoxedFalse),
                    TypeCode.Int64 => s_Int64 ??= new LessThanInt64(Utils.BoxedFalse),
                    TypeCode.Byte => s_Byte ??= new LessThanByte(Utils.BoxedFalse),
                    TypeCode.UInt16 => s_UInt16 ??= new LessThanUInt16(Utils.BoxedFalse),
                    TypeCode.UInt32 => s_UInt32 ??= new LessThanUInt32(Utils.BoxedFalse),
                    TypeCode.UInt64 => s_UInt64 ??= new LessThanUInt64(Utils.BoxedFalse),
                    TypeCode.Single => s_Single ??= new LessThanSingle(Utils.BoxedFalse),
                    TypeCode.Double => s_Double ??= new LessThanDouble(Utils.BoxedFalse),
                    _ => throw ContractUtils.Unreachable,
                };
            }
        }
    }
}
