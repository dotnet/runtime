// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Compiler
{
    internal partial class StackSpiller
    {
        private sealed class TempMaker
        {
            /// <summary>
            /// Current temporary variable
            /// </summary>
            private int _temp;

            /// <summary>
            /// List of free temporary variables. These can be recycled for new temps.
            /// </summary>
            private List<ParameterExpression> _freeTemps;

            /// <summary>
            /// Stack of currently active temporary variables.
            /// </summary>
            private Stack<ParameterExpression> _usedTemps;

            /// <summary>
            /// List of all temps created by stackspiller for this rule/lambda
            /// </summary>
            private List<ParameterExpression> _temps = new List<ParameterExpression>();

            internal List<ParameterExpression> Temps => _temps;

            internal ParameterExpression Temp(Type type)
            {
                ParameterExpression temp;
                if (_freeTemps != null)
                {
                    // Recycle from the free-list if possible.
                    for (int i = _freeTemps.Count - 1; i >= 0; i--)
                    {
                        temp = _freeTemps[i];
                        if (temp.Type == type)
                        {
                            _freeTemps.RemoveAt(i);
                            return UseTemp(temp);
                        }
                    }
                }
                // Not on the free-list, create a brand new one.
                temp = ParameterExpression.Make(type, "$temp$" + _temp++, isByRef: false);
                _temps.Add(temp);
                return UseTemp(temp);
            }

            private ParameterExpression UseTemp(ParameterExpression temp)
            {
                Debug.Assert(_freeTemps == null || !_freeTemps.Contains(temp));
                Debug.Assert(_usedTemps == null || !_usedTemps.Contains(temp));

                if (_usedTemps == null)
                {
                    _usedTemps = new Stack<ParameterExpression>();
                }
                _usedTemps.Push(temp);
                return temp;
            }

            private void FreeTemp(ParameterExpression temp)
            {
                Debug.Assert(_freeTemps == null || !_freeTemps.Contains(temp));
                if (_freeTemps == null)
                {
                    _freeTemps = new List<ParameterExpression>();
                }
                _freeTemps.Add(temp);
            }

            internal int Mark()
            {
                return _usedTemps != null ? _usedTemps.Count : 0;
            }

            // Free temporaries created since the last marking. 
            // This is a performance optimization to lower the overall number of temporaries needed.
            internal void Free(int mark)
            {
                // (_usedTemps != null) ==> (mark <= _usedTemps.Count)
                Debug.Assert(_usedTemps == null || mark <= _usedTemps.Count);
                // (_usedTemps == null) ==> (mark == 0)
                Debug.Assert(mark == 0 || _usedTemps != null);

                if (_usedTemps != null)
                {
                    while (mark < _usedTemps.Count)
                    {
                        FreeTemp(_usedTemps.Pop());
                    }
                }
            }

            [Conditional("DEBUG")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
            internal void VerifyTemps()
            {
                Debug.Assert(_usedTemps == null || _usedTemps.Count == 0);
            }
        }


        /// <summary>
        /// Rewrites child expressions, spilling them into temps if needed. The
        /// stack starts in the initial state, and after the first subexpression
        /// is added it is changed to non-empty. This behavior can be overridden
        /// by setting the stack manually between adds.
        /// 
        /// When all children have been added, the caller should rewrite the 
        /// node if Rewrite is true. Then, it should call Finish with either
        /// the original expression or the rewritten expression. Finish will call
        /// Expression.Comma if necessary and return a new Result.
        /// </summary>
        private sealed class ChildRewriter
        {
            private readonly StackSpiller _self;
            private readonly Expression[] _expressions;
            private int _expressionsCount;
            private int _lastSpillIndex;
            private List<Expression> _comma;
            private RewriteAction _action;
            private Stack _stack;
            private bool _done;

            internal ChildRewriter(StackSpiller self, Stack stack, int count)
            {
                _self = self;
                _stack = stack;
                _expressions = new Expression[count];
            }

            internal void Add(Expression node)
            {
                Debug.Assert(!_done);

                if (node == null)
                {
                    _expressions[_expressionsCount++] = null;
                    return;
                }

                Result exp = _self.RewriteExpression(node, _stack);
                _action |= exp.Action;
                _stack = Stack.NonEmpty;

                if (exp.Action == RewriteAction.SpillStack)
                {
                    _lastSpillIndex = _expressionsCount;
                }

                // track items in case we need to copy or spill stack
                _expressions[_expressionsCount++] = exp.Node;
            }

            internal void Add(ReadOnlyCollection<Expression> expressions)
            {
                for (int i = 0, count = expressions.Count; i < count; i++)
                {
                    Add(expressions[i]);
                }
            }

            internal void AddArguments(IArgumentProvider expressions)
            {
                for (int i = 0, count = expressions.ArgumentCount; i < count; i++)
                {
                    Add(expressions.GetArgument(i));
                }
            }

            private void EnsureDone()
            {
                // done adding arguments, build the comma if necessary
                if (!_done)
                {
                    _done = true;

                    if (_action == RewriteAction.SpillStack)
                    {
                        Expression[] clone = _expressions;
                        int count = _lastSpillIndex + 1;
                        List<Expression> comma = new List<Expression>(count + 1);
                        for (int i = 0; i < count; i++)
                        {
                            Expression current = clone[i];
                            if (ShouldSaveToTemp(current))
                            {
                                Expression temp;
                                clone[i] = _self.ToTemp(current, out temp);
                                comma.Add(temp);
                            }
                        }
                        comma.Capacity = comma.Count + 1;
                        _comma = comma;
                    }
                }
            }

            private static bool ShouldSaveToTemp(Expression expression)
            {
                if (expression == null)
                    return false;

                // Some expressions have no side-effects and don't have to be
                // stored into temporaries, e.g.
                //
                //     xs[0] = try { ... }
                //           |
                //           v
                //        t0 = xs
                //        t1 = 0            // <-- this is redundant
                //        t2 = try { ... }
                //    t0[t1] = t2
                //           |
                //           v
                //        t0 = xs
                //        t1 = try { ... }
                //     t0[0] = t1

                switch (expression.NodeType)
                {
                    // Emits ldnull, ldc, initobj, closure constant access, etc.
                    case ExpressionType.Constant:
                    case ExpressionType.Default:
                        return false;

                    // Emits calls to pure RuntimeOps methods with immutable arguments
                    case ExpressionType.RuntimeVariables:
                        return false;

                    case ExpressionType.MemberAccess:
                        var member = (MemberExpression)expression;
                        var field = member.Member as FieldInfo;
                        if (field != null)
                        {
                            // Emits ldc for the raw value of the field
                            if (field.IsLiteral)
                                return false;

                            // For read-only fields we could save the receiver, but
                            // that's more involved, so we'll just handle static fields
                            if (field.IsInitOnly && field.IsStatic)
                                return false;
                        }
                        break;
                }

                // NB: We omit Lambda because it may interfere with the Invoke/Lambda
                //     inlining optimizations. Parameter is out too because we don't
                //     have any sophisticated load/store analysis.

                // NB: We omit Quote because the emitted call to RuntimeOps.Quote will
                //     trigger reduction of extension nodes which can cause the timing
                //     of exceptions thrown from Reduce methods to change.

                return true;
            }

            internal bool Rewrite => _action != RewriteAction.None;

            internal RewriteAction Action => _action;

            internal Result Finish(Expression expr)
            {
                EnsureDone();

                if (_action == RewriteAction.SpillStack)
                {
                    Debug.Assert(_comma.Capacity == _comma.Count + 1);
                    _comma.Add(expr);
                    expr = MakeBlock(_comma);
                }

                return new Result(_action, expr);
            }

            internal Expression this[int index]
            {
                get
                {
                    EnsureDone();
                    if (index < 0)
                    {
                        index += _expressions.Length;
                    }
                    return _expressions[index];
                }
            }

            internal Expression[] this[int first, int last]
            {
                get
                {
                    EnsureDone();
                    if (last < 0)
                    {
                        last += _expressions.Length;
                    }
                    int count = last - first + 1;
                    ContractUtils.RequiresArrayRange(_expressions, first, count, nameof(first), nameof(last));

                    if (count == _expressions.Length)
                    {
                        Debug.Assert(first == 0);
                        // if the entire array is requested just return it so we don't make a new array
                        return _expressions;
                    }

                    Expression[] clone = new Expression[count];
                    Array.Copy(_expressions, first, clone, 0, count);
                    return clone;
                }
            }
        }


        private ParameterExpression MakeTemp(Type type)
        {
            return _tm.Temp(type);
        }

        private int Mark()
        {
            return _tm.Mark();
        }

        private void Free(int mark)
        {
            _tm.Free(mark);
        }

        [Conditional("DEBUG")]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private void VerifyTemps()
        {
            _tm.VerifyTemps();
        }

        /// <summary>
        /// Will perform:
        ///     save: temp = expression
        ///     return value: temp
        /// </summary>
        private ParameterExpression ToTemp(Expression expression, out Expression save)
        {
            ParameterExpression temp = MakeTemp(expression.Type);
            save = Expression.Assign(temp, expression);
            return temp;
        }

        /// <summary>
        /// Creates a special block that is marked as not allowing jumps in.
        /// This should not be used for rewriting BlockExpression itself, or
        /// anything else that supports jumping.
        /// </summary>
        private static Expression MakeBlock(params Expression[] expressions)
        {
            return MakeBlock((IReadOnlyList<Expression>)expressions);
        }

        /// <summary>
        /// Creates a special block that is marked as not allowing jumps in.
        /// This should not be used for rewriting BlockExpression itself, or
        /// anything else that supports jumping.
        /// </summary>
        private static Expression MakeBlock(IReadOnlyList<Expression> expressions)
        {
            return new SpilledExpressionBlock(expressions);
        }
    }

    /// <summary>
    /// A special subtype of BlockExpression that indicates to the compiler
    /// that this block is a spilled expression and should not allow jumps in.
    /// </summary>
    internal sealed class SpilledExpressionBlock : BlockN
    {
        internal SpilledExpressionBlock(IReadOnlyList<Expression> expressions)
            : base(expressions)
        {
        }

        [ExcludeFromCodeCoverage] // Unreachable
        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            throw ContractUtils.Unreachable;
        }
    }
}
