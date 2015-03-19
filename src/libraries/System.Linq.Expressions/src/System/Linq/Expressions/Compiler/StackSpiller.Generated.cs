// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic.Utils;

namespace System.Linq.Expressions.Compiler
{
    internal partial class StackSpiller
    {
        /// <summary>
        /// Rewrite the expression
        /// </summary>
        /// 
        /// <param name="node">Expression to rewrite</param>
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

            Result result;
            switch (node.NodeType)
            {
                case ExpressionType.Add:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.AddChecked:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.And:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.AndAlso:
                    result = RewriteLogicalBinaryExpression(node, stack);
                    break;
                case ExpressionType.ArrayLength:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.ArrayIndex:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.Call:
                    result = RewriteMethodCallExpression(node, stack);
                    break;
                case ExpressionType.Coalesce:
                    result = RewriteLogicalBinaryExpression(node, stack);
                    break;
                case ExpressionType.Conditional:
                    result = RewriteConditionalExpression(node, stack);
                    break;
                case ExpressionType.Convert:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.ConvertChecked:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.Divide:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.Equal:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.ExclusiveOr:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.GreaterThan:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.Invoke:
                    result = RewriteInvocationExpression(node, stack);
                    break;
                case ExpressionType.Lambda:
                    result = RewriteLambdaExpression(node, stack);
                    break;
                case ExpressionType.LeftShift:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.LessThan:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.LessThanOrEqual:
                    result = RewriteBinaryExpression(node, stack);
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
                case ExpressionType.Modulo:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.Multiply:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.MultiplyChecked:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.Negate:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.UnaryPlus:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.NegateChecked:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.New:
                    result = RewriteNewExpression(node, stack);
                    break;
                case ExpressionType.NewArrayInit:
                    result = RewriteNewArrayExpression(node, stack);
                    break;
                case ExpressionType.NewArrayBounds:
                    result = RewriteNewArrayExpression(node, stack);
                    break;
                case ExpressionType.Not:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.NotEqual:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.Or:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.OrElse:
                    result = RewriteLogicalBinaryExpression(node, stack);
                    break;
                case ExpressionType.Power:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.RightShift:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.Subtract:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.SubtractChecked:
                    result = RewriteBinaryExpression(node, stack);
                    break;
                case ExpressionType.TypeAs:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.TypeIs:
                    result = RewriteTypeBinaryExpression(node, stack);
                    break;
                case ExpressionType.Assign:
                    result = RewriteAssignBinaryExpression(node, stack);
                    break;
                case ExpressionType.Block:
                    result = RewriteBlockExpression(node, stack);
                    break;
                case ExpressionType.Decrement:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.Dynamic:
                    result = RewriteDynamicExpression(node, stack);
                    break;
                case ExpressionType.Extension:
                    result = RewriteExtensionExpression(node, stack);
                    break;
                case ExpressionType.Goto:
                    result = RewriteGotoExpression(node, stack);
                    break;
                case ExpressionType.Increment:
                    result = RewriteUnaryExpression(node, stack);
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
                case ExpressionType.Throw:
                    result = RewriteThrowUnaryExpression(node, stack);
                    break;
                case ExpressionType.Try:
                    result = RewriteTryExpression(node, stack);
                    break;
                case ExpressionType.Unbox:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.TypeEqual:
                    result = RewriteTypeBinaryExpression(node, stack);
                    break;
                case ExpressionType.OnesComplement:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.IsTrue:
                    result = RewriteUnaryExpression(node, stack);
                    break;
                case ExpressionType.IsFalse:
                    result = RewriteUnaryExpression(node, stack);
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
                    return new Result(RewriteAction.None, node);

                default:
                    throw ContractUtils.Unreachable;
            }

            VerifyRewrite(result, node);

            return result;
        }
    }
}

