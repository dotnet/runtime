// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryGreaterThanOrEqualTests
    {
        public static IEnumerable<object[]> TestData()
        {
            foreach (bool useInterpreter in new bool[] { true, false })
            {
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
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void GreaterThanOrEqual(Array array, bool useInterpreter)
        {
            Type type = array.GetType().GetElementType();
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    object a = array.GetValue(i);
                    object b = array.GetValue(j);
                    BinaryExpression equal = Expression.GreaterThanOrEqual(Expression.Constant(a, type), Expression.Constant(b, type));
                    GeneralBinaryTests.CompileBinaryExpression(equal, useInterpreter, GeneralBinaryTests.CustomGreaterThanOrEqual(a, b));
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void GreaterThan(Array array, bool useInterpreter)
        {
            Type type = array.GetType().GetElementType();
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    object a = array.GetValue(i);
                    object b = array.GetValue(j);
                    BinaryExpression equal = Expression.GreaterThan(Expression.Constant(a, type), Expression.Constant(b, type));
                    GeneralBinaryTests.CompileBinaryExpression(equal, useInterpreter, GeneralBinaryTests.CustomGreaterThan(a, b));
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void LessThanOrEqual(Array array, bool useInterpreter)
        {
            Type type = array.GetType().GetElementType();
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    object a = array.GetValue(i);
                    object b = array.GetValue(j);
                    BinaryExpression equal = Expression.LessThanOrEqual(Expression.Constant(a, type), Expression.Constant(b, type));
                    GeneralBinaryTests.CompileBinaryExpression(equal, useInterpreter, GeneralBinaryTests.CustomLessThanOrEqual(a, b));
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void LessThan(Array array, bool useInterpreter)
        {
            Type type = array.GetType().GetElementType();
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    object a = array.GetValue(i);
                    object b = array.GetValue(j);
                    BinaryExpression equal = Expression.LessThan(Expression.Constant(a, type), Expression.Constant(b, type));
                    GeneralBinaryTests.CompileBinaryExpression(equal, useInterpreter, GeneralBinaryTests.CustomLessThan(a, b));
                }
            }
        }

        [Fact]
        public static void GreaterThanOrEqual_CannotReduce()
        {
            Expression exp = Expression.GreaterThanOrEqual(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void GreaterThan_CannotReduce()
        {
            Expression exp = Expression.GreaterThan(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void LessThanOrEqual_CannotReduce()
        {
            Expression exp = Expression.LessThanOrEqual(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void LessThan_CannotReduce()
        {
            Expression exp = Expression.LessThan(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void ThrowsOnLeftNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.GreaterThanOrEqual(null, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.GreaterThan(null, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.LessThanOrEqual(null, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.LessThanOrEqual(null, Expression.Constant(0)));
        }

        [Fact]
        public static void ThrowsOnRightNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.GreaterThanOrEqual(Expression.Constant(0), null));
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.GreaterThan(Expression.Constant(0), null));
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.LessThanOrEqual(Expression.Constant(0), null));
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.LessThan(Expression.Constant(0), null));
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
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.GreaterThanOrEqual(value, Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.GreaterThan(value, Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.LessThanOrEqual(value, Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.LessThan(value, Expression.Constant(1)));
        }

        [Fact]
        public static void ThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.GreaterThanOrEqual(Expression.Constant(1), value));
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.GreaterThan(Expression.Constant(1), value));
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.LessThanOrEqual(Expression.Constant(1), value));
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.LessThan(Expression.Constant(1), value));
        }

        [Fact]
        public static void GreaterThanOrEqual_ToString()
        {
            BinaryExpression e = Expression.GreaterThanOrEqual(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a >= b)", e.ToString());
        }

        [Fact]
        public static void GreaterThan_ToString()
        {
            BinaryExpression e = Expression.GreaterThan(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a > b)", e.ToString());
        }

        [Fact]
        public static void LessThanOrEqual_ToString()
        {
            BinaryExpression e = Expression.LessThanOrEqual(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a <= b)", e.ToString());
        }

        [Fact]
        public static void LessThan_ToString()
        {
            BinaryExpression e = Expression.LessThan(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a < b)", e.ToString());
        }
    }
}
