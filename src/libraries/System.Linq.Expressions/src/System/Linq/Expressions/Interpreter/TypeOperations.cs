// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter
{
    internal sealed class CreateDelegateInstruction : Instruction
    {
        private readonly LightDelegateCreator _creator;

        internal CreateDelegateInstruction(LightDelegateCreator delegateCreator)
        {
            _creator = delegateCreator;
        }

        public override int ConsumedStack { get { return _creator.Interpreter.ClosureSize; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "CreateDelegate"; }
        }
        public override int Run(InterpretedFrame frame)
        {
            IStrongBox[] closure;
            if (ConsumedStack > 0)
            {
                closure = new IStrongBox[ConsumedStack];
                for (int i = closure.Length - 1; i >= 0; i--)
                {
                    closure[i] = (IStrongBox)frame.Pop();
                }
            }
            else
            {
                closure = null;
            }

            Delegate d = _creator.CreateDelegate(closure);

            frame.Push(d);
            return +1;
        }
    }

    internal sealed class NewInstruction : Instruction
    {
        private readonly ConstructorInfo _constructor;
        private readonly int _argCount;

        public NewInstruction(ConstructorInfo constructor)
        {
            _constructor = constructor;
            _argCount = constructor.GetParameters().Length;
        }
        public override int ConsumedStack { get { return _argCount; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "New"; }
        }
        public override int Run(InterpretedFrame frame)
        {
            object[] args = new object[_argCount];
            for (int i = _argCount - 1; i >= 0; i--)
            {
                args[i] = frame.Pop();
            }

            object ret;
            try
            {
                ret = _constructor.Invoke(args);
            }
            catch (TargetInvocationException e)
            {
                ExceptionHelpers.UpdateForRethrow(e.InnerException);
                throw e.InnerException;
            }
            frame.Push(ret);
            return +1;
        }

        public override string ToString()
        {
            return "New " + _constructor.DeclaringType.Name + "(" + _constructor + ")";
        }
    }

    internal partial class ByRefNewInstruction : Instruction
    {
        private readonly ByRefUpdater[] _byrefArgs;
        private readonly ConstructorInfo _constructor;
        private readonly int _argCount;

        internal ByRefNewInstruction(ConstructorInfo target, ByRefUpdater[] byrefArgs)
        {
            _constructor = target;
            _argCount = target.GetParameters().Length;
            _byrefArgs = byrefArgs;
        }

        public override int ConsumedStack { get { return _argCount; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "ByRefNew"; }
        }
        public sealed override int Run(InterpretedFrame frame)
        {
            object[] args = new object[_argCount];
            for (int i = _argCount - 1; i >= 0; i--)
            {
                args[i] = frame.Pop();
            }

            try
            {
                object ret;
                try
                {
                    ret = _constructor.Invoke(args);
                }
                catch (TargetInvocationException e)
                {
                    throw ExceptionHelpers.UpdateForRethrow(e.InnerException);
                }

                frame.Push(ret);
            }
            finally
            {
                if (args != null)
                {
                    foreach (var arg in _byrefArgs)
                    {
                        arg.Update(frame, args[arg.ArgumentIndex]);
                    }
                }
            }

            return 1;
        }
    }

    internal sealed class DefaultValueInstruction : Instruction
    {
        private readonly Type _type;

        internal DefaultValueInstruction(Type type)
        {
            _type = type;
        }

        public override int ConsumedStack { get { return 0; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "DefaultValue"; }
        }
        public override int Run(InterpretedFrame frame)
        {
            object value = _type.GetTypeInfo().IsValueType ? Activator.CreateInstance(_type) : null;
            frame.Push(value);
            return +1;
        }

        public override string ToString()
        {
            return "New " + _type;
        }
    }

    internal sealed class TypeIsInstruction : Instruction
    {
        private readonly Type _type;

        internal TypeIsInstruction(Type type)
        {
            _type = type;
        }

        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "TypeIs"; }
        }
        public override int Run(InterpretedFrame frame)
        {
            frame.Push(ScriptingRuntimeHelpers.BooleanToObject(_type.IsInstanceOfType(frame.Pop())));
            return +1;
        }

        public override string ToString()
        {
            return "TypeIs " + _type.ToString();
        }
    }

    internal sealed class TypeAsInstruction : Instruction
    {
        private readonly Type _type;

        internal TypeAsInstruction(Type type)
        {
            _type = type;
        }

        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "TypeAs"; }
        }
        public override int Run(InterpretedFrame frame)
        {
            object value = frame.Pop();
            if (_type.IsInstanceOfType(value))
            {
                frame.Push(value);
            }
            else
            {
                frame.Push(null);
            }
            return +1;
        }

        public override string ToString()
        {
            return "TypeAs " + _type.ToString();
        }
    }

    internal sealed class TypeEqualsInstruction : Instruction
    {
        public static readonly TypeEqualsInstruction Instance = new TypeEqualsInstruction();

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "TypeEquals"; }
        }
        private TypeEqualsInstruction()
        {
        }

        public override int Run(InterpretedFrame frame)
        {
            object type = frame.Pop();
            object obj = frame.Pop();
            frame.Push(ScriptingRuntimeHelpers.BooleanToObject(obj != null && (object)obj.GetType() == type));
            return +1;
        }
    }

    internal sealed class NullableTypeEqualsInstruction : Instruction
    {
        public static readonly NullableTypeEqualsInstruction Instance = new NullableTypeEqualsInstruction();

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "NullableTypeEquals"; }
        }
        private NullableTypeEqualsInstruction()
        {
        }

        public override int Run(InterpretedFrame frame)
        {
            object type = frame.Pop();
            object obj = frame.Pop();
            frame.Push(ScriptingRuntimeHelpers.BooleanToObject(obj != null && (object)obj.GetType() == type));
            return +1;
        }
    }

    internal sealed class ArrayLengthInstruction : Instruction
    {
        public static readonly ArrayLengthInstruction Instance = new ArrayLengthInstruction();

        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "ArrayLength"; }
        }
        private ArrayLengthInstruction()
        {
        }

        public override int Run(InterpretedFrame frame)
        {
            object obj = frame.Pop();
            frame.Push(((Array)obj).Length);
            return +1;
        }
    }

    internal abstract class NegateInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_single, s_double;

        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "Negate"; }
        }
        private NegateInstruction()
        {
        }

        internal sealed class NegateInt32 : NegateInstruction
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
                    frame.Push(ScriptingRuntimeHelpers.Int32ToObject(unchecked(-(Int32)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateInt16 : NegateInstruction
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
                    frame.Push(unchecked((Int16)(-(Int16)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateInt64 : NegateInstruction
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
                    frame.Push(unchecked((Int64)(-(Int64)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateSingle : NegateInstruction
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
                    frame.Push(unchecked((Single)(-(Single)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateDouble : NegateInstruction
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
                    frame.Push(unchecked((Double)(-(Double)obj)));
                }
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new NegateInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new NegateInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new NegateInt64());
                case TypeCode.Single: return s_single ?? (s_single = new NegateSingle());
                case TypeCode.Double: return s_double ?? (s_double = new NegateDouble());

                default:
                    throw Error.ExpressionNotSupportedForType("Negate", type);
            }
        }

        public override string ToString()
        {
            return "Negate()";
        }
    }

    internal abstract class NegateCheckedInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_single, s_double;

        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "NegateChecked"; }
        }
        private NegateCheckedInstruction()
        {
        }

        internal sealed class NegateCheckedInt32 : NegateCheckedInstruction
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
                    frame.Push(ScriptingRuntimeHelpers.Int32ToObject(checked(-(Int32)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateCheckedInt16 : NegateCheckedInstruction
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
                    frame.Push(checked((Int16)(-(Int16)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateCheckedInt64 : NegateCheckedInstruction
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
                    frame.Push(checked((Int64)(-(Int64)obj)));
                }
                return +1;
            }
        }
        internal sealed class NegateCheckedSingle : NegateCheckedInstruction
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
                    frame.Push(checked((Single)(-(Single)obj)));
                }
                return +1;
            }
        }

        internal sealed class NegateCheckedDouble : NegateCheckedInstruction
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
                    frame.Push(checked((Double)(-(Double)obj)));
                }
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new NegateCheckedInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new NegateCheckedInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new NegateCheckedInt64());
                case TypeCode.Single: return s_single ?? (s_single = new NegateCheckedSingle());
                case TypeCode.Double: return s_double ?? (s_double = new NegateCheckedDouble());
                default:
                    throw Error.ExpressionNotSupportedForType("NegateChecked", type);
            }
        }

        public override string ToString()
        {
            return "NegateChecked()";
        }
    }

    internal abstract class OnesComplementInstruction : Instruction
    {
        private static Instruction s_byte, s_sbyte, s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "OnesComplement"; }
        }
        private OnesComplementInstruction()
        {
        }

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

        public override string ToString()
        {
            return "OnesComplement()";
        }
    }

    internal abstract class IncrementInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "Increment"; }
        }
        private IncrementInstruction()
        {
        }

        internal sealed class IncrementInt32 : IncrementInstruction
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
                    frame.Push(ScriptingRuntimeHelpers.Int32ToObject(unchecked(1 + (Int32)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementInt16 : IncrementInstruction
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
                    frame.Push(unchecked((Int16)(1 + (Int16)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementInt64 : IncrementInstruction
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
                    frame.Push(unchecked((Int64)(1 + (Int64)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementUInt16 : IncrementInstruction
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
                    frame.Push(unchecked((UInt16)(1 + (UInt16)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementUInt32 : IncrementInstruction
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
                    frame.Push(unchecked((UInt32)(1 + (UInt32)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementUInt64 : IncrementInstruction
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
                    frame.Push(unchecked((UInt64)(1 + (UInt64)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementSingle : IncrementInstruction
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
                    frame.Push(unchecked((Single)(1 + (Single)obj)));
                }
                return +1;
            }
        }

        internal sealed class IncrementDouble : IncrementInstruction
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
                    frame.Push(unchecked((Double)(1 + (Double)obj)));
                }
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new IncrementInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new IncrementInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new IncrementInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new IncrementUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new IncrementUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new IncrementUInt64());
                case TypeCode.Single: return s_single ?? (s_single = new IncrementSingle());
                case TypeCode.Double: return s_double ?? (s_double = new IncrementDouble());

                default:
                    throw Error.ExpressionNotSupportedForType("Increment", type);
            }
        }

        public override string ToString()
        {
            return "Increment()";
        }
    }

    internal abstract class DecrementInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "Decrement"; }
        }
        private DecrementInstruction()
        {
        }

        internal sealed class DecrementInt32 : DecrementInstruction
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
                    frame.Push(ScriptingRuntimeHelpers.Int32ToObject(unchecked((Int32)obj - 1)));
                }
                return +1;
            }
        }

        internal sealed class DecrementInt16 : DecrementInstruction
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
                    frame.Push(unchecked((Int16)((Int16)obj - 1)));
                }
                return +1;
            }
        }

        internal sealed class DecrementInt64 : DecrementInstruction
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
                    frame.Push(unchecked((Int64)((Int64)obj - 1)));
                }
                return +1;
            }
        }

        internal sealed class DecrementUInt16 : DecrementInstruction
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
                    frame.Push(unchecked((UInt16)((UInt16)obj - 1)));
                }
                return +1;
            }
        }

        internal sealed class DecrementUInt32 : DecrementInstruction
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
                    frame.Push(unchecked((UInt32)((UInt32)obj - 1)));
                }
                return +1;
            }
        }

        internal sealed class DecrementUInt64 : DecrementInstruction
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
                    frame.Push(unchecked((UInt64)((UInt64)obj - 1)));
                }
                return +1;
            }
        }

        internal sealed class DecrementSingle : DecrementInstruction
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
                    frame.Push(unchecked((Single)((Single)obj - 1)));
                }
                return +1;
            }
        }

        internal sealed class DecrementDouble : DecrementInstruction
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
                    frame.Push(unchecked((Double)((Double)obj - 1)));
                }
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new DecrementInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new DecrementInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new DecrementInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new DecrementUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new DecrementUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new DecrementUInt64());
                case TypeCode.Single: return s_single ?? (s_single = new DecrementSingle());
                case TypeCode.Double: return s_double ?? (s_double = new DecrementDouble());

                default:
                    throw Error.ExpressionNotSupportedForType("Decrement", type);
            }
        }

        public override string ToString()
        {
            return "Decrement()";
        }
    }


    internal abstract class LeftShiftInstruction : Instruction
    {
        private static Instruction s_SByte, s_int16, s_int32, s_int64, s_byte, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "LeftShift"; }
        }
        private LeftShiftInstruction()
        {
        }

        internal sealed class LeftShiftSByte : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((SByte)(((SByte)value) << ((int)shift)));
                }
                return +1;
            }
        }

        internal sealed class LeftShiftInt16 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((Int16)(((Int16)value) << ((int)shift)));
                }
                return +1;
            }
        }

        internal sealed class LeftShiftInt32 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(((Int32)value) << ((int)shift));
                }
                return +1;
            }
        }

        internal sealed class LeftShiftInt64 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(((Int64)value) << ((int)shift));
                }
                return +1;
            }
        }

        internal sealed class LeftShiftByte : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((Byte)(((Byte)value) << ((int)shift)));
                }
                return +1;
            }
        }

        internal sealed class LeftShiftUInt16 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((UInt16)(((UInt16)value) << ((int)shift)));
                }
                return +1;
            }
        }

        internal sealed class LeftShiftUInt32 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(((UInt32)value) << ((int)shift));
                }
                return +1;
            }
        }

        internal sealed class LeftShiftUInt64 : LeftShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(((UInt64)value) << ((int)shift));
                }
                return +1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type)
        {
            // Boxed enums can be unboxed as their underlying types:
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(type.GetTypeInfo().IsEnum ? Enum.GetUnderlyingType(type) : TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new LeftShiftSByte());
                case TypeCode.Byte: return s_byte ?? (s_byte = new LeftShiftByte());
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new LeftShiftInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new LeftShiftInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new LeftShiftInt64());

                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new LeftShiftUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new LeftShiftUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new LeftShiftUInt64());

                default:
                    throw Error.ExpressionNotSupportedForType("LeftShift", type);
            }
        }

        public override string ToString()
        {
            return "LeftShift()";
        }
    }

    internal abstract class RightShiftInstruction : Instruction
    {
        private static Instruction s_SByte, s_int16, s_int32, s_int64, s_byte, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "RightShift"; }
        }
        private RightShiftInstruction()
        {
        }

        internal sealed class RightShiftSByte : RightShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((SByte)(((SByte)value) >> ((int)shift)));
                }
                return +1;
            }
        }

        internal sealed class RightShiftInt16 : RightShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((Int16)(((Int16)value) >> ((int)shift)));
                }
                return +1;
            }
        }

        internal sealed class RightShiftInt32 : RightShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(((Int32)value) >> ((int)shift));
                }
                return +1;
            }
        }

        internal sealed class RightShiftInt64 : RightShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(((Int64)value) >> ((int)shift));
                }
                return +1;
            }
        }

        internal sealed class RightShiftByte : RightShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((Byte)(((Byte)value) >> ((int)shift)));
                }
                return +1;
            }
        }

        internal sealed class RightShiftUInt16 : RightShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push((UInt16)(((UInt16)value) >> ((int)shift)));
                }
                return +1;
            }
        }

        internal sealed class RightShiftUInt32 : RightShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(((UInt32)value) >> ((int)shift));
                }
                return +1;
            }
        }

        internal sealed class RightShiftUInt64 : RightShiftInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var shift = frame.Pop();
                var value = frame.Pop();
                if (value == null || shift == null)
                {
                    frame.Push(null);
                }
                else
                {
                    frame.Push(((UInt64)value) >> ((int)shift));
                }
                return +1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type)
        {
            // Boxed enums can be unboxed as their underlying types:
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(type.GetTypeInfo().IsEnum ? Enum.GetUnderlyingType(type) : TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new RightShiftSByte());
                case TypeCode.Byte: return s_byte ?? (s_byte = new RightShiftByte());
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new RightShiftInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new RightShiftInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new RightShiftInt64());

                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new RightShiftUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new RightShiftUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new RightShiftUInt64());

                default:
                    throw Error.ExpressionNotSupportedForType("RightShift", type);
            }
        }

        public override string ToString()
        {
            return "RightShift()";
        }
    }

    internal abstract class ExclusiveOrInstruction : Instruction
    {
        private static Instruction s_SByte, s_int16, s_int32, s_int64, s_byte, s_UInt16, s_UInt32, s_UInt64, s_bool;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "ExclusiveOr"; }
        }
        private ExclusiveOrInstruction()
        {
        }

        internal sealed class ExclusiveOrSByte : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((SByte)(((SByte)left) ^ ((SByte)right)));
                return +1;
            }
        }

        internal sealed class ExclusiveOrInt16 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((Int16)(((Int16)left) ^ ((Int16)right)));
                return +1;
            }
        }

        internal sealed class ExclusiveOrInt32 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((Int32)left) ^ ((Int32)right));
                return +1;
            }
        }

        internal sealed class ExclusiveOrInt64 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((Int64)left) ^ ((Int64)right));
                return +1;
            }
        }

        internal sealed class ExclusiveOrByte : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((Byte)(((Byte)left) ^ ((Byte)right)));
                return +1;
            }
        }

        internal sealed class ExclusiveOrUInt16 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((UInt16)(((UInt16)left) ^ ((UInt16)right)));
                return +1;
            }
        }

        internal sealed class ExclusiveOrUInt32 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((UInt32)left) ^ ((UInt32)right));
                return +1;
            }
        }

        internal sealed class ExclusiveOrUInt64 : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((UInt64)left) ^ ((UInt64)right));
                return +1;
            }
        }

        internal sealed class ExclusiveOrBool : ExclusiveOrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((Boolean)left) ^ ((Boolean)right));
                return +1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type)
        {
            // Boxed enums can be unboxed as their underlying types:
            switch (GetTypeCode(type))
            {
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new ExclusiveOrSByte());
                case TypeCode.Byte: return s_byte ?? (s_byte = new ExclusiveOrByte());
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new ExclusiveOrInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new ExclusiveOrInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new ExclusiveOrInt64());

                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new ExclusiveOrUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new ExclusiveOrUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new ExclusiveOrUInt64());
                case TypeCode.Boolean: return s_bool ?? (s_bool = new ExclusiveOrBool());

                default:
                    throw Error.ExpressionNotSupportedForType("ExclusiveOr", type);
            }
        }

        private static TypeCode GetTypeCode(Type type)
        {
            return System.Dynamic.Utils.TypeExtensions.GetTypeCode(type.GetTypeInfo().IsEnum ? Enum.GetUnderlyingType(type) : TypeUtils.GetNonNullableType(type));
        }

        public override string ToString()
        {
            return "ExclusiveOr()";
        }
    }

    internal abstract class OrInstruction : Instruction
    {
        private static Instruction s_SByte, s_int16, s_int32, s_int64, s_byte, s_UInt16, s_UInt32, s_UInt64, s_bool;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "Or"; }
        }
        private OrInstruction()
        {
        }

        internal sealed class OrSByte : OrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((SByte)(((SByte)left) | ((SByte)right)));
                return +1;
            }
        }

        internal sealed class OrInt16 : OrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((Int16)(((Int16)left) | ((Int16)right)));
                return +1;
            }
        }

        internal sealed class OrInt32 : OrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((Int32)left) | ((Int32)right));
                return +1;
            }
        }

        internal sealed class OrInt64 : OrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((Int64)left) | ((Int64)right));
                return +1;
            }
        }

        internal sealed class OrByte : OrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((Byte)(((Byte)left) | ((Byte)right)));
                return +1;
            }
        }

        internal sealed class OrUInt16 : OrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((UInt16)(((UInt16)left) | ((UInt16)right)));
                return +1;
            }
        }

        internal sealed class OrUInt32 : OrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((UInt32)left) | ((UInt32)right));
                return +1;
            }
        }

        internal sealed class OrUInt64 : OrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((UInt64)left) | ((UInt64)right));
                return +1;
            }
        }

        internal sealed class OrBool : OrInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    if (right == null)
                    {
                        frame.Push(null);
                    }
                    else
                    {
                        frame.Push((Boolean)right ? ScriptingRuntimeHelpers.True : null);
                    }
                    return +1;
                }
                else if (right == null)
                {
                    frame.Push((Boolean)left ? ScriptingRuntimeHelpers.True : null);
                    return +1;
                }
                frame.Push(((Boolean)left) | ((Boolean)right));
                return +1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type)
        {
            // Boxed enums can be unboxed as their underlying types:
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(type.GetTypeInfo().IsEnum ? Enum.GetUnderlyingType(type) : TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new OrSByte());
                case TypeCode.Byte: return s_byte ?? (s_byte = new OrByte());
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new OrInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new OrInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new OrInt64());

                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new OrUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new OrUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new OrUInt64());
                case TypeCode.Boolean: return s_bool ?? (s_bool = new OrBool());

                default:
                    throw Error.ExpressionNotSupportedForType("Or", type);
            }
        }

        public override string ToString()
        {
            return "Or()";
        }
    }

    internal abstract class AndInstruction : Instruction
    {
        private static Instruction s_SByte, s_int16, s_int32, s_int64, s_byte, s_UInt16, s_UInt32, s_UInt64, s_bool;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "And"; }
        }
        private AndInstruction()
        {
        }

        internal sealed class AndSByte : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((SByte)(((SByte)left) & ((SByte)right)));
                return +1;
            }
        }

        internal sealed class AndInt16 : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((Int16)(((Int16)left) & ((Int16)right)));
                return +1;
            }
        }

        internal sealed class AndInt32 : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((Int32)left) & ((Int32)right));
                return +1;
            }
        }

        internal sealed class AndInt64 : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((Int64)left) & ((Int64)right));
                return +1;
            }
        }

        internal sealed class AndByte : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((Byte)(((Byte)left) & ((Byte)right)));
                return +1;
            }
        }

        internal sealed class AndUInt16 : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push((UInt16)(((UInt16)left) & ((UInt16)right)));
                return +1;
            }
        }

        internal sealed class AndUInt32 : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((UInt32)left) & ((UInt32)right));
                return +1;
            }
        }

        internal sealed class AndUInt64 : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var left = frame.Pop();
                var right = frame.Pop();
                if (left == null || right == null)
                {
                    frame.Push(null);
                    return +1;
                }
                frame.Push(((UInt64)left) & ((UInt64)right));
                return +1;
            }
        }

        internal sealed class AndBool : AndInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var right = frame.Pop();
                var left = frame.Pop();
                if (left == null)
                {
                    if (right == null)
                    {
                        frame.Push(null);
                    }
                    else
                    {
                        frame.Push((Boolean)right ? null : ScriptingRuntimeHelpers.False);
                    }
                    return +1;
                }
                else if (right == null)
                {
                    frame.Push((Boolean)left ? null : ScriptingRuntimeHelpers.False);
                    return +1;
                }
                frame.Push(((Boolean)left) & ((Boolean)right));
                return +1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type)
        {
            // Boxed enums can be unboxed as their underlying types:
            switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(type.GetTypeInfo().IsEnum ? Enum.GetUnderlyingType(type) : TypeUtils.GetNonNullableType(type)))
            {
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new AndSByte());
                case TypeCode.Byte: return s_byte ?? (s_byte = new AndByte());
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new AndInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new AndInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new AndInt64());

                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new AndUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new AndUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new AndUInt64());
                case TypeCode.Boolean: return s_bool ?? (s_bool = new AndBool());

                default:
                    throw Error.ExpressionNotSupportedForType("And", type);
            }
        }

        public override string ToString()
        {
            return "And()";
        }
    }

    internal abstract class NullableMethodCallInstruction : Instruction
    {
        private static NullableMethodCallInstruction s_hasValue, s_value, s_equals, s_getHashCode, s_getValueOrDefault1, s_toString;

        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "NullableMethod"; }
        }
        private NullableMethodCallInstruction()
        {
        }

        private class HasValue : NullableMethodCallInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var obj = frame.Pop();
                frame.Push(ScriptingRuntimeHelpers.BooleanToObject(obj != null));
                return +1;
            }
        }

        private class GetValue : NullableMethodCallInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                if (frame.Peek() == null)
                {
                    frame.Pop();
                    throw new InvalidOperationException();
                }
                return +1;
            }
        }

        private class GetValueOrDefault : NullableMethodCallInstruction
        {
            private readonly Type defaultValueType;

            public GetValueOrDefault(MethodInfo mi)
            {
                defaultValueType = mi.ReturnType;
            }

            public override int Run(InterpretedFrame frame)
            {
                if (frame.Peek() == null)
                {
                    frame.Pop();
                    frame.Push(Activator.CreateInstance(defaultValueType));
                }
                return +1;
            }
        }

        private class GetValueOrDefault1 : NullableMethodCallInstruction
        {
            public override int ConsumedStack { get { return 2; } }

            public override int Run(InterpretedFrame frame)
            {
                var dflt = frame.Pop();
                var obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(dflt);
                }
                else
                {
                    frame.Push(obj);
                }
                return +1;
            }
        }

        private class EqualsClass : NullableMethodCallInstruction
        {
            public override int ConsumedStack { get { return 2; } }

            public override int Run(InterpretedFrame frame)
            {
                var other = frame.Pop();
                var obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(other == null));
                }
                else if (other == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.False);
                }
                else
                {
                    frame.Push(ScriptingRuntimeHelpers.BooleanToObject(obj.Equals(other)));
                }
                return +1;
            }
        }

        private class ToStringClass : NullableMethodCallInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push("");
                }
                else
                {
                    frame.Push(obj.ToString());
                }
                return +1;
            }
        }

        private class GetHashCodeClass : NullableMethodCallInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(ScriptingRuntimeHelpers.Int32ToObject(0));
                }
                else
                {
                    frame.Push(obj.GetHashCode());
                }
                return +1;
            }
        }

        public static Instruction Create(string method, int argCount, MethodInfo mi)
        {
            switch (method)
            {
                case "get_HasValue": return s_hasValue ?? (s_hasValue = new HasValue());
                case "get_Value": return s_value ?? (s_value = new GetValue());
                case "Equals": return s_equals ?? (s_equals = new EqualsClass());
                case "GetHashCode": return s_getHashCode ?? (s_getHashCode = new GetHashCodeClass());
                case "GetValueOrDefault":
                    if (argCount == 0)
                    {
                        return new GetValueOrDefault(mi);
                    }
                    else
                    {
                        return s_getValueOrDefault1 ?? (s_getValueOrDefault1 = new GetValueOrDefault1());
                    }
                case "ToString": return s_toString ?? (s_toString = new ToStringClass());
                default:
                    // System.Nullable doesn't have other instance methods 
                    throw Assert.Unreachable;
            }
        }

        public static Instruction CreateGetValue()
        {
            return s_value ?? (s_value = new GetValue());
        }
    }

    internal abstract class CastInstruction : Instruction
    {
        private static CastInstruction s_boolean, s_byte, s_char, s_dateTime, s_decimal, s_double, s_int16, s_int32, s_int64, s_SByte, s_single, s_string, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "Cast"; }
        }

        private class CastInstructionT<T> : CastInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                var value = frame.Pop();
                frame.Push((T)value);
                return +1;
            }
        }

        private abstract class CastInstructionNoT : CastInstruction
        {
            private readonly Type _t;
            protected CastInstructionNoT(Type t)
            {
                _t = t;
            }

            public new static CastInstruction Create(Type t)
            {
                if (t.GetTypeInfo().IsValueType && !TypeUtils.IsNullableType(t))
                {
                    return new Value(t);
                }
                else
                {
                    return new Ref(t);
                }
            }

            public override int Run(InterpretedFrame frame)
            {
                var value = frame.Pop();
                if (value != null)
                {
                    var valueType = value.GetType();

                    if (!TypeUtils.HasReferenceConversion(valueType, _t) &&
                        !TypeUtils.HasIdentityPrimitiveOrNullableConversion(valueType, _t))
                    {
                        throw new InvalidCastException();
                    }

                    if (!_t.IsAssignableFrom(valueType))
                    {
                        throw new InvalidCastException();
                    }

                    frame.Push(value);
                }
                else
                {
                    ConvertNull(frame);
                }
                return +1;
            }

            protected abstract void ConvertNull(InterpretedFrame frame);

            class Ref : CastInstructionNoT
            {
                public Ref(Type t)
                    : base(t)
                {
                }

                protected override void ConvertNull(InterpretedFrame frame)
                {
                    frame.Push(null);
                }
            }

            class Value : CastInstructionNoT
            {
                public Value(Type t)
                    : base(t)
                {
                }

                protected override void ConvertNull(InterpretedFrame frame)
                {
                    throw new NullReferenceException();
                }
            }
        }

        public static Instruction Create(Type t)
        {
            if (!t.GetTypeInfo().IsEnum)
            {
                switch (System.Dynamic.Utils.TypeExtensions.GetTypeCode(t))
                {
                    case TypeCode.Boolean: return s_boolean ?? (s_boolean = new CastInstructionT<Boolean>());
                    case TypeCode.Byte: return s_byte ?? (s_byte = new CastInstructionT<Byte>());
                    case TypeCode.Char: return s_char ?? (s_char = new CastInstructionT<Char>());
                    case TypeCode.DateTime: return s_dateTime ?? (s_dateTime = new CastInstructionT<DateTime>());
                    case TypeCode.Decimal: return s_decimal ?? (s_decimal = new CastInstructionT<Decimal>());
                    case TypeCode.Double: return s_double ?? (s_double = new CastInstructionT<Double>());
                    case TypeCode.Int16: return s_int16 ?? (s_int16 = new CastInstructionT<Int16>());
                    case TypeCode.Int32: return s_int32 ?? (s_int32 = new CastInstructionT<Int32>());
                    case TypeCode.Int64: return s_int64 ?? (s_int64 = new CastInstructionT<Int64>());
                    case TypeCode.SByte: return s_SByte ?? (s_SByte = new CastInstructionT<SByte>());
                    case TypeCode.Single: return s_single ?? (s_single = new CastInstructionT<Single>());
                    case TypeCode.String: return s_string ?? (s_string = new CastInstructionT<String>());
                    case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new CastInstructionT<UInt16>());
                    case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new CastInstructionT<UInt32>());
                    case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new CastInstructionT<UInt64>());
                }
            }

            return CastInstructionNoT.Create(t);
        }
    }

    internal class CastToEnumInstruction : CastInstruction
    {
        private readonly Type _t;
        public CastToEnumInstruction(Type t)
        {
            Debug.Assert(t.GetTypeInfo().IsEnum);
            _t = t;
        }

        public override int Run(InterpretedFrame frame)
        {
            var from = frame.Pop();
            var to = from != null ? Enum.ToObject(_t, from) : from;
            frame.Push(to);

            return +1;
        }
    }

    internal class QuoteInstruction : Instruction
    {
        private readonly Expression _operand;
        private readonly Dictionary<ParameterExpression, LocalVariable> _hoistedVariables;

        public QuoteInstruction(Expression operand, Dictionary<ParameterExpression, LocalVariable> hoistedVariables)
        {
            _operand = operand;
            _hoistedVariables = hoistedVariables;
        }

        public override int ConsumedStack { get { return 0; } }
        public override int ProducedStack { get { return 1; } }
        public override string InstructionName
        {
            get { return "Quote"; }
        }

        public override int Run(InterpretedFrame frame)
        {
            Expression operand = _operand;
            if (_hoistedVariables != null)
            {
                operand = new ExpressionQuoter(_hoistedVariables, frame).Visit(operand);
            }
            frame.Push(operand);
            return +1;
        }

        // Modifies a quoted Expression instance by changing hoisted variables and
        // parameters into hoisted local references. The variable's StrongBox is
        // burned as a constant, and all hoisted variables/parameters are rewritten
        // as indexing expressions.
        //
        // The behavior of Quote is indended to be like C# and VB expression quoting
        private sealed class ExpressionQuoter : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, LocalVariable> _variables;
            private readonly InterpretedFrame _frame;

            // A stack of variables that are defined in nested scopes. We search
            // this first when resolving a variable in case a nested scope shadows
            // one of our variable instances.
            private readonly Stack<Set<ParameterExpression>> _shadowedVars = new Stack<Set<ParameterExpression>>();

            internal ExpressionQuoter(Dictionary<ParameterExpression, LocalVariable> hoistedVariables, InterpretedFrame frame)
            {
                _variables = hoistedVariables;
                _frame = frame;
            }

            protected internal override Expression VisitLambda<T>(Expression<T> node)
            {
                _shadowedVars.Push(new Set<ParameterExpression>(node.Parameters));
                Expression b = Visit(node.Body);
                _shadowedVars.Pop();
                if (b == node.Body)
                {
                    return node;
                }
                return node.Update(b, node.Parameters);
            }

            protected internal override Expression VisitBlock(BlockExpression node)
            {
                if (node.Variables.Count > 0)
                {
                    _shadowedVars.Push(new Set<ParameterExpression>(node.Variables));
                }
                var b = Visit(node.Expressions);
                if (node.Variables.Count > 0)
                {
                    _shadowedVars.Pop();
                }
                if (b == node.Expressions)
                {
                    return node;
                }
                return Expression.Block(node.Variables, b);
            }

            protected override CatchBlock VisitCatchBlock(CatchBlock node)
            {
                if (node.Variable != null)
                {
                    _shadowedVars.Push(new Set<ParameterExpression>(new[] { node.Variable }));
                }
                Expression b = Visit(node.Body);
                Expression f = Visit(node.Filter);
                if (node.Variable != null)
                {
                    _shadowedVars.Pop();
                }
                if (b == node.Body && f == node.Filter)
                {
                    return node;
                }
                return Expression.MakeCatchBlock(node.Test, node.Variable, b, f);
            }

            protected internal override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
            {
                int count = node.Variables.Count;
                var boxes = new List<IStrongBox>();
                var vars = new List<ParameterExpression>();
                var indexes = new int[count];
                for (int i = 0; i < count; i++)
                {
                    LocalVariable var;
                    if (_variables.TryGetValue(node.Variables[i], out var))
                    {
                        indexes[i] = -1 - boxes.Count;
                        if (var.InClosure)
                        {
                            boxes.Add(_frame.Closure[var.Index]);
                        }
                        else
                        {
                            boxes.Add((IStrongBox)_frame.Data[var.Index]);
                        }
                    }
                    else
                    {
                        indexes[i] = vars.Count;
                        vars.Add(node.Variables[i]);
                    }
                }

                // No variables were rewritten. Just return the original node
                if (boxes.Count == 0)
                {
                    return node;
                }

                var boxesConst = Expression.Constant(new RuntimeVariables(boxes.ToArray()), typeof(IRuntimeVariables));
                // All of them were rewritten. Just return the array as a constant
                if (vars.Count == 0)
                {
                    return boxesConst;
                }

                // Otherwise, we need to return an object that merges them
                return Expression.Invoke(
                    Expression.Constant(new Func<IRuntimeVariables, IRuntimeVariables, int[], IRuntimeVariables>(MergeRuntimeVariables)),
                    Expression.RuntimeVariables(new TrueReadOnlyCollection<ParameterExpression>(vars.ToArray())),
                    boxesConst,
                    Expression.Constant(indexes)
                );
            }

            private static IRuntimeVariables MergeRuntimeVariables(IRuntimeVariables first, IRuntimeVariables second, int[] indexes)
            {
                return new MergedRuntimeVariables(first, second, indexes);
            }

            protected internal override Expression VisitParameter(ParameterExpression node)
            {
                LocalVariable var;
                if (_variables.TryGetValue(node, out var))
                {
                    return Expression.Convert(
                        Expression.Field(
                            var.LoadFromArray(
                                Expression.Constant(_frame.Data),
                                Expression.Constant(_frame.Closure),
                                node.Type
                            ),
                            "Value"
                        ),
                        node.Type
                    );
                }
                return node;
            }

            private sealed class RuntimeVariables : IRuntimeVariables
            {
                private readonly IStrongBox[] _boxes;

                internal RuntimeVariables(IStrongBox[] boxes)
                {
                    _boxes = boxes;
                }

                int IRuntimeVariables.Count
                {
                    get { return _boxes.Length; }
                }

                object IRuntimeVariables.this[int index]
                {
                    get
                    {
                        return _boxes[index].Value;
                    }
                    set
                    {
                        _boxes[index].Value = value;
                    }
                }
            }

            /// <summary>
            /// Provides a list of variables, supporing read/write of the values
            /// Exposed via RuntimeVariablesExpression
            /// </summary>
            private sealed class MergedRuntimeVariables : IRuntimeVariables
            {
                private readonly IRuntimeVariables _first;
                private readonly IRuntimeVariables _second;

                // For reach item, the index into the first or second list
                // Positive values mean the first array, negative means the second
                private readonly int[] _indexes;

                internal MergedRuntimeVariables(IRuntimeVariables first, IRuntimeVariables second, int[] indexes)
                {
                    _first = first;
                    _second = second;
                    _indexes = indexes;
                }

                public int Count
                {
                    get { return _indexes.Length; }
                }

                public object this[int index]
                {
                    get
                    {
                        index = _indexes[index];
                        return (index >= 0) ? _first[index] : _second[-1 - index];
                    }
                    set
                    {
                        index = _indexes[index];
                        if (index >= 0)
                        {
                            _first[index] = value;
                        }
                        else
                        {
                            _second[-1 - index] = value;
                        }
                    }
                }
            }
        }
    }
}
