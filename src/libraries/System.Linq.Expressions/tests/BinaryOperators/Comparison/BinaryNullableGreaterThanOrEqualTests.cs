// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryNullableGreaterThanOrEqualTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableByteGreaterThanOrEqualTest(bool useInterpreter)
        {
            byte?[] array = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableByteGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableCharGreaterThanOrEqualTest(bool useInterpreter)
        {
            char?[] array = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableCharGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDecimalGreaterThanOrEqualTest(bool useInterpreter)
        {
            decimal?[] array = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDecimalGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDoubleGreaterThanOrEqualTest(bool useInterpreter)
        {
            double?[] array = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDoubleGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableFloatGreaterThanOrEqualTest(bool useInterpreter)
        {
            float?[] array = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableFloatGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntGreaterThanOrEqualTest(bool useInterpreter)
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableIntGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableLongGreaterThanOrEqualTest(bool useInterpreter)
        {
            long?[] array = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableLongGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableSByteGreaterThanOrEqualTest(bool useInterpreter)
        {
            sbyte?[] array = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableSByteGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableShortGreaterThanOrEqualTest(bool useInterpreter)
        {
            short?[] array = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableShortGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUIntGreaterThanOrEqualTest(bool useInterpreter)
        {
            uint?[] array = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUIntGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableULongGreaterThanOrEqualTest(bool useInterpreter)
        {
            ulong?[] array = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableULongGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUShortGreaterThanOrEqualTest(bool useInterpreter)
        {
            ushort?[] array = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUShortGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableByteGreaterThanOrEqual(byte? a, byte? b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyNullableCharGreaterThanOrEqual(char? a, char? b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyNullableDecimalGreaterThanOrEqual(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyNullableDoubleGreaterThanOrEqual(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyNullableFloatGreaterThanOrEqual(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyNullableIntGreaterThanOrEqual(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyNullableLongGreaterThanOrEqual(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyNullableSByteGreaterThanOrEqual(sbyte? a, sbyte? b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyNullableShortGreaterThanOrEqual(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyNullableUIntGreaterThanOrEqual(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyNullableULongGreaterThanOrEqual(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyNullableUShortGreaterThanOrEqual(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        #endregion
    }
}
