// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class SubInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "Sub"; }
        }
        private SubInstruction()
        {
        }

        internal sealed class SubInt32 : SubInstruction
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
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(unchecked((Int32)l - (Int32)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubInt16 : SubInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((Int16)((Int16)l - (Int16)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubInt64 : SubInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((Int64)((Int64)l - (Int64)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubUInt16 : SubInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((UInt16)((UInt16)l - (UInt16)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubUInt32 : SubInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((UInt32)((UInt32)l - (UInt32)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubUInt64 : SubInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((UInt64)((UInt64)l - (UInt64)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubSingle : SubInstruction
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
                    frame.Data[frame.StackIndex - 2] = (Single)((Single)l - (Single)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubDouble : SubInstruction
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
                    frame.Data[frame.StackIndex - 2] = (Double)l - (Double)r;
                }
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(TypeUtils.IsArithmetic(type));
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new SubInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new SubInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new SubInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new SubUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new SubUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new SubUInt64());
                case TypeCode.Single: return s_single ?? (s_single = new SubSingle());
                case TypeCode.Double: return s_double ?? (s_double = new SubDouble());
                default:
                    throw ContractUtils.Unreachable;
            }
        }
    }

    internal abstract class SubOvfInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "SubOvf"; }
        }
        private SubOvfInstruction()
        {
        }

        internal sealed class SubOvfInt32 : SubOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(checked((Int32)l - (Int32)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubOvfInt16 : SubOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((Int16)((Int16)l - (Int16)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubOvfInt64 : SubOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((Int64)((Int64)l - (Int64)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubOvfUInt16 : SubOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((UInt16)((UInt16)l - (UInt16)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubOvfUInt32 : SubOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((UInt32)((UInt32)l - (UInt32)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubOvfUInt64 : SubOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((UInt64)((UInt64)l - (UInt64)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(TypeUtils.IsArithmetic(type));
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new SubOvfInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new SubOvfInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new SubOvfInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new SubOvfUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new SubOvfUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new SubOvfUInt64());
                default:
                    return SubInstruction.Create(type);
            }
        }
    }
}
