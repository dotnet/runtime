// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LiftedBitwiseExclusiveOrNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseExclusiveOrNullableByteTest(bool useInterpreter)
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseExclusiveOrNullableByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseExclusiveOrNullableIntTest(bool useInterpreter)
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseExclusiveOrNullableInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseExclusiveOrNullableLongTest(bool useInterpreter)
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseExclusiveOrNullableLong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseExclusiveOrNullableSByteTest(bool useInterpreter)
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseExclusiveOrNullableSByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseExclusiveOrNullableShortTest(bool useInterpreter)
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseExclusiveOrNullableShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseExclusiveOrNullableUIntTest(bool useInterpreter)
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseExclusiveOrNullableUInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseExclusiveOrNullableULongTest(bool useInterpreter)
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseExclusiveOrNullableULong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseExclusiveOrNullableUShortTest(bool useInterpreter)
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseExclusiveOrNullableUShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedBitwiseExclusiveOrNullableNumberTest(bool useInterpreter)
        {
            Number?[] values = new Number?[] { null, new Number(0), new Number(1), Number.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseExclusiveOrNullableNumber(values[i], values[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Helpers

        public static byte ExclusiveOrNullableByte(byte a, byte b)
        {
            return (byte)(a ^ b);
        }

        public static int ExclusiveOrNullableInt(int a, int b)
        {
            return (int)(a ^ b);
        }

        public static long ExclusiveOrNullableLong(long a, long b)
        {
            return (long)(a ^ b);
        }

        public static sbyte ExclusiveOrNullableSByte(sbyte a, sbyte b)
        {
            return (sbyte)(a ^ b);
        }

        public static short ExclusiveOrNullableShort(short a, short b)
        {
            return (short)(a ^ b);
        }

        public static uint ExclusiveOrNullableUInt(uint a, uint b)
        {
            return (uint)(a ^ b);
        }

        public static ulong ExclusiveOrNullableULong(ulong a, ulong b)
        {
            return (ulong)(a ^ b);
        }

        public static ushort ExclusiveOrNullableUShort(ushort a, ushort b)
        {
            return (ushort)(a ^ b);
        }

        #endregion

        #region Test verifiers

        private static void VerifyBitwiseExclusiveOrNullableByte(byte? a, byte? b, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedBitwiseExclusiveOrNullableTests).GetTypeInfo().GetDeclaredMethod("ExclusiveOrNullableByte")));
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(a ^ b, f());
        }

        private static void VerifyBitwiseExclusiveOrNullableInt(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedBitwiseExclusiveOrNullableTests).GetTypeInfo().GetDeclaredMethod("ExclusiveOrNullableInt")));
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(a ^ b, f());
        }

        private static void VerifyBitwiseExclusiveOrNullableLong(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedBitwiseExclusiveOrNullableTests).GetTypeInfo().GetDeclaredMethod("ExclusiveOrNullableLong")));
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(a ^ b, f());
        }

        private static void VerifyBitwiseExclusiveOrNullableSByte(sbyte? a, sbyte? b, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedBitwiseExclusiveOrNullableTests).GetTypeInfo().GetDeclaredMethod("ExclusiveOrNullableSByte")));
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(a ^ b, f());
        }

        private static void VerifyBitwiseExclusiveOrNullableShort(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedBitwiseExclusiveOrNullableTests).GetTypeInfo().GetDeclaredMethod("ExclusiveOrNullableShort")));
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(a ^ b, f());
        }

        private static void VerifyBitwiseExclusiveOrNullableUInt(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedBitwiseExclusiveOrNullableTests).GetTypeInfo().GetDeclaredMethod("ExclusiveOrNullableUInt")));
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(a ^ b, f());
        }

        private static void VerifyBitwiseExclusiveOrNullableULong(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedBitwiseExclusiveOrNullableTests).GetTypeInfo().GetDeclaredMethod("ExclusiveOrNullableULong")));
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(a ^ b, f());
        }

        private static void VerifyBitwiseExclusiveOrNullableUShort(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedBitwiseExclusiveOrNullableTests).GetTypeInfo().GetDeclaredMethod("ExclusiveOrNullableUShort")));
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(a ^ b, f());
        }

        private static void VerifyBitwiseExclusiveOrNullableNumber(Number? a, Number? b, bool useInterpreter)
        {
            Expression<Func<Number?>> e =
                Expression.Lambda<Func<Number?>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(Number?)),
                        Expression.Constant(b, typeof(Number?))));
            Assert.Equal(typeof(Number?), e.Body.Type);
            Func<Number?> f = e.Compile(useInterpreter);

            Number? expected = a ^ b;
            Assert.Equal(expected, f());
        }

        #endregion
    }
}
