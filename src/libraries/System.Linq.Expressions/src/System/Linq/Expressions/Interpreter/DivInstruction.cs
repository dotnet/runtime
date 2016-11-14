// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class DivInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "Div";

        private DivInstruction() { }

        private sealed class DivInt32 : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject((int)l / (int)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class DivInt16 : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (short)((short)l / (short)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class DivInt64 : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (long)((long)l / (long)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class DivUInt16 : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (ushort)((ushort)l / (ushort)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class DivUInt32 : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (uint)((uint)l / (uint)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class DivUInt64 : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (ulong)((ulong)l / (ulong)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class DivSingle : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (float)((float)l / (float)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class DivDouble : DivInstruction
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
                    frame.Data[frame.StackIndex - 2] = (double)l / (double)r;
                }
                frame.StackIndex--;
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (type.GetNonNullableType().GetTypeCode())
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
    }

    internal abstract class ModuloInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "Modulo";

        private ModuloInstruction() { }

        private sealed class ModuloInt32 : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject((int)l % (int)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class ModuloInt16 : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (short)((short)l % (short)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class ModuloInt64 : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (long)((long)l % (long)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class ModuloUInt16 : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (ushort)((ushort)l % (ushort)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class ModuloUInt32 : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (uint)((uint)l % (uint)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class ModuloUInt64 : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (ulong)((ulong)l % (ulong)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class ModuloSingle : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (float)((float)l % (float)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class ModuloDouble : ModuloInstruction
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
                    frame.Data[frame.StackIndex - 2] = (double)l % (double)r;
                }
                frame.StackIndex--;
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (type.GetNonNullableType().GetTypeCode())
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
    }
}
