// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class DivInstruction : Instruction
    {
        private static Instruction s_int16,s_int32,s_int64,s_UInt16,s_UInt32,s_UInt64,s_single,s_double;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "Div"; }
        }

        private DivInstruction()
        {
        }

        internal sealed class DivInt32 : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject((Int32)l / (Int32)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivInt16 : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (Int16)((Int16)l / (Int16)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivInt64 : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (Int64)((Int64)l / (Int64)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivUInt16 : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (UInt16)((UInt16)l / (UInt16)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivUInt32 : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (UInt32)((UInt32)l / (UInt32)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivUInt64 : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (UInt64)((UInt64)l / (UInt64)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivSingle : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (Single)((Single)l / (Single)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivDouble : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (Double)l / (Double)r;
                }
                frame.StackIndex--;
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new DivInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new DivInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new DivInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new DivUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new DivUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new DivUInt64());
                case TypeCode.Single: return s_single ?? (s_single = new DivSingle());
                case TypeCode.Double: return s_double ?? (s_double = new DivDouble());

                default:
                    throw Error.ExpressionNotSupportedForType("Div", type);
            }
        }

        public override string ToString()
        {
            return "Div()";
        }
    }

    internal abstract class ModuloInstruction : Instruction
    {
        private static Instruction s_int16,s_int32,s_int64,s_UInt16,s_UInt32,s_UInt64,s_single,s_double;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "Modulo"; }
        }

        private ModuloInstruction()
        {
        }

        internal sealed class ModuloInt32 : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject((Int32)l % (Int32)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class ModuloInt16 : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (Int16)((Int16)l % (Int16)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class ModuloInt64 : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (Int64)((Int64)l % (Int64)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class ModuloUInt16 : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (UInt16)((UInt16)l % (UInt16)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class ModuloUInt32 : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (UInt32)((UInt32)l % (UInt32)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class ModuloUInt64 : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (UInt64)((UInt64)l % (UInt64)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class ModuloSingle : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (Single)((Single)l % (Single)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class ModuloDouble : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (Double)l % (Double)r;
                }
                frame.StackIndex--;
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new ModuloInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new ModuloInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new ModuloInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new ModuloUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new ModuloUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new ModuloUInt64());
                case TypeCode.Single: return s_single ?? (s_single = new ModuloSingle());
                case TypeCode.Double: return s_double ?? (s_double = new ModuloDouble());

                default:
                    throw Error.ExpressionNotSupportedForType("Modulo", type);
            }
        }

        public override string ToString()
        {
            return "Modulo()";
        }
    }
}
