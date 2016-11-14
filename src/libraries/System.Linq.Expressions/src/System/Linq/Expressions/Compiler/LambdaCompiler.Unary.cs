// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;
using static System.Linq.Expressions.CachedReflectionInfo;

namespace System.Linq.Expressions.Compiler
{
    internal partial class LambdaCompiler
    {
        private void EmitQuoteUnaryExpression(Expression expr)
        {
            EmitQuote((UnaryExpression)expr);
        }


        private void EmitQuote(UnaryExpression quote)
        {
            // emit the quoted expression as a runtime constant
            EmitConstant(quote.Operand, quote.Type);

            // Heuristic: only emit the tree rewrite logic if we have hoisted
            // locals.
            if (_scope.NearestHoistedLocals != null)
            {
                // HoistedLocals is internal so emit as System.Object
                EmitConstant(_scope.NearestHoistedLocals, typeof(object));
                _scope.EmitGet(_scope.NearestHoistedLocals.SelfVariable);
                _ilg.Emit(OpCodes.Call, RuntimeOps_Quote);

                if (quote.Type != typeof(Expression))
                {
                    _ilg.Emit(OpCodes.Castclass, quote.Type);
                }
            }
        }

        private void EmitThrowUnaryExpression(Expression expr)
        {
            EmitThrow((UnaryExpression)expr, CompilationFlags.EmitAsDefaultType);
        }

        private void EmitThrow(UnaryExpression expr, CompilationFlags flags)
        {
            if (expr.Operand == null)
            {
                CheckRethrow();

                _ilg.Emit(OpCodes.Rethrow);
            }
            else
            {
                EmitExpression(expr.Operand);
                _ilg.Emit(OpCodes.Throw);
            }

            EmitUnreachable(expr, flags);
        }

        private void EmitUnaryExpression(Expression expr, CompilationFlags flags)
        {
            EmitUnary((UnaryExpression)expr, flags);
        }

        private void EmitUnary(UnaryExpression node, CompilationFlags flags)
        {
            if (node.Method != null)
            {
                EmitUnaryMethod(node, flags);
            }
            else if (node.NodeType == ExpressionType.NegateChecked && node.Operand.Type.IsInteger())
            {
                EmitExpression(node.Operand);
                LocalBuilder loc = GetLocal(node.Operand.Type);
                _ilg.Emit(OpCodes.Stloc, loc);
                _ilg.EmitInt(0);
                _ilg.EmitConvertToType(typeof(int), node.Operand.Type, isChecked: false);
                _ilg.Emit(OpCodes.Ldloc, loc);
                FreeLocal(loc);
                EmitBinaryOperator(ExpressionType.SubtractChecked, node.Operand.Type, node.Operand.Type, node.Type, liftedToNull: false);
            }
            else
            {
                EmitExpression(node.Operand);
                EmitUnaryOperator(node.NodeType, node.Operand.Type, node.Type);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void EmitUnaryOperator(ExpressionType op, Type operandType, Type resultType)
        {
            bool operandIsNullable = operandType.IsNullableType();

            if (op == ExpressionType.ArrayLength)
            {
                _ilg.Emit(OpCodes.Ldlen);
                return;
            }

            if (operandIsNullable)
            {
                switch (op)
                {
                    case ExpressionType.Not:
                        {
                            if (operandType != typeof(bool?))
                            {
                                goto case ExpressionType.Negate;
                            }
                            Label labEnd = _ilg.DefineLabel();
                            LocalBuilder loc = GetLocal(operandType);

                            // store values (reverse order since they are already on the stack)
                            _ilg.Emit(OpCodes.Stloc, loc);

                            // test for null
                            _ilg.Emit(OpCodes.Ldloca, loc);
                            _ilg.EmitHasValue(operandType);
                            _ilg.Emit(OpCodes.Brfalse_S, labEnd);

                            // do op on non-null value
                            _ilg.Emit(OpCodes.Ldloca, loc);
                            _ilg.EmitGetValueOrDefault(operandType);
                            Type nnOperandType = operandType.GetNonNullableType();
                            EmitUnaryOperator(op, nnOperandType, typeof(bool));

                            // construct result
                            ConstructorInfo ci = resultType.GetConstructor(ArrayOfType_Bool);
                            _ilg.Emit(OpCodes.Newobj, ci);
                            _ilg.Emit(OpCodes.Stloc, loc);

                            _ilg.MarkLabel(labEnd);
                            _ilg.Emit(OpCodes.Ldloc, loc);
                            FreeLocal(loc);
                            return;
                        }
                    case ExpressionType.UnaryPlus:
                    case ExpressionType.NegateChecked:
                    case ExpressionType.Negate:
                    case ExpressionType.Increment:
                    case ExpressionType.Decrement:
                    case ExpressionType.OnesComplement:
                    case ExpressionType.IsFalse:
                    case ExpressionType.IsTrue:
                        {
                            Debug.Assert(TypeUtils.AreEquivalent(operandType, resultType));
                            Label labIfNull = _ilg.DefineLabel();
                            Label labEnd = _ilg.DefineLabel();
                            LocalBuilder loc = GetLocal(operandType);

                            // check for null
                            _ilg.Emit(OpCodes.Stloc, loc);
                            _ilg.Emit(OpCodes.Ldloca, loc);
                            _ilg.EmitHasValue(operandType);
                            _ilg.Emit(OpCodes.Brfalse_S, labIfNull);

                            // apply operator to non-null value
                            _ilg.Emit(OpCodes.Ldloca, loc);
                            _ilg.EmitGetValueOrDefault(operandType);
                            Type nnOperandType = resultType.GetNonNullableType();
                            EmitUnaryOperator(op, nnOperandType, nnOperandType);

                            // construct result
                            ConstructorInfo ci = resultType.GetConstructor(new Type[] { nnOperandType });
                            _ilg.Emit(OpCodes.Newobj, ci);
                            _ilg.Emit(OpCodes.Stloc, loc);
                            _ilg.Emit(OpCodes.Br_S, labEnd);

                            // if null then create a default one
                            _ilg.MarkLabel(labIfNull);
                            _ilg.Emit(OpCodes.Ldloca, loc);
                            _ilg.Emit(OpCodes.Initobj, resultType);

                            _ilg.MarkLabel(labEnd);
                            _ilg.Emit(OpCodes.Ldloc, loc);
                            FreeLocal(loc);
                            return;
                        }
                    case ExpressionType.TypeAs:
                        _ilg.Emit(OpCodes.Box, operandType);
                        _ilg.Emit(OpCodes.Isinst, resultType);
                        if (resultType.IsNullableType())
                        {
                            _ilg.Emit(OpCodes.Unbox_Any, resultType);
                        }
                        return;
                    default:
                        throw Error.UnhandledUnary(op);
                }
            }
            else
            {
                switch (op)
                {
                    case ExpressionType.Not:
                        if (operandType == typeof(bool))
                        {
                            _ilg.Emit(OpCodes.Ldc_I4_0);
                            _ilg.Emit(OpCodes.Ceq);
                        }
                        else
                        {
                            _ilg.Emit(OpCodes.Not);
                        }
                        break;
                    case ExpressionType.OnesComplement:
                        _ilg.Emit(OpCodes.Not);
                        break;
                    case ExpressionType.IsFalse:
                        _ilg.Emit(OpCodes.Ldc_I4_0);
                        _ilg.Emit(OpCodes.Ceq);
                        // Not an arithmetic operation -> no conversion
                        return;
                    case ExpressionType.IsTrue:
                        _ilg.Emit(OpCodes.Ldc_I4_1);
                        _ilg.Emit(OpCodes.Ceq);
                        // Not an arithmetic operation -> no conversion
                        return;
                    case ExpressionType.UnaryPlus:
                        _ilg.Emit(OpCodes.Nop);
                        break;
                    case ExpressionType.Negate:
                    case ExpressionType.NegateChecked:
                        _ilg.Emit(OpCodes.Neg);
                        break;
                    case ExpressionType.TypeAs:
                        if (operandType.GetTypeInfo().IsValueType)
                        {
                            _ilg.Emit(OpCodes.Box, operandType);
                        }
                        _ilg.Emit(OpCodes.Isinst, resultType);
                        if (resultType.IsNullableType())
                        {
                            _ilg.Emit(OpCodes.Unbox_Any, resultType);
                        }
                        // Not an arithmetic operation -> no conversion
                        return;
                    case ExpressionType.Increment:
                        EmitConstantOne(resultType);
                        _ilg.Emit(OpCodes.Add);
                        break;
                    case ExpressionType.Decrement:
                        EmitConstantOne(resultType);
                        _ilg.Emit(OpCodes.Sub);
                        break;
                    default:
                        throw Error.UnhandledUnary(op);
                }

                EmitConvertArithmeticResult(op, resultType);
            }
        }

        private void EmitConstantOne(Type type)
        {
            switch (type.GetTypeCode())
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.Int16:
                case TypeCode.Int32:
                    _ilg.Emit(OpCodes.Ldc_I4_1);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    _ilg.Emit(OpCodes.Ldc_I8, (long)1);
                    break;
                case TypeCode.Single:
                    _ilg.Emit(OpCodes.Ldc_R4, 1.0f);
                    break;
                case TypeCode.Double:
                    _ilg.Emit(OpCodes.Ldc_R8, 1.0d);
                    break;
                default:
                    // we only have to worry about arithmetic types, see
                    // TypeUtils.IsArithmetic
                    throw ContractUtils.Unreachable;
            }
        }

        private void EmitUnboxUnaryExpression(Expression expr)
        {
            var node = (UnaryExpression)expr;
            Debug.Assert(node.Type.GetTypeInfo().IsValueType);

            // Unbox_Any leaves the value on the stack
            EmitExpression(node.Operand);
            _ilg.Emit(OpCodes.Unbox_Any, node.Type);
        }

        private void EmitConvertUnaryExpression(Expression expr, CompilationFlags flags)
        {
            EmitConvert((UnaryExpression)expr, flags);
        }

        private void EmitConvert(UnaryExpression node, CompilationFlags flags)
        {
            if (node.Method != null)
            {
                // User-defined conversions are only lifted if both source and
                // destination types are value types.  The C# compiler gets this wrong.
                // In C#, if you have an implicit conversion from int->MyClass and you
                // "lift" the conversion to int?->MyClass then a null int? goes to a
                // null MyClass.  This is contrary to the specification, which states
                // that the correct behaviour is to unwrap the int?, throw an exception
                // if it is null, and then call the conversion.
                //
                // We cannot fix this in C# but there is no reason why we need to
                // propagate this behavior into the expression tree API.  Unfortunately
                // this means that when the C# compiler generates the lambda
                // (int? i)=>(MyClass)i, we will get different results for converting
                // that lambda to a delegate directly and converting that lambda to
                // an expression tree and then compiling it.  We can live with this
                // discrepancy however.

                if (node.IsLifted && (!node.Type.GetTypeInfo().IsValueType || !node.Operand.Type.GetTypeInfo().IsValueType))
                {
                    ParameterInfo[] pis = node.Method.GetParametersCached();
                    Debug.Assert(pis != null && pis.Length == 1);
                    Type paramType = pis[0].ParameterType;
                    if (paramType.IsByRef)
                    {
                        paramType = paramType.GetElementType();
                    }

                    UnaryExpression e = Expression.Convert(
                        Expression.Call(
                            node.Method,
                            Expression.Convert(node.Operand, pis[0].ParameterType)
                        ),
                        node.Type
                    );

                    EmitConvert(e, flags);
                }
                else
                {
                    EmitUnaryMethod(node, flags);
                }
            }
            else if (node.Type == typeof(void))
            {
                EmitExpressionAsVoid(node.Operand, flags);
            }
            else
            {
                if (TypeUtils.AreEquivalent(node.Operand.Type, node.Type))
                {
                    EmitExpression(node.Operand, flags);
                }
                else
                {
                    // A conversion is emitted after emitting the operand, no tail call is emitted
                    EmitExpression(node.Operand);
                    _ilg.EmitConvertToType(node.Operand.Type, node.Type, node.NodeType == ExpressionType.ConvertChecked);
                }
            }
        }


        private void EmitUnaryMethod(UnaryExpression node, CompilationFlags flags)
        {
            if (node.IsLifted)
            {
                ParameterExpression v = Expression.Variable(node.Operand.Type.GetNonNullableType(), name: null);
                MethodCallExpression mc = Expression.Call(node.Method, v);

                Type resultType = mc.Type.GetNullableType();
                EmitLift(node.NodeType, resultType, mc, new ParameterExpression[] { v }, new Expression[] { node.Operand });
                _ilg.EmitConvertToType(resultType, node.Type, isChecked: false);
            }
            else
            {
                EmitMethodCallExpression(Expression.Call(node.Method, node.Operand), flags);
            }
        }
    }
}
