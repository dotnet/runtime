// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class OnesComplementTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementShortTest(bool useInterpreter)
        {
            short[] values = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementShort(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementUShortTest(bool useInterpreter)
        {
            ushort[] values = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementUShort(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementIntTest(bool useInterpreter)
        {
            int[] values = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementInt(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementUIntTest(bool useInterpreter)
        {
            uint[] values = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementUInt(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementLongTest(bool useInterpreter)
        {
            long[] values = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementLong(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementULongTest(bool useInterpreter)
        {
            ulong[] values = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementULong(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementByteTest(bool useInterpreter)
        {
            byte[] values = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementByte(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementSByteTest(bool useInterpreter)
        {
            sbyte[] values = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementSByte(values[i], useInterpreter);
            }
        }

        [Fact]
        public static void CheckUnaryArithmeticOnesComplementBooleanTest()
        {
            Expression operand = Expression.Variable(typeof(bool));
            Assert.Throws<InvalidOperationException>(() => Expression.OnesComplement(operand));
        }

        [Fact]
        public static void ToStringTest()
        {
            UnaryExpression e = Expression.OnesComplement(Expression.Parameter(typeof(int), "x"));
            Assert.Equal("~(x)", e.ToString());
        }

        #endregion

        #region Test verifiers

        private static void VerifyArithmeticOnesComplementShort(short value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);
            Assert.Equal((short)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementUShort(ushort value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((ushort)(~value)), f());
        }

        private static void VerifyArithmeticOnesComplementInt(int value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal((int)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementUInt(uint value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);
            Assert.Equal((uint)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementLong(long value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);
            Assert.Equal((long)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementULong(ulong value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);
            Assert.Equal((ulong)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementByte(byte value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((byte)(~value)), f());
        }

        private static void VerifyArithmeticOnesComplementSByte(sbyte value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);
            Assert.Equal((sbyte)(~value), f());
        }

        #endregion

        public static IEnumerable<object[]> Int32OnesComplements()
        {
            yield return new object[] { 0, ~0 };
            yield return new object[] { 1, ~1 };
            yield return new object[] { -1, ~-1 };
            yield return new object[] { int.MinValue, ~int.MinValue };
            yield return new object[] { int.MaxValue, ~int.MaxValue };
        }

        [Theory, PerCompilationType(nameof(Int32OnesComplements))]
        public static void MakeUnaryOnesComplement(int value, int expected, bool useInterpreter)
        {
            Expression<Func<int>> lambda = Expression.Lambda<Func<int>>(
                Expression.MakeUnary(ExpressionType.OnesComplement, Expression.Constant(value), null));
            Func<int> func = lambda.Compile(useInterpreter);
            Assert.Equal(expected, func());
        }

        [Fact]
        public static void OnesComplementNonIntegral()
        {
            Expression operand = Expression.Constant(2.0);
            Assert.Throws<InvalidOperationException>(() => Expression.OnesComplement(operand));
        }

        private class Complementary
        {
            public int Value { get; set; }

            public static Complementary operator ~(Complementary c) => new Complementary {Value = ~c.Value};

            public static Complementary OnesComplement(Complementary c) => new Complementary { Value = ~c.Value };
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CustomOperatorOnesComplement(bool useInterpreter)
        {
            Complementary value = new Complementary {Value = 43};
            Expression<Func<Complementary>> lambda = Expression.Lambda<Func<Complementary>>(
                Expression.OnesComplement(Expression.Constant(value)));
            Func<Complementary> func = lambda.Compile(useInterpreter);
            Assert.Equal(~43, func().Value);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ExplicitOperatorOnesComplement(bool useInterpreter)
        {
            Complementary value = new Complementary {Value = 43};
            MethodInfo method = typeof(Complementary).GetMethod(nameof(Complementary.OnesComplement));
            Expression<Func<Complementary>> lambda = Expression.Lambda<Func<Complementary>>(
                Expression.OnesComplement(Expression.Constant(value), method));
            Func<Complementary> func = lambda.Compile(useInterpreter);
            Assert.Equal(~43, func().Value);
        }
    }
}
