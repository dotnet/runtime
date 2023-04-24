// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class DecrementInstruction : Instruction
    {
        private static Instruction? s_Int16, s_Int32, s_Int64, s_UInt16, s_UInt32, s_UInt64, s_Single, s_Double;

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "Decrement";

        private DecrementInstruction() { }

        private sealed class DecrementInt16 : DecrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((short)((short)obj - 1)));
                }
                return 1;
            }
        }

        private sealed class DecrementInt32 : DecrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((int)obj - 1));
                }
                return 1;
            }
        }

        private sealed class DecrementInt64 : DecrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((long)obj - 1));
                }
                return 1;
            }
        }

        private sealed class DecrementUInt16 : DecrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((ushort)((ushort)obj - 1)));
                }
                return 1;
            }
        }

        private sealed class DecrementUInt32 : DecrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((uint)obj - 1));
                }
                return 1;
            }
        }

        private sealed class DecrementUInt64 : DecrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((ulong)obj - 1));
                }
                return 1;
            }
        }

        private sealed class DecrementSingle : DecrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((float)obj - 1));
                }
                return 1;
            }
        }

        private sealed class DecrementDouble : DecrementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(unchecked((double)obj - 1));
                }
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.IsEnum);
            return type.GetNonNullableType().GetTypeCode() switch
            {
                TypeCode.Int16 => s_Int16 ??= new DecrementInt16(),
                TypeCode.Int32 => s_Int32 ??= new DecrementInt32(),
                TypeCode.Int64 => s_Int64 ??= new DecrementInt64(),
                TypeCode.UInt16 => s_UInt16 ??= new DecrementUInt16(),
                TypeCode.UInt32 => s_UInt32 ??= new DecrementUInt32(),
                TypeCode.UInt64 => s_UInt64 ??= new DecrementUInt64(),
                TypeCode.Single => s_Single ??= new DecrementSingle(),
                TypeCode.Double => s_Double ??= new DecrementDouble(),
                _ => throw ContractUtils.Unreachable,
            };
        }
    }
}
