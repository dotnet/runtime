// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class GreaterThanInstruction : Instruction
    {
        private readonly object _nullValue;
        private static Instruction s_SByte, s_int16, s_char, s_int32, s_int64, s_byte, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;
        private static Instruction s_liftedToNullSByte, s_liftedToNullInt16, s_liftedToNullChar, s_liftedToNullInt32, s_liftedToNullInt64, s_liftedToNullByte, s_liftedToNullUInt16, s_liftedToNullUInt32, s_liftedToNullUInt64, s_liftedToNullSingle, s_liftedToNullDouble;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "GreaterThan";

        private GreaterThanInstruction(object nullValue)
        {
            _nullValue = nullValue;
        }

        private sealed class GreaterThanSByte : GreaterThanInstruction
        {
            public GreaterThanSByte(object nullValue)
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
                    frame.Push(((sbyte)left) > (sbyte)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanInt16 : GreaterThanInstruction
        {
            public GreaterThanInt16(object nullValue)
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
                    frame.Push(((short)left) > (short)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanChar : GreaterThanInstruction
        {
            public GreaterThanChar(object nullValue)
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
                    frame.Push(((char)left) > (char)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanInt32 : GreaterThanInstruction
        {
            public GreaterThanInt32(object nullValue)
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
                    frame.Push(((int)left) > (int)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanInt64 : GreaterThanInstruction
        {
            public GreaterThanInt64(object nullValue)
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
                    frame.Push(((long)left) > (long)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanByte : GreaterThanInstruction
        {
            public GreaterThanByte(object nullValue)
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
                    frame.Push(((byte)left) > (byte)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanUInt16 : GreaterThanInstruction
        {
            public GreaterThanUInt16(object nullValue)
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
                    frame.Push(((ushort)left) > (ushort)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanUInt32 : GreaterThanInstruction
        {
            public GreaterThanUInt32(object nullValue)
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
                    frame.Push(((uint)left) > (uint)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanUInt64 : GreaterThanInstruction
        {
            public GreaterThanUInt64(object nullValue)
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
                    frame.Push(((ulong)left) > (ulong)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanSingle : GreaterThanInstruction
        {
            public GreaterThanSingle(object nullValue)
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
                    frame.Push(((float)left) > (float)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanDouble : GreaterThanInstruction
        {
            public GreaterThanDouble(object nullValue)
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
                    frame.Push(((double)left) > (double)right);
                }
                return +1;
            }
        }

        public static Instruction Create(Type type, bool liftedToNull = false)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            if (liftedToNull)
            {
                switch (type.GetNonNullableType().GetTypeCode())
                {
                    case TypeCode.SByte: return s_liftedToNullSByte ?? (s_liftedToNullSByte = new GreaterThanSByte(null));
                    case TypeCode.Byte: return s_liftedToNullByte ?? (s_liftedToNullByte = new GreaterThanByte(null));
                    case TypeCode.Char: return s_liftedToNullChar ?? (s_liftedToNullChar = new GreaterThanChar(null));
                    case TypeCode.Int16: return s_liftedToNullInt16 ?? (s_liftedToNullInt16 = new GreaterThanInt16(null));
                    case TypeCode.Int32: return s_liftedToNullInt32 ?? (s_liftedToNullInt32 = new GreaterThanInt32(null));
                    case TypeCode.Int64: return s_liftedToNullInt64 ?? (s_liftedToNullInt64 = new GreaterThanInt64(null));
                    case TypeCode.UInt16: return s_liftedToNullUInt16 ?? (s_liftedToNullUInt16 = new GreaterThanUInt16(null));
                    case TypeCode.UInt32: return s_liftedToNullUInt32 ?? (s_liftedToNullUInt32 = new GreaterThanUInt32(null));
                    case TypeCode.UInt64: return s_liftedToNullUInt64 ?? (s_liftedToNullUInt64 = new GreaterThanUInt64(null));
                    case TypeCode.Single: return s_liftedToNullSingle ?? (s_liftedToNullSingle = new GreaterThanSingle(null));
                    case TypeCode.Double: return s_liftedToNullDouble ?? (s_liftedToNullDouble = new GreaterThanDouble(null));

                    default:
                        throw Error.ExpressionNotSupportedForType("GreaterThan", type);
                }
            }
            else
            {
                switch (type.GetNonNullableType().GetTypeCode())
                {
                    case TypeCode.SByte: return s_SByte ?? (s_SByte = new GreaterThanSByte(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Byte: return s_byte ?? (s_byte = new GreaterThanByte(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Char: return s_char ?? (s_char = new GreaterThanChar(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Int16: return s_int16 ?? (s_int16 = new GreaterThanInt16(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Int32: return s_int32 ?? (s_int32 = new GreaterThanInt32(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Int64: return s_int64 ?? (s_int64 = new GreaterThanInt64(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new GreaterThanUInt16(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new GreaterThanUInt32(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new GreaterThanUInt64(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Single: return s_single ?? (s_single = new GreaterThanSingle(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Double: return s_double ?? (s_double = new GreaterThanDouble(ScriptingRuntimeHelpers.Boolean_False));

                    default:
                        throw Error.ExpressionNotSupportedForType("GreaterThan", type);
                }
            }
        }

        public override string ToString() => "GreaterThan()";
    }

    internal abstract class GreaterThanOrEqualInstruction : Instruction
    {
        private readonly object _nullValue;
        private static Instruction s_SByte, s_int16, s_char, s_int32, s_int64, s_byte, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;
        private static Instruction s_liftedToNullSByte, s_liftedToNullInt16, s_liftedToNullChar, s_liftedToNullInt32, s_liftedToNullInt64, s_liftedToNullByte, s_liftedToNullUInt16, s_liftedToNullUInt32, s_liftedToNullUInt64, s_liftedToNullSingle, s_liftedToNullDouble;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "GreaterThanOrEqual";

        private GreaterThanOrEqualInstruction(object nullValue)
        {
            _nullValue = nullValue;
        }

        private sealed class GreaterThanOrEqualSByte : GreaterThanOrEqualInstruction
        {
            public GreaterThanOrEqualSByte(object nullValue)
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
                    frame.Push(((sbyte)left) >= (sbyte)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanOrEqualInt16 : GreaterThanOrEqualInstruction
        {
            public GreaterThanOrEqualInt16(object nullValue)
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
                    frame.Push(((short)left) >= (short)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanOrEqualChar : GreaterThanOrEqualInstruction
        {
            public GreaterThanOrEqualChar(object nullValue)
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
                    frame.Push(((char)left) >= (char)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanOrEqualInt32 : GreaterThanOrEqualInstruction
        {
            public GreaterThanOrEqualInt32(object nullValue)
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
                    frame.Push(((int)left) >= (int)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanOrEqualInt64 : GreaterThanOrEqualInstruction
        {
            public GreaterThanOrEqualInt64(object nullValue)
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
                    frame.Push(((long)left) >= (long)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanOrEqualByte : GreaterThanOrEqualInstruction
        {
            public GreaterThanOrEqualByte(object nullValue)
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
                    frame.Push(((byte)left) >= (byte)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanOrEqualUInt16 : GreaterThanOrEqualInstruction
        {
            public GreaterThanOrEqualUInt16(object nullValue)
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
                    frame.Push(((ushort)left) >= (ushort)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanOrEqualUInt32 : GreaterThanOrEqualInstruction
        {
            public GreaterThanOrEqualUInt32(object nullValue)
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
                    frame.Push(((uint)left) >= (uint)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanOrEqualUInt64 : GreaterThanOrEqualInstruction
        {
            public GreaterThanOrEqualUInt64(object nullValue)
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
                    frame.Push(((ulong)left) >= (ulong)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanOrEqualSingle : GreaterThanOrEqualInstruction
        {
            public GreaterThanOrEqualSingle(object nullValue)
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
                    frame.Push(((float)left) >= (float)right);
                }
                return +1;
            }
        }

        private sealed class GreaterThanOrEqualDouble : GreaterThanOrEqualInstruction
        {
            public GreaterThanOrEqualDouble(object nullValue)
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
                    frame.Push(((double)left) >= (double)right);
                }
                return +1;
            }
        }

        public static Instruction Create(Type type, bool liftedToNull = false)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            if (liftedToNull)
            {
                switch (type.GetNonNullableType().GetTypeCode())
                {
                    case TypeCode.SByte: return s_liftedToNullSByte ?? (s_liftedToNullSByte = new GreaterThanOrEqualSByte(null));
                    case TypeCode.Byte: return s_liftedToNullByte ?? (s_liftedToNullByte = new GreaterThanOrEqualByte(null));
                    case TypeCode.Char: return s_liftedToNullChar ?? (s_liftedToNullChar = new GreaterThanOrEqualChar(null));
                    case TypeCode.Int16: return s_liftedToNullInt16 ?? (s_liftedToNullInt16 = new GreaterThanOrEqualInt16(null));
                    case TypeCode.Int32: return s_liftedToNullInt32 ?? (s_liftedToNullInt32 = new GreaterThanOrEqualInt32(null));
                    case TypeCode.Int64: return s_liftedToNullInt64 ?? (s_liftedToNullInt64 = new GreaterThanOrEqualInt64(null));
                    case TypeCode.UInt16: return s_liftedToNullUInt16 ?? (s_liftedToNullUInt16 = new GreaterThanOrEqualUInt16(null));
                    case TypeCode.UInt32: return s_liftedToNullUInt32 ?? (s_liftedToNullUInt32 = new GreaterThanOrEqualUInt32(null));
                    case TypeCode.UInt64: return s_liftedToNullUInt64 ?? (s_liftedToNullUInt64 = new GreaterThanOrEqualUInt64(null));
                    case TypeCode.Single: return s_liftedToNullSingle ?? (s_liftedToNullSingle = new GreaterThanOrEqualSingle(null));
                    case TypeCode.Double: return s_liftedToNullDouble ?? (s_liftedToNullDouble = new GreaterThanOrEqualDouble(null));

                    default:
                        throw Error.ExpressionNotSupportedForType("GreaterThanOrEqual", type);
                }
            }
            else
            {
                switch (type.GetNonNullableType().GetTypeCode())
                {
                    case TypeCode.SByte: return s_SByte ?? (s_SByte = new GreaterThanOrEqualSByte(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Byte: return s_byte ?? (s_byte = new GreaterThanOrEqualByte(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Char: return s_char ?? (s_char = new GreaterThanOrEqualChar(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Int16: return s_int16 ?? (s_int16 = new GreaterThanOrEqualInt16(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Int32: return s_int32 ?? (s_int32 = new GreaterThanOrEqualInt32(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Int64: return s_int64 ?? (s_int64 = new GreaterThanOrEqualInt64(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new GreaterThanOrEqualUInt16(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new GreaterThanOrEqualUInt32(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new GreaterThanOrEqualUInt64(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Single: return s_single ?? (s_single = new GreaterThanOrEqualSingle(ScriptingRuntimeHelpers.Boolean_False));
                    case TypeCode.Double: return s_double ?? (s_double = new GreaterThanOrEqualDouble(ScriptingRuntimeHelpers.Boolean_False));

                    default:
                        throw Error.ExpressionNotSupportedForType("GreaterThanOrEqual", type);
                }
            }
        }

        public override string ToString() => "GreaterThanOrEqual()";
    }
}
