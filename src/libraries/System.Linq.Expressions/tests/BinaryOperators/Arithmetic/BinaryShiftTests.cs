// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryShiftTests
    {
        #region Test methods

        [Fact]
        public static void CheckByteShiftTest()
        {
            byte[] array = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyByteShift(array[i]);
            }
        }

        [Fact]
        public static void CheckSByteShiftTest()
        {
            sbyte[] array = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifySByteShift(array[i]);
            }
        }

        [Fact]
        public static void CheckUShortShiftTest()
        {
            ushort[] array = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyUShortShift(array[i]);
            }
        }

        [Fact]
        public static void CheckShortShiftTest()
        {
            short[] array = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyShortShift(array[i]);
            }
        }

        [Fact]
        public static void CheckUIntShiftTest()
        {
            uint[] array = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyUIntShift(array[i]);
            }
        }

        [Fact]
        public static void CheckIntShiftTest()
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntShift(array[i]);
            }
        }

        [Fact]
        public static void CheckULongShiftTest()
        {
            ulong[] array = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyULongShift(array[i]);
            }
        }

        [Fact]
        public static void CheckLongShiftTest()
        {
            long[] array = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyLongShift(array[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static int[] s_shifts = new[] { int.MinValue, -1, 0, 1, 2, 31, 32, 63, 64, int.MaxValue };

        private static void VerifyByteShift(byte a)
        {
            foreach (var b in s_shifts)
            {
                VerifyByteShift(a, b, true);
                VerifyByteShift(a, b, false);
            }
        }

        private static void VerifyByteShift(byte a, int b, bool left)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(byte)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(byte)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<byte> f = e.Compile();

            // shift with expression tree
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

            // shift with real IL
            byte csResult = default(byte);
            Exception csException = null;
            try
            {
                csResult = (byte)(left ? a << b : a >> b);
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

        private static void VerifySByteShift(sbyte a)
        {
            foreach (var b in s_shifts)
            {
                VerifySByteShift(a, b, true);
                VerifySByteShift(a, b, false);
            }
        }

        private static void VerifySByteShift(sbyte a, int b, bool left)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(sbyte)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(sbyte)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<sbyte> f = e.Compile();

            // shift with expression tree
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

            // shift with real IL
            sbyte csResult = default(sbyte);
            Exception csException = null;
            try
            {
                csResult = (sbyte)(left ? a << b : a >> b);
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

        private static void VerifyUShortShift(ushort a)
        {
            foreach (var b in s_shifts)
            {
                VerifyUShortShift(a, b, true);
                VerifyUShortShift(a, b, false);
            }
        }

        private static void VerifyUShortShift(ushort a, int b, bool left)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<ushort> f = e.Compile();

            // shift with expression tree
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

            // shift with real IL
            ushort csResult = default(ushort);
            Exception csException = null;
            try
            {
                csResult = (ushort)(left ? a << b : a >> b);
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

        private static void VerifyShortShift(short a)
        {
            foreach (var b in s_shifts)
            {
                VerifyShortShift(a, b, true);
                VerifyShortShift(a, b, false);
            }
        }

        private static void VerifyShortShift(short a, int b, bool left)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<short> f = e.Compile();

            // shift with expression tree
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

            // shift with real IL
            short csResult = default(short);
            Exception csException = null;
            try
            {
                csResult = (short)(left ? a << b : a >> b);
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

        private static void VerifyUIntShift(uint a)
        {
            foreach (var b in s_shifts)
            {
                VerifyUIntShift(a, b, true);
                VerifyUIntShift(a, b, false);
            }
        }

        private static void VerifyUIntShift(uint a, int b, bool left)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<uint> f = e.Compile();

            // shift with expression tree
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

            // shift with real IL
            uint csResult = default(uint);
            Exception csException = null;
            try
            {
                csResult = (uint)(left ? a << b : a >> b);
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

        private static void VerifyIntShift(int a)
        {
            foreach (var b in s_shifts)
            {
                VerifyIntShift(a, b, true);
                VerifyIntShift(a, b, false);
            }
        }

        private static void VerifyIntShift(int a, int b, bool left)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<int> f = e.Compile();

            // shift with expression tree
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

            // shift with real IL
            int csResult = default(int);
            Exception csException = null;
            try
            {
                csResult = (int)(left ? a << b : a >> b);
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

        private static void VerifyULongShift(ulong a)
        {
            foreach (var b in s_shifts)
            {
                VerifyULongShift(a, b, true);
                VerifyULongShift(a, b, false);
            }
        }

        private static void VerifyULongShift(ulong a, int b, bool left)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<ulong> f = e.Compile();

            // shift with expression tree
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

            // shift with real IL
            ulong csResult = default(ulong);
            Exception csException = null;
            try
            {
                csResult = (ulong)(left ? a << b : a >> b);
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

        private static void VerifyLongShift(long a)
        {
            foreach (var b in s_shifts)
            {
                VerifyLongShift(a, b, true);
                VerifyLongShift(a, b, false);
            }
        }

        private static void VerifyLongShift(long a, int b, bool left)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<long> f = e.Compile();

            // shift with expression tree
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

            // shift with real IL
            long csResult = default(long);
            Exception csException = null;
            try
            {
                csResult = (long)(left ? a << b : a >> b);
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

        [Fact]
        public static void CannotReduceLeft()
        {
            Expression exp = Expression.LeftShift(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void CannotReduceRight()
        {
            Expression exp = Expression.RightShift(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void LeftThrowsOnLeftNull()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.LeftShift(null, Expression.Constant("")));
        }

        [Fact]
        public static void LeftThrowsOnRightNull()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.LeftShift(Expression.Constant(""), null));
        }

        [Fact]
        public static void RightThrowsOnLeftNull()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.RightShift(null, Expression.Constant("")));
        }

        [Fact]
        public static void RightThrowsOnRightNull()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.RightShift(Expression.Constant(""), null));
        }

        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        [Fact]
        public static void LeftThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("left", () => Expression.LeftShift(value, Expression.Constant(1)));
        }

        [Fact]
        public static void LeftThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("right", () => Expression.LeftShift(Expression.Constant(1), value));
        }

        [Fact]
        public static void RightThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("left", () => Expression.RightShift(value, Expression.Constant(1)));
        }

        [Fact]
        public static void RightThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("right", () => Expression.RightShift(Expression.Constant(1), value));
        }
    }
}
