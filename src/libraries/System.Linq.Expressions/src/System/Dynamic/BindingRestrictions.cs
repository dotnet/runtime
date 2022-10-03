// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using AstUtils = System.Linq.Expressions.Utils;

namespace System.Dynamic
{
    /// <summary>
    /// Represents a set of binding restrictions on the <see cref="DynamicMetaObject"/> under which the dynamic binding is valid.
    /// </summary>
    [DebuggerTypeProxy(typeof(BindingRestrictionsProxy)), DebuggerDisplay("{DebugView}")]
    public abstract class BindingRestrictions
    {
        /// <summary>
        /// Represents an empty set of binding restrictions. This field is read-only.
        /// </summary>
        public static readonly BindingRestrictions Empty = new CustomRestriction(AstUtils.Constant(true));

        private const int TypeRestrictionHash =                    0b_0100_1001_0010_0100_1001_0010_0100_1001;
        private const int InstanceRestrictionHash = unchecked((int)0b_1001_0010_0100_1001_0010_0100_1001_0010);
        private const int CustomRestrictionHash =                  0b_0010_0100_1001_0010_0100_1001_0010_0100;

        private BindingRestrictions()
        {
        }

        // Overridden by specialized subclasses
        internal abstract Expression GetExpression();

        /// <summary>
        /// Merges the set of binding restrictions with the current binding restrictions.
        /// </summary>
        /// <param name="restrictions">The set of restrictions with which to merge the current binding restrictions.</param>
        /// <returns>The new set of binding restrictions.</returns>
        public BindingRestrictions Merge(BindingRestrictions restrictions)
        {
            ArgumentNullException.ThrowIfNull(restrictions);
            if (this == Empty)
            {
                return restrictions;
            }

            if (restrictions == Empty)
            {
                return this;
            }

            return new MergedRestriction(this, restrictions);
        }

        /// <summary>
        /// Creates the binding restriction that check the expression for runtime type identity.
        /// </summary>
        /// <param name="expression">The expression to test.</param>
        /// <param name="type">The exact type to test.</param>
        /// <returns>The new binding restrictions.</returns>
        public static BindingRestrictions GetTypeRestriction(Expression expression, Type type)
        {
            ArgumentNullException.ThrowIfNull(expression);
            ArgumentNullException.ThrowIfNull(type);

            return new TypeRestriction(expression, type);
        }

        /// <summary>
        /// The method takes a DynamicMetaObject, and returns an instance restriction for testing null if the object
        /// holds a null value, otherwise returns a type restriction.
        /// </summary>
        internal static BindingRestrictions GetTypeRestriction(DynamicMetaObject obj)
        {
            Debug.Assert(obj != null);
            if (obj.Value == null && obj.HasValue)
            {
                return GetInstanceRestriction(obj.Expression, null);
            }
            else
            {
                return GetTypeRestriction(obj.Expression, obj.LimitType);
            }
        }

        /// <summary>
        /// Creates the binding restriction that checks the expression for object instance identity.
        /// </summary>
        /// <param name="expression">The expression to test.</param>
        /// <param name="instance">The exact object instance to test.</param>
        /// <returns>The new binding restrictions.</returns>
        public static BindingRestrictions GetInstanceRestriction(Expression expression, object? instance)
        {
            ArgumentNullException.ThrowIfNull(expression);

            return new InstanceRestriction(expression, instance);
        }

        /// <summary>
        /// Creates the binding restriction that checks the expression for arbitrary immutable properties.
        /// </summary>
        /// <param name="expression">The expression expressing the restrictions.</param>
        /// <returns>The new binding restrictions.</returns>
        /// <remarks>
        /// By convention, the general restrictions created by this method must only test
        /// immutable object properties.
        /// </remarks>
        public static BindingRestrictions GetExpressionRestriction(Expression expression)
        {
            ArgumentNullException.ThrowIfNull(expression);
            ContractUtils.Requires(expression.Type == typeof(bool), nameof(expression));
            return new CustomRestriction(expression);
        }

        /// <summary>
        /// Combines binding restrictions from the list of <see cref="DynamicMetaObject"/> instances into one set of restrictions.
        /// </summary>
        /// <param name="contributingObjects">The list of <see cref="DynamicMetaObject"/> instances from which to combine restrictions.</param>
        /// <returns>The new set of binding restrictions.</returns>
        public static BindingRestrictions Combine(IList<DynamicMetaObject>? contributingObjects)
        {
            BindingRestrictions res = Empty;
            if (contributingObjects != null)
            {
                foreach (DynamicMetaObject mo in contributingObjects)
                {
                    if (mo != null)
                    {
                        res = res.Merge(mo.Restrictions);
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Builds a balanced tree of AndAlso nodes.
        /// We do this so the compiler won't stack overflow if we have many
        /// restrictions.
        /// </summary>
        private sealed class TestBuilder
        {
            private readonly HashSet<BindingRestrictions> _unique = new HashSet<BindingRestrictions>();
            private readonly Stack<AndNode> _tests = new Stack<AndNode>();

            private struct AndNode
            {
                internal int Depth;
                internal Expression Node;
            }

            internal void Append(BindingRestrictions restrictions)
            {
                if (_unique.Add(restrictions))
                {
                    Push(restrictions.GetExpression(), 0);
                }
            }

            internal Expression ToExpression()
            {
                Expression result = _tests.Pop().Node;
                while (_tests.Count > 0)
                {
                    result = Expression.AndAlso(_tests.Pop().Node, result);
                }
                return result;
            }

            private void Push(Expression node, int depth)
            {
                while (_tests.Count > 0 && _tests.Peek().Depth == depth)
                {
                    node = Expression.AndAlso(_tests.Pop().Node, node);
                    depth++;
                }
                _tests.Push(new AndNode { Node = node, Depth = depth });
            }
        }

        /// <summary>
        /// Creates the <see cref="Expression"/> representing the binding restrictions.
        /// </summary>
        /// <returns>The expression tree representing the restrictions.</returns>
        public Expression ToExpression() => GetExpression();

        private sealed class MergedRestriction : BindingRestrictions
        {
            internal readonly BindingRestrictions Left;
            internal readonly BindingRestrictions Right;

            internal MergedRestriction(BindingRestrictions left, BindingRestrictions right)
            {
                Left = left;
                Right = right;
            }

            internal override Expression GetExpression()
            {
                // We could optimize this better, e.g. common subexpression elimination
                // But for now, it's good enough.

                var testBuilder = new TestBuilder();

                // Visit the tree, left to right.
                // Use an explicit stack so we don't stack overflow.
                //
                // Left-most node is on top of the stack, so we always expand the
                // left most node each iteration.
                var stack = new Stack<BindingRestrictions>();
                BindingRestrictions top = this;
                while (true)
                {
                    if (top is MergedRestriction m)
                    {
                        stack.Push(m.Right);
                        top = m.Left;
                    }
                    else
                    {
                        testBuilder.Append(top);
                        if (stack.Count == 0)
                        {
                            return testBuilder.ToExpression();
                        }

                        top = stack.Pop();
                    }
                }
            }
        }

        private sealed class CustomRestriction : BindingRestrictions
        {
            private readonly Expression _expression;

            internal CustomRestriction(Expression expression)
            {
                Debug.Assert(expression != null);
                _expression = expression;
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                return obj is CustomRestriction other && other._expression == _expression;
            }

            public override int GetHashCode() => CustomRestrictionHash ^ _expression.GetHashCode();

            internal override Expression GetExpression() => _expression;
        }

        private sealed class TypeRestriction : BindingRestrictions
        {
            private readonly Expression _expression;
            private readonly Type _type;

            internal TypeRestriction(Expression parameter, Type type)
            {
                Debug.Assert(parameter != null);
                Debug.Assert(type != null);
                _expression = parameter;
                _type = type;
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                return obj is TypeRestriction other && other._expression == _expression && TypeUtils.AreEquivalent(other._type, _type);
            }

            public override int GetHashCode() => TypeRestrictionHash ^ _expression.GetHashCode() ^ _type.GetHashCode();

            internal override Expression GetExpression() => Expression.TypeEqual(_expression, _type);
        }

        private sealed class InstanceRestriction : BindingRestrictions
        {
            private readonly Expression _expression;
            private readonly object? _instance;

            internal InstanceRestriction(Expression parameter, object? instance)
            {
                Debug.Assert(parameter != null);
                _expression = parameter;
                _instance = instance;
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                return obj is InstanceRestriction other && other._expression == _expression && other._instance == _instance;
            }

            public override int GetHashCode()
                => InstanceRestrictionHash ^ RuntimeHelpers.GetHashCode(_instance!) ^ _expression.GetHashCode();

            internal override Expression GetExpression()
            {
                if (_instance == null)
                {
                    return Expression.Equal(
                        Expression.Convert(_expression, typeof(object)),
                        AstUtils.Null
                    );
                }

                ParameterExpression temp = Expression.Parameter(typeof(object), null);
                return Expression.Block(
                    new TrueReadOnlyCollection<ParameterExpression>(temp),
                    new TrueReadOnlyCollection<Expression>(
#if ENABLEDYNAMICPROGRAMMING
                        Expression.Assign(
                            temp,
                            Expression.Property(
                                Expression.Constant(new WeakReference(_instance)),
                                typeof(WeakReference).GetProperty("Target")
                            )
                        ),
#else
                        Expression.Assign(
                            temp,
                            Expression.Constant(_instance, typeof(object))
                        ),
#endif
                        Expression.AndAlso(
                            //check that WeakReference was not collected.
                            Expression.NotEqual(temp, AstUtils.Null),
                            Expression.Equal(
                                Expression.Convert(_expression, typeof(object)),
                                temp
                            )
                        )
                    )
                );
            }
        }

        private string DebugView => ToExpression().ToString();

        private sealed class BindingRestrictionsProxy
        {
            private readonly BindingRestrictions _node;

            public BindingRestrictionsProxy(BindingRestrictions node)
            {
                ArgumentNullException.ThrowIfNull(node);
                _node = node;
            }

            public bool IsEmpty => _node == Empty;

            public Expression Test => _node.ToExpression();

            public BindingRestrictions[] Restrictions
            {
                get
                {
                    var restrictions = new List<BindingRestrictions>();

                    // Visit the tree, left to right
                    //
                    // Left-most node is on top of the stack, so we always expand the
                    // left most node each iteration.
                    var stack = new Stack<BindingRestrictions>();
                    BindingRestrictions top = _node;
                    while (true)
                    {
                        if (top is MergedRestriction m)
                        {
                            stack.Push(m.Right);
                            top = m.Left;
                        }
                        else
                        {
                            restrictions.Add(top);
                            if (stack.Count == 0)
                            {
                                return restrictions.ToArray();
                            }

                            top = stack.Pop();
                        }
                    }
                }
            }

            // To prevent fxcop warning about this field
            public override string ToString() => _node.DebugView;
        }
    }
}
