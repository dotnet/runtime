// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Binary
{
    public static unsafe class BinaryExclusiveOrTests
    {
        #region Test methods

        [Fact]
        public static void CheckByteExclusiveOrTest()
        {
            byte[] array = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyByteExclusiveOr(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckSByteExclusiveOrTest()
        {
            sbyte[] array = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifySByteExclusiveOr(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckUShortExclusiveOrTest()
        {
            ushort[] array = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyUShortExclusiveOr(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckShortExclusiveOrTest()
        {
            short[] array = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyShortExclusiveOr(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckUIntExclusiveOrTest()
        {
            uint[] array = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyUIntExclusiveOr(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckIntExclusiveOrTest()
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyIntExclusiveOr(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckULongExclusiveOrTest()
        {
            ulong[] array = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyULongExclusiveOr(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckLongExclusiveOrTest()
        {
            long[] array = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyLongExclusiveOr(array[i], array[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyByteExclusiveOr(byte a, byte b)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(byte)),
                        Expression.Constant(b, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile();

            // compute with expression tree
            byte etResult = default(byte);
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
            byte csResult = default(byte);
            Exception csException = null;
            try
            {
                csResult = (byte)(a ^ b);
            }
            catch (Exception ex)
            {
                csException = ex;
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

        private static void VerifySByteExclusiveOr(sbyte a, sbyte b)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(sbyte)),
                        Expression.Constant(b, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile();

            // compute with expression tree
            sbyte etResult = default(sbyte);
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
            sbyte csResult = default(sbyte);
            Exception csException = null;
            try
            {
                csResult = (sbyte)(a ^ b);
            }
            catch (Exception ex)
            {
                csException = ex;
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

        private static void VerifyUShortExclusiveOr(ushort a, ushort b)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile();

            // compute with expression tree
            ushort etResult = default(ushort);
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
            ushort csResult = default(ushort);
            Exception csException = null;
            try
            {
                csResult = (ushort)(a ^ b);
            }
            catch (Exception ex)
            {
                csException = ex;
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

        private static void VerifyShortExclusiveOr(short a, short b)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile();

            // compute with expression tree
            short etResult = default(short);
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
            short csResult = default(short);
            Exception csException = null;
            try
            {
                csResult = (short)(a ^ b);
            }
            catch (Exception ex)
            {
                csException = ex;
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

        private static void VerifyUIntExclusiveOr(uint a, uint b)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile();

            // compute with expression tree
            uint etResult = default(uint);
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
            uint csResult = default(uint);
            Exception csException = null;
            try
            {
                csResult = (uint)(a ^ b);
            }
            catch (Exception ex)
            {
                csException = ex;
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

        private static void VerifyIntExclusiveOr(int a, int b)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            // compute with expression tree
            int etResult = default(int);
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
            int csResult = default(int);
            Exception csException = null;
            try
            {
                csResult = (int)(a ^ b);
            }
            catch (Exception ex)
            {
                csException = ex;
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

        private static void VerifyULongExclusiveOr(ulong a, ulong b)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile();

            // compute with expression tree
            ulong etResult = default(ulong);
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
            ulong csResult = default(ulong);
            Exception csException = null;
            try
            {
                csResult = (ulong)(a ^ b);
            }
            catch (Exception ex)
            {
                csException = ex;
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

        private static void VerifyLongExclusiveOr(long a, long b)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ExclusiveOr(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile();

            // compute with expression tree
            long etResult = default(long);
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
            long csResult = default(long);
            Exception csException = null;
            try
            {
                csResult = (long)(a ^ b);
            }
            catch (Exception ex)
            {
                csException = ex;
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
    }
}
