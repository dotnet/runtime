// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class GreaterThanInstruction : Instruction
    {
        private readonly object? _nullValue;
        private static Instruction? s_SByte, s_Int16, s_Char, s_Int32, s_Int64, s_Byte, s_UInt16, s_UInt32, s_UInt64, s_Single, s_Double;
        private static Instruction? s_liftedToNullSByte, s_liftedToNullInt16, s_liftedToNullChar, s_liftedToNullInt32, s_liftedToNullInt64, s_liftedToNullByte, s_liftedToNullUInt16, s_liftedToNullUInt32, s_liftedToNullUInt64, s_liftedToNullSingle, s_liftedToNullDouble;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "GreaterThan";

        private GreaterThanInstruction(object? nullValue)
        {
            _nullValue = nullValue;
        }

        private sealed class GreaterThanSByte : GreaterThanInstruction
        {
            public GreaterThanSByte(object? nullValue)
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
                    frame.Push((sbyte)left > (sbyte)right);
                }
                return 1;
            }
        }

        private sealed class GreaterThanInt16 : GreaterThanInstruction
        {
            public GreaterThanInt16(object? nullValue)
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
                    frame.Push((short)left > (short)right);
                }
                return 1;
            }
        }

        private sealed class GreaterThanChar : GreaterThanInstruction
        {
            public GreaterThanChar(object? nullValue)
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
                    frame.Push((char)left > (char)right);
                }
                return 1;
            }
        }

        private sealed class GreaterThanInt32 : GreaterThanInstruction
        {
            public GreaterThanInt32(object? nullValue)
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
                    frame.Push((int)left > (int)right);
                }
                return 1;
            }
        }

        private sealed class GreaterThanInt64 : GreaterThanInstruction
        {
            public GreaterThanInt64(object? nullValue)
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
                    frame.Push((long)left > (long)right);
                }
                return 1;
            }
        }

        private sealed class GreaterThanByte : GreaterThanInstruction
        {
            public GreaterThanByte(object? nullValue)
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
                    frame.Push((byte)left > (byte)right);
                }
                return 1;
            }
        }

        private sealed class GreaterThanUInt16 : GreaterThanInstruction
        {
            public GreaterThanUInt16(object? nullValue)
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
                    frame.Push((ushort)left > (ushort)right);
                }
                return 1;
            }
        }

        private sealed class GreaterThanUInt32 : GreaterThanInstruction
        {
            public GreaterThanUInt32(object? nullValue)
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
                    frame.Push((uint)left > (uint)right);
                }
                return 1;
            }
        }

        private sealed class GreaterThanUInt64 : GreaterThanInstruction
        {
            public GreaterThanUInt64(object? nullValue)
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
                    frame.Push((ulong)left > (ulong)right);
                }
                return 1;
            }
        }

        private sealed class GreaterThanSingle : GreaterThanInstruction
        {
            public GreaterThanSingle(object? nullValue)
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
                    frame.Push((float)left > (float)right);
                }
                return 1;
            }
        }

        private sealed class GreaterThanDouble : GreaterThanInstruction
        {
            public GreaterThanDouble(object? nullValue)
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
                    frame.Push((double)left > (double)right);
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
                    TypeCode.SByte => s_liftedToNullSByte ??= new GreaterThanSByte(null),
                    TypeCode.Int16 => s_liftedToNullInt16 ??= new GreaterThanInt16(null),
                    TypeCode.Char => s_liftedToNullChar ??= new GreaterThanChar(null),
                    TypeCode.Int32 => s_liftedToNullInt32 ??= new GreaterThanInt32(null),
                    TypeCode.Int64 => s_liftedToNullInt64 ??= new GreaterThanInt64(null),
                    TypeCode.Byte => s_liftedToNullByte ??= new GreaterThanByte(null),
                    TypeCode.UInt16 => s_liftedToNullUInt16 ??= new GreaterThanUInt16(null),
                    TypeCode.UInt32 => s_liftedToNullUInt32 ??= new GreaterThanUInt32(null),
                    TypeCode.UInt64 => s_liftedToNullUInt64 ??= new GreaterThanUInt64(null),
                    TypeCode.Single => s_liftedToNullSingle ??= new GreaterThanSingle(null),
                    TypeCode.Double => s_liftedToNullDouble ??= new GreaterThanDouble(null),
                    _ => throw ContractUtils.Unreachable,
                };
            }
            else
            {
                return type.GetNonNullableType().GetTypeCode() switch
                {
                    TypeCode.SByte => s_SByte ??= new GreaterThanSByte(Utils.BoxedFalse),
                    TypeCode.Int16 => s_Int16 ??= new GreaterThanInt16(Utils.BoxedFalse),
                    TypeCode.Char => s_Char ??= new GreaterThanChar(Utils.BoxedFalse),
                    TypeCode.Int32 => s_Int32 ??= new GreaterThanInt32(Utils.BoxedFalse),
                    TypeCode.Int64 => s_Int64 ??= new GreaterThanInt64(Utils.BoxedFalse),
                    TypeCode.Byte => s_Byte ??= new GreaterThanByte(Utils.BoxedFalse),
                    TypeCode.UInt16 => s_UInt16 ??= new GreaterThanUInt16(Utils.BoxedFalse),
                    TypeCode.UInt32 => s_UInt32 ??= new GreaterThanUInt32(Utils.BoxedFalse),
                    TypeCode.UInt64 => s_UInt64 ??= new GreaterThanUInt64(Utils.BoxedFalse),
                    TypeCode.Single => s_Single ??= new GreaterThanSingle(Utils.BoxedFalse),
                    TypeCode.Double => s_Double ??= new GreaterThanDouble(Utils.BoxedFalse),
                    _ => throw ContractUtils.Unreachable,
                };
            }
        }
    }
}
