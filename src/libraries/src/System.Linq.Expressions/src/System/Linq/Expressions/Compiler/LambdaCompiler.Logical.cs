// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Linq.Expressions.Compiler
{
    internal partial class LambdaCompiler
    {
        #region Conditional

        private void EmitConditionalExpression(Expression expr, CompilationFlags flags)
        {
            ConditionalExpression node = (ConditionalExpression)expr;
            Debug.Assert(node.Test.Type == typeof(bool));
            Label labFalse = _ilg.DefineLabel();
            EmitExpressionAndBranch(false, node.Test, labFalse);
            EmitExpressionAsType(node.IfTrue, node.Type, flags);

            if (NotEmpty(node.IfFalse))
            {
                Label labEnd = _ilg.DefineLabel();
                if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail)
                {
                    // We know the conditional expression is at the end of the lambda,
                    // so it is safe to emit Ret here.
                    _ilg.Emit(OpCodes.Ret);
                }
                else
                {
                    _ilg.Emit(OpCodes.Br, labEnd);
                }
                _ilg.MarkLabel(labFalse);
                EmitExpressionAsType(node.IfFalse, node.Type, flags);
                _ilg.MarkLabel(labEnd);
            }
            else
            {
                _ilg.MarkLabel(labFalse);
            }
        }

        /// <summary>
        /// returns true if the expression is not empty, otherwise false.
        /// </summary>
        private static bool NotEmpty(Expression node)
        {
            var empty = node as DefaultExpression;
            if (empty == null || empty.Type != typeof(void))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// returns true if the expression is NOT empty and is not debug info,
        /// or a block that contains only insignificant expressions.
        /// </summary>
        private static bool Significant(Expression node)
        {
            var block = node as BlockExpression;
            if (block != null)
            {
                for (int i = 0; i < block.ExpressionCount; i++)
                {
                    if (Significant(block.GetExpression(i)))
                    {
                        return true;
                    }
                }
                return false;
            }
            return NotEmpty(node) && !(node is DebugInfoExpression);
        }

        #endregion

        #region Coalesce


        private void EmitCoalesceBinaryExpression(Expression expr)
        {
            BinaryExpression b = (BinaryExpression)expr;
            Debug.Assert(b.Method == null);

            if (b.Left.Type.IsNullableType())
            {
                EmitNullableCoalesce(b);
            }
            else
            {
                Debug.Assert(!b.Left.Type.IsValueType);
                if (b.Conversion != null)
                {
                    EmitLambdaReferenceCoalesce(b);
                }
                else
                {
                    EmitReferenceCoalesceWithoutConversion(b);
                }
            }
        }


        private void EmitNullableCoalesce(BinaryExpression b)
        {
            Debug.Assert(b.Method == null);

            LocalBuilder loc = GetLocal(b.Left.Type);
            Label labIfNull = _ilg.DefineLabel();
            Label labEnd = _ilg.DefineLabel();
            EmitExpression(b.Left);
            _ilg.Emit(OpCodes.Stloc, loc);
            _ilg.Emit(OpCodes.Ldloca, loc);
            _ilg.EmitHasValue(b.Left.Type);
            _ilg.Emit(OpCodes.Brfalse, labIfNull);

            Type nnLeftType = b.Left.Type.GetNonNullableType();
            if (b.Conversion != null)
            {
                Debug.Assert(b.Conversion.ParameterCount == 1);
                ParameterExpression p = b.Conversion.GetParameter(0);
                Debug.Assert(p.Type.IsAssignableFrom(b.Left.Type) ||
                             p.Type.IsAssignableFrom(nnLeftType));

                // emit the delegate instance
                EmitLambdaExpression(b.Conversion);

                // emit argument
                if (!p.Type.IsAssignableFrom(b.Left.Type))
                {
                    _ilg.Emit(OpCodes.Ldloca, loc);
                    _ilg.EmitGetValueOrDefault(b.Left.Type);
                }
                else
                {
                    _ilg.Emit(OpCodes.Ldloc, loc);
                }

                // emit call to invoke
                _ilg.Emit(OpCodes.Callvirt, b.Conversion.Type.GetInvokeMethod());
            }
            else if (TypeUtils.AreEquivalent(b.Type, b.Left.Type))
            {
                _ilg.Emit(OpCodes.Ldloc, loc);
            }
            else
            {
                _ilg.Emit(OpCodes.Ldloca, loc);
                _ilg.EmitGetValueOrDefault(b.Left.Type);
                if (!TypeUtils.AreEquivalent(b.Type, nnLeftType))
                {
                    _ilg.EmitConvertToType(nnLeftType, b.Type, isChecked: true, locals: this);
                }
            }

            FreeLocal(loc);

            _ilg.Emit(OpCodes.Br, labEnd);
            _ilg.MarkLabel(labIfNull);
            EmitExpression(b.Right);
            if (!TypeUtils.AreEquivalent(b.Right.Type, b.Type))
            {
                _ilg.EmitConvertToType(b.Right.Type, b.Type, isChecked: true, locals: this);
            }
            _ilg.MarkLabel(labEnd);
        }


        private void EmitLambdaReferenceCoalesce(BinaryExpression b)
        {
            LocalBuilder loc = GetLocal(b.Left.Type);
            Label labEnd = _ilg.DefineLabel();
            Label labNotNull = _ilg.DefineLabel();
            EmitExpression(b.Left);
            _ilg.Emit(OpCodes.Dup);
            _ilg.Emit(OpCodes.Stloc, loc);
            _ilg.Emit(OpCodes.Brtrue, labNotNull);
            EmitExpression(b.Right);
            _ilg.Emit(OpCodes.Br, labEnd);

            // if not null, call conversion
            _ilg.MarkLabel(labNotNull);
            Debug.Assert(b.Conversion.ParameterCount == 1);

            // emit the delegate instance
            EmitLambdaExpression(b.Conversion);

            // emit argument
            _ilg.Emit(OpCodes.Ldloc, loc);
            FreeLocal(loc);

            // emit call to invoke
            _ilg.Emit(OpCodes.Callvirt, b.Conversion.Type.GetInvokeMethod());

            _ilg.MarkLabel(labEnd);
        }


        private void EmitReferenceCoalesceWithoutConversion(BinaryExpression b)
        {
            Label labEnd = _ilg.DefineLabel();
            Label labCast = _ilg.DefineLabel();
            EmitExpression(b.Left);
            _ilg.Emit(OpCodes.Dup);
            _ilg.Emit(OpCodes.Brtrue, labCast);
            _ilg.Emit(OpCodes.Pop);
            EmitExpression(b.Right);
            if (!TypeUtils.AreEquivalent(b.Right.Type, b.Type))
            {
                if (b.Right.Type.IsValueType)
                {
                    _ilg.Emit(OpCodes.Box, b.Right.Type);
                }
                _ilg.Emit(OpCodes.Castclass, b.Type);
            }
            _ilg.Emit(OpCodes.Br_S, labEnd);
            _ilg.MarkLabel(labCast);
            if (!TypeUtils.AreEquivalent(b.Left.Type, b.Type))
            {
                Debug.Assert(!b.Left.Type.IsValueType);
                _ilg.Emit(OpCodes.Castclass, b.Type);
            }
            _ilg.MarkLabel(labEnd);
        }

        #endregion

        #region AndAlso

        private void EmitLiftedAndAlso(BinaryExpression b)
        {
            Type type = typeof(bool?);
            Label returnLeft = _ilg.DefineLabel();
            Label returnRight = _ilg.DefineLabel();
            Label exit = _ilg.DefineLabel();
            // Compute left
            EmitExpression(b.Left);
            LocalBuilder locLeft = GetLocal(type);
            _ilg.Emit(OpCodes.Stloc, locLeft);
            _ilg.Emit(OpCodes.Ldloca, locLeft);
            _ilg.EmitHasValue(type);
            _ilg.Emit(OpCodes.Ldloca, locLeft);
            _ilg.EmitGetValueOrDefault(type);
            _ilg.Emit(OpCodes.Not);
            _ilg.Emit(OpCodes.And);
            // if left == false
            _ilg.Emit(OpCodes.Brtrue, returnLeft);
            // Compute right
            EmitExpression(b.Right);
            LocalBuilder locRight = GetLocal(type);
            _ilg.Emit(OpCodes.Stloc, locRight);
            _ilg.Emit(OpCodes.Ldloca, locLeft);
            _ilg.EmitGetValueOrDefault(type);
            // if left == true
            _ilg.Emit(OpCodes.Brtrue_S, returnRight);
            _ilg.Emit(OpCodes.Ldloca, locRight);
            _ilg.EmitGetValueOrDefault(type);
            // if right == true
            _ilg.Emit(OpCodes.Brtrue_S, returnLeft);
            _ilg.MarkLabel(returnRight);
            _ilg.Emit(OpCodes.Ldloc, locRight);
            FreeLocal(locRight);
            _ilg.Emit(OpCodes.Br_S, exit);
            _ilg.MarkLabel(returnLeft);
            _ilg.Emit(OpCodes.Ldloc, locLeft);
            FreeLocal(locLeft);
            _ilg.MarkLabel(exit);
        }

        private void EmitMethodAndAlso(BinaryExpression b, CompilationFlags flags)
        {
            Debug.Assert(b.Method.IsStatic);

            Label labEnd = _ilg.DefineLabel();
            EmitExpression(b.Left);
            _ilg.Emit(OpCodes.Dup);
            MethodInfo opFalse = TypeUtils.GetBooleanOperator(b.Method.DeclaringType, "op_False");
            Debug.Assert(opFalse != null, "factory should check that the method exists");
            _ilg.Emit(OpCodes.Call, opFalse);
            _ilg.Emit(OpCodes.Brtrue, labEnd);

            EmitExpression(b.Right);
            if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail)
            {
                _ilg.Emit(OpCodes.Tailcall);
            }

            _ilg.Emit(OpCodes.Call, b.Method);
            _ilg.MarkLabel(labEnd);
        }

        private void EmitUnliftedAndAlso(BinaryExpression b)
        {
            Label @else = _ilg.DefineLabel();
            Label end = _ilg.DefineLabel();
            EmitExpressionAndBranch(false, b.Left, @else);
            EmitExpression(b.Right);
            _ilg.Emit(OpCodes.Br, end);
            _ilg.MarkLabel(@else);
            _ilg.Emit(OpCodes.Ldc_I4_0);
            _ilg.MarkLabel(end);
        }

        private void EmitAndAlsoBinaryExpression(Expression expr, CompilationFlags flags)
        {
            BinaryExpression b = (BinaryExpression)expr;

            if (b.Method != null)
            {
                if (b.IsLiftedLogical)
                {
                    EmitExpression(b.ReduceUserdefinedLifted());
                }
                else
                {
                    EmitMethodAndAlso(b, flags);
                }
            }
            else if (b.Left.Type == typeof(bool?))
            {
                EmitLiftedAndAlso(b);
            }
            else
            {
                EmitUnliftedAndAlso(b);
            }
        }

        #endregion

        #region OrElse

        private void EmitLiftedOrElse(BinaryExpression b)
        {
            Type type = typeof(bool?);
            Label returnLeft = _ilg.DefineLabel();
            Label exit = _ilg.DefineLabel();
            LocalBuilder locLeft = GetLocal(type);
            EmitExpression(b.Left);
            _ilg.Emit(OpCodes.Stloc, locLeft);
            _ilg.Emit(OpCodes.Ldloca, locLeft);
            _ilg.EmitGetValueOrDefault(type);
            // if left == true
            _ilg.Emit(OpCodes.Brtrue, returnLeft);
            EmitExpression(b.Right);
            LocalBuilder locRight = GetLocal(type);
            _ilg.Emit(OpCodes.Stloc, locRight);
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

        private void EmitUnliftedOrElse(BinaryExpression b)
        {
            Label @else = _ilg.DefineLabel();
            Label end = _ilg.DefineLabel();
            EmitExpressionAndBranch(false, b.Left, @else);
            _ilg.Emit(OpCodes.Ldc_I4_1);
            _ilg.Emit(OpCodes.Br, end);
            _ilg.MarkLabel(@else);
            EmitExpression(b.Right);
            _ilg.MarkLabel(end);
        }

        private void EmitMethodOrElse(BinaryExpression b, CompilationFlags flags)
        {
            Debug.Assert(b.Method.IsStatic);

            Label labEnd = _ilg.DefineLabel();
            EmitExpression(b.Left);
            _ilg.Emit(OpCodes.Dup);
            MethodInfo opTrue = TypeUtils.GetBooleanOperator(b.Method.DeclaringType, "op_True");
            Debug.Assert(opTrue != null, "factory should check that the method exists");

            _ilg.Emit(OpCodes.Call, opTrue);
            _ilg.Emit(OpCodes.Brtrue, labEnd);
            EmitExpression(b.Right);
            if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail)
            {
                _ilg.Emit(OpCodes.Tailcall);
            }

            _ilg.Emit(OpCodes.Call, b.Method);
            _ilg.MarkLabel(labEnd);
        }

        private void EmitOrElseBinaryExpression(Expression expr, CompilationFlags flags)
        {
            BinaryExpression b = (BinaryExpression)expr;

            if (b.Method != null)
            {
                if (b.IsLiftedLogical)
                {
                    EmitExpression(b.ReduceUserdefinedLifted());
                }
                else
                {
                    EmitMethodOrElse(b, flags);
                }
            }
            else if (b.Left.Type == typeof(bool?))
            {
                EmitLiftedOrElse(b);
            }
            else
            {
                EmitUnliftedOrElse(b);
            }
        }

        #endregion

        #region Optimized branching

        /// <summary>
        /// Emits the expression and then either brtrue/brfalse to the label.
        /// </summary>
        /// <param name="branchValue">True for brtrue, false for brfalse.</param>
        /// <param name="node">The expression to emit.</param>
        /// <param name="label">The label to conditionally branch to.</param>
        /// <remarks>
        /// This function optimizes equality and short circuiting logical
        /// operators to avoid double-branching, minimize instruction count,
        /// and generate similar IL to the C# compiler. This is important for
        /// the JIT to optimize patterns like:
        ///     x != null AndAlso x.GetType() == typeof(SomeType)
        ///
        /// One optimization we don't do: we always emits at least one
        /// conditional branch to the label, and always possibly falls through,
        /// even if we know if the branch will always succeed or always fail.
        /// We do this to avoid generating unreachable code, which is fine for
        /// the CLR JIT, but doesn't verify with peverify.
        ///
        /// This kind of optimization could be implemented safely, by doing
        /// constant folding over conditionals and logical expressions at the
        /// tree level.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void EmitExpressionAndBranch(bool branchValue, Expression node, Label label)
        {
            Debug.Assert(node.Type == typeof(bool));
            CompilationFlags startEmitted = EmitExpressionStart(node);
            switch (node.NodeType)
            {
                case ExpressionType.Not:
                    EmitBranchNot(branchValue, (UnaryExpression)node, label);
                    break;

                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    EmitBranchLogical(branchValue, (BinaryExpression)node, label);
                    break;

                case ExpressionType.Block:
                    EmitBranchBlock(branchValue, (BlockExpression)node, label);
                    break;

                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    EmitBranchComparison(branchValue, (BinaryExpression)node, label);
                    break;

                default:
                    EmitExpression(node, CompilationFlags.EmitAsNoTail | CompilationFlags.EmitNoExpressionStart);
                    EmitBranchOp(branchValue, label);
                    break;
            }

            EmitExpressionEnd(startEmitted);
        }

        private void EmitBranchOp(bool branch, Label label)
        {
            _ilg.Emit(branch ? OpCodes.Brtrue : OpCodes.Brfalse, label);
        }

        private void EmitBranchNot(bool branch, UnaryExpression node, Label label)
        {
            if (node.Method != null)
            {
                EmitExpression(node, CompilationFlags.EmitAsNoTail | CompilationFlags.EmitNoExpressionStart);
                EmitBranchOp(branch, label);
                return;
            }

            EmitExpressionAndBranch(!branch, node.Operand, label);
        }

        private void EmitBranchComparison(bool branch, BinaryExpression node, Label label)
        {
            Debug.Assert(node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual);
            Debug.Assert(!node.IsLiftedToNull);

            // To share code paths, we want to treat NotEqual as an inverted Equal
            bool branchWhenEqual = branch == (node.NodeType == ExpressionType.Equal);

            if (node.Method != null)
            {
                EmitBinaryMethod(node, CompilationFlags.EmitAsNoTail);
                // EmitBinaryMethod takes into account the Equal/NotEqual
                // node kind, so use the original branch value
                EmitBranchOp(branch, label);
            }
            else if (ConstantCheck.IsNull(node.Left))
            {
                if (node.Right.Type.IsNullableType())
                {
                    EmitAddress(node.Right, node.Right.Type);
                    _ilg.EmitHasValue(node.Right.Type);
                }
                else
                {
                    Debug.Assert(!node.Right.Type.IsValueType);
                    EmitExpression(GetEqualityOperand(node.Right));
                }
                EmitBranchOp(!branchWhenEqual, label);
            }
            else if (ConstantCheck.IsNull(node.Right))
            {
                if (node.Left.Type.IsNullableType())
                {
                    EmitAddress(node.Left, node.Left.Type);
                    _ilg.EmitHasValue(node.Left.Type);
                }
                else
                {
                    Debug.Assert(!node.Left.Type.IsValueType);
                    EmitExpression(GetEqualityOperand(node.Left));
                }
                EmitBranchOp(!branchWhenEqual, label);
            }
            else if (node.Left.Type.IsNullableType() || node.Right.Type.IsNullableType())
            {
                EmitBinaryExpression(node);
                // EmitBinaryExpression takes into account the Equal/NotEqual
                // node kind, so use the original branch value
                EmitBranchOp(branch, label);
            }
            else
            {
                EmitExpression(GetEqualityOperand(node.Left));
                EmitExpression(GetEqualityOperand(node.Right));
                _ilg.Emit(branchWhenEqual ? OpCodes.Beq : OpCodes.Bne_Un, label);
            }
        }

        // For optimized Equal/NotEqual, we can eliminate reference
        // conversions. IL allows comparing managed pointers regardless of
        // type. See ECMA-335 "Binary Comparison or Branch Operations", in
        // Partition III, Section 1.5 Table 4.
        private static Expression GetEqualityOperand(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var convert = (UnaryExpression)expression;
                if (TypeUtils.AreReferenceAssignable(convert.Type, convert.Operand.Type))
                {
                    return convert.Operand;
                }
            }
            return expression;
        }

        private void EmitBranchLogical(bool branch, BinaryExpression node, Label label)
        {
            Debug.Assert(node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse);
            Debug.Assert(!node.IsLiftedToNull);

            if (node.Method != null || node.IsLifted)
            {
                EmitExpression(node);
                EmitBranchOp(branch, label);
                return;
            }


            bool isAnd = node.NodeType == ExpressionType.AndAlso;

            // To share code, we make the following substitutions:
            //     if (!(left || right)) branch value
            // becomes:
            //     if (!left && !right) branch value
            // and:
            //     if (!(left && right)) branch value
            // becomes:
            //     if (!left || !right) branch value
            //
            // The observation is that "brtrue(x && y)" has the same codegen as
            // "brfalse(x || y)" except the branches have the opposite sign.
            // Same for "brfalse(x && y)" and "brtrue(x || y)".
            //
            if (branch == isAnd)
            {
                EmitBranchAnd(branch, node, label);
            }
            else
            {
                EmitBranchOr(branch, node, label);
            }
        }

        // Generates optimized AndAlso with branch == true
        // or optimized OrElse with branch == false
        private void EmitBranchAnd(bool branch, BinaryExpression node, Label label)
        {
            // if (left) then
            //   if (right) branch label
            // endif

            Label endif = _ilg.DefineLabel();
            EmitExpressionAndBranch(!branch, node.Left, endif);
            EmitExpressionAndBranch(branch, node.Right, label);
            _ilg.MarkLabel(endif);
        }

        // Generates optimized OrElse with branch == true
        // or optimized AndAlso with branch == false
        private void EmitBranchOr(bool branch, BinaryExpression node, Label label)
        {
            // if (left OR right) branch label

            EmitExpressionAndBranch(branch, node.Left, label);
            EmitExpressionAndBranch(branch, node.Right, label);
        }

        private void EmitBranchBlock(bool branch, BlockExpression node, Label label)
        {
            EnterScope(node);

            int count = node.ExpressionCount;
            for (int i = 0; i < count - 1; i++)
            {
                EmitExpressionAsVoid(node.GetExpression(i));
            }
            EmitExpressionAndBranch(branch, node.GetExpression(count - 1), label);

            ExitScope(node);
        }

        #endregion
    }
}
