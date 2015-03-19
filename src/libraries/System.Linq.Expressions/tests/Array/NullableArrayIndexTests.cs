// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Array
{
    public static unsafe class NullableArrayIndexTests
    {
        #region NullableBool tests

        [Fact]
        public static void CheckNullableBoolArrayIndexTest()
        {
            CheckNullableBoolArrayIndex(GenerateNullableBoolArray(0));
            CheckNullableBoolArrayIndex(GenerateNullableBoolArray(1));
            CheckNullableBoolArrayIndex(GenerateNullableBoolArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableBoolArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableBoolArrayIndex(null, -1);
            CheckExceptionNullableBoolArrayIndex(null, 0);
            CheckExceptionNullableBoolArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableBoolArrayIndex(GenerateNullableBoolArray(0), -1);
            CheckExceptionNullableBoolArrayIndex(GenerateNullableBoolArray(0), 0);
            CheckExceptionNullableBoolArrayIndex(GenerateNullableBoolArray(1), -1);
            CheckExceptionNullableBoolArrayIndex(GenerateNullableBoolArray(1), 1);
            CheckExceptionNullableBoolArrayIndex(GenerateNullableBoolArray(5), -1);
            CheckExceptionNullableBoolArrayIndex(GenerateNullableBoolArray(5), 5);
        }

        #endregion

        #region NullableByte tests

        [Fact]
        public static void CheckNullableByteArrayIndexTest()
        {
            CheckNullableByteArrayIndex(GenerateNullableByteArray(0));
            CheckNullableByteArrayIndex(GenerateNullableByteArray(1));
            CheckNullableByteArrayIndex(GenerateNullableByteArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableByteArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableByteArrayIndex(null, -1);
            CheckExceptionNullableByteArrayIndex(null, 0);
            CheckExceptionNullableByteArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableByteArrayIndex(GenerateNullableByteArray(0), -1);
            CheckExceptionNullableByteArrayIndex(GenerateNullableByteArray(0), 0);
            CheckExceptionNullableByteArrayIndex(GenerateNullableByteArray(1), -1);
            CheckExceptionNullableByteArrayIndex(GenerateNullableByteArray(1), 1);
            CheckExceptionNullableByteArrayIndex(GenerateNullableByteArray(5), -1);
            CheckExceptionNullableByteArrayIndex(GenerateNullableByteArray(5), 5);
        }

        #endregion

        #region NullableChar tests

        [Fact]
        public static void CheckNullableCharArrayIndexTest()
        {
            CheckNullableCharArrayIndex(GenerateNullableCharArray(0));
            CheckNullableCharArrayIndex(GenerateNullableCharArray(1));
            CheckNullableCharArrayIndex(GenerateNullableCharArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableCharArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableCharArrayIndex(null, -1);
            CheckExceptionNullableCharArrayIndex(null, 0);
            CheckExceptionNullableCharArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableCharArrayIndex(GenerateNullableCharArray(0), -1);
            CheckExceptionNullableCharArrayIndex(GenerateNullableCharArray(0), 0);
            CheckExceptionNullableCharArrayIndex(GenerateNullableCharArray(1), -1);
            CheckExceptionNullableCharArrayIndex(GenerateNullableCharArray(1), 1);
            CheckExceptionNullableCharArrayIndex(GenerateNullableCharArray(5), -1);
            CheckExceptionNullableCharArrayIndex(GenerateNullableCharArray(5), 5);
        }

        #endregion

        #region NullableDecimal tests

        [Fact]
        public static void CheckNullableDecimalArrayIndexTest()
        {
            CheckNullableDecimalArrayIndex(GenerateNullableDecimalArray(0));
            CheckNullableDecimalArrayIndex(GenerateNullableDecimalArray(1));
            CheckNullableDecimalArrayIndex(GenerateNullableDecimalArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableDecimalArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableDecimalArrayIndex(null, -1);
            CheckExceptionNullableDecimalArrayIndex(null, 0);
            CheckExceptionNullableDecimalArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableDecimalArrayIndex(GenerateNullableDecimalArray(0), -1);
            CheckExceptionNullableDecimalArrayIndex(GenerateNullableDecimalArray(0), 0);
            CheckExceptionNullableDecimalArrayIndex(GenerateNullableDecimalArray(1), -1);
            CheckExceptionNullableDecimalArrayIndex(GenerateNullableDecimalArray(1), 1);
            CheckExceptionNullableDecimalArrayIndex(GenerateNullableDecimalArray(5), -1);
            CheckExceptionNullableDecimalArrayIndex(GenerateNullableDecimalArray(5), 5);
        }

        #endregion

        #region NullableDouble tests

        [Fact]
        public static void CheckNullableDoubleArrayIndexTest()
        {
            CheckNullableDoubleArrayIndex(GenerateNullableDoubleArray(0));
            CheckNullableDoubleArrayIndex(GenerateNullableDoubleArray(1));
            CheckNullableDoubleArrayIndex(GenerateNullableDoubleArray(9));
        }

        [Fact]
        public static void CheckExceptionNullableDoubleArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableDoubleArrayIndex(null, -1);
            CheckExceptionNullableDoubleArrayIndex(null, 0);
            CheckExceptionNullableDoubleArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableDoubleArrayIndex(GenerateNullableDoubleArray(0), -1);
            CheckExceptionNullableDoubleArrayIndex(GenerateNullableDoubleArray(0), 0);
            CheckExceptionNullableDoubleArrayIndex(GenerateNullableDoubleArray(1), -1);
            CheckExceptionNullableDoubleArrayIndex(GenerateNullableDoubleArray(1), 1);
            CheckExceptionNullableDoubleArrayIndex(GenerateNullableDoubleArray(9), -1);
            CheckExceptionNullableDoubleArrayIndex(GenerateNullableDoubleArray(9), 9);
        }

        #endregion

        #region NullableEnum tests

        [Fact]
        public static void CheckNullableEnumArrayIndexTest()
        {
            CheckNullableEnumArrayIndex(GenerateNullableEnumArray(0));
            CheckNullableEnumArrayIndex(GenerateNullableEnumArray(1));
            CheckNullableEnumArrayIndex(GenerateNullableEnumArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableEnumArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableEnumArrayIndex(null, -1);
            CheckExceptionNullableEnumArrayIndex(null, 0);
            CheckExceptionNullableEnumArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableEnumArrayIndex(GenerateNullableEnumArray(0), -1);
            CheckExceptionNullableEnumArrayIndex(GenerateNullableEnumArray(0), 0);
            CheckExceptionNullableEnumArrayIndex(GenerateNullableEnumArray(1), -1);
            CheckExceptionNullableEnumArrayIndex(GenerateNullableEnumArray(1), 1);
            CheckExceptionNullableEnumArrayIndex(GenerateNullableEnumArray(5), -1);
            CheckExceptionNullableEnumArrayIndex(GenerateNullableEnumArray(5), 5);
        }

        #endregion

        #region NullableLongEnum tests

        [Fact]
        public static void CheckNullableLongEnumArrayIndexTest()
        {
            CheckNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(0));
            CheckNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(1));
            CheckNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableLongEnumArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableLongEnumArrayIndex(null, -1);
            CheckExceptionNullableLongEnumArrayIndex(null, 0);
            CheckExceptionNullableLongEnumArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(0), -1);
            CheckExceptionNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(0), 0);
            CheckExceptionNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(1), -1);
            CheckExceptionNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(1), 1);
            CheckExceptionNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(5), -1);
            CheckExceptionNullableLongEnumArrayIndex(GenerateNullableLongEnumArray(5), 5);
        }

        #endregion

        #region NullableFloat tests

        [Fact]
        public static void CheckNullableFloatArrayIndexTest()
        {
            CheckNullableFloatArrayIndex(GenerateNullableFloatArray(0));
            CheckNullableFloatArrayIndex(GenerateNullableFloatArray(1));
            CheckNullableFloatArrayIndex(GenerateNullableFloatArray(9));
        }

        [Fact]
        public static void CheckExceptionNullableFloatArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableFloatArrayIndex(null, -1);
            CheckExceptionNullableFloatArrayIndex(null, 0);
            CheckExceptionNullableFloatArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableFloatArrayIndex(GenerateNullableFloatArray(0), -1);
            CheckExceptionNullableFloatArrayIndex(GenerateNullableFloatArray(0), 0);
            CheckExceptionNullableFloatArrayIndex(GenerateNullableFloatArray(1), -1);
            CheckExceptionNullableFloatArrayIndex(GenerateNullableFloatArray(1), 1);
            CheckExceptionNullableFloatArrayIndex(GenerateNullableFloatArray(9), -1);
            CheckExceptionNullableFloatArrayIndex(GenerateNullableFloatArray(9), 9);
        }

        #endregion

        #region NullableInt tests

        [Fact]
        public static void CheckNullableIntArrayIndexTest()
        {
            CheckNullableIntArrayIndex(GenerateNullableIntArray(0));
            CheckNullableIntArrayIndex(GenerateNullableIntArray(1));
            CheckNullableIntArrayIndex(GenerateNullableIntArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableIntArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableIntArrayIndex(null, -1);
            CheckExceptionNullableIntArrayIndex(null, 0);
            CheckExceptionNullableIntArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableIntArrayIndex(GenerateNullableIntArray(0), -1);
            CheckExceptionNullableIntArrayIndex(GenerateNullableIntArray(0), 0);
            CheckExceptionNullableIntArrayIndex(GenerateNullableIntArray(1), -1);
            CheckExceptionNullableIntArrayIndex(GenerateNullableIntArray(1), 1);
            CheckExceptionNullableIntArrayIndex(GenerateNullableIntArray(5), -1);
            CheckExceptionNullableIntArrayIndex(GenerateNullableIntArray(5), 5);
        }

        #endregion

        #region NullableLong tests

        [Fact]
        public static void CheckNullableLongArrayIndexTest()
        {
            CheckNullableLongArrayIndex(GenerateNullableLongArray(0));
            CheckNullableLongArrayIndex(GenerateNullableLongArray(1));
            CheckNullableLongArrayIndex(GenerateNullableLongArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableLongArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableLongArrayIndex(null, -1);
            CheckExceptionNullableLongArrayIndex(null, 0);
            CheckExceptionNullableLongArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableLongArrayIndex(GenerateNullableLongArray(0), -1);
            CheckExceptionNullableLongArrayIndex(GenerateNullableLongArray(0), 0);
            CheckExceptionNullableLongArrayIndex(GenerateNullableLongArray(1), -1);
            CheckExceptionNullableLongArrayIndex(GenerateNullableLongArray(1), 1);
            CheckExceptionNullableLongArrayIndex(GenerateNullableLongArray(5), -1);
            CheckExceptionNullableLongArrayIndex(GenerateNullableLongArray(5), 5);
        }

        #endregion

        #region NullableSByte tests

        [Fact]
        public static void CheckNullableSByteArrayIndexTest()
        {
            CheckNullableSByteArrayIndex(GenerateNullableSByteArray(0));
            CheckNullableSByteArrayIndex(GenerateNullableSByteArray(1));
            CheckNullableSByteArrayIndex(GenerateNullableSByteArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableSByteArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableSByteArrayIndex(null, -1);
            CheckExceptionNullableSByteArrayIndex(null, 0);
            CheckExceptionNullableSByteArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableSByteArrayIndex(GenerateNullableSByteArray(0), -1);
            CheckExceptionNullableSByteArrayIndex(GenerateNullableSByteArray(0), 0);
            CheckExceptionNullableSByteArrayIndex(GenerateNullableSByteArray(1), -1);
            CheckExceptionNullableSByteArrayIndex(GenerateNullableSByteArray(1), 1);
            CheckExceptionNullableSByteArrayIndex(GenerateNullableSByteArray(5), -1);
            CheckExceptionNullableSByteArrayIndex(GenerateNullableSByteArray(5), 5);
        }

        #endregion

        #region NullableShort tests

        [Fact]
        public static void CheckNullableShortArrayIndexTest()
        {
            CheckNullableShortArrayIndex(GenerateNullableShortArray(0));
            CheckNullableShortArrayIndex(GenerateNullableShortArray(1));
            CheckNullableShortArrayIndex(GenerateNullableShortArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableShortArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableShortArrayIndex(null, -1);
            CheckExceptionNullableShortArrayIndex(null, 0);
            CheckExceptionNullableShortArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableShortArrayIndex(GenerateNullableShortArray(0), -1);
            CheckExceptionNullableShortArrayIndex(GenerateNullableShortArray(0), 0);
            CheckExceptionNullableShortArrayIndex(GenerateNullableShortArray(1), -1);
            CheckExceptionNullableShortArrayIndex(GenerateNullableShortArray(1), 1);
            CheckExceptionNullableShortArrayIndex(GenerateNullableShortArray(5), -1);
            CheckExceptionNullableShortArrayIndex(GenerateNullableShortArray(5), 5);
        }

        #endregion

        #region NullableStruct tests

        [Fact]
        public static void CheckNullableStructArrayIndexTest()
        {
            CheckNullableStructArrayIndex(GenerateNullableStructArray(0));
            CheckNullableStructArrayIndex(GenerateNullableStructArray(1));
            CheckNullableStructArrayIndex(GenerateNullableStructArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableStructArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableStructArrayIndex(null, -1);
            CheckExceptionNullableStructArrayIndex(null, 0);
            CheckExceptionNullableStructArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableStructArrayIndex(GenerateNullableStructArray(0), -1);
            CheckExceptionNullableStructArrayIndex(GenerateNullableStructArray(0), 0);
            CheckExceptionNullableStructArrayIndex(GenerateNullableStructArray(1), -1);
            CheckExceptionNullableStructArrayIndex(GenerateNullableStructArray(1), 1);
            CheckExceptionNullableStructArrayIndex(GenerateNullableStructArray(5), -1);
            CheckExceptionNullableStructArrayIndex(GenerateNullableStructArray(5), 5);
        }

        #endregion

        #region NullableStructWithString tests

        [Fact]
        public static void CheckNullableStructWithStringArrayIndexTest()
        {
            CheckNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(0));
            CheckNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(1));
            CheckNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableStructWithStringArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableStructWithStringArrayIndex(null, -1);
            CheckExceptionNullableStructWithStringArrayIndex(null, 0);
            CheckExceptionNullableStructWithStringArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(0), -1);
            CheckExceptionNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(0), 0);
            CheckExceptionNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(1), -1);
            CheckExceptionNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(1), 1);
            CheckExceptionNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(5), -1);
            CheckExceptionNullableStructWithStringArrayIndex(GenerateNullableStructWithStringArray(5), 5);
        }

        #endregion

        #region NullableStructWithStringAndValue tests

        [Fact]
        public static void CheckNullableStructWithStringAndValueArrayIndexTest()
        {
            CheckNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(0));
            CheckNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(1));
            CheckNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableStructWithStringAndValueArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableStructWithStringAndValueArrayIndex(null, -1);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(null, 0);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(0), -1);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(0), 0);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(1), -1);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(1), 1);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(5), -1);
            CheckExceptionNullableStructWithStringAndValueArrayIndex(GenerateNullableStructWithStringAndValueArray(5), 5);
        }

        #endregion

        #region NullableStructWithTwoValues tests

        [Fact]
        public static void CheckNullableStructWithTwoValuesArrayIndexTest()
        {
            CheckNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(0));
            CheckNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(1));
            CheckNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableStructWithTwoValuesArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableStructWithTwoValuesArrayIndex(null, -1);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(null, 0);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(0), -1);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(0), 0);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(1), -1);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(1), 1);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(5), -1);
            CheckExceptionNullableStructWithTwoValuesArrayIndex(GenerateNullableStructWithTwoValuesArray(5), 5);
        }

        #endregion

        #region NullableStructWithStruct tests

        [Fact]
        public static void CheckNullableStructWithStructArrayIndexTest()
        {
            CheckNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(0));
            CheckNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(1));
            CheckNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableStructWithStructArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableStructWithStructArrayIndex(null, -1);
            CheckExceptionNullableStructWithStructArrayIndex(null, 0);
            CheckExceptionNullableStructWithStructArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(0), -1);
            CheckExceptionNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(0), 0);
            CheckExceptionNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(1), -1);
            CheckExceptionNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(1), 1);
            CheckExceptionNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(5), -1);
            CheckExceptionNullableStructWithStructArrayIndex(GenerateNullableStructWithStructArray(5), 5);
        }

        #endregion

        #region NullableUInt tests

        [Fact]
        public static void CheckNullableUIntArrayIndexTest()
        {
            CheckNullableUIntArrayIndex(GenerateNullableUIntArray(0));
            CheckNullableUIntArrayIndex(GenerateNullableUIntArray(1));
            CheckNullableUIntArrayIndex(GenerateNullableUIntArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableUIntArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableUIntArrayIndex(null, -1);
            CheckExceptionNullableUIntArrayIndex(null, 0);
            CheckExceptionNullableUIntArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableUIntArrayIndex(GenerateNullableUIntArray(0), -1);
            CheckExceptionNullableUIntArrayIndex(GenerateNullableUIntArray(0), 0);
            CheckExceptionNullableUIntArrayIndex(GenerateNullableUIntArray(1), -1);
            CheckExceptionNullableUIntArrayIndex(GenerateNullableUIntArray(1), 1);
            CheckExceptionNullableUIntArrayIndex(GenerateNullableUIntArray(5), -1);
            CheckExceptionNullableUIntArrayIndex(GenerateNullableUIntArray(5), 5);
        }

        #endregion

        #region NullableULong tests

        [Fact]
        public static void CheckNullableULongArrayIndexTest()
        {
            CheckNullableULongArrayIndex(GenerateNullableULongArray(0));
            CheckNullableULongArrayIndex(GenerateNullableULongArray(1));
            CheckNullableULongArrayIndex(GenerateNullableULongArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableULongArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableULongArrayIndex(null, -1);
            CheckExceptionNullableULongArrayIndex(null, 0);
            CheckExceptionNullableULongArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableULongArrayIndex(GenerateNullableULongArray(0), -1);
            CheckExceptionNullableULongArrayIndex(GenerateNullableULongArray(0), 0);
            CheckExceptionNullableULongArrayIndex(GenerateNullableULongArray(1), -1);
            CheckExceptionNullableULongArrayIndex(GenerateNullableULongArray(1), 1);
            CheckExceptionNullableULongArrayIndex(GenerateNullableULongArray(5), -1);
            CheckExceptionNullableULongArrayIndex(GenerateNullableULongArray(5), 5);
        }

        #endregion

        #region NullableUShort tests

        [Fact]
        public static void CheckNullableUShortArrayIndexTest()
        {
            CheckNullableUShortArrayIndex(GenerateNullableUShortArray(0));
            CheckNullableUShortArrayIndex(GenerateNullableUShortArray(1));
            CheckNullableUShortArrayIndex(GenerateNullableUShortArray(5));
        }

        [Fact]
        public static void CheckExceptionNullableUShortArrayIndexTest()
        {
            // null arrays
            CheckExceptionNullableUShortArrayIndex(null, -1);
            CheckExceptionNullableUShortArrayIndex(null, 0);
            CheckExceptionNullableUShortArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionNullableUShortArrayIndex(GenerateNullableUShortArray(0), -1);
            CheckExceptionNullableUShortArrayIndex(GenerateNullableUShortArray(0), 0);
            CheckExceptionNullableUShortArrayIndex(GenerateNullableUShortArray(1), -1);
            CheckExceptionNullableUShortArrayIndex(GenerateNullableUShortArray(1), 1);
            CheckExceptionNullableUShortArrayIndex(GenerateNullableUShortArray(5), -1);
            CheckExceptionNullableUShortArrayIndex(GenerateNullableUShortArray(5), 5);
        }

        #endregion

        #region Generic tests

        [Fact]
        public static void CheckNullableGenericWithStructRestriction()
        {
            CheckNullableGenericWithStructRestrictionHelper<E>();
        }

        [Fact]
        public static void CheckExceptionNullableGenericWithStructRestriction()
        {
            CheckExceptionNullableGenericWithStructRestrictionHelper<E>();
        }

        [Fact]
        public static void CheckNullableGenericWithStructRestriction2()
        {
            CheckNullableGenericWithStructRestrictionHelper<S>();
        }

        [Fact]
        public static void CheckExceptionNullableGenericWithStructRestriction2()
        {
            CheckExceptionNullableGenericWithStructRestrictionHelper<S>();
        }

        [Fact]
        public static void CheckNullableGenericWithStructRestriction3()
        {
            CheckNullableGenericWithStructRestrictionHelper<Scs>();
        }

        [Fact]
        public static void CheckExceptionNullableGenericWithStructRestriction3()
        {
            CheckExceptionNullableGenericWithStructRestrictionHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckNullableGenericWithStructRestrictionHelper<Ts>() where Ts : struct
        {
            CheckNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(0));
            CheckNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(1));
            CheckNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(5));
        }

        private static void CheckExceptionNullableGenericWithStructRestrictionHelper<Ts>() where Ts : struct
        {
            // null arrays
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(null, -1);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(null, 0);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(null, 1);

            // index out of bounds
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(0), -1);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(0), 0);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(1), -1);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(1), 1);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(5), -1);
            CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(GenerateNullableGenericWithStructRestrictionArray<Ts>(5), 5);
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
        private static void CheckNullableBoolArrayIndex(bool?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableBoolArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableByteArrayIndex(byte?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableByteArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableCharArrayIndex(char?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableCharArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableDecimalArrayIndex(decimal?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableDecimalArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableDoubleArrayIndex(double?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableDoubleArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableEnumArrayIndex(E?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableEnumArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableLongEnumArrayIndex(El?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableLongEnumArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableFloatArrayIndex(float?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableFloatArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableIntArrayIndex(int?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableIntArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableLongArrayIndex(long?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableLongArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableSByteArrayIndex(sbyte?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableSByteArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableShortArrayIndex(short?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableShortArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableStructArrayIndex(S?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableStructArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableStructWithStringArrayIndex(Sc?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableStructWithStringArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableStructWithStringAndValueArrayIndex(Scs?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableStructWithStringAndValueArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableStructWithTwoValuesArrayIndex(Sp?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableStructWithTwoValuesArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableStructWithStructArrayIndex(Ss?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableStructWithStructArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableUIntArrayIndex(uint?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableUIntArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableULongArrayIndex(ulong?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableULongArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableUShortArrayIndex(ushort?[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableUShortArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckNullableGenericWithStructRestrictionArrayIndex<Ts>(Ts?[] array) where Ts : struct
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckNullableGenericWithStructRestrictionArrayIndexExpression<Ts>(array, i);
            }

            Assert.True(success);
        }

        #endregion

        #region Check index expression
        private static bool CheckNullableBoolArrayIndexExpression(bool?[] array, int index)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(bool?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableByteArrayIndexExpression(byte?[] array, int index)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(byte?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableCharArrayIndexExpression(char?[] array, int index)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(char?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableDecimalArrayIndexExpression(decimal?[] array, int index)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(decimal?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableDoubleArrayIndexExpression(double?[] array, int index)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(double?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableEnumArrayIndexExpression(E?[] array, int index)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(E?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableLongEnumArrayIndexExpression(El?[] array, int index)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(El?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableFloatArrayIndexExpression(float?[] array, int index)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(float?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableIntArrayIndexExpression(int?[] array, int index)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(int?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableLongArrayIndexExpression(long?[] array, int index)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(long?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableSByteArrayIndexExpression(sbyte?[] array, int index)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(sbyte?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableShortArrayIndexExpression(short?[] array, int index)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(short?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableStructArrayIndexExpression(S?[] array, int index)
        {
            Expression<Func<S?>> e =
                Expression.Lambda<Func<S?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(S?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableStructWithStringArrayIndexExpression(Sc?[] array, int index)
        {
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Sc?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableStructWithStringAndValueArrayIndexExpression(Scs?[] array, int index)
        {
            Expression<Func<Scs?>> e =
                Expression.Lambda<Func<Scs?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Scs?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableStructWithTwoValuesArrayIndexExpression(Sp?[] array, int index)
        {
            Expression<Func<Sp?>> e =
                Expression.Lambda<Func<Sp?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Sp?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableStructWithStructArrayIndexExpression(Ss?[] array, int index)
        {
            Expression<Func<Ss?>> e =
                Expression.Lambda<Func<Ss?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Ss?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableUIntArrayIndexExpression(uint?[] array, int index)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(uint?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableULongArrayIndexExpression(ulong?[] array, int index)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(ulong?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableUShortArrayIndexExpression(ushort?[] array, int index)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(ushort?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckNullableGenericWithStructRestrictionArrayIndexExpression<Ts>(Ts?[] array, int index) where Ts : struct
        {
            Expression<Func<Ts?>> e =
                Expression.Lambda<Func<Ts?>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Ts?[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        #endregion

        #region Check exception array index

        private static void CheckExceptionNullableBoolArrayIndex(bool?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableBoolArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableByteArrayIndex(byte?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableByteArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableCharArrayIndex(char?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableCharArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableDecimalArrayIndex(decimal?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableDecimalArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableDoubleArrayIndex(double?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableDoubleArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableEnumArrayIndex(E?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableEnumArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableLongEnumArrayIndex(El?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableLongEnumArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableFloatArrayIndex(float?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableFloatArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableIntArrayIndex(int?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableIntArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableLongArrayIndex(long?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableLongArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableSByteArrayIndex(sbyte?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableSByteArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableShortArrayIndex(short?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableShortArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableStructArrayIndex(S?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableStructArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableStructWithStringArrayIndex(Sc?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableStructWithStringArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableStructWithStringAndValueArrayIndex(Scs?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableStructWithStringAndValueArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableStructWithTwoValuesArrayIndex(Sp?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableStructWithTwoValuesArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableStructWithStructArrayIndex(Ss?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableStructWithStructArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableUIntArrayIndex(uint?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableUIntArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableULongArrayIndex(ulong?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableULongArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableUShortArrayIndex(ushort?[] array, int index)
        {
            bool success = true;
            try
            {
                CheckNullableUShortArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionNullableGenericWithStructRestrictionArrayIndex<Ts>(Ts?[] array, int index) where Ts : struct
        {
            bool success = true;
            try
            {
                CheckNullableGenericWithStructRestrictionArrayIndexExpression<Ts>(array, index); // expect to fail
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
