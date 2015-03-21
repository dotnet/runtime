// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Array
{
    public static unsafe class ArrayArrayIndexTests
    {
        #region Bool tests

        [Fact]
        public static void CheckBoolArrayArrayIndexTest()
        {
            CheckBoolArrayArrayIndex(GenerateBoolArrayArray(0));
            CheckBoolArrayArrayIndex(GenerateBoolArrayArray(1));
            CheckBoolArrayArrayIndex(GenerateBoolArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionBoolArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionBoolArrayArrayIndex(null, -1);
            CheckExceptionBoolArrayArrayIndex(null, 0);
            CheckExceptionBoolArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionBoolArrayArrayIndex(GenerateBoolArrayArray(0), -1);
            CheckExceptionBoolArrayArrayIndex(GenerateBoolArrayArray(0), 0);
            CheckExceptionBoolArrayArrayIndex(GenerateBoolArrayArray(1), -1);
            CheckExceptionBoolArrayArrayIndex(GenerateBoolArrayArray(1), 1);
            CheckExceptionBoolArrayArrayIndex(GenerateBoolArrayArray(5), -1);
            CheckExceptionBoolArrayArrayIndex(GenerateBoolArrayArray(5), 5);
        }

        #endregion

        #region Byte tests

        [Fact]
        public static void CheckByteArrayArrayIndexTest()
        {
            CheckByteArrayArrayIndex(GenerateByteArrayArray(0));
            CheckByteArrayArrayIndex(GenerateByteArrayArray(1));
            CheckByteArrayArrayIndex(GenerateByteArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionByteArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionByteArrayArrayIndex(null, -1);
            CheckExceptionByteArrayArrayIndex(null, 0);
            CheckExceptionByteArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionByteArrayArrayIndex(GenerateByteArrayArray(0), -1);
            CheckExceptionByteArrayArrayIndex(GenerateByteArrayArray(0), 0);
            CheckExceptionByteArrayArrayIndex(GenerateByteArrayArray(1), -1);
            CheckExceptionByteArrayArrayIndex(GenerateByteArrayArray(1), 1);
            CheckExceptionByteArrayArrayIndex(GenerateByteArrayArray(5), -1);
            CheckExceptionByteArrayArrayIndex(GenerateByteArrayArray(5), 5);
        }

        #endregion

        #region Custom tests

        [Fact]
        public static void CheckCustomArrayArrayIndexTest()
        {
            CheckCustomArrayArrayIndex(GenerateCustomArrayArray(0));
            CheckCustomArrayArrayIndex(GenerateCustomArrayArray(1));
            CheckCustomArrayArrayIndex(GenerateCustomArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionCustomArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionCustomArrayArrayIndex(null, -1);
            CheckExceptionCustomArrayArrayIndex(null, 0);
            CheckExceptionCustomArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionCustomArrayArrayIndex(GenerateCustomArrayArray(0), -1);
            CheckExceptionCustomArrayArrayIndex(GenerateCustomArrayArray(0), 0);
            CheckExceptionCustomArrayArrayIndex(GenerateCustomArrayArray(1), -1);
            CheckExceptionCustomArrayArrayIndex(GenerateCustomArrayArray(1), 1);
            CheckExceptionCustomArrayArrayIndex(GenerateCustomArrayArray(5), -1);
            CheckExceptionCustomArrayArrayIndex(GenerateCustomArrayArray(5), 5);
        }

        #endregion

        #region Char tests

        [Fact]
        public static void CheckCharArrayArrayIndexTest()
        {
            CheckCharArrayArrayIndex(GenerateCharArrayArray(0));
            CheckCharArrayArrayIndex(GenerateCharArrayArray(1));
            CheckCharArrayArrayIndex(GenerateCharArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionCharArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionCharArrayArrayIndex(null, -1);
            CheckExceptionCharArrayArrayIndex(null, 0);
            CheckExceptionCharArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionCharArrayArrayIndex(GenerateCharArrayArray(0), -1);
            CheckExceptionCharArrayArrayIndex(GenerateCharArrayArray(0), 0);
            CheckExceptionCharArrayArrayIndex(GenerateCharArrayArray(1), -1);
            CheckExceptionCharArrayArrayIndex(GenerateCharArrayArray(1), 1);
            CheckExceptionCharArrayArrayIndex(GenerateCharArrayArray(5), -1);
            CheckExceptionCharArrayArrayIndex(GenerateCharArrayArray(5), 5);
        }

        #endregion

        #region Custom2 tests

        [Fact]
        public static void CheckCustom2ArrayArrayIndexTest()
        {
            CheckCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(0));
            CheckCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(1));
            CheckCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionCustom2ArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionCustom2ArrayArrayIndex(null, -1);
            CheckExceptionCustom2ArrayArrayIndex(null, 0);
            CheckExceptionCustom2ArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(0), -1);
            CheckExceptionCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(0), 0);
            CheckExceptionCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(1), -1);
            CheckExceptionCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(1), 1);
            CheckExceptionCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(5), -1);
            CheckExceptionCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(5), 5);
        }

        #endregion

        #region Decimal tests

        [Fact]
        public static void CheckDecimalArrayArrayIndexTest()
        {
            CheckDecimalArrayArrayIndex(GenerateDecimalArrayArray(0));
            CheckDecimalArrayArrayIndex(GenerateDecimalArrayArray(1));
            CheckDecimalArrayArrayIndex(GenerateDecimalArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionDecimalArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionDecimalArrayArrayIndex(null, -1);
            CheckExceptionDecimalArrayArrayIndex(null, 0);
            CheckExceptionDecimalArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionDecimalArrayArrayIndex(GenerateDecimalArrayArray(0), -1);
            CheckExceptionDecimalArrayArrayIndex(GenerateDecimalArrayArray(0), 0);
            CheckExceptionDecimalArrayArrayIndex(GenerateDecimalArrayArray(1), -1);
            CheckExceptionDecimalArrayArrayIndex(GenerateDecimalArrayArray(1), 1);
            CheckExceptionDecimalArrayArrayIndex(GenerateDecimalArrayArray(5), -1);
            CheckExceptionDecimalArrayArrayIndex(GenerateDecimalArrayArray(5), 5);
        }

        #endregion

        #region Delegate tests

        [Fact]
        public static void CheckDelegateArrayArrayIndexTest()
        {
            CheckDelegateArrayArrayIndex(GenerateDelegateArrayArray(0));
            CheckDelegateArrayArrayIndex(GenerateDelegateArrayArray(1));
            CheckDelegateArrayArrayIndex(GenerateDelegateArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionDelegateArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionDelegateArrayArrayIndex(null, -1);
            CheckExceptionDelegateArrayArrayIndex(null, 0);
            CheckExceptionDelegateArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionDelegateArrayArrayIndex(GenerateDelegateArrayArray(0), -1);
            CheckExceptionDelegateArrayArrayIndex(GenerateDelegateArrayArray(0), 0);
            CheckExceptionDelegateArrayArrayIndex(GenerateDelegateArrayArray(1), -1);
            CheckExceptionDelegateArrayArrayIndex(GenerateDelegateArrayArray(1), 1);
            CheckExceptionDelegateArrayArrayIndex(GenerateDelegateArrayArray(5), -1);
            CheckExceptionDelegateArrayArrayIndex(GenerateDelegateArrayArray(5), 5);
        }

        #endregion

        #region double tests

        [Fact]
        public static void CheckdoubleArrayArrayIndexTest()
        {
            CheckdoubleArrayArrayIndex(GeneratedoubleArrayArray(0));
            CheckdoubleArrayArrayIndex(GeneratedoubleArrayArray(1));
            CheckdoubleArrayArrayIndex(GeneratedoubleArrayArray(5));
        }

        [Fact]
        public static void CheckExceptiondoubleArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptiondoubleArrayArrayIndex(null, -1);
            CheckExceptiondoubleArrayArrayIndex(null, 0);
            CheckExceptiondoubleArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptiondoubleArrayArrayIndex(GeneratedoubleArrayArray(0), -1);
            CheckExceptiondoubleArrayArrayIndex(GeneratedoubleArrayArray(0), 0);
            CheckExceptiondoubleArrayArrayIndex(GeneratedoubleArrayArray(1), -1);
            CheckExceptiondoubleArrayArrayIndex(GeneratedoubleArrayArray(1), 1);
            CheckExceptiondoubleArrayArrayIndex(GeneratedoubleArrayArray(5), -1);
            CheckExceptiondoubleArrayArrayIndex(GeneratedoubleArrayArray(5), 5);
        }

        #endregion

        #region Enum tests

        [Fact]
        public static void CheckEnumArrayArrayIndexTest()
        {
            CheckEnumArrayArrayIndex(GenerateEnumArrayArray(0));
            CheckEnumArrayArrayIndex(GenerateEnumArrayArray(1));
            CheckEnumArrayArrayIndex(GenerateEnumArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionEnumArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionEnumArrayArrayIndex(null, -1);
            CheckExceptionEnumArrayArrayIndex(null, 0);
            CheckExceptionEnumArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionEnumArrayArrayIndex(GenerateEnumArrayArray(0), -1);
            CheckExceptionEnumArrayArrayIndex(GenerateEnumArrayArray(0), 0);
            CheckExceptionEnumArrayArrayIndex(GenerateEnumArrayArray(1), -1);
            CheckExceptionEnumArrayArrayIndex(GenerateEnumArrayArray(1), 1);
            CheckExceptionEnumArrayArrayIndex(GenerateEnumArrayArray(5), -1);
            CheckExceptionEnumArrayArrayIndex(GenerateEnumArrayArray(5), 5);
        }

        #endregion

        #region EnumLong tests

        [Fact]
        public static void CheckEnumLongArrayArrayIndexTest()
        {
            CheckEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(0));
            CheckEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(1));
            CheckEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionEnumLongArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionEnumLongArrayArrayIndex(null, -1);
            CheckExceptionEnumLongArrayArrayIndex(null, 0);
            CheckExceptionEnumLongArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(0), -1);
            CheckExceptionEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(0), 0);
            CheckExceptionEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(1), -1);
            CheckExceptionEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(1), 1);
            CheckExceptionEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(5), -1);
            CheckExceptionEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(5), 5);
        }

        #endregion

        #region Float tests

        [Fact]
        public static void CheckFloatArrayArrayIndexTest()
        {
            CheckFloatArrayArrayIndex(GenerateFloatArrayArray(0));
            CheckFloatArrayArrayIndex(GenerateFloatArrayArray(1));
            CheckFloatArrayArrayIndex(GenerateFloatArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionFloatArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionFloatArrayArrayIndex(null, -1);
            CheckExceptionFloatArrayArrayIndex(null, 0);
            CheckExceptionFloatArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionFloatArrayArrayIndex(GenerateFloatArrayArray(0), -1);
            CheckExceptionFloatArrayArrayIndex(GenerateFloatArrayArray(0), 0);
            CheckExceptionFloatArrayArrayIndex(GenerateFloatArrayArray(1), -1);
            CheckExceptionFloatArrayArrayIndex(GenerateFloatArrayArray(1), 1);
            CheckExceptionFloatArrayArrayIndex(GenerateFloatArrayArray(5), -1);
            CheckExceptionFloatArrayArrayIndex(GenerateFloatArrayArray(5), 5);
        }

        #endregion

        #region Func tests

        [Fact]
        public static void CheckFuncArrayArrayIndexTest()
        {
            CheckFuncArrayArrayIndex(GenerateFuncArrayArray(0));
            CheckFuncArrayArrayIndex(GenerateFuncArrayArray(1));
            CheckFuncArrayArrayIndex(GenerateFuncArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionFuncArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionFuncArrayArrayIndex(null, -1);
            CheckExceptionFuncArrayArrayIndex(null, 0);
            CheckExceptionFuncArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionFuncArrayArrayIndex(GenerateFuncArrayArray(0), -1);
            CheckExceptionFuncArrayArrayIndex(GenerateFuncArrayArray(0), 0);
            CheckExceptionFuncArrayArrayIndex(GenerateFuncArrayArray(1), -1);
            CheckExceptionFuncArrayArrayIndex(GenerateFuncArrayArray(1), 1);
            CheckExceptionFuncArrayArrayIndex(GenerateFuncArrayArray(5), -1);
            CheckExceptionFuncArrayArrayIndex(GenerateFuncArrayArray(5), 5);
        }

        #endregion

        #region Interface tests

        [Fact]
        public static void CheckInterfaceArrayArrayIndexTest()
        {
            CheckInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(0));
            CheckInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(1));
            CheckInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionInterfaceArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionInterfaceArrayArrayIndex(null, -1);
            CheckExceptionInterfaceArrayArrayIndex(null, 0);
            CheckExceptionInterfaceArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(0), -1);
            CheckExceptionInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(0), 0);
            CheckExceptionInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(1), -1);
            CheckExceptionInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(1), 1);
            CheckExceptionInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(5), -1);
            CheckExceptionInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(5), 5);
        }

        #endregion

        #region IEquatable tests

        [Fact]
        public static void CheckIEquatableArrayArrayIndexTest()
        {
            CheckIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(0));
            CheckIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(1));
            CheckIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionIEquatableArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionIEquatableArrayArrayIndex(null, -1);
            CheckExceptionIEquatableArrayArrayIndex(null, 0);
            CheckExceptionIEquatableArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(0), -1);
            CheckExceptionIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(0), 0);
            CheckExceptionIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(1), -1);
            CheckExceptionIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(1), 1);
            CheckExceptionIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(5), -1);
            CheckExceptionIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(5), 5);
        }

        #endregion

        #region IEquatable2 tests

        [Fact]
        public static void CheckIEquatable2ArrayArrayIndexTest()
        {
            CheckIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(0));
            CheckIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(1));
            CheckIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionIEquatable2ArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionIEquatable2ArrayArrayIndex(null, -1);
            CheckExceptionIEquatable2ArrayArrayIndex(null, 0);
            CheckExceptionIEquatable2ArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(0), -1);
            CheckExceptionIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(0), 0);
            CheckExceptionIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(1), -1);
            CheckExceptionIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(1), 1);
            CheckExceptionIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(5), -1);
            CheckExceptionIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(5), 5);
        }

        #endregion

        #region Int tests

        [Fact]
        public static void CheckIntArrayArrayIndexTest()
        {
            CheckIntArrayArrayIndex(GenerateIntArrayArray(0));
            CheckIntArrayArrayIndex(GenerateIntArrayArray(1));
            CheckIntArrayArrayIndex(GenerateIntArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionIntArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionIntArrayArrayIndex(null, -1);
            CheckExceptionIntArrayArrayIndex(null, 0);
            CheckExceptionIntArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionIntArrayArrayIndex(GenerateIntArrayArray(0), -1);
            CheckExceptionIntArrayArrayIndex(GenerateIntArrayArray(0), 0);
            CheckExceptionIntArrayArrayIndex(GenerateIntArrayArray(1), -1);
            CheckExceptionIntArrayArrayIndex(GenerateIntArrayArray(1), 1);
            CheckExceptionIntArrayArrayIndex(GenerateIntArrayArray(5), -1);
            CheckExceptionIntArrayArrayIndex(GenerateIntArrayArray(5), 5);
        }

        #endregion

        #region Long tests

        [Fact]
        public static void CheckLongArrayArrayIndexTest()
        {
            CheckLongArrayArrayIndex(GenerateLongArrayArray(0));
            CheckLongArrayArrayIndex(GenerateLongArrayArray(1));
            CheckLongArrayArrayIndex(GenerateLongArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionLongArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionLongArrayArrayIndex(null, -1);
            CheckExceptionLongArrayArrayIndex(null, 0);
            CheckExceptionLongArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionLongArrayArrayIndex(GenerateLongArrayArray(0), -1);
            CheckExceptionLongArrayArrayIndex(GenerateLongArrayArray(0), 0);
            CheckExceptionLongArrayArrayIndex(GenerateLongArrayArray(1), -1);
            CheckExceptionLongArrayArrayIndex(GenerateLongArrayArray(1), 1);
            CheckExceptionLongArrayArrayIndex(GenerateLongArrayArray(5), -1);
            CheckExceptionLongArrayArrayIndex(GenerateLongArrayArray(5), 5);
        }

        #endregion

        #region Object tests

        [Fact]
        public static void CheckObjectArrayArrayIndexTest()
        {
            CheckObjectArrayArrayIndex(GenerateObjectArrayArray(0));
            CheckObjectArrayArrayIndex(GenerateObjectArrayArray(1));
            CheckObjectArrayArrayIndex(GenerateObjectArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionObjectArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionObjectArrayArrayIndex(null, -1);
            CheckExceptionObjectArrayArrayIndex(null, 0);
            CheckExceptionObjectArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionObjectArrayArrayIndex(GenerateObjectArrayArray(0), -1);
            CheckExceptionObjectArrayArrayIndex(GenerateObjectArrayArray(0), 0);
            CheckExceptionObjectArrayArrayIndex(GenerateObjectArrayArray(1), -1);
            CheckExceptionObjectArrayArrayIndex(GenerateObjectArrayArray(1), 1);
            CheckExceptionObjectArrayArrayIndex(GenerateObjectArrayArray(5), -1);
            CheckExceptionObjectArrayArrayIndex(GenerateObjectArrayArray(5), 5);
        }

        #endregion

        #region Struct tests

        [Fact]
        public static void CheckStructArrayArrayIndexTest()
        {
            CheckStructArrayArrayIndex(GenerateStructArrayArray(0));
            CheckStructArrayArrayIndex(GenerateStructArrayArray(1));
            CheckStructArrayArrayIndex(GenerateStructArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionStructArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionStructArrayArrayIndex(null, -1);
            CheckExceptionStructArrayArrayIndex(null, 0);
            CheckExceptionStructArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionStructArrayArrayIndex(GenerateStructArrayArray(0), -1);
            CheckExceptionStructArrayArrayIndex(GenerateStructArrayArray(0), 0);
            CheckExceptionStructArrayArrayIndex(GenerateStructArrayArray(1), -1);
            CheckExceptionStructArrayArrayIndex(GenerateStructArrayArray(1), 1);
            CheckExceptionStructArrayArrayIndex(GenerateStructArrayArray(5), -1);
            CheckExceptionStructArrayArrayIndex(GenerateStructArrayArray(5), 5);
        }

        #endregion

        #region SByte tests

        [Fact]
        public static void CheckSByteArrayArrayIndexTest()
        {
            CheckSByteArrayArrayIndex(GenerateSByteArrayArray(0));
            CheckSByteArrayArrayIndex(GenerateSByteArrayArray(1));
            CheckSByteArrayArrayIndex(GenerateSByteArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionSByteArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionSByteArrayArrayIndex(null, -1);
            CheckExceptionSByteArrayArrayIndex(null, 0);
            CheckExceptionSByteArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionSByteArrayArrayIndex(GenerateSByteArrayArray(0), -1);
            CheckExceptionSByteArrayArrayIndex(GenerateSByteArrayArray(0), 0);
            CheckExceptionSByteArrayArrayIndex(GenerateSByteArrayArray(1), -1);
            CheckExceptionSByteArrayArrayIndex(GenerateSByteArrayArray(1), 1);
            CheckExceptionSByteArrayArrayIndex(GenerateSByteArrayArray(5), -1);
            CheckExceptionSByteArrayArrayIndex(GenerateSByteArrayArray(5), 5);
        }

        #endregion

        #region StructWithString tests

        [Fact]
        public static void CheckStructWithStringArrayArrayIndexTest()
        {
            CheckStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(0));
            CheckStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(1));
            CheckStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionStructWithStringArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionStructWithStringArrayArrayIndex(null, -1);
            CheckExceptionStructWithStringArrayArrayIndex(null, 0);
            CheckExceptionStructWithStringArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(0), -1);
            CheckExceptionStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(0), 0);
            CheckExceptionStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(1), -1);
            CheckExceptionStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(1), 1);
            CheckExceptionStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(5), -1);
            CheckExceptionStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(5), 5);
        }

        #endregion

        #region StructWithStringAndStruct tests

        [Fact]
        public static void CheckStructWithStringAndStructArrayArrayIndexTest()
        {
            CheckStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(0));
            CheckStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(1));
            CheckStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionStructWithStringAndStructArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionStructWithStringAndStructArrayArrayIndex(null, -1);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(null, 0);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(0), -1);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(0), 0);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(1), -1);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(1), 1);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(5), -1);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(5), 5);
        }

        #endregion

        #region Short tests

        [Fact]
        public static void CheckShortArrayArrayIndexTest()
        {
            CheckShortArrayArrayIndex(GenerateShortArrayArray(0));
            CheckShortArrayArrayIndex(GenerateShortArrayArray(1));
            CheckShortArrayArrayIndex(GenerateShortArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionShortArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionShortArrayArrayIndex(null, -1);
            CheckExceptionShortArrayArrayIndex(null, 0);
            CheckExceptionShortArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionShortArrayArrayIndex(GenerateShortArrayArray(0), -1);
            CheckExceptionShortArrayArrayIndex(GenerateShortArrayArray(0), 0);
            CheckExceptionShortArrayArrayIndex(GenerateShortArrayArray(1), -1);
            CheckExceptionShortArrayArrayIndex(GenerateShortArrayArray(1), 1);
            CheckExceptionShortArrayArrayIndex(GenerateShortArrayArray(5), -1);
            CheckExceptionShortArrayArrayIndex(GenerateShortArrayArray(5), 5);
        }

        #endregion

        #region StructWithTwoFields tests

        [Fact]
        public static void CheckStructWithTwoFieldsArrayArrayIndexTest()
        {
            CheckStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(0));
            CheckStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(1));
            CheckStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionStructWithTwoFieldsArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(null, -1);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(null, 0);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(0), -1);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(0), 0);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(1), -1);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(1), 1);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(5), -1);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(5), 5);
        }

        #endregion

        #region StructWithValue tests

        [Fact]
        public static void CheckStructWithValueArrayArrayIndexTest()
        {
            CheckStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(0));
            CheckStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(1));
            CheckStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionStructWithValueArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionStructWithValueArrayArrayIndex(null, -1);
            CheckExceptionStructWithValueArrayArrayIndex(null, 0);
            CheckExceptionStructWithValueArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(0), -1);
            CheckExceptionStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(0), 0);
            CheckExceptionStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(1), -1);
            CheckExceptionStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(1), 1);
            CheckExceptionStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(5), -1);
            CheckExceptionStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(5), 5);
        }

        #endregion

        #region String tests

        [Fact]
        public static void CheckStringArrayArrayIndexTest()
        {
            CheckStringArrayArrayIndex(GenerateStringArrayArray(0));
            CheckStringArrayArrayIndex(GenerateStringArrayArray(1));
            CheckStringArrayArrayIndex(GenerateStringArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionStringArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionStringArrayArrayIndex(null, -1);
            CheckExceptionStringArrayArrayIndex(null, 0);
            CheckExceptionStringArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionStringArrayArrayIndex(GenerateStringArrayArray(0), -1);
            CheckExceptionStringArrayArrayIndex(GenerateStringArrayArray(0), 0);
            CheckExceptionStringArrayArrayIndex(GenerateStringArrayArray(1), -1);
            CheckExceptionStringArrayArrayIndex(GenerateStringArrayArray(1), 1);
            CheckExceptionStringArrayArrayIndex(GenerateStringArrayArray(5), -1);
            CheckExceptionStringArrayArrayIndex(GenerateStringArrayArray(5), 5);
        }

        #endregion

        #region UInt tests

        [Fact]
        public static void CheckUIntArrayArrayIndexTest()
        {
            CheckUIntArrayArrayIndex(GenerateUIntArrayArray(0));
            CheckUIntArrayArrayIndex(GenerateUIntArrayArray(1));
            CheckUIntArrayArrayIndex(GenerateUIntArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionUIntArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionUIntArrayArrayIndex(null, -1);
            CheckExceptionUIntArrayArrayIndex(null, 0);
            CheckExceptionUIntArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionUIntArrayArrayIndex(GenerateUIntArrayArray(0), -1);
            CheckExceptionUIntArrayArrayIndex(GenerateUIntArrayArray(0), 0);
            CheckExceptionUIntArrayArrayIndex(GenerateUIntArrayArray(1), -1);
            CheckExceptionUIntArrayArrayIndex(GenerateUIntArrayArray(1), 1);
            CheckExceptionUIntArrayArrayIndex(GenerateUIntArrayArray(5), -1);
            CheckExceptionUIntArrayArrayIndex(GenerateUIntArrayArray(5), 5);
        }

        #endregion

        #region ULong tests

        [Fact]
        public static void CheckULongArrayArrayIndexTest()
        {
            CheckULongArrayArrayIndex(GenerateULongArrayArray(0));
            CheckULongArrayArrayIndex(GenerateULongArrayArray(1));
            CheckULongArrayArrayIndex(GenerateULongArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionULongArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionULongArrayArrayIndex(null, -1);
            CheckExceptionULongArrayArrayIndex(null, 0);
            CheckExceptionULongArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionULongArrayArrayIndex(GenerateULongArrayArray(0), -1);
            CheckExceptionULongArrayArrayIndex(GenerateULongArrayArray(0), 0);
            CheckExceptionULongArrayArrayIndex(GenerateULongArrayArray(1), -1);
            CheckExceptionULongArrayArrayIndex(GenerateULongArrayArray(1), 1);
            CheckExceptionULongArrayArrayIndex(GenerateULongArrayArray(5), -1);
            CheckExceptionULongArrayArrayIndex(GenerateULongArrayArray(5), 5);
        }

        #endregion

        #region UShort tests

        [Fact]
        public static void CheckUShortArrayArrayIndexTest()
        {
            CheckUShortArrayArrayIndex(GenerateUShortArrayArray(0));
            CheckUShortArrayArrayIndex(GenerateUShortArrayArray(1));
            CheckUShortArrayArrayIndex(GenerateUShortArrayArray(5));
        }

        [Fact]
        public static void CheckExceptionUShortArrayArrayIndexTest()
        {
            // null arrays
            CheckExceptionUShortArrayArrayIndex(null, -1);
            CheckExceptionUShortArrayArrayIndex(null, 0);
            CheckExceptionUShortArrayArrayIndex(null, 1);

            // index out of bounds
            CheckExceptionUShortArrayArrayIndex(GenerateUShortArrayArray(0), -1);
            CheckExceptionUShortArrayArrayIndex(GenerateUShortArrayArray(0), 0);
            CheckExceptionUShortArrayArrayIndex(GenerateUShortArrayArray(1), -1);
            CheckExceptionUShortArrayArrayIndex(GenerateUShortArrayArray(1), 1);
            CheckExceptionUShortArrayArrayIndex(GenerateUShortArrayArray(5), -1);
            CheckExceptionUShortArrayArrayIndex(GenerateUShortArrayArray(5), 5);
        }

        #endregion

        #region Generic tests

        [Fact]
        public static void CheckGenericCustomArrayArrayIndexTest()
        {
            CheckGenericArrayArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericCustomArrayArrayIndexTest()
        {
            CheckExceptionGenericArrayArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericEnumArrayArrayIndexTest()
        {
            CheckGenericArrayArrayIndexTestHelper<E>();
        }

        [Fact]
        public static void CheckExceptionGenericEnumArrayArrayIndexTest()
        {
            CheckExceptionGenericArrayArrayIndexTestHelper<E>();
        }

        [Fact]
        public static void CheckGenericObjectArrayArrayIndexTest()
        {
            CheckGenericArrayArrayIndexTestHelper<object>();
        }

        [Fact]
        public static void CheckExceptionGenericObjectArrayArrayIndexTest()
        {
            CheckExceptionGenericArrayArrayIndexTestHelper<object>();
        }

        [Fact]
        public static void CheckGenericStructArrayArrayIndexTest()
        {
            CheckGenericArrayArrayIndexTestHelper<S>();
        }

        [Fact]
        public static void CheckExceptionGenericStructArrayArrayIndexTest()
        {
            CheckExceptionGenericArrayArrayIndexTestHelper<S>();
        }

        [Fact]
        public static void CheckGenericStructWithStringAndFieldArrayArrayIndexTest()
        {
            CheckGenericArrayArrayIndexTestHelper<Scs>();
        }

        [Fact]
        public static void CheckExceptionGenericStructWithStringAndFieldArrayArrayIndexTest()
        {
            CheckExceptionGenericArrayArrayIndexTestHelper<Scs>();
        }

        [Fact]
        public static void CheckGenericCustomWithClassRestrictionArrayArrayIndexTest()
        {
            CheckGenericWithClassRestrictionArrayArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericCustomWithClassRestrictionArrayArrayIndexTest()
        {
            CheckExceptionGenericWithClassRestrictionArrayArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericObjectWithClassRestrictionArrayArrayIndexTest()
        {
            CheckGenericWithClassRestrictionArrayArrayIndexTestHelper<object>();
        }

        [Fact]
        public static void CheckExceptionGenericObjectWithClassRestrictionArrayArrayIndexTest()
        {
            CheckExceptionGenericWithClassRestrictionArrayArrayIndexTestHelper<object>();
        }

        [Fact]
        public static void CheckGenericCustomWithClassAndNewRestrictionArrayArrayIndexTest()
        {
            CheckGenericWithClassAndNewRestrictionArrayArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericCustomWithClassAndNewRestrictionArrayArrayIndexTest()
        {
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericObjectWithClassAndNewRestrictionArrayArrayIndexTest()
        {
            CheckGenericWithClassAndNewRestrictionArrayArrayIndexTestHelper<object>();
        }

        [Fact]
        public static void CheckExceptionGenericObjectWithClassAndNewRestrictionArrayArrayIndexTest()
        {
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndexTestHelper<object>();
        }

        [Fact]
        public static void CheckGenericCustomWithSubClassRestrictionArrayArrayIndexTest()
        {
            CheckGenericWithSubClassRestrictionArrayArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericCustomWithSubClassRestrictionArrayArrayIndexTest()
        {
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericCustomWithSubClassAndNewRestrictionArrayArrayIndexTest()
        {
            CheckGenericWithSubClassAndNewRestrictionArrayArrayIndexTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericCustomWithSubClassAndNewRestrictionArrayArrayIndexTest()
        {
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndexTestHelper<C>();
        }

        #endregion

        #region Generic helpers

        public static void CheckGenericArrayArrayIndexTestHelper<T>()
        {
            CheckGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(0));
            CheckGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(1));
            CheckGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(5));
        }

        public static void CheckExceptionGenericArrayArrayIndexTestHelper<T>()
        {
            // null arrays
            CheckExceptionGenericArrayArrayIndex<T>(null, -1);
            CheckExceptionGenericArrayArrayIndex<T>(null, 0);
            CheckExceptionGenericArrayArrayIndex<T>(null, 1);

            // index out of bounds
            CheckExceptionGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(0), -1);
            CheckExceptionGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(0), 0);
            CheckExceptionGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(1), -1);
            CheckExceptionGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(1), 1);
            CheckExceptionGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(5), -1);
            CheckExceptionGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(5), 5);
        }

        public static void CheckGenericWithClassRestrictionArrayArrayIndexTestHelper<Tc>() where Tc : class
        {
            CheckGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(0));
            CheckGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(1));
            CheckGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(5));
        }

        public static void CheckExceptionGenericWithClassRestrictionArrayArrayIndexTestHelper<Tc>() where Tc : class
        {
            // null arrays
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(null, -1);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(null, 0);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(null, 1);

            // index out of bounds
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(0), -1);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(0), 0);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(1), -1);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(1), 1);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(5), -1);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(5), 5);
        }

        public static void CheckGenericWithClassAndNewRestrictionArrayArrayIndexTestHelper<Tcn>() where Tcn : class, new()
        {
            CheckGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(0));
            CheckGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(1));
            CheckGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(5));
        }

        public static void CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndexTestHelper<Tcn>() where Tcn : class, new()
        {
            // null arrays
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(null, -1);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(null, 0);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(null, 1);

            // index out of bounds
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(0), -1);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(0), 0);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(1), -1);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(1), 1);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(5), -1);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(5), 5);
        }

        public static void CheckGenericWithSubClassRestrictionArrayArrayIndexTestHelper<TC>() where TC : C
        {
            CheckGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(0));
            CheckGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(1));
            CheckGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(5));
        }

        public static void CheckExceptionGenericWithSubClassRestrictionArrayArrayIndexTestHelper<TC>() where TC : C
        {
            // null arrays
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(null, -1);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(null, 0);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(null, 1);

            // index out of bounds
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(0), -1);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(0), 0);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(1), -1);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(1), 1);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(5), -1);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(5), 5);
        }

        public static void CheckGenericWithSubClassAndNewRestrictionArrayArrayIndexTestHelper<TCn>() where TCn : C, new()
        {
            CheckGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(0));
            CheckGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(1));
            CheckGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(5));
        }

        public static void CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndexTestHelper<TCn>() where TCn : C, new()
        {
            // null arrays
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(null, -1);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(null, 0);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(null, 1);

            // index out of bounds
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(0), -1);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(0), 0);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(1), -1);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(1), 1);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(5), -1);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(5), 5);
        }

        #endregion

        #region Generate array

        private static bool[][] GenerateBoolArrayArray(int size)
        {
            bool[][] array = new bool[][] { null, new bool[0], new bool[] { true, false }, new bool[100] };
            bool[][] result = new bool[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static byte[][] GenerateByteArrayArray(int size)
        {
            byte[][] array = new byte[][] { null, new byte[0], new byte[] { 0, 1, byte.MaxValue }, new byte[100] };
            byte[][] result = new byte[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static C[][] GenerateCustomArrayArray(int size)
        {
            C[][] array = new C[][] { null, new C[0], new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[100] };
            C[][] result = new C[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static char[][] GenerateCharArrayArray(int size)
        {
            char[][] array = new char[][] { null, new char[0], new char[] { '\0', '\b', 'A', '\uffff' }, new char[100] };
            char[][] result = new char[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static D[][] GenerateCustom2ArrayArray(int size)
        {
            D[][] array = new D[][] { null, new D[0], new D[] { null, new D(), new D(0), new D(5) }, new D[100] };
            D[][] result = new D[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static decimal[][] GenerateDecimalArrayArray(int size)
        {
            decimal[][] array = new decimal[][] { null, new decimal[0], new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue }, new decimal[100] };
            decimal[][] result = new decimal[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Delegate[][] GenerateDelegateArrayArray(int size)
        {
            Delegate[][] array = new Delegate[][] { null, new Delegate[0], new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } }, new Delegate[100] };
            Delegate[][] result = new Delegate[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static double[][] GeneratedoubleArrayArray(int size)
        {
            double[][] array = new double[][] { null, new double[0], new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN }, new double[100] };
            double[][] result = new double[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static E[][] GenerateEnumArrayArray(int size)
        {
            E[][] array = new E[][] { null, new E[0], new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue }, new E[100] };
            E[][] result = new E[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static El[][] GenerateEnumLongArrayArray(int size)
        {
            El[][] array = new El[][] { null, new El[0], new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue }, new El[100] };
            El[][] result = new El[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static float[][] GenerateFloatArrayArray(int size)
        {
            float[][] array = new float[][] { null, new float[0], new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN }, new float[100] };
            float[][] result = new float[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Func<object>[][] GenerateFuncArrayArray(int size)
        {
            Func<object>[][] array = new Func<object>[][] { null, new Func<object>[0], new Func<object>[] { null, (Func<object>)delegate () { return null; } }, new Func<object>[100] };
            Func<object>[][] result = new Func<object>[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static I[][] GenerateInterfaceArrayArray(int size)
        {
            I[][] array = new I[][] { null, new I[0], new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[100] };
            I[][] result = new I[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static IEquatable<C>[][] GenerateIEquatableArrayArray(int size)
        {
            IEquatable<C>[][] array = new IEquatable<C>[][] { null, new IEquatable<C>[0], new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) }, new IEquatable<C>[100] };
            IEquatable<C>[][] result = new IEquatable<C>[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static IEquatable<D>[][] GenerateIEquatable2ArrayArray(int size)
        {
            IEquatable<D>[][] array = new IEquatable<D>[][] { null, new IEquatable<D>[0], new IEquatable<D>[] { null, new D(), new D(0), new D(5) }, new IEquatable<D>[100] };
            IEquatable<D>[][] result = new IEquatable<D>[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static int[][] GenerateIntArrayArray(int size)
        {
            int[][] array = new int[][] { null, new int[0], new int[] { 0, 1, -1, int.MinValue, int.MaxValue }, new int[100] };
            int[][] result = new int[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static long[][] GenerateLongArrayArray(int size)
        {
            long[][] array = new long[][] { null, new long[0], new long[] { 0, 1, -1, long.MinValue, long.MaxValue }, new long[100] };
            long[][] result = new long[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static object[][] GenerateObjectArrayArray(int size)
        {
            object[][] array = new object[][] { null, new object[0], new object[] { null, new object(), new C(), new D(3) }, new object[100] };
            object[][] result = new object[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static S[][] GenerateStructArrayArray(int size)
        {
            S[][] array = new S[][] { null, new S[0], new S[] { default(S), new S() }, new S[100] };
            S[][] result = new S[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static sbyte[][] GenerateSByteArrayArray(int size)
        {
            sbyte[][] array = new sbyte[][] { null, new sbyte[0], new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }, new sbyte[100] };
            sbyte[][] result = new sbyte[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Sc[][] GenerateStructWithStringArrayArray(int size)
        {
            Sc[][] array = new Sc[][] { null, new Sc[0], new Sc[] { default(Sc), new Sc(), new Sc(null) }, new Sc[100] };
            Sc[][] result = new Sc[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Scs[][] GenerateStructWithStringAndStructArrayArray(int size)
        {
            Scs[][] array = new Scs[][] { null, new Scs[0], new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) }, new Scs[100] };
            Scs[][] result = new Scs[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static short[][] GenerateShortArrayArray(int size)
        {
            short[][] array = new short[][] { null, new short[0], new short[] { 0, 1, -1, short.MinValue, short.MaxValue }, new short[100] };
            short[][] result = new short[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Sp[][] GenerateStructWithTwoFieldsArrayArray(int size)
        {
            Sp[][] array = new Sp[][] { null, new Sp[0], new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) }, new Sp[100] };
            Sp[][] result = new Sp[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Ss[][] GenerateStructWithValueArrayArray(int size)
        {
            Ss[][] array = new Ss[][] { null, new Ss[0], new Ss[] { default(Ss), new Ss(), new Ss(new S()) }, new Ss[100] };
            Ss[][] result = new Ss[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static string[][] GenerateStringArrayArray(int size)
        {
            string[][] array = new string[][] { null, new string[0], new string[] { null, "", "a", "foo" }, new string[100] };
            string[][] result = new string[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static uint[][] GenerateUIntArrayArray(int size)
        {
            uint[][] array = new uint[][] { null, new uint[0], new uint[] { 0, 1, uint.MaxValue }, new uint[100] };
            uint[][] result = new uint[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static ulong[][] GenerateULongArrayArray(int size)
        {
            ulong[][] array = new ulong[][] { null, new ulong[0], new ulong[] { 0, 1, ulong.MaxValue }, new ulong[100] };
            ulong[][] result = new ulong[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static ushort[][] GenerateUShortArrayArray(int size)
        {
            ushort[][] array = new ushort[][] { null, new ushort[0], new ushort[] { 0, 1, ushort.MaxValue }, new ushort[100] };
            ushort[][] result = new ushort[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static T[][] GenerateGenericWithCustomArrayArray<T>(int size)
        {
            T[][] array = new T[][] { null, new T[0], new T[] { default(T) }, new T[100] };
            T[][] result = new T[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static T[][] GenerateGenericArrayArray<T>(int size)
        {
            T[][] array = new T[][] { null, new T[0], new T[] { default(T) }, new T[100] };
            T[][] result = new T[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Tc[][] GenerateGenericWithClassRestrictionArrayArray<Tc>(int size) where Tc : class
        {
            Tc[][] array = new Tc[][] { null, new Tc[0], new Tc[] { null, default(Tc) }, new Tc[100] };
            Tc[][] result = new Tc[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Tcn[][] GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(int size) where Tcn : class, new()
        {
            Tcn[][] array = new Tcn[][] { null, new Tcn[0], new Tcn[] { null, default(Tcn), new Tcn() }, new Tcn[100] };
            Tcn[][] result = new Tcn[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static TC[][] GenerateGenericWithSubClassRestrictionArrayArray<TC>(int size) where TC : C
        {
            TC[][] array = new TC[][] { null, new TC[0], new TC[] { null, default(TC), (TC)new C() }, new TC[100] };
            TC[][] result = new TC[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static TCn[][] GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(int size) where TCn : C, new()
        {
            TCn[][] array = new TCn[][] { null, new TCn[0], new TCn[] { null, default(TCn), new TCn(), (TCn)new C() }, new TCn[100] };
            TCn[][] result = new TCn[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        #endregion

        #region Check array index

        private static void CheckBoolArrayArrayIndex(bool[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckBoolArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckByteArrayArrayIndex(byte[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckByteArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckCustomArrayArrayIndex(C[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckCustomArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckCharArrayArrayIndex(char[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckCharArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckCustom2ArrayArrayIndex(D[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckCustom2ArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckDecimalArrayArrayIndex(decimal[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckDecimalArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckDelegateArrayArrayIndex(Delegate[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckDelegateArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckdoubleArrayArrayIndex(double[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckdoubleArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckEnumArrayArrayIndex(E[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckEnumArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckEnumLongArrayArrayIndex(El[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckEnumLongArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckFloatArrayArrayIndex(float[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckFloatArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckFuncArrayArrayIndex(Func<object>[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckFuncArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckInterfaceArrayArrayIndex(I[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckInterfaceArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckIEquatableArrayArrayIndex(IEquatable<C>[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckIEquatableArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckIEquatable2ArrayArrayIndex(IEquatable<D>[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckIEquatable2ArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckIntArrayArrayIndex(int[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckIntArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckLongArrayArrayIndex(long[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckLongArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckObjectArrayArrayIndex(object[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckObjectArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckStructArrayArrayIndex(S[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckSByteArrayArrayIndex(sbyte[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckSByteArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckStructWithStringArrayArrayIndex(Sc[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithStringArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckStructWithStringAndStructArrayArrayIndex(Scs[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithStringAndStructArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckShortArrayArrayIndex(short[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckShortArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckStructWithTwoFieldsArrayArrayIndex(Sp[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithTwoFieldsArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckStructWithValueArrayArrayIndex(Ss[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithValueArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckStringArrayArrayIndex(string[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStringArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckUIntArrayArrayIndex(uint[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckUIntArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckULongArrayArrayIndex(ulong[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckULongArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckUShortArrayArrayIndex(ushort[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckUShortArrayArrayIndexExpression(array, i);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithCustomArrayArrayIndex<T>(T[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithCustomArrayArrayIndexExpression<T>(array, i);
            }

            Assert.True(success);
        }

        private static void CheckGenericArrayArrayIndex<T>(T[][] array)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericArrayArrayIndexExpression<T>(array, i);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithClassRestrictionArrayArrayIndex<Tc>(Tc[][] array) where Tc : class
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithClassRestrictionArrayArrayIndexExpression<Tc>(array, i);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(Tcn[][] array) where Tcn : class, new()
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithClassAndNewRestrictionArrayArrayIndexExpression<Tcn>(array, i);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithSubClassRestrictionArrayArrayIndex<TC>(TC[][] array) where TC : C
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithSubClassRestrictionArrayArrayIndexExpression<TC>(array, i);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(TCn[][] array) where TCn : C, new()
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithSubClassAndNewRestrictionArrayArrayIndexExpression<TCn>(array, i);
            }

            Assert.True(success);
        }

        #endregion

        #region Check index expression

        private static bool CheckBoolArrayArrayIndexExpression(bool[][] array, int index)
        {
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(bool[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckByteArrayArrayIndexExpression(byte[][] array, int index)
        {
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(byte[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckCustomArrayArrayIndexExpression(C[][] array, int index)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(C[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckCharArrayArrayIndexExpression(char[][] array, int index)
        {
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(char[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckCustom2ArrayArrayIndexExpression(D[][] array, int index)
        {
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(D[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckDecimalArrayArrayIndexExpression(decimal[][] array, int index)
        {
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(decimal[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckDelegateArrayArrayIndexExpression(Delegate[][] array, int index)
        {
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Delegate[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckdoubleArrayArrayIndexExpression(double[][] array, int index)
        {
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(double[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckEnumArrayArrayIndexExpression(E[][] array, int index)
        {
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(E[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckEnumLongArrayArrayIndexExpression(El[][] array, int index)
        {
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(El[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckFloatArrayArrayIndexExpression(float[][] array, int index)
        {
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(float[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckFuncArrayArrayIndexExpression(Func<object>[][] array, int index)
        {
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Func<object>[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckInterfaceArrayArrayIndexExpression(I[][] array, int index)
        {
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(I[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckIEquatableArrayArrayIndexExpression(IEquatable<C>[][] array, int index)
        {
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(IEquatable<C>[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckIEquatable2ArrayArrayIndexExpression(IEquatable<D>[][] array, int index)
        {
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(IEquatable<D>[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckIntArrayArrayIndexExpression(int[][] array, int index)
        {
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(int[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckLongArrayArrayIndexExpression(long[][] array, int index)
        {
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(long[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckObjectArrayArrayIndexExpression(object[][] array, int index)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(object[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructArrayArrayIndexExpression(S[][] array, int index)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(S[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckSByteArrayArrayIndexExpression(sbyte[][] array, int index)
        {
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(sbyte[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithStringArrayArrayIndexExpression(Sc[][] array, int index)
        {
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Sc[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithStringAndStructArrayArrayIndexExpression(Scs[][] array, int index)
        {
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Scs[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckShortArrayArrayIndexExpression(short[][] array, int index)
        {
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(short[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithTwoFieldsArrayArrayIndexExpression(Sp[][] array, int index)
        {
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Sp[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithValueArrayArrayIndexExpression(Ss[][] array, int index)
        {
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Ss[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStringArrayArrayIndexExpression(string[][] array, int index)
        {
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(string[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckUIntArrayArrayIndexExpression(uint[][] array, int index)
        {
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(uint[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckULongArrayArrayIndexExpression(ulong[][] array, int index)
        {
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(ulong[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckUShortArrayArrayIndexExpression(ushort[][] array, int index)
        {
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(ushort[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithCustomArrayArrayIndexExpression<T>(T[][] array, int index)
        {
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(T[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericArrayArrayIndexExpression<T>(T[][] array, int index)
        {
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(T[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithClassRestrictionArrayArrayIndexExpression<Tc>(Tc[][] array, int index) where Tc : class
        {
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Tc[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithClassAndNewRestrictionArrayArrayIndexExpression<Tcn>(Tcn[][] array, int index) where Tcn : class, new()
        {
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Tcn[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithSubClassRestrictionArrayArrayIndexExpression<TC>(TC[][] array, int index) where TC : C
        {
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(TC[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithSubClassAndNewRestrictionArrayArrayIndexExpression<TCn>(TCn[][] array, int index) where TCn : C, new()
        {
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(TCn[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile();
            return object.Equals(f(), array[index]);
        }

        #endregion

        #region Check exception array index

        private static void CheckExceptionBoolArrayArrayIndex(bool[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckBoolArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionByteArrayArrayIndex(byte[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckByteArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionCustomArrayArrayIndex(C[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckCustomArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionCharArrayArrayIndex(char[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckCharArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionCustom2ArrayArrayIndex(D[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckCustom2ArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionDecimalArrayArrayIndex(decimal[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckDecimalArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionDelegateArrayArrayIndex(Delegate[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckDelegateArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptiondoubleArrayArrayIndex(double[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckdoubleArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionEnumArrayArrayIndex(E[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckEnumArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionEnumLongArrayArrayIndex(El[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckEnumLongArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionFloatArrayArrayIndex(float[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckFloatArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionFuncArrayArrayIndex(Func<object>[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckFuncArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionInterfaceArrayArrayIndex(I[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckInterfaceArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionIEquatableArrayArrayIndex(IEquatable<C>[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckIEquatableArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionIEquatable2ArrayArrayIndex(IEquatable<D>[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckIEquatable2ArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionIntArrayArrayIndex(int[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckIntArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionLongArrayArrayIndex(long[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckLongArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionObjectArrayArrayIndex(object[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckObjectArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructArrayArrayIndex(S[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckStructArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionSByteArrayArrayIndex(sbyte[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckSByteArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructWithStringArrayArrayIndex(Sc[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckStructWithStringArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructWithStringAndStructArrayArrayIndex(Scs[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckStructWithStringAndStructArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionShortArrayArrayIndex(short[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckShortArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructWithTwoFieldsArrayArrayIndex(Sp[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckStructWithTwoFieldsArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructWithValueArrayArrayIndex(Ss[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckStructWithValueArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStringArrayArrayIndex(string[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckStringArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionUIntArrayArrayIndex(uint[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckUIntArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionULongArrayArrayIndex(ulong[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckULongArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionUShortArrayArrayIndex(ushort[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckUShortArrayArrayIndexExpression(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithCustomArrayArrayIndex<T>(T[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckGenericWithCustomArrayArrayIndexExpression<T>(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericArrayArrayIndex<T>(T[][] array, int index)
        {
            bool success = true;
            try
            {
                CheckGenericArrayArrayIndexExpression<T>(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(Tc[][] array, int index) where Tc : class
        {
            bool success = true;
            try
            {
                CheckGenericWithClassRestrictionArrayArrayIndexExpression<Tc>(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(Tcn[][] array, int index) where Tcn : class, new()
        {
            bool success = true;
            try
            {
                CheckGenericWithClassAndNewRestrictionArrayArrayIndexExpression<Tcn>(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(TC[][] array, int index) where TC : C
        {
            bool success = true;
            try
            {
                CheckGenericWithSubClassRestrictionArrayArrayIndexExpression<TC>(array, index); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(TCn[][] array, int index) where TCn : C, new()
        {
            bool success = true;
            try
            {
                CheckGenericWithSubClassAndNewRestrictionArrayArrayIndexExpression<TCn>(array, index); // expect to fail
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
