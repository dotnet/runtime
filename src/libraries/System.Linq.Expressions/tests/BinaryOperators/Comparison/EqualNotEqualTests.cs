// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryEqualTests
    {
        public static IEnumerable<object[]> TestData()
        {
            foreach (bool useInterpreter in new bool[] { true, false })
            {
                yield return new object[] { new bool[] { true, false }, useInterpreter };
                yield return new object[] { new byte[] { 0, 1, byte.MaxValue }, useInterpreter };
                yield return new object[] { new char[] { '\0', '\b', 'A', '\uffff' }, useInterpreter };
                yield return new object[] { new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue }, useInterpreter };
                yield return new object[] { new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN }, useInterpreter };
                yield return new object[] { new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN }, useInterpreter };
                yield return new object[] { new int[] { 0, 1, -1, int.MinValue, int.MaxValue }, useInterpreter };
                yield return new object[] { new long[] { 0, 1, -1, long.MinValue, long.MaxValue }, useInterpreter };
                yield return new object[] { new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }, useInterpreter };
                yield return new object[] { new short[] { 0, 1, -1, short.MinValue, short.MaxValue }, useInterpreter };
                yield return new object[] { new uint[] { 0, 1, uint.MaxValue }, useInterpreter };
                yield return new object[] { new ulong[] { 0, 1, ulong.MaxValue }, useInterpreter };
                yield return new object[] { new ushort[] { 0, 1, ushort.MaxValue }, useInterpreter };
                yield return new object[] { new TestClass[] { new TestClass(), new TestClass() }, useInterpreter };
                yield return new object[] { new TestEnum[] { new TestEnum(), new TestEnum() }, useInterpreter };
                yield return new object[] { new E[] {E.A, E.B, (E)int.MinValue}, useInterpreter };

                yield return new object[] { new bool?[] { null, true, false }, useInterpreter };
                yield return new object[] { new byte?[] { null, 0, 1, byte.MaxValue }, useInterpreter };
                yield return new object[] { new char?[] { null, '\0', '\b', 'A', '\uffff' }, useInterpreter };
                yield return new object[] { new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue }, useInterpreter };
                yield return new object[] { new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN }, useInterpreter };
                yield return new object[] { new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN }, useInterpreter };
                yield return new object[] { new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue }, useInterpreter };
                yield return new object[] { new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue }, useInterpreter };
                yield return new object[] { new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }, useInterpreter };
                yield return new object[] { new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue }, useInterpreter };
                yield return new object[] { new uint?[] { null, 0, 1, uint.MaxValue }, useInterpreter };
                yield return new object[] { new ulong?[] { null, 0, 1, ulong.MaxValue }, useInterpreter };
                yield return new object[] { new ushort?[] { null, 0, 1, ushort.MaxValue }, useInterpreter };
                yield return new object[] { new E?[] {null, E.A, E.B, (E)int.MaxValue, (E)int.MinValue}, useInterpreter };
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void Equal(Array array, bool useInterpreter)
        {
            Type type = array.GetType().GetElementType();
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    object a = array.GetValue(i);
                    object b = array.GetValue(j);
                    BinaryExpression equal = Expression.Equal(Expression.Constant(a, type), Expression.Constant(b, type));
                    GeneralBinaryTests.CompileBinaryExpression(equal, useInterpreter, GeneralBinaryTests.CustomEquals(a, b));
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void NotEqual(Array array, bool useInterpreter)
        {
            Type type = array.GetType().GetElementType();
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    object a = array.GetValue(i);
                    object b = array.GetValue(j);
                    BinaryExpression equal = Expression.NotEqual(Expression.Constant(a, type), Expression.Constant(b, type));
                    GeneralBinaryTests.CompileBinaryExpression(equal, useInterpreter, !GeneralBinaryTests.CustomEquals(a, b));
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void Equal_Constant_DefaultString(bool useInterpreter)
        {
            var array = new Expression[] { Expression.Constant("bar", typeof(string)), Expression.Constant(null, typeof(string)), Expression.Default(typeof(string)) };
            var isNull = new bool[] { false, true, true };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    BinaryExpression equal = Expression.Equal(array[i], array[j]);
                    GeneralBinaryTests.CompileBinaryExpression(equal, useInterpreter, isNull[i] == isNull[j]);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void Equal_Constant_DefaultNullable(bool useInterpreter)
        {
            var array = new Expression[] { Expression.Constant(42, typeof(int?)), Expression.Constant(null, typeof(int?)), Expression.Default(typeof(int?)) };
            var isNull = new bool[] { false, true, true };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    BinaryExpression equal = Expression.Equal(array[i], array[j]);
                    GeneralBinaryTests.CompileBinaryExpression(equal, useInterpreter, isNull[i] == isNull[j]);
                }
            }
        }

        [Fact]
        public static void Equal_CannotReduce()
        {
            Expression exp = Expression.Equal(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void NotEqual_CannotReduce()
        {
            Expression exp = Expression.NotEqual(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void ThrowsOnLeftNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.Equal(null, Expression.Constant("")));
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.NotEqual(null, Expression.Constant("")));
        }

        [Fact]
        public static void ThrowsOnRightNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.Equal(Expression.Constant(""), null));
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.NotEqual(Expression.Constant(""), null));
        }

        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        [Fact]
        public static void ThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.Equal(value, Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.NotEqual(value, Expression.Constant(1)));
        }

        [Fact]
        public static void ThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.Equal(Expression.Constant(1), value));
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.NotEqual(Expression.Constant(1), value));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void Update_ReferenceEquals(bool useInterpreter)
        {
            TestClass testClass1 = new TestClass();
            TestClass testClass2 = new TestClass();
            BinaryExpression equal = Expression.Equal(Expression.Constant(testClass1), Expression.Constant(testClass2));
            BinaryExpression newEqual = equal.Update(Expression.Constant(testClass1), equal.Conversion, Expression.Constant(testClass1));

            // Original BinaryExpression should be unchanged
            GeneralBinaryTests.CompileBinaryExpression(equal, useInterpreter, false);
            GeneralBinaryTests.CompileBinaryExpression(newEqual, useInterpreter, true);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void Update_ReferenceNotEquals(bool useInterpreter)
        {
            TestClass testClass1 = new TestClass();
            TestClass testClass2 = new TestClass();
            BinaryExpression equal = Expression.NotEqual(Expression.Constant(testClass1), Expression.Constant(testClass2));
            BinaryExpression newEqual = equal.Update(Expression.Constant(testClass1), equal.Conversion, Expression.Constant(testClass1));

            // Original BinaryExpression should be unchanged
            GeneralBinaryTests.CompileBinaryExpression(equal, useInterpreter, true);
            GeneralBinaryTests.CompileBinaryExpression(newEqual, useInterpreter, false);
        }

        [Fact]
        public static void Equal_ToString()
        {
            BinaryExpression e = Expression.Equal(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a == b)", e.ToString());
        }

        [Fact]
        public static void NotEqual_ToString()
        {
            BinaryExpression e = Expression.NotEqual(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a != b)", e.ToString());
        }

        [Fact]
        public static void CannotPreformEqualityOnValueTypesWithoutOperators()
        {
            var uvConst = Expression.Constant(new UselessValue());
            Assert.Throws<InvalidOperationException>(() => Expression.Equal(uvConst, uvConst));
            Assert.Throws<InvalidOperationException>(() => Expression.NotEqual(uvConst, uvConst));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CanPerformEqualityOnNullableWithoutOperatorsToConstantNull(bool useInterpreter)
        {
            var nullConst = Expression.Constant(null, typeof(UselessValue?));
            var uvConst = Expression.Constant(new UselessValue(), typeof(UselessValue?));
            var exp = Expression.Lambda<Func<bool>>(Expression.Equal(nullConst, uvConst));
            var func = exp.Compile(useInterpreter);
            Assert.False(func());

            exp = Expression.Lambda<Func<bool>>(Expression.Equal(uvConst, nullConst));
            func = exp.Compile(useInterpreter);
            Assert.False(func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CanPerformInequalityOnNullableWithoutOperatorsToConstantNull(bool useInterpreter)
        {
            var nullConst = Expression.Constant(null, typeof(UselessValue?));
            var uvConst = Expression.Constant(new UselessValue(), typeof(UselessValue?));
            var exp = Expression.Lambda<Func<bool>>(Expression.NotEqual(nullConst, uvConst));
            var func = exp.Compile(useInterpreter);
            Assert.True(func());

            exp = Expression.Lambda<Func<bool>>(Expression.NotEqual(uvConst, nullConst));
            func = exp.Compile(useInterpreter);
            Assert.True(func());
        }

        [Fact]
        public static void CannotDoNullComparisonWithoutOperatorIfBothNullConstants()
        {
            var typedNullConst = Expression.Constant(null, typeof(UselessValue?));
            Assert.Throws<InvalidOperationException>(() => Expression.Equal(typedNullConst, typedNullConst));
        }

        // DBNull having a different type code to other objects could result in bugs surrounding it if
        // that type code got incorrectly used.

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CanCompareDBNullEqual(bool useInterpreter)
        {
            var x = Expression.Parameter(typeof(DBNull));
            var y = Expression.Parameter(typeof(DBNull));
            var lambda = Expression.Lambda<Func<DBNull, DBNull, bool>>(Expression.Equal(x, y), x, y);
            var func = lambda.Compile(useInterpreter);
            foreach(var xVal in new[] { DBNull.Value, null})
                foreach(var yVal in new[] { DBNull.Value, null})
                    Assert.Equal(xVal == yVal, func(xVal, yVal));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CanCompareDBNullNotEqual(bool useInterpreter)
        {
            var x = Expression.Parameter(typeof(DBNull));
            var y = Expression.Parameter(typeof(DBNull));
            var lambda = Expression.Lambda<Func<DBNull, DBNull, bool>>(Expression.NotEqual(x, y), x, y);
            var func = lambda.Compile(useInterpreter);
            foreach(var xVal in new[] { DBNull.Value, null})
                foreach(var yVal in new[] { DBNull.Value, null})
                    Assert.Equal(xVal != yVal, func(xVal, yVal));
        }

        private struct UselessValue
        {
        }

        public class TestClass { }
        public enum TestEnum { }
    }
}
