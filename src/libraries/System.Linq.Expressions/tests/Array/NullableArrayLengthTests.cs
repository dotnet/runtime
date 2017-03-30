// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class NullableArrayLengthTests
    {
        #region NullableBool tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableBoolArrayLengthTest(bool useInterpreter)
        {
            CheckNullableBoolArrayLengthExpression(GenerateNullableBoolArray(0), useInterpreter);
            CheckNullableBoolArrayLengthExpression(GenerateNullableBoolArray(1), useInterpreter);
            CheckNullableBoolArrayLengthExpression(GenerateNullableBoolArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableBoolArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableBoolArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableByte tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableByteArrayLengthTest(bool useInterpreter)
        {
            CheckNullableByteArrayLengthExpression(GenerateNullableByteArray(0), useInterpreter);
            CheckNullableByteArrayLengthExpression(GenerateNullableByteArray(1), useInterpreter);
            CheckNullableByteArrayLengthExpression(GenerateNullableByteArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableByteArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableByteArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableChar tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableCharArrayLengthTest(bool useInterpreter)
        {
            CheckNullableCharArrayLengthExpression(GenerateNullableCharArray(0), useInterpreter);
            CheckNullableCharArrayLengthExpression(GenerateNullableCharArray(1), useInterpreter);
            CheckNullableCharArrayLengthExpression(GenerateNullableCharArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableCharArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableCharArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableDecimal tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDecimalArrayLengthTest(bool useInterpreter)
        {
            CheckNullableDecimalArrayLengthExpression(GenerateNullableDecimalArray(0), useInterpreter);
            CheckNullableDecimalArrayLengthExpression(GenerateNullableDecimalArray(1), useInterpreter);
            CheckNullableDecimalArrayLengthExpression(GenerateNullableDecimalArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableDecimalArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableDecimalArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableDouble tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDoubleArrayLengthTest(bool useInterpreter)
        {
            CheckNullableDoubleArrayLengthExpression(GenerateNullableDoubleArray(0), useInterpreter);
            CheckNullableDoubleArrayLengthExpression(GenerateNullableDoubleArray(1), useInterpreter);
            CheckNullableDoubleArrayLengthExpression(GenerateNullableDoubleArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableDoubleArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableDoubleArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableEnum tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableEnumArrayLengthTest(bool useInterpreter)
        {
            CheckNullableEnumArrayLengthExpression(GenerateNullableEnumArray(0), useInterpreter);
            CheckNullableEnumArrayLengthExpression(GenerateNullableEnumArray(1), useInterpreter);
            CheckNullableEnumArrayLengthExpression(GenerateNullableEnumArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableEnumArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableEnumArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableEnumLong tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableEnumLongArrayLengthTest(bool useInterpreter)
        {
            CheckNullableEnumLongArrayLengthExpression(GenerateNullableEnumLongArray(0), useInterpreter);
            CheckNullableEnumLongArrayLengthExpression(GenerateNullableEnumLongArray(1), useInterpreter);
            CheckNullableEnumLongArrayLengthExpression(GenerateNullableEnumLongArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableEnumLongArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableEnumLongArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableFloat tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableFloatArrayLengthTest(bool useInterpreter)
        {
            CheckNullableFloatArrayLengthExpression(GenerateNullableFloatArray(0), useInterpreter);
            CheckNullableFloatArrayLengthExpression(GenerateNullableFloatArray(1), useInterpreter);
            CheckNullableFloatArrayLengthExpression(GenerateNullableFloatArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableFloatArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableFloatArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableInt tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntArrayLengthTest(bool useInterpreter)
        {
            CheckNullableIntArrayLengthExpression(GenerateNullableIntArray(0), useInterpreter);
            CheckNullableIntArrayLengthExpression(GenerateNullableIntArray(1), useInterpreter);
            CheckNullableIntArrayLengthExpression(GenerateNullableIntArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableIntArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableIntArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableLong tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableLongArrayLengthTest(bool useInterpreter)
        {
            CheckNullableLongArrayLengthExpression(GenerateNullableLongArray(0), useInterpreter);
            CheckNullableLongArrayLengthExpression(GenerateNullableLongArray(1), useInterpreter);
            CheckNullableLongArrayLengthExpression(GenerateNullableLongArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableLongArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableLongArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableSByte tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableSByteArrayLengthTest(bool useInterpreter)
        {
            CheckNullableSByteArrayLengthExpression(GenerateNullableSByteArray(0), useInterpreter);
            CheckNullableSByteArrayLengthExpression(GenerateNullableSByteArray(1), useInterpreter);
            CheckNullableSByteArrayLengthExpression(GenerateNullableSByteArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableSByteArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableSByteArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableStruct tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructArrayLengthTest(bool useInterpreter)
        {
            CheckNullableStructArrayLengthExpression(GenerateNullableStructArray(0), useInterpreter);
            CheckNullableStructArrayLengthExpression(GenerateNullableStructArray(1), useInterpreter);
            CheckNullableStructArrayLengthExpression(GenerateNullableStructArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableStructArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableStructArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableStructWithString tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithStringArrayLengthTest(bool useInterpreter)
        {
            CheckNullableStructWithStringArrayLengthExpression(GenerateNullableStructWithStringArray(0), useInterpreter);
            CheckNullableStructWithStringArrayLengthExpression(GenerateNullableStructWithStringArray(1), useInterpreter);
            CheckNullableStructWithStringArrayLengthExpression(GenerateNullableStructWithStringArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableStructWithStringArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableStructWithStringArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableStructWithStringAndValue tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithStringAndValueArrayLengthTest(bool useInterpreter)
        {
            CheckNullableStructWithStringAndValueArrayLengthExpression(GenerateNullableStructWithStringAndValueArray(0), useInterpreter);
            CheckNullableStructWithStringAndValueArrayLengthExpression(GenerateNullableStructWithStringAndValueArray(1), useInterpreter);
            CheckNullableStructWithStringAndValueArrayLengthExpression(GenerateNullableStructWithStringAndValueArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableStructWithStringAndValueArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableStructWithStringAndValueArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableStructWithTwoParameters tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithTwoParametersArrayLengthTest(bool useInterpreter)
        {
            CheckNullableStructWithTwoParametersArrayLengthExpression(GenerateNullableStructWithTwoParametersArray(0), useInterpreter);
            CheckNullableStructWithTwoParametersArrayLengthExpression(GenerateNullableStructWithTwoParametersArray(1), useInterpreter);
            CheckNullableStructWithTwoParametersArrayLengthExpression(GenerateNullableStructWithTwoParametersArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableStructWithTwoParametersArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableStructWithTwoParametersArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableStructWithValue tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithValueArrayLengthTest(bool useInterpreter)
        {
            CheckNullableStructWithValueArrayLengthExpression(GenerateNullableStructWithValueArray(0), useInterpreter);
            CheckNullableStructWithValueArrayLengthExpression(GenerateNullableStructWithValueArray(1), useInterpreter);
            CheckNullableStructWithValueArrayLengthExpression(GenerateNullableStructWithValueArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableStructWithValueArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableStructWithValueArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableShort tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableShortArrayLengthTest(bool useInterpreter)
        {
            CheckNullableShortArrayLengthExpression(GenerateNullableShortArray(0), useInterpreter);
            CheckNullableShortArrayLengthExpression(GenerateNullableShortArray(1), useInterpreter);
            CheckNullableShortArrayLengthExpression(GenerateNullableShortArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableShortArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableShortArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableUInt tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUIntArrayLengthTest(bool useInterpreter)
        {
            CheckNullableUIntArrayLengthExpression(GenerateNullableUIntArray(0), useInterpreter);
            CheckNullableUIntArrayLengthExpression(GenerateNullableUIntArray(1), useInterpreter);
            CheckNullableUIntArrayLengthExpression(GenerateNullableUIntArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableUIntArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableUIntArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableULong tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableULongArrayLengthTest(bool useInterpreter)
        {
            CheckNullableULongArrayLengthExpression(GenerateNullableULongArray(0), useInterpreter);
            CheckNullableULongArrayLengthExpression(GenerateNullableULongArray(1), useInterpreter);
            CheckNullableULongArrayLengthExpression(GenerateNullableULongArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableULongArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableULongArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region NullableUShort tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUShortArrayLengthTest(bool useInterpreter)
        {
            CheckNullableUShortArrayLengthExpression(GenerateNullableUShortArray(0), useInterpreter);
            CheckNullableUShortArrayLengthExpression(GenerateNullableUShortArray(1), useInterpreter);
            CheckNullableUShortArrayLengthExpression(GenerateNullableUShortArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableUShortArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckNullableUShortArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Generic tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericNullableEnumWithStructRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayLengthTestHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericNullableEnumWithStructRestrictionArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckGenericWithStructRestrictionArrayLengthExpression<E>(null, useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericNullableStructWithStructRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayLengthTestHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericNullableStructWithStructRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericNullableStructWithStringAndFieldWithStructRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayLengthTestHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericNullableStructWithStringAndFieldWithStructRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        public static void CheckGenericWithStructRestrictionArrayLengthTestHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            CheckGenericWithStructRestrictionArrayLengthExpression<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(0), useInterpreter);
            CheckGenericWithStructRestrictionArrayLengthExpression<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(1), useInterpreter);
            CheckGenericWithStructRestrictionArrayLengthExpression<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Assert.Throws<NullReferenceException>(() => CheckGenericWithStructRestrictionArrayLengthExpression<Ts>(null, useInterpreter));
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

        private static void CheckNullableBoolArrayLengthExpression(bool?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(bool?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableByteArrayLengthExpression(byte?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(byte?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableCharArrayLengthExpression(char?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(char?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableDecimalArrayLengthExpression(decimal?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(decimal?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableDoubleArrayLengthExpression(double?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(double?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableEnumArrayLengthExpression(E?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(E?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableEnumLongArrayLengthExpression(El?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(El?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableFloatArrayLengthExpression(float?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(float?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableIntArrayLengthExpression(int?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(int?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableLongArrayLengthExpression(long?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(long?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableSByteArrayLengthExpression(sbyte?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(sbyte?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableStructArrayLengthExpression(S?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(S?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableStructWithStringArrayLengthExpression(Sc?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Sc?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableStructWithStringAndValueArrayLengthExpression(Scs?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Scs?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableStructWithTwoParametersArrayLengthExpression(Sp?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Sp?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableStructWithValueArrayLengthExpression(Ss?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Ss?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableShortArrayLengthExpression(short?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(short?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableUIntArrayLengthExpression(uint?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(uint?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableULongArrayLengthExpression(ulong?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(ulong?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckNullableUShortArrayLengthExpression(ushort?[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(ushort?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithStructRestrictionArrayLengthExpression<Ts>(Ts?[] array, bool useInterpreter) where Ts : struct
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Ts?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        #endregion
    }
}
