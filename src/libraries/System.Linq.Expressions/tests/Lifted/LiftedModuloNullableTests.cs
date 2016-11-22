// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LiftedModuloNullableTests
    {
        #region Test methods

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/513
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableByteTest(bool useInterpreter)
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/513
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableCharTest(bool useInterpreter)
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableChar(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableDecimalTest(bool useInterpreter)
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableDecimal(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableDoubleTest(bool useInterpreter)
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableDouble(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableFloatTest(bool useInterpreter)
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableFloat(values[i], values[j], useInterpreter);
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/513
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableIntTest(bool useInterpreter)
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/513
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableLongTest(bool useInterpreter)
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableLong(values[i], values[j], useInterpreter);
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/513
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableSByteTest(bool useInterpreter)
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableSByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/513
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableShortTest(bool useInterpreter)
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/513
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableUIntTest(bool useInterpreter)
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableUInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/513
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableULongTest(bool useInterpreter)
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableULong(values[i], values[j], useInterpreter);
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/513
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableUShortTest(bool useInterpreter)
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableUShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/513
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedModuloNullableNumberTest(bool useInterpreter)
        {
            Number?[] values = new Number?[] { null, new Number(0), new Number(1), Number.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloNullableNumber(values[i], values[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Helpers

        public static byte ModuloNullableByte(byte a, byte b)
        {
            return (byte)(a % b);
        }

        public static char ModuloNullableChar(char a, char b)
        {
            return (char)(a % b);
        }

        public static decimal ModuloNullableDecimal(decimal a, decimal b)
        {
            return (decimal)(a % b);
        }

        public static double ModuloNullableDouble(double a, double b)
        {
            return (double)(a % b);
        }

        public static float ModuloNullableFloat(float a, float b)
        {
            return (float)(a % b);
        }

        public static int ModuloNullableInt(int a, int b)
        {
            return (int)(a % b);
        }

        public static long ModuloNullableLong(long a, long b)
        {
            return (long)(a % b);
        }

        public static sbyte ModuloNullableSByte(sbyte a, sbyte b)
        {
            return (sbyte)(a % b);
        }

        public static short ModuloNullableShort(short a, short b)
        {
            return (short)(a % b);
        }

        public static uint ModuloNullableUInt(uint a, uint b)
        {
            return (uint)(a % b);
        }

        public static ulong ModuloNullableULong(ulong a, ulong b)
        {
            return (ulong)(a % b);
        }

        public static ushort ModuloNullableUShort(ushort a, ushort b)
        {
            return (ushort)(a % b);
        }

        #endregion

        #region Test verifiers

        private static void VerifyModuloNullableByte(byte? a, byte? b, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedModuloNullableTests).GetTypeInfo().GetDeclaredMethod("ModuloNullableByte")));
            Func<byte?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal((byte?)(a % b), f());
        }

        private static void VerifyModuloNullableChar(char? a, char? b, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        typeof(LiftedModuloNullableTests).GetTypeInfo().GetDeclaredMethod("ModuloNullableChar")));
            Func<char?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == '\0')
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal((char?)(a % b), f());
        }

        private static void VerifyModuloNullableDecimal(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        typeof(LiftedModuloNullableTests).GetTypeInfo().GetDeclaredMethod("ModuloNullableDecimal")));
            Func<decimal?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        private static void VerifyModuloNullableDouble(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        typeof(LiftedModuloNullableTests).GetTypeInfo().GetDeclaredMethod("ModuloNullableDouble")));
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(a % b, f());
        }

        private static void VerifyModuloNullableFloat(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        typeof(LiftedModuloNullableTests).GetTypeInfo().GetDeclaredMethod("ModuloNullableFloat")));
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(a % b, f());
        }

        private static void VerifyModuloNullableInt(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedModuloNullableTests).GetTypeInfo().GetDeclaredMethod("ModuloNullableInt")));
            Func<int?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else if (a == int.MinValue && b == -1)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        private static void VerifyModuloNullableLong(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedModuloNullableTests).GetTypeInfo().GetDeclaredMethod("ModuloNullableLong")));
            Func<long?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else if (a == long.MinValue && b == -1)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        private static void VerifyModuloNullableSByte(sbyte? a, sbyte? b, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedModuloNullableTests).GetTypeInfo().GetDeclaredMethod("ModuloNullableSByte")));
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal((sbyte?)(a % b), f());
        }

        private static void VerifyModuloNullableShort(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedModuloNullableTests).GetTypeInfo().GetDeclaredMethod("ModuloNullableShort")));
            Func<short?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal((short?)(a % b), f());
        }

        private static void VerifyModuloNullableUInt(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedModuloNullableTests).GetTypeInfo().GetDeclaredMethod("ModuloNullableUInt")));
            Func<uint?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        private static void VerifyModuloNullableULong(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedModuloNullableTests).GetTypeInfo().GetDeclaredMethod("ModuloNullableULong")));
            Func<ulong?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        private static void VerifyModuloNullableUShort(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedModuloNullableTests).GetTypeInfo().GetDeclaredMethod("ModuloNullableUShort")));
            Func<ushort?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal((ushort?)(a % b), f());
        }

        private static void VerifyModuloNullableNumber(Number? a, Number? b, bool useInterpreter)
        {
            Expression<Func<Number?>> e =
                Expression.Lambda<Func<Number?>>(
                    Expression.Modulo(
                        Expression.Constant(a, typeof(Number?)),
                        Expression.Constant(b, typeof(Number?))));
            Assert.Equal(typeof(Number?), e.Body.Type);
            Func<Number?> f = e.Compile(useInterpreter);

            if (a.HasValue && b == new Number(0))
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a % b, f());
        }

        #endregion
    }
}
