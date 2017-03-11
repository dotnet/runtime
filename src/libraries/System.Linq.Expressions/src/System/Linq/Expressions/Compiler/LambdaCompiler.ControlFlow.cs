// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Compiler
{
    // The part of the LambdaCompiler dealing with low level control flow
    // break, continue, return, exceptions, etc
    internal partial class LambdaCompiler
    {
        private LabelInfo EnsureLabel(LabelTarget node)
        {
            LabelInfo result;
            if (!_labelInfo.TryGetValue(node, out result))
            {
                _labelInfo.Add(node, result = new LabelInfo(_ilg, node, false));
            }
            return result;
        }

        private LabelInfo ReferenceLabel(LabelTarget node)
        {
            LabelInfo result = EnsureLabel(node);
            result.Reference(_labelBlock);
            return result;
        }

        private LabelInfo DefineLabel(LabelTarget node)
        {
            if (node == null)
            {
                return new LabelInfo(_ilg, null, false);
            }
            LabelInfo result = EnsureLabel(node);
            result.Define(_labelBlock);
            return result;
        }

        private void PushLabelBlock(LabelScopeKind type)
        {
            _labelBlock = new LabelScopeInfo(_labelBlock, type);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "kind")]
        private void PopLabelBlock(LabelScopeKind kind)
        {
            Debug.Assert(_labelBlock != null && _labelBlock.Kind == kind);
            _labelBlock = _labelBlock.Parent;
        }

        private void EmitLabelExpression(Expression expr, CompilationFlags flags)
        {
            var node = (LabelExpression)expr;
            Debug.Assert(node.Target != null);

            // If we're an immediate child of a block, our label will already
            // be defined. If not, we need to define our own block so this
            // label isn't exposed except to its own child expression.
            LabelInfo label = null;

            if (_labelBlock.Kind == LabelScopeKind.Block)
            {
                _labelBlock.TryGetLabelInfo(node.Target, out label);

                // We're in a block but didn't find our label, try switch
                if (label == null && _labelBlock.Parent.Kind == LabelScopeKind.Switch)
                {
                    _labelBlock.Parent.TryGetLabelInfo(node.Target, out label);
                }

                // if we're in a switch or block, we should've found the label
                Debug.Assert(label != null);
            }

            if (label == null)
            {
                label = DefineLabel(node.Target);
            }

            if (node.DefaultValue != null)
            {
                if (node.Target.Type == typeof(void))
                {
                    EmitExpressionAsVoid(node.DefaultValue, flags);
                }
                else
                {
                    flags = UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitExpressionStart);
                    EmitExpression(node.DefaultValue, flags);
                }
            }

            label.Mark();
        }

        private void EmitGotoExpression(Expression expr, CompilationFlags flags)
        {
            var node = (GotoExpression)expr;
            LabelInfo labelInfo = ReferenceLabel(node.Target);

            CompilationFlags tailCall = flags & CompilationFlags.EmitAsTailCallMask;
            if (tailCall != CompilationFlags.EmitAsNoTail)
            {
                // Since tail call flags are not passed into EmitTryExpression, CanReturn
                // means the goto will be emitted as Ret. Therefore we can emit the goto's
                // default value with tail call. This can be improved by detecting if the
                // target label is equivalent to the return label.
                tailCall = labelInfo.CanReturn ? CompilationFlags.EmitAsTail : CompilationFlags.EmitAsNoTail;
                flags = UpdateEmitAsTailCallFlag(flags, tailCall);
            }

            if (node.Value != null)
            {
                if (node.Target.Type == typeof(void))
                {
                    EmitExpressionAsVoid(node.Value, flags);
                }
                else
                {
                    flags = UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitExpressionStart);
                    EmitExpression(node.Value, flags);
                }
            }

            labelInfo.EmitJump();

            EmitUnreachable(node, flags);
        }

        // We need to push default(T), unless we're emitting ourselves as
        // void. Even though the code is unreachable, we still have to
        // generate correct IL. We can get rid of this once we have better
        // reachability analysis.
        private void EmitUnreachable(Expression node, CompilationFlags flags)
        {
            if (node.Type != typeof(void) && (flags & CompilationFlags.EmitAsVoidType) == 0)
            {
                _ilg.EmitDefault(node.Type, this);
            }
        }

        private bool TryPushLabelBlock(Expression node)
        {
            // Anything that is "statement-like" -- e.g. has no associated
            // stack state can be jumped into, with the exception of try-blocks
            // We indicate this by a "Block"
            //
            // Otherwise, we push an "Expression" to indicate that it can't be
            // jumped into
            switch (node.NodeType)
            {
                default:
                    if (_labelBlock.Kind != LabelScopeKind.Expression)
                    {
                        PushLabelBlock(LabelScopeKind.Expression);
                        return true;
                    }
                    return false;
                case ExpressionType.Label:
                    // LabelExpression is a bit special, if it's directly in a
                    // block it becomes associate with the block's scope. Same
                    // thing if it's in a switch case body.
                    if (_labelBlock.Kind == LabelScopeKind.Block)
                    {
                        LabelTarget label = ((LabelExpression)node).Target;
                        if (_labelBlock.ContainsTarget(label))
                        {
                            return false;
                        }
                        if (_labelBlock.Parent.Kind == LabelScopeKind.Switch &&
                            _labelBlock.Parent.ContainsTarget(label))
                        {
                            return false;
                        }
                    }
                    PushLabelBlock(LabelScopeKind.Statement);
                    return true;
                case ExpressionType.Block:
                    if (node is SpilledExpressionBlock)
                    {
                        // treat it as an expression
                        goto default;
                    }

                    PushLabelBlock(LabelScopeKind.Block);
                    // Labels defined immediately in the block are valid for
                    // the whole block.
                    if (_labelBlock.Parent.Kind != LabelScopeKind.Switch)
                    {
                        DefineBlockLabels(node);
                    }
                    return true;
                case ExpressionType.Switch:
                    PushLabelBlock(LabelScopeKind.Switch);
                    // Define labels inside of the switch cases so they are in
                    // scope for the whole switch. This allows "goto case" and
                    // "goto default" to be considered as local jumps.
                    var @switch = (SwitchExpression)node;
                    foreach (SwitchCase c in @switch.Cases)
                    {
                        DefineBlockLabels(c.Body);
                    }
                    DefineBlockLabels(@switch.DefaultBody);
                    return true;

                // Remove this when Convert(Void) goes away.
                case ExpressionType.Convert:
                    if (node.Type != typeof(void))
                    {
                        // treat it as an expression
                        goto default;
                    }
                    PushLabelBlock(LabelScopeKind.Statement);
                    return true;

                case ExpressionType.Conditional:
                case ExpressionType.Loop:
                case ExpressionType.Goto:
                    PushLabelBlock(LabelScopeKind.Statement);
                    return true;
            }
        }

        private void DefineBlockLabels(Expression node)
        {
            var block = node as BlockExpression;
            if (block == null || block is SpilledExpressionBlock)
            {
                return;
            }
            for (int i = 0, n = block.ExpressionCount; i < n; i++)
            {
                Expression e = block.GetExpression(i);

                var label = e as LabelExpression;
                if (label != null)
                {
                    DefineLabel(label.Target);
                }
            }
        }

        // See if this lambda has a return label
        // If so, we'll create it now and mark it as allowing the "ret" opcode
        // This allows us to generate better IL
        private void AddReturnLabel(LambdaExpression lambda)
        {
            Expression expression = lambda.Body;

            while (true)
            {
                switch (expression.NodeType)
                {
                    default:
                        // Didn't find return label
                        return;
                    case ExpressionType.Label:
                        // Found the label. We can directly return from this place
                        // only if the label type is reference assignable to the lambda return type.
                        LabelTarget label = ((LabelExpression)expression).Target;
                        _labelInfo.Add(label, new LabelInfo(_ilg, label, TypeUtils.AreReferenceAssignable(lambda.ReturnType, label.Type)));
                        return;
                    case ExpressionType.Block:
                        // Look in the last significant expression of a block
                        var body = (BlockExpression)expression;
                        // omit empty and debuginfo at the end of the block since they
                        // are not going to emit any IL
                        if (body.ExpressionCount == 0)
                        {
                            return;
                        }
                        for (int i = body.ExpressionCount - 1; i >= 0; i--)
                        {
                            expression = body.GetExpression(i);
                            if (Significant(expression))
                            {
                                break;
                            }
                        }
                        continue;
                }
            }
        }
    }
}
