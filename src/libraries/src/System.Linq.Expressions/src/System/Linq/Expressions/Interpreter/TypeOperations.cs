// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public override int ConsumedStack => _creator.Interpreter.ClosureSize;
        public override int ProducedStack => 1;
        public override string InstructionName => "CreateDelegate";

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

    internal sealed class TypeIsInstruction : Instruction
    {
        private readonly Type _type;

        internal TypeIsInstruction(Type type)
        {
            _type = type;
        }

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "TypeIs";
        
        public override int Run(InterpretedFrame frame)
        {
            frame.Push(ScriptingRuntimeHelpers.BooleanToObject(_type.IsInstanceOfType(frame.Pop())));
            return +1;
        }

        public override string ToString() => "TypeIs " + _type.ToString();
    }

    internal sealed class TypeAsInstruction : Instruction
    {
        private readonly Type _type;

        internal TypeAsInstruction(Type type)
        {
            _type = type;
        }

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "TypeAs";
        
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

        public override string ToString() => "TypeAs " + _type.ToString();
    }

    internal sealed class TypeEqualsInstruction : Instruction
    {
        public static readonly TypeEqualsInstruction Instance = new TypeEqualsInstruction();

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "TypeEquals";

        private TypeEqualsInstruction() { }

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

        public override int ConsumedStack => 2;
        public override int ProducedStack => 1;
        public override string InstructionName => "NullableTypeEquals";

        private NullableTypeEqualsInstruction() { }

        public override int Run(InterpretedFrame frame)
        {
            object type = frame.Pop();
            object obj = frame.Pop();
            frame.Push(ScriptingRuntimeHelpers.BooleanToObject(obj != null && (object)obj.GetType() == type));
            return +1;
        }
    }

    internal abstract class NullableMethodCallInstruction : Instruction
    {
        private static NullableMethodCallInstruction s_hasValue, s_value, s_equals, s_getHashCode, s_getValueOrDefault1, s_toString;

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "NullableMethod";

        private NullableMethodCallInstruction() { }

        private class HasValue : NullableMethodCallInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object obj = frame.Pop();
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
            public override int ConsumedStack => 2;

            public override int Run(InterpretedFrame frame)
            {
                object dflt = frame.Pop();
                object obj = frame.Pop();
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
            public override int ConsumedStack => 2;

            public override int Run(InterpretedFrame frame)
            {
                object other = frame.Pop();
                object obj = frame.Pop();
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
                object obj = frame.Pop();
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
                object obj = frame.Pop();
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
                    throw ContractUtils.Unreachable;
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

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "Cast";

        private class CastInstructionT<T> : CastInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object value = frame.Pop();
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
                object value = frame.Pop();
                if (value != null)
                {
                    Type valueType = value.GetType();

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
                switch (t.GetTypeCode())
                {
                    case TypeCode.Boolean: return s_boolean ?? (s_boolean = new CastInstructionT<bool>());
                    case TypeCode.Byte: return s_byte ?? (s_byte = new CastInstructionT<byte>());
                    case TypeCode.Char: return s_char ?? (s_char = new CastInstructionT<char>());
                    case TypeCode.DateTime: return s_dateTime ?? (s_dateTime = new CastInstructionT<DateTime>());
                    case TypeCode.Decimal: return s_decimal ?? (s_decimal = new CastInstructionT<decimal>());
                    case TypeCode.Double: return s_double ?? (s_double = new CastInstructionT<double>());
                    case TypeCode.Int16: return s_int16 ?? (s_int16 = new CastInstructionT<short>());
                    case TypeCode.Int32: return s_int32 ?? (s_int32 = new CastInstructionT<int>());
                    case TypeCode.Int64: return s_int64 ?? (s_int64 = new CastInstructionT<long>());
                    case TypeCode.SByte: return s_SByte ?? (s_SByte = new CastInstructionT<sbyte>());
                    case TypeCode.Single: return s_single ?? (s_single = new CastInstructionT<float>());
                    case TypeCode.String: return s_string ?? (s_string = new CastInstructionT<string>());
                    case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new CastInstructionT<ushort>());
                    case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new CastInstructionT<uint>());
                    case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new CastInstructionT<ulong>());
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
            object from = frame.Pop();
            switch (Convert.GetTypeCode(from))
            {
                case TypeCode.Empty:
                    frame.Push(null);
                    break;
                case TypeCode.Int32:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int64:
                case TypeCode.UInt32:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt64:
                case TypeCode.Char:
                case TypeCode.Boolean:
                    frame.Push(Enum.ToObject(_t, from));
                    break;
                default:
                    throw new InvalidCastException();
            }

            return +1;
        }
    }

    internal sealed class CastReferenceToEnumInstruction : CastInstruction
    {
        private readonly Type _t;

        public CastReferenceToEnumInstruction(Type t)
        {
            Debug.Assert(t.GetTypeInfo().IsEnum);
            _t = t;
        }

        public override int Run(InterpretedFrame frame)
        {
            object from = frame.Pop();
            if (from == null)
            {
                frame.Push(null);
            }
            else
            {
                Type underlying = _t.GetTypeInfo().IsEnum ? Enum.GetUnderlyingType(_t) : _t;
                // Order checks in order of likelihood. int first as the vast majority of enums
                // are int-based, then long as that is sometimes used when required for a large set of flags
                // and so-on.
                if (underlying == typeof(int))
                {
                    // If from is neither an int nor a type assignable to int (viz. an int-backed enum)
                    // this will cause an InvalidCastException, which is what this operation should
                    // throw in this case.
                    frame.Push(Enum.ToObject(_t, (int)from));
                }
                else if (underlying == typeof(long))
                {
                    frame.Push(Enum.ToObject(_t, (long)from));
                }
                else if (underlying == typeof(uint))
                {
                    frame.Push(Enum.ToObject(_t, (uint)from));
                }
                else if (underlying == typeof(ulong))
                {
                    frame.Push(Enum.ToObject(_t, (ulong)from));
                }
                else if (underlying == typeof(byte))
                {
                    frame.Push(Enum.ToObject(_t, (byte)from));
                }
                else if (underlying == typeof(sbyte))
                {
                    frame.Push(Enum.ToObject(_t, (sbyte)from));
                }
                else if (underlying == typeof(short))
                {
                    frame.Push(Enum.ToObject(_t, (short)from));
                }
                else if (underlying == typeof(ushort))
                {
                    frame.Push(Enum.ToObject(_t, (ushort)from));
                }
                else if (underlying == typeof(char))
                {
                    // Disallowed in C#, but allowed in CIL
                    frame.Push(Enum.ToObject(_t, (char)from));
                }
                else if (underlying == typeof(bool))
                {
                    // Disallowed in C#, but allowed in CIL
                    frame.Push(Enum.ToObject(_t, (bool)from));
                }
                else
                {
                    throw new InvalidCastException();
                }
            }

            return 1;
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

        public override int ConsumedStack => 0;
        public override int ProducedStack => 1;
        public override string InstructionName => "Quote";

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
        // The behavior of Quote is intended to be like C# and VB expression quoting
        private sealed class ExpressionQuoter : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, LocalVariable> _variables;
            private readonly InterpretedFrame _frame;

            // A stack of variables that are defined in nested scopes. We search
            // this first when resolving a variable in case a nested scope shadows
            // one of our variable instances.
            private readonly Stack<HashSet<ParameterExpression>> _shadowedVars = new Stack<HashSet<ParameterExpression>>();

            internal ExpressionQuoter(Dictionary<ParameterExpression, LocalVariable> hoistedVariables, InterpretedFrame frame)
            {
                _variables = hoistedVariables;
                _frame = frame;
            }

            protected internal override Expression VisitLambda<T>(Expression<T> node)
            {
                if (node.Parameters.Count > 0)
                {
                    _shadowedVars.Push(new HashSet<ParameterExpression>(node.Parameters));
                }
                Expression b = Visit(node.Body);
                if (node.Parameters.Count > 0)
                {
                    _shadowedVars.Pop();
                }
                if (b == node.Body)
                {
                    return node;
                }
                return Expression.Lambda<T>(b, node.Name, node.TailCall, node.Parameters);
            }

            protected internal override Expression VisitBlock(BlockExpression node)
            {
                if (node.Variables.Count > 0)
                {
                    _shadowedVars.Push(new HashSet<ParameterExpression>(node.Variables));
                }
                Expression[] b = ExpressionVisitorUtils.VisitBlockExpressions(this, node);
                if (node.Variables.Count > 0)
                {
                    _shadowedVars.Pop();
                }
                if (b == null)
                {
                    return node;
                }
                return node.Rewrite(node.Variables, b);
            }

            protected override CatchBlock VisitCatchBlock(CatchBlock node)
            {
                if (node.Variable != null)
                {
                    _shadowedVars.Push(new HashSet<ParameterExpression>{ node.Variable });
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
                    IStrongBox box = GetBox(node.Variables[i]);
                    if (box == null)
                    {
                        indexes[i] = vars.Count;
                        vars.Add(node.Variables[i]);
                    }
                    else
                    {
                        indexes[i] = -1 - boxes.Count;
                        boxes.Add(box);
                    }
                }

                // No variables were rewritten. Just return the original node
                if (boxes.Count == 0)
                {
                    return node;
                }

                ConstantExpression boxesConst = Expression.Constant(new RuntimeOps.RuntimeVariables(boxes.ToArray()), typeof(IRuntimeVariables));
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
                return new RuntimeOps.MergedRuntimeVariables(first, second, indexes);
            }

            protected internal override Expression VisitParameter(ParameterExpression node)
            {
                IStrongBox box = GetBox(node);
                if (box == null)
                {
                    return node;
                }
                return Expression.Convert(Expression.Field(Expression.Constant(box), "Value"), node.Type);
            }

            private IStrongBox GetBox(ParameterExpression variable)
            {
                LocalVariable var;
                if (_variables.TryGetValue(variable, out var))
                {
                    if (var.InClosure)
                    {
                        return _frame.Closure[var.Index];
                    }
                    else
                    {
                        return (IStrongBox)_frame.Data[var.Index];
                    }
                }

                return null;
            }
        }
    }
}
