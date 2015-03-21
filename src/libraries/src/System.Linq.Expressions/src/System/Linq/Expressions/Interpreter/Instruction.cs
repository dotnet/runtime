// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter
{
    internal interface IInstructionProvider
    {
        void AddInstructions(LightCompiler compiler);
    }

    internal abstract partial class Instruction
    {
        public const int UnknownInstrIndex = int.MaxValue;

        public virtual int ConsumedStack { get { return 0; } }
        public virtual int ProducedStack { get { return 0; } }
        public virtual int ConsumedContinuations { get { return 0; } }
        public virtual int ProducedContinuations { get { return 0; } }

        public int StackBalance
        {
            get { return ProducedStack - ConsumedStack; }
        }

        public int ContinuationsBalance
        {
            get { return ProducedContinuations - ConsumedContinuations; }
        }

        public abstract int Run(InterpretedFrame frame);

        public virtual string InstructionName
        {
            get { return "<Unknown>"; }
        }

        public override string ToString()
        {
            return InstructionName + "()";
        }

        public virtual string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects)
        {
            return ToString();
        }

        public virtual object GetDebugCookie(LightCompiler compiler)
        {
            return null;
        }
    }

    internal abstract class NotInstruction : Instruction
    {
        public static Instruction _Bool, _Int64, _Int32, _Int16, _UInt64, _UInt32, _UInt16, _Byte, _SByte;

        private NotInstruction() { }
        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }

        public override string InstructionName
        {
            get { return "Not"; }
        }

        private class BoolNot : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var value = frame.Pop();
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
                var value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((Int64)~(Int64)value);
                }
                return +1;
            }
        }

        private class Int32Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((Int32)(~(Int32)value));
                }
                return +1;
            }
        }

        private class Int16Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((Int16)(~(Int16)value));
                }
                return +1;
            }
        }

        private class UInt64Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((UInt64)(~(UInt64)value));
                }
                return +1;
            }
        }

        private class UInt32Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((UInt32)(~(UInt32)value));
                }
                return +1;
            }
        }

        private class UInt16Not : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((UInt16)(~(UInt16)value));
                }
                return +1;
            }
        }

        private class ByteNot : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((object)(Byte)(~(Byte)value));
                }
                return +1;
            }
        }

        private class SByteNot : NotInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var value = frame.Pop();
                if (value == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((object)(SByte)(~(SByte)value));
                }
                return +1;
            }
        }

        public static Instruction Create(Type t)
        {
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(t)))
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
