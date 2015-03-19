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
    internal abstract class NotEqualInstruction : Instruction
    {
        // Perf: EqualityComparer<T> but is 3/2 to 2 times slower.
        private static Instruction s_reference,s_boolean,s_SByte,s_int16,s_char,s_int32,s_int64,s_byte,s_UInt16,s_UInt32,s_UInt64,s_single,s_double;
        private static Instruction s_referenceLiftedToNull,s_booleanLiftedToNull,s_SByteLiftedToNull,s_int16LiftedToNull,s_charLiftedToNull,s_int32LiftedToNull,s_int64LiftedToNull,s_byteLiftedToNull,s_UInt16LiftedToNull,s_UInt32LiftedToNull,s_UInt64LiftedToNull,s_singleLiftedToNull,s_doubleLiftedToNull;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "NotEqual"; }
        }
        private NotEqualInstruction()
        {
        }

        internal sealed class NotEqualBoolean : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(right != null));
                }
                else if (right == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.True);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Boolean)left) != ((Boolean)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualSByte : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(right != null));
                }
                else if (right == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.True);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((SByte)left) != ((SByte)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualInt16 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(right != null));
                }
                else if (right == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.True);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Int16)left) != ((Int16)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualChar : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(right != null));
                }
                else if (right == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.True);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Char)left) != ((Char)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualInt32 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(right != null));
                }
                else if (right == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.True);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Int32)left) != ((Int32)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualInt64 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(right != null));
                }
                else if (right == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.True);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Int64)left) != ((Int64)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualByte : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(right != null));
                }
                else if (right == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.True);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Byte)left) != ((Byte)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualUInt16 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(right != null));
                }
                else if (right == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.True);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((UInt16)left) != ((UInt16)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualUInt32 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(right != null));
                }
                else if (right == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.True);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((UInt32)left) != ((UInt32)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualUInt64 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(right != null));
                }
                else if (right == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.True);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((UInt64)left) != ((UInt64)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualSingle : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(right != null));
                }
                else if (right == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.True);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Single)left) != ((Single)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualDouble : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(right != null));
                }
                else if (right == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.True);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Double)left) != ((Double)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualReference : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(ScriptingRuntimeHelpers.BooleanToObject(frame.Pop() != frame.Pop()));
                return +1;
            }
        }


        internal sealed class NotEqualBooleanLiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Boolean)left) != ((Boolean)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualSByteLiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((SByte)left) != ((SByte)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualInt16LiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Int16)left) != ((Int16)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualCharLiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Char)left) != ((Char)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualInt32LiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Int32)left) != ((Int32)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualInt64LiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Int64)left) != ((Int64)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualByteLiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Byte)left) != ((Byte)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualUInt16LiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((UInt16)left) != ((UInt16)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualUInt32LiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((UInt32)left) != ((UInt32)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualUInt64LiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((UInt64)left) != ((UInt64)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualSingleLiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Single)left) != ((Single)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualDoubleLiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(((Double)left) != ((Double)right)));
                }
                return +1;
            }
        }

        internal sealed class NotEqualReferenceLiftedToNull : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(ScriptingRuntimeHelpers.BooleanToObject(frame.Pop() != frame.Pop()));
                return +1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type, bool liftedToNull)
        {
            if (liftedToNull)
            {
                // Boxed enums can be unboxed as their underlying types:
                switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(type.GetTypeInfo().IsEnum ? Enum.GetUnderlyingType(type) : TypeUtils.GetNonNullableType(type)))
                {
                    case TypeCode.Boolean: return s_booleanLiftedToNull ?? (s_booleanLiftedToNull = new NotEqualBooleanLiftedToNull());
                    case TypeCode.SByte: return s_SByteLiftedToNull ?? (s_SByteLiftedToNull = new NotEqualSByteLiftedToNull());
                    case TypeCode.Byte: return s_byteLiftedToNull ?? (s_byteLiftedToNull = new NotEqualByteLiftedToNull());
                    case TypeCode.Char: return s_charLiftedToNull ?? (s_charLiftedToNull = new NotEqualCharLiftedToNull());
                    case TypeCode.Int16: return s_int16LiftedToNull ?? (s_int16LiftedToNull = new NotEqualInt16LiftedToNull());
                    case TypeCode.Int32: return s_int32LiftedToNull ?? (s_int32LiftedToNull = new NotEqualInt32LiftedToNull());
                    case TypeCode.Int64: return s_int64LiftedToNull ?? (s_int64LiftedToNull = new NotEqualInt64LiftedToNull());

                    case TypeCode.UInt16: return s_UInt16LiftedToNull ?? (s_UInt16LiftedToNull = new NotEqualUInt16LiftedToNull());
                    case TypeCode.UInt32: return s_UInt32LiftedToNull ?? (s_UInt32LiftedToNull = new NotEqualUInt32LiftedToNull());
                    case TypeCode.UInt64: return s_UInt64LiftedToNull ?? (s_UInt64LiftedToNull = new NotEqualUInt64LiftedToNull());

                    case TypeCode.Single: return s_singleLiftedToNull ?? (s_singleLiftedToNull = new NotEqualSingleLiftedToNull());
                    case TypeCode.Double: return s_doubleLiftedToNull ?? (s_doubleLiftedToNull = new NotEqualDoubleLiftedToNull());

                    case TypeCode.String:
                    case TypeCode.Object:
                        if (!type.GetTypeInfo().IsValueType)
                        {
                            return s_referenceLiftedToNull ?? (s_referenceLiftedToNull = new NotEqualReferenceLiftedToNull());
                        }
                        // TODO: Nullable<T>
                        throw Error.ExpressionNotSupportedForNullableType("NotEqual", type);
                    default:
                        throw Error.ExpressionNotSupportedForType("NotEqual", type);
                }
            }
            else
            {
                // Boxed enums can be unboxed as their underlying types:
                switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(type.GetTypeInfo().IsEnum ? Enum.GetUnderlyingType(type) : TypeUtils.GetNonNullableType(type)))
                {
                    case TypeCode.Boolean: return s_boolean ?? (s_boolean = new NotEqualBoolean());
                    case TypeCode.SByte: return s_SByte ?? (s_SByte = new NotEqualSByte());
                    case TypeCode.Byte: return s_byte ?? (s_byte = new NotEqualByte());
                    case TypeCode.Char: return s_char ?? (s_char = new NotEqualChar());
                    case TypeCode.Int16: return s_int16 ?? (s_int16 = new NotEqualInt16());
                    case TypeCode.Int32: return s_int32 ?? (s_int32 = new NotEqualInt32());
                    case TypeCode.Int64: return s_int64 ?? (s_int64 = new NotEqualInt64());

                    case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new NotEqualUInt16());
                    case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new NotEqualUInt32());
                    case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new NotEqualUInt64());

                    case TypeCode.Single: return s_single ?? (s_single = new NotEqualSingle());
                    case TypeCode.Double: return s_double ?? (s_double = new NotEqualDouble());

                    case TypeCode.String:
                    case TypeCode.Object:
                        if (!type.GetTypeInfo().IsValueType)
                        {
                            return s_reference ?? (s_reference = new NotEqualReference());
                        }
                        // TODO: Nullable<T>
                        throw Error.ExpressionNotSupportedForNullableType("NotEqual", type);
                    default:
                        throw Error.ExpressionNotSupportedForType("NotEqual", type);
                }
            }
        }

        public override string ToString()
        {
            return "NotEqual()";
        }
    }
}

