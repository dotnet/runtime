// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class MulInstruction : Instruction
    {
        private static Instruction s_Int16, s_Int32, s_Int64, s_UInt16, s_UInt32, s_UInt64, s_Single, s_Double;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "Mul";

        private MulInstruction() { }

        private sealed class MulInt16 : MulInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((short)((short)l * (short)r));
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class MulInt32 : MulInstruction
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
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(unchecked((int)l * (int)r));
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class MulInt64 : MulInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((long)l * (long)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class MulUInt16 : MulInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((ushort)((ushort)l * (ushort)r));
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class MulUInt32 : MulInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((uint)l * (uint)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class MulUInt64 : MulInstruction
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
                    frame.Data[frame.StackIndex - 2] = unchecked((ulong)l * (ulong)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class MulSingle : MulInstruction
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
                    frame.Data[frame.StackIndex - 2] = (float)l * (float)r;
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class MulDouble : MulInstruction
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
                    frame.Data[frame.StackIndex - 2] = (double)l * (double)r;
                }
                frame.StackIndex--;
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(type.IsArithmetic());
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new MulInt16());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new MulInt32());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new MulInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new MulUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new MulUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new MulUInt64());
                case TypeCode.Single: return s_Single ?? (s_Single = new MulSingle());
                case TypeCode.Double: return s_Double ?? (s_Double = new MulDouble());

                default:
                    throw ContractUtils.Unreachable;
            }
        }
    }

    internal abstract class MulOvfInstruction : Instruction
    {
        private static Instruction s_Int16, s_Int32, s_Int64, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "MulOvf";

        private MulOvfInstruction() { }

        private sealed class MulOvfInt16 : MulOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((short)((short)l * (short)r));
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class MulOvfInt32 : MulOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(checked((int)l * (int)r));
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class MulOvfInt64 : MulOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((long)l * (long)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class MulOvfUInt16 : MulOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((ushort)((ushort)l * (ushort)r));
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class MulOvfUInt32 : MulOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((uint)l * (uint)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        private sealed class MulOvfUInt64 : MulOvfInstruction
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
                    frame.Data[frame.StackIndex - 2] = checked((ulong)l * (ulong)r);
                }
                frame.StackIndex--;
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(type.IsArithmetic());
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.Int16: return s_Int16 ?? (s_Int16 = new MulOvfInt16());
                case TypeCode.Int32: return s_Int32 ?? (s_Int32 = new MulOvfInt32());
                case TypeCode.Int64: return s_Int64 ?? (s_Int64 = new MulOvfInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new MulOvfUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new MulOvfUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new MulOvfUInt64());
                default:
                    return MulInstruction.Create(type);
            }
        }
    }
}
