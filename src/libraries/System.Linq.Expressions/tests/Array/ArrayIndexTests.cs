// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Array
{
    public static unsafe class ArrayIndexTests
    {
        #region Bool tests

        [Fact]
        public static void CheckBoolArrayIndexTest()
        {
            CheckBoolArrayIndex(GenerateBoolArray(0));
            CheckBoolArrayIndex(GenerateBoolArray(1));
            CheckBoolArrayIndex(GenerateBoolArray(5));
        }

        [Fact]
        public static void CheckExceptionBoolArrayIndexTest()
        {
            // null arrays
            CheckExceptionBoolArrayIndex(null, -1);
            CheckExceptionBoolArrayIndex(null, 0);
            CheckExceptionBoolArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionBoolArrayIndex(GenerateBoolArray(0), -1);
            CheckExceptionBoolArrayIndex(GenerateBoolArray(0), 0);
            CheckExceptionBoolArrayIndex(GenerateBoolArray(1), -1);
            CheckExceptionBoolArrayIndex(GenerateBoolArray(1), 1);
            CheckExceptionBoolArrayIndex(GenerateBoolArray(5), -1);
            CheckExceptionBoolArrayIndex(GenerateBoolArray(5), 5);
        }

        #endregion

        #region Byte tests

        [Fact]
        public static void CheckByteArrayIndexTest()
        {
            CheckByteArrayIndex(GenerateByteArray(0));
            CheckByteArrayIndex(GenerateByteArray(1));
            CheckByteArrayIndex(GenerateByteArray(5));
        }

        [Fact]
        public static void CheckExceptionByteArrayIndexTest()
        {
            // null arrays
            CheckExceptionByteArrayIndex(null, -1);
            CheckExceptionByteArrayIndex(null, 0);
            CheckExceptionByteArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionByteArrayIndex(GenerateByteArray(0), -1);
            CheckExceptionByteArrayIndex(GenerateByteArray(0), 0);
            CheckExceptionByteArrayIndex(GenerateByteArray(1), -1);
            CheckExceptionByteArrayIndex(GenerateByteArray(1), 1);
            CheckExceptionByteArrayIndex(GenerateByteArray(5), -1);
            CheckExceptionByteArrayIndex(GenerateByteArray(5), 5);
        }

        #endregion

        #region Custom type tests

        [Fact]
        public static void CheckCustomArrayIndexTest()
        {
            CheckCustomArrayIndex(GenerateCustomArray(0));
            CheckCustomArrayIndex(GenerateCustomArray(1));
            CheckCustomArrayIndex(GenerateCustomArray(5));
        }

        [Fact]
        public static void CheckExceptionCustomArrayIndexTest()
        {
            // null arrays
            CheckExceptionCustomArrayIndex(null, -1);
            CheckExceptionCustomArrayIndex(null, 0);
            CheckExceptionCustomArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionCustomArrayIndex(GenerateCustomArray(0), -1);
            CheckExceptionCustomArrayIndex(GenerateCustomArray(0), 0);
            CheckExceptionCustomArrayIndex(GenerateCustomArray(1), -1);
            CheckExceptionCustomArrayIndex(GenerateCustomArray(1), 1);
            CheckExceptionCustomArrayIndex(GenerateCustomArray(5), -1);
            CheckExceptionCustomArrayIndex(GenerateCustomArray(5), 5);
        }

        #endregion

        #region Char tests

        [Fact]
        public static void CheckCharArrayIndexTest()
        {
            CheckCharArrayIndex(GenerateCharArray(0));
            CheckCharArrayIndex(GenerateCharArray(1));
            CheckCharArrayIndex(GenerateCharArray(5));
        }

        [Fact]
        public static void CheckExceptionCharArrayIndexTest()
        {
            // null arrays
            CheckExceptionCharArrayIndex(null, -1);
            CheckExceptionCharArrayIndex(null, 0);
            CheckExceptionCharArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionCharArrayIndex(GenerateCharArray(0), -1);
            CheckExceptionCharArrayIndex(GenerateCharArray(0), 0);
            CheckExceptionCharArrayIndex(GenerateCharArray(1), -1);
            CheckExceptionCharArrayIndex(GenerateCharArray(1), 1);
            CheckExceptionCharArrayIndex(GenerateCharArray(5), -1);
            CheckExceptionCharArrayIndex(GenerateCharArray(5), 5);
        }

        #endregion

        #region Custom 2 type tests

        [Fact]
        public static void CheckCustom2ArrayIndexTest()
        {
            CheckCustom2ArrayIndex(GenerateCustom2Array(0));
            CheckCustom2ArrayIndex(GenerateCustom2Array(1));
            CheckCustom2ArrayIndex(GenerateCustom2Array(5));
        }

        [Fact]
        public static void CheckExceptionCustom2ArrayIndexTest()
        {
            // null arrays
            CheckExceptionCustom2ArrayIndex(null, -1);
            CheckExceptionCustom2ArrayIndex(null, 0);
            CheckExceptionCustom2ArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionCustom2ArrayIndex(GenerateCustom2Array(0), -1);
            CheckExceptionCustom2ArrayIndex(GenerateCustom2Array(0), 0);
            CheckExceptionCustom2ArrayIndex(GenerateCustom2Array(1), -1);
            CheckExceptionCustom2ArrayIndex(GenerateCustom2Array(1), 1);
            CheckExceptionCustom2ArrayIndex(GenerateCustom2Array(5), -1);
            CheckExceptionCustom2ArrayIndex(GenerateCustom2Array(5), 5);
        }

        #endregion

        #region Decimal tests

        [Fact]
        public static void CheckDecimalArrayIndexTest()
        {
            CheckDecimalArrayIndex(GenerateDecimalArray(0));
            CheckDecimalArrayIndex(GenerateDecimalArray(1));
            CheckDecimalArrayIndex(GenerateDecimalArray(5));
        }

        [Fact]
        public static void CheckExceptionDecimalArrayIndexTest()
        {
            // null arrays
            CheckExceptionDecimalArrayIndex(null, -1);
            CheckExceptionDecimalArrayIndex(null, 0);
            CheckExceptionDecimalArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionDecimalArrayIndex(GenerateDecimalArray(0), -1);
            CheckExceptionDecimalArrayIndex(GenerateDecimalArray(0), 0);
            CheckExceptionDecimalArrayIndex(GenerateDecimalArray(1), -1);
            CheckExceptionDecimalArrayIndex(GenerateDecimalArray(1), 1);
            CheckExceptionDecimalArrayIndex(GenerateDecimalArray(5), -1);
            CheckExceptionDecimalArrayIndex(GenerateDecimalArray(5), 5);
        }

        #endregion

        #region Delegate tests

        [Fact]
        public static void CheckDelegateArrayIndexTest()
        {
            CheckDelegateArrayIndex(GenerateDelegateArray(0));
            CheckDelegateArrayIndex(GenerateDelegateArray(1));
            CheckDelegateArrayIndex(GenerateDelegateArray(5));
        }

        [Fact]
        public static void CheckExceptionDelegateArrayIndexTest()
        {
            // null arrays
            CheckExceptionDelegateArrayIndex(null, -1);
            CheckExceptionDelegateArrayIndex(null, 0);
            CheckExceptionDelegateArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionDelegateArrayIndex(GenerateDelegateArray(0), -1);
            CheckExceptionDelegateArrayIndex(GenerateDelegateArray(0), 0);
            CheckExceptionDelegateArrayIndex(GenerateDelegateArray(1), -1);
            CheckExceptionDelegateArrayIndex(GenerateDelegateArray(1), 1);
            CheckExceptionDelegateArrayIndex(GenerateDelegateArray(5), -1);
            CheckExceptionDelegateArrayIndex(GenerateDelegateArray(5), 5);
        }

        #endregion

        #region Double tests

        [Fact]
        public static void CheckDoubleArrayIndexTest()
        {
            CheckDoubleArrayIndex(GenerateDoubleArray(0));
            CheckDoubleArrayIndex(GenerateDoubleArray(1));
            CheckDoubleArrayIndex(GenerateDoubleArray(9));
        }

        [Fact]
        public static void CheckExceptionDoubleArrayIndexTest()
        {
            // null arrays
            CheckExceptionDoubleArrayIndex(null, -1);
            CheckExceptionDoubleArrayIndex(null, 0);
            CheckExceptionDoubleArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionDoubleArrayIndex(GenerateDoubleArray(0), -1);
            CheckExceptionDoubleArrayIndex(GenerateDoubleArray(0), 0);
            CheckExceptionDoubleArrayIndex(GenerateDoubleArray(1), -1);
            CheckExceptionDoubleArrayIndex(GenerateDoubleArray(1), 1);
            CheckExceptionDoubleArrayIndex(GenerateDoubleArray(9), -1);
            CheckExceptionDoubleArrayIndex(GenerateDoubleArray(9), 9);
        }

        #endregion

        #region Enum tests

        [Fact]
        public static void CheckEnumArrayIndexTest()
        {
            CheckEnumArrayIndex(GenerateEnumArray(0));
            CheckEnumArrayIndex(GenerateEnumArray(1));
            CheckEnumArrayIndex(GenerateEnumArray(5));
        }

        [Fact]
        public static void CheckExceptionEnumArrayIndexTest()
        {
            // null arrays
            CheckExceptionEnumArrayIndex(null, -1);
            CheckExceptionEnumArrayIndex(null, 0);
            CheckExceptionEnumArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionEnumArrayIndex(GenerateEnumArray(0), -1);
            CheckExceptionEnumArrayIndex(GenerateEnumArray(0), 0);
            CheckExceptionEnumArrayIndex(GenerateEnumArray(1), -1);
            CheckExceptionEnumArrayIndex(GenerateEnumArray(1), 1);
            CheckExceptionEnumArrayIndex(GenerateEnumArray(5), -1);
            CheckExceptionEnumArrayIndex(GenerateEnumArray(5), 5);
        }

        #endregion

        #region Enum long tests

        [Fact]
        public static void CheckEnumLongArrayIndexTest()
        {
            CheckEnumLongArrayIndex(GenerateEnumLongArray(0));
            CheckEnumLongArrayIndex(GenerateEnumLongArray(1));
            CheckEnumLongArrayIndex(GenerateEnumLongArray(5));
        }

        [Fact]
        public static void CheckExceptionEnumLongArrayIndexTest()
        {
            // null arrays
            CheckExceptionEnumLongArrayIndex(null, -1);
            CheckExceptionEnumLongArrayIndex(null, 0);
            CheckExceptionEnumLongArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionEnumLongArrayIndex(GenerateEnumLongArray(0), -1);
            CheckExceptionEnumLongArrayIndex(GenerateEnumLongArray(0), 0);
            CheckExceptionEnumLongArrayIndex(GenerateEnumLongArray(1), -1);
            CheckExceptionEnumLongArrayIndex(GenerateEnumLongArray(1), 1);
            CheckExceptionEnumLongArrayIndex(GenerateEnumLongArray(5), -1);
            CheckExceptionEnumLongArrayIndex(GenerateEnumLongArray(5), 5);
        }

        #endregion

        #region Float tests

        [Fact]
        public static void CheckFloatArrayIndexTest()
        {
            CheckFloatArrayIndex(GenerateFloatArray(0));
            CheckFloatArrayIndex(GenerateFloatArray(1));
            CheckFloatArrayIndex(GenerateFloatArray(9));
        }

        [Fact]
        public static void CheckExceptionFloatArrayIndexTest()
        {
            // null arrays
            CheckExceptionFloatArrayIndex(null, -1);
            CheckExceptionFloatArrayIndex(null, 0);
            CheckExceptionFloatArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionFloatArrayIndex(GenerateFloatArray(0), -1);
            CheckExceptionFloatArrayIndex(GenerateFloatArray(0), 0);
            CheckExceptionFloatArrayIndex(GenerateFloatArray(1), -1);
            CheckExceptionFloatArrayIndex(GenerateFloatArray(1), 1);
            CheckExceptionFloatArrayIndex(GenerateFloatArray(9), -1);
            CheckExceptionFloatArrayIndex(GenerateFloatArray(9), 9);
        }

        #endregion

        #region Func tests

        [Fact]
        public static void CheckFuncArrayIndexTest()
        {
            CheckFuncArrayIndex(GenerateFuncArray(0));
            CheckFuncArrayIndex(GenerateFuncArray(1));
            CheckFuncArrayIndex(GenerateFuncArray(5));
        }

        [Fact]
        public static void CheckExceptionFuncArrayIndexTest()
        {
            // null arrays
            CheckExceptionFuncArrayIndex(null, -1);
            CheckExceptionFuncArrayIndex(null, 0);
            CheckExceptionFuncArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionFuncArrayIndex(GenerateFuncArray(0), -1);
            CheckExceptionFuncArrayIndex(GenerateFuncArray(0), 0);
            CheckExceptionFuncArrayIndex(GenerateFuncArray(1), -1);
            CheckExceptionFuncArrayIndex(GenerateFuncArray(1), 1);
            CheckExceptionFuncArrayIndex(GenerateFuncArray(5), -1);
            CheckExceptionFuncArrayIndex(GenerateFuncArray(5), 5);
        }

        #endregion

        #region Interface tests

        [Fact]
        public static void CheckInterfaceArrayIndexTest()
        {
            CheckInterfaceArrayIndex(GenerateInterfaceArray(0));
            CheckInterfaceArrayIndex(GenerateInterfaceArray(1));
            CheckInterfaceArrayIndex(GenerateInterfaceArray(5));
        }

        [Fact]
        public static void CheckExceptionInterfaceArrayIndexTest()
        {
            // null arrays
            CheckExceptionInterfaceArrayIndex(null, -1);
            CheckExceptionInterfaceArrayIndex(null, 0);
            CheckExceptionInterfaceArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionInterfaceArrayIndex(GenerateInterfaceArray(0), -1);
            CheckExceptionInterfaceArrayIndex(GenerateInterfaceArray(0), 0);
            CheckExceptionInterfaceArrayIndex(GenerateInterfaceArray(1), -1);
            CheckExceptionInterfaceArrayIndex(GenerateInterfaceArray(1), 1);
            CheckExceptionInterfaceArrayIndex(GenerateInterfaceArray(5), -1);
            CheckExceptionInterfaceArrayIndex(GenerateInterfaceArray(5), 5);
        }

        #endregion

        #region IEquatable custom tests

        [Fact]
        public static void CheckIEquatableCustomArrayIndexTest()
        {
            CheckIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(0));
            CheckIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(1));
            CheckIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(5));
        }

        [Fact]
        public static void CheckExceptionIEquatableCustomArrayIndexTest()
        {
            // null arrays
            CheckExceptionIEquatableCustomArrayIndex(null, -1);
            CheckExceptionIEquatableCustomArrayIndex(null, 0);
            CheckExceptionIEquatableCustomArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(0), -1);
            CheckExceptionIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(0), 0);
            CheckExceptionIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(1), -1);
            CheckExceptionIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(1), 1);
            CheckExceptionIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(5), -1);
            CheckExceptionIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(5), 5);
        }

        #endregion

        #region IEquatable custom 2 tests

        [Fact]
        public static void CheckIEquatableCustom2ArrayIndexTest()
        {
            CheckIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(0));
            CheckIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(1));
            CheckIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(5));
        }

        [Fact]
        public static void CheckExceptionIEquatableCustom2ArrayIndexTest()
        {
            // null arrays
            CheckExceptionIEquatableCustom2ArrayIndex(null, -1);
            CheckExceptionIEquatableCustom2ArrayIndex(null, 0);
            CheckExceptionIEquatableCustom2ArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(0), -1);
            CheckExceptionIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(0), 0);
            CheckExceptionIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(1), -1);
            CheckExceptionIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(1), 1);
            CheckExceptionIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(5), -1);
            CheckExceptionIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(5), 5);
        }

        #endregion

        #region Int tests

        [Fact]
        public static void CheckIntArrayIndexTest()
        {
            CheckIntArrayIndex(GenerateIntArray(0));
            CheckIntArrayIndex(GenerateIntArray(1));
            CheckIntArrayIndex(GenerateIntArray(5));
        }

        [Fact]
        public static void CheckExceptionIntArrayIndexTest()
        {
            // null arrays
            CheckExceptionIntArrayIndex(null, -1);
            CheckExceptionIntArrayIndex(null, 0);
            CheckExceptionIntArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionIntArrayIndex(GenerateIntArray(0), -1);
            CheckExceptionIntArrayIndex(GenerateIntArray(0), 0);
            CheckExceptionIntArrayIndex(GenerateIntArray(1), -1);
            CheckExceptionIntArrayIndex(GenerateIntArray(1), 1);
            CheckExceptionIntArrayIndex(GenerateIntArray(5), -1);
            CheckExceptionIntArrayIndex(GenerateIntArray(5), 5);
        }

        #endregion

        #region Long tests

        [Fact]
        public static void CheckLongArrayIndexTest()
        {
            CheckLongArrayIndex(GenerateLongArray(0));
            CheckLongArrayIndex(GenerateLongArray(1));
            CheckLongArrayIndex(GenerateLongArray(5));
        }

        [Fact]
        public static void CheckExceptionLongArrayIndexTest()
        {
            // null arrays
            CheckExceptionLongArrayIndex(null, -1);
            CheckExceptionLongArrayIndex(null, 0);
            CheckExceptionLongArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionLongArrayIndex(GenerateLongArray(0), -1);
            CheckExceptionLongArrayIndex(GenerateLongArray(0), 0);
            CheckExceptionLongArrayIndex(GenerateLongArray(1), -1);
            CheckExceptionLongArrayIndex(GenerateLongArray(1), 1);
            CheckExceptionLongArrayIndex(GenerateLongArray(5), -1);
            CheckExceptionLongArrayIndex(GenerateLongArray(5), 5);
        }

        #endregion

        #region Object tests

        [Fact]
        public static void CheckObjectArrayIndexTest()
        {
            CheckObjectArrayIndex(GenerateObjectArray(0));
            CheckObjectArrayIndex(GenerateObjectArray(1));
            CheckObjectArrayIndex(GenerateObjectArray(5));
        }

        [Fact]
        public static void CheckExceptionObjectArrayIndexTest()
        {
            // null arrays
            CheckExceptionObjectArrayIndex(null, -1);
            CheckExceptionObjectArrayIndex(null, 0);
            CheckExceptionObjectArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionObjectArrayIndex(GenerateObjectArray(0), -1);
            CheckExceptionObjectArrayIndex(GenerateObjectArray(0), 0);
            CheckExceptionObjectArrayIndex(GenerateObjectArray(1), -1);
            CheckExceptionObjectArrayIndex(GenerateObjectArray(1), 1);
            CheckExceptionObjectArrayIndex(GenerateObjectArray(5), -1);
            CheckExceptionObjectArrayIndex(GenerateObjectArray(5), 5);
        }

        #endregion

        #region Struct tests

        [Fact]
        public static void CheckStructArrayIndexTest()
        {
            CheckStructArrayIndex(GenerateStructArray(0));
            CheckStructArrayIndex(GenerateStructArray(1));
            CheckStructArrayIndex(GenerateStructArray(5));
        }

        [Fact]
        public static void CheckExceptionStructArrayIndexTest()
        {
            // null arrays
            CheckExceptionStructArrayIndex(null, -1);
            CheckExceptionStructArrayIndex(null, 0);
            CheckExceptionStructArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionStructArrayIndex(GenerateStructArray(0), -1);
            CheckExceptionStructArrayIndex(GenerateStructArray(0), 0);
            CheckExceptionStructArrayIndex(GenerateStructArray(1), -1);
            CheckExceptionStructArrayIndex(GenerateStructArray(1), 1);
            CheckExceptionStructArrayIndex(GenerateStructArray(5), -1);
            CheckExceptionStructArrayIndex(GenerateStructArray(5), 5);
        }

        #endregion

        #region SByte tests

        [Fact]
        public static void CheckSByteArrayIndexTest()
        {
            CheckSByteArrayIndex(GenerateSByteArray(0));
            CheckSByteArrayIndex(GenerateSByteArray(1));
            CheckSByteArrayIndex(GenerateSByteArray(5));
        }

        [Fact]
        public static void CheckExceptionSByteArrayIndexTest()
        {
            // null arrays
            CheckExceptionSByteArrayIndex(null, -1);
            CheckExceptionSByteArrayIndex(null, 0);
            CheckExceptionSByteArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionSByteArrayIndex(GenerateSByteArray(0), -1);
            CheckExceptionSByteArrayIndex(GenerateSByteArray(0), 0);
            CheckExceptionSByteArrayIndex(GenerateSByteArray(1), -1);
            CheckExceptionSByteArrayIndex(GenerateSByteArray(1), 1);
            CheckExceptionSByteArrayIndex(GenerateSByteArray(5), -1);
            CheckExceptionSByteArrayIndex(GenerateSByteArray(5), 5);
        }

        #endregion

        #region StructWithString tests

        [Fact]
        public static void CheckStructWithStringArrayIndexTest()
        {
            CheckStructWithStringArrayIndex(GenerateStructWithStringArray(0));
            CheckStructWithStringArrayIndex(GenerateStructWithStringArray(1));
            CheckStructWithStringArrayIndex(GenerateStructWithStringArray(5));
        }

        [Fact]
        public static void CheckExceptionStructWithStringArrayIndexTest()
        {
            // null arrays
            CheckExceptionStructWithStringArrayIndex(null, -1);
            CheckExceptionStructWithStringArrayIndex(null, 0);
            CheckExceptionStructWithStringArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionStructWithStringArrayIndex(GenerateStructWithStringArray(0), -1);
            CheckExceptionStructWithStringArrayIndex(GenerateStructWithStringArray(0), 0);
            CheckExceptionStructWithStringArrayIndex(GenerateStructWithStringArray(1), -1);
            CheckExceptionStructWithStringArrayIndex(GenerateStructWithStringArray(1), 1);
            CheckExceptionStructWithStringArrayIndex(GenerateStructWithStringArray(5), -1);
            CheckExceptionStructWithStringArrayIndex(GenerateStructWithStringArray(5), 5);
        }

        #endregion

        #region StructWithValueAndString tests

        [Fact]
        public static void CheckStructWithValueAndStringArrayIndexTest()
        {
            CheckStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(0));
            CheckStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(1));
            CheckStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(5));
        }

        [Fact]
        public static void CheckExceptionStructWithValueAndStringArrayIndexTest()
        {
            // null arrays
            CheckExceptionStructWithValueAndStringArrayIndex(null, -1);
            CheckExceptionStructWithValueAndStringArrayIndex(null, 0);
            CheckExceptionStructWithValueAndStringArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(0), -1);
            CheckExceptionStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(0), 0);
            CheckExceptionStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(1), -1);
            CheckExceptionStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(1), 1);
            CheckExceptionStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(5), -1);
            CheckExceptionStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(5), 5);
        }

        #endregion

        #region Short tests

        [Fact]
        public static void CheckShortArrayIndexTest()
        {
            CheckShortArrayIndex(GenerateShortArray(0));
            CheckShortArrayIndex(GenerateShortArray(1));
            CheckShortArrayIndex(GenerateShortArray(5));
        }

        [Fact]
        public static void CheckExceptionShortArrayIndexTest()
        {
            // null arrays
            CheckExceptionShortArrayIndex(null, -1);
            CheckExceptionShortArrayIndex(null, 0);
            CheckExceptionShortArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionShortArrayIndex(GenerateShortArray(0), -1);
            CheckExceptionShortArrayIndex(GenerateShortArray(0), 0);
            CheckExceptionShortArrayIndex(GenerateShortArray(1), -1);
            CheckExceptionShortArrayIndex(GenerateShortArray(1), 1);
            CheckExceptionShortArrayIndex(GenerateShortArray(5), -1);
            CheckExceptionShortArrayIndex(GenerateShortArray(5), 5);
        }

        #endregion

        #region StructWithParameters tests

        [Fact]
        public static void CheckStructWithParametersArrayIndexTest()
        {
            CheckStructWithParametersArrayIndex(GenerateStructWithParametersArray(0));
            CheckStructWithParametersArrayIndex(GenerateStructWithParametersArray(1));
            CheckStructWithParametersArrayIndex(GenerateStructWithParametersArray(5));
        }

        [Fact]
        public static void CheckExceptionStructWithParametersArrayIndexTest()
        {
            // null arrays
            CheckExceptionStructWithParametersArrayIndex(null, -1);
            CheckExceptionStructWithParametersArrayIndex(null, 0);
            CheckExceptionStructWithParametersArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionStructWithParametersArrayIndex(GenerateStructWithParametersArray(0), -1);
            CheckExceptionStructWithParametersArrayIndex(GenerateStructWithParametersArray(0), 0);
            CheckExceptionStructWithParametersArrayIndex(GenerateStructWithParametersArray(1), -1);
            CheckExceptionStructWithParametersArrayIndex(GenerateStructWithParametersArray(1), 1);
            CheckExceptionStructWithParametersArrayIndex(GenerateStructWithParametersArray(5), -1);
            CheckExceptionStructWithParametersArrayIndex(GenerateStructWithParametersArray(5), 5);
        }

        #endregion

        #region StructWithStruct tests

        [Fact]
        public static void CheckStructWithStructArrayIndexTest()
        {
            CheckStructWithStructArrayIndex(GenerateStructWithStructArray(0));
            CheckStructWithStructArrayIndex(GenerateStructWithStructArray(1));
            CheckStructWithStructArrayIndex(GenerateStructWithStructArray(5));
        }

        [Fact]
        public static void CheckExceptionStructWithStructArrayIndexTest()
        {
            // null arrays
            CheckExceptionStructWithStructArrayIndex(null, -1);
            CheckExceptionStructWithStructArrayIndex(null, 0);
            CheckExceptionStructWithStructArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionStructWithStructArrayIndex(GenerateStructWithStructArray(0), -1);
            CheckExceptionStructWithStructArrayIndex(GenerateStructWithStructArray(0), 0);
            CheckExceptionStructWithStructArrayIndex(GenerateStructWithStructArray(1), -1);
            CheckExceptionStructWithStructArrayIndex(GenerateStructWithStructArray(1), 1);
            CheckExceptionStructWithStructArrayIndex(GenerateStructWithStructArray(5), -1);
            CheckExceptionStructWithStructArrayIndex(GenerateStructWithStructArray(5), 5);
        }

        #endregion

        #region String tests

        [Fact]
        public static void CheckStringArrayIndexTest()
        {
            CheckStringArrayIndex(GenerateStringArray(0));
            CheckStringArrayIndex(GenerateStringArray(1));
            CheckStringArrayIndex(GenerateStringArray(5));
        }

        [Fact]
        public static void CheckExceptionStringArrayIndexTest()
        {
            // null arrays
            CheckExceptionStringArrayIndex(null, -1);
            CheckExceptionStringArrayIndex(null, 0);
            CheckExceptionStringArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionStringArrayIndex(GenerateStringArray(0), -1);
            CheckExceptionStringArrayIndex(GenerateStringArray(0), 0);
            CheckExceptionStringArrayIndex(GenerateStringArray(1), -1);
            CheckExceptionStringArrayIndex(GenerateStringArray(1), 1);
            CheckExceptionStringArrayIndex(GenerateStringArray(5), -1);
            CheckExceptionStringArrayIndex(GenerateStringArray(5), 5);
        }

        #endregion

        #region UInt tests

        [Fact]
        public static void CheckUIntArrayIndexTest()
        {
            CheckUIntArrayIndex(GenerateUIntArray(0));
            CheckUIntArrayIndex(GenerateUIntArray(1));
            CheckUIntArrayIndex(GenerateUIntArray(5));
        }

        [Fact]
        public static void CheckExceptionUIntArrayIndexTest()
        {
            // null arrays
            CheckExceptionUIntArrayIndex(null, -1);
            CheckExceptionUIntArrayIndex(null, 0);
            CheckExceptionUIntArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionUIntArrayIndex(GenerateUIntArray(0), -1);
            CheckExceptionUIntArrayIndex(GenerateUIntArray(0), 0);
            CheckExceptionUIntArrayIndex(GenerateUIntArray(1), -1);
            CheckExceptionUIntArrayIndex(GenerateUIntArray(1), 1);
            CheckExceptionUIntArrayIndex(GenerateUIntArray(5), -1);
            CheckExceptionUIntArrayIndex(GenerateUIntArray(5), 5);
        }

        #endregion

        #region ULong tests

        [Fact]
        public static void CheckULongArrayIndexTest()
        {
            CheckULongArrayIndex(GenerateULongArray(0));
            CheckULongArrayIndex(GenerateULongArray(1));
            CheckULongArrayIndex(GenerateULongArray(5));
        }

        [Fact]
        public static void CheckExceptionULongArrayIndexTest()
        {
            // null arrays
            CheckExceptionULongArrayIndex(null, -1);
            CheckExceptionULongArrayIndex(null, 0);
            CheckExceptionULongArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionULongArrayIndex(GenerateULongArray(0), -1);
            CheckExceptionULongArrayIndex(GenerateULongArray(0), 0);
            CheckExceptionULongArrayIndex(GenerateULongArray(1), -1);
            CheckExceptionULongArrayIndex(GenerateULongArray(1), 1);
            CheckExceptionULongArrayIndex(GenerateULongArray(5), -1);
            CheckExceptionULongArrayIndex(GenerateULongArray(5), 5);
        }

        #endregion

        #region UShort tests

        [Fact]
        public static void CheckUShortArrayIndexTest()
        {
            CheckUShortArrayIndex(GenerateUShortArray(0));
            CheckUShortArrayIndex(GenerateUShortArray(1));
            CheckUShortArrayIndex(GenerateUShortArray(5));
        }

        [Fact]
        public static void CheckExceptionUShortArrayIndexTest()
        {
            // null arrays
            CheckExceptionUShortArrayIndex(null, -1);
            CheckExceptionUShortArrayIndex(null, 0);
            CheckExceptionUShortArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionUShortArrayIndex(GenerateUShortArray(0), -1);
            CheckExceptionUShortArrayIndex(GenerateUShortArray(0), 0);
            CheckExceptionUShortArrayIndex(GenerateUShortArray(1), -1);
            CheckExceptionUShortArrayIndex(GenerateUShortArray(1), 1);
            CheckExceptionUShortArrayIndex(GenerateUShortArray(5), -1);
            CheckExceptionUShortArrayIndex(GenerateUShortArray(5), 5);
        }

        #endregion

        #region Generic tests

        [Fact]
        public static void CheckGenericCustomArrayIndexTest()
        {
            CheckGenericArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericCustomArrayIndexTest()
        {
            CheckExceptionGenericArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericEnumArrayIndexTest()
        {
            CheckGenericArrayIndexTestHelper<E>();
        }

        [Fact]
        public static void CheckExceptionGenericEnumArrayIndexTest()
        {
            CheckExceptionGenericArrayIndexTestHelper<E>();
        }

        [Fact]
        public static void CheckGenericObjectArrayIndexTest()
        {
            CheckGenericArrayIndexTestHelper<object>();
        }

        [Fact]
        public static void CheckExceptionGenericObjectArrayIndexTest()
        {
            CheckExceptionGenericArrayIndexTestHelper<object>();
        }

        [Fact]
        public static void CheckGenericStructArrayIndexTest()
        {
            CheckGenericArrayIndexTestHelper<S>();
        }

        [Fact]
        public static void CheckExceptionGenericStructArrayIndexTest()
        {
            CheckExceptionGenericArrayIndexTestHelper<S>();
        }

        [Fact]
        public static void CheckGenericStructWithValueAndStringArrayIndexTest()
        {
            CheckGenericArrayIndexTestHelper<Scs>();
        }

        [Fact]
        public static void CheckExceptionGenericStructWithValueAndStringArrayIndexTest()
        {
            CheckExceptionGenericArrayIndexTestHelper<Scs>();
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayIndexTest()
        {
            CheckGenericWithClassRestrictionArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericWithClassRestrictionArrayIndexTest()
        {
            CheckExceptionGenericWithClassRestrictionArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericWithObjectRestrictionArrayIndexTest()
        {
            CheckGenericWithClassRestrictionArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericWithObjectRestrictionArrayIndexTest()
        {
            CheckExceptionGenericWithClassRestrictionArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericWithSubClassRestrictionArrayIndexTest()
        {
            CheckGenericWithSubClassRestrictionArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericWithSubClassRestrictionArrayIndexTest()
        {
            CheckExceptionGenericWithSubClassRestrictionArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericWithNewClassRestrictionArrayIndexTest()
        {
            CheckGenericWithNewClassRestrictionArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericWithNewClassRestrictionArrayIndexTest()
        {
            CheckExceptionGenericWithNewClassRestrictionArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericWithSubClassNewRestrictionArrayIndexTest()
        {
            CheckGenericWithSubClassNewRestrictionArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericWithSubClassNewRestrictionArrayIndexTest()
        {
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericWithSubClassNewObjectRestrictionArrayIndexTest()
        {
            CheckGenericWithNewClassRestrictionArrayIndexTestHelper<object>();
        }

        [Fact]
        public static void CheckExceptionGenericWithSubClassNewObjectRestrictionArrayIndexTest()
        {
            CheckExceptionGenericWithNewClassRestrictionArrayIndexTestHelper<object>();
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayIndexTest()
        {
            CheckGenericWithStructRestrictionArrayIndexTestHelper<E>();
            CheckGenericWithStructRestrictionArrayIndexTestHelper<S>();
            CheckGenericWithStructRestrictionArrayIndexTestHelper<Scs>();
        }

        [Fact]
        public static void CheckExceptionGenericWithStructRestrictionArrayIndexTest()
        {
            CheckExceptionGenericWithStructRestrictionArrayIndexTestHelper<E>();
            CheckExceptionGenericWithStructRestrictionArrayIndexTestHelper<S>();
            CheckExceptionGenericWithStructRestrictionArrayIndexTestHelper<Scs>();
        }

        #endregion

        #region Generic test helpers

        private static void CheckGenericArrayIndexTestHelper<T>()
        {
            CheckGenericArrayIndex<T>(GenerateGenericArray<T>(0));
            CheckGenericArrayIndex<T>(GenerateGenericArray<T>(1));
            CheckGenericArrayIndex<T>(GenerateGenericArray<T>(5));
        }

        private static void CheckExceptionGenericArrayIndexTestHelper<T>()
        {
            // null arrays
            CheckExceptionGenericArrayIndex<T>(null, -1);
            CheckExceptionGenericArrayIndex<T>(null, 0);
            CheckExceptionGenericArrayIndex<T>(null, 1);

            // index out of bounds
            CheckExceptionGenericArrayIndex<T>(GenerateGenericArray<T>(0), -1);
            CheckExceptionGenericArrayIndex<T>(GenerateGenericArray<T>(0), 0);
            CheckExceptionGenericArrayIndex<T>(GenerateGenericArray<T>(1), -1);
            CheckExceptionGenericArrayIndex<T>(GenerateGenericArray<T>(1), 1);
            CheckExceptionGenericArrayIndex<T>(GenerateGenericArray<T>(5), -1);
            CheckExceptionGenericArrayIndex<T>(GenerateGenericArray<T>(5), 5);
        }

        private static void CheckGenericWithClassRestrictionArrayIndexTestHelper<Tc>() where Tc : class
        {
            CheckGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(0));
            CheckGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(1));
            CheckGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(5));
        }

        private static void CheckExceptionGenericWithClassRestrictionArrayIndexTestHelper<Tc>() where Tc : class
        {
            // null arrays
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(null, -1);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(null, 0);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(null, 1);

            // index out of bounds
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(0), -1);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(0), 0);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(1), -1);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(1), 1);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(5), -1);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(5), 5);
        }

        private static void CheckGenericWithSubClassRestrictionArrayIndexTestHelper<Tc>() where Tc : C
        {
            CheckGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(0));
            CheckGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(1));
            CheckGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(5));
        }

        private static void CheckExceptionGenericWithSubClassRestrictionArrayIndexTestHelper<Tc>() where Tc : C
        {
            // null arrays
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(null, -1);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(null, 0);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(null, 1);

            // index out of bounds
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(0), -1);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(0), 0);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(1), -1);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(1), 1);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(5), -1);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(5), 5);
        }

        private static void CheckGenericWithNewClassRestrictionArrayIndexTestHelper<Tcn>() where Tcn : class, new()
        {
            CheckGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(0));
            CheckGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(1));
            CheckGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(5));
        }

        private static void CheckExceptionGenericWithNewClassRestrictionArrayIndexTestHelper<Tcn>() where Tcn : class, new()
        {
            // null arrays
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(null, -1);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(null, 0);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(null, 1);

            // index out of bounds
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(0), -1);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(0), 0);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(1), -1);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(1), 1);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(5), -1);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(5), 5);
        }

        private static void CheckGenericWithSubClassNewRestrictionArrayIndexTestHelper<Tcn>() where Tcn : C, new()
        {
            CheckGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(0));
            CheckGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(1));
            CheckGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(5));
        }

        private static void CheckExceptionGenericWithSubClassNewRestrictionArrayIndexTestHelper<Tcn>() where Tcn : C, new()
        {
            // null arrays
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(null, -1);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(null, 0);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(null, 1);

            // index out of bounds
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(0), -1);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(0), 0);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(1), -1);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(1), 1);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(5), -1);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(5), 5);
        }

        private static void CheckGenericWithStructRestrictionArrayIndexTestHelper<Ts>() where Ts : struct
        {
            CheckGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(0));
            CheckGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(1));
            CheckGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(5));
        }

        private static void CheckExceptionGenericWithStructRestrictionArrayIndexTestHelper<Ts>() where Ts : struct
        {
            // null arrays
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(null, -1);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(null, 0);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(null, 1);

            // index out of bounds
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(0), -1);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(0), 0);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(1), -1);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(1), 1);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(5), -1);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(5), 5);
        }

        #endregion

        #region Generate array

        private static bool[] GenerateBoolArray(int size)
        {
            bool[] array = new bool[] { true, false };
            bool[] result = new bool[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static byte[] GenerateByteArray(int size)
        {
            byte[] array = new byte[] { 0, 1, byte.MaxValue };
            byte[] result = new byte[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static C[] GenerateCustomArray(int size)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            C[] result = new C[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static char[] GenerateCharArray(int size)
        {
            char[] array = new char[] { '\0', '\b', 'A', '\uffff' };
            char[] result = new char[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static D[] GenerateCustom2Array(int size)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            D[] result = new D[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static decimal[] GenerateDecimalArray(int size)
        {
            decimal[] array = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            decimal[] result = new decimal[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Delegate[] GenerateDelegateArray(int size)
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            Delegate[] result = new Delegate[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static double[] GenerateDoubleArray(int size)
        {
            double[] array = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            double[] result = new double[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static E[] GenerateEnumArray(int size)
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            E[] result = new E[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static El[] GenerateEnumLongArray(int size)
        {
            El[] array = new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            El[] result = new El[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static float[] GenerateFloatArray(int size)
        {
            float[] array = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            float[] result = new float[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Func<object>[] GenerateFuncArray(int size)
        {
            Func<object>[] array = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            Func<object>[] result = new Func<object>[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static I[] GenerateInterfaceArray(int size)
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            I[] result = new I[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static IEquatable<C>[] GenerateIEquatableCustomArray(int size)
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            IEquatable<C>[] result = new IEquatable<C>[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static IEquatable<D>[] GenerateIEquatableCustom2Array(int size)
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            IEquatable<D>[] result = new IEquatable<D>[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static int[] GenerateIntArray(int size)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            int[] result = new int[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static long[] GenerateLongArray(int size)
        {
            long[] array = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            long[] result = new long[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static object[] GenerateObjectArray(int size)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            object[] result = new object[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static S[] GenerateStructArray(int size)
        {
            S[] array = new S[] { default(S), new S() };
            S[] result = new S[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static sbyte[] GenerateSByteArray(int size)
        {
            sbyte[] array = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            sbyte[] result = new sbyte[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Sc[] GenerateStructWithStringArray(int size)
        {
            Sc[] array = new Sc[] { default(Sc), new Sc(), new Sc(null) };
            Sc[] result = new Sc[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Scs[] GenerateStructWithValueAndStringArray(int size)
        {
            Scs[] array = new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) };
            Scs[] result = new Scs[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static short[] GenerateShortArray(int size)
        {
            short[] array = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            short[] result = new short[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Sp[] GenerateStructWithParametersArray(int size)
        {
            Sp[] array = new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) };
            Sp[] result = new Sp[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Ss[] GenerateStructWithStructArray(int size)
        {
            Ss[] array = new Ss[] { default(Ss), new Ss(), new Ss(new S()) };
            Ss[] result = new Ss[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static string[] GenerateStringArray(int size)
        {
            string[] array = new string[] { null, "", "a", "foo" };
            string[] result = new string[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static uint[] GenerateUIntArray(int size)
        {
            uint[] array = new uint[] { 0, 1, uint.MaxValue };
            uint[] result = new uint[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static ulong[] GenerateULongArray(int size)
        {
            ulong[] array = new ulong[] { 0, 1, ulong.MaxValue };
            ulong[] result = new ulong[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static ushort[] GenerateUShortArray(int size)
        {
            ushort[] array = new ushort[] { 0, 1, ushort.MaxValue };
            ushort[] result = new ushort[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static T[] GenerateGenericArray<T>(int size)
        {
            T[] result = new T[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = default(T);
            }

            return result;
        }

        private static Tc[] GenerateGenericWithClassRestrictionArray<Tc>(int size) where Tc : class
        {
            Tc[] array = new Tc[] { null, default(Tc) };
            Tc[] result = new Tc[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Tc[] GenerateGenericWithSubClassRestrictionArray<Tc>(int size) where Tc : C
        {
            Tc[] array = new Tc[] { null, default(Tc), (Tc)new C() };
            Tc[] result = new Tc[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Tcn[] GenerateGenericWithNewClassRestrictionArray<Tcn>(int size) where Tcn : class, new()
        {
            Tcn[] array = new Tcn[] { null, default(Tcn), new Tcn() };
            Tcn[] result = new Tcn[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Tcn[] GenerateGenericWithSubClassNewRestrictionArray<Tcn>(int size) where Tcn : C, new()
        {
            Tcn[] array = new Tcn[] { null, default(Tcn), new Tcn(), (Tcn)new C() };
            Tcn[] result = new Tcn[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Ts[] GenerateGenericWithStructRestrictionArray<Ts>(int size) where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            Ts[] result = new Ts[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        #endregion

        #region Check array index

        private static void CheckBoolArrayIndex(bool[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckBoolArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckByteArrayIndex(byte[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckByteArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckCustomArrayIndex(C[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckCustomArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckCharArrayIndex(char[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckCharArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckCustom2ArrayIndex(D[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckCustom2ArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckDecimalArrayIndex(decimal[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckDecimalArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckDelegateArrayIndex(Delegate[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckDelegateArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckDoubleArrayIndex(double[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckDoubleArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckEnumArrayIndex(E[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckEnumArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckEnumLongArrayIndex(El[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckEnumLongArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckFloatArrayIndex(float[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckFloatArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckFuncArrayIndex(Func<object>[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckFuncArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckInterfaceArrayIndex(I[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckInterfaceArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckIEquatableCustomArrayIndex(IEquatable<C>[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckIEquatableCustomArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckIEquatableCustom2ArrayIndex(IEquatable<D>[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckIEquatableCustom2ArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckIntArrayIndex(int[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckIntArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckLongArrayIndex(long[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckLongArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckObjectArrayIndex(object[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckObjectArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckStructArrayIndex(S[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckSByteArrayIndex(sbyte[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckSByteArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckStructWithStringArrayIndex(Sc[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithStringArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckStructWithValueAndStringArrayIndex(Scs[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithValueAndStringArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckShortArrayIndex(short[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckShortArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckStructWithParametersArrayIndex(Sp[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithParametersArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckStructWithStructArrayIndex(Ss[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithStructArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckStringArrayIndex(string[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStringArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckUIntArrayIndex(uint[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckUIntArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckULongArrayIndex(ulong[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckULongArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckUShortArrayIndex(ushort[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckUShortArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckGenericArrayIndex<T>(T[] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericArrayIndexExpression<T>(array, i);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithClassRestrictionArrayIndex<Tc>(Tc[] array) where Tc : class
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithClassRestrictionArrayIndexExpression<Tc>(array, i);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithSubClassRestrictionArrayIndex<Tc>(Tc[] array) where Tc : C
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithSubClassRestrictionArrayIndexExpression<Tc>(array, i);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithNewClassRestrictionArrayIndex<Tcn>(Tcn[] array) where Tcn : class, new()
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithNewClassRestrictionArrayIndexExpression<Tcn>(array, i);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithSubClassNewRestrictionArrayIndex<Tcn>(Tcn[] array) where Tcn : C, new()
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithSubClassNewRestrictionArrayIndexExpression<Tcn>(array, i);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithStructRestrictionArrayIndex<Ts>(Ts[] array) where Ts : struct
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithStructRestrictionArrayIndexExpression<Ts>(array, i);
            }

            Assert.True(success);
        }

        #endregion

        #region Check index expression

        private static bool CheckBoolArrayIndexExpression(bool[] array, int index)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(bool[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckByteArrayIndexExpression(byte[] array, int index)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(byte[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckCustomArrayIndexExpression(C[] array, int index)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(C[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckCharArrayIndexExpression(char[] array, int index)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(char[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckCustom2ArrayIndexExpression(D[] array, int index)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(D[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckDecimalArrayIndexExpression(decimal[] array, int index)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(decimal[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckDelegateArrayIndexExpression(Delegate[] array, int index)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Delegate[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckDoubleArrayIndexExpression(double[] array, int index)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(double[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckEnumArrayIndexExpression(E[] array, int index)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(E[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckEnumLongArrayIndexExpression(El[] array, int index)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(El[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckFloatArrayIndexExpression(float[] array, int index)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(float[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckFuncArrayIndexExpression(Func<object>[] array, int index)
        {
            Expression<Func<Func<object>>> e =
                Expression.Lambda<Func<Func<object>>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Func<object>[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckInterfaceArrayIndexExpression(I[] array, int index)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(I[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckIEquatableCustomArrayIndexExpression(IEquatable<C>[] array, int index)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(IEquatable<C>[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckIEquatableCustom2ArrayIndexExpression(IEquatable<D>[] array, int index)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(IEquatable<D>[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckIntArrayIndexExpression(int[] array, int index)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(int[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckLongArrayIndexExpression(long[] array, int index)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(long[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckObjectArrayIndexExpression(object[] array, int index)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(object[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructArrayIndexExpression(S[] array, int index)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(S[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckSByteArrayIndexExpression(sbyte[] array, int index)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(sbyte[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithStringArrayIndexExpression(Sc[] array, int index)
        {
            Expression<Func<Sc>> e =
                Expression.Lambda<Func<Sc>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Sc[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithValueAndStringArrayIndexExpression(Scs[] array, int index)
        {
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Scs[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckShortArrayIndexExpression(short[] array, int index)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(short[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithParametersArrayIndexExpression(Sp[] array, int index)
        {
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Sp[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithStructArrayIndexExpression(Ss[] array, int index)
        {
            Expression<Func<Ss>> e =
                Expression.Lambda<Func<Ss>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Ss[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStringArrayIndexExpression(string[] array, int index)
        {
            Expression<Func<string>> e =
                Expression.Lambda<Func<string>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(string[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckUIntArrayIndexExpression(uint[] array, int index)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(uint[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckULongArrayIndexExpression(ulong[] array, int index)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(ulong[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckUShortArrayIndexExpression(ushort[] array, int index)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(ushort[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericArrayIndexExpression<T>(T[] array, int index)
        {
            Expression<Func<T>> e =
                Expression.Lambda<Func<T>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(T[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithClassRestrictionArrayIndexExpression<Tc>(Tc[] array, int index) where Tc : class
        {
            Expression<Func<Tc>> e =
                Expression.Lambda<Func<Tc>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Tc[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithSubClassRestrictionArrayIndexExpression<Tc>(Tc[] array, int index) where Tc : C
        {
            Expression<Func<Tc>> e =
                Expression.Lambda<Func<Tc>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Tc[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithNewClassRestrictionArrayIndexExpression<Tcn>(Tcn[] array, int index) where Tcn : class, new()
        {
            Expression<Func<Tcn>> e =
                Expression.Lambda<Func<Tcn>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Tcn[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithSubClassNewRestrictionArrayIndexExpression<Tcn>(Tcn[] array, int index) where Tcn : C, new()
        {
            Expression<Func<Tcn>> e =
                Expression.Lambda<Func<Tcn>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Tcn[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithStructRestrictionArrayIndexExpression<Ts>(Ts[] array, int index) where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Ts[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        #endregion

        #region Check exception array index

        private static void CheckExceptionBoolArrayIndex(bool[] array, int index)
        {
            bool success = true;
            try
            {
                CheckBoolArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionByteArrayIndex(byte[] array, int index)
        {
            bool success = true;
            try
            {
                CheckByteArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionCustomArrayIndex(C[] array, int index)
        {
            bool success = true;
            try
            {
                CheckCustomArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionCharArrayIndex(char[] array, int index)
        {
            bool success = true;
            try
            {
                CheckCharArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionCustom2ArrayIndex(D[] array, int index)
        {
            bool success = true;
            try
            {
                CheckCustom2ArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionDecimalArrayIndex(decimal[] array, int index)
        {
            bool success = true;
            try
            {
                CheckDecimalArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionDelegateArrayIndex(Delegate[] array, int index)
        {
            bool success = true;
            try
            {
                CheckDelegateArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionDoubleArrayIndex(double[] array, int index)
        {
            bool success = true;
            try
            {
                CheckDoubleArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionEnumArrayIndex(E[] array, int index)
        {
            bool success = true;
            try
            {
                CheckEnumArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionEnumLongArrayIndex(El[] array, int index)
        {
            bool success = true;
            try
            {
                CheckEnumLongArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionFloatArrayIndex(float[] array, int index)
        {
            bool success = true;
            try
            {
                CheckFloatArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionFuncArrayIndex(Func<object>[] array, int index)
        {
            bool success = true;
            try
            {
                CheckFuncArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionInterfaceArrayIndex(I[] array, int index)
        {
            bool success = true;
            try
            {
                CheckInterfaceArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionIEquatableCustomArrayIndex(IEquatable<C>[] array, int index)
        {
            bool success = true;
            try
            {
                CheckIEquatableCustomArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionIEquatableCustom2ArrayIndex(IEquatable<D>[] array, int index)
        {
            bool success = true;
            try
            {
                CheckIEquatableCustom2ArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionIntArrayIndex(int[] array, int index)
        {
            bool success = true;
            try
            {
                CheckIntArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionLongArrayIndex(long[] array, int index)
        {
            bool success = true;
            try
            {
                CheckLongArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionObjectArrayIndex(object[] array, int index)
        {
            bool success = true;
            try
            {
                CheckObjectArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructArrayIndex(S[] array, int index)
        {
            bool success = true;
            try
            {
                CheckStructArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionSByteArrayIndex(sbyte[] array, int index)
        {
            bool success = true;
            try
            {
                CheckSByteArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructWithStringArrayIndex(Sc[] array, int index)
        {
            bool success = true;
            try
            {
                CheckStructWithStringArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructWithValueAndStringArrayIndex(Scs[] array, int index)
        {
            bool success = true;
            try
            {
                CheckStructWithValueAndStringArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionShortArrayIndex(short[] array, int index)
        {
            bool success = true;
            try
            {
                CheckShortArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructWithParametersArrayIndex(Sp[] array, int index)
        {
            bool success = true;
            try
            {
                CheckStructWithParametersArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructWithStructArrayIndex(Ss[] array, int index)
        {
            bool success = true;
            try
            {
                CheckStructWithStructArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStringArrayIndex(string[] array, int index)
        {
            bool success = true;
            try
            {
                CheckStringArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionUIntArrayIndex(uint[] array, int index)
        {
            bool success = true;
            try
            {
                CheckUIntArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionULongArrayIndex(ulong[] array, int index)
        {
            bool success = true;
            try
            {
                CheckULongArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionUShortArrayIndex(ushort[] array, int index)
        {
            bool success = true;
            try
            {
                CheckUShortArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericArrayIndex<T>(T[] array, int index)
        {
            bool success = true;
            try
            {
                CheckGenericArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(Tc[] array, int index) where Tc : class
        {
            bool success = true;
            try
            {
                CheckGenericWithClassRestrictionArrayIndexExpression<Tc>(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(Tc[] array, int index) where Tc : C
        {
            bool success = true;
            try
            {
                CheckGenericWithSubClassRestrictionArrayIndexExpression<Tc>(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(Tcn[] array, int index) where Tcn : class, new()
        {
            bool success = true;
            try
            {
                CheckGenericWithNewClassRestrictionArrayIndexExpression<Tcn>(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(Tcn[] array, int index) where Tcn : C, new()
        {
            bool success = true;
            try
            {
                CheckGenericWithSubClassNewRestrictionArrayIndexExpression<Tcn>(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(Ts[] array, int index) where Ts : struct
        {
            bool success = true;
            try
            {
                CheckGenericWithStructRestrictionArrayIndexExpression<Ts>(array, index); // expect to fail
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
