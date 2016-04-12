// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryNullableCoalesceTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableBoolCoalesceTest(bool useInterpreter)
        {
            bool?[] array1 = new bool?[] { null, true, false };
            bool?[] array2 = new bool?[] { null, true, false };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableBoolCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableByteCoalesceTest(bool useInterpreter)
        {
            byte?[] array1 = new byte?[] { null, 0, 1, byte.MaxValue };
            byte?[] array2 = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableByteCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableCharCoalesceTest(bool useInterpreter)
        {
            char?[] array1 = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            char?[] array2 = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableCharCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDecimalCoalesceTest(bool useInterpreter)
        {
            decimal?[] array1 = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            decimal?[] array2 = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableDecimalCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDoubleCoalesceTest(bool useInterpreter)
        {
            double?[] array1 = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            double?[] array2 = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableDoubleCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableEnumCoalesceTest(bool useInterpreter)
        {
            E?[] array1 = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            E?[] array2 = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableEnumCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableEnumLongCoalesceTest(bool useInterpreter)
        {
            El?[] array1 = new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            El?[] array2 = new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableEnumLongCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableFloatCoalesceTest(bool useInterpreter)
        {
            float?[] array1 = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            float?[] array2 = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableFloatCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntCoalesceTest(bool useInterpreter)
        {
            int?[] array1 = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            int?[] array2 = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableIntCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableLongCoalesceTest(bool useInterpreter)
        {
            long?[] array1 = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            long?[] array2 = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableLongCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructCoalesceTest(bool useInterpreter)
        {
            S?[] array1 = new S?[] { null, default(S), new S() };
            S?[] array2 = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableStructCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableSByteCoalesceTest(bool useInterpreter)
        {
            sbyte?[] array1 = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            sbyte?[] array2 = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableSByteCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithStringCoalesceTest(bool useInterpreter)
        {
            Sc?[] array1 = new Sc?[] { null, default(Sc), new Sc(), new Sc(null) };
            Sc?[] array2 = new Sc?[] { null, default(Sc), new Sc(), new Sc(null) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableStructWithStringCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithStringAndFieldCoalesceTest(bool useInterpreter)
        {
            Scs?[] array1 = new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) };
            Scs?[] array2 = new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableStructWithStringAndFieldCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableShortCoalesceTest(bool useInterpreter)
        {
            short?[] array1 = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            short?[] array2 = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableShortCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithTwoValuesCoalesceTest(bool useInterpreter)
        {
            Sp?[] array1 = new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) };
            Sp?[] array2 = new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableStructWithTwoValuesCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithValueCoalesceTest(bool useInterpreter)
        {
            Ss?[] array1 = new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) };
            Ss?[] array2 = new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableStructWithValueCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUIntCoalesceTest(bool useInterpreter)
        {
            uint?[] array1 = new uint?[] { null, 0, 1, uint.MaxValue };
            uint?[] array2 = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableUIntCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableULongCoalesceTest(bool useInterpreter)
        {
            ulong?[] array1 = new ulong?[] { null, 0, 1, ulong.MaxValue };
            ulong?[] array2 = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableULongCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUShortCoalesceTest(bool useInterpreter)
        {
            ushort?[] array1 = new ushort?[] { null, 0, 1, ushort.MaxValue };
            ushort?[] array2 = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableUShortCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericEnumWithStructRestrictionCoalesceTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCoalesceHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStructRestrictionCoalesceTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCoalesceHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStringAndFieldWithStructRestrictionCoalesceTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCoalesceHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckGenericWithStructRestrictionCoalesceHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts?[] array1 = new Ts?[] { null, default(Ts), new Ts() };
            Ts?[] array2 = new Ts?[] { null, default(Ts), new Ts() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyGenericWithStructRestrictionCoalesce<Ts>(array1[i], array2[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableBoolCoalesce(bool? a, bool? b, bool useInterpreter)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(bool?)),
                        Expression.Constant(b, typeof(bool?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableByteCoalesce(byte? a, byte? b, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableCharCoalesce(char? a, char? b, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableDecimalCoalesce(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableDoubleCoalesce(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableEnumCoalesce(E? a, E? b, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(E?)),
                        Expression.Constant(b, typeof(E?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableEnumLongCoalesce(El? a, El? b, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(El?)),
                        Expression.Constant(b, typeof(El?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableFloatCoalesce(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableIntCoalesce(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableLongCoalesce(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableStructCoalesce(S? a, S? b, bool useInterpreter)
        {
            Expression<Func<S?>> e =
                Expression.Lambda<Func<S?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(S?)),
                        Expression.Constant(b, typeof(S?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableSByteCoalesce(sbyte? a, sbyte? b, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableStructWithStringCoalesce(Sc? a, Sc? b, bool useInterpreter)
        {
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Sc?)),
                        Expression.Constant(b, typeof(Sc?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableStructWithStringAndFieldCoalesce(Scs? a, Scs? b, bool useInterpreter)
        {
            Expression<Func<Scs?>> e =
                Expression.Lambda<Func<Scs?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Scs?)),
                        Expression.Constant(b, typeof(Scs?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableShortCoalesce(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableStructWithTwoValuesCoalesce(Sp? a, Sp? b, bool useInterpreter)
        {
            Expression<Func<Sp?>> e =
                Expression.Lambda<Func<Sp?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Sp?)),
                        Expression.Constant(b, typeof(Sp?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableStructWithValueCoalesce(Ss? a, Ss? b, bool useInterpreter)
        {
            Expression<Func<Ss?>> e =
                Expression.Lambda<Func<Ss?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Ss?)),
                        Expression.Constant(b, typeof(Ss?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableUIntCoalesce(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableULongCoalesce(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyNullableUShortCoalesce(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyGenericWithStructRestrictionCoalesce<Ts>(Ts? a, Ts? b, bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts?>> e =
                Expression.Lambda<Func<Ts?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Ts?)),
                        Expression.Constant(b, typeof(Ts?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        #endregion
    }
}
