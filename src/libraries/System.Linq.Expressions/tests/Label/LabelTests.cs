// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class LabelTests
    {
        // The actual use of label when compiling, interpreting or otherwise acting upon an expression
        // that makes use of them is by necessity covered by testing those GotoExpressions that make use of them.
        // These tests focus on the LabelTarget class and the factory methods producing them.

        [Fact]
        public void FactoryProducesUniqueLabels()
        {
            LabelTarget target = Expression.Label();
            Assert.NotSame(Expression.Label(target), Expression.Label(target));
            LabelTarget typedTarget = Expression.Label(typeof(int));
            ConstantExpression zero = Expression.Constant(0);
            Assert.NotSame(Expression.Label(target, zero), Expression.Label(target, zero));
        }

        [Fact]
        public void NullTarget()
        {
            AssertExtensions.Throws<ArgumentNullException>("target", () => Expression.Label(default(LabelTarget)));
            AssertExtensions.Throws<ArgumentNullException>("target", () => Expression.Label(null, Expression.Default(typeof(int))));
        }

        [Fact]
        public void NullDefaultValueAllowedWithVoidTarget()
        {
            Assert.Null(Expression.Label(Expression.Label(), null).DefaultValue);
            Assert.Null(Expression.Label(Expression.Label(typeof(void)), null).DefaultValue);
        }

        [Fact]
        public void NullDefaultValueNotAllowedWithTypedTarget()
        {
            AssertExtensions.Throws<ArgumentException>("target", () => Expression.Label(Expression.Label(typeof(int)), null));
        }

        [Fact]
        public void DefaultMustMatchLabelType()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Label(Expression.Label(typeof(int)), Expression.Constant("hello")));
        }

        [Fact]
        public void AssignableDefaultAllowed()
        {
            Assert.Equal(typeof(object), Expression.Label(Expression.Label(typeof(object)), Expression.Constant("hello")).Type);
        }

        [Fact]
        public void AssignableOnlyReferenceAssignableNotImplicitConversion()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Label(Expression.Label(typeof(long)), Expression.Constant(0)));
        }

        [Fact]
        public void AssignableDefaultByQuotingAllowed()
        {
            Expression<Func<int>> lambda = Expression.Lambda<Func<int>>(Expression.Constant(0));
            LabelExpression label = Expression.Label(Expression.Label(typeof(Expression<Func<int>>)), lambda);
            Assert.Equal(typeof(Expression<Func<int>>), label.Type);
            Assert.Equal(ExpressionType.Quote, label.DefaultValue.NodeType);
            Assert.Same(lambda, ((UnaryExpression)label.DefaultValue).Operand);
        }

        [Fact]
        public void LabelAndDefaultSameAsSourceParameters()
        {
            LabelTarget target = Expression.Label(typeof(int));
            ConstantExpression defaultVal = Expression.Constant(42);
            LabelExpression label = Expression.Label(target, defaultVal);
            Assert.Same(target, label.Target);
            Assert.Same(defaultVal, label.DefaultValue);
        }

        [Fact]
        public void UpdateWithSameTargetAndDefaultReturnsSameLabel()
        {
            LabelTarget target = Expression.Label(typeof(int));
            ConstantExpression defaultVal = Expression.Constant(42);
            LabelExpression label = Expression.Label(target, defaultVal);
            Assert.Same(label, label.Update(target, defaultVal));
            Assert.Same(label, NoOpVisitor.Instance.Visit(label));
        }

        [Fact]
        public void UpdateWithDifferentTargetOrDefaultReturnsNewLabel()
        {
            LabelTarget target = Expression.Label(typeof(int));
            ConstantExpression defaultVal = Expression.Constant(42);
            LabelExpression label = Expression.Label(target, defaultVal);
            Assert.NotSame(label, label.Update(Expression.Label(typeof(int)), defaultVal));
            Assert.NotSame(label, label.Update(target, Expression.Constant(42)));
        }

        [Fact]
        public void NodeTypeIsLabel()
        {
            Assert.Equal(ExpressionType.Label, Expression.Label(Expression.Label()).NodeType);
            Assert.Equal(ExpressionType.Label, Expression.Label(Expression.Label(typeof(int)), Expression.Constant(1)).NodeType);
        }
    }
}
