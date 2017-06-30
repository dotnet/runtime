// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ReferenceEqual : ReferenceEqualityTests
    {
        [Theory]
        [PerCompilationType(nameof(ReferenceObjectsData))]
        public void TrueOnSame(object item, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceEqual(
                    Expression.Constant(item, item.GetType()),
                    Expression.Constant(item, item.GetType())
                );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ReferenceTypesData))]
        public void TrueOnBothNull(Type type, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceEqual(
                Expression.Constant(null, type),
                Expression.Constant(null, type)
                );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ReferenceObjectsData))]
        public void FalseIfLeftNull(object item, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceEqual(
                    Expression.Constant(null, item.GetType()),
                    Expression.Constant(item, item.GetType())
                );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ReferenceObjectsData))]
        public void FalseIfRightNull(object item, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceEqual(
                    Expression.Constant(item, item.GetType()),
                    Expression.Constant(null, item.GetType())
                );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(DifferentObjects))]
        public void FalseIfDifferentObjectsAsObject(object x, object y, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceEqual(
                    Expression.Constant(x, typeof(object)),
                    Expression.Constant(y, typeof(object))
                );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(DifferentObjects))]
        public void FalseIfDifferentObjectsOwnType(object x, object y, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceEqual(
                    Expression.Constant(x),
                    Expression.Constant(y)
                );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [MemberData(nameof(LeftValueType))]
        [MemberData(nameof(RightValueType))]
        [MemberData(nameof(BothValueType))]
        public void ThrowsOnValueTypeArguments(object x, object y)
        {
            Expression xExp = Expression.Constant(x);
            Expression yExp = Expression.Constant(y);
            Assert.Throws<InvalidOperationException>(() => Expression.ReferenceEqual(xExp, yExp));
        }

        [Theory]
        [MemberData(nameof(UnassignablePairs))]
        public void ThrowsOnUnassignablePairs(object x, object y)
        {
            Expression xExp = Expression.Constant(x);
            Expression yExp = Expression.Constant(y);
            Assert.Throws<InvalidOperationException>(() => Expression.ReferenceEqual(xExp, yExp));
        }

        [Theory]
        [PerCompilationType(nameof(ComparableValuesData))]
        public void TrueOnSameViaInterface(object item, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceEqual(
                Expression.Constant(item, typeof(IComparable)),
                Expression.Constant(item, typeof(IComparable))
            );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(DifferentComparableValues))]
        public void FalseOnDifferentViaInterface(object x, object y, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceEqual(
                Expression.Constant(x, typeof(IComparable)),
                Expression.Constant(y, typeof(IComparable))
            );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ComparableReferenceTypesData))]
        public void TrueOnSameLeftViaInterface(object item, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceEqual(
                Expression.Constant(item, typeof(IComparable)),
                Expression.Constant(item)
            );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ComparableReferenceTypesData))]
        public void TrueOnSameRightViaInterface(object item, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceEqual(
                Expression.Constant(item),
                Expression.Constant(item, typeof(IComparable))
            );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Fact]
        public void CannotReduce()
        {
            Expression exp = Expression.ReferenceEqual(Expression.Constant(""), Expression.Constant(""));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public void ThrowsOnLeftNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.ReferenceEqual(null, Expression.Constant("")));
        }

        [Fact]
        public void ThrowsOnRightNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.ReferenceEqual(Expression.Constant(""), null));
        }

        [Fact]
        public static void ThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<string>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.ReferenceEqual(value, Expression.Constant("")));
        }

        [Fact]
        public static void ThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<string>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.ReferenceEqual(Expression.Constant(""), value));
        }

        [Fact]
        public void Update()
        {
            Expression e1 = Expression.Constant("bar");
            Expression e2 = Expression.Constant("foo");
            Expression e3 = Expression.Constant("qux");

            BinaryExpression eq = Expression.ReferenceEqual(e1, e2);

            Assert.Same(eq, eq.Update(e1, null, e2));

            BinaryExpression eq1 = eq.Update(e1, null, e3);
            Assert.Equal(ExpressionType.Equal, eq1.NodeType);
            Assert.Same(e1, eq1.Left);
            Assert.Same(e3, eq1.Right);
            Assert.Null(eq1.Conversion);
            Assert.Null(eq1.Method);

            BinaryExpression eq2 = eq.Update(e3, null, e2);
            Assert.Equal(ExpressionType.Equal, eq2.NodeType);
            Assert.Same(e3, eq2.Left);
            Assert.Same(e2, eq2.Right);
            Assert.Null(eq2.Conversion);
            Assert.Null(eq2.Method);
        }
    }
}
