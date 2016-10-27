// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LiftedBitwiseAndNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseAndNullableByteTest(bool useInterpreter)
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseAndNullableByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseAndNullableIntTest(bool useInterpreter)
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseAndNullableInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseAndNullableLongTest(bool useInterpreter)
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseAndNullableLong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseAndNullableSByteTest(bool useInterpreter)
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseAndNullableSByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseAndNullableShortTest(bool useInterpreter)
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseAndNullableShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseAndNullableUIntTest(bool useInterpreter)
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseAndNullableUInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseAndNullableULongTest(bool useInterpreter)
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseAndNullableULong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseAndNullableUShortTest(bool useInterpreter)
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseAndNullableUShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseAndNullableNumberTest(bool useInterpreter)
        {
            Number?[] values = new Number?[] { null, new Number(0), new Number(1), Number.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseAndNullableNumber(values[i], values[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Helpers

        public static byte AndNullableByte(byte a, byte b)
        {
            return (byte)(a & b);
        }

        public static int AndNullableInt(int a, int b)
        {
            return (int)(a & b);
        }

        public static long AndNullableLong(long a, long b)
        {
            return (long)(a & b);
        }

        public static sbyte AndNullableSByte(sbyte a, sbyte b)
        {
            return (sbyte)(a & b);
        }

        public static short AndNullableShort(short a, short b)
        {
            return (short)(a & b);
        }

        public static uint AndNullableUInt(uint a, uint b)
        {
            return (uint)(a & b);
        }

        public static ulong AndNullableULong(ulong a, ulong b)
        {
            return (ulong)(a & b);
        }

        public static ushort AndNullableUShort(ushort a, ushort b)
        {
            return (ushort)(a & b);
        }

        #endregion

        #region Test verifiers

        private static void VerifyBitwiseAndNullableByte(byte? a, byte? b, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.And(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedBitwiseAndNullableTests).GetTypeInfo().GetDeclaredMethod("AndNullableByte")));
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(a & b, f());
        }

        private static void VerifyBitwiseAndNullableInt(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.And(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedBitwiseAndNullableTests).GetTypeInfo().GetDeclaredMethod("AndNullableInt")));
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(a & b, f());
        }

        private static void VerifyBitwiseAndNullableLong(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.And(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedBitwiseAndNullableTests).GetTypeInfo().GetDeclaredMethod("AndNullableLong")));
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(a & b, f());
        }

        private static void VerifyBitwiseAndNullableSByte(sbyte? a, sbyte? b, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.And(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedBitwiseAndNullableTests).GetTypeInfo().GetDeclaredMethod("AndNullableSByte")));
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(a & b, f());
        }

        private static void VerifyBitwiseAndNullableShort(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.And(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedBitwiseAndNullableTests).GetTypeInfo().GetDeclaredMethod("AndNullableShort")));
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(a & b, f());
        }

        private static void VerifyBitwiseAndNullableUInt(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.And(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedBitwiseAndNullableTests).GetTypeInfo().GetDeclaredMethod("AndNullableUInt")));
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(a & b, f());
        }

        private static void VerifyBitwiseAndNullableULong(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.And(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedBitwiseAndNullableTests).GetTypeInfo().GetDeclaredMethod("AndNullableULong")));
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(a & b, f());
        }

        private static void VerifyBitwiseAndNullableUShort(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.And(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedBitwiseAndNullableTests).GetTypeInfo().GetDeclaredMethod("AndNullableUShort")));
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(a & b, f());
        }

        private static void VerifyBitwiseAndNullableNumber(Number? a, Number? b, bool useInterpreter)
        {
            Expression<Func<Number?>> e =
                Expression.Lambda<Func<Number?>>(
                    Expression.And(
                        Expression.Constant(a, typeof(Number?)),
                        Expression.Constant(b, typeof(Number?))));
            Assert.Equal(typeof(Number?), e.Body.Type);
            Func<Number?> f = e.Compile(useInterpreter);

            Number? expected = a & b;
            Assert.Equal(expected, f());
        }

        #endregion
    }
}
