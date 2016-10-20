// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class NotInstruction : Instruction
    {
        public static Instruction _Bool, _Int64, _Int32, _Int16, _UInt64, _UInt32, _UInt16, _Byte, _SByte;

        private NotInstruction() { }

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "Not";

        private class BoolNot : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((bool)value ? ScriptingRuntimeHelpers.False : ScriptingRuntimeHelpers.True);
                }
                return +1;
            }
        }

        private class Int64Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((long)~(long)value);
                }
                return +1;
            }
        }

        private class Int32Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((int)(~(int)value));
                }
                return +1;
            }
        }

        private class Int16Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((short)(~(short)value));
                }
                return +1;
            }
        }

        private class UInt64Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((ulong)(~(ulong)value));
                }
                return +1;
            }
        }

        private class UInt32Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((uint)(~(uint)value));
                }
                return +1;
            }
        }

        private class UInt16Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((ushort)(~(ushort)value));
                }
                return +1;
            }
        }

        private class ByteNot : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((object)(byte)(~(byte)value));
                }
                return +1;
            }
        }

        private class SByteNot : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((object)(sbyte)(~(sbyte)value));
                }
                return +1;
            }
        }

        public static Instruction Create(Type t)
        {
            switch (TypeUtils.GetNonNullableType(t).GetTypeCode())
            {
                case TypeCode.Boolean: return _Bool ?? (_Bool = new BoolNot());
                case TypeCode.Int64: return _Int64 ?? (_Int64 = new Int64Not());
                case TypeCode.Int32: return _Int32 ?? (_Int32 = new Int32Not());
                case TypeCode.Int16: return _Int16 ?? (_Int16 = new Int16Not());
                case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new UInt64Not());
                case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new UInt32Not());
                case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new UInt16Not());
                case TypeCode.Byte: return _Byte ?? (_Byte = new ByteNot());
                case TypeCode.SByte: return _SByte ?? (_SByte = new SByteNot());
                default:
                    throw new InvalidOperationException("Not for " + t.ToString());
            }
        }
    }
}
