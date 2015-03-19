// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Constant
{
    public static unsafe class ConstantNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckNullableBoolConstantTest()
        {
            foreach (bool? value in new bool?[] { null, true, false })
            {
                VerifyNullableBoolConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableByteConstantTest()
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableCharConstantTest()
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableDecimalConstantTest()
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableDoubleConstantTest()
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableEnumConstantTest()
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableEnumLongConstantTest()
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableFloatConstantTest()
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableIntConstantTest()
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableLongConstantTest()
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableStructConstantTest()
        {
            foreach (S? value in new S?[] { null, default(S), new S() })
            {
                VerifyNullableStructConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableSByteConstantTest()
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableStructWithStringConstantTest()
        {
            foreach (Sc? value in new Sc?[] { null, default(Sc), new Sc(), new Sc(null) })
            {
                VerifyNullableStructWithStringConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableStructWithStringAndFieldConstantTest()
        {
            foreach (Scs? value in new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) })
            {
                VerifyNullableStructWithStringAndFieldConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableShortConstantTest()
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableStructWithTwoValuesConstantTest()
        {
            foreach (Sp? value in new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) })
            {
                VerifyNullableStructWithTwoValuesConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableStructWithValueConstantTest()
        {
            foreach (Ss? value in new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) })
            {
                VerifyNullableStructWithValueConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableUIntConstantTest()
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableULongConstantTest()
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableUShortConstantTest()
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortConstant(value);
            }
        }

        [Fact]
        public static void CheckNullableGenericWithStructRestrictionWithEnumConstantTest()
        {
            CheckNullableGenericWithStructRestrictionConstantHelper<E>();
        }

        [Fact]
        public static void CheckNullableGenericWithStructRestrictionWithStructConstantTest()
        {
            CheckNullableGenericWithStructRestrictionConstantHelper<S>();
        }

        [Fact]
        public static void CheckNullableGenericWithStructRestrictionWithStructWithStringAndValueConstantTest()
        {
            CheckNullableGenericWithStructRestrictionConstantHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        public static void CheckNullableGenericWithStructRestrictionConstantHelper<Ts>() where Ts : struct
        {
            foreach (Ts? value in new Ts?[] { null, default(Ts), new Ts() })
            {
                VerifyNullableGenericWithStructRestriction<Ts>(value);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableBoolConstant(bool? value)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Constant(value, typeof(bool?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableByteConstant(byte? value)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Constant(value, typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableCharConstant(char? value)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Constant(value, typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableDecimalConstant(decimal? value)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Constant(value, typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableDoubleConstant(double? value)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Constant(value, typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableEnumConstant(E? value)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Constant(value, typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableEnumLongConstant(El? value)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Constant(value, typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableFloatConstant(float? value)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Constant(value, typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableIntConstant(int? value)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Constant(value, typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableLongConstant(long? value)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Constant(value, typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructConstant(S? value)
        {
            Expression<Func<S?>> e =
                Expression.Lambda<Func<S?>>(
                    Expression.Constant(value, typeof(S?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableSByteConstant(sbyte? value)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Constant(value, typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructWithStringConstant(Sc? value)
        {
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.Constant(value, typeof(Sc?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructWithStringAndFieldConstant(Scs? value)
        {
            Expression<Func<Scs?>> e =
                Expression.Lambda<Func<Scs?>>(
                    Expression.Constant(value, typeof(Scs?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableShortConstant(short? value)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Constant(value, typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructWithTwoValuesConstant(Sp? value)
        {
            Expression<Func<Sp?>> e =
                Expression.Lambda<Func<Sp?>>(
                    Expression.Constant(value, typeof(Sp?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructWithValueConstant(Ss? value)
        {
            Expression<Func<Ss?>> e =
                Expression.Lambda<Func<Ss?>>(
                    Expression.Constant(value, typeof(Ss?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableUIntConstant(uint? value)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Constant(value, typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableULongConstant(ulong? value)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Constant(value, typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableUShortConstant(ushort? value)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Constant(value, typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyNullableGenericWithStructRestriction<Ts>(Ts? value) where Ts : struct
        {
            Expression<Func<Ts?>> e =
                Expression.Lambda<Func<Ts?>>(
                    Expression.Constant(value, typeof(Ts?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f = e.Compile();
            Assert.Equal(value, f());
        }

        #endregion
    }
}
