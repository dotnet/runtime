// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class UnboxTests
    {
        [Theory]
        [PerCompilationType(nameof(UnboxableFromObject))]
        [PerCompilationType(nameof(NullableUnboxableFromObject))]
        [PerCompilationType(nameof(UnboxableFromIComparable))]
        [PerCompilationType(nameof(NullableUnboxableFromIComparable))]
        [PerCompilationType(nameof(UnboxableFromIComparableT))]
        [PerCompilationType(nameof(NullableUnboxableFromIComparableT))]
        public void CanUnbox(object value, Type type, Type boxedType, bool useInterpreter)
        {
            Expression expression = Expression.Constant(value, boxedType);
            UnaryExpression unbox = Expression.Unbox(expression, type);
            Assert.Equal(type, unbox.Type);
            BinaryExpression isEqual = Expression.Equal(Expression.Constant(value, type), unbox);
            Assert.True(Expression.Lambda<Func<bool>>(isEqual).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(UnboxableFromObject))]
        [PerCompilationType(nameof(NullableUnboxableFromObject))]
        [PerCompilationType(nameof(UnboxableFromIComparable))]
        [PerCompilationType(nameof(NullableUnboxableFromIComparable))]
        [PerCompilationType(nameof(UnboxableFromIComparableT))]
        [PerCompilationType(nameof(NullableUnboxableFromIComparableT))]
        public void CanUnboxFromMake(object value, Type type, Type boxedType, bool useInterpreter)
        {
            Expression expression = Expression.Constant(value, boxedType);
            UnaryExpression unbox = Expression.MakeUnary(ExpressionType.Unbox, expression, type);
            Assert.Equal(type, unbox.Type);
            BinaryExpression isEqual = Expression.Equal(Expression.Constant(value, type), unbox);
            Assert.True(Expression.Lambda<Func<bool>>(isEqual).Compile(useInterpreter)());
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
        [PerCompilationType(nameof(NullableTypes))]
        public void NullNullable(Type type, bool useInterpreter)
        {
            UnaryExpression unbox = Expression.Unbox(Expression.Default(typeof(object)), type);
            Func<bool> isNull = Expression.Lambda<Func<bool>>(Expression.Equal(Expression.Default(type), unbox)).Compile(useInterpreter);
            Assert.True(isNull());
        }

        [Fact]
        public void CannotUnboxToNonInterfaceExceptObject()
        {
            Expression value = Expression.Constant(0);
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Unbox(value, typeof(int)));
        }

        [Fact]
        public void CannotUnboxReferenceType()
        {
            Expression value = Expression.Constant("", typeof(IComparable<string>));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Unbox(value, typeof(string)));
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
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Unbox(value, typeof(int)));
        }

        [Fact]
        public void ExpressionNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("expression", () => Expression.Unbox(null, typeof(int)));
        }

        [Fact]
        public void TypeNull()
        {
            Expression value = Expression.Constant(0, typeof(object));
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Unbox(value, null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void MistmatchFailsOnRuntime(bool useInterpreter)
        {
            Expression unbox = Expression.Unbox(Expression.Constant(0, typeof(object)), typeof(long));
            Func<long> del = Expression.Lambda<Func<long>>(unbox).Compile(useInterpreter);
            Assert.Throws<InvalidCastException>(() => del());
        }

        [Fact]
        public void CannotReduce()
        {
            Expression unbox = Expression.Unbox(Expression.Constant(0, typeof(object)), typeof(int));
            Assert.False(unbox.CanReduce);
            Assert.Same(unbox, unbox.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => unbox.ReduceAndCheck());
        }

        [Fact]
        public static void PointerType()
        {
            Type pointerType = typeof(int).MakePointerType();
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Unbox(Expression.Constant(new object()), pointerType));
        }

        [Fact]
        public static void ByRefType()
        {
            Type byRefType = typeof(int).MakeByRefType();
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Unbox(Expression.Constant(new object()), byRefType));
        }

        private struct GenericValueType<T>
        {
            public T Value { get; set; }
        }

        [Fact]
        public static void GenericType()
        {
            Type genType = typeof(GenericValueType<>);
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Unbox(Expression.Constant(new object()), genType));
        }


        [Fact]
        public static void GenericTypeParameters()
        {
            Type genType = typeof(GenericValueType<>);
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Unbox(Expression.Constant(new object()), genType.MakeGenericType(genType)));
        }
    }
}
