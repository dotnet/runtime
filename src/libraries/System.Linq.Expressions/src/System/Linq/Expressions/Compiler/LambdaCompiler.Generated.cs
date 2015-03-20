// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Compiler
{
    partial class LambdaCompiler
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void EmitExpression(Expression node, CompilationFlags flags)
        {
            Debug.Assert(node != null);

            bool emitStart = (flags & CompilationFlags.EmitExpressionStartMask) == CompilationFlags.EmitExpressionStart;

            CompilationFlags startEmitted = emitStart ? EmitExpressionStart(node) : CompilationFlags.EmitNoExpressionStart;
            // only pass tail call flags to emit the expression
            flags = flags & CompilationFlags.EmitAsTailCallMask;
            
            switch (node.NodeType)
            {
                case ExpressionType.Add:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.AddChecked:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.And:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.AndAlso:
                    EmitAndAlsoBinaryExpression(node, flags);
                    break;
                case ExpressionType.ArrayLength:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.ArrayIndex:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Call:
                    EmitMethodCallExpression(node, flags);
                    break;
                case ExpressionType.Coalesce:
                    EmitCoalesceBinaryExpression(node);
                    break;
                case ExpressionType.Conditional:
                    EmitConditionalExpression(node, flags);
                    break;
                case ExpressionType.Constant:
                    EmitConstantExpression(node);
                    break;
                case ExpressionType.Convert:
                    EmitConvertUnaryExpression(node, flags);
                    break;
                case ExpressionType.ConvertChecked:
                    EmitConvertUnaryExpression(node, flags);
                    break;
                case ExpressionType.Divide:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Equal:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.ExclusiveOr:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.GreaterThan:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Invoke:
                    EmitInvocationExpression(node, flags);
                    break;
                case ExpressionType.Lambda:
                    EmitLambdaExpression(node);
                    break;
                case ExpressionType.LeftShift:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.LessThan:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.LessThanOrEqual:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.ListInit:
                    EmitListInitExpression(node);
                    break;
                case ExpressionType.MemberAccess:
                    EmitMemberExpression(node);
                    break;
                case ExpressionType.MemberInit:
                    EmitMemberInitExpression(node);
                    break;
                case ExpressionType.Modulo:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Multiply:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.MultiplyChecked:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Negate:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.UnaryPlus:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.NegateChecked:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.New:
                    EmitNewExpression(node);
                    break;
                case ExpressionType.NewArrayInit:
                    EmitNewArrayExpression(node);
                    break;
                case ExpressionType.NewArrayBounds:
                    EmitNewArrayExpression(node);
                    break;
                case ExpressionType.Not:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.NotEqual:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Or:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.OrElse:
                    EmitOrElseBinaryExpression(node, flags);
                    break;
                case ExpressionType.Parameter:
                    EmitParameterExpression(node);
                    break;
                case ExpressionType.Power:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Quote:
                    EmitQuoteUnaryExpression(node);
                    break;
                case ExpressionType.RightShift:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Subtract:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.SubtractChecked:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.TypeAs:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.TypeIs:
                    EmitTypeBinaryExpression(node);
                    break;
                case ExpressionType.Assign:
                    EmitAssignBinaryExpression(node);
                    break;
                case ExpressionType.Block:
                    EmitBlockExpression(node, flags);
                    break;
                case ExpressionType.DebugInfo:
                    EmitDebugInfoExpression(node);
                    break;
                case ExpressionType.Decrement:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.Dynamic:
                    EmitDynamicExpression(node);
                    break;
                case ExpressionType.Default:
                    EmitDefaultExpression(node);
                    break;
                case ExpressionType.Extension:
                    EmitExtensionExpression(node);
                    break;
                case ExpressionType.Goto:
                    EmitGotoExpression(node, flags);
                    break;
                case ExpressionType.Increment:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.Index:
                    EmitIndexExpression(node);
                    break;
                case ExpressionType.Label:
                    EmitLabelExpression(node, flags);
                    break;
                case ExpressionType.RuntimeVariables:
                    EmitRuntimeVariablesExpression(node);
                    break;
                case ExpressionType.Loop:
                    EmitLoopExpression(node);
                    break;
                case ExpressionType.Switch:
                    EmitSwitchExpression(node, flags);
                    break;
                case ExpressionType.Throw:
                    EmitThrowUnaryExpression(node);
                    break;
                case ExpressionType.Try:
                    EmitTryExpression(node);
                    break;
                case ExpressionType.Unbox:
                    EmitUnboxUnaryExpression(node);
                    break;
                case ExpressionType.TypeEqual:
                    EmitTypeBinaryExpression(node);
                    break;
                case ExpressionType.OnesComplement:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.IsTrue:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.IsFalse:
                    EmitUnaryExpression(node, flags);
                    break;

                default:
                    throw ContractUtils.Unreachable;
            }

            if (emitStart)
            {
                EmitExpressionEnd(startEmitted);
            }
        }

        private static bool IsChecked(ExpressionType op)
        {
            switch (op)
            {
                case ExpressionType.AddChecked:
                case ExpressionType.ConvertChecked:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.NegateChecked:
                case ExpressionType.SubtractChecked:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.SubtractAssignChecked:
                    return true;
            }
            return false;
        }

    }
}
