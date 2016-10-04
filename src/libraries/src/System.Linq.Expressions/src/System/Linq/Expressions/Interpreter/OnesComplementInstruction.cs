// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class OnesComplementInstruction : Instruction
    {
        private static Instruction s_byte, s_sbyte, s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "OnesComplement";

        private OnesComplementInstruction() { }

        internal sealed class OnesComplementInt32 : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.Int32ToObject(~(Int32)obj));
                }
                return +1;
            }
        }

        internal sealed class OnesComplementInt16 : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((Int16)(~(Int16)obj));
                }
                return +1;
            }
        }

        internal sealed class OnesComplementInt64 : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((Int64)(~(Int64)obj));
                }
                return +1;
            }
        }

        internal sealed class OnesComplementUInt16 : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((UInt16)(~(UInt16)obj));
                }
                return +1;
            }
        }

        internal sealed class OnesComplementUInt32 : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((UInt32)(~(UInt32)obj));
                }
                return +1;
            }
        }

        internal sealed class OnesComplementUInt64 : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((UInt64)(~(UInt64)obj));
                }
                return +1;
            }
        }

        internal sealed class OnesComplementByte : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((Byte)(~(Byte)obj));
                }
                return +1;
            }
        }

        internal sealed class OnesComplementSByte : OnesComplementInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((SByte)(~(SByte)obj));
                }
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Byte: return s_byte ?? (s_byte = new OnesComplementByte());
                case TypeCode.SByte: return s_sbyte ?? (s_sbyte = new OnesComplementSByte());
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new OnesComplementInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new OnesComplementInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new OnesComplementInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new OnesComplementUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new OnesComplementUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new OnesComplementUInt64());

                default:
                    throw Error.ExpressionNotSupportedForType("OnesComplement", type);
            }
        }
    }
}