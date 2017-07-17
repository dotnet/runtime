// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class NegateInstruction : Instruction
    {
        private static Instruction s_Int16, s_Int32, s_Int64, s_Single, s_Double;

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "Negate";

        private NegateInstruction() { }

        private sealed class NegateInt16 : NegateInstruction
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
                    frame.Push(unchecked((short)(-(short)obj)));
                }
                return 1;
            }
        }

        private sealed class NegateInt32 : NegateInstruction
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
                    frame.Push(unchecked(-(int)obj));
                }
                return 1;
            }
        }

        private sealed class NegateInt64 : NegateInstruction
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
                    frame.Push(unchecked(-(long)obj));
                }
                return 1;
            }
        }

        private sealed class NegateSingle : NegateInstruction
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
                    frame.Push(-(float)obj);
                }
                return 1;
            }
        }

        private sealed class NegateDouble : NegateInstruction
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
                    frame.Push(-(double)obj);
                }
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.IsEnum);
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new NegateInt16());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new NegateInt32());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new NegateInt64());
                case TypeCode.Single: return s_Single ?? (s_Single = new NegateSingle());
                case TypeCode.Double: return s_Double ?? (s_Double = new NegateDouble());
                default:
                    throw ContractUtils.Unreachable;
            }
        }
    }

    internal abstract class NegateCheckedInstruction : Instruction
    {
        private static Instruction s_Int16, s_Int32, s_Int64;

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "NegateChecked";

        private NegateCheckedInstruction() { }

        private sealed class NegateCheckedInt32 : NegateCheckedInstruction
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
                    frame.Push(checked(-(int)obj));
                }
                return 1;
            }
        }

        private sealed class NegateCheckedInt16 : NegateCheckedInstruction
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
                    frame.Push(checked((short)(-(short)obj)));
                }
                return 1;
            }
        }

        private sealed class NegateCheckedInt64 : NegateCheckedInstruction
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
                    frame.Push(checked(-(long)obj));
                }
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.IsEnum);
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new NegateCheckedInt16());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new NegateCheckedInt32());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new NegateCheckedInt64());
                default:
                    return NegateInstruction.Create(type);
            }
        }
    }
}
