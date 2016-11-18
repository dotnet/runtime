// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class EqualInstruction : Instruction
    {
        // Perf: EqualityComparer<T> but is 3/2 to 2 times slower.
        private static Instruction s_reference, s_boolean, s_SByte, s_int16, s_char, s_int32, s_int64, s_byte, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;
        private static Instruction s_referenceLiftedToNull, s_booleanLiftedToNull, s_SByteLiftedToNull, s_int16LiftedToNull, s_charLiftedToNull, s_int32LiftedToNull, s_int64LiftedToNull, s_byteLiftedToNull, s_UInt16LiftedToNull, s_UInt32LiftedToNull, s_UInt64LiftedToNull, s_singleLiftedToNull, s_doubleLiftedToNull;

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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
            }
        }

        private sealed class EqualReference : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(frame.Pop() == frame.Pop());
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
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
                return +1;
            }
        }


        private sealed class EqualReferenceLiftedToNull : EqualInstruction
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
                    frame.Push(left == right);
                }
                return +1;
            }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type, bool liftedToNull)
        {
            // Boxed enums can be unboxed as their underlying types:
            Type underlyingType = type.GetTypeInfo().IsEnum ? Enum.GetUnderlyingType(type) : type.GetNonNullableType();

            if (liftedToNull)
            {
                switch (underlyingType.GetTypeCode())
                {
                    case TypeCode.Boolean: return s_booleanLiftedToNull ?? (s_booleanLiftedToNull = new EqualBooleanLiftedToNull());
                    case TypeCode.SByte: return s_SByteLiftedToNull ?? (s_SByteLiftedToNull = new EqualSByteLiftedToNull());
                    case TypeCode.Byte: return s_byteLiftedToNull ?? (s_byteLiftedToNull = new EqualByteLiftedToNull());
                    case TypeCode.Char: return s_charLiftedToNull ?? (s_charLiftedToNull = new EqualCharLiftedToNull());
                    case TypeCode.Int16: return s_int16LiftedToNull ?? (s_int16LiftedToNull = new EqualInt16LiftedToNull());
                    case TypeCode.Int32: return s_int32LiftedToNull ?? (s_int32LiftedToNull = new EqualInt32LiftedToNull());
                    case TypeCode.Int64: return s_int64LiftedToNull ?? (s_int64LiftedToNull = new EqualInt64LiftedToNull());

                    case TypeCode.UInt16: return s_UInt16LiftedToNull ?? (s_UInt16LiftedToNull = new EqualUInt16LiftedToNull());
                    case TypeCode.UInt32: return s_UInt32LiftedToNull ?? (s_UInt32LiftedToNull = new EqualUInt32LiftedToNull());
                    case TypeCode.UInt64: return s_UInt64LiftedToNull ?? (s_UInt64LiftedToNull = new EqualUInt64LiftedToNull());

                    case TypeCode.Single: return s_singleLiftedToNull ?? (s_singleLiftedToNull = new EqualSingleLiftedToNull());
                    case TypeCode.Double: return s_doubleLiftedToNull ?? (s_doubleLiftedToNull = new EqualDoubleLiftedToNull());

                    case TypeCode.String:
                    case TypeCode.Object:
                        if (!type.GetTypeInfo().IsValueType)
                        {
                            return s_referenceLiftedToNull ?? (s_referenceLiftedToNull = new EqualReferenceLiftedToNull());
                        }
                        // TODO: Nullable<T>
                        throw Error.ExpressionNotSupportedForNullableType("Equal", type);

                    default:
                        throw Error.ExpressionNotSupportedForType("Equal", type);
                }
            }
            else
            {
                switch (underlyingType.GetTypeCode())
                {
                    case TypeCode.Boolean: return s_boolean ?? (s_boolean = new EqualBoolean());
                    case TypeCode.SByte: return s_SByte ?? (s_SByte = new EqualSByte());
                    case TypeCode.Byte: return s_byte ?? (s_byte = new EqualByte());
                    case TypeCode.Char: return s_char ?? (s_char = new EqualChar());
                    case TypeCode.Int16: return s_int16 ?? (s_int16 = new EqualInt16());
                    case TypeCode.Int32: return s_int32 ?? (s_int32 = new EqualInt32());
                    case TypeCode.Int64: return s_int64 ?? (s_int64 = new EqualInt64());

                    case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new EqualUInt16());
                    case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new EqualUInt32());
                    case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new EqualUInt64());

                    case TypeCode.Single: return s_single ?? (s_single = new EqualSingle());
                    case TypeCode.Double: return s_double ?? (s_double = new EqualDouble());

                    case TypeCode.String:
                    case TypeCode.Object:
                        if (!type.GetTypeInfo().IsValueType)
                        {
                            return s_reference ?? (s_reference = new EqualReference());
                        }
                        // TODO: Nullable<T>
                        throw Error.ExpressionNotSupportedForNullableType("Equal", type);

                    default:
                        throw Error.ExpressionNotSupportedForType("Equal", type);
                }
            }
        }

        public override string ToString() => "Equal()";
    }
}
