// Copyright (c) Jon Hanna. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ReferenceNotEqual : ReferenceEqualityTests
    {
        [Theory]
        [MemberData("ReferenceObjectsData")]
        public void FalseOnSame(object item)
        {
            Expression exp = Expression.ReferenceNotEqual(
                    Expression.Constant(item, item.GetType()),
                    Expression.Constant(item, item.GetType())
                );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile()());
        }

        [Theory]
        [MemberData("ReferenceTypesData")]
        public void FalseOnBothNull(Type type)
        {
            Expression exp = Expression.ReferenceNotEqual(
                Expression.Constant(null, type),
                Expression.Constant(null, type)
                );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile()());
        }

        [Theory]
        [MemberData("ReferenceObjectsData")]
        public void TrueIfLeftNull(object item)
        {
            Expression exp = Expression.ReferenceNotEqual(
                    Expression.Constant(null, item.GetType()),
                    Expression.Constant(item, item.GetType())
                );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile()());
        }

        [Theory]
        [MemberData("ReferenceObjectsData")]
        public void TrueIfRightNull(object item)
        {
            Expression exp = Expression.ReferenceNotEqual(
                    Expression.Constant(item, item.GetType()),
                    Expression.Constant(null, item.GetType())
                );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile()());
        }

        [Theory]
        [MemberData("DifferentObjects")]
        public void TrueIfDifferentObjectsAsObject(object x, object y)
        {
            Expression exp = Expression.ReferenceNotEqual(
                    Expression.Constant(x, typeof(object)),
                    Expression.Constant(y, typeof(object))
                );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile()());
        }

        [Theory]
        [MemberData("DifferentObjects")]
        public void TrueIfDifferentObjectsOwnType(object x, object y)
        {
            Expression exp = Expression.ReferenceNotEqual(
                    Expression.Constant(x),
                    Expression.Constant(y)
                );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile()());
        }

        [Theory]
        [MemberData("LeftValueType")]
        [MemberData("RightValueType")]
        [MemberData("BothValueType")]
        public void ThrowsOnValueTypeArguments(object x, object y)
        {
            Expression xExp = Expression.Constant(x);
            Expression yExp = Expression.Constant(y);
            Assert.Throws<InvalidOperationException>(() => Expression.ReferenceNotEqual(xExp, yExp));
        }

        [Theory]
        [MemberData("UnassignablePairs")]
        public void ThrowsOnUnassignablePairs(object x, object y)
        {
            Expression xExp = Expression.Constant(x);
            Expression yExp = Expression.Constant(y);
            Assert.Throws<InvalidOperationException>(() => Expression.ReferenceNotEqual(xExp, yExp));
        }

        [Theory]
        [MemberData("ComparableValuesData")]
        public void FalseOnSameViaInterface(object item)
        {
            Expression exp = Expression.ReferenceNotEqual(
                Expression.Constant(item, typeof(IComparable)),
                Expression.Constant(item, typeof(IComparable))
            );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile()());
        }

        [Theory]
        [MemberData("DifferentComparableValues")]
        public void TrueOnDifferentViaInterface(object x, object y)
        {
            Expression exp = Expression.ReferenceNotEqual(
                Expression.Constant(x, typeof(IComparable)),
                Expression.Constant(y, typeof(IComparable))
            );
            Assert.True(Expression.Lambda<Func<bool>>(exp).Compile()());
        }

        [Theory]
        [MemberData("ComparableReferenceTypesData")]
        public void FalseOnSameLeftViaInterface(object item)
        {
            Expression exp = Expression.ReferenceNotEqual(
                Expression.Constant(item, typeof(IComparable)),
                Expression.Constant(item)
            );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile()());
        }

        [Theory]
        [MemberData("ComparableReferenceTypesData")]
        public void FalseOnSameRightViaInterface(object item)
        {
            Expression exp = Expression.ReferenceNotEqual(
                Expression.Constant(item),
                Expression.Constant(item, typeof(IComparable))
            );
            Assert.False(Expression.Lambda<Func<bool>>(exp).Compile()());
        }

        [Fact]
        public void CannotReduce()
        {
            Expression exp = Expression.ReferenceNotEqual(Expression.Constant(""), Expression.Constant(""));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public void ThrowsOnLeftNull()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.ReferenceNotEqual(null, Expression.Constant("")));
        }

        [Fact]
        public void ThrowsOnRightNull()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.ReferenceNotEqual(Expression.Constant(""), null));
        }

        [Fact]
        public static void ThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<string>), "WriteOnly");
            Assert.Throws<ArgumentException>("left", () => Expression.ReferenceNotEqual(value, Expression.Constant("")));
        }

        [Fact]
        public static void ThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<string>), "WriteOnly");
            Assert.Throws<ArgumentException>("right", () => Expression.ReferenceNotEqual(Expression.Constant(""), value));
        }
    }
}
