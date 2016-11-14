// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class DecrementInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "Decrement";

        private DecrementInstruction() { }

        private sealed class DecrementInt32 : DecrementInstruction
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
                    frame.Push(ScriptingRuntimeHelpers.Int32ToObject(unchecked((int)obj - 1)));
                }
                return +1;
            }
        }

        private sealed class DecrementInt16 : DecrementInstruction
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
                    frame.Push(unchecked((short)((short)obj - 1)));
                }
                return +1;
            }
        }

        private sealed class DecrementInt64 : DecrementInstruction
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
                    frame.Push(unchecked((long)((long)obj - 1)));
                }
                return +1;
            }
        }

        private sealed class DecrementUInt16 : DecrementInstruction
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
                    frame.Push(unchecked((ushort)((ushort)obj - 1)));
                }
                return +1;
            }
        }

        private sealed class DecrementUInt32 : DecrementInstruction
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
                    frame.Push(unchecked((uint)((uint)obj - 1)));
                }
                return +1;
            }
        }

        private sealed class DecrementUInt64 : DecrementInstruction
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
                    frame.Push(unchecked((ulong)((ulong)obj - 1)));
                }
                return +1;
            }
        }

        private sealed class DecrementSingle : DecrementInstruction
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
                    frame.Push(unchecked((float)((float)obj - 1)));
                }
                return +1;
            }
        }

        private sealed class DecrementDouble : DecrementInstruction
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
                    frame.Push(unchecked((double)((double)obj - 1)));
                }
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new DecrementInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new DecrementInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new DecrementInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new DecrementUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new DecrementUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new DecrementUInt64());
                case TypeCode.Single: return s_single ?? (s_single = new DecrementSingle());
                case TypeCode.Double: return s_double ?? (s_double = new DecrementDouble());

                default:
                    throw Error.ExpressionNotSupportedForType("Decrement", type);
            }
        }
    }
}