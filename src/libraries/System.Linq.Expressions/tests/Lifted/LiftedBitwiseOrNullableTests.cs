// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Tests.ExpressionCompiler.Lifted
{
    public static unsafe class LiftedBitwiseOrNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckLiftedBitwiseOrNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseOrNullableByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedBitwiseOrNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseOrNullableInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedBitwiseOrNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseOrNullableLong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedBitwiseOrNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseOrNullableSByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedBitwiseOrNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseOrNullableShort(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedBitwiseOrNullableUIntTest()
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseOrNullableUInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedBitwiseOrNullableULongTest()
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseOrNullableULong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedBitwiseOrNullableUShortTest()
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyBitwiseOrNullableUShort(values[i], values[j]);
                }
            }
        }

        #endregion

        #region Helpers

        public static byte OrNullableByte(byte a, byte b)
        {
            return (byte)(a | b);
        }

        public static int OrNullableInt(int a, int b)
        {
            return (int)(a | b);
        }

        public static long OrNullableLong(long a, long b)
        {
            return (long)(a | b);
        }

        public static sbyte OrNullableSByte(sbyte a, sbyte b)
        {
            return (sbyte)(a | b);
        }

        public static short OrNullableShort(short a, short b)
        {
            return (short)(a | b);
        }

        public static uint OrNullableUInt(uint a, uint b)
        {
            return (uint)(a | b);
        }

        public static ulong OrNullableULong(ulong a, ulong b)
        {
            return (ulong)(a | b);
        }

        public static ushort OrNullableUShort(ushort a, ushort b)
        {
            return (ushort)(a | b);
        }

        #endregion

        #region Test verifiers

        private static void VerifyBitwiseOrNullableByte(byte? a, byte? b)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Or(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedBitwiseOrNullableTests).GetTypeInfo().GetDeclaredMethod("OrNullableByte")));
            Func<byte?> f = e.Compile();

            byte? result = default(byte);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            byte? expected = default(byte);
            Exception csEx = null;
            try
            {
                expected = (byte?)(a | b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyBitwiseOrNullableInt(int? a, int? b)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Or(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedBitwiseOrNullableTests).GetTypeInfo().GetDeclaredMethod("OrNullableInt")));
            Func<int?> f = e.Compile();

            int? result = default(int);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            int? expected = default(int);
            Exception csEx = null;
            try
            {
                expected = (int?)(a | b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyBitwiseOrNullableLong(long? a, long? b)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Or(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedBitwiseOrNullableTests).GetTypeInfo().GetDeclaredMethod("OrNullableLong")));
            Func<long?> f = e.Compile();

            long? result = default(long);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            long? expected = default(long);
            Exception csEx = null;
            try
            {
                expected = (long?)(a | b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyBitwiseOrNullableSByte(sbyte? a, sbyte? b)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Or(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedBitwiseOrNullableTests).GetTypeInfo().GetDeclaredMethod("OrNullableSByte")));
            Func<sbyte?> f = e.Compile();

            sbyte? result = default(sbyte);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            sbyte? expected = default(sbyte);
            Exception csEx = null;
            try
            {
                expected = (sbyte?)(a | b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyBitwiseOrNullableShort(short? a, short? b)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Or(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedBitwiseOrNullableTests).GetTypeInfo().GetDeclaredMethod("OrNullableShort")));
            Func<short?> f = e.Compile();

            short? result = default(short);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            short? expected = default(short);
            Exception csEx = null;
            try
            {
                expected = (short?)(a | b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyBitwiseOrNullableUInt(uint? a, uint? b)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Or(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedBitwiseOrNullableTests).GetTypeInfo().GetDeclaredMethod("OrNullableUInt")));
            Func<uint?> f = e.Compile();

            uint? result = default(uint);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            uint? expected = default(uint);
            Exception csEx = null;
            try
            {
                expected = (uint?)(a | b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyBitwiseOrNullableULong(ulong? a, ulong? b)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Or(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedBitwiseOrNullableTests).GetTypeInfo().GetDeclaredMethod("OrNullableULong")));
            Func<ulong?> f = e.Compile();

            ulong? result = default(ulong);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            ulong? expected = default(ulong);
            Exception csEx = null;
            try
            {
                expected = (ulong?)(a | b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyBitwiseOrNullableUShort(ushort? a, ushort? b)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Or(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedBitwiseOrNullableTests).GetTypeInfo().GetDeclaredMethod("OrNullableUShort")));
            Func<ushort?> f = e.Compile();

            ushort? result = default(ushort);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            ushort? expected = default(ushort);
            Exception csEx = null;
            try
            {
                expected = (ushort?)(a | b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        #endregion
    }
}
