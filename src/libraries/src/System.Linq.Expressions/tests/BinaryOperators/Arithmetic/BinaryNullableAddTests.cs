// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryNullableAddTests
    {
        #region Test methods

        [Fact]
        public static void CheckNullableByteAddTest()
        {
            byte?[] array = { 0, 1, byte.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableByteAdd(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableSByteAddTest()
        {
            sbyte?[] array = { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableSByteAdd(array[i], array[j]);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUShortAddTest(bool useInterpreter)
        {
            ushort?[] array = { 0, 1, ushort.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUShortAdd(array[i], array[j], useInterpreter);
                    VerifyNullableUShortAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableShortAddTest(bool useInterpreter)
        {
            short?[] array = { 0, 1, -1, short.MinValue, short.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableShortAdd(array[i], array[j], useInterpreter);
                    VerifyNullableShortAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUIntAddTest(bool useInterpreter)
        {
            uint?[] array = { 0, 1, uint.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUIntAdd(array[i], array[j], useInterpreter);
                    VerifyNullableUIntAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntAddTest(bool useInterpreter)
        {
            int?[] array = { 0, 1, -1, int.MinValue, int.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableIntAdd(array[i], array[j], useInterpreter);
                    VerifyNullableIntAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableULongAddTest(bool useInterpreter)
        {
            ulong?[] array = { 0, 1, ulong.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableULongAdd(array[i], array[j], useInterpreter);
                    VerifyNullableULongAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableLongAddTest(bool useInterpreter)
        {
            long?[] array = { 0, 1, -1, long.MinValue, long.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableLongAdd(array[i], array[j], useInterpreter);
                    VerifyNullableLongAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableFloatAddTest(bool useInterpreter)
        {
            float?[] array = { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN, null };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableFloatAdd(array[i], array[j], useInterpreter);
                    VerifyNullableFloatAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDoubleAddTest(bool useInterpreter)
        {
            double?[] array = { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN, null };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDoubleAdd(array[i], array[j], useInterpreter);
                    VerifyNullableDoubleAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDecimalAddTest(bool useInterpreter)
        {
            decimal?[] array = { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDecimalAdd(array[i], array[j], useInterpreter);
                    VerifyNullableDecimalAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Fact]
        public static void CheckNullableCharAddTest()
        {
            char?[] array = { '\0', '\b', 'A', '\uffff', null };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableCharAdd(array[i], array[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableByteAdd(byte? a, byte? b)
        {
            Expression aExp = Expression.Constant(a, typeof(byte?));
            Expression bExp = Expression.Constant(b, typeof(byte?));
            Assert.Throws<InvalidOperationException>(() => Expression.Add(aExp, bExp));
            Assert.Throws<InvalidOperationException>(() => Expression.AddChecked(aExp, bExp));
        }

        private static void VerifyNullableSByteAdd(sbyte? a, sbyte? b)
        {
            Expression aExp = Expression.Constant(a, typeof(sbyte?));
            Expression bExp = Expression.Constant(b, typeof(sbyte?));
            Assert.Throws<InvalidOperationException>(() => Expression.Add(aExp, bExp));
            Assert.Throws<InvalidOperationException>(() => Expression.AddChecked(aExp, bExp));
        }

        private static void VerifyNullableUShortAdd(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort?)(a + b)), f());
        }

        private static void VerifyNullableUShortAddOvf(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            int? expected = a + b;
            if (expected < 0 || expected > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyNullableShortAdd(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short?)(a + b)), f());
        }

        private static void VerifyNullableShortAddOvf(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            int? expected = a + b;
            if (expected < short.MinValue || expected > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyNullableUIntAdd(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a + b), f());
        }

        private static void VerifyNullableUIntAddOvf(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            long? expected = a + (long?)b;
            if (expected < 0 || expected > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyNullableIntAdd(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a + b), f());
        }

        private static void VerifyNullableIntAddOvf(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            long? expected = a + (long?)b;
            if (expected < int.MinValue || expected > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyNullableULongAdd(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a + b), f());
        }

        private static void VerifyNullableULongAddOvf(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            ulong? expected;
            try
            {
                expected = checked(a + b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyNullableLongAdd(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a + b), f());
        }

        private static void VerifyNullableLongAddOvf(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            long? expected;
            try
            {
                expected = checked(a + b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyNullableFloatAdd(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(a + b, f());
        }

        private static void VerifyNullableFloatAddOvf(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(a + b, f());
        }

        private static void VerifyNullableDoubleAdd(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(a + b, f());
        }

        private static void VerifyNullableDoubleAddOvf(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(a + b, f());
        }

        private static void VerifyNullableDecimalAdd(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            decimal? expected;
            try
            {
                expected = a + b;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyNullableDecimalAddOvf(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            decimal? expected;
            try
            {
                expected = a + b;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyNullableCharAdd(char? a, char? b)
        {
            Expression aExp = Expression.Constant(a, typeof(char?));
            Expression bExp = Expression.Constant(b, typeof(char?));
            Assert.Throws<InvalidOperationException>(() => Expression.Add(aExp, bExp));
            Assert.Throws<InvalidOperationException>(() => Expression.AddChecked(aExp, bExp));
        }

        #endregion
    }
}
