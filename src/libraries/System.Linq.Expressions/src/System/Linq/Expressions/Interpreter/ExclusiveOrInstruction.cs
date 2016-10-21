// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class ExclusiveOrInstruction : Instruction
    {
        private static Instruction s_SByte, s_int16, s_int32, s_int64, s_byte, s_UInt16, s_UInt32, s_UInt64, s_bool;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "ExclusiveOr";

        private ExclusiveOrInstruction() { }

        internal sealed class ExclusiveOrSByte : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((sbyte)(((sbyte)left) ^ ((sbyte)right)));
                return +1;
            }
        }

        internal sealed class ExclusiveOrInt16 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((short)(((short)left) ^ ((short)right)));
                return +1;
            }
        }

        internal sealed class ExclusiveOrInt32 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((int)left) ^ ((int)right));
                return +1;
            }
        }

        internal sealed class ExclusiveOrInt64 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((long)left) ^ ((long)right));
                return +1;
            }
        }

        internal sealed class ExclusiveOrByte : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((byte)(((byte)left) ^ ((byte)right)));
                return +1;
            }
        }

        internal sealed class ExclusiveOrUInt16 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((ushort)(((ushort)left) ^ ((ushort)right)));
                return +1;
            }
        }

        internal sealed class ExclusiveOrUInt32 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((uint)left) ^ ((uint)right));
                return +1;
            }
        }

        internal sealed class ExclusiveOrUInt64 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((ulong)left) ^ ((ulong)right));
                return +1;
            }
        }

        internal sealed class ExclusiveOrBool : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object left = frame.Pop();
                object right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((bool)left) ^ ((bool)right));
                return +1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type)
        {
            // Boxed enums can be unboxed as their underlying types:
            Type underlyingType = type.GetTypeInfo().IsEnum ? Enum.GetUnderlyingType(type) : TypeUtils.GetNonNullableType(type);

            switch (underlyingType.GetTypeCode())
            {
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new ExclusiveOrSByte());
                case TypeCode.Byte: return s_byte ?? (s_byte = new ExclusiveOrByte());
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new ExclusiveOrInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new ExclusiveOrInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new ExclusiveOrInt64());

                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new ExclusiveOrUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new ExclusiveOrUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new ExclusiveOrUInt64());
                case TypeCode.Boolean: return s_bool ?? (s_bool = new ExclusiveOrBool());

                default:
                    throw Error.ExpressionNotSupportedForType("ExclusiveOr", type);
            }
        }
    }
}