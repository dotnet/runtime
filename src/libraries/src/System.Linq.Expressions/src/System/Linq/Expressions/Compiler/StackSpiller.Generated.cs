// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Linq.Expressions.Compiler
{
    internal partial class StackSpiller
    {
        private readonly StackGuard _guard = new StackGuard();

        /// <summary>
        /// Rewrite the expression by performing stack spilling where necessary.
        /// </summary>
        /// <param name="node">Expression to rewrite.</param>
        /// <param name="stack">State of the stack before the expression is emitted.</param>
        /// <returns>Rewritten expression.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private Result RewriteExpression(Expression node, Stack stack)
        {
            if (node == null)
            {
                return new Result(RewriteAction.None, null);
            }

            // When compiling deep trees, we run the risk of triggering a terminating StackOverflowException,
            // so we use the StackGuard utility here to probe for sufficient stack and continue the work on
            // another thread when we run out of stack space.
            if (!_guard.TryEnterOnCurrentStack())
            {
                return _guard.RunOnEmptyStack((StackSpiller @this, Expression n, Stack s) => @this.RewriteExpression(n, s), this, node, stack);
            }

            Result result;
            switch (node.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.And:
                case ExpressionType.ArrayIndex:
                case ExpressionType.Divide:
                case ExpressionType.Equal:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LeftShift:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.NotEqual:
                case ExpressionType.Or:
                case ExpressionType.Power:
                case ExpressionType.RightShift:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.AndAlso:
                case ExpressionType.Coalesce:
                case ExpressionType.OrElse:
                    result = RewriteLogicalBinaryExpression(node, stack);
                    break;
                case ExpressionType.Assign:
                    result = RewriteAssignBinaryExpression(node, stack);
                    break;
                case ExpressionType.ArrayLength:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Decrement:
                case ExpressionType.Increment:
                case ExpressionType.IsFalse:
                case ExpressionType.IsTrue:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.OnesComplement:
                case ExpressionType.TypeAs:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Unbox:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.Throw:
                    result = RewriteThrowUnaryExpression(node, stack);
                    break;
                case ExpressionType.Call:
                    result = RewriteMethodCallExpression(node, stack);
                    break;
                case ExpressionType.Conditional:
                    result = RewriteConditionalExpression(node, stack);
                    break;
                case ExpressionType.Invoke:
                    result = RewriteInvocationExpression(node, stack);
                    break;
                case ExpressionType.Lambda:
                    result = RewriteLambdaExpression(node);
                    break;
                case ExpressionType.ListInit:
                    result = RewriteListInitExpression(node, stack);
                    break;
                case ExpressionType.MemberAccess:
                    result = RewriteMemberExpression(node, stack);
                    break;
                case ExpressionType.MemberInit:
                    result = RewriteMemberInitExpression(node, stack);
                    break;
                case ExpressionType.New:
                    result = RewriteNewExpression(node, stack);
                    break;
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    result = RewriteNewArrayExpression(node, stack);
                    break;
                case ExpressionType.TypeEqual:
                case ExpressionType.TypeIs:
                    result = RewriteTypeBinaryExpression(node, stack);
                    break;
                case ExpressionType.Block:
                    result = RewriteBlockExpression(node, stack);
                    break;
                case ExpressionType.Dynamic:
                    result = RewriteDynamicExpression(node);
                    break;
                case ExpressionType.Extension:
                    result = RewriteExtensionExpression(node, stack);
                    break;
                case ExpressionType.Goto:
                    result = RewriteGotoExpression(node, stack);
                    break;
                case ExpressionType.Index:
                    result = RewriteIndexExpression(node, stack);
                    break;
                case ExpressionType.Label:
                    result = RewriteLabelExpression(node, stack);
                    break;
                case ExpressionType.Loop:
                    result = RewriteLoopExpression(node, stack);
                    break;
                case ExpressionType.Switch:
                    result = RewriteSwitchExpression(node, stack);
                    break;
                case ExpressionType.Try:
                    result = RewriteTryExpression(node, stack);
                    break;
                case ExpressionType.AddAssign:
                case ExpressionType.AndAssign:
                case ExpressionType.DivideAssign:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.OrAssign:
                case ExpressionType.PowerAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.SubtractAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.SubtractAssignChecked:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PostDecrementAssign:
                    result = RewriteReducibleExpression(node, stack);
                    break;
                case ExpressionType.Quote:
                case ExpressionType.Parameter:
                case ExpressionType.Constant:
                case ExpressionType.RuntimeVariables:
                case ExpressionType.Default:
                case ExpressionType.DebugInfo:
                    result = new Result(RewriteAction.None, node);
                    break;

                default:
                    result = RewriteExpression(node.ReduceAndCheck(), stack);
                    if (result.Action == RewriteAction.None)
                    {
                        // It's at least Copy because we reduced the node.
                        result = new Result(result.Action | RewriteAction.Copy, result.Node);
                    }

                    break;
            }

            VerifyRewrite(result, node);

            return result;
        }
    }
}
