// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class NegateInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_single, s_double;

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "Negate";

        private NegateInstruction() { }

        internal sealed class NegateInt32 : NegateInstruction
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
                    frame.Push(ScriptingRuntimeHelpers.Int32ToObject(unchecked(-(Int32)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateInt16 : NegateInstruction
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
                    frame.Push(unchecked((Int16)(-(Int16)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateInt64 : NegateInstruction
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
                    frame.Push(unchecked((Int64)(-(Int64)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateSingle : NegateInstruction
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
                    frame.Push(unchecked((Single)(-(Single)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateDouble : NegateInstruction
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
                    frame.Push(unchecked((Double)(-(Double)obj)));
                }
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new NegateInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new NegateInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new NegateInt64());
                case TypeCode.Single: return s_single ?? (s_single = new NegateSingle());
                case TypeCode.Double: return s_double ?? (s_double = new NegateDouble());

                default:
                    throw Error.ExpressionNotSupportedForType("Negate", type);
            }
        }
    }

    internal abstract class NegateCheckedInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_single, s_double;

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "NegateChecked";

        private NegateCheckedInstruction() { }

        internal sealed class NegateCheckedInt32 : NegateCheckedInstruction
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
                    frame.Push(ScriptingRuntimeHelpers.Int32ToObject(checked(-(Int32)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateCheckedInt16 : NegateCheckedInstruction
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
                    frame.Push(checked((Int16)(-(Int16)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateCheckedInt64 : NegateCheckedInstruction
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
                    frame.Push(checked((Int64)(-(Int64)obj)));
                }
                return +1;
            }
        }
        internal sealed class NegateCheckedSingle : NegateCheckedInstruction
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
                    frame.Push(checked((Single)(-(Single)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateCheckedDouble : NegateCheckedInstruction
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
                    frame.Push(checked((Double)(-(Double)obj)));
                }
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new NegateCheckedInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new NegateCheckedInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new NegateCheckedInt64());
                case TypeCode.Single: return s_single ?? (s_single = new NegateCheckedSingle());
                case TypeCode.Double: return s_double ?? (s_double = new NegateCheckedDouble());
                default:
                    throw Error.ExpressionNotSupportedForType("NegateChecked", type);
            }
        }
    }
}
