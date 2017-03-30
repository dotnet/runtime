// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class NullableArrayIndexTests
    {
        #region NullableBool tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableBoolArrayIndexTest(bool useInterpreter)
        {
            CheckNullableBoolArrayIndex(GenerateNullableBoolArray(0), useInterpreter);
            CheckNullableBoolArrayIndex(GenerateNullableBoolArray(1), useInterpreter);
            CheckNullableBoolArrayIndex(GenerateNullableBoolArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableBoolArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableBoolArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableBoolArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableBoolArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableBoolArrayIndex(GenerateNullableBoolArray(0), -1, useInterpreter);
            CheckExceptionNullableBoolArrayIndex(GenerateNullableBoolArray(0), 0, useInterpreter);
            CheckExceptionNullableBoolArrayIndex(GenerateNullableBoolArray(1), -1, useInterpreter);
            CheckExceptionNullableBoolArrayIndex(GenerateNullableBoolArray(1), 1, useInterpreter);
            CheckExceptionNullableBoolArrayIndex(GenerateNullableBoolArray(5), -1, useInterpreter);
            CheckExceptionNullableBoolArrayIndex(GenerateNullableBoolArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableByte tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableByteArrayIndexTest(bool useInterpreter)
        {
            CheckNullableByteArrayIndex(GenerateNullableByteArray(0), useInterpreter);
            CheckNullableByteArrayIndex(GenerateNullableByteArray(1), useInterpreter);
            CheckNullableByteArrayIndex(GenerateNullableByteArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableByteArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableByteArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableByteArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableByteArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableByteArrayIndex(GenerateNullableByteArray(0), -1, useInterpreter);
            CheckExceptionNullableByteArrayIndex(GenerateNullableByteArray(0), 0, useInterpreter);
            CheckExceptionNullableByteArrayIndex(GenerateNullableByteArray(1), -1, useInterpreter);
            CheckExceptionNullableByteArrayIndex(GenerateNullableByteArray(1), 1, useInterpreter);
            CheckExceptionNullableByteArrayIndex(GenerateNullableByteArray(5), -1, useInterpreter);
            CheckExceptionNullableByteArrayIndex(GenerateNullableByteArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableChar tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableCharArrayIndexTest(bool useInterpreter)
        {
            CheckNullableCharArrayIndex(GenerateNullableCharArray(0), useInterpreter);
            CheckNullableCharArrayIndex(GenerateNullableCharArray(1), useInterpreter);
            CheckNullableCharArrayIndex(GenerateNullableCharArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableCharArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableCharArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableCharArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableCharArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableCharArrayIndex(GenerateNullableCharArray(0), -1, useInterpreter);
            CheckExceptionNullableCharArrayIndex(GenerateNullableCharArray(0), 0, useInterpreter);
            CheckExceptionNullableCharArrayIndex(GenerateNullableCharArray(1), -1, useInterpreter);
            CheckExceptionNullableCharArrayIndex(GenerateNullableCharArray(1), 1, useInterpreter);
            CheckExceptionNullableCharArrayIndex(GenerateNullableCharArray(5), -1, useInterpreter);
            CheckExceptionNullableCharArrayIndex(GenerateNullableCharArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableDecimal tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDecimalArrayIndexTest(bool useInterpreter)
        {
            CheckNullableDecimalArrayIndex(GenerateNullableDecimalArray(0), useInterpreter);
            CheckNullableDecimalArrayIndex(GenerateNullableDecimalArray(1), useInterpreter);
            CheckNullableDecimalArrayIndex(GenerateNullableDecimalArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableDecimalArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableDecimalArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableDecimalArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableDecimalArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableDecimalArrayIndex(GenerateNullableDecimalArray(0), -1, useInterpreter);
            CheckExceptionNullableDecimalArrayIndex(GenerateNullableDecimalArray(0), 0, useInterpreter);
            CheckExceptionNullableDecimalArrayIndex(GenerateNullableDecimalArray(1), -1, useInterpreter);
            CheckExceptionNullableDecimalArrayIndex(GenerateNullableDecimalArray(1), 1, useInterpreter);
            CheckExceptionNullableDecimalArrayIndex(GenerateNullableDecimalArray(5), -1, useInterpreter);
            CheckExceptionNullableDecimalArrayIndex(GenerateNullableDecimalArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableDouble tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDoubleArrayIndexTest(bool useInterpreter)
        {
            CheckNullableDoubleArrayIndex(GenerateNullableDoubleArray(0), useInterpreter);
            CheckNullableDoubleArrayIndex(GenerateNullableDoubleArray(1), useInterpreter);
            CheckNullableDoubleArrayIndex(GenerateNullableDoubleArray(9), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableDoubleArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableDoubleArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableDoubleArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableDoubleArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableDoubleArrayIndex(GenerateNullableDoubleArray(0), -1, useInterpreter);
            CheckExceptionNullableDoubleArrayIndex(GenerateNullableDoubleArray(0), 0, useInterpreter);
            CheckExceptionNullableDoubleArrayIndex(GenerateNullableDoubleArray(1), -1, useInterpreter);
            CheckExceptionNullableDoubleArrayIndex(GenerateNullableDoubleArray(1), 1, useInterpreter);
            CheckExceptionNullableDoubleArrayIndex(GenerateNullableDoubleArray(9), -1, useInterpreter);
            CheckExceptionNullableDoubleArrayIndex(GenerateNullableDoubleArray(9), 9, useInterpreter);
        }

        #endregion

        #region NullableEnum tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableEnumArrayIndexTest(bool useInterpreter)
        {
            CheckNullableEnumArrayIndex(GenerateNullableEnumArray(0), useInterpreter);
            CheckNullableEnumArrayIndex(GenerateNullableEnumArray(1), useInterpreter);
            CheckNullableEnumArrayIndex(GenerateNullableEnumArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableEnumArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableEnumArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableEnumArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableEnumArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableEnumArrayIndex(GenerateNullableEnumArray(0), -1, useInterpreter);
            CheckExceptionNullableEnumArrayIndex(GenerateNullableEnumArray(0), 0, useInterpreter);
            CheckExceptionNullableEnumArrayIndex(GenerateNullableEnumArray(1), -1, useInterpreter);
            CheckExceptionNullableEnumArrayIndex(GenerateNullableEnumArray(1), 1, useInterpreter);
            CheckExceptionNullableEnumArrayIndex(GenerateNullableEnumArray(5), -1, useInterpreter);
            CheckExceptionNullableEnumArrayIndex(GenerateNullableEnumArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableLongEnum tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableLongEnumArrayIndexTest(bool useInterpreter)
        {
            CheckNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(0), useInterpreter);
            CheckNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(1), useInterpreter);
            CheckNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableLongEnumArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableLongEnumArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableLongEnumArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableLongEnumArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(0), -1, useInterpreter);
            CheckExceptionNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(0), 0, useInterpreter);
            CheckExceptionNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(1), -1, useInterpreter);
            CheckExceptionNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(1), 1, useInterpreter);
            CheckExceptionNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(5), -1, useInterpreter);
            CheckExceptionNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableFloat tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableFloatArrayIndexTest(bool useInterpreter)
        {
            CheckNullableFloatArrayIndex(GenerateNullableFloatArray(0), useInterpreter);
            CheckNullableFloatArrayIndex(GenerateNullableFloatArray(1), useInterpreter);
            CheckNullableFloatArrayIndex(GenerateNullableFloatArray(9), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableFloatArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableFloatArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableFloatArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableFloatArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableFloatArrayIndex(GenerateNullableFloatArray(0), -1, useInterpreter);
            CheckExceptionNullableFloatArrayIndex(GenerateNullableFloatArray(0), 0, useInterpreter);
            CheckExceptionNullableFloatArrayIndex(GenerateNullableFloatArray(1), -1, useInterpreter);
            CheckExceptionNullableFloatArrayIndex(GenerateNullableFloatArray(1), 1, useInterpreter);
            CheckExceptionNullableFloatArrayIndex(GenerateNullableFloatArray(9), -1, useInterpreter);
            CheckExceptionNullableFloatArrayIndex(GenerateNullableFloatArray(9), 9, useInterpreter);
        }

        #endregion

        #region NullableInt tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntArrayIndexTest(bool useInterpreter)
        {
            CheckNullableIntArrayIndex(GenerateNullableIntArray(0), useInterpreter);
            CheckNullableIntArrayIndex(GenerateNullableIntArray(1), useInterpreter);
            CheckNullableIntArrayIndex(GenerateNullableIntArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableIntArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableIntArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableIntArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableIntArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableIntArrayIndex(GenerateNullableIntArray(0), -1, useInterpreter);
            CheckExceptionNullableIntArrayIndex(GenerateNullableIntArray(0), 0, useInterpreter);
            CheckExceptionNullableIntArrayIndex(GenerateNullableIntArray(1), -1, useInterpreter);
            CheckExceptionNullableIntArrayIndex(GenerateNullableIntArray(1), 1, useInterpreter);
            CheckExceptionNullableIntArrayIndex(GenerateNullableIntArray(5), -1, useInterpreter);
            CheckExceptionNullableIntArrayIndex(GenerateNullableIntArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableLong tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableLongArrayIndexTest(bool useInterpreter)
        {
            CheckNullableLongArrayIndex(GenerateNullableLongArray(0), useInterpreter);
            CheckNullableLongArrayIndex(GenerateNullableLongArray(1), useInterpreter);
            CheckNullableLongArrayIndex(GenerateNullableLongArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableLongArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableLongArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableLongArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableLongArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableLongArrayIndex(GenerateNullableLongArray(0), -1, useInterpreter);
            CheckExceptionNullableLongArrayIndex(GenerateNullableLongArray(0), 0, useInterpreter);
            CheckExceptionNullableLongArrayIndex(GenerateNullableLongArray(1), -1, useInterpreter);
            CheckExceptionNullableLongArrayIndex(GenerateNullableLongArray(1), 1, useInterpreter);
            CheckExceptionNullableLongArrayIndex(GenerateNullableLongArray(5), -1, useInterpreter);
            CheckExceptionNullableLongArrayIndex(GenerateNullableLongArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableSByte tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableSByteArrayIndexTest(bool useInterpreter)
        {
            CheckNullableSByteArrayIndex(GenerateNullableSByteArray(0), useInterpreter);
            CheckNullableSByteArrayIndex(GenerateNullableSByteArray(1), useInterpreter);
            CheckNullableSByteArrayIndex(GenerateNullableSByteArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableSByteArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableSByteArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableSByteArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableSByteArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableSByteArrayIndex(GenerateNullableSByteArray(0), -1, useInterpreter);
            CheckExceptionNullableSByteArrayIndex(GenerateNullableSByteArray(0), 0, useInterpreter);
            CheckExceptionNullableSByteArrayIndex(GenerateNullableSByteArray(1), -1, useInterpreter);
            CheckExceptionNullableSByteArrayIndex(GenerateNullableSByteArray(1), 1, useInterpreter);
            CheckExceptionNullableSByteArrayIndex(GenerateNullableSByteArray(5), -1, useInterpreter);
            CheckExceptionNullableSByteArrayIndex(GenerateNullableSByteArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableShort tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableShortArrayIndexTest(bool useInterpreter)
        {
            CheckNullableShortArrayIndex(GenerateNullableShortArray(0), useInterpreter);
            CheckNullableShortArrayIndex(GenerateNullableShortArray(1), useInterpreter);
            CheckNullableShortArrayIndex(GenerateNullableShortArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableShortArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableShortArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableShortArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableShortArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableShortArrayIndex(GenerateNullableShortArray(0), -1, useInterpreter);
            CheckExceptionNullableShortArrayIndex(GenerateNullableShortArray(0), 0, useInterpreter);
            CheckExceptionNullableShortArrayIndex(GenerateNullableShortArray(1), -1, useInterpreter);
            CheckExceptionNullableShortArrayIndex(GenerateNullableShortArray(1), 1, useInterpreter);
            CheckExceptionNullableShortArrayIndex(GenerateNullableShortArray(5), -1, useInterpreter);
            CheckExceptionNullableShortArrayIndex(GenerateNullableShortArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableStruct tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructArrayIndexTest(bool useInterpreter)
        {
            CheckNullableStructArrayIndex(GenerateNullableStructArray(0), useInterpreter);
            CheckNullableStructArrayIndex(GenerateNullableStructArray(1), useInterpreter);
            CheckNullableStructArrayIndex(GenerateNullableStructArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableStructArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableStructArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableStructArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableStructArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableStructArrayIndex(GenerateNullableStructArray(0), -1, useInterpreter);
            CheckExceptionNullableStructArrayIndex(GenerateNullableStructArray(0), 0, useInterpreter);
            CheckExceptionNullableStructArrayIndex(GenerateNullableStructArray(1), -1, useInterpreter);
            CheckExceptionNullableStructArrayIndex(GenerateNullableStructArray(1), 1, useInterpreter);
            CheckExceptionNullableStructArrayIndex(GenerateNullableStructArray(5), -1, useInterpreter);
            CheckExceptionNullableStructArrayIndex(GenerateNullableStructArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableStructWithString tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithStringArrayIndexTest(bool useInterpreter)
        {
            CheckNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(0), useInterpreter);
            CheckNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(1), useInterpreter);
            CheckNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableStructWithStringArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableStructWithStringArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableStructWithStringArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableStructWithStringArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(0), -1, useInterpreter);
            CheckExceptionNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(0), 0, useInterpreter);
            CheckExceptionNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(1), -1, useInterpreter);
            CheckExceptionNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(1), 1, useInterpreter);
            CheckExceptionNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(5), -1, useInterpreter);
            CheckExceptionNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableStructWithStringAndValue tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithStringAndValueArrayIndexTest(bool useInterpreter)
        {
            CheckNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(0), useInterpreter);
            CheckNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(1), useInterpreter);
            CheckNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableStructWithStringAndValueArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableStructWithStringAndValueArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(0), -1, useInterpreter);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(0), 0, useInterpreter);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(1), -1, useInterpreter);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(1), 1, useInterpreter);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(5), -1, useInterpreter);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableStructWithTwoValues tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithTwoValuesArrayIndexTest(bool useInterpreter)
        {
            CheckNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(0), useInterpreter);
            CheckNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(1), useInterpreter);
            CheckNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableStructWithTwoValuesArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableStructWithTwoValuesArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(0), -1, useInterpreter);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(0), 0, useInterpreter);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(1), -1, useInterpreter);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(1), 1, useInterpreter);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(5), -1, useInterpreter);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableStructWithStruct tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructWithStructArrayIndexTest(bool useInterpreter)
        {
            CheckNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(0), useInterpreter);
            CheckNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(1), useInterpreter);
            CheckNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableStructWithStructArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableStructWithStructArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableStructWithStructArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableStructWithStructArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(0), -1, useInterpreter);
            CheckExceptionNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(0), 0, useInterpreter);
            CheckExceptionNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(1), -1, useInterpreter);
            CheckExceptionNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(1), 1, useInterpreter);
            CheckExceptionNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(5), -1, useInterpreter);
            CheckExceptionNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableUInt tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUIntArrayIndexTest(bool useInterpreter)
        {
            CheckNullableUIntArrayIndex(GenerateNullableUIntArray(0), useInterpreter);
            CheckNullableUIntArrayIndex(GenerateNullableUIntArray(1), useInterpreter);
            CheckNullableUIntArrayIndex(GenerateNullableUIntArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableUIntArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableUIntArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableUIntArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableUIntArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableUIntArrayIndex(GenerateNullableUIntArray(0), -1, useInterpreter);
            CheckExceptionNullableUIntArrayIndex(GenerateNullableUIntArray(0), 0, useInterpreter);
            CheckExceptionNullableUIntArrayIndex(GenerateNullableUIntArray(1), -1, useInterpreter);
            CheckExceptionNullableUIntArrayIndex(GenerateNullableUIntArray(1), 1, useInterpreter);
            CheckExceptionNullableUIntArrayIndex(GenerateNullableUIntArray(5), -1, useInterpreter);
            CheckExceptionNullableUIntArrayIndex(GenerateNullableUIntArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableULong tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableULongArrayIndexTest(bool useInterpreter)
        {
            CheckNullableULongArrayIndex(GenerateNullableULongArray(0), useInterpreter);
            CheckNullableULongArrayIndex(GenerateNullableULongArray(1), useInterpreter);
            CheckNullableULongArrayIndex(GenerateNullableULongArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableULongArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableULongArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableULongArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableULongArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableULongArrayIndex(GenerateNullableULongArray(0), -1, useInterpreter);
            CheckExceptionNullableULongArrayIndex(GenerateNullableULongArray(0), 0, useInterpreter);
            CheckExceptionNullableULongArrayIndex(GenerateNullableULongArray(1), -1, useInterpreter);
            CheckExceptionNullableULongArrayIndex(GenerateNullableULongArray(1), 1, useInterpreter);
            CheckExceptionNullableULongArrayIndex(GenerateNullableULongArray(5), -1, useInterpreter);
            CheckExceptionNullableULongArrayIndex(GenerateNullableULongArray(5), 5, useInterpreter);
        }

        #endregion

        #region NullableUShort tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUShortArrayIndexTest(bool useInterpreter)
        {
            CheckNullableUShortArrayIndex(GenerateNullableUShortArray(0), useInterpreter);
            CheckNullableUShortArrayIndex(GenerateNullableUShortArray(1), useInterpreter);
            CheckNullableUShortArrayIndex(GenerateNullableUShortArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableUShortArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionNullableUShortArrayIndex(null, -1, useInterpreter);
            CheckExceptionNullableUShortArrayIndex(null, 0, useInterpreter);
            CheckExceptionNullableUShortArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableUShortArrayIndex(GenerateNullableUShortArray(0), -1, useInterpreter);
            CheckExceptionNullableUShortArrayIndex(GenerateNullableUShortArray(0), 0, useInterpreter);
            CheckExceptionNullableUShortArrayIndex(GenerateNullableUShortArray(1), -1, useInterpreter);
            CheckExceptionNullableUShortArrayIndex(GenerateNullableUShortArray(1), 1, useInterpreter);
            CheckExceptionNullableUShortArrayIndex(GenerateNullableUShortArray(5), -1, useInterpreter);
            CheckExceptionNullableUShortArrayIndex(GenerateNullableUShortArray(5), 5, useInterpreter);
        }

        #endregion

        #region Generic tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableGenericWithStructRestriction(bool useInterpreter)
        {
            CheckNullableGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableGenericWithStructRestriction(bool useInterpreter)
        {
            CheckExceptionNullableGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableGenericWithStructRestriction2(bool useInterpreter)
        {
            CheckNullableGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableGenericWithStructRestriction2(bool useInterpreter)
        {
            CheckExceptionNullableGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableGenericWithStructRestriction3(bool useInterpreter)
        {
            CheckNullableGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionNullableGenericWithStructRestriction3(bool useInterpreter)
        {
            CheckExceptionNullableGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckNullableGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            CheckNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(0), useInterpreter);
            CheckNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(1), useInterpreter);
            CheckNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(5), useInterpreter);
        }

        private static void CheckExceptionNullableGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            // null arrays
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(null, -1, useInterpreter);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(null, 0, useInterpreter);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(0), -1, useInterpreter);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(0), 0, useInterpreter);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(1), -1, useInterpreter);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(1), 1, useInterpreter);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(5), -1, useInterpreter);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(5), 5, useInterpreter);
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

        private static El?[] GenerateNullableLongEnumArray(int size)
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

        private static Sp?[] GenerateNullableStructWithTwoValuesArray(int size)
        {
            Sp?[] array = new Sp?[] { default(Sp), new Sp(), new Sp(5, 5.0) };
            Sp?[] result = new Sp?[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Ss?[] GenerateNullableStructWithStructArray(int size)
        {
            Ss?[] array = new Ss?[] { default(Ss), new Ss(), new Ss(new S()) };
            Ss?[] result = new Ss?[size];
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

        private static Ts?[] GenerateNullableGenericWithStructRestrictionArray<Ts>(int size) where Ts : struct
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

        #region Check array index
        private static void CheckNullableBoolArrayIndex(bool?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableBoolArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableByteArrayIndex(byte?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableByteArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableCharArrayIndex(char?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableCharArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableDecimalArrayIndex(decimal?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableDecimalArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableDoubleArrayIndex(double?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableDoubleArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableEnumArrayIndex(E?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableEnumArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableLongEnumArrayIndex(El?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableLongEnumArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableFloatArrayIndex(float?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableFloatArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableIntArrayIndex(int?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableIntArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableLongArrayIndex(long?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableLongArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableSByteArrayIndex(sbyte?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableSByteArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableShortArrayIndex(short?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableShortArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableStructArrayIndex(S?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableStructArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableStructWithStringArrayIndex(Sc?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableStructWithStringArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableStructWithStringAndValueArrayIndex(Scs?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableStructWithStringAndValueArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableStructWithTwoValuesArrayIndex(Sp?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableStructWithTwoValuesArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableStructWithStructArrayIndex(Ss?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableStructWithStructArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableUIntArrayIndex(uint?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableUIntArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableULongArrayIndex(ulong?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableULongArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableUShortArrayIndex(ushort?[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableUShortArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckNullableGenericWithStructRestrictionArrayIndex<Ts>(Ts?[] array, bool useInterpreter) where Ts : struct
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableGenericWithStructRestrictionArrayIndexExpression<Ts>(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        #endregion

        #region Check index expression
        private static bool CheckNullableBoolArrayIndexExpression(bool?[] array, int index, bool useInterpreter)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(bool?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableByteArrayIndexExpression(byte?[] array, int index, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(byte?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableCharArrayIndexExpression(char?[] array, int index, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(char?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableDecimalArrayIndexExpression(decimal?[] array, int index, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(decimal?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableDoubleArrayIndexExpression(double?[] array, int index, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(double?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableEnumArrayIndexExpression(E?[] array, int index, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(E?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableLongEnumArrayIndexExpression(El?[] array, int index, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(El?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableFloatArrayIndexExpression(float?[] array, int index, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(float?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableIntArrayIndexExpression(int?[] array, int index, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(int?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableLongArrayIndexExpression(long?[] array, int index, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(long?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableSByteArrayIndexExpression(sbyte?[] array, int index, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(sbyte?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableShortArrayIndexExpression(short?[] array, int index, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(short?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableStructArrayIndexExpression(S?[] array, int index, bool useInterpreter)
        {
            Expression<Func<S?>> e =
                Expression.Lambda<Func<S?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(S?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableStructWithStringArrayIndexExpression(Sc?[] array, int index, bool useInterpreter)
        {
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Sc?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableStructWithStringAndValueArrayIndexExpression(Scs?[] array, int index, bool useInterpreter)
        {
            Expression<Func<Scs?>> e =
                Expression.Lambda<Func<Scs?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Scs?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableStructWithTwoValuesArrayIndexExpression(Sp?[] array, int index, bool useInterpreter)
        {
            Expression<Func<Sp?>> e =
                Expression.Lambda<Func<Sp?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Sp?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableStructWithStructArrayIndexExpression(Ss?[] array, int index, bool useInterpreter)
        {
            Expression<Func<Ss?>> e =
                Expression.Lambda<Func<Ss?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Ss?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableUIntArrayIndexExpression(uint?[] array, int index, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(uint?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableULongArrayIndexExpression(ulong?[] array, int index, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(ulong?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableUShortArrayIndexExpression(ushort?[] array, int index, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(ushort?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableGenericWithStructRestrictionArrayIndexExpression<Ts>(Ts?[] array, int index, bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts?>> e =
                Expression.Lambda<Func<Ts?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Ts?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        #endregion

        #region Check exception array index

        private static void CheckExceptionNullableBoolArrayIndex(bool?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableBoolArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableBoolArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableByteArrayIndex(byte?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableByteArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableByteArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableCharArrayIndex(char?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableCharArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableCharArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableDecimalArrayIndex(decimal?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableDecimalArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableDecimalArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableDoubleArrayIndex(double?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableDoubleArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableDoubleArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableEnumArrayIndex(E?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableEnumArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableEnumArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableLongEnumArrayIndex(El?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableLongEnumArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableLongEnumArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableFloatArrayIndex(float?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableFloatArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableFloatArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableIntArrayIndex(int?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableIntArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableIntArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableLongArrayIndex(long?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableLongArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableLongArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableSByteArrayIndex(sbyte?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableSByteArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableSByteArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableShortArrayIndex(short?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableShortArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableShortArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableStructArrayIndex(S?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableStructArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableStructArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableStructWithStringArrayIndex(Sc?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableStructWithStringArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableStructWithStringArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableStructWithStringAndValueArrayIndex(Scs?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableStructWithStringAndValueArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableStructWithStringAndValueArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableStructWithTwoValuesArrayIndex(Sp?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableStructWithTwoValuesArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableStructWithTwoValuesArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableStructWithStructArrayIndex(Ss?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableStructWithStructArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableStructWithStructArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableUIntArrayIndex(uint?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableUIntArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableUIntArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableULongArrayIndex(ulong?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableULongArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableULongArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableUShortArrayIndex(ushort?[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableUShortArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableUShortArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(Ts?[] array, int index, bool useInterpreter) where Ts : struct
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckNullableGenericWithStructRestrictionArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckNullableGenericWithStructRestrictionArrayIndexExpression(array, index, useInterpreter));
        }

        #endregion
    }
}
