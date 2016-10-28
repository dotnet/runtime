// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class IncrementInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "Increment";

        private IncrementInstruction() { }

        private sealed class IncrementInt32 : IncrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.Int32ToObject(unchecked(1 + (int)obj)));
                }
                return +1;
            }
        }

        private sealed class IncrementInt16 : IncrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((short)(1 + (short)obj)));
                }
                return +1;
            }
        }

        private sealed class IncrementInt64 : IncrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((long)(1 + (long)obj)));
                }
                return +1;
            }
        }

        private sealed class IncrementUInt16 : IncrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((ushort)(1 + (ushort)obj)));
                }
                return +1;
            }
        }

        private sealed class IncrementUInt32 : IncrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((uint)(1 + (uint)obj)));
                }
                return +1;
            }
        }

        private sealed class IncrementUInt64 : IncrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((ulong)(1 + (ulong)obj)));
                }
                return +1;
            }
        }

        private sealed class IncrementSingle : IncrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((float)(1 + (float)obj)));
                }
                return +1;
            }
        }

        private sealed class IncrementDouble : IncrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((double)(1 + (double)obj)));
                }
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (TypeUtils.GetNonNullableType(type).GetTypeCode())
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new IncrementInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new IncrementInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new IncrementInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new IncrementUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new IncrementUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new IncrementUInt64());
                case TypeCode.Single: return s_single ?? (s_single = new IncrementSingle());
                case TypeCode.Double: return s_double ?? (s_double = new IncrementDouble());

                default:
                    throw Error.ExpressionNotSupportedForType("Increment", type);
            }
        }
    }
}