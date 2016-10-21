// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler
{
    /// <summary>
    /// Expression rewriting to spill the CLR stack into temporary variables
    /// in order to guarantee some properties of code generation, for
    /// example that we always enter try block on empty stack.
    /// </summary>
    internal sealed partial class StackSpiller
    {
        // Is the evaluation stack empty?
        private enum Stack
        {
            Empty,
            NonEmpty
        };

        // Should the parent nodes be rewritten, and in what way?
        // Designed so bitwise-or produces the correct result when merging two
        // subtrees. In particular, SpillStack is preferred over Copy which is
        // preferred over None.
        //
        // Values:
        //   None -> no rewrite needed
        //   Copy -> copy into a new node
        //   SpillStack -> spill stack into temps
        [Flags]
        private enum RewriteAction
        {
            None = 0,
            Copy = 1,
            SpillStack = 3,
        }

        // Result of a rewrite operation. Always contains an action and a node.
        private struct Result
        {
            internal readonly RewriteAction Action;
            internal readonly Expression Node;

            internal Result(RewriteAction action, Expression node)
            {
                Action = action;
                Node = node;
            }
        }

        /// <summary>
        /// The source of temporary variables
        /// </summary>
        private readonly TempMaker _tm = new TempMaker();

        /// <summary>
        /// Initial stack state. Normally empty, but when inlining the lambda
        /// we might have a non-empty starting stack state.
        /// </summary>
        private readonly Stack _startingStack;

        /// <summary>
        /// Lambda rewrite result. We need this for inlined lambdas to figure
        /// out whether we need to guarantee it an empty stack.
        /// </summary>
        private RewriteAction _lambdaRewrite;

        /// <summary>
        /// Analyzes a lambda, producing a new one that has correct invariants
        /// for codegen. In particular, it spills the IL stack to temps in
        /// places where it's invalid to have a non-empty stack (for example,
        /// entering a try statement).
        /// </summary>
        internal static LambdaExpression AnalyzeLambda(LambdaExpression lambda)
        {
            return lambda.Accept(new StackSpiller(Stack.Empty));
        }

        private StackSpiller(Stack stack)
        {
            _startingStack = stack;
        }

        // called by Expression<T>.Accept
        internal Expression<T> Rewrite<T>(Expression<T> lambda)
        {
            VerifyTemps();

            // Lambda starts with an empty stack
            Result body = RewriteExpressionFreeTemps(lambda.Body, _startingStack);
            _lambdaRewrite = body.Action;

            VerifyTemps();

            if (body.Action != RewriteAction.None)
            {
                // Create a new scope for temps
                // (none of these will be hoisted so there is no closure impact)
                Expression newBody = body.Node;
                if (_tm.Temps.Count > 0)
                {
                    newBody = Expression.Block(_tm.Temps, newBody);
                }

                // Clone the lambda, replacing the body & variables
                return new Expression<T>(newBody, lambda.Name, lambda.TailCall, lambda.Parameters);
            }

            return lambda;
        }

        #region Expressions

        [Conditional("DEBUG")]
        private static void VerifyRewrite(Result result, Expression node)
        {
            Debug.Assert(result.Node != null);

            // (result.Action == RewriteAction.None) if and only if (node == result.Node)
            Debug.Assert((result.Action == RewriteAction.None) ^ (node != result.Node), "rewrite action does not match node object identity");

            // if the original node is an extension node, it should have been rewritten
            Debug.Assert(result.Node.NodeType != ExpressionType.Extension, "extension nodes must be rewritten");

            // if we have Copy, then node type must match
            Debug.Assert(
                result.Action != RewriteAction.Copy || node.NodeType == result.Node.NodeType || node.CanReduce,
                "rewrite action does not match node object kind"
            );

            // New type must be reference assignable to the old type
            // (our rewrites preserve type exactly, but the rules for rewriting
            // an extension node are more lenient, see Expression.ReduceAndCheck())
            Debug.Assert(
                TypeUtils.AreReferenceAssignable(node.Type, result.Node.Type),
                "rewritten object must be reference assignable to the original type"
            );
        }

        private Result RewriteExpressionFreeTemps(Expression expression, Stack stack)
        {
            int mark = Mark();
            Result result = RewriteExpression(expression, stack);
            Free(mark);
            return result;
        }

        // DynamicExpression
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "stack")]
        private Result RewriteDynamicExpression(Expression expr, Stack stack)
        {
            var node = (IDynamicExpression)expr;

            // CallSite is on the stack
            ChildRewriter cr = new ChildRewriter(this, Stack.NonEmpty, node.ArgumentCount);
            cr.AddArguments(node);
            if (cr.Action == RewriteAction.SpillStack)
            {
                RequireNoRefArgs(node.DelegateType.GetMethod("Invoke"));
            }
            return cr.Finish(cr.Rewrite ? node.Rewrite(cr[0, -1]) : expr);
        }

        private Result RewriteIndexAssignment(BinaryExpression node, Stack stack)
        {
            IndexExpression index = (IndexExpression)node.Left;

            ChildRewriter cr = new ChildRewriter(this, stack, 2 + index.ArgumentCount);

            cr.Add(index.Object);
            cr.AddArguments(index);
            cr.Add(node.Right);

            if (cr.Action == RewriteAction.SpillStack)
            {
                RequireNotRefInstance(index.Object);
            }

            if (cr.Rewrite)
            {
                node = new AssignBinaryExpression(
                    new IndexExpression(
                        cr[0],                              // Object
                        index.Indexer,
                        cr[1, -2]                           // arguments                        
                    ),
                    cr[-1]                                  // value
                );
            }

            return cr.Finish(node);
        }

        // BinaryExpression: AndAlso, OrElse
        private Result RewriteLogicalBinaryExpression(Expression expr, Stack stack)
        {
            BinaryExpression node = (BinaryExpression)expr;

            // Left expression runs on a stack as left by parent
            Result left = RewriteExpression(node.Left, stack);
            // ... and so does the right one
            Result right = RewriteExpression(node.Right, stack);
            //conversion is a lambda. stack state will be ignored. 
            Result conversion = RewriteExpression(node.Conversion, stack);

            RewriteAction action = left.Action | right.Action | conversion.Action;
            if (action != RewriteAction.None)
            {
                // We don't have to worry about byref parameters here, because the
                // factory doesn't allow it (it requires identical parameters and
                // return type from the AndAlso/OrElse method)

                expr = BinaryExpression.Create(
                    node.NodeType,
                    left.Node,
                    right.Node,
                    node.Type,
                    node.Method,
                    (LambdaExpression)conversion.Node
                );
            }
            return new Result(action, expr);
        }

        private Result RewriteReducibleExpression(Expression expr, Stack stack)
        {
            Result result = RewriteExpression(expr.Reduce(), stack);
            // it's at least Copy because we reduced the node
            return new Result(result.Action | RewriteAction.Copy, result.Node);
        }

        // BinaryExpression
        private Result RewriteBinaryExpression(Expression expr, Stack stack)
        {
            BinaryExpression node = (BinaryExpression)expr;

            ChildRewriter cr = new ChildRewriter(this, stack, 3);
            // Left expression executes on the stack as left by parent
            cr.Add(node.Left);
            // Right expression always has non-empty stack (left is on it)
            cr.Add(node.Right);
            // conversion is a lambda, stack state will be ignored
            cr.Add(node.Conversion);

            if (cr.Action == RewriteAction.SpillStack)
            {
                RequireNoRefArgs(node.Method);
            }

            return cr.Finish(cr.Rewrite ?
                                    BinaryExpression.Create(
                                            node.NodeType,
                                            cr[0],
                                            cr[1],
                                            node.Type,
                                            node.Method,
                                            (LambdaExpression)cr[2]) :
                                    expr);
        }

        // variable assignment
        private Result RewriteVariableAssignment(BinaryExpression node, Stack stack)
        {
            // Expression is evaluated on a stack in current state
            Result right = RewriteExpression(node.Right, stack);
            if (right.Action != RewriteAction.None)
            {
                node = new AssignBinaryExpression(node.Left, right.Node);
            }
            return new Result(right.Action, node);
        }

        private Result RewriteAssignBinaryExpression(Expression expr, Stack stack)
        {
            var node = (BinaryExpression)expr;

            switch (node.Left.NodeType)
            {
                case ExpressionType.Index:
                    return RewriteIndexAssignment(node, stack);
                case ExpressionType.MemberAccess:
                    return RewriteMemberAssignment(node, stack);
                case ExpressionType.Parameter:
                    return RewriteVariableAssignment(node, stack);
                case ExpressionType.Extension:
                    return RewriteExtensionAssignment(node, stack);
                default:
                    throw Error.InvalidLvalue(node.Left.NodeType);
            }
        }

        private Result RewriteExtensionAssignment(BinaryExpression node, Stack stack)
        {
            node = new AssignBinaryExpression(node.Left.ReduceExtensions(), node.Right);
            Result result = RewriteAssignBinaryExpression(node, stack);
            // it's at least Copy because we reduced the node
            return new Result(result.Action | RewriteAction.Copy, result.Node);
        }

        // LambdaExpression
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "stack")]
        private static Result RewriteLambdaExpression(Expression expr, Stack stack)
        {
            LambdaExpression node = (LambdaExpression)expr;

            // Call back into the rewriter
            expr = AnalyzeLambda(node);

            // If the lambda gets rewritten, we don't need to spill the stack,
            // but we do need to rebuild the tree above us so it includes the new node.
            RewriteAction action = (expr == node) ? RewriteAction.None : RewriteAction.Copy;

            return new Result(action, expr);
        }

        // ConditionalExpression
        private Result RewriteConditionalExpression(Expression expr, Stack stack)
        {
            ConditionalExpression node = (ConditionalExpression)expr;
            // Test executes at the stack as left by parent
            Result test = RewriteExpression(node.Test, stack);
            // The test is popped by conditional jump so branches execute
            // at the stack as left by parent too.
            Result ifTrue = RewriteExpression(node.IfTrue, stack);
            Result ifFalse = RewriteExpression(node.IfFalse, stack);

            RewriteAction action = test.Action | ifTrue.Action | ifFalse.Action;
            if (action != RewriteAction.None)
            {
                expr = ConditionalExpression.Make(test.Node, ifTrue.Node, ifFalse.Node, node.Type);
            }

            return new Result(action, expr);
        }

        // member assignment
        private Result RewriteMemberAssignment(BinaryExpression node, Stack stack)
        {
            MemberExpression lvalue = (MemberExpression)node.Left;

            ChildRewriter cr = new ChildRewriter(this, stack, 2);

            // If there's an instance, it executes on the stack in current state
            // and rest is executed on non-empty stack.
            // Otherwise the stack is left unchanged.
            cr.Add(lvalue.Expression);

            cr.Add(node.Right);

            if (cr.Action == RewriteAction.SpillStack)
            {
                RequireNotRefInstance(lvalue.Expression);
            }

            if (cr.Rewrite)
            {
                return cr.Finish(
                    new AssignBinaryExpression(
                        MemberExpression.Make(cr[0], lvalue.Member),
                        cr[1]
                    )
                );
            }
            return new Result(RewriteAction.None, node);
        }

        // MemberExpression
        private Result RewriteMemberExpression(Expression expr, Stack stack)
        {
            MemberExpression node = (MemberExpression)expr;

            // Expression is emitted on top of the stack in current state
            Result expression = RewriteExpression(node.Expression, stack);
            if (expression.Action != RewriteAction.None)
            {
                if (expression.Action == RewriteAction.SpillStack &&
                    node.Member is PropertyInfo)
                {
                    // Only need to validate properties because reading a field
                    // is always side-effect free.
                    RequireNotRefInstance(node.Expression);
                }
                expr = MemberExpression.Make(expression.Node, node.Member);
            }
            return new Result(expression.Action, expr);
        }

        //RewriteIndexExpression
        private Result RewriteIndexExpression(Expression expr, Stack stack)
        {
            IndexExpression node = (IndexExpression)expr;

            ChildRewriter cr = new ChildRewriter(this, stack, node.ArgumentCount + 1);

            // For instance methods, the instance executes on the
            // stack as is, but stays on the stack, making it non-empty.
            cr.Add(node.Object);
            cr.AddArguments(node);

            if (cr.Action == RewriteAction.SpillStack)
            {
                RequireNotRefInstance(node.Object);
            }

            if (cr.Rewrite)
            {
                expr = new IndexExpression(
                    cr[0],
                    node.Indexer,
                    cr[1, -1]
                );
            }

            return cr.Finish(expr);
        }

        // MethodCallExpression
        private Result RewriteMethodCallExpression(Expression expr, Stack stack)
        {
            MethodCallExpression node = (MethodCallExpression)expr;

            ChildRewriter cr = new ChildRewriter(this, stack, node.ArgumentCount + 1);

            // For instance methods, the instance executes on the
            // stack as is, but stays on the stack, making it non-empty.
            cr.Add(node.Object);

            cr.AddArguments(node);

            if (cr.Action == RewriteAction.SpillStack)
            {
                RequireNotRefInstance(node.Object);
                RequireNoRefArgs(node.Method);
            }

            if (cr.Rewrite)
            {
                if (node.Object != null)
                {
                    expr = new InstanceMethodCallExpressionN(node.Method, cr[0], cr[1, -1]);
                }
                else
                {
                    expr = new MethodCallExpressionN(node.Method, cr[1, -1]);
                }
            }

            return cr.Finish(expr);
        }

        // NewArrayExpression
        private Result RewriteNewArrayExpression(Expression expr, Stack stack)
        {
            NewArrayExpression node = (NewArrayExpression)expr;

            if (node.NodeType == ExpressionType.NewArrayInit)
            {
                // In a case of array construction with element initialization
                // the element expressions are never emitted on an empty stack because
                // the array reference and the index are on the stack.
                stack = Stack.NonEmpty;
            }
            else
            {
                // In a case of NewArrayBounds we make no modifications to the stack 
                // before emitting bounds expressions.
            }

            ChildRewriter cr = new ChildRewriter(this, stack, node.Expressions.Count);
            cr.Add(node.Expressions);

            if (cr.Rewrite)
            {
                expr = NewArrayExpression.Make(node.NodeType, node.Type, new TrueReadOnlyCollection<Expression>(cr[0, -1]));
            }

            return cr.Finish(expr);
        }

        // InvocationExpression
        private Result RewriteInvocationExpression(Expression expr, Stack stack)
        {
            InvocationExpression node = (InvocationExpression)expr;

            ChildRewriter cr;

            // See if the lambda will be inlined
            LambdaExpression lambda = node.LambdaOperand;
            if (lambda != null)
            {
                // Arguments execute on current stack
                cr = new ChildRewriter(this, stack, node.ArgumentCount);
                cr.AddArguments(node);

                if (cr.Action == RewriteAction.SpillStack)
                {
                    RequireNoRefArgs(Expression.GetInvokeMethod(node.Expression));
                }

                // Lambda body also executes on current stack 
                var spiller = new StackSpiller(stack);
                lambda = lambda.Accept(spiller);

                if (cr.Rewrite || spiller._lambdaRewrite != RewriteAction.None)
                {
                    node = new InvocationExpressionN(lambda, cr[0, -1], node.Type);
                }

                Result result = cr.Finish(node);
                return new Result(result.Action | spiller._lambdaRewrite, result.Node);
            }

            cr = new ChildRewriter(this, stack, node.ArgumentCount + 1);

            // first argument starts on stack as provided
            cr.Add(node.Expression);

            // rest of arguments have non-empty stack (delegate instance on the stack)
            cr.AddArguments(node);

            if (cr.Action == RewriteAction.SpillStack)
            {
                RequireNoRefArgs(Expression.GetInvokeMethod(node.Expression));
            }

            return cr.Finish(cr.Rewrite ? new InvocationExpressionN(cr[0], cr[1, -1], node.Type) : expr);
        }

        // NewExpression
        private Result RewriteNewExpression(Expression expr, Stack stack)
        {
            NewExpression node = (NewExpression)expr;

            // The first expression starts on a stack as provided by parent,
            // rest are definitely non-empty (which ChildRewriter guarantees)
            ChildRewriter cr = new ChildRewriter(this, stack, node.ArgumentCount);
            cr.AddArguments(node);

            if (cr.Action == RewriteAction.SpillStack)
            {
                RequireNoRefArgs(node.Constructor);
            }

            return cr.Finish(cr.Rewrite ? new NewExpression(node.Constructor, cr[0, -1], node.Members) : expr);
        }

        // TypeBinaryExpression
        private Result RewriteTypeBinaryExpression(Expression expr, Stack stack)
        {
            TypeBinaryExpression node = (TypeBinaryExpression)expr;
            // The expression is emitted on top of current stack
            Result expression = RewriteExpression(node.Expression, stack);
            if (expression.Action != RewriteAction.None)
            {
                expr = new TypeBinaryExpression(expression.Node, node.TypeOperand, node.NodeType);
            }
            return new Result(expression.Action, expr);
        }

        // Throw
        private Result RewriteThrowUnaryExpression(Expression expr, Stack stack)
        {
            UnaryExpression node = (UnaryExpression)expr;

            // Throw statement itself does not care about the stack
            // but it will empty the stack and it may cause stack imbalance
            // it so we need to restore stack after unconditional throw to make JIT happy
            // this has an effect of executing Throw on an empty stack.

            Result value = RewriteExpressionFreeTemps(node.Operand, Stack.Empty);
            RewriteAction action = value.Action;

            if (stack != Stack.Empty)
            {
                action = RewriteAction.SpillStack;
            }

            if (action != RewriteAction.None)
            {
                expr = new UnaryExpression(ExpressionType.Throw, value.Node, node.Type, null);
            }

            return new Result(action, expr);
        }

        // UnaryExpression
        private Result RewriteUnaryExpression(Expression expr, Stack stack)
        {
            UnaryExpression node = (UnaryExpression)expr;

            Debug.Assert(node.NodeType != ExpressionType.Quote, "unexpected Quote");
            Debug.Assert(node.NodeType != ExpressionType.Throw, "unexpected Throw");

            // Operand is emitted on top of the stack as is
            Result expression = RewriteExpression(node.Operand, stack);

            if (expression.Action == RewriteAction.SpillStack)
            {
                RequireNoRefArgs(node.Method);
            }

            if (expression.Action != RewriteAction.None)
            {
                expr = new UnaryExpression(node.NodeType, expression.Node, node.Type, node.Method);
            }
            return new Result(expression.Action, expr);
        }

        // RewriteListInitExpression
        private Result RewriteListInitExpression(Expression expr, Stack stack)
        {
            ListInitExpression node = (ListInitExpression)expr;

            //ctor runs on initial stack
            Result newResult = RewriteExpression(node.NewExpression, stack);
            Expression rewrittenNew = newResult.Node;
            RewriteAction action = newResult.Action;

            ReadOnlyCollection<ElementInit> inits = node.Initializers;

            ChildRewriter[] cloneCrs = new ChildRewriter[inits.Count];

            for (int i = 0; i < inits.Count; i++)
            {
                ElementInit init = inits[i];

                //initializers all run on nonempty stack
                ChildRewriter cr = new ChildRewriter(this, Stack.NonEmpty, init.Arguments.Count);
                cr.Add(init.Arguments);

                action |= cr.Action;
                cloneCrs[i] = cr;
            }

            switch (action)
            {
                case RewriteAction.None:
                    break;
                case RewriteAction.Copy:
                    ElementInit[] newInits = new ElementInit[inits.Count];
                    for (int i = 0; i < inits.Count; i++)
                    {
                        ChildRewriter cr = cloneCrs[i];
                        if (cr.Action == RewriteAction.None)
                        {
                            newInits[i] = inits[i];
                        }
                        else
                        {
                            newInits[i] = new ElementInit(inits[i].AddMethod, new TrueReadOnlyCollection<Expression>(cr[0, -1]));
                        }
                    }
                    expr = new ListInitExpression((NewExpression)rewrittenNew, new TrueReadOnlyCollection<ElementInit>(newInits));
                    break;
                case RewriteAction.SpillStack:
                    RequireNotRefInstance(node.NewExpression);

                    ParameterExpression tempNew = MakeTemp(rewrittenNew.Type);
                    Expression[] comma = new Expression[inits.Count + 2];
                    comma[0] = new AssignBinaryExpression(tempNew, rewrittenNew);

                    for (int i = 0; i < inits.Count; i++)
                    {
                        ChildRewriter cr = cloneCrs[i];
                        Result add = cr.Finish(new InstanceMethodCallExpressionN(inits[i].AddMethod, tempNew, cr[0, -1]));
                        comma[i + 1] = add.Node;
                    }
                    comma[inits.Count + 1] = tempNew;
                    expr = MakeBlock(comma);
                    break;
                default:
                    throw ContractUtils.Unreachable;
            }

            return new Result(action, expr);
        }

        // RewriteMemberInitExpression
        private Result RewriteMemberInitExpression(Expression expr, Stack stack)
        {
            MemberInitExpression node = (MemberInitExpression)expr;

            //ctor runs on original stack
            Result result = RewriteExpression(node.NewExpression, stack);
            Expression rewrittenNew = result.Node;
            RewriteAction action = result.Action;

            ReadOnlyCollection<MemberBinding> bindings = node.Bindings;
            BindingRewriter[] bindingRewriters = new BindingRewriter[bindings.Count];
            for (int i = 0; i < bindings.Count; i++)
            {
                MemberBinding binding = bindings[i];
                //bindings run on nonempty stack
                BindingRewriter rewriter = BindingRewriter.Create(binding, this, Stack.NonEmpty);
                bindingRewriters[i] = rewriter;
                action |= rewriter.Action;
            }

            switch (action)
            {
                case RewriteAction.None:
                    break;
                case RewriteAction.Copy:
                    MemberBinding[] newBindings = new MemberBinding[bindings.Count];
                    for (int i = 0; i < bindings.Count; i++)
                    {
                        newBindings[i] = bindingRewriters[i].AsBinding();
                    }
                    expr = new MemberInitExpression((NewExpression)rewrittenNew, new TrueReadOnlyCollection<MemberBinding>(newBindings));
                    break;
                case RewriteAction.SpillStack:
                    RequireNotRefInstance(node.NewExpression);

                    ParameterExpression tempNew = MakeTemp(rewrittenNew.Type);
                    Expression[] comma = new Expression[bindings.Count + 2];
                    comma[0] = new AssignBinaryExpression(tempNew, rewrittenNew);
                    for (int i = 0; i < bindings.Count; i++)
                    {
                        BindingRewriter cr = bindingRewriters[i];
                        Expression initExpr = cr.AsExpression(tempNew);
                        comma[i + 1] = initExpr;
                    }
                    comma[bindings.Count + 1] = tempNew;
                    expr = MakeBlock(comma);
                    break;
                default:
                    throw ContractUtils.Unreachable;
            }
            return new Result(action, expr);
        }

        #endregion

        #region Statements

        // Block
        private Result RewriteBlockExpression(Expression expr, Stack stack)
        {
            BlockExpression node = (BlockExpression)expr;

            int count = node.ExpressionCount;
            RewriteAction action = RewriteAction.None;
            Expression[] clone = null;
            for (int i = 0; i < count; i++)
            {
                Expression expression = node.GetExpression(i);
                // All statements within the block execute at the
                // same stack state.
                Result rewritten = RewriteExpression(expression, stack);
                action |= rewritten.Action;

                if (clone == null && rewritten.Action != RewriteAction.None)
                {
                    clone = Clone(node.Expressions, i);
                }

                if (clone != null)
                {
                    clone[i] = rewritten.Node;
                }
            }

            if (action != RewriteAction.None)
            {
                // okay to wrap since we know no one can mutate the clone array
                expr = node.Rewrite(null, clone);
            }
            return new Result(action, expr);
        }

        // LabelExpression
        private Result RewriteLabelExpression(Expression expr, Stack stack)
        {
            LabelExpression node = (LabelExpression)expr;

            Result expression = RewriteExpression(node.DefaultValue, stack);
            if (expression.Action != RewriteAction.None)
            {
                expr = new LabelExpression(node.Target, expression.Node);
            }
            return new Result(expression.Action, expr);
        }

        // LoopStatement
        private Result RewriteLoopExpression(Expression expr, Stack stack)
        {
            LoopExpression node = (LoopExpression)expr;

            // The loop statement requires empty stack for itself, so it
            // can guarantee it to the child nodes.
            Result body = RewriteExpression(node.Body, Stack.Empty);

            RewriteAction action = body.Action;

            // However, the loop itself requires that it executes on an empty stack
            // so we need to rewrite if the stack is not empty.
            if (stack != Stack.Empty)
            {
                action = RewriteAction.SpillStack;
            }

            if (action != RewriteAction.None)
            {
                expr = new LoopExpression(body.Node, node.BreakLabel, node.ContinueLabel);
            }
            return new Result(action, expr);
        }

        // GotoExpression
        // Note: goto does not necessarily need an empty stack. We could always
        // emit it as a "leave" which would clear the stack for us. That would
        // prevent us from doing certain optimizations we might want to do,
        // however, like the switch-case-goto pattern. For now, be conservative
        private Result RewriteGotoExpression(Expression expr, Stack stack)
        {
            GotoExpression node = (GotoExpression)expr;

            // Goto requires empty stack to execute so the expression is
            // going to execute on an empty stack.
            Result value = RewriteExpressionFreeTemps(node.Value, Stack.Empty);

            // However, the statement itself needs an empty stack for itself
            // so if stack is not empty, rewrite to empty the stack.
            RewriteAction action = value.Action;
            if (stack != Stack.Empty)
            {
                action = RewriteAction.SpillStack;
            }

            if (action != RewriteAction.None)
            {
                expr = Expression.MakeGoto(node.Kind, node.Target, value.Node, node.Type);
            }
            return new Result(action, expr);
        }

        // SwitchStatement
        private Result RewriteSwitchExpression(Expression expr, Stack stack)
        {
            SwitchExpression node = (SwitchExpression)expr;

            // The switch statement test is emitted on the stack in current state
            Result switchValue = RewriteExpressionFreeTemps(node.SwitchValue, stack);

            RewriteAction action = switchValue.Action;
            ReadOnlyCollection<SwitchCase> cases = node.Cases;
            SwitchCase[] clone = null;
            for (int i = 0; i < cases.Count; i++)
            {
                SwitchCase @case = cases[i];

                Expression[] cloneTests = null;
                ReadOnlyCollection<Expression> testValues = @case.TestValues;
                for (int j = 0; j < testValues.Count; j++)
                {
                    // All tests execute at the same stack state as the switch.
                    // This is guaranteed by the compiler (to simplify spilling)
                    Result test = RewriteExpression(testValues[j], stack);
                    action |= test.Action;

                    if (cloneTests == null && test.Action != RewriteAction.None)
                    {
                        cloneTests = Clone(testValues, j);
                    }

                    if (cloneTests != null)
                    {
                        cloneTests[j] = test.Node;
                    }
                }

                // And all the cases also run on the same stack level.
                Result body = RewriteExpression(@case.Body, stack);
                action |= body.Action;

                if (body.Action != RewriteAction.None || cloneTests != null)
                {
                    if (cloneTests != null)
                    {
                        testValues = new ReadOnlyCollection<Expression>(cloneTests);
                    }
                    @case = new SwitchCase(body.Node, testValues);

                    if (clone == null)
                    {
                        clone = Clone(cases, i);
                    }
                }

                if (clone != null)
                {
                    clone[i] = @case;
                }
            }

            // default body also runs on initial stack
            Result defaultBody = RewriteExpression(node.DefaultBody, stack);
            action |= defaultBody.Action;

            if (action != RewriteAction.None)
            {
                if (clone != null)
                {
                    // okay to wrap because we aren't modifying the array
                    cases = new ReadOnlyCollection<SwitchCase>(clone);
                }

                expr = new SwitchExpression(node.Type, switchValue.Node, defaultBody.Node, node.Comparison, cases);
            }

            return new Result(action, expr);
        }

        // TryStatement
        private Result RewriteTryExpression(Expression expr, Stack stack)
        {
            TryExpression node = (TryExpression)expr;

            // Try statement definitely needs an empty stack so its
            // child nodes execute at empty stack.
            Result body = RewriteExpression(node.Body, Stack.Empty);
            ReadOnlyCollection<CatchBlock> handlers = node.Handlers;
            CatchBlock[] clone = null;

            RewriteAction action = body.Action;
            if (handlers != null)
            {
                for (int i = 0; i < handlers.Count; i++)
                {
                    RewriteAction curAction = body.Action;

                    CatchBlock handler = handlers[i];

                    Expression filter = handler.Filter;
                    if (handler.Filter != null)
                    {
                        // our code gen saves the incoming filter value and provides it as a variable so the stack is empty
                        Result rfault = RewriteExpression(handler.Filter, Stack.Empty);
                        action |= rfault.Action;
                        curAction |= rfault.Action;
                        filter = rfault.Node;
                    }

                    // Catch block starts with an empty stack (guaranteed by TryStatement)
                    Result rbody = RewriteExpression(handler.Body, Stack.Empty);
                    action |= rbody.Action;
                    curAction |= rbody.Action;

                    if (curAction != RewriteAction.None)
                    {
                        handler = Expression.MakeCatchBlock(handler.Test, handler.Variable, rbody.Node, filter);

                        if (clone == null)
                        {
                            clone = Clone(handlers, i);
                        }
                    }

                    if (clone != null)
                    {
                        clone[i] = handler;
                    }
                }
            }

            Result fault = RewriteExpression(node.Fault, Stack.Empty);
            action |= fault.Action;

            Result @finally = RewriteExpression(node.Finally, Stack.Empty);
            action |= @finally.Action;

            // If the stack is initially not empty, rewrite to spill the stack
            if (stack != Stack.Empty)
            {
                action = RewriteAction.SpillStack;
            }

            if (action != RewriteAction.None)
            {
                if (clone != null)
                {
                    // okay to wrap because we aren't modifying the array
                    handlers = new ReadOnlyCollection<CatchBlock>(clone);
                }

                expr = new TryExpression(node.Type, body.Node, @finally.Node, fault.Node, handlers);
            }
            return new Result(action, expr);
        }

        private Result RewriteExtensionExpression(Expression expr, Stack stack)
        {
            Result result = RewriteExpression(expr.ReduceExtensions(), stack);
            // it's at least Copy because we reduced the node
            return new Result(result.Action | RewriteAction.Copy, result.Node);
        }

        #endregion

        #region Cloning

        /// <summary>
        /// Will clone an IList into an array of the same size, and copy
        /// all values up to (and NOT including) the max index
        /// </summary>
        /// <returns>The cloned array.</returns>
        private static T[] Clone<T>(ReadOnlyCollection<T> original, int max)
        {
            Debug.Assert(original != null);
            Debug.Assert(max < original.Count);

            T[] clone = new T[original.Count];
            for (int j = 0; j < max; j++)
            {
                clone[j] = original[j];
            }
            return clone;
        }

        #endregion

        /// <summary>
        /// If we are spilling, requires that there are no byref arguments to
        /// the method call.
        /// 
        /// Used for:
        ///   NewExpression,
        ///   MethodCallExpression,
        ///   InvocationExpression,
        ///   DynamicExpression,
        ///   UnaryExpression,
        ///   BinaryExpression.
        /// </summary>
        /// <remarks>
        /// We could support this if spilling happened later in the compiler.
        /// Other expressions that can emit calls with arguments (such as
        /// ListInitExpression and IndexExpression) don't allow byref arguments.
        /// </remarks>
        private static void RequireNoRefArgs(MethodBase method)
        {
            if (method != null && method.GetParametersCached().Any(p => p.ParameterType.IsByRef))
            {
                throw Error.TryNotSupportedForMethodsWithRefArgs(method);
            }
        }

        /// <summary>
        /// Requires that the instance is not a value type (primitive types are
        /// okay because they're immutable).
        /// 
        /// Used for:
        ///  MethodCallExpression,
        ///  MemberExpression (for properties),
        ///  IndexExpression,
        ///  ListInitExpression,
        ///  MemberInitExpression,
        ///  assign to MemberExpression,
        ///  assign to IndexExpression.
        /// </summary>
        /// <remarks>
        /// We could support this if spilling happened later in the compiler.
        /// </remarks>
        private static void RequireNotRefInstance(Expression instance)
        {
            // Primitive value types are okay because they are all readonly,
            // but we can't rely on this for non-primitive types. So we throw
            // NotSupported.
            if (instance != null && instance.Type.GetTypeInfo().IsValueType && instance.Type.GetTypeCode() == TypeCode.Object)
            {
                throw Error.TryNotSupportedForValueTypeInstances(instance.Type);
            }
        }
    }
}
