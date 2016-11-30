// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LiftedAddCheckedNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableByteTest(bool useInterpreter)
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableCharTest(bool useInterpreter)
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableChar(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableDecimalTest(bool useInterpreter)
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableDecimal(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableDoubleTest(bool useInterpreter)
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableDouble(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableFloatTest(bool useInterpreter)
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableFloat(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableIntTest(bool useInterpreter)
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableLongTest(bool useInterpreter)
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableLong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableSByteTest(bool useInterpreter)
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableSByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableShortTest(bool useInterpreter)
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableUIntTest(bool useInterpreter)
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableUInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableULongTest(bool useInterpreter)
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableULong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableUShortTest(bool useInterpreter)
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableUShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddCheckedNullableNumberTest(bool useInterpreter)
        {
            Number?[] values = new Number?[] { null, new Number(0), new Number(1), Number.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddCheckedNullableNumber(values[i], values[j], useInterpreter);
                }
            }
        }

        [Fact] // See https://github.com/dotnet/corefx/issues/13048
        public static void CheckLiftedAddCheckedRegressionTest()
        {
            // Regression test for an issue where `liftToNull` was set to `false` in `AddChecked`,
            // causing the return type not to get lifted to null, unlike for other binary node types.

            BinaryExpression expr =
                Expression.AddChecked(
                    Expression.Parameter(typeof(PeculiarAddable?)),
                    Expression.Parameter(typeof(PeculiarAddable?))
                );

            Assert.Equal(typeof(bool?), expr.Type);
        }

        #endregion

        #region Helpers

        public static byte AddCheckedNullableByte(byte a, byte b)
        {
            return checked((byte)(a + b));
        }

        public static char AddCheckedNullableChar(char a, char b)
        {
            return checked((char)(a + b));
        }

        public static decimal AddCheckedNullableDecimal(decimal a, decimal b)
        {
            return checked(a + b);
        }

        public static double AddCheckedNullableDouble(double a, double b)
        {
            return checked(a + b);
        }

        public static float AddCheckedNullableFloat(float a, float b)
        {
            return checked(a + b);
        }

        public static int AddCheckedNullableInt(int a, int b)
        {
            return checked(a + b);
        }

        public static long AddCheckedNullableLong(long a, long b)
        {
            return checked(a + b);
        }

        public static sbyte AddCheckedNullableSByte(sbyte a, sbyte b)
        {
            return checked((sbyte)(a + b));
        }

        public static short AddCheckedNullableShort(short a, short b)
        {
            return checked((short)(a + b));
        }

        public static uint AddCheckedNullableUInt(uint a, uint b)
        {
            return checked(a + b);
        }

        public static ulong AddCheckedNullableULong(ulong a, ulong b)
        {
            return checked(a + b);
        }

        public static ushort AddCheckedNullableUShort(ushort a, ushort b)
        {
            return checked((ushort)(a + b));
        }

        #endregion

        #region Test verifiers

        private static void VerifyAddCheckedNullableByte(byte? a, byte? b, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedAddCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("AddCheckedNullableByte")));
            Func<byte?> f = e.Compile(useInterpreter);

            int? expected = a + b;
            if (expected > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyAddCheckedNullableChar(char? a, char? b, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        typeof(LiftedAddCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("AddCheckedNullableChar")));
            Func<char?> f = e.Compile(useInterpreter);

            int? expected = a + b;
            if (expected > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyAddCheckedNullableDecimal(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        typeof(LiftedAddCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("AddCheckedNullableDecimal")));
            Func<decimal?> f = e.Compile(useInterpreter);

            decimal? expected = null;
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

        private static void VerifyAddCheckedNullableDouble(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        typeof(LiftedAddCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("AddCheckedNullableDouble")));
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(a + b, f());
        }

        private static void VerifyAddCheckedNullableFloat(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        typeof(LiftedAddCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("AddCheckedNullableFloat")));
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(a + b, f());
        }

        private static void VerifyAddCheckedNullableInt(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedAddCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("AddCheckedNullableInt")));
            Func<int?> f = e.Compile(useInterpreter);

            long? expected = (long?)a + b;
            if (expected < int.MinValue | expected > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int?)expected, f());
        }

        private static void VerifyAddCheckedNullableLong(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedAddCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("AddCheckedNullableLong")));
            Func<long?> f = e.Compile(useInterpreter);

            long? expected = null;
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

        private static void VerifyAddCheckedNullableSByte(sbyte? a, sbyte? b, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedAddCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("AddCheckedNullableSByte")));
            Func<sbyte?> f = e.Compile(useInterpreter);

            int? expected = a + b;
            if (expected < sbyte.MinValue | expected > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyAddCheckedNullableShort(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedAddCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("AddCheckedNullableShort")));
            Func<short?> f = e.Compile(useInterpreter);

            int? expected = a + b;
            if (expected < short.MinValue | expected > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyAddCheckedNullableUInt(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedAddCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("AddCheckedNullableUInt")));
            Func<uint?> f = e.Compile(useInterpreter);

            ulong? expected = (ulong?)a + b;
            if (expected > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyAddCheckedNullableULong(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedAddCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("AddCheckedNullableULong")));
            Func<ulong?> f = e.Compile(useInterpreter);

            ulong? expected = null;
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

        private static void VerifyAddCheckedNullableUShort(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedAddCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("AddCheckedNullableUShort")));
            Func<ushort?> f = e.Compile(useInterpreter);

            int? expected = a + b;
            if (expected > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyAddCheckedNullableNumber(Number? a, Number? b, bool useInterpreter)
        {
            Expression<Func<Number?>> e =
                Expression.Lambda<Func<Number?>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(Number?)),
                        Expression.Constant(b, typeof(Number?))));
            Assert.Equal(typeof(Number?), e.Body.Type);
            Func<Number?> f = e.Compile(useInterpreter);

            Number? expected = a + b;
            Assert.Equal(expected, f()); // NB: checked behavior doesn't apply to non-primitive types
        }

        #endregion

        #region Helper types

        struct PeculiarAddable
        {
            public static bool operator +(PeculiarAddable l, PeculiarAddable r) => true;
        }

        #endregion
    }
}
