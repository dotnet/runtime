// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LiftedDivideNullableTests
    {
        #region Test methods

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableByteTest(bool useInterpreter)
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableCharTest(bool useInterpreter)
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableChar(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableDecimalTest(bool useInterpreter)
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableDecimal(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableDoubleTest(bool useInterpreter)
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableDouble(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableFloatTest(bool useInterpreter)
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableFloat(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableIntTest(bool useInterpreter)
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableLongTest(bool useInterpreter)
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableLong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableSByteTest(bool useInterpreter)
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableSByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableShortTest(bool useInterpreter)
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableUIntTest(bool useInterpreter)
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableUInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableULongTest(bool useInterpreter)
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableULong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableUShortTest(bool useInterpreter)
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableUShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedDivideNullableNumberTest(bool useInterpreter)
        {
            Number?[] values = new Number?[] { null, new Number(0), new Number(1), Number.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyDivideNullableNumber(values[i], values[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Helpers

        public static byte DivideNullableByte(byte a, byte b)
        {
            return (byte)(a / b);
        }

        public static char DivideNullableChar(char a, char b)
        {
            return (char)(a / b);
        }

        public static decimal DivideNullableDecimal(decimal a, decimal b)
        {
            return (decimal)(a / b);
        }

        public static double DivideNullableDouble(double a, double b)
        {
            return (double)(a / b);
        }

        public static float DivideNullableFloat(float a, float b)
        {
            return (float)(a / b);
        }

        public static int DivideNullableInt(int a, int b)
        {
            return (int)(a / b);
        }

        public static long DivideNullableLong(long a, long b)
        {
            return (long)(a / b);
        }

        public static sbyte DivideNullableSByte(sbyte a, sbyte b)
        {
            return unchecked((sbyte)(a / b));
        }

        public static short DivideNullableShort(short a, short b)
        {
            return unchecked((short)(a / b));
        }

        public static uint DivideNullableUInt(uint a, uint b)
        {
            return (uint)(a / b);
        }

        public static ulong DivideNullableULong(ulong a, ulong b)
        {
            return (ulong)(a / b);
        }

        public static ushort DivideNullableUShort(ushort a, ushort b)
        {
            return (ushort)(a / b);
        }

        #endregion

        #region Test verifiers

        private static void VerifyDivideNullableByte(byte? a, byte? b, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedDivideNullableTests).GetTypeInfo().GetDeclaredMethod("DivideNullableByte")));
            Func<byte?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal((byte?)(a / b), f());
        }

        private static void VerifyDivideNullableChar(char? a, char? b, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        typeof(LiftedDivideNullableTests).GetTypeInfo().GetDeclaredMethod("DivideNullableChar")));
            Func<char?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == '\0')
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal((char?)(a / b), f());
        }

        private static void VerifyDivideNullableDecimal(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        typeof(LiftedDivideNullableTests).GetTypeInfo().GetDeclaredMethod("DivideNullableDecimal")));
            Func<decimal?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a / b, f());
        }

        private static void VerifyDivideNullableDouble(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        typeof(LiftedDivideNullableTests).GetTypeInfo().GetDeclaredMethod("DivideNullableDouble")));
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(a / b, f());
        }

        private static void VerifyDivideNullableFloat(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        typeof(LiftedDivideNullableTests).GetTypeInfo().GetDeclaredMethod("DivideNullableFloat")));
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(a / b, f());
        }

        private static void VerifyDivideNullableInt(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedDivideNullableTests).GetTypeInfo().GetDeclaredMethod("DivideNullableInt")));
            Func<int?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else if (a == int.MinValue && b == -1)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(a / b, f());
        }

        private static void VerifyDivideNullableLong(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedDivideNullableTests).GetTypeInfo().GetDeclaredMethod("DivideNullableLong")));
            Func<long?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else if (a == long.MinValue && b == -1)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(a / b, f());
        }

        private static void VerifyDivideNullableSByte(sbyte? a, sbyte? b, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedDivideNullableTests).GetTypeInfo().GetDeclaredMethod("DivideNullableSByte")));
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(unchecked((sbyte?)(a / b)), f());
        }

        private static void VerifyDivideNullableShort(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedDivideNullableTests).GetTypeInfo().GetDeclaredMethod("DivideNullableShort")));
            Func<short?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(unchecked((short?)(a / b)), f());
        }

        private static void VerifyDivideNullableUInt(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedDivideNullableTests).GetTypeInfo().GetDeclaredMethod("DivideNullableUInt")));
            Func<uint?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a / b, f());
        }

        private static void VerifyDivideNullableULong(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedDivideNullableTests).GetTypeInfo().GetDeclaredMethod("DivideNullableULong")));
            Func<ulong?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a / b, f());
        }

        private static void VerifyDivideNullableUShort(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedDivideNullableTests).GetTypeInfo().GetDeclaredMethod("DivideNullableUShort")));
            Func<ushort?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal((ushort?)(a / b), f());
        }

        private static void VerifyDivideNullableNumber(Number? a, Number? b, bool useInterpreter)
        {
            Expression<Func<Number?>> e =
                Expression.Lambda<Func<Number?>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(Number?)),
                        Expression.Constant(b, typeof(Number?))));
            Assert.Equal(typeof(Number?), e.Body.Type);
            Func<Number?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == new Number(0))
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a / b, f());
        }

        #endregion
    }
}
