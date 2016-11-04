// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace System.Dynamic
{
    /// <summary>
    /// Represents a set of binding restrictions on the <see cref="DynamicMetaObject"/>under which the dynamic binding is valid.
    /// </summary>
    [DebuggerTypeProxy(typeof(BindingRestrictionsProxy)), DebuggerDisplay("{DebugView}")]
    public abstract class BindingRestrictions
    {
        /// <summary>
        /// Represents an empty set of binding restrictions. This field is read only.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly BindingRestrictions Empty = new CustomRestriction(Expression.Constant(true));

        private const int TypeRestrictionHash = 0x10000000;
        private const int InstanceRestrictionHash = 0x20000000;
        private const int CustomRestrictionHash = 0x40000000;

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
            ContractUtils.RequiresNotNull(restrictions, nameof(restrictions));
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
            ContractUtils.RequiresNotNull(expression, nameof(expression));
            ContractUtils.RequiresNotNull(type, nameof(type));

            return new TypeRestriction(expression, type);
        }

        /// <summary>
        /// The method takes a DynamicMetaObject, and returns an instance restriction for testing null if the object
        /// holds a null value, otherwise returns a type restriction.
        /// </summary>
        internal static BindingRestrictions GetTypeRestriction(DynamicMetaObject obj)
        {
            if (obj.Value == null && obj.HasValue)
            {
                return BindingRestrictions.GetInstanceRestriction(obj.Expression, null);
            }
            else
            {
                return BindingRestrictions.GetTypeRestriction(obj.Expression, obj.LimitType);
            }
        }

        /// <summary>
        /// Creates the binding restriction that checks the expression for object instance identity.
        /// </summary>
        /// <param name="expression">The expression to test.</param>
        /// <param name="instance">The exact object instance to test.</param>
        /// <returns>The new binding restrictions.</returns>
        public static BindingRestrictions GetInstanceRestriction(Expression expression, object instance)
        {
            ContractUtils.RequiresNotNull(expression, nameof(expression));

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
            ContractUtils.RequiresNotNull(expression, nameof(expression));
            ContractUtils.Requires(expression.Type == typeof(bool), nameof(expression));
            return new CustomRestriction(expression);
        }

        /// <summary>
        /// Combines binding restrictions from the list of <see cref="DynamicMetaObject"/> instances into one set of restrictions.
        /// </summary>
        /// <param name="contributingObjects">The list of <see cref="DynamicMetaObject"/> instances from which to combine restrictions.</param>
        /// <returns>The new set of binding restrictions.</returns>
        public static BindingRestrictions Combine(IList<DynamicMetaObject> contributingObjects)
        {
            BindingRestrictions res = BindingRestrictions.Empty;
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
                if (_unique.Contains(restrictions))
                {
                    return;
                }
                _unique.Add(restrictions);

                Push(restrictions.GetExpression(), 0);
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
        public Expression ToExpression()
        {
            // We could optimize this better, e.g. common subexpression elimination
            // But for now, it's good enough.

            if (this == Empty)
            {
                return Expression.Constant(true);
            }

            var testBuilder = new TestBuilder();

            // Visit the tree, left to right.
            // Use an explicit stack so we don't stack overflow.
            //
            // Left-most node is on top of the stack, so we always expand the
            // left most node each iteration.
            var stack = new Stack<BindingRestrictions>();
            stack.Push(this);
            do
            {
                var top = stack.Pop();
                var m = top as MergedRestriction;
                if (m != null)
                {
                    stack.Push(m.Right);
                    stack.Push(m.Left);
                }
                else
                {
                    testBuilder.Append(top);
                }
            } while (stack.Count > 0);

            return testBuilder.ToExpression();
        }

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
                throw ContractUtils.Unreachable;
            }
        }

        private sealed class CustomRestriction : BindingRestrictions
        {
            private readonly Expression _expression;

            internal CustomRestriction(Expression expression)
            {
                _expression = expression;
            }

            public override bool Equals(object obj)
            {
                var other = obj as CustomRestriction;
                return other != null && other._expression == _expression;
            }

            public override int GetHashCode()
            {
                return CustomRestrictionHash ^ _expression.GetHashCode();
            }

            internal override Expression GetExpression()
            {
                return _expression;
            }
        }

        private sealed class TypeRestriction : BindingRestrictions
        {
            private readonly Expression _expression;
            private readonly Type _type;

            internal TypeRestriction(Expression parameter, Type type)
            {
                _expression = parameter;
                _type = type;
            }

            public override bool Equals(object obj)
            {
                var other = obj as TypeRestriction;
                return other != null && TypeUtils.AreEquivalent(other._type, _type) && other._expression == _expression;
            }

            public override int GetHashCode()
            {
                return TypeRestrictionHash ^ _expression.GetHashCode() ^ _type.GetHashCode();
            }

            internal override Expression GetExpression()
            {
                return Expression.TypeEqual(_expression, _type);
            }
        }

        private sealed class InstanceRestriction : BindingRestrictions
        {
            private readonly Expression _expression;
            private readonly object _instance;

            internal InstanceRestriction(Expression parameter, object instance)
            {
                _expression = parameter;
                _instance = instance;
            }

            public override bool Equals(object obj)
            {
                var other = obj as InstanceRestriction;
                return other != null && other._instance == _instance && other._expression == _expression;
            }

            public override int GetHashCode()
            {
                return InstanceRestrictionHash ^ RuntimeHelpers.GetHashCode(_instance) ^ _expression.GetHashCode();
            }

            internal override Expression GetExpression()
            {
                if (_instance == null)
                {
                    return Expression.Equal(
                        Expression.Convert(_expression, typeof(object)),
                        Expression.Constant(null)
                    );
                }

                ParameterExpression temp = Expression.Parameter(typeof(object), null);
                return Expression.Block(
                    new[] { temp },
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
                        //check that WeekReference was not collected.
                        Expression.NotEqual(temp, Expression.Constant(null)),
                        Expression.Equal(
                            Expression.Convert(_expression, typeof(object)),
                            temp
                        )
                    )
                );
            }
        }

        private string DebugView
        {
            get { return ToExpression().ToString(); }
        }

        private sealed class BindingRestrictionsProxy
        {
            private readonly BindingRestrictions _node;

            public BindingRestrictionsProxy(BindingRestrictions node)
            {
                _node = node;
            }

            public bool IsEmpty
            {
                get { return _node == Empty; }
            }

            public Expression Test
            {
                get { return _node.ToExpression(); }
            }

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
                    stack.Push(_node);
                    do
                    {
                        var top = stack.Pop();
                        var m = top as MergedRestriction;
                        if (m != null)
                        {
                            stack.Push(m.Right);
                            stack.Push(m.Left);
                        }
                        else
                        {
                            restrictions.Add(top);
                        }
                    } while (stack.Count > 0);

                    return restrictions.ToArray();
                }
            }

            public override string ToString()
            {
                // To prevent fxcop warning about this field
                return _node.DebugView;
            }
        }
    }
}
