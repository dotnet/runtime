// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ConstantNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableBoolConstantTest(bool useInterpreter)
        {
            foreach (bool? value in new bool?[] { null, true, false })
            {
                VerifyNullableBoolConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableByteConstantTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableCharConstantTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDecimalConstantTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDoubleConstantTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableEnumConstantTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableEnumLongConstantTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableFloatConstantTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntConstantTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableLongConstantTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructConstantTest(bool useInterpreter)
        {
            foreach (S? value in new S?[] { null, default(S), new S() })
            {
                VerifyNullableStructConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableSByteConstantTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithStringConstantTest(bool useInterpreter)
        {
            foreach (Sc? value in new Sc?[] { null, default(Sc), new Sc(), new Sc(null) })
            {
                VerifyNullableStructWithStringConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithStringAndFieldConstantTest(bool useInterpreter)
        {
            foreach (Scs? value in new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) })
            {
                VerifyNullableStructWithStringAndFieldConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableShortConstantTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithTwoValuesConstantTest(bool useInterpreter)
        {
            foreach (Sp? value in new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) })
            {
                VerifyNullableStructWithTwoValuesConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithValueConstantTest(bool useInterpreter)
        {
            foreach (Ss? value in new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) })
            {
                VerifyNullableStructWithValueConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUIntConstantTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableULongConstantTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUShortConstantTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableGenericWithStructRestrictionWithEnumConstantTest(bool useInterpreter)
        {
            CheckNullableGenericWithStructRestrictionConstantHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableGenericWithStructRestrictionWithStructConstantTest(bool useInterpreter)
        {
            CheckNullableGenericWithStructRestrictionConstantHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableGenericWithStructRestrictionWithStructWithStringAndValueConstantTest(bool useInterpreter)
        {
            CheckNullableGenericWithStructRestrictionConstantHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        public static void CheckNullableGenericWithStructRestrictionConstantHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            foreach (Ts? value in new Ts?[] { null, default(Ts), new Ts() })
            {
                VerifyNullableGenericWithStructRestriction<Ts>(value, useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableBoolConstant(bool? value, bool useInterpreter)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Constant(value, typeof(bool?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableByteConstant(byte? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Constant(value, typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableCharConstant(char? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Constant(value, typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableDecimalConstant(decimal? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Constant(value, typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableDoubleConstant(double? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Constant(value, typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableEnumConstant(E? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Constant(value, typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableEnumLongConstant(El? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Constant(value, typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableFloatConstant(float? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Constant(value, typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableIntConstant(int? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Constant(value, typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableLongConstant(long? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Constant(value, typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructConstant(S? value, bool useInterpreter)
        {
            Expression<Func<S?>> e =
                Expression.Lambda<Func<S?>>(
                    Expression.Constant(value, typeof(S?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableSByteConstant(sbyte? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Constant(value, typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructWithStringConstant(Sc? value, bool useInterpreter)
        {
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.Constant(value, typeof(Sc?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructWithStringAndFieldConstant(Scs? value, bool useInterpreter)
        {
            Expression<Func<Scs?>> e =
                Expression.Lambda<Func<Scs?>>(
                    Expression.Constant(value, typeof(Scs?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableShortConstant(short? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Constant(value, typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructWithTwoValuesConstant(Sp? value, bool useInterpreter)
        {
            Expression<Func<Sp?>> e =
                Expression.Lambda<Func<Sp?>>(
                    Expression.Constant(value, typeof(Sp?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructWithValueConstant(Ss? value, bool useInterpreter)
        {
            Expression<Func<Ss?>> e =
                Expression.Lambda<Func<Ss?>>(
                    Expression.Constant(value, typeof(Ss?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableUIntConstant(uint? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Constant(value, typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableULongConstant(ulong? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Constant(value, typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableUShortConstant(ushort? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Constant(value, typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableGenericWithStructRestriction<Ts>(Ts? value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts?>> e =
                Expression.Lambda<Func<Ts?>>(
                    Expression.Constant(value, typeof(Ts?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        #endregion
    }
}
