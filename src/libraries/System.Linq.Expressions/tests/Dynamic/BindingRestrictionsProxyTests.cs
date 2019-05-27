// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Dynamic.Tests
{
    public class BindingRestrictionsProxyTests
    {
        private class BindingRestrictionsProxyProxy
        {
            // Proxy the proxy, to not have to make the same reflection calls repeatedly.

            private static PropertyInfo _isEmpty;
            private static PropertyInfo _test;
            private static PropertyInfo _restrictions;
            private static readonly MethodInfo ToStringMeth = typeof(object).GetMethod("ToString");

            private readonly object _proxy;

            public BindingRestrictionsProxyProxy(object proxy)
            {
                _proxy = proxy;
                if (_isEmpty == null)
                {
                    Type type = _proxy.GetType();
                    _isEmpty = type.GetProperty("IsEmpty");
                    _test = type.GetProperty("Test");
                    _restrictions = type.GetProperty("Restrictions");
                }
            }

            public bool IsEmpty => (bool)_isEmpty.GetValue(_proxy);

            public Expression Test => (Expression)_test.GetValue(_proxy);

            public BindingRestrictions[] Restrictions => (BindingRestrictions[])_restrictions.GetValue(_proxy);

            public override string ToString() => (string)ToStringMeth.Invoke(_proxy, new object[0]);
        }

        private static readonly Type BindingRestrictionsDebugViewType = GetDebugViewType(typeof(BindingRestrictions));
        private static readonly ConstructorInfo BindingRestrictionsProxyCtor =
            BindingRestrictionsDebugViewType?.GetConstructors().Single();

        private static Type GetDebugViewType(Type type)
        {
            var att =
                (DebuggerTypeProxyAttribute)
                    type.GetCustomAttributes().SingleOrDefault(at => at.TypeId.Equals(typeof(DebuggerTypeProxyAttribute)));
            if (att == null)
            {
                return null;
            }
            string proxyName = att.ProxyTypeName;
            proxyName = proxyName.Substring(0, proxyName.IndexOf(','));
            return type.GetTypeInfo().Assembly.GetType(proxyName);
        }

        private static BindingRestrictionsProxyProxy GetDebugViewObject(object obj)
            => new BindingRestrictionsProxyProxy(BindingRestrictionsProxyCtor.Invoke(new[] {obj}));

        [ConditionalFact]
        public void EmptyRestiction()
        {
            if (BindingRestrictionsDebugViewType == null)
            {
                throw new SkipTestException("Didn't find DebuggerTypeProxyAttribute on BindingRestrictions.");
            }
            BindingRestrictions empty = BindingRestrictions.Empty;
            BindingRestrictionsProxyProxy view = GetDebugViewObject(empty);
            Assert.True(view.IsEmpty);
            BindingRestrictions[] restrictions = view.Restrictions;
            Assert.Equal(1, restrictions.Length);
            Assert.Same(empty, restrictions[0]);
            Assert.Same(empty.ToExpression(), view.Test);
            Assert.Equal(empty.ToExpression().ToString(), view.ToString());
        }

        [ConditionalFact]
        public void CustomRestriction()
        {
            if (BindingRestrictionsDebugViewType == null)
            {
                throw new SkipTestException("Didn't find DebuggerTypeProxyAttribute on BindingRestrictions.");
            }
            ConstantExpression exp = Expression.Constant(false);
            BindingRestrictions custom = BindingRestrictions.GetExpressionRestriction(exp);
            BindingRestrictionsProxyProxy view = GetDebugViewObject(custom);
            Assert.False(view.IsEmpty);
            BindingRestrictions[] restrictions = view.Restrictions;
            Assert.Equal(1, restrictions.Length);
            Assert.Same(custom, restrictions[0]);
            Assert.NotSame(BindingRestrictions.Empty.ToExpression(), view.Test);
            Assert.Same(exp, view.Test);
            Assert.Equal(exp.ToString(), view.ToString());
        }

        [ConditionalFact]
        public void MergedRestrictionsProperties()
        {
            var exps = new Expression[]
            {
                Expression.Constant(false), Expression.Constant(true),
                Expression.Equal(Expression.Constant(2), Expression.Constant(3))
            };

            BindingRestrictions br = BindingRestrictions.Empty;
            var restrictions = new List<BindingRestrictions>();
            foreach (var exp in exps)
            {
                BindingRestrictions res = BindingRestrictions.GetExpressionRestriction(exp);
                restrictions.Add(res);
                br = br.Merge(res);
            }

            if (BindingRestrictionsDebugViewType == null)
            {
                throw new SkipTestException("Didn't find DebuggerTypeProxyAttribute on BindingRestrictions.");
            }

            BindingRestrictionsProxyProxy view = GetDebugViewObject(br);
            Assert.False(view.IsEmpty);

            Assert.Equal(br.ToExpression().ToString(), view.ToString());

            BindingRestrictions[] viewedRestrictions = view.Restrictions;

            // Check equal to source restrictions, but not insisting on order.
            Assert.Equal(3, viewedRestrictions.Length);
            Assert.True(viewedRestrictions.All(r => restrictions.Contains(r)));
        }

        [ConditionalFact]
        public void MergedRestrictionsExpressions()
        {
            var exps = new Expression[]
            {
                Expression.Constant(false), Expression.Constant(true),
                Expression.Equal(Expression.Constant(2), Expression.Constant(3))
            };

            BindingRestrictions br = BindingRestrictions.Empty;
            foreach (var exp in exps)
            {
                br = br.Merge(BindingRestrictions.GetExpressionRestriction(exp));
            }

            if (BindingRestrictionsDebugViewType == null)
            {
                throw new SkipTestException("Didn't find DebuggerTypeProxyAttribute on BindingRestrictions.");
            }

            BindingRestrictionsProxyProxy view = GetDebugViewObject(br);

            // The expression in the view will be a tree of AndAlso nodes.
            // If we examine the expression of the restriction a new AndAlso
            // will be created, so we strip out the leaf expressions and compare
            // with the initial set.

            var notAndAlso = new List<Expression>();
            Expression vExp = view.Test;
            Assert.Equal(ExpressionType.AndAlso, vExp.NodeType);

            Stack<Expression> toSplit = new Stack<Expression>();
            for (;;)
            {
                if (vExp.NodeType == ExpressionType.AndAlso)
                {
                    var bin = (BinaryExpression)vExp;
                    toSplit.Push(bin.Left);
                    vExp = bin.Right;
                }
                else
                {
                    notAndAlso.Add(vExp);
                    if (toSplit.Count == 0)
                    {
                        break;
                    }

                    vExp = toSplit.Pop();
                }
            }

            // Check equal to source expressions, but not insisting on order.
            Assert.Equal(3, notAndAlso.Count);
            Assert.True(notAndAlso.All(ex => exps.Contains(ex)));
        }

        [ConditionalFact]
        public void ThrowOnNullToCtor()
        {
            if (BindingRestrictionsDebugViewType == null)
            {
                throw new SkipTestException("Didn't find DebuggerTypeProxyAttribute on BindingRestrictions.");
            }
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => BindingRestrictionsProxyCtor.Invoke(new object[] {null}));
            ArgumentNullException ane = (ArgumentNullException)tie.InnerException;
            Assert.Equal("node", ane.ParamName);
        }
    }
}
