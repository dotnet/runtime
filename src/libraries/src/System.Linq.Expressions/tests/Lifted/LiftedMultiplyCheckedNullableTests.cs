// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LiftedMultiplyCheckedNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableByteTest(bool useInterpreter)
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableCharTest(bool useInterpreter)
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableChar(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableDecimalTest(bool useInterpreter)
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableDecimal(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableDoubleTest(bool useInterpreter)
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableDouble(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableFloatTest(bool useInterpreter)
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableFloat(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableIntTest(bool useInterpreter)
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableLongTest(bool useInterpreter)
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableLong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableSByteTest(bool useInterpreter)
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableSByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableShortTest(bool useInterpreter)
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableUIntTest(bool useInterpreter)
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableUInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableULongTest(bool useInterpreter)
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableULong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableUShortTest(bool useInterpreter)
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableUShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyCheckedNullableNumberTest(bool useInterpreter)
        {
            Number?[] values = new Number?[] { null, new Number(0), new Number(1), Number.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableNumber(values[i], values[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Helpers

        public static byte MultiplyCheckedNullableByte(byte a, byte b)
        {
            return checked((byte)(a * b));
        }

        public static char MultiplyCheckedNullableChar(char a, char b)
        {
            return checked((char)(a * b));
        }

        public static decimal MultiplyCheckedNullableDecimal(decimal a, decimal b)
        {
            return checked(a * b);
        }

        public static double MultiplyCheckedNullableDouble(double a, double b)
        {
            return checked(a * b);
        }

        public static float MultiplyCheckedNullableFloat(float a, float b)
        {
            return checked(a * b);
        }

        public static int MultiplyCheckedNullableInt(int a, int b)
        {
            return checked(a * b);
        }

        public static long MultiplyCheckedNullableLong(long a, long b)
        {
            return checked(a * b);
        }

        public static sbyte MultiplyCheckedNullableSByte(sbyte a, sbyte b)
        {
            return checked((sbyte)(a * b));
        }

        public static short MultiplyCheckedNullableShort(short a, short b)
        {
            return checked((short)(a * b));
        }

        public static uint MultiplyCheckedNullableUInt(uint a, uint b)
        {
            return checked(a * b);
        }

        public static ulong MultiplyCheckedNullableULong(ulong a, ulong b)
        {
            return checked(a * b);
        }

        public static ushort MultiplyCheckedNullableUShort(ushort a, ushort b)
        {
            return checked((ushort)(a * b));
        }

        #endregion

        #region Test verifiers

        private static void VerifyMultiplyCheckedNullableByte(byte? a, byte? b, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableByte")));
            Func<byte?> f = e.Compile(useInterpreter);

            byte? expected = null;
            try
            {
                expected = checked((byte?)(a * b));
            }
            catch(OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyMultiplyCheckedNullableChar(char? a, char? b, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableChar")));
            Func<char?> f = e.Compile(useInterpreter);

            char? expected = null;
            try
            {
                expected = checked((char?)(a * b));
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyMultiplyCheckedNullableDecimal(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableDecimal")));
            Func<decimal?> f = e.Compile(useInterpreter);

            decimal? expected = null;
            try
            {
                expected = a * b;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyMultiplyCheckedNullableDouble(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableDouble")));
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(a * b, f());
        }

        private static void VerifyMultiplyCheckedNullableFloat(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableFloat")));
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(a * b, f());
        }

        private static void VerifyMultiplyCheckedNullableInt(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableInt")));
            Func<int?> f = e.Compile(useInterpreter);

            int? expected = null;
            try
            {
                expected = checked(a * b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyMultiplyCheckedNullableLong(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableLong")));
            Func<long?> f = e.Compile(useInterpreter);

            long? expected = null;
            try
            {
                expected = checked(a * b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyMultiplyCheckedNullableSByte(sbyte? a, sbyte? b, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableSByte")));
            Func<sbyte?> f = e.Compile(useInterpreter);

            sbyte? expected = null;
            try
            {
                expected = checked((sbyte?)(a * b));
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyMultiplyCheckedNullableShort(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableShort")));
            Func<short?> f = e.Compile(useInterpreter);

            short? expected = null;
            try
            {
                expected = checked((short?)(a * b));
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyMultiplyCheckedNullableUInt(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableUInt")));
            Func<uint?> f = e.Compile(useInterpreter);

            uint? expected = null;
            try
            {
                expected = checked(a * b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyMultiplyCheckedNullableULong(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableULong")));
            Func<ulong?> f = e.Compile(useInterpreter);

            ulong? expected = null;
            try
            {
                expected = checked(a * b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyMultiplyCheckedNullableUShort(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableUShort")));
            Func<ushort?> f = e.Compile(useInterpreter);

            ushort? expected = null;
            try
            {
                expected = checked((ushort?)(a * b));
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }
        
        private static void VerifyMultiplyCheckedNullableNumber(Number? a, Number? b, bool useInterpreter)
        {
            Expression<Func<Number?>> e =
                Expression.Lambda<Func<Number?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(Number?)),
                        Expression.Constant(b, typeof(Number?))));
            Assert.Equal(typeof(Number?), e.Body.Type);
            Func<Number?> f = e.Compile(useInterpreter);

            Number? expected = a * b;
            Assert.Equal(expected, f()); // NB: checked behavior doesn't apply to non-primitive types
        }

        #endregion
    }
}
