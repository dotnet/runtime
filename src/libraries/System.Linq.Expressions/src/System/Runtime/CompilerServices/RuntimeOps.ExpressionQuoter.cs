// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Linq.Expressions;
using System.Linq.Expressions.Compiler;
using static System.Linq.Expressions.CachedReflectionInfo;

namespace System.Runtime.CompilerServices
{
    public partial class RuntimeOps
    {
        /// <summary>
        /// Quotes the provided expression tree.
        /// </summary>
        /// <param name="expression">The expression to quote.</param>
        /// <param name="hoistedLocals">The hoisted local state provided by the compiler.</param>
        /// <param name="locals">The actual hoisted local values.</param>
        /// <returns>The quoted expression.</returns>
        [Obsolete("RuntimeOps has been deprecated and is not supported.", true), EditorBrowsable(EditorBrowsableState.Never)]
        [return: NotNullIfNotNull("expression")]
        public static Expression? Quote(Expression? expression, object hoistedLocals, object[] locals)
        {
            Debug.Assert(hoistedLocals != null && locals != null);
            var quoter = new ExpressionQuoter((HoistedLocals)hoistedLocals, locals);
            return quoter.Visit(expression);
        }

        /// <summary>
        /// Combines two runtime variable lists and returns a new list.
        /// </summary>
        /// <param name="first">The first list.</param>
        /// <param name="second">The second list.</param>
        /// <param name="indexes">The index array indicating which list to get variables from.</param>
        /// <returns>The merged runtime variables.</returns>
        [Obsolete("RuntimeOps has been deprecated and is not supported.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static IRuntimeVariables MergeRuntimeVariables(IRuntimeVariables first, IRuntimeVariables second, int[] indexes)
        {
            return new MergedRuntimeVariables(first, second, indexes);
        }

        // Modifies a quoted Expression instance by changing hoisted variables and
        // parameters into hoisted local references. The variable's StrongBox is
        // burned as a constant, and all hoisted variables/parameters are rewritten
        // as indexing expressions.
        //
        // The behavior of Quote is intended to be like C# and VB expression quoting
        private sealed class ExpressionQuoter : ExpressionVisitor
        {
            private readonly HoistedLocals _scope;
            private readonly object[] _locals;

            // A stack of variables that are defined in nested scopes. We search
            // this first when resolving a variable in case a nested scope shadows
            // one of our variable instances.
            private readonly Stack<HashSet<ParameterExpression>> _shadowedVars = new Stack<HashSet<ParameterExpression>>();

            internal ExpressionQuoter(HoistedLocals scope, object[] locals)
            {
                _scope = scope;
                _locals = locals;
            }

            protected internal override Expression VisitLambda<T>(Expression<T> node)
            {
                if (node.ParameterCount > 0)
                {
                    var parameters = new HashSet<ParameterExpression>();

                    for (int i = 0, n = node.ParameterCount; i < n; i++)
                    {
                        parameters.Add(node.GetParameter(i));
                    }

                    _shadowedVars.Push(parameters);
                }
                Expression b = Visit(node.Body);
                if (node.ParameterCount > 0)
                {
                    _shadowedVars.Pop();
                }
                if (b == node.Body)
                {
                    return node;
                }
                return node.Rewrite(b, parameters: null);
            }

            protected internal override Expression VisitBlock(BlockExpression node)
            {
                if (node.Variables.Count > 0)
                {
                    _shadowedVars.Push(new HashSet<ParameterExpression>(node.Variables));
                }
                Expression[]? b = ExpressionVisitorUtils.VisitBlockExpressions(this, node);
                if (node.Variables.Count > 0)
                {
                    _shadowedVars.Pop();
                }
                if (b == null)
                {
                    return node;
                }
                return node.Rewrite(node.Variables, b);
            }

            protected override CatchBlock VisitCatchBlock(CatchBlock node)
            {
                if (node.Variable != null)
                {
                    _shadowedVars.Push(new HashSet<ParameterExpression> { node.Variable });
                }
                Expression b = Visit(node.Body);
                Expression? f = Visit(node.Filter);
                if (node.Variable != null)
                {
                    _shadowedVars.Pop();
                }
                if (b == node.Body && f == node.Filter)
                {
                    return node;
                }
                return Expression.MakeCatchBlock(node.Test, node.Variable, b, f);
            }

            protected internal override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
            {
                int count = node.Variables.Count;
                var boxes = new List<IStrongBox>();
                var vars = new List<ParameterExpression>();
                var indexes = new int[count];
                for (int i = 0; i < indexes.Length; i++)
                {
                    IStrongBox? box = GetBox(node.Variables[i]);
                    if (box == null)
                    {
                        indexes[i] = vars.Count;
                        vars.Add(node.Variables[i]);
                    }
                    else
                    {
                        indexes[i] = -1 - boxes.Count;
                        boxes.Add(box);
                    }
                }

                // No variables were rewritten. Just return the original node
                if (boxes.Count == 0)
                {
                    return node;
                }

                ConstantExpression boxesConst = Expression.Constant(new RuntimeVariables(boxes.ToArray()), typeof(IRuntimeVariables));
                // All of them were rewritten. Just return the array as a constant
                if (vars.Count == 0)
                {
                    return boxesConst;
                }

                // Otherwise, we need to return an object that merges them
                return Expression.Call(
                    RuntimeOps_MergeRuntimeVariables,
                    Expression.RuntimeVariables(new TrueReadOnlyCollection<ParameterExpression>(vars.ToArray())),
                    boxesConst,
                    Expression.Constant(indexes)
                );
            }

            protected internal override Expression VisitParameter(ParameterExpression node)
            {
                IStrongBox? box = GetBox(node);
                if (box == null)
                {
                    return node;
                }
                return Utils.GetStrongBoxValueField(Expression.Constant(box));
            }

            private IStrongBox? GetBox(ParameterExpression variable)
            {
                // Skip variables that are shadowed by a nested scope/lambda
                foreach (HashSet<ParameterExpression> hidden in _shadowedVars)
                {
                    if (hidden.Contains(variable))
                    {
                        return null;
                    }
                }

                HoistedLocals? scope = _scope;
                object[] locals = _locals;
                while (true)
                {
                    int hoistIndex;
                    if (scope.Indexes.TryGetValue(variable, out hoistIndex))
                    {
                        return (IStrongBox)locals[hoistIndex];
                    }
                    scope = scope.Parent;
                    if (scope == null)
                    {
                        break;
                    }
                    locals = HoistedLocals.GetParent(locals);
                }

                // Unbound variable: an error should've been thrown already
                // from VariableBinder
                throw ContractUtils.Unreachable;
            }
        }
    }
}
