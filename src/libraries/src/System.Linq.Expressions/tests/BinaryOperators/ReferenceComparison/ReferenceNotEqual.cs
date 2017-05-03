// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ReferenceNotEqual : ReferenceEqualityTests
    {
        [Theory]
        [PerCompilationType(nameof(ReferenceObjectsData))]
        public void FalseOnSame(object item, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceNotEqual(
                    Expression.Constant(item, item.GetType()),
                    Expression.Constant(item, item.GetType())
                );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ReferenceTypesData))]
        public void FalseOnBothNull(Type type, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceNotEqual(
                Expression.Constant(null, type),
                Expression.Constant(null, type)
                );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ReferenceObjectsData))]
        public void TrueIfLeftNull(object item, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceNotEqual(
                    Expression.Constant(null, item.GetType()),
                    Expression.Constant(item, item.GetType())
                );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ReferenceObjectsData))]
        public void TrueIfRightNull(object item, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceNotEqual(
                    Expression.Constant(item, item.GetType()),
                    Expression.Constant(null, item.GetType())
                );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(DifferentObjects))]
        public void TrueIfDifferentObjectsAsObject(object x, object y, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceNotEqual(
                    Expression.Constant(x, typeof(object)),
                    Expression.Constant(y, typeof(object))
                );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(DifferentObjects))]
        public void TrueIfDifferentObjectsOwnType(object x, object y, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceNotEqual(
                    Expression.Constant(x),
                    Expression.Constant(y)
                );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [MemberData(nameof(LeftValueType))]
        [MemberData(nameof(RightValueType))]
        [MemberData(nameof(BothValueType))]
        public void ThrowsOnValueTypeArguments(object x, object y)
        {
            Expression xExp = Expression.Constant(x);
            Expression yExp = Expression.Constant(y);
            Assert.Throws<InvalidOperationException>(() => Expression.ReferenceNotEqual(xExp, yExp));
        }

        [Theory]
        [MemberData(nameof(UnassignablePairs))]
        public void ThrowsOnUnassignablePairs(object x, object y)
        {
            Expression xExp = Expression.Constant(x);
            Expression yExp = Expression.Constant(y);
            Assert.Throws<InvalidOperationException>(() => Expression.ReferenceNotEqual(xExp, yExp));
        }

        [Theory]
        [PerCompilationType(nameof(ComparableValuesData))]
        public void FalseOnSameViaInterface(object item, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceNotEqual(
                Expression.Constant(item, typeof(IComparable)),
                Expression.Constant(item, typeof(IComparable))
            );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(DifferentComparableValues))]
        public void TrueOnDifferentViaInterface(object x, object y, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceNotEqual(
                Expression.Constant(x, typeof(IComparable)),
                Expression.Constant(y, typeof(IComparable))
            );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ComparableReferenceTypesData))]
        public void FalseOnSameLeftViaInterface(object item, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceNotEqual(
                Expression.Constant(item, typeof(IComparable)),
                Expression.Constant(item)
            );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ComparableReferenceTypesData))]
        public void FalseOnSameRightViaInterface(object item, bool useInterpreter)
        {
            Expression exp = Expression.ReferenceNotEqual(
                Expression.Constant(item),
                Expression.Constant(item, typeof(IComparable))
            );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile(useInterpreter)());
        }

        [Fact]
        public void CannotReduce()
        {
            Expression exp = Expression.ReferenceNotEqual(Expression.Constant(""), Expression.Constant(""));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public void ThrowsOnLeftNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.ReferenceNotEqual(null, Expression.Constant("")));
        }

        [Fact]
        public void ThrowsOnRightNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.ReferenceNotEqual(Expression.Constant(""), null));
        }

        [Fact]
        public static void ThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<string>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.ReferenceNotEqual(value, Expression.Constant("")));
        }

        [Fact]
        public static void ThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<string>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.ReferenceNotEqual(Expression.Constant(""), value));
        }

        [Fact]
        public void Update()
        {
            Expression e1 = Expression.Constant("bar");
            Expression e2 = Expression.Constant("foo");
            Expression e3 = Expression.Constant("qux");

            BinaryExpression ne = Expression.ReferenceNotEqual(e1, e2);

            Assert.Same(ne, ne.Update(e1, null, e2));

            BinaryExpression ne1 = ne.Update(e1, null, e3);
            Assert.Equal(ExpressionType.NotEqual, ne1.NodeType);
            Assert.Same(e1, ne1.Left);
            Assert.Same(e3, ne1.Right);
            Assert.Null(ne1.Conversion);
            Assert.Null(ne1.Method);

            BinaryExpression ne2 = ne.Update(e3, null, e2);
            Assert.Equal(ExpressionType.NotEqual, ne2.NodeType);
            Assert.Same(e3, ne2.Left);
            Assert.Same(e2, ne2.Right);
            Assert.Null(ne2.Conversion);
            Assert.Null(ne2.Method);
        }
    }
}
