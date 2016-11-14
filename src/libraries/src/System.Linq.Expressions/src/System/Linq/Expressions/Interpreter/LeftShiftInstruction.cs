// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class LeftShiftInstruction : Instruction
    {
        private static Instruction s_SByte, s_int16, s_int32, s_int64, s_byte, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "LeftShift";

        private LeftShiftInstruction() { }

        private sealed class LeftShiftSByte : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((sbyte)(((sbyte)value) << ((int)shift)));
                }
                return +1;
            }
        }

        private sealed class LeftShiftInt16 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((short)(((short)value) << ((int)shift)));
                }
                return +1;
            }
        }

        private sealed class LeftShiftInt32 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(((int)value) << ((int)shift));
                }
                return +1;
            }
        }

        private sealed class LeftShiftInt64 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(((long)value) << ((int)shift));
                }
                return +1;
            }
        }

        private sealed class LeftShiftByte : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((byte)(((byte)value) << ((int)shift)));
                }
                return +1;
            }
        }

        private sealed class LeftShiftUInt16 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((ushort)(((ushort)value) << ((int)shift)));
                }
                return +1;
            }
        }

        private sealed class LeftShiftUInt32 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(((uint)value) << ((int)shift));
                }
                return +1;
            }
        }

        private sealed class LeftShiftUInt64 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object shift = frame.Pop();
                object value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(((ulong)value) << ((int)shift));
                }
                return +1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type)
        {
            // Boxed enums can be unboxed as their underlying types:
            Type underlyingType = type.GetTypeInfo().IsEnum ? Enum.GetUnderlyingType(type) : type.GetNonNullableType();

            switch (underlyingType.GetTypeCode())
            {
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new LeftShiftSByte());
                case TypeCode.Byte: return s_byte ?? (s_byte = new LeftShiftByte());
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new LeftShiftInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new LeftShiftInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new LeftShiftInt64());

                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new LeftShiftUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new LeftShiftUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new LeftShiftUInt64());

                default:
                    throw Error.ExpressionNotSupportedForType("LeftShift", type);
            }
        }
    }
}