// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
            IStrongBox[]? closure;
            if (ConsumedStack > 0)
            {
                closure = new IStrongBox[ConsumedStack];
                for (int i = closure.Length - 1; i >= 0; i--)
                {
                    closure[i] = (IStrongBox?)frame.Pop()!;
                }
            }
            else
            {
                closure = null;
            }

            Delegate d = _creator.CreateDelegate(closure);

            frame.Push(d);
            return 1;
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
            frame.Push(_type.IsInstanceOfType(frame.Pop()));
            return 1;
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
            object? value = frame.Pop();
            frame.Push(_type.IsInstanceOfType(value) ? value : null);
            return 1;
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
            object? type = frame.Pop();
            object? obj = frame.Pop();
            frame.Push((object?)obj?.GetType() == type);
            return 1;
        }
    }

    internal abstract class NullableMethodCallInstruction : Instruction
    {
        private static NullableMethodCallInstruction? s_hasValue, s_value, s_equals, s_getHashCode, s_getValueOrDefault1, s_toString;

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "NullableMethod";

        private NullableMethodCallInstruction() { }

        private sealed class HasValue : NullableMethodCallInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? obj = frame.Pop();
                frame.Push(obj != null);
                return 1;
            }
        }

        private sealed class GetValue : NullableMethodCallInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                if (frame.Peek() == null)
                {
                    // Trigger InvalidOperationException with same localized method as if we'd called the Value getter.
                    return (int)default(int?)!;
                }

                return 1;
            }
        }

        private sealed class GetValueOrDefault : NullableMethodCallInstruction
        {
            private readonly Type _defaultValueType;

            public GetValueOrDefault(MethodInfo mi)
            {
                Debug.Assert(mi.ReturnType.IsValueType, "Nullable is only allowed on value types.");
                Debug.Assert(!mi.ReturnType.IsNullableType());

                _defaultValueType = mi.ReturnType;
            }

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077:UnrecognizedReflectionPattern",
                Justification = "_defaultValueType is a ValueType. You can always get an uninitialized ValueType.")]
            public override int Run(InterpretedFrame frame)
            {
                if (frame.Peek() == null)
                {
                    frame.Pop();
                    frame.Push(RuntimeHelpers.GetUninitializedObject(_defaultValueType));
                }
                return 1;
            }
        }

        private sealed class GetValueOrDefault1 : NullableMethodCallInstruction
        {
            public override int ConsumedStack => 2;

            public override int Run(InterpretedFrame frame)
            {
                object? dflt = frame.Pop();
                object? obj = frame.Pop();
                frame.Push(obj ?? dflt);
                return 1;
            }
        }

        private sealed class EqualsClass : NullableMethodCallInstruction
        {
            public override int ConsumedStack => 2;

            public override int Run(InterpretedFrame frame)
            {
                object? other = frame.Pop();
                object? obj = frame.Pop();
                if (obj == null)
                {
                    frame.Push(other == null);
                }
                else if (other == null)
                {
                    frame.Push(Utils.BoxedFalse);
                }
                else
                {
                    frame.Push(obj.Equals(other));
                }
                return 1;
            }
        }

        private sealed class ToStringClass : NullableMethodCallInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? obj = frame.Pop();
                frame.Push(obj == null ? "" : obj.ToString());
                return 1;
            }
        }

        private sealed class GetHashCodeClass : NullableMethodCallInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? obj = frame.Pop();
                frame.Push(obj?.GetHashCode() ?? 0);
                return 1;
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
        private static CastInstruction? s_Boolean, s_Byte, s_Char, s_DateTime, s_Decimal, s_Double, s_Int16, s_Int32, s_Int64, s_SByte, s_Single, s_String, s_UInt16, s_UInt32, s_UInt64;

        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;
        public override string InstructionName => "Cast";

        private sealed class CastInstructionT<T> : CastInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object? value = frame.Pop();
                frame.Push((T)value!);
                return 1;
            }
        }

        private abstract class CastInstructionNoT : CastInstruction
        {
            private readonly Type _t;

            protected CastInstructionNoT(Type t)
            {
                _t = t;
            }

            public static new CastInstruction Create(Type t)
            {
                if (t.IsValueType && !t.IsNullableType())
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
                object? value = frame.Pop();
                if (value != null)
                {
                    Type valueType = value.GetType();

                    if (!valueType.HasReferenceConversionTo(_t) &&
                        !valueType.HasIdentityPrimitiveOrNullableConversionTo(_t))
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
                return 1;
            }

            protected abstract void ConvertNull(InterpretedFrame frame);

            private sealed class Ref : CastInstructionNoT
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

            private sealed class Value : CastInstructionNoT
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
            Debug.Assert(!t.IsEnum);
            return t.GetTypeCode() switch
            {
                TypeCode.Boolean => s_Boolean ?? (s_Boolean = new CastInstructionT<bool>()),
                TypeCode.Byte => s_Byte ?? (s_Byte = new CastInstructionT<byte>()),
                TypeCode.Char => s_Char ?? (s_Char = new CastInstructionT<char>()),
                TypeCode.DateTime => s_DateTime ?? (s_DateTime = new CastInstructionT<DateTime>()),
                TypeCode.Decimal => s_Decimal ?? (s_Decimal = new CastInstructionT<decimal>()),
                TypeCode.Double => s_Double ?? (s_Double = new CastInstructionT<double>()),
                TypeCode.Int16 => s_Int16 ?? (s_Int16 = new CastInstructionT<short>()),
                TypeCode.Int32 => s_Int32 ?? (s_Int32 = new CastInstructionT<int>()),
                TypeCode.Int64 => s_Int64 ?? (s_Int64 = new CastInstructionT<long>()),
                TypeCode.SByte => s_SByte ?? (s_SByte = new CastInstructionT<sbyte>()),
                TypeCode.Single => s_Single ?? (s_Single = new CastInstructionT<float>()),
                TypeCode.String => s_String ?? (s_String = new CastInstructionT<string>()),
                TypeCode.UInt16 => s_UInt16 ?? (s_UInt16 = new CastInstructionT<ushort>()),
                TypeCode.UInt32 => s_UInt32 ?? (s_UInt32 = new CastInstructionT<uint>()),
                TypeCode.UInt64 => s_UInt64 ?? (s_UInt64 = new CastInstructionT<ulong>()),

                _ => CastInstructionNoT.Create(t),
            };
        }
    }

    internal sealed class CastToEnumInstruction : CastInstruction
    {
        private readonly Type _t;

        public CastToEnumInstruction(Type t)
        {
            Debug.Assert(t.IsEnum);
            _t = t;
        }

        public override int Run(InterpretedFrame frame)
        {
            object? from = frame.Pop();
            Debug.Assert(
                new[]
                {
                    TypeCode.Empty, TypeCode.Int32, TypeCode.SByte, TypeCode.Int16, TypeCode.Int64, TypeCode.UInt32,
                    TypeCode.Byte, TypeCode.UInt16, TypeCode.UInt64, TypeCode.Char, TypeCode.Boolean
                }.Contains(Convert.GetTypeCode(from)));
            frame.Push(from == null ? null : Enum.ToObject(_t, from));
            return 1;
        }
    }

    internal sealed class CastReferenceToEnumInstruction : CastInstruction
    {
        private readonly Type _t;

        public CastReferenceToEnumInstruction(Type t)
        {
            Debug.Assert(t.IsEnum);
            _t = t;
        }

        public override int Run(InterpretedFrame frame)
        {
            object? from = frame.Pop();
            Debug.Assert(from != null);

            // If from is neither a T nor a type assignable to T (viz. an T-backed enum)
            // this will cause an InvalidCastException, which is what this operation should
            // throw in this case.

            switch (_t.GetTypeCode())
            {
                case TypeCode.Int32:
                    frame.Push(Enum.ToObject(_t, (int)from));
                    break;
                case TypeCode.Int64:
                    frame.Push(Enum.ToObject(_t, (long)from));
                    break;
                case TypeCode.UInt32:
                    frame.Push(Enum.ToObject(_t, (uint)from));
                    break;
                case TypeCode.UInt64:
                    frame.Push(Enum.ToObject(_t, (ulong)from));
                    break;
                case TypeCode.Byte:
                    frame.Push(Enum.ToObject(_t, (byte)from));
                    break;
                case TypeCode.SByte:
                    frame.Push(Enum.ToObject(_t, (sbyte)from));
                    break;
                case TypeCode.Int16:
                    frame.Push(Enum.ToObject(_t, (short)from));
                    break;
                case TypeCode.UInt16:
                    frame.Push(Enum.ToObject(_t, (ushort)from));
                    break;
                case TypeCode.Char:
                    // Disallowed in C#, but allowed in CIL
                    frame.Push(Enum.ToObject(_t, (char)from));
                    break;
                default:
                    // Only remaining possible type.
                    // Disallowed in C#, but allowed in CIL
                    Debug.Assert(_t.GetTypeCode() == TypeCode.Boolean);
                    frame.Push(Enum.ToObject(_t, (bool)from));
                    break;
            }

            return 1;
        }
    }

    internal sealed class QuoteInstruction : Instruction
    {
        private readonly Expression _operand;
        private readonly Dictionary<ParameterExpression, LocalVariable>? _hoistedVariables;

        public QuoteInstruction(Expression operand, Dictionary<ParameterExpression, LocalVariable>? hoistedVariables)
        {
            _operand = operand;
            _hoistedVariables = hoistedVariables;
        }

        public override int ProducedStack => 1;

        public override string InstructionName => "Quote";

        public override int Run(InterpretedFrame frame)
        {
            Expression? operand = _operand;
            if (_hoistedVariables != null)
            {
                operand = new ExpressionQuoter(_hoistedVariables, frame).Visit(operand);
            }
            frame.Push(operand);
            return 1;
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
                if (node.ParameterCount > 0)
                {
                    var parameters = new HashSet<ParameterExpression>();

                    for (int i = 0, n = node.ParameterCount; i < n; i++)
                    {
                        parameters.Add(node.GetParameter(i));
                    }

                    _shadowedVars.Push(parameters);
                }
                Expression? b = Visit(node.Body);
                if (node.ParameterCount > 0)
                {
                    _shadowedVars.Pop();
                }
                if (b == node.Body)
                {
                    return node;
                }
                return node.Rewrite(b, parameters: null);
            }

            protected internal override Expression VisitBlock(BlockExpression node)
            {
                if (node.Variables.Count > 0)
                {
                    _shadowedVars.Push(new HashSet<ParameterExpression>(node.Variables));
                }
                Expression[]? b = ExpressionVisitorUtils.VisitBlockExpressions(this, node);
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
                    _shadowedVars.Push(new HashSet<ParameterExpression> { node.Variable });
                }
                Expression? b = Visit(node.Body);
                Expression? f = Visit(node.Filter);
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
                for (int i = 0; i < indexes.Length; i++)
                {
                    IStrongBox? box = GetBox(node.Variables[i]);
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

                // No variables were rewritten. Just return the original node.
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

                // Otherwise, we need to return an object that merges them.
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
                IStrongBox? box = GetBox(node);
                if (box == null)
                {
                    return node;
                }
                return Expression.Convert(Utils.GetStrongBoxValueField(Expression.Constant(box)), node.Type);
            }

            private IStrongBox? GetBox(ParameterExpression variable)
            {
                if (_variables.TryGetValue(variable, out LocalVariable? var))
                {
                    if (var.InClosure)
                    {
                        return _frame.Closure![var.Index];
                    }
                    else
                    {
                        return (IStrongBox?)_frame.Data[var.Index];
                    }
                }

                return null;
            }
        }
    }
}
