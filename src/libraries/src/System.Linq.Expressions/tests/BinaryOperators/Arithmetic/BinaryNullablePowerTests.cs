// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Tests.ExpressionCompiler.Binary
{
    public static unsafe class BinaryNullablePowerTests
    {
        #region Test methods

        [Fact]
        public static void CheckNullableBytePowerTest()
        {
            byte?[] array = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableBytePower(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableSBytePowerTest()
        {
            sbyte?[] array = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableSBytePower(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableUShortPowerTest()
        {
            ushort?[] array = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUShortPower(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableShortPowerTest()
        {
            short?[] array = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableShortPower(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableUIntPowerTest()
        {
            uint?[] array = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUIntPower(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableIntPowerTest()
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableIntPower(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableULongPowerTest()
        {
            ulong?[] array = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableULongPower(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableLongPowerTest()
        {
            long?[] array = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableLongPower(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableFloatPowerTest()
        {
            float?[] array = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableFloatPower(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableDoublePowerTest()
        {
            double?[] array = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDoublePower(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableDecimalPowerTest()
        {
            decimal?[] array = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDecimalPower(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableCharPowerTest()
        {
            char?[] array = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableCharPower(array[i], array[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableBytePower(byte? a, byte? b)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerByte")
                    ));
            Func<byte?> f = e.Compile();

            // compute with expression tree
            byte? etResult = default(byte);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            byte? csResult = default(byte);
            Exception csException = null;
            if (a == null || b == null)
            {
                csResult = null;
            }
            else
            {
                try
                {
                    csResult = (byte?)Math.Pow((double)a, (double)b);
                }
                catch (Exception ex)
                {
                    csException = ex;
                }
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableSBytePower(sbyte? a, sbyte? b)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerSByte")
                    ));
            Func<sbyte?> f = e.Compile();

            // compute with expression tree
            sbyte? etResult = default(sbyte);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            sbyte? csResult = default(sbyte);
            Exception csException = null;
            if (a == null || b == null)
            {
                csResult = null;
            }
            else
            {
                try
                {
                    csResult = (sbyte?)Math.Pow((double)a, (double)b);
                }
                catch (Exception ex)
                {
                    csException = ex;
                }
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableUShortPower(ushort? a, ushort? b)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerUShort")
                    ));
            Func<ushort?> f = e.Compile();

            // compute with expression tree
            ushort? etResult = default(ushort);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            ushort? csResult = default(ushort);
            Exception csException = null;
            if (a == null || b == null)
            {
                csResult = null;
            }
            else
            {
                try
                {
                    csResult = (ushort?)Math.Pow((double)a, (double)b);
                }
                catch (Exception ex)
                {
                    csException = ex;
                }
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableShortPower(short? a, short? b)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerShort")
                    ));
            Func<short?> f = e.Compile();

            // compute with expression tree
            short? etResult = default(short);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            short? csResult = default(short);
            Exception csException = null;
            if (a == null || b == null)
            {
                csResult = null;
            }
            else
            {
                try
                {
                    csResult = (short?)Math.Pow((double)a, (double)b);
                }
                catch (Exception ex)
                {
                    csException = ex;
                }
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableUIntPower(uint? a, uint? b)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerUInt")
                    ));
            Func<uint?> f = e.Compile();

            // compute with expression tree
            uint? etResult = default(uint);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            uint? csResult = default(uint);
            Exception csException = null;
            if (a == null || b == null)
            {
                csResult = null;
            }
            else
            {
                try
                {
                    csResult = (uint?)Math.Pow((double)a, (double)b);
                }
                catch (Exception ex)
                {
                    csException = ex;
                }
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableIntPower(int? a, int? b)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerInt")
                    ));
            Func<int?> f = e.Compile();

            // compute with expression tree
            int? etResult = default(int);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            int? csResult = default(int);
            Exception csException = null;
            if (a == null || b == null)
            {
                csResult = null;
            }
            else
            {
                try
                {
                    csResult = (int?)Math.Pow((double)a, (double)b);
                }
                catch (Exception ex)
                {
                    csException = ex;
                }
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableULongPower(ulong? a, ulong? b)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerULong")
                    ));
            Func<ulong?> f = e.Compile();

            // compute with expression tree
            ulong? etResult = default(ulong);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            ulong? csResult = default(ulong);
            Exception csException = null;
            if (a == null || b == null)
            {
                csResult = null;
            }
            else
            {
                try
                {
                    csResult = (ulong?)Math.Pow((double)a, (double)b);
                }
                catch (Exception ex)
                {
                    csException = ex;
                }
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableLongPower(long? a, long? b)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerLong")
                    ));
            Func<long?> f = e.Compile();

            // compute with expression tree
            long? etResult = default(long);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            long? csResult = default(long);
            Exception csException = null;
            if (a == null || b == null)
            {
                csResult = null;
            }
            else
            {
                try
                {
                    csResult = (long?)Math.Pow((double)a, (double)b);
                }
                catch (Exception ex)
                {
                    csException = ex;
                }
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableFloatPower(float? a, float? b)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerFloat")
                    ));
            Func<float?> f = e.Compile();

            // compute with expression tree
            float? etResult = default(float);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            float? csResult = default(float);
            Exception csException = null;
            if (a == null || b == null)
            {
                csResult = null;
            }
            else
            {
                try
                {
                    csResult = (float?)Math.Pow((double)a, (double)b);
                }
                catch (Exception ex)
                {
                    csException = ex;
                }
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableDoublePower(double? a, double? b)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerDouble")
                    ));
            Func<double?> f = e.Compile();

            // compute with expression tree
            double? etResult = default(double);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            double? csResult = default(double);
            Exception csException = null;
            if (a == null || b == null)
            {
                csResult = null;
            }
            else
            {
                try
                {
                    csResult = (double?)Math.Pow((double)a, (double)b);
                }
                catch (Exception ex)
                {
                    csException = ex;
                }
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableDecimalPower(decimal? a, decimal? b)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerDecimal")
                    ));
            Func<decimal?> f = e.Compile();

            // compute with expression tree
            decimal? etResult = default(decimal);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            decimal? csResult = default(decimal);
            Exception csException = null;
            if (a == null || b == null)
            {
                csResult = null;
            }
            else
            {
                try
                {
                    csResult = (decimal?)Math.Pow((double)a, (double)b);
                }
                catch (Exception ex)
                {
                    csException = ex;
                }
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableCharPower(char? a, char? b)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerChar")
                    ));
            Func<char?> f = e.Compile();

            // compute with expression tree
            char? etResult = default(char);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            char? csResult = default(char);
            Exception csException = null;
            if (a == null || b == null)
            {
                csResult = null;
            }
            else
            {
                try
                {
                    csResult = (char?)Math.Pow((double)a, (double)b);
                }
                catch (Exception ex)
                {
                    csException = ex;
                }
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        #endregion

        #region Helper methods

        public static byte PowerByte(byte a, byte b)
        {
            return (byte)Math.Pow((double)a, (double)b);
        }

        public static sbyte PowerSByte(sbyte a, sbyte b)
        {
            return (sbyte)Math.Pow((double)a, (double)b);
        }

        public static ushort PowerUShort(ushort a, ushort b)
        {
            return (ushort)Math.Pow((double)a, (double)b);
        }

        public static short PowerShort(short a, short b)
        {
            return (short)Math.Pow((double)a, (double)b);
        }

        public static uint PowerUInt(uint a, uint b)
        {
            return (uint)Math.Pow((double)a, (double)b);
        }

        public static int PowerInt(int a, int b)
        {
            return (int)Math.Pow((double)a, (double)b);
        }

        public static ulong PowerULong(ulong a, ulong b)
        {
            return (ulong)Math.Pow((double)a, (double)b);
        }

        public static long PowerLong(long a, long b)
        {
            return (long)Math.Pow((double)a, (double)b);
        }

        public static float PowerFloat(float a, float b)
        {
            return (float)Math.Pow((double)a, (double)b);
        }

        public static double PowerDouble(double a, double b)
        {
            return (double)Math.Pow((double)a, (double)b);
        }

        public static decimal PowerDecimal(decimal a, decimal b)
        {
            return (decimal)Math.Pow((double)a, (double)b);
        }

        public static char PowerChar(char a, char b)
        {
            return (char)Math.Pow((double)a, (double)b);
        }

        #endregion
    }
}
