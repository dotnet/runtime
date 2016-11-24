// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LiftedSubtractCheckedNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableByteTest(bool useInterpreter)
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableCharTest(bool useInterpreter)
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableChar(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableDecimalTest(bool useInterpreter)
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableDecimal(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableDoubleTest(bool useInterpreter)
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableDouble(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableFloatTest(bool useInterpreter)
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableFloat(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableIntTest(bool useInterpreter)
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableLongTest(bool useInterpreter)
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableLong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableSByteTest(bool useInterpreter)
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableSByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableShortTest(bool useInterpreter)
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableUIntTest(bool useInterpreter)
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableUInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableULongTest(bool useInterpreter)
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableULong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableUShortTest(bool useInterpreter)
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableUShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedSubtractCheckedNullableNumberTest(bool useInterpreter)
        {
            Number?[] values = new Number?[] { null, new Number(0), new Number(1), Number.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractCheckedNullableNumber(values[i], values[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Helpers

        public static byte SubtractCheckedNullableByte(byte a, byte b)
        {
            return checked((byte)(a - b));
        }

        public static char SubtractCheckedNullableChar(char a, char b)
        {
            return checked((char)(a - b));
        }

        public static decimal SubtractCheckedNullableDecimal(decimal a, decimal b)
        {
            return checked(a - b);
        }

        public static double SubtractCheckedNullableDouble(double a, double b)
        {
            return checked(a - b);
        }

        public static float SubtractCheckedNullableFloat(float a, float b)
        {
            return checked(a - b);
        }

        public static int SubtractCheckedNullableInt(int a, int b)
        {
            return checked(a - b);
        }

        public static long SubtractCheckedNullableLong(long a, long b)
        {
            return checked(a - b);
        }

        public static sbyte SubtractCheckedNullableSByte(sbyte a, sbyte b)
        {
            return checked((sbyte)(a - b));
        }

        public static short SubtractCheckedNullableShort(short a, short b)
        {
            return checked((short)(a - b));
        }

        public static uint SubtractCheckedNullableUInt(uint a, uint b)
        {
            return checked(a - b);
        }

        public static ulong SubtractCheckedNullableULong(ulong a, ulong b)
        {
            return checked(a - b);
        }

        public static ushort SubtractCheckedNullableUShort(ushort a, ushort b)
        {
            return checked((ushort)(a - b));
        }

        #endregion

        #region Test verifiers

        private static void VerifySubtractCheckedNullableByte(byte? a, byte? b, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedSubtractCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractCheckedNullableByte")));
            Func<byte?> f = e.Compile(useInterpreter);

            if (a < b)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(checked((byte?)(a - b)), f());
        }

        private static void VerifySubtractCheckedNullableChar(char? a, char? b, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        typeof(LiftedSubtractCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractCheckedNullableChar")));
            Func<char?> f = e.Compile(useInterpreter);

            if (a < b)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(checked((char?)(a - b)), f());
        }

        private static void VerifySubtractCheckedNullableDecimal(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        typeof(LiftedSubtractCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractCheckedNullableDecimal")));
            Func<decimal?> f = e.Compile(useInterpreter);

            decimal? expected = default(decimal);
            try
            {
                expected = checked(a - b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifySubtractCheckedNullableDouble(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        typeof(LiftedSubtractCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractCheckedNullableDouble")));
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(a - b, f());
        }

        private static void VerifySubtractCheckedNullableFloat(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        typeof(LiftedSubtractCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractCheckedNullableFloat")));
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(a - b, f());
        }

        private static void VerifySubtractCheckedNullableInt(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedSubtractCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractCheckedNullableInt")));
            Func<int?> f = e.Compile(useInterpreter);

            long? expected = (long?)a - b;
            if (expected < int.MinValue | expected > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int?)expected, f());
        }

        private static void VerifySubtractCheckedNullableLong(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedSubtractCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractCheckedNullableLong")));
            Func<long?> f = e.Compile(useInterpreter);

            long? expected = null;
            try
            {
                expected = checked(a - b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifySubtractCheckedNullableSByte(sbyte? a, sbyte? b, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedSubtractCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractCheckedNullableSByte")));
            Func<sbyte?> f = e.Compile(useInterpreter);

            int? expected = a - b;

            if (expected < sbyte.MinValue | expected > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifySubtractCheckedNullableShort(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedSubtractCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractCheckedNullableShort")));
            Func<short?> f = e.Compile(useInterpreter);

            int? expected = a - b;
            if (expected < short.MinValue | expected > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifySubtractCheckedNullableUInt(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedSubtractCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractCheckedNullableUInt")));
            Func<uint?> f = e.Compile(useInterpreter);

            if (a < b)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(a - b, f());
        }

        private static void VerifySubtractCheckedNullableULong(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedSubtractCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractCheckedNullableULong")));
            Func<ulong?> f = e.Compile(useInterpreter);

            if (a < b)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(a - b, f());
        }

        private static void VerifySubtractCheckedNullableUShort(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedSubtractCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractCheckedNullableUShort")));
            Func<ushort?> f = e.Compile(useInterpreter);

            if (a < b)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(a - b, f());
        }

        private static void VerifySubtractCheckedNullableNumber(Number? a, Number? b, bool useInterpreter)
        {
            Expression<Func<Number?>> e =
                Expression.Lambda<Func<Number?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(Number?)),
                        Expression.Constant(b, typeof(Number?))));
            Assert.Equal(typeof(Number?), e.Body.Type);
            Func<Number?> f = e.Compile(useInterpreter);

            Number? expected = a - b;
            Assert.Equal(expected, f()); // NB: checked behavior doesn't apply to non-primitive types
        }

        #endregion
    }
}
