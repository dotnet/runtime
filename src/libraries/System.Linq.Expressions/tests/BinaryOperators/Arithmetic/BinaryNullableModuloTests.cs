// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryNullableModuloTests
    {
        #region Test methods

        [Fact]
        public static void CheckNullableByteModuloTest()
        {
            byte?[] array = new byte?[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableByteModulo(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableSByteModuloTest()
        {
            sbyte?[] array = new sbyte?[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableSByteModulo(array[i], array[j]);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUShortModuloTest(bool useInterpreter)
        {
            ushort?[] array = new ushort?[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUShortModulo(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableShortModuloTest(bool useInterpreter)
        {
            short?[] array = new short?[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableShortModulo(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUIntModuloTest(bool useInterpreter)
        {
            uint?[] array = new uint?[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUIntModulo(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntModuloTest(bool useInterpreter)
        {
            int?[] array = new int?[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableIntModulo(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableULongModuloTest(bool useInterpreter)
        {
            ulong?[] array = new ulong?[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableULongModulo(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableLongModuloTest(bool useInterpreter)
        {
            long?[] array = new long?[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableLongModulo(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableFloatModuloTest(bool useInterpreter)
        {
            float?[] array = new float?[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableFloatModulo(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDoubleModuloTest(bool useInterpreter)
        {
            double?[] array = new double?[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDoubleModulo(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDecimalModuloTest(bool useInterpreter)
        {
            decimal?[] array = new decimal?[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDecimalModulo(array[i], array[j], useInterpreter);
                }
            }
        }

        [Fact]
        public static void CheckNullableCharModuloTest()
        {
            char?[] array = new char?[] { '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableCharModulo(array[i], array[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableByteModulo(byte? a, byte? b)
        {
            Expression aExp = Expression.Constant(a, typeof(byte?));
            Expression bExp = Expression.Constant(b, typeof(byte?));
            Assert.Throws<InvalidOperationException>(() => Expression.Modulo(aExp, bExp));
        }

        private static void VerifyNullableSByteModulo(sbyte? a, sbyte? b)
        {
            Expression aExp = Expression.Constant(a, typeof(sbyte?));
            Expression bExp = Expression.Constant(b, typeof(sbyte?));
            Assert.Throws<InvalidOperationException>(() => Expression.Modulo(aExp, bExp));
        }

        private static void VerifyNullableUShortModulo(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        private static void VerifyNullableShortModulo(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        private static void VerifyNullableUIntModulo(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        private static void VerifyNullableIntModulo(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else if (b == -1 && a == int.MinValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        private static void VerifyNullableULongModulo(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        private static void VerifyNullableLongModulo(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else if (b == -1 && a == long.MinValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        private static void VerifyNullableFloatModulo(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(a % b, f());
        }

        private static void VerifyNullableDoubleModulo(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(a % b, f());
        }

        private static void VerifyNullableDecimalModulo(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        private static void VerifyNullableCharModulo(char? a, char? b)
        {
            Expression aExp = Expression.Constant(a, typeof(char?));
            Expression bExp = Expression.Constant(b, typeof(char?));
            Assert.Throws<InvalidOperationException>(() => Expression.Modulo(aExp, bExp));
        }

        #endregion
    }
}
