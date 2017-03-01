// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class OnesComplementNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableShortTest(bool useInterpreter)
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableShort(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableUShortTest(bool useInterpreter)
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableUShort(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableIntTest(bool useInterpreter)
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableInt(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableUIntTest(bool useInterpreter)
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableUInt(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableLongTest(bool useInterpreter)
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableLong(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableULongTest(bool useInterpreter)
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableULong(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableByteTest(bool useInterpreter)
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableByte(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableSByteTest(bool useInterpreter)
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableSByte(values[i], useInterpreter);
            }
        }

        [Fact]
        public static void CheckUnaryArithmeticOnesComplementNullableBooleanTest()
        {
            Expression operand = Expression.Variable(typeof(bool?));
            Assert.Throws<InvalidOperationException>(() => Expression.OnesComplement(operand));
        }

        #endregion

        #region Test verifiers

        private static void VerifyArithmeticOnesComplementNullableShort(short? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);
            Assert.Equal((short?)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementNullableUShort(ushort? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((ushort?)(~value)), f());
        }

        private static void VerifyArithmeticOnesComplementNullableInt(int? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);
            Assert.Equal((int?)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementNullableUInt(uint? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);
            Assert.Equal((uint?)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementNullableLong(long? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);
            Assert.Equal((long?)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementNullableULong(ulong? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);
            Assert.Equal((ulong?)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementNullableByte(byte? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((byte?)(~value)), f());
        }

        private static void VerifyArithmeticOnesComplementNullableSByte(sbyte? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);
            Assert.Equal((sbyte?)(~value), f());
        }

        #endregion
    }
}
