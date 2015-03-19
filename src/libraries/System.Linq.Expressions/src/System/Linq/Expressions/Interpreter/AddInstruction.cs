// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class AddInstruction : Instruction
    {
        private static Instruction s_int16,s_int32,s_int64,s_UInt16,s_UInt32,s_UInt64,s_single,s_double;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "Add"; }
        }


        private AddInstruction()
        {
        }

        internal sealed class AddInt32 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(unchecked((Int32)l + (Int32)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddInt16 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (Int16)unchecked((Int16)l + (Int16)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddInt64 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (Int64)unchecked((Int64)l + (Int64)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddUInt16 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (UInt16)unchecked((UInt16)l + (UInt16)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddUInt32 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (UInt32)unchecked((UInt32)l + (UInt32)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddUInt64 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (UInt64)unchecked((UInt64)l + (UInt64)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddSingle : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (Single)((Single)l + (Single)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddDouble : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (Double)l + (Double)r;
                }
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new AddInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new AddInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new AddInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new AddUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new AddUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new AddUInt64());
                case TypeCode.Single: return s_single ?? (s_single = new AddSingle());
                case TypeCode.Double: return s_double ?? (s_double = new AddDouble());

                default:
                    throw Error.ExpressionNotSupportedForType("Add", type);
            }
        }

        public override string ToString()
        {
            return "Add()";
        }
    }

    internal abstract class AddOvfInstruction : Instruction
    {
        private static Instruction s_int16,s_int32,s_int64,s_UInt16,s_UInt32,s_UInt64,s_single,s_double;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }

        public override string InstructionName
        {
            get { return "AddOvf"; }
        }

        private AddOvfInstruction()
        {
        }

        internal sealed class AddOvfInt32 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(checked((Int32)l + (Int32)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfInt16 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (Int16)checked((Int16)l + (Int16)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfInt64 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (Int64)checked((Int64)l + (Int64)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfUInt16 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (UInt16)checked((UInt16)l + (UInt16)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfUInt32 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (UInt32)checked((UInt32)l + (UInt32)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfUInt64 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (UInt64)checked((Int16)l + (Int16)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfSingle : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (Single)((Single)l + (Single)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfDouble : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                if (l == null || r == null)
                {
                    frame.Data[frame.StackIndex - 2] = null;
                }
                else
                {
                    frame.Data[frame.StackIndex - 2] = (Double)l + (Double)r;
                }
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new AddOvfInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new AddOvfInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new AddOvfInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new AddOvfUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new AddOvfUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new AddOvfUInt64());
                case TypeCode.Single: return s_single ?? (s_single = new AddOvfSingle());
                case TypeCode.Double: return s_double ?? (s_double = new AddOvfDouble());

                default:
                    throw Error.ExpressionNotSupportedForType("AddOvf", type);
            }
        }

        public override string ToString()
        {
            return "AddOvf()";
        }
    }
}
