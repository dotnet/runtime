// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class SubInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "Sub";

        private SubInstruction() { }

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
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(unchecked((int)l - (int)r));
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
                    frame.Data[frame.StackIndex - 2] = unchecked((short)((short)l - (short)r));
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
                    frame.Data[frame.StackIndex - 2] = unchecked((long)((long)l - (long)r));
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
                    frame.Data[frame.StackIndex - 2] = unchecked((ushort)((ushort)l - (ushort)r));
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
                    frame.Data[frame.StackIndex - 2] = unchecked((uint)((uint)l - (uint)r));
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
                    frame.Data[frame.StackIndex - 2] = unchecked((ulong)((ulong)l - (ulong)r));
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
                    frame.Data[frame.StackIndex - 2] = (float)((float)l - (float)r);
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
                    frame.Data[frame.StackIndex - 2] = (double)l - (double)r;
                }
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(TypeUtils.IsArithmetic(type));
            switch (TypeUtils.GetNonNullableType(type).GetTypeCode())
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

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "SubOvf";

        private SubOvfInstruction() { }

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
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(checked((int)l - (int)r));
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
                    frame.Data[frame.StackIndex - 2] = checked((short)((short)l - (short)r));
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
                    frame.Data[frame.StackIndex - 2] = checked((long)((long)l - (long)r));
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
                    frame.Data[frame.StackIndex - 2] = checked((ushort)((ushort)l - (ushort)r));
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
                    frame.Data[frame.StackIndex - 2] = checked((uint)((uint)l - (uint)r));
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
                    frame.Data[frame.StackIndex - 2] = checked((ulong)((ulong)l - (ulong)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(TypeUtils.IsArithmetic(type));
            switch (TypeUtils.GetNonNullableType(type).GetTypeCode())
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
