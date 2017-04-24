// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Compiler
{
    partial class LambdaCompiler
    {
        private readonly StackGuard _guard = new StackGuard();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void EmitExpression(Expression node, CompilationFlags flags)
        {
            // When compiling deep trees, we run the risk of triggering a terminating StackOverflowException,
            // so we use the StackGuard utility here to probe for sufficient stack and continue the work on
            // another thread when we run out of stack space.
            if (!_guard.TryEnterOnCurrentStack())
            {
                _guard.RunOnEmptyStack((LambdaCompiler @this, Expression n, CompilationFlags f) => @this.EmitExpression(n, f), this, node, flags);
                return;
            }

            Debug.Assert(node != null);

            bool emitStart = (flags & CompilationFlags.EmitExpressionStartMask) == CompilationFlags.EmitExpressionStart;

            CompilationFlags startEmitted = emitStart ? EmitExpressionStart(node) : CompilationFlags.EmitNoExpressionStart;
            // only pass tail call flags to emit the expression
            flags = flags & CompilationFlags.EmitAsTailCallMask;

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
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.AndAlso:
                    EmitAndAlsoBinaryExpression(node, flags);
                    break;
                case ExpressionType.OrElse:
                    EmitOrElseBinaryExpression(node, flags);
                    break;
                case ExpressionType.Coalesce:
                    EmitCoalesceBinaryExpression(node);
                    break;
                case ExpressionType.Assign:
                    EmitAssignBinaryExpression(node);
                    break;
                case ExpressionType.ArrayLength:
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
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    EmitConvertUnaryExpression(node, flags);
                    break;
                case ExpressionType.Quote:
                    EmitQuoteUnaryExpression(node);
                    break;
                case ExpressionType.Throw:
                    EmitThrowUnaryExpression(node);
                    break;
                case ExpressionType.Unbox:
                    EmitUnboxUnaryExpression(node);
                    break;
                case ExpressionType.Call:
                    EmitMethodCallExpression(node, flags);
                    break;
                case ExpressionType.Conditional:
                    EmitConditionalExpression(node, flags);
                    break;
                case ExpressionType.Constant:
                    EmitConstantExpression(node);
                    break;
                case ExpressionType.Invoke:
                    EmitInvocationExpression(node, flags);
                    break;
                case ExpressionType.Lambda:
                    EmitLambdaExpression(node);
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
                case ExpressionType.New:
                    EmitNewExpression(node);
                    break;
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    EmitNewArrayExpression(node);
                    break;
                case ExpressionType.Parameter:
                    EmitParameterExpression(node);
                    break;
                case ExpressionType.TypeEqual:
                case ExpressionType.TypeIs:
                    EmitTypeBinaryExpression(node);
                    break;
                case ExpressionType.Block:
                    EmitBlockExpression(node, flags);
                    break;
                case ExpressionType.DebugInfo:
                    EmitDebugInfoExpression(node);
                    break;
                case ExpressionType.Dynamic:
                    EmitDynamicExpression(node);
                    break;
                case ExpressionType.Default:
                    EmitDefaultExpression(node);
                    break;
                case ExpressionType.Goto:
                    EmitGotoExpression(node, flags);
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
                case ExpressionType.Try:
                    EmitTryExpression(node);
                    break;
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
