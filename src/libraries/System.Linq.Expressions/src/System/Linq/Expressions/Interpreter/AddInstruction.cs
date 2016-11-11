// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class AddInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "Add";

        private AddInstruction() { }

        private sealed class AddInt32 : AddInstruction
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
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(unchecked((int)l + (int)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        private sealed class AddInt16 : AddInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((short)((short)l + (short)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        private sealed class AddInt64 : AddInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((long)((long)l + (long)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        private sealed class AddUInt16 : AddInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((ushort)((ushort)l + (ushort)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        private sealed class AddUInt32 : AddInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((uint)((uint)l + (uint)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        private sealed class AddUInt64 : AddInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((ulong)((ulong)l + (ulong)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        private sealed class AddSingle : AddInstruction
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
                    frame.Data[frame.StackIndex - 2] = (float)((float)l + (float)r);
                }
                frame.StackIndex--;
                return +1;
            }
        }

        private sealed class AddDouble : AddInstruction
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
                    frame.Data[frame.StackIndex - 2] = (double)l + (double)r;
                }
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(type.IsArithmetic());
            switch (type.GetNonNullableType().GetTypeCode())
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
                    throw ContractUtils.Unreachable;
            }
        }
    }

    internal abstract class AddOvfInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "AddOvf";

        private AddOvfInstruction() { }

        private sealed class AddOvfInt32 : AddOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(checked((int)l + (int)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        private sealed class AddOvfInt16 : AddOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((short)((short)l + (short)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        private sealed class AddOvfInt64 : AddOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((long)((long)l + (long)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        private sealed class AddOvfUInt16 : AddOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((ushort)((ushort)l + (ushort)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        private sealed class AddOvfUInt32 : AddOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((uint)((uint)l + (uint)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        private sealed class AddOvfUInt64 : AddOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((ulong)((ulong)l + (ulong)r));
                }
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(type.IsArithmetic());
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new AddOvfInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new AddOvfInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new AddOvfInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new AddOvfUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new AddOvfUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new AddOvfUInt64());

                default:
                    return AddInstruction.Create(type);
            }
        }
    }
}
