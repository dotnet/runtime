// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class OnesComplementTests
    {
        #region Test methods

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementShortTest()
        {
            short[] values = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementShort(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementUShortTest()
        {
            ushort[] values = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementUShort(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementIntTest()
        {
            int[] values = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementInt(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementUIntTest()
        {
            uint[] values = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementUInt(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementLongTest()
        {
            long[] values = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementLong(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementULongTest()
        {
            ulong[] values = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementULong(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementByteTest()
        {
            byte[] values = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementByte(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementSByteTest()
        {
            sbyte[] values = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementSByte(values[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyArithmeticOnesComplementShort(short value)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile();
            Assert.Equal((short)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementUShort(ushort value)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile();
            Assert.Equal((ushort)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementInt(int value)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal((int)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementUInt(uint value)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile();
            Assert.Equal((uint)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementLong(long value)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile();
            Assert.Equal((long)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementULong(ulong value)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile();
            Assert.Equal((ulong)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementByte(byte value)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile();
            Assert.Equal((byte)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementSByte(sbyte value)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile();
            Assert.Equal((sbyte)(~value), f());
        }

        #endregion
    }
}
