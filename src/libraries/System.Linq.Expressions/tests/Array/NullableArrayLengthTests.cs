// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Array
{
    public static unsafe class NullableArrayLengthTests
    {
        #region NullableBool tests

        [Fact]
        public static void CheckNullableBoolArrayLengthTest()
        {
            CheckNullableBoolArrayLengthExpression(GenerateNullableBoolArray(0));
            CheckNullableBoolArrayLengthExpression(GenerateNullableBoolArray(1));
            CheckNullableBoolArrayLengthExpression(GenerateNullableBoolArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableBoolArrayLengthTest()
        {
            CheckExceptionNullableBoolArrayLength(null);
        }

        #endregion

        #region NullableByte tests

        [Fact]
        public static void CheckNullableByteArrayLengthTest()
        {
            CheckNullableByteArrayLengthExpression(GenerateNullableByteArray(0));
            CheckNullableByteArrayLengthExpression(GenerateNullableByteArray(1));
            CheckNullableByteArrayLengthExpression(GenerateNullableByteArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableByteArrayLengthTest()
        {
            CheckExceptionNullableByteArrayLength(null);
        }

        #endregion

        #region NullableChar tests

        [Fact]
        public static void CheckNullableCharArrayLengthTest()
        {
            CheckNullableCharArrayLengthExpression(GenerateNullableCharArray(0));
            CheckNullableCharArrayLengthExpression(GenerateNullableCharArray(1));
            CheckNullableCharArrayLengthExpression(GenerateNullableCharArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableCharArrayLengthTest()
        {
            CheckExceptionNullableCharArrayLength(null);
        }

        #endregion

        #region NullableDecimal tests

        [Fact]
        public static void CheckNullableDecimalArrayLengthTest()
        {
            CheckNullableDecimalArrayLengthExpression(GenerateNullableDecimalArray(0));
            CheckNullableDecimalArrayLengthExpression(GenerateNullableDecimalArray(1));
            CheckNullableDecimalArrayLengthExpression(GenerateNullableDecimalArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableDecimalArrayLengthTest()
        {
            CheckExceptionNullableDecimalArrayLength(null);
        }

        #endregion

        #region NullableDouble tests

        [Fact]
        public static void CheckNullableDoubleArrayLengthTest()
        {
            CheckNullableDoubleArrayLengthExpression(GenerateNullableDoubleArray(0));
            CheckNullableDoubleArrayLengthExpression(GenerateNullableDoubleArray(1));
            CheckNullableDoubleArrayLengthExpression(GenerateNullableDoubleArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableDoubleArrayLengthTest()
        {
            CheckExceptionNullableDoubleArrayLength(null);
        }

        #endregion

        #region NullableEnum tests

        [Fact]
        public static void CheckNullableEnumArrayLengthTest()
        {
            CheckNullableEnumArrayLengthExpression(GenerateNullableEnumArray(0));
            CheckNullableEnumArrayLengthExpression(GenerateNullableEnumArray(1));
            CheckNullableEnumArrayLengthExpression(GenerateNullableEnumArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableEnumArrayLengthTest()
        {
            CheckExceptionNullableEnumArrayLength(null);
        }

        #endregion

        #region NullableEnumLong tests

        [Fact]
        public static void CheckNullableEnumLongArrayLengthTest()
        {
            CheckNullableEnumLongArrayLengthExpression(GenerateNullableEnumLongArray(0));
            CheckNullableEnumLongArrayLengthExpression(GenerateNullableEnumLongArray(1));
            CheckNullableEnumLongArrayLengthExpression(GenerateNullableEnumLongArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableEnumLongArrayLengthTest()
        {
            CheckExceptionNullableEnumLongArrayLength(null);
        }

        #endregion

        #region NullableFloat tests

        [Fact]
        public static void CheckNullableFloatArrayLengthTest()
        {
            CheckNullableFloatArrayLengthExpression(GenerateNullableFloatArray(0));
            CheckNullableFloatArrayLengthExpression(GenerateNullableFloatArray(1));
            CheckNullableFloatArrayLengthExpression(GenerateNullableFloatArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableFloatArrayLengthTest()
        {
            CheckExceptionNullableFloatArrayLength(null);
        }

        #endregion

        #region NullableInt tests

        [Fact]
        public static void CheckNullableIntArrayLengthTest()
        {
            CheckNullableIntArrayLengthExpression(GenerateNullableIntArray(0));
            CheckNullableIntArrayLengthExpression(GenerateNullableIntArray(1));
            CheckNullableIntArrayLengthExpression(GenerateNullableIntArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableIntArrayLengthTest()
        {
            CheckExceptionNullableIntArrayLength(null);
        }

        #endregion

        #region NullableLong tests

        [Fact]
        public static void CheckNullableLongArrayLengthTest()
        {
            CheckNullableLongArrayLengthExpression(GenerateNullableLongArray(0));
            CheckNullableLongArrayLengthExpression(GenerateNullableLongArray(1));
            CheckNullableLongArrayLengthExpression(GenerateNullableLongArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableLongArrayLengthTest()
        {
            CheckExceptionNullableLongArrayLength(null);
        }

        #endregion

        #region NullableSByte tests

        [Fact]
        public static void CheckNullableSByteArrayLengthTest()
        {
            CheckNullableSByteArrayLengthExpression(GenerateNullableSByteArray(0));
            CheckNullableSByteArrayLengthExpression(GenerateNullableSByteArray(1));
            CheckNullableSByteArrayLengthExpression(GenerateNullableSByteArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableSByteArrayLengthTest()
        {
            CheckExceptionNullableSByteArrayLength(null);
        }

        #endregion

        #region NullableStruct tests

        [Fact]
        public static void CheckNullableStructArrayLengthTest()
        {
            CheckNullableStructArrayLengthExpression(GenerateNullableStructArray(0));
            CheckNullableStructArrayLengthExpression(GenerateNullableStructArray(1));
            CheckNullableStructArrayLengthExpression(GenerateNullableStructArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableStructArrayLengthTest()
        {
            CheckExceptionNullableStructArrayLength(null);
        }

        #endregion

        #region NullableStructWithString tests

        [Fact]
        public static void CheckNullableStructWithStringArrayLengthTest()
        {
            CheckNullableStructWithStringArrayLengthExpression(GenerateNullableStructWithStringArray(0));
            CheckNullableStructWithStringArrayLengthExpression(GenerateNullableStructWithStringArray(1));
            CheckNullableStructWithStringArrayLengthExpression(GenerateNullableStructWithStringArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableStructWithStringArrayLengthTest()
        {
            CheckExceptionNullableStructWithStringArrayLength(null);
        }

        #endregion

        #region NullableStructWithStringAndValue tests

        [Fact]
        public static void CheckNullableStructWithStringAndValueArrayLengthTest()
        {
            CheckNullableStructWithStringAndValueArrayLengthExpression(GenerateNullableStructWithStringAndValueArray(0));
            CheckNullableStructWithStringAndValueArrayLengthExpression(GenerateNullableStructWithStringAndValueArray(1));
            CheckNullableStructWithStringAndValueArrayLengthExpression(GenerateNullableStructWithStringAndValueArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableStructWithStringAndValueArrayLengthTest()
        {
            CheckExceptionNullableStructWithStringAndValueArrayLength(null);
        }

        #endregion

        #region NullableStructWithTwoParameters tests

        [Fact]
        public static void CheckNullableStructWithTwoParametersArrayLengthTest()
        {
            CheckNullableStructWithTwoParametersArrayLengthExpression(GenerateNullableStructWithTwoParametersArray(0));
            CheckNullableStructWithTwoParametersArrayLengthExpression(GenerateNullableStructWithTwoParametersArray(1));
            CheckNullableStructWithTwoParametersArrayLengthExpression(GenerateNullableStructWithTwoParametersArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableStructWithTwoParametersArrayLengthTest()
        {
            CheckExceptionNullableStructWithTwoParametersArrayLength(null);
        }

        #endregion

        #region NullableStructWithValue tests

        [Fact]
        public static void CheckNullableStructWithValueArrayLengthTest()
        {
            CheckNullableStructWithValueArrayLengthExpression(GenerateNullableStructWithValueArray(0));
            CheckNullableStructWithValueArrayLengthExpression(GenerateNullableStructWithValueArray(1));
            CheckNullableStructWithValueArrayLengthExpression(GenerateNullableStructWithValueArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableStructWithValueArrayLengthTest()
        {
            CheckExceptionNullableStructWithValueArrayLength(null);
        }

        #endregion

        #region NullableShort tests

        [Fact]
        public static void CheckNullableShortArrayLengthTest()
        {
            CheckNullableShortArrayLengthExpression(GenerateNullableShortArray(0));
            CheckNullableShortArrayLengthExpression(GenerateNullableShortArray(1));
            CheckNullableShortArrayLengthExpression(GenerateNullableShortArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableShortArrayLengthTest()
        {
            CheckExceptionNullableShortArrayLength(null);
        }

        #endregion

        #region NullableUInt tests

        [Fact]
        public static void CheckNullableUIntArrayLengthTest()
        {
            CheckNullableUIntArrayLengthExpression(GenerateNullableUIntArray(0));
            CheckNullableUIntArrayLengthExpression(GenerateNullableUIntArray(1));
            CheckNullableUIntArrayLengthExpression(GenerateNullableUIntArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableUIntArrayLengthTest()
        {
            CheckExceptionNullableUIntArrayLength(null);
        }

        #endregion

        #region NullableULong tests

        [Fact]
        public static void CheckNullableULongArrayLengthTest()
        {
            CheckNullableULongArrayLengthExpression(GenerateNullableULongArray(0));
            CheckNullableULongArrayLengthExpression(GenerateNullableULongArray(1));
            CheckNullableULongArrayLengthExpression(GenerateNullableULongArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableULongArrayLengthTest()
        {
            CheckExceptionNullableULongArrayLength(null);
        }

        #endregion

        #region NullableUShort tests

        [Fact]
        public static void CheckNullableUShortArrayLengthTest()
        {
            CheckNullableUShortArrayLengthExpression(GenerateNullableUShortArray(0));
            CheckNullableUShortArrayLengthExpression(GenerateNullableUShortArray(1));
            CheckNullableUShortArrayLengthExpression(GenerateNullableUShortArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableUShortArrayLengthTest()
        {
            CheckExceptionNullableUShortArrayLength(null);
        }

        #endregion

        #region Generic tests

        [Fact]
        public static void CheckGenericNullableEnumWithStructRestrictionArrayLengthTest()
        {
            CheckGenericWithStructRestrictionArrayLengthTestHelper<E>();
        }

        [Fact]
        public static void CheckExceptionGenericNullableEnumWithStructRestrictionArrayLengthTest()
        {
            CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<E>();
        }

        [Fact]
        public static void CheckGenericNullableStructWithStructRestrictionArrayLengthTest()
        {
            CheckGenericWithStructRestrictionArrayLengthTestHelper<S>();
        }

        [Fact]
        public static void CheckExceptionGenericNullableStructWithStructRestrictionArrayLengthTest()
        {
            CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<S>();
        }

        [Fact]
        public static void CheckGenericNullableStructWithStringAndFieldWithStructRestrictionArrayLengthTest()
        {
            CheckGenericWithStructRestrictionArrayLengthTestHelper<Scs>();
        }

        [Fact]
        public static void CheckExceptionGenericNullableStructWithStringAndFieldWithStructRestrictionArrayLengthTest()
        {
            CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        public static void CheckGenericWithStructRestrictionArrayLengthTestHelper<Ts>() where Ts : struct
        {
            CheckGenericWithStructRestrictionArrayLengthExpression<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(0));
            CheckGenericWithStructRestrictionArrayLengthExpression<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(1));
            CheckGenericWithStructRestrictionArrayLengthExpression<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(5));
        }

        public static void CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<Ts>() where Ts : struct
        {
            CheckExceptionGenericWithStructRestrictionArrayLength<Ts>(null);
        }

        #endregion

        #region Generate array

        private static bool?[] GenerateNullableBoolArray(int size)
        {
            bool?[] array = new bool?[] { true, false };
            bool?[] result = new bool?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static byte?[] GenerateNullableByteArray(int size)
        {
            byte?[] array = new byte?[] { 0, 1, byte.MaxValue };
            byte?[] result = new byte?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static char?[] GenerateNullableCharArray(int size)
        {
            char?[] array = new char?[] { '\0', '\b', 'A', '\uffff' };
            char?[] result = new char?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static decimal?[] GenerateNullableDecimalArray(int size)
        {
            decimal?[] array = new decimal?[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            decimal?[] result = new decimal?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static double?[] GenerateNullableDoubleArray(int size)
        {
            double?[] array = new double?[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            double?[] result = new double?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static E?[] GenerateNullableEnumArray(int size)
        {
            E?[] array = new E?[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            E?[] result = new E?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static El?[] GenerateNullableEnumLongArray(int size)
        {
            El?[] array = new El?[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            El?[] result = new El?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static float?[] GenerateNullableFloatArray(int size)
        {
            float?[] array = new float?[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            float?[] result = new float?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static int?[] GenerateNullableIntArray(int size)
        {
            int?[] array = new int?[] { 0, 1, -1, int.MinValue, int.MaxValue };
            int?[] result = new int?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static long?[] GenerateNullableLongArray(int size)
        {
            long?[] array = new long?[] { 0, 1, -1, long.MinValue, long.MaxValue };
            long?[] result = new long?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static sbyte?[] GenerateNullableSByteArray(int size)
        {
            sbyte?[] array = new sbyte?[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            sbyte?[] result = new sbyte?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static S?[] GenerateNullableStructArray(int size)
        {
            S?[] array = new S?[] { default(S), new S() };
            S?[] result = new S?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Sc?[] GenerateNullableStructWithStringArray(int size)
        {
            Sc?[] array = new Sc?[] { default(Sc), new Sc(), new Sc(null) };
            Sc?[] result = new Sc?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Scs?[] GenerateNullableStructWithStringAndValueArray(int size)
        {
            Scs?[] array = new Scs?[] { default(Scs), new Scs(), new Scs(null, new S()) };
            Scs?[] result = new Scs?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Sp?[] GenerateNullableStructWithTwoParametersArray(int size)
        {
            Sp?[] array = new Sp?[] { default(Sp), new Sp(), new Sp(5, 5.0) };
            Sp?[] result = new Sp?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Ss?[] GenerateNullableStructWithValueArray(int size)
        {
            Ss?[] array = new Ss?[] { default(Ss), new Ss(), new Ss(new S()) };
            Ss?[] result = new Ss?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static short?[] GenerateNullableShortArray(int size)
        {
            short?[] array = new short?[] { 0, 1, -1, short.MinValue, short.MaxValue };
            short?[] result = new short?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static uint?[] GenerateNullableUIntArray(int size)
        {
            uint?[] array = new uint?[] { 0, 1, uint.MaxValue };
            uint?[] result = new uint?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static ulong?[] GenerateNullableULongArray(int size)
        {
            ulong?[] array = new ulong?[] { 0, 1, ulong.MaxValue };
            ulong?[] result = new ulong?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static ushort?[] GenerateNullableUShortArray(int size)
        {
            ushort?[] array = new ushort?[] { 0, 1, ushort.MaxValue };
            ushort?[] result = new ushort?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Ts?[] GenerateGenericWithStructRestrictionArray<Ts>(int size) where Ts : struct
        {
            Ts?[] array = new Ts?[] { default(Ts), new Ts() };
            Ts?[] result = new Ts?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        #endregion

        #region Check length expression

        private static void CheckNullableBoolArrayLengthExpression(bool?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(bool?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableByteArrayLengthExpression(byte?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(byte?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableCharArrayLengthExpression(char?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(char?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableDecimalArrayLengthExpression(decimal?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(decimal?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableDoubleArrayLengthExpression(double?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(double?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableEnumArrayLengthExpression(E?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(E?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableEnumLongArrayLengthExpression(El?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(El?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableFloatArrayLengthExpression(float?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(float?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableIntArrayLengthExpression(int?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(int?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableLongArrayLengthExpression(long?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(long?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableSByteArrayLengthExpression(sbyte?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(sbyte?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableStructArrayLengthExpression(S?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(S?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableStructWithStringArrayLengthExpression(Sc?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Sc?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableStructWithStringAndValueArrayLengthExpression(Scs?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Scs?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableStructWithTwoParametersArrayLengthExpression(Sp?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Sp?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableStructWithValueArrayLengthExpression(Ss?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Ss?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableShortArrayLengthExpression(short?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(short?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableUIntArrayLengthExpression(uint?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(uint?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableULongArrayLengthExpression(ulong?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(ulong?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableUShortArrayLengthExpression(ushort?[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(ushort?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithStructRestrictionArrayLengthExpression<Ts>(Ts?[] array) where Ts : struct
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Ts?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        #endregion

        #region Check exception array length

        private static void CheckExceptionNullableBoolArrayLength(bool?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableBoolArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableByteArrayLength(byte?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableByteArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableCharArrayLength(char?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableCharArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableDecimalArrayLength(decimal?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableDecimalArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableDoubleArrayLength(double?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableDoubleArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableEnumArrayLength(E?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableEnumArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableEnumLongArrayLength(El?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableEnumLongArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableFloatArrayLength(float?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableFloatArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableIntArrayLength(int?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableIntArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableLongArrayLength(long?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableLongArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableSByteArrayLength(sbyte?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableSByteArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableStructArrayLength(S?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableStructArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableStructWithStringArrayLength(Sc?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableStructWithStringArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableStructWithStringAndValueArrayLength(Scs?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableStructWithStringAndValueArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableStructWithTwoParametersArrayLength(Sp?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableStructWithTwoParametersArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableStructWithValueArrayLength(Ss?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableStructWithValueArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableShortArrayLength(short?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableShortArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableUIntArrayLength(uint?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableUIntArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableULongArrayLength(ulong?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableULongArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableUShortArrayLength(ushort?[] array)
        {
            bool success = true;
            try
            {
                CheckNullableUShortArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithStructRestrictionArrayLength<Ts>(Ts?[] array) where Ts : struct
        {
            bool success = true;
            try
            {
                CheckGenericWithStructRestrictionArrayLengthExpression<Ts>(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        #endregion
    }
}
