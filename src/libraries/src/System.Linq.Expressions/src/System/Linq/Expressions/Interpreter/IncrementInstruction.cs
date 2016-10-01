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

        internal sealed class IncrementInt32 : IncrementInstruction
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
                    frame.Push(ScriptingRuntimeHelpers.Int32ToObject(unchecked(1 + (Int32)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementInt16 : IncrementInstruction
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
                    frame.Push(unchecked((Int16)(1 + (Int16)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementInt64 : IncrementInstruction
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
                    frame.Push(unchecked((Int64)(1 + (Int64)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementUInt16 : IncrementInstruction
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
                    frame.Push(unchecked((UInt16)(1 + (UInt16)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementUInt32 : IncrementInstruction
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
                    frame.Push(unchecked((UInt32)(1 + (UInt32)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementUInt64 : IncrementInstruction
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
                    frame.Push(unchecked((UInt64)(1 + (UInt64)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementSingle : IncrementInstruction
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
                    frame.Push(unchecked((Single)(1 + (Single)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementDouble : IncrementInstruction
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
                    frame.Push(unchecked((Double)(1 + (Double)obj)));
                }
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
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