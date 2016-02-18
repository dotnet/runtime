// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class UnboxTests
    {
        [Theory]
        [MemberData(nameof(UnboxableFromObject))]
        [MemberData(nameof(NullableUnboxableFromObject))]
        [MemberData(nameof(UnboxableFromIComparable))]
        [MemberData(nameof(NullableUnboxableFromIComparable))]
        [MemberData(nameof(UnboxableFromIComparableT))]
        [MemberData(nameof(NullableUnboxableFromIComparableT))]
        public void CanUnbox(object value, Type type, Type boxedType)
        {
            Expression expression = Expression.Constant(value, boxedType);
            UnaryExpression unbox = Expression.Unbox(expression, type);
            Assert.Equal(type, unbox.Type);
            BinaryExpression isEqual = Expression.Equal(Expression.Constant(value, type), unbox);
            Assert.True(Expression.Lambda<Func<bool>>(isEqual).Compile()());
        }

        [Theory]
        [MemberData(nameof(UnboxableFromObject))]
        [MemberData(nameof(NullableUnboxableFromObject))]
        [MemberData(nameof(UnboxableFromIComparable))]
        [MemberData(nameof(NullableUnboxableFromIComparable))]
        [MemberData(nameof(UnboxableFromIComparableT))]
        [MemberData(nameof(NullableUnboxableFromIComparableT))]
        public void CanUnboxFromMake(object value, Type type, Type boxedType)
        {
            Expression expression = Expression.Constant(value, boxedType);
            UnaryExpression unbox = Expression.MakeUnary(ExpressionType.Unbox, expression, type);
            Assert.Equal(type, unbox.Type);
            BinaryExpression isEqual = Expression.Equal(Expression.Constant(value, type), unbox);
            Assert.True(Expression.Lambda<Func<bool>>(isEqual).Compile()());
        }

        public static IEnumerable<object[]> UnboxableFromObject()
        {
            yield return new object[] { 1, typeof(int), typeof(object) };
            yield return new object[] { 42, typeof(int), typeof(object) };
            yield return new object[] { DateTime.MinValue, typeof(DateTime), typeof(object) };
            yield return new object[] { DateTimeOffset.MinValue, typeof(DateTimeOffset), typeof(object) };
            yield return new object[] { 42L, typeof(long), typeof(object) };
            yield return new object[] { 13m, typeof(decimal), typeof(object) };
            yield return new object[] { ExpressionType.Unbox, typeof(ExpressionType), typeof(object) };
        }

        public static IEnumerable<object[]> NullableUnboxableFromObject()
        {
            yield return new object[] { 1, typeof(int?), typeof(object) };
            yield return new object[] { 42, typeof(int?), typeof(object) };
            yield return new object[] { DateTime.MinValue, typeof(DateTime?), typeof(object) };
            yield return new object[] { DateTimeOffset.MinValue, typeof(DateTimeOffset?), typeof(object) };
            yield return new object[] { 42L, typeof(long?), typeof(object) };
            yield return new object[] { 13m, typeof(decimal?), typeof(object) };
            yield return new object[] { ExpressionType.Unbox, typeof(ExpressionType?), typeof(object) };
        }

        public static IEnumerable<object[]> UnboxableFromIComparable()
        {
            yield return new object[] { 1, typeof(int), typeof(IComparable) };
            yield return new object[] { 42, typeof(int), typeof(IComparable) };
            yield return new object[] { DateTime.MinValue, typeof(DateTime), typeof(IComparable) };
            yield return new object[] { DateTimeOffset.MinValue, typeof(DateTimeOffset), typeof(IComparable) };
            yield return new object[] { 42L, typeof(long), typeof(IComparable) };
            yield return new object[] { 13m, typeof(decimal), typeof(IComparable) };
            yield return new object[] { ExpressionType.Unbox, typeof(ExpressionType), typeof(IComparable) };
        }

        public static IEnumerable<object[]> NullableUnboxableFromIComparable()
        {
            yield return new object[] { 1, typeof(int?), typeof(IComparable) };
            yield return new object[] { 42, typeof(int?), typeof(IComparable) };
            yield return new object[] { DateTime.MinValue, typeof(DateTime?), typeof(IComparable) };
            yield return new object[] { DateTimeOffset.MinValue, typeof(DateTimeOffset?), typeof(IComparable) };
            yield return new object[] { 42L, typeof(long?), typeof(IComparable) };
            yield return new object[] { 13m, typeof(decimal?), typeof(IComparable) };
            yield return new object[] { ExpressionType.Unbox, typeof(ExpressionType?), typeof(IComparable) };
        }

        public static IEnumerable<object[]> UnboxableFromIComparableT()
        {
            yield return new object[] { 1, typeof(int), typeof(IComparable<int>) };
            yield return new object[] { 42, typeof(int), typeof(IComparable<int>) };
            yield return new object[] { DateTime.MinValue, typeof(DateTime), typeof(IComparable<DateTime>) };
            yield return new object[] { DateTimeOffset.MinValue, typeof(DateTimeOffset), typeof(IComparable<DateTimeOffset>) };
            yield return new object[] { 42L, typeof(long), typeof(IComparable<long>) };
            yield return new object[] { 13m, typeof(decimal), typeof(IComparable<decimal>) };
        }

        public static IEnumerable<object[]> NullableUnboxableFromIComparableT()
        {
            yield return new object[] { 1, typeof(int?), typeof(IComparable<int>) };
            yield return new object[] { 42, typeof(int?), typeof(IComparable<int>) };
            yield return new object[] { DateTime.MinValue, typeof(DateTime?), typeof(IComparable<DateTime>) };
            yield return new object[] { DateTimeOffset.MinValue, typeof(DateTimeOffset?), typeof(IComparable<DateTimeOffset>) };
            yield return new object[] { 42L, typeof(long?), typeof(IComparable<long>) };
            yield return new object[] { 13m, typeof(decimal?), typeof(IComparable<decimal>) };
        }

        public static IEnumerable<object[]> NullableTypes()
        {
            yield return new object[] { typeof(int?) };
            yield return new object[] { typeof(DateTime?) };
            yield return new object[] { typeof(DateTimeKind?) };
            yield return new object[] { typeof(DateTimeOffset?) };
            yield return new object[] { typeof(long?) };
            yield return new object[] { typeof(decimal?) };
        }

        [Theory]
        [MemberData(nameof(NullableTypes))]
        public void NullNullable(Type type)
        {
            UnaryExpression unbox = Expression.Unbox(Expression.Default(typeof(object)), type);
            Func<bool> isNull = Expression.Lambda<Func<bool>>(Expression.Equal(Expression.Default(type), unbox)).Compile();
            Assert.True(isNull());
        }

        [Fact]
        public void CannotUnboxToNonInterfaceExceptObject()
        {
            Expression value = Expression.Constant(0);
            Assert.Throws<ArgumentException>(() => Expression.Unbox(value, typeof(int)));
        }

        [Fact]
        public void CannotUnboxReferenceType()
        {
            Expression value = Expression.Constant("", typeof(IComparable<string>));
            Assert.Throws<ArgumentException>(() => Expression.Unbox(value, typeof(string)));
        }

        private static class Unreadable
        {
            public static object WriteOnly
            {
                set { }
            }
        }

        [Fact]
        public void CannotUnboxUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable), "WriteOnly");
            Assert.Throws<ArgumentException>("expression", () => Expression.Unbox(value, typeof(int)));
        }

        [Fact]
        public void ExpressionNull()
        {
            Assert.Throws<ArgumentNullException>("expression", () => Expression.Unbox(null, typeof(int)));
        }

        [Fact]
        public void TypeNull()
        {
            Expression value = Expression.Constant(0, typeof(object));
            Assert.Throws<ArgumentNullException>("type", () => Expression.Unbox(value, null));
        }

        [Fact]
        public void MistmatchFailsOnRuntime()
        {
            Expression unbox = Expression.Unbox(Expression.Constant(0, typeof(object)), typeof(long));
            Func<long> del = Expression.Lambda<Func<long>>(unbox).Compile();
            Assert.Throws<InvalidCastException>(() => del());
        }

        [Fact]
        public void CannotReduce()
        {
            Expression unbox = Expression.Unbox(Expression.Constant(0, typeof(object)), typeof(int));
            Assert.False(unbox.CanReduce);
            Assert.Same(unbox, unbox.Reduce());
            Assert.Throws<ArgumentException>(() => unbox.ReduceAndCheck());
        }
    }
}
