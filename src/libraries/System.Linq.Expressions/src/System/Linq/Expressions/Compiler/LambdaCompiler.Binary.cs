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
        private void EmitBinaryExpression(Expression expr)
        {
            EmitBinaryExpression(expr, CompilationFlags.EmitAsNoTail);
        }

        private void EmitBinaryExpression(Expression expr, CompilationFlags flags)
        {
            BinaryExpression b = (BinaryExpression)expr;

            Debug.Assert(b.NodeType != ExpressionType.AndAlso && b.NodeType != ExpressionType.OrElse && b.NodeType != ExpressionType.Coalesce);

            if (b.Method != null)
            {
                EmitBinaryMethod(b, flags);
                return;
            }

            // For EQ and NE, if there is a user-specified method, use it.
            // Otherwise implement the C# semantics that allow equality
            // comparisons on non-primitive nullable structs that don't
            // overload "=="
            if ((b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual) &&
                (b.Type == typeof(bool) || b.Type == typeof(bool?)))
            {
                // If we have x==null, x!=null, null==x or null!=x where x is
                // nullable but not null, then generate a call to x.HasValue.
                Debug.Assert(!b.IsLiftedToNull || b.Type == typeof(bool?));
                if (ConstantCheck.IsNull(b.Left) && !ConstantCheck.IsNull(b.Right) && b.Right.Type.IsNullableType())
                {
                    EmitNullEquality(b.NodeType, b.Right, b.IsLiftedToNull);
                    return;
                }
                if (ConstantCheck.IsNull(b.Right) && !ConstantCheck.IsNull(b.Left) && b.Left.Type.IsNullableType())
                {
                    EmitNullEquality(b.NodeType, b.Left, b.IsLiftedToNull);
                    return;
                }

                // For EQ and NE, we can avoid some conversions if we're
                // ultimately just comparing two managed pointers.
                EmitExpression(GetEqualityOperand(b.Left));
                EmitExpression(GetEqualityOperand(b.Right));
            }
            else
            {
                // Otherwise generate it normally
                EmitExpression(b.Left);
                EmitExpression(b.Right);
            }

            EmitBinaryOperator(b.NodeType, b.Left.Type, b.Right.Type, b.Type, b.IsLiftedToNull);
        }


        private void EmitNullEquality(ExpressionType op, Expression e, bool isLiftedToNull)
        {
            Debug.Assert(e.Type.IsNullableType());
            Debug.Assert(op == ExpressionType.Equal || op == ExpressionType.NotEqual);
            // If we are lifted to null then just evaluate the expression for its side effects, discard,
            // and generate null.  If we are not lifted to null then generate a call to HasValue.
            if (isLiftedToNull)
            {
                EmitExpressionAsVoid(e);
                _ilg.EmitDefault(typeof(bool?), this);
            }
            else
            {
                EmitAddress(e, e.Type);
                _ilg.EmitHasValue(e.Type);
                if (op == ExpressionType.Equal)
                {
                    _ilg.Emit(OpCodes.Ldc_I4_0);
                    _ilg.Emit(OpCodes.Ceq);
                }
            }
        }


        private void EmitBinaryMethod(BinaryExpression b, CompilationFlags flags)
        {
            if (b.IsLifted)
            {
                ParameterExpression p1 = Expression.Variable(b.Left.Type.GetNonNullableType(), name: null);
                ParameterExpression p2 = Expression.Variable(b.Right.Type.GetNonNullableType(), name: null);
                MethodCallExpression mc = Expression.Call(null, b.Method, p1, p2);
                Type resultType;
                if (b.IsLiftedToNull)
                {
                    resultType = mc.Type.GetNullableType();
                }
                else
                {
                    Debug.Assert(mc.Type == typeof(bool));
                    Debug.Assert(b.NodeType == ExpressionType.Equal
                        || b.NodeType == ExpressionType.NotEqual
                        || b.NodeType == ExpressionType.LessThan
                        || b.NodeType == ExpressionType.LessThanOrEqual
                        || b.NodeType == ExpressionType.GreaterThan
                        || b.NodeType == ExpressionType.GreaterThanOrEqual);

                    resultType = typeof(bool);
                }

                Debug.Assert(TypeUtils.AreReferenceAssignable(p1.Type, b.Left.Type.GetNonNullableType()));
                Debug.Assert(TypeUtils.AreReferenceAssignable(p2.Type, b.Right.Type.GetNonNullableType()));
                EmitLift(b.NodeType, resultType, mc, new[] { p1, p2 }, new[] { b.Left, b.Right });
            }
            else
            {
                EmitMethodCallExpression(Expression.Call(null, b.Method, b.Left, b.Right), flags);
            }
        }


        private void EmitBinaryOperator(ExpressionType op, Type leftType, Type rightType, Type resultType, bool liftedToNull)
        {
            Debug.Assert(op != ExpressionType.Coalesce);
            if (op == ExpressionType.ArrayIndex)
            {
                Debug.Assert(rightType == typeof(int));
                EmitGetArrayElement(leftType);
            }
            else if (leftType.IsNullableType() || rightType.IsNullableType())
            {
                EmitLiftedBinaryOp(op, leftType, rightType, resultType, liftedToNull);
            }
            else
            {
                EmitUnliftedBinaryOp(op, leftType, rightType);
            }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void EmitUnliftedBinaryOp(ExpressionType op, Type leftType, Type rightType)
        {
            Debug.Assert(!leftType.IsNullableType());
            Debug.Assert(!rightType.IsNullableType());
            Debug.Assert(leftType.IsPrimitive || (op == ExpressionType.Equal || op == ExpressionType.NotEqual) && (!leftType.IsValueType || leftType.IsEnum));

            switch (op)
            {
                case ExpressionType.NotEqual:
                    if (leftType.GetTypeCode() == TypeCode.Boolean)
                    {
                        goto case ExpressionType.ExclusiveOr;
                    }

                    _ilg.Emit(OpCodes.Ceq);
                    _ilg.Emit(OpCodes.Ldc_I4_0);
                    goto case ExpressionType.Equal;
                case ExpressionType.Equal:
                    _ilg.Emit(OpCodes.Ceq);
                    return;
                case ExpressionType.Add:
                    _ilg.Emit(OpCodes.Add);
                    break;
                case ExpressionType.AddChecked:
                    _ilg.Emit(leftType.IsFloatingPoint() ? OpCodes.Add : (leftType.IsUnsigned() ? OpCodes.Add_Ovf_Un : OpCodes.Add_Ovf));
                    break;
                case ExpressionType.Subtract:
                    _ilg.Emit(OpCodes.Sub);
                    break;
                case ExpressionType.SubtractChecked:
                    if (leftType.IsUnsigned())
                    {
                        _ilg.Emit(OpCodes.Sub_Ovf_Un);
                        // Guaranteed to fit within result type: no conversion
                        return;
                    }
                    else
                    {
                        _ilg.Emit(leftType.IsFloatingPoint() ? OpCodes.Sub : OpCodes.Sub_Ovf);
                    }
                    break;
                case ExpressionType.Multiply:
                    _ilg.Emit(OpCodes.Mul);
                    break;
                case ExpressionType.MultiplyChecked:
                    _ilg.Emit(leftType.IsFloatingPoint() ? OpCodes.Mul : (leftType.IsUnsigned() ? OpCodes.Mul_Ovf_Un : OpCodes.Mul_Ovf));
                    break;
                case ExpressionType.Divide:
                    _ilg.Emit(leftType.IsUnsigned() ? OpCodes.Div_Un : OpCodes.Div);
                    break;
                case ExpressionType.Modulo:
                    _ilg.Emit(leftType.IsUnsigned() ? OpCodes.Rem_Un : OpCodes.Rem);
                    // Guaranteed to fit within result type: no conversion
                    return;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    _ilg.Emit(OpCodes.And);
                    // Not an arithmetic operation: no conversion
                    return;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    _ilg.Emit(OpCodes.Or);
                    // Not an arithmetic operation: no conversion
                    return;
                case ExpressionType.LessThan:
                    _ilg.Emit(leftType.IsUnsigned() ? OpCodes.Clt_Un : OpCodes.Clt);
                    // Not an arithmetic operation: no conversion
                    return;
                case ExpressionType.LessThanOrEqual:
                    _ilg.Emit(leftType.IsUnsigned() || leftType.IsFloatingPoint() ? OpCodes.Cgt_Un : OpCodes.Cgt);
                    _ilg.Emit(OpCodes.Ldc_I4_0);
                    _ilg.Emit(OpCodes.Ceq);
                    // Not an arithmetic operation: no conversion
                    return;
                case ExpressionType.GreaterThan:
                    _ilg.Emit(leftType.IsUnsigned() ? OpCodes.Cgt_Un : OpCodes.Cgt);
                    // Not an arithmetic operation: no conversion
                    return;
                case ExpressionType.GreaterThanOrEqual:
                    _ilg.Emit(leftType.IsUnsigned() || leftType.IsFloatingPoint() ? OpCodes.Clt_Un : OpCodes.Clt);
                    _ilg.Emit(OpCodes.Ldc_I4_0);
                    _ilg.Emit(OpCodes.Ceq);
                    // Not an arithmetic operation: no conversion
                    return;
                case ExpressionType.ExclusiveOr:
                    _ilg.Emit(OpCodes.Xor);
                    // Not an arithmetic operation: no conversion
                    return;
                case ExpressionType.LeftShift:
                    Debug.Assert(rightType == typeof(int));
                    EmitShiftMask(leftType);
                    _ilg.Emit(OpCodes.Shl);
                    break;
                case ExpressionType.RightShift:
                    Debug.Assert(rightType == typeof(int));
                    EmitShiftMask(leftType);
                    _ilg.Emit(leftType.IsUnsigned() ? OpCodes.Shr_Un : OpCodes.Shr);
                    // Guaranteed to fit within result type: no conversion
                    return;
            }

            EmitConvertArithmeticResult(op, leftType);
        }

        // Shift operations have undefined behavior if the shift amount exceeds
        // the number of bits in the value operand. See CLI III.3.58 and C# 7.9
        // for the bit mask used below.
        private void EmitShiftMask(Type leftType)
        {
            int mask = leftType.IsInteger64() ? 0x3F : 0x1F;
            _ilg.EmitPrimitive(mask);
            _ilg.Emit(OpCodes.And);
        }

        // Binary/unary operations on 8 and 16 bit operand types will leave a
        // 32-bit value on the stack, because that's how IL works. For these
        // cases, we need to cast it back to the resultType, possibly using a
        // checked conversion if the original operator was convert
        private void EmitConvertArithmeticResult(ExpressionType op, Type resultType)
        {
            Debug.Assert(!resultType.IsNullableType());

            switch (resultType.GetTypeCode())
            {
                case TypeCode.Byte:
                    _ilg.Emit(IsChecked(op) ? OpCodes.Conv_Ovf_U1 : OpCodes.Conv_U1);
                    break;
                case TypeCode.SByte:
                    _ilg.Emit(IsChecked(op) ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_I1);
                    break;
                case TypeCode.UInt16:
                    _ilg.Emit(IsChecked(op) ? OpCodes.Conv_Ovf_U2 : OpCodes.Conv_U2);
                    break;
                case TypeCode.Int16:
                    _ilg.Emit(IsChecked(op) ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_I2);
                    break;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void EmitLiftedBinaryOp(ExpressionType op, Type leftType, Type rightType, Type resultType, bool liftedToNull)
        {
            Debug.Assert(leftType.IsNullableType() || rightType.IsNullableType());
            switch (op)
            {
                case ExpressionType.And:
                    if (leftType == typeof(bool?))
                    {
                        EmitLiftedBooleanAnd();
                    }
                    else
                    {
                        EmitLiftedBinaryArithmetic(op, leftType, rightType, resultType);
                    }
                    break;
                case ExpressionType.Or:
                    if (leftType == typeof(bool?))
                    {
                        EmitLiftedBooleanOr();
                    }
                    else
                    {
                        EmitLiftedBinaryArithmetic(op, leftType, rightType, resultType);
                    }
                    break;
                case ExpressionType.ExclusiveOr:
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.LeftShift:
                case ExpressionType.RightShift:
                    EmitLiftedBinaryArithmetic(op, leftType, rightType, resultType);
                    break;
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    Debug.Assert(leftType == rightType);
                    if (liftedToNull)
                    {
                        Debug.Assert(resultType == typeof(bool?));
                        EmitLiftedToNullRelational(op, leftType);
                    }
                    else
                    {
                        Debug.Assert(resultType == typeof(bool));
                        EmitLiftedRelational(op, leftType);
                    }
                    break;
            }
        }

        private void EmitLiftedRelational(ExpressionType op, Type type)
        {
            // Equal is (left.GetValueOrDefault() == right.GetValueOrDefault()) & (left.HasValue == right.HasValue)
            // NotEqual is !((left.GetValueOrDefault() == right.GetValueOrDefault()) & (left.HasValue == right.HasValue))
            // Others are (left.GetValueOrDefault() op right.GetValueOrDefault()) & (left.HasValue & right.HasValue)

            bool invert = op == ExpressionType.NotEqual;
            if (invert)
            {
                op = ExpressionType.Equal;
            }

            LocalBuilder locLeft = GetLocal(type);
            LocalBuilder locRight = GetLocal(type);

            _ilg.Emit(OpCodes.Stloc, locRight);
            _ilg.Emit(OpCodes.Stloc, locLeft);
            _ilg.Emit(OpCodes.Ldloca, locLeft);
            _ilg.EmitGetValueOrDefault(type);
            _ilg.Emit(OpCodes.Ldloca, locRight);
            _ilg.EmitGetValueOrDefault(type);
            Type unnullable = type.GetNonNullableType();
            EmitUnliftedBinaryOp(op, unnullable, unnullable);
            _ilg.Emit(OpCodes.Ldloca, locLeft);
            _ilg.EmitHasValue(type);
            _ilg.Emit(OpCodes.Ldloca, locRight);
            _ilg.EmitHasValue(type);
            FreeLocal(locLeft);
            FreeLocal(locRight);
            _ilg.Emit(op == ExpressionType.Equal ? OpCodes.Ceq : OpCodes.And);
            _ilg.Emit(OpCodes.And);
            if (invert)
            {
                _ilg.Emit(OpCodes.Ldc_I4_0);
                _ilg.Emit(OpCodes.Ceq);
            }
        }

        private void EmitLiftedToNullRelational(ExpressionType op, Type type)
        {
            // (left.HasValue & right.HasValue) ? left.GetValueOrDefault() op right.GetValueOrDefault() : default(bool?)
            Label notNull = _ilg.DefineLabel();
            Label end = _ilg.DefineLabel();

            LocalBuilder locLeft = GetLocal(type);
            LocalBuilder locRight = GetLocal(type);

            _ilg.Emit(OpCodes.Stloc, locRight);
            _ilg.Emit(OpCodes.Stloc, locLeft);
            _ilg.Emit(OpCodes.Ldloca, locLeft);
            _ilg.EmitHasValue(type);
            _ilg.Emit(OpCodes.Ldloca, locRight);
            _ilg.EmitHasValue(type);
            _ilg.Emit(OpCodes.And);
            _ilg.Emit(OpCodes.Brtrue_S, notNull);
            _ilg.EmitDefault(typeof(bool?), this);
            _ilg.Emit(OpCodes.Br_S, end);
            _ilg.MarkLabel(notNull);
            _ilg.Emit(OpCodes.Ldloca, locLeft);
            _ilg.EmitGetValueOrDefault(type);
            _ilg.Emit(OpCodes.Ldloca, locRight);
            _ilg.EmitGetValueOrDefault(type);
            FreeLocal(locLeft);
            FreeLocal(locRight);
            Type unnullable = type.GetNonNullableType();
            EmitUnliftedBinaryOp(op, unnullable, unnullable);
            _ilg.Emit(OpCodes.Newobj, Nullable_Boolean_Ctor);
            _ilg.MarkLabel(end);
        }


        private void EmitLiftedBinaryArithmetic(ExpressionType op, Type leftType, Type rightType, Type resultType)
        {
            bool leftIsNullable = leftType.IsNullableType();
            bool rightIsNullable = rightType.IsNullableType();

            Debug.Assert(leftIsNullable || rightIsNullable);

            Label labIfNull = _ilg.DefineLabel();
            Label labEnd = _ilg.DefineLabel();
            LocalBuilder locLeft = GetLocal(leftType);
            LocalBuilder locRight = GetLocal(rightType);
            LocalBuilder locResult = GetLocal(resultType);

            // store values (reverse order since they are already on the stack)
            _ilg.Emit(OpCodes.Stloc, locRight);
            _ilg.Emit(OpCodes.Stloc, locLeft);

            // test for null
            // don't use short circuiting
            if (leftIsNullable)
            {
                _ilg.Emit(OpCodes.Ldloca, locLeft);
                _ilg.EmitHasValue(leftType);
            }

            if (rightIsNullable)
            {
                _ilg.Emit(OpCodes.Ldloca, locRight);
                _ilg.EmitHasValue(rightType);
                if (leftIsNullable)
                {
                    _ilg.Emit(OpCodes.And);
                }
            }

            _ilg.Emit(OpCodes.Brfalse_S, labIfNull);

            // do op on values
            if (leftIsNullable)
            {
                _ilg.Emit(OpCodes.Ldloca, locLeft);
                _ilg.EmitGetValueOrDefault(leftType);
            }
            else
            {
                _ilg.Emit(OpCodes.Ldloc, locLeft);
            }

            if (rightIsNullable)
            {
                _ilg.Emit(OpCodes.Ldloca, locRight);
                _ilg.EmitGetValueOrDefault(rightType);
            }
            else
            {
                _ilg.Emit(OpCodes.Ldloc, locRight);
            }

            //RELEASING locLeft locRight
            FreeLocal(locLeft);
            FreeLocal(locRight);

            EmitBinaryOperator(op, leftType.GetNonNullableType(), rightType.GetNonNullableType(), resultType.GetNonNullableType(), liftedToNull: false);

            // construct result type
            ConstructorInfo ci = resultType.GetConstructor(new Type[] { resultType.GetNonNullableType() });
            _ilg.Emit(OpCodes.Newobj, ci);
            _ilg.Emit(OpCodes.Stloc, locResult);
            _ilg.Emit(OpCodes.Br_S, labEnd);

            // if null then create a default one
            _ilg.MarkLabel(labIfNull);
            _ilg.Emit(OpCodes.Ldloca, locResult);
            _ilg.Emit(OpCodes.Initobj, resultType);

            _ilg.MarkLabel(labEnd);

            _ilg.Emit(OpCodes.Ldloc, locResult);

            //RELEASING locResult
            FreeLocal(locResult);
        }


        private void EmitLiftedBooleanAnd()
        {
            Type type = typeof(bool?);
            Label returnRight = _ilg.DefineLabel();
            Label exit = _ilg.DefineLabel();
            // store values (reverse order since they are already on the stack)
            LocalBuilder locLeft = GetLocal(type);
            LocalBuilder locRight = GetLocal(type);
            _ilg.Emit(OpCodes.Stloc, locRight);
            _ilg.Emit(OpCodes.Stloc, locLeft);
            _ilg.Emit(OpCodes.Ldloca, locLeft);
            _ilg.EmitGetValueOrDefault(type);
            // if left == true
            _ilg.Emit(OpCodes.Brtrue_S, returnRight);
            _ilg.Emit(OpCodes.Ldloca, locLeft);
            _ilg.EmitHasValue(type);
            _ilg.Emit(OpCodes.Ldloca, locRight);
            _ilg.EmitGetValueOrDefault(type);
            _ilg.Emit(OpCodes.Or);
            // if !(left != null | right == true)
            _ilg.Emit(OpCodes.Brfalse_S, returnRight);
            _ilg.Emit(OpCodes.Ldloc, locLeft);
            FreeLocal(locLeft);
            _ilg.Emit(OpCodes.Br_S, exit);
            _ilg.MarkLabel(returnRight);
            _ilg.Emit(OpCodes.Ldloc, locRight);
            FreeLocal(locRight);
            _ilg.MarkLabel(exit);
        }


        private void EmitLiftedBooleanOr()
        {
            Type type = typeof(bool?);
            Label returnLeft = _ilg.DefineLabel();
            Label exit = _ilg.DefineLabel();
            // store values (reverse order since they are already on the stack)
            LocalBuilder locLeft = GetLocal(type);
            LocalBuilder locRight = GetLocal(type);
            _ilg.Emit(OpCodes.Stloc, locRight);
            _ilg.Emit(OpCodes.Stloc, locLeft);
            _ilg.Emit(OpCodes.Ldloca, locLeft);
            _ilg.EmitGetValueOrDefault(type);
            // if left == true
            _ilg.Emit(OpCodes.Brtrue_S, returnLeft);
            _ilg.Emit(OpCodes.Ldloca, locRight);
            _ilg.EmitGetValueOrDefault(type);
            _ilg.Emit(OpCodes.Ldloca, locLeft);
            _ilg.EmitHasValue(type);
            _ilg.Emit(OpCodes.Or);
            // if !(right == true | left != null)
            _ilg.Emit(OpCodes.Brfalse_S, returnLeft);
            _ilg.Emit(OpCodes.Ldloc, locRight);
            FreeLocal(locRight);
            _ilg.Emit(OpCodes.Br_S, exit);
            _ilg.MarkLabel(returnLeft);
            _ilg.Emit(OpCodes.Ldloc, locLeft);
            FreeLocal(locLeft);
            _ilg.MarkLabel(exit);
        }
    }
}
