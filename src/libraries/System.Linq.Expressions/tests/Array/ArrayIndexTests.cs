// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ArrayIndexTests
    {
        #region Boolean tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayIndexTest(bool useInterpreter)
        {
            CheckBoolArrayIndex(GenerateBoolArray(0), useInterpreter);
            CheckBoolArrayIndex(GenerateBoolArray(1), useInterpreter);
            CheckBoolArrayIndex(GenerateBoolArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionBoolArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionBoolArrayIndex(null, -1, useInterpreter);
            CheckExceptionBoolArrayIndex(null, 0, useInterpreter);
            CheckExceptionBoolArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionBoolArrayIndex(GenerateBoolArray(0), -1, useInterpreter);
            CheckExceptionBoolArrayIndex(GenerateBoolArray(0), 0, useInterpreter);
            CheckExceptionBoolArrayIndex(GenerateBoolArray(1), -1, useInterpreter);
            CheckExceptionBoolArrayIndex(GenerateBoolArray(1), 1, useInterpreter);
            CheckExceptionBoolArrayIndex(GenerateBoolArray(5), -1, useInterpreter);
            CheckExceptionBoolArrayIndex(GenerateBoolArray(5), 5, useInterpreter);
        }

        #endregion

        #region Byte tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayIndexTest(bool useInterpreter)
        {
            CheckByteArrayIndex(GenerateByteArray(0), useInterpreter);
            CheckByteArrayIndex(GenerateByteArray(1), useInterpreter);
            CheckByteArrayIndex(GenerateByteArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionByteArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionByteArrayIndex(null, -1, useInterpreter);
            CheckExceptionByteArrayIndex(null, 0, useInterpreter);
            CheckExceptionByteArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionByteArrayIndex(GenerateByteArray(0), -1, useInterpreter);
            CheckExceptionByteArrayIndex(GenerateByteArray(0), 0, useInterpreter);
            CheckExceptionByteArrayIndex(GenerateByteArray(1), -1, useInterpreter);
            CheckExceptionByteArrayIndex(GenerateByteArray(1), 1, useInterpreter);
            CheckExceptionByteArrayIndex(GenerateByteArray(5), -1, useInterpreter);
            CheckExceptionByteArrayIndex(GenerateByteArray(5), 5, useInterpreter);
        }

        #endregion

        #region Custom type tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayIndexTest(bool useInterpreter)
        {
            CheckCustomArrayIndex(GenerateCustomArray(0), useInterpreter);
            CheckCustomArrayIndex(GenerateCustomArray(1), useInterpreter);
            CheckCustomArrayIndex(GenerateCustomArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionCustomArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionCustomArrayIndex(null, -1, useInterpreter);
            CheckExceptionCustomArrayIndex(null, 0, useInterpreter);
            CheckExceptionCustomArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionCustomArrayIndex(GenerateCustomArray(0), -1, useInterpreter);
            CheckExceptionCustomArrayIndex(GenerateCustomArray(0), 0, useInterpreter);
            CheckExceptionCustomArrayIndex(GenerateCustomArray(1), -1, useInterpreter);
            CheckExceptionCustomArrayIndex(GenerateCustomArray(1), 1, useInterpreter);
            CheckExceptionCustomArrayIndex(GenerateCustomArray(5), -1, useInterpreter);
            CheckExceptionCustomArrayIndex(GenerateCustomArray(5), 5, useInterpreter);
        }

        #endregion

        #region Char tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayIndexTest(bool useInterpreter)
        {
            CheckCharArrayIndex(GenerateCharArray(0), useInterpreter);
            CheckCharArrayIndex(GenerateCharArray(1), useInterpreter);
            CheckCharArrayIndex(GenerateCharArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionCharArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionCharArrayIndex(null, -1, useInterpreter);
            CheckExceptionCharArrayIndex(null, 0, useInterpreter);
            CheckExceptionCharArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionCharArrayIndex(GenerateCharArray(0), -1, useInterpreter);
            CheckExceptionCharArrayIndex(GenerateCharArray(0), 0, useInterpreter);
            CheckExceptionCharArrayIndex(GenerateCharArray(1), -1, useInterpreter);
            CheckExceptionCharArrayIndex(GenerateCharArray(1), 1, useInterpreter);
            CheckExceptionCharArrayIndex(GenerateCharArray(5), -1, useInterpreter);
            CheckExceptionCharArrayIndex(GenerateCharArray(5), 5, useInterpreter);
        }

        #endregion

        #region Custom 2 type tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayIndexTest(bool useInterpreter)
        {
            CheckCustom2ArrayIndex(GenerateCustom2Array(0), useInterpreter);
            CheckCustom2ArrayIndex(GenerateCustom2Array(1), useInterpreter);
            CheckCustom2ArrayIndex(GenerateCustom2Array(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionCustom2ArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionCustom2ArrayIndex(null, -1, useInterpreter);
            CheckExceptionCustom2ArrayIndex(null, 0, useInterpreter);
            CheckExceptionCustom2ArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionCustom2ArrayIndex(GenerateCustom2Array(0), -1, useInterpreter);
            CheckExceptionCustom2ArrayIndex(GenerateCustom2Array(0), 0, useInterpreter);
            CheckExceptionCustom2ArrayIndex(GenerateCustom2Array(1), -1, useInterpreter);
            CheckExceptionCustom2ArrayIndex(GenerateCustom2Array(1), 1, useInterpreter);
            CheckExceptionCustom2ArrayIndex(GenerateCustom2Array(5), -1, useInterpreter);
            CheckExceptionCustom2ArrayIndex(GenerateCustom2Array(5), 5, useInterpreter);
        }

        #endregion

        #region Decimal tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayIndexTest(bool useInterpreter)
        {
            CheckDecimalArrayIndex(GenerateDecimalArray(0), useInterpreter);
            CheckDecimalArrayIndex(GenerateDecimalArray(1), useInterpreter);
            CheckDecimalArrayIndex(GenerateDecimalArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionDecimalArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionDecimalArrayIndex(null, -1, useInterpreter);
            CheckExceptionDecimalArrayIndex(null, 0, useInterpreter);
            CheckExceptionDecimalArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionDecimalArrayIndex(GenerateDecimalArray(0), -1, useInterpreter);
            CheckExceptionDecimalArrayIndex(GenerateDecimalArray(0), 0, useInterpreter);
            CheckExceptionDecimalArrayIndex(GenerateDecimalArray(1), -1, useInterpreter);
            CheckExceptionDecimalArrayIndex(GenerateDecimalArray(1), 1, useInterpreter);
            CheckExceptionDecimalArrayIndex(GenerateDecimalArray(5), -1, useInterpreter);
            CheckExceptionDecimalArrayIndex(GenerateDecimalArray(5), 5, useInterpreter);
        }

        #endregion

        #region Delegate tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayIndexTest(bool useInterpreter)
        {
            CheckDelegateArrayIndex(GenerateDelegateArray(0), useInterpreter);
            CheckDelegateArrayIndex(GenerateDelegateArray(1), useInterpreter);
            CheckDelegateArrayIndex(GenerateDelegateArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionDelegateArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionDelegateArrayIndex(null, -1, useInterpreter);
            CheckExceptionDelegateArrayIndex(null, 0, useInterpreter);
            CheckExceptionDelegateArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionDelegateArrayIndex(GenerateDelegateArray(0), -1, useInterpreter);
            CheckExceptionDelegateArrayIndex(GenerateDelegateArray(0), 0, useInterpreter);
            CheckExceptionDelegateArrayIndex(GenerateDelegateArray(1), -1, useInterpreter);
            CheckExceptionDelegateArrayIndex(GenerateDelegateArray(1), 1, useInterpreter);
            CheckExceptionDelegateArrayIndex(GenerateDelegateArray(5), -1, useInterpreter);
            CheckExceptionDelegateArrayIndex(GenerateDelegateArray(5), 5, useInterpreter);
        }

        #endregion

        #region Double tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayIndexTest(bool useInterpreter)
        {
            CheckDoubleArrayIndex(GenerateDoubleArray(0), useInterpreter);
            CheckDoubleArrayIndex(GenerateDoubleArray(1), useInterpreter);
            CheckDoubleArrayIndex(GenerateDoubleArray(9), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionDoubleArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionDoubleArrayIndex(null, -1, useInterpreter);
            CheckExceptionDoubleArrayIndex(null, 0, useInterpreter);
            CheckExceptionDoubleArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionDoubleArrayIndex(GenerateDoubleArray(0), -1, useInterpreter);
            CheckExceptionDoubleArrayIndex(GenerateDoubleArray(0), 0, useInterpreter);
            CheckExceptionDoubleArrayIndex(GenerateDoubleArray(1), -1, useInterpreter);
            CheckExceptionDoubleArrayIndex(GenerateDoubleArray(1), 1, useInterpreter);
            CheckExceptionDoubleArrayIndex(GenerateDoubleArray(9), -1, useInterpreter);
            CheckExceptionDoubleArrayIndex(GenerateDoubleArray(9), 9, useInterpreter);
        }

        #endregion

        #region Enum tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayIndexTest(bool useInterpreter)
        {
            CheckEnumArrayIndex(GenerateEnumArray(0), useInterpreter);
            CheckEnumArrayIndex(GenerateEnumArray(1), useInterpreter);
            CheckEnumArrayIndex(GenerateEnumArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionEnumArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionEnumArrayIndex(null, -1, useInterpreter);
            CheckExceptionEnumArrayIndex(null, 0, useInterpreter);
            CheckExceptionEnumArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionEnumArrayIndex(GenerateEnumArray(0), -1, useInterpreter);
            CheckExceptionEnumArrayIndex(GenerateEnumArray(0), 0, useInterpreter);
            CheckExceptionEnumArrayIndex(GenerateEnumArray(1), -1, useInterpreter);
            CheckExceptionEnumArrayIndex(GenerateEnumArray(1), 1, useInterpreter);
            CheckExceptionEnumArrayIndex(GenerateEnumArray(5), -1, useInterpreter);
            CheckExceptionEnumArrayIndex(GenerateEnumArray(5), 5, useInterpreter);
        }

        #endregion

        #region Enum long tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckEnumLongArrayIndexTest(bool useInterpreter)
        {
            CheckEnumLongArrayIndex(GenerateEnumLongArray(0), useInterpreter);
            CheckEnumLongArrayIndex(GenerateEnumLongArray(1), useInterpreter);
            CheckEnumLongArrayIndex(GenerateEnumLongArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionEnumLongArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionEnumLongArrayIndex(null, -1, useInterpreter);
            CheckExceptionEnumLongArrayIndex(null, 0, useInterpreter);
            CheckExceptionEnumLongArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionEnumLongArrayIndex(GenerateEnumLongArray(0), -1, useInterpreter);
            CheckExceptionEnumLongArrayIndex(GenerateEnumLongArray(0), 0, useInterpreter);
            CheckExceptionEnumLongArrayIndex(GenerateEnumLongArray(1), -1, useInterpreter);
            CheckExceptionEnumLongArrayIndex(GenerateEnumLongArray(1), 1, useInterpreter);
            CheckExceptionEnumLongArrayIndex(GenerateEnumLongArray(5), -1, useInterpreter);
            CheckExceptionEnumLongArrayIndex(GenerateEnumLongArray(5), 5, useInterpreter);
        }

        #endregion

        #region Float tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayIndexTest(bool useInterpreter)
        {
            CheckFloatArrayIndex(GenerateFloatArray(0), useInterpreter);
            CheckFloatArrayIndex(GenerateFloatArray(1), useInterpreter);
            CheckFloatArrayIndex(GenerateFloatArray(9), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionFloatArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionFloatArrayIndex(null, -1, useInterpreter);
            CheckExceptionFloatArrayIndex(null, 0, useInterpreter);
            CheckExceptionFloatArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionFloatArrayIndex(GenerateFloatArray(0), -1, useInterpreter);
            CheckExceptionFloatArrayIndex(GenerateFloatArray(0), 0, useInterpreter);
            CheckExceptionFloatArrayIndex(GenerateFloatArray(1), -1, useInterpreter);
            CheckExceptionFloatArrayIndex(GenerateFloatArray(1), 1, useInterpreter);
            CheckExceptionFloatArrayIndex(GenerateFloatArray(9), -1, useInterpreter);
            CheckExceptionFloatArrayIndex(GenerateFloatArray(9), 9, useInterpreter);
        }

        #endregion

        #region Func tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayIndexTest(bool useInterpreter)
        {
            CheckFuncArrayIndex(GenerateFuncArray(0), useInterpreter);
            CheckFuncArrayIndex(GenerateFuncArray(1), useInterpreter);
            CheckFuncArrayIndex(GenerateFuncArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionFuncArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionFuncArrayIndex(null, -1, useInterpreter);
            CheckExceptionFuncArrayIndex(null, 0, useInterpreter);
            CheckExceptionFuncArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionFuncArrayIndex(GenerateFuncArray(0), -1, useInterpreter);
            CheckExceptionFuncArrayIndex(GenerateFuncArray(0), 0, useInterpreter);
            CheckExceptionFuncArrayIndex(GenerateFuncArray(1), -1, useInterpreter);
            CheckExceptionFuncArrayIndex(GenerateFuncArray(1), 1, useInterpreter);
            CheckExceptionFuncArrayIndex(GenerateFuncArray(5), -1, useInterpreter);
            CheckExceptionFuncArrayIndex(GenerateFuncArray(5), 5, useInterpreter);
        }

        #endregion

        #region Interface tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayIndexTest(bool useInterpreter)
        {
            CheckInterfaceArrayIndex(GenerateInterfaceArray(0), useInterpreter);
            CheckInterfaceArrayIndex(GenerateInterfaceArray(1), useInterpreter);
            CheckInterfaceArrayIndex(GenerateInterfaceArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionInterfaceArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionInterfaceArrayIndex(null, -1, useInterpreter);
            CheckExceptionInterfaceArrayIndex(null, 0, useInterpreter);
            CheckExceptionInterfaceArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionInterfaceArrayIndex(GenerateInterfaceArray(0), -1, useInterpreter);
            CheckExceptionInterfaceArrayIndex(GenerateInterfaceArray(0), 0, useInterpreter);
            CheckExceptionInterfaceArrayIndex(GenerateInterfaceArray(1), -1, useInterpreter);
            CheckExceptionInterfaceArrayIndex(GenerateInterfaceArray(1), 1, useInterpreter);
            CheckExceptionInterfaceArrayIndex(GenerateInterfaceArray(5), -1, useInterpreter);
            CheckExceptionInterfaceArrayIndex(GenerateInterfaceArray(5), 5, useInterpreter);
        }

        #endregion

        #region IEquatable custom tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayIndexTest(bool useInterpreter)
        {
            CheckIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(0), useInterpreter);
            CheckIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(1), useInterpreter);
            CheckIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionIEquatableCustomArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionIEquatableCustomArrayIndex(null, -1, useInterpreter);
            CheckExceptionIEquatableCustomArrayIndex(null, 0, useInterpreter);
            CheckExceptionIEquatableCustomArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(0), -1, useInterpreter);
            CheckExceptionIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(0), 0, useInterpreter);
            CheckExceptionIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(1), -1, useInterpreter);
            CheckExceptionIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(1), 1, useInterpreter);
            CheckExceptionIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(5), -1, useInterpreter);
            CheckExceptionIEquatableCustomArrayIndex(GenerateIEquatableCustomArray(5), 5, useInterpreter);
        }

        #endregion

        #region IEquatable custom 2 tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayIndexTest(bool useInterpreter)
        {
            CheckIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(0), useInterpreter);
            CheckIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(1), useInterpreter);
            CheckIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionIEquatableCustom2ArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionIEquatableCustom2ArrayIndex(null, -1, useInterpreter);
            CheckExceptionIEquatableCustom2ArrayIndex(null, 0, useInterpreter);
            CheckExceptionIEquatableCustom2ArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(0), -1, useInterpreter);
            CheckExceptionIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(0), 0, useInterpreter);
            CheckExceptionIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(1), -1, useInterpreter);
            CheckExceptionIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(1), 1, useInterpreter);
            CheckExceptionIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(5), -1, useInterpreter);
            CheckExceptionIEquatableCustom2ArrayIndex(GenerateIEquatableCustom2Array(5), 5, useInterpreter);
        }

        #endregion

        #region Int tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayIndexTest(bool useInterpreter)
        {
            CheckIntArrayIndex(GenerateIntArray(0), useInterpreter);
            CheckIntArrayIndex(GenerateIntArray(1), useInterpreter);
            CheckIntArrayIndex(GenerateIntArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionIntArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionIntArrayIndex(null, -1, useInterpreter);
            CheckExceptionIntArrayIndex(null, 0, useInterpreter);
            CheckExceptionIntArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionIntArrayIndex(GenerateIntArray(0), -1, useInterpreter);
            CheckExceptionIntArrayIndex(GenerateIntArray(0), 0, useInterpreter);
            CheckExceptionIntArrayIndex(GenerateIntArray(1), -1, useInterpreter);
            CheckExceptionIntArrayIndex(GenerateIntArray(1), 1, useInterpreter);
            CheckExceptionIntArrayIndex(GenerateIntArray(5), -1, useInterpreter);
            CheckExceptionIntArrayIndex(GenerateIntArray(5), 5, useInterpreter);
        }

        #endregion

        #region Long tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayIndexTest(bool useInterpreter)
        {
            CheckLongArrayIndex(GenerateLongArray(0), useInterpreter);
            CheckLongArrayIndex(GenerateLongArray(1), useInterpreter);
            CheckLongArrayIndex(GenerateLongArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionLongArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionLongArrayIndex(null, -1, useInterpreter);
            CheckExceptionLongArrayIndex(null, 0, useInterpreter);
            CheckExceptionLongArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionLongArrayIndex(GenerateLongArray(0), -1, useInterpreter);
            CheckExceptionLongArrayIndex(GenerateLongArray(0), 0, useInterpreter);
            CheckExceptionLongArrayIndex(GenerateLongArray(1), -1, useInterpreter);
            CheckExceptionLongArrayIndex(GenerateLongArray(1), 1, useInterpreter);
            CheckExceptionLongArrayIndex(GenerateLongArray(5), -1, useInterpreter);
            CheckExceptionLongArrayIndex(GenerateLongArray(5), 5, useInterpreter);
        }

        #endregion

        #region Object tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayIndexTest(bool useInterpreter)
        {
            CheckObjectArrayIndex(GenerateObjectArray(0), useInterpreter);
            CheckObjectArrayIndex(GenerateObjectArray(1), useInterpreter);
            CheckObjectArrayIndex(GenerateObjectArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionObjectArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionObjectArrayIndex(null, -1, useInterpreter);
            CheckExceptionObjectArrayIndex(null, 0, useInterpreter);
            CheckExceptionObjectArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionObjectArrayIndex(GenerateObjectArray(0), -1, useInterpreter);
            CheckExceptionObjectArrayIndex(GenerateObjectArray(0), 0, useInterpreter);
            CheckExceptionObjectArrayIndex(GenerateObjectArray(1), -1, useInterpreter);
            CheckExceptionObjectArrayIndex(GenerateObjectArray(1), 1, useInterpreter);
            CheckExceptionObjectArrayIndex(GenerateObjectArray(5), -1, useInterpreter);
            CheckExceptionObjectArrayIndex(GenerateObjectArray(5), 5, useInterpreter);
        }

        #endregion

        #region Struct tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayIndexTest(bool useInterpreter)
        {
            CheckStructArrayIndex(GenerateStructArray(0), useInterpreter);
            CheckStructArrayIndex(GenerateStructArray(1), useInterpreter);
            CheckStructArrayIndex(GenerateStructArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionStructArrayIndex(null, -1, useInterpreter);
            CheckExceptionStructArrayIndex(null, 0, useInterpreter);
            CheckExceptionStructArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionStructArrayIndex(GenerateStructArray(0), -1, useInterpreter);
            CheckExceptionStructArrayIndex(GenerateStructArray(0), 0, useInterpreter);
            CheckExceptionStructArrayIndex(GenerateStructArray(1), -1, useInterpreter);
            CheckExceptionStructArrayIndex(GenerateStructArray(1), 1, useInterpreter);
            CheckExceptionStructArrayIndex(GenerateStructArray(5), -1, useInterpreter);
            CheckExceptionStructArrayIndex(GenerateStructArray(5), 5, useInterpreter);
        }

        #endregion

        #region SByte tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayIndexTest(bool useInterpreter)
        {
            CheckSByteArrayIndex(GenerateSByteArray(0), useInterpreter);
            CheckSByteArrayIndex(GenerateSByteArray(1), useInterpreter);
            CheckSByteArrayIndex(GenerateSByteArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionSByteArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionSByteArrayIndex(null, -1, useInterpreter);
            CheckExceptionSByteArrayIndex(null, 0, useInterpreter);
            CheckExceptionSByteArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionSByteArrayIndex(GenerateSByteArray(0), -1, useInterpreter);
            CheckExceptionSByteArrayIndex(GenerateSByteArray(0), 0, useInterpreter);
            CheckExceptionSByteArrayIndex(GenerateSByteArray(1), -1, useInterpreter);
            CheckExceptionSByteArrayIndex(GenerateSByteArray(1), 1, useInterpreter);
            CheckExceptionSByteArrayIndex(GenerateSByteArray(5), -1, useInterpreter);
            CheckExceptionSByteArrayIndex(GenerateSByteArray(5), 5, useInterpreter);
        }

        #endregion

        #region StructWithString tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayIndexTest(bool useInterpreter)
        {
            CheckStructWithStringArrayIndex(GenerateStructWithStringArray(0), useInterpreter);
            CheckStructWithStringArrayIndex(GenerateStructWithStringArray(1), useInterpreter);
            CheckStructWithStringArrayIndex(GenerateStructWithStringArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithStringArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionStructWithStringArrayIndex(null, -1, useInterpreter);
            CheckExceptionStructWithStringArrayIndex(null, 0, useInterpreter);
            CheckExceptionStructWithStringArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionStructWithStringArrayIndex(GenerateStructWithStringArray(0), -1, useInterpreter);
            CheckExceptionStructWithStringArrayIndex(GenerateStructWithStringArray(0), 0, useInterpreter);
            CheckExceptionStructWithStringArrayIndex(GenerateStructWithStringArray(1), -1, useInterpreter);
            CheckExceptionStructWithStringArrayIndex(GenerateStructWithStringArray(1), 1, useInterpreter);
            CheckExceptionStructWithStringArrayIndex(GenerateStructWithStringArray(5), -1, useInterpreter);
            CheckExceptionStructWithStringArrayIndex(GenerateStructWithStringArray(5), 5, useInterpreter);
        }

        #endregion

        #region StructWithValueAndString tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueAndStringArrayIndexTest(bool useInterpreter)
        {
            CheckStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(0), useInterpreter);
            CheckStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(1), useInterpreter);
            CheckStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithValueAndStringArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionStructWithValueAndStringArrayIndex(null, -1, useInterpreter);
            CheckExceptionStructWithValueAndStringArrayIndex(null, 0, useInterpreter);
            CheckExceptionStructWithValueAndStringArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(0), -1, useInterpreter);
            CheckExceptionStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(0), 0, useInterpreter);
            CheckExceptionStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(1), -1, useInterpreter);
            CheckExceptionStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(1), 1, useInterpreter);
            CheckExceptionStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(5), -1, useInterpreter);
            CheckExceptionStructWithValueAndStringArrayIndex(GenerateStructWithValueAndStringArray(5), 5, useInterpreter);
        }

        #endregion

        #region Short tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayIndexTest(bool useInterpreter)
        {
            CheckShortArrayIndex(GenerateShortArray(0), useInterpreter);
            CheckShortArrayIndex(GenerateShortArray(1), useInterpreter);
            CheckShortArrayIndex(GenerateShortArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionShortArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionShortArrayIndex(null, -1, useInterpreter);
            CheckExceptionShortArrayIndex(null, 0, useInterpreter);
            CheckExceptionShortArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionShortArrayIndex(GenerateShortArray(0), -1, useInterpreter);
            CheckExceptionShortArrayIndex(GenerateShortArray(0), 0, useInterpreter);
            CheckExceptionShortArrayIndex(GenerateShortArray(1), -1, useInterpreter);
            CheckExceptionShortArrayIndex(GenerateShortArray(1), 1, useInterpreter);
            CheckExceptionShortArrayIndex(GenerateShortArray(5), -1, useInterpreter);
            CheckExceptionShortArrayIndex(GenerateShortArray(5), 5, useInterpreter);
        }

        #endregion

        #region StructWithParameters tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithParametersArrayIndexTest(bool useInterpreter)
        {
            CheckStructWithParametersArrayIndex(GenerateStructWithParametersArray(0), useInterpreter);
            CheckStructWithParametersArrayIndex(GenerateStructWithParametersArray(1), useInterpreter);
            CheckStructWithParametersArrayIndex(GenerateStructWithParametersArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithParametersArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionStructWithParametersArrayIndex(null, -1, useInterpreter);
            CheckExceptionStructWithParametersArrayIndex(null, 0, useInterpreter);
            CheckExceptionStructWithParametersArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionStructWithParametersArrayIndex(GenerateStructWithParametersArray(0), -1, useInterpreter);
            CheckExceptionStructWithParametersArrayIndex(GenerateStructWithParametersArray(0), 0, useInterpreter);
            CheckExceptionStructWithParametersArrayIndex(GenerateStructWithParametersArray(1), -1, useInterpreter);
            CheckExceptionStructWithParametersArrayIndex(GenerateStructWithParametersArray(1), 1, useInterpreter);
            CheckExceptionStructWithParametersArrayIndex(GenerateStructWithParametersArray(5), -1, useInterpreter);
            CheckExceptionStructWithParametersArrayIndex(GenerateStructWithParametersArray(5), 5, useInterpreter);
        }

        #endregion

        #region StructWithStruct tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStructArrayIndexTest(bool useInterpreter)
        {
            CheckStructWithStructArrayIndex(GenerateStructWithStructArray(0), useInterpreter);
            CheckStructWithStructArrayIndex(GenerateStructWithStructArray(1), useInterpreter);
            CheckStructWithStructArrayIndex(GenerateStructWithStructArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithStructArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionStructWithStructArrayIndex(null, -1, useInterpreter);
            CheckExceptionStructWithStructArrayIndex(null, 0, useInterpreter);
            CheckExceptionStructWithStructArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionStructWithStructArrayIndex(GenerateStructWithStructArray(0), -1, useInterpreter);
            CheckExceptionStructWithStructArrayIndex(GenerateStructWithStructArray(0), 0, useInterpreter);
            CheckExceptionStructWithStructArrayIndex(GenerateStructWithStructArray(1), -1, useInterpreter);
            CheckExceptionStructWithStructArrayIndex(GenerateStructWithStructArray(1), 1, useInterpreter);
            CheckExceptionStructWithStructArrayIndex(GenerateStructWithStructArray(5), -1, useInterpreter);
            CheckExceptionStructWithStructArrayIndex(GenerateStructWithStructArray(5), 5, useInterpreter);
        }

        #endregion

        #region String tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayIndexTest(bool useInterpreter)
        {
            CheckStringArrayIndex(GenerateStringArray(0), useInterpreter);
            CheckStringArrayIndex(GenerateStringArray(1), useInterpreter);
            CheckStringArrayIndex(GenerateStringArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStringArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionStringArrayIndex(null, -1, useInterpreter);
            CheckExceptionStringArrayIndex(null, 0, useInterpreter);
            CheckExceptionStringArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionStringArrayIndex(GenerateStringArray(0), -1, useInterpreter);
            CheckExceptionStringArrayIndex(GenerateStringArray(0), 0, useInterpreter);
            CheckExceptionStringArrayIndex(GenerateStringArray(1), -1, useInterpreter);
            CheckExceptionStringArrayIndex(GenerateStringArray(1), 1, useInterpreter);
            CheckExceptionStringArrayIndex(GenerateStringArray(5), -1, useInterpreter);
            CheckExceptionStringArrayIndex(GenerateStringArray(5), 5, useInterpreter);
        }

        #endregion

        #region UInt tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayIndexTest(bool useInterpreter)
        {
            CheckUIntArrayIndex(GenerateUIntArray(0), useInterpreter);
            CheckUIntArrayIndex(GenerateUIntArray(1), useInterpreter);
            CheckUIntArrayIndex(GenerateUIntArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionUIntArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionUIntArrayIndex(null, -1, useInterpreter);
            CheckExceptionUIntArrayIndex(null, 0, useInterpreter);
            CheckExceptionUIntArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionUIntArrayIndex(GenerateUIntArray(0), -1, useInterpreter);
            CheckExceptionUIntArrayIndex(GenerateUIntArray(0), 0, useInterpreter);
            CheckExceptionUIntArrayIndex(GenerateUIntArray(1), -1, useInterpreter);
            CheckExceptionUIntArrayIndex(GenerateUIntArray(1), 1, useInterpreter);
            CheckExceptionUIntArrayIndex(GenerateUIntArray(5), -1, useInterpreter);
            CheckExceptionUIntArrayIndex(GenerateUIntArray(5), 5, useInterpreter);
        }

        #endregion

        #region ULong tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayIndexTest(bool useInterpreter)
        {
            CheckULongArrayIndex(GenerateULongArray(0), useInterpreter);
            CheckULongArrayIndex(GenerateULongArray(1), useInterpreter);
            CheckULongArrayIndex(GenerateULongArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionULongArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionULongArrayIndex(null, -1, useInterpreter);
            CheckExceptionULongArrayIndex(null, 0, useInterpreter);
            CheckExceptionULongArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionULongArrayIndex(GenerateULongArray(0), -1, useInterpreter);
            CheckExceptionULongArrayIndex(GenerateULongArray(0), 0, useInterpreter);
            CheckExceptionULongArrayIndex(GenerateULongArray(1), -1, useInterpreter);
            CheckExceptionULongArrayIndex(GenerateULongArray(1), 1, useInterpreter);
            CheckExceptionULongArrayIndex(GenerateULongArray(5), -1, useInterpreter);
            CheckExceptionULongArrayIndex(GenerateULongArray(5), 5, useInterpreter);
        }

        #endregion

        #region UShort tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayIndexTest(bool useInterpreter)
        {
            CheckUShortArrayIndex(GenerateUShortArray(0), useInterpreter);
            CheckUShortArrayIndex(GenerateUShortArray(1), useInterpreter);
            CheckUShortArrayIndex(GenerateUShortArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionUShortArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionUShortArrayIndex(null, -1, useInterpreter);
            CheckExceptionUShortArrayIndex(null, 0, useInterpreter);
            CheckExceptionUShortArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionUShortArrayIndex(GenerateUShortArray(0), -1, useInterpreter);
            CheckExceptionUShortArrayIndex(GenerateUShortArray(0), 0, useInterpreter);
            CheckExceptionUShortArrayIndex(GenerateUShortArray(1), -1, useInterpreter);
            CheckExceptionUShortArrayIndex(GenerateUShortArray(1), 1, useInterpreter);
            CheckExceptionUShortArrayIndex(GenerateUShortArray(5), -1, useInterpreter);
            CheckExceptionUShortArrayIndex(GenerateUShortArray(5), 5, useInterpreter);
        }

        #endregion

        #region Generic tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomArrayIndexTest(bool useInterpreter)
        {
            CheckGenericArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericEnumArrayIndexTest(bool useInterpreter)
        {
            CheckGenericArrayIndexTestHelper<E>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericEnumArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayIndexTestHelper<E>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericObjectArrayIndexTest(bool useInterpreter)
        {
            CheckGenericArrayIndexTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericObjectArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayIndexTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructArrayIndexTest(bool useInterpreter)
        {
            CheckGenericArrayIndexTestHelper<S>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericStructArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayIndexTestHelper<S>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithValueAndStringArrayIndexTest(bool useInterpreter)
        {
            CheckGenericArrayIndexTestHelper<Scs>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericStructWithValueAndStringArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayIndexTestHelper<Scs>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericWithClassRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassRestrictionArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithObjectRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericWithObjectRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassRestrictionArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithSubClassRestrictionArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericWithSubClassRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithSubClassRestrictionArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithNewClassRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithNewClassRestrictionArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericWithNewClassRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithNewClassRestrictionArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassNewRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithSubClassNewRestrictionArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericWithSubClassNewRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassNewObjectRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithNewClassRestrictionArrayIndexTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericWithSubClassNewObjectRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithNewClassRestrictionArrayIndexTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayIndexTestHelper<E>(useInterpreter);
            CheckGenericWithStructRestrictionArrayIndexTestHelper<S>(useInterpreter);
            CheckGenericWithStructRestrictionArrayIndexTestHelper<Scs>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericWithStructRestrictionArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithStructRestrictionArrayIndexTestHelper<E>(useInterpreter);
            CheckExceptionGenericWithStructRestrictionArrayIndexTestHelper<S>(useInterpreter);
            CheckExceptionGenericWithStructRestrictionArrayIndexTestHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic test helpers

        private static void CheckGenericArrayIndexTestHelper<T>(bool useInterpreter)
        {
            CheckGenericArrayIndex<T>(GenerateGenericArray<T>(0), useInterpreter);
            CheckGenericArrayIndex<T>(GenerateGenericArray<T>(1), useInterpreter);
            CheckGenericArrayIndex<T>(GenerateGenericArray<T>(5), useInterpreter);
        }

        private static void CheckExceptionGenericArrayIndexTestHelper<T>(bool useInterpreter)
        {
            // null arrays
            CheckExceptionGenericArrayIndex<T>(null, -1, useInterpreter);
            CheckExceptionGenericArrayIndex<T>(null, 0, useInterpreter);
            CheckExceptionGenericArrayIndex<T>(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionGenericArrayIndex<T>(GenerateGenericArray<T>(0), -1, useInterpreter);
            CheckExceptionGenericArrayIndex<T>(GenerateGenericArray<T>(0), 0, useInterpreter);
            CheckExceptionGenericArrayIndex<T>(GenerateGenericArray<T>(1), -1, useInterpreter);
            CheckExceptionGenericArrayIndex<T>(GenerateGenericArray<T>(1), 1, useInterpreter);
            CheckExceptionGenericArrayIndex<T>(GenerateGenericArray<T>(5), -1, useInterpreter);
            CheckExceptionGenericArrayIndex<T>(GenerateGenericArray<T>(5), 5, useInterpreter);
        }

        private static void CheckGenericWithClassRestrictionArrayIndexTestHelper<Tc>(bool useInterpreter) where Tc : class
        {
            CheckGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(0), useInterpreter);
            CheckGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(1), useInterpreter);
            CheckGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(5), useInterpreter);
        }

        private static void CheckExceptionGenericWithClassRestrictionArrayIndexTestHelper<Tc>(bool useInterpreter) where Tc : class
        {
            // null arrays
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(null, -1, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(null, 0, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(0), -1, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(0), 0, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(1), -1, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(1), 1, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(5), -1, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(5), 5, useInterpreter);
        }

        private static void CheckGenericWithSubClassRestrictionArrayIndexTestHelper<Tc>(bool useInterpreter) where Tc : C
        {
            CheckGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(0), useInterpreter);
            CheckGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(1), useInterpreter);
            CheckGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(5), useInterpreter);
        }

        private static void CheckExceptionGenericWithSubClassRestrictionArrayIndexTestHelper<Tc>(bool useInterpreter) where Tc : C
        {
            // null arrays
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(null, -1, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(null, 0, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(0), -1, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(0), 0, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(1), -1, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(1), 1, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(5), -1, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(GenerateGenericWithSubClassRestrictionArray<Tc>(5), 5, useInterpreter);
        }

        private static void CheckGenericWithNewClassRestrictionArrayIndexTestHelper<Tcn>(bool useInterpreter) where Tcn : class, new()
        {
            CheckGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(0), useInterpreter);
            CheckGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(1), useInterpreter);
            CheckGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(5), useInterpreter);
        }

        private static void CheckExceptionGenericWithNewClassRestrictionArrayIndexTestHelper<Tcn>(bool useInterpreter) where Tcn : class, new()
        {
            // null arrays
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(null, -1, useInterpreter);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(null, 0, useInterpreter);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(0), -1, useInterpreter);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(0), 0, useInterpreter);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(1), -1, useInterpreter);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(1), 1, useInterpreter);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(5), -1, useInterpreter);
            CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(GenerateGenericWithNewClassRestrictionArray<Tcn>(5), 5, useInterpreter);
        }

        private static void CheckGenericWithSubClassNewRestrictionArrayIndexTestHelper<Tcn>(bool useInterpreter) where Tcn : C, new()
        {
            CheckGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(0), useInterpreter);
            CheckGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(1), useInterpreter);
            CheckGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(5), useInterpreter);
        }

        private static void CheckExceptionGenericWithSubClassNewRestrictionArrayIndexTestHelper<Tcn>(bool useInterpreter) where Tcn : C, new()
        {
            // null arrays
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(null, -1, useInterpreter);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(null, 0, useInterpreter);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(0), -1, useInterpreter);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(0), 0, useInterpreter);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(1), -1, useInterpreter);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(1), 1, useInterpreter);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(5), -1, useInterpreter);
            CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(GenerateGenericWithSubClassNewRestrictionArray<Tcn>(5), 5, useInterpreter);
        }

        private static void CheckGenericWithStructRestrictionArrayIndexTestHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            CheckGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(0), useInterpreter);
            CheckGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(1), useInterpreter);
            CheckGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(5), useInterpreter);
        }

        private static void CheckExceptionGenericWithStructRestrictionArrayIndexTestHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            // null arrays
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(null, -1, useInterpreter);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(null, 0, useInterpreter);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(0), -1, useInterpreter);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(0), 0, useInterpreter);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(1), -1, useInterpreter);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(1), 1, useInterpreter);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(5), -1, useInterpreter);
            CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(GenerateGenericWithStructRestrictionArray<Ts>(5), 5, useInterpreter);
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

        private static void CheckBoolArrayIndex(bool[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckBoolArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckByteArrayIndex(byte[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckByteArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckCustomArrayIndex(C[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckCustomArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckCharArrayIndex(char[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckCharArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckCustom2ArrayIndex(D[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckCustom2ArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckDecimalArrayIndex(decimal[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckDecimalArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckDelegateArrayIndex(Delegate[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckDelegateArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckDoubleArrayIndex(double[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckDoubleArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckEnumArrayIndex(E[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckEnumArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckEnumLongArrayIndex(El[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckEnumLongArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckFloatArrayIndex(float[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckFloatArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckFuncArrayIndex(Func<object>[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckFuncArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckInterfaceArrayIndex(I[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckInterfaceArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckIEquatableCustomArrayIndex(IEquatable<C>[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckIEquatableCustomArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckIEquatableCustom2ArrayIndex(IEquatable<D>[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckIEquatableCustom2ArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckIntArrayIndex(int[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckIntArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckLongArrayIndex(long[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckLongArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckObjectArrayIndex(object[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckObjectArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckStructArrayIndex(S[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckSByteArrayIndex(sbyte[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckSByteArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckStructWithStringArrayIndex(Sc[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithStringArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckStructWithValueAndStringArrayIndex(Scs[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithValueAndStringArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckShortArrayIndex(short[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckShortArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckStructWithParametersArrayIndex(Sp[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithParametersArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckStructWithStructArrayIndex(Ss[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithStructArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckStringArrayIndex(string[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStringArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckUIntArrayIndex(uint[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckUIntArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckULongArrayIndex(ulong[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckULongArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckUShortArrayIndex(ushort[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckUShortArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckGenericArrayIndex<T>(T[] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericArrayIndexExpression<T>(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithClassRestrictionArrayIndex<Tc>(Tc[] array, bool useInterpreter) where Tc : class
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithClassRestrictionArrayIndexExpression<Tc>(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithSubClassRestrictionArrayIndex<Tc>(Tc[] array, bool useInterpreter) where Tc : C
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithSubClassRestrictionArrayIndexExpression<Tc>(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithNewClassRestrictionArrayIndex<Tcn>(Tcn[] array, bool useInterpreter) where Tcn : class, new()
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithNewClassRestrictionArrayIndexExpression<Tcn>(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithSubClassNewRestrictionArrayIndex<Tcn>(Tcn[] array, bool useInterpreter) where Tcn : C, new()
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithSubClassNewRestrictionArrayIndexExpression<Tcn>(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithStructRestrictionArrayIndex<Ts>(Ts[] array, bool useInterpreter) where Ts : struct
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithStructRestrictionArrayIndexExpression<Ts>(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        #endregion

        #region Check index expression

        private static bool CheckBoolArrayIndexExpression(bool[] array, int index, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(bool[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckByteArrayIndexExpression(byte[] array, int index, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(byte[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckCustomArrayIndexExpression(C[] array, int index, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(C[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckCharArrayIndexExpression(char[] array, int index, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(char[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckCustom2ArrayIndexExpression(D[] array, int index, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(D[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckDecimalArrayIndexExpression(decimal[] array, int index, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(decimal[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckDelegateArrayIndexExpression(Delegate[] array, int index, bool useInterpreter)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Delegate[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckDoubleArrayIndexExpression(double[] array, int index, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(double[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckEnumArrayIndexExpression(E[] array, int index, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(E[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckEnumLongArrayIndexExpression(El[] array, int index, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(El[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckFloatArrayIndexExpression(float[] array, int index, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(float[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckFuncArrayIndexExpression(Func<object>[] array, int index, bool useInterpreter)
        {
            Expression<Func<Func<object>>> e =
                Expression.Lambda<Func<Func<object>>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Func<object>[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckInterfaceArrayIndexExpression(I[] array, int index, bool useInterpreter)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(I[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckIEquatableCustomArrayIndexExpression(IEquatable<C>[] array, int index, bool useInterpreter)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(IEquatable<C>[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckIEquatableCustom2ArrayIndexExpression(IEquatable<D>[] array, int index, bool useInterpreter)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(IEquatable<D>[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckIntArrayIndexExpression(int[] array, int index, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(int[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckLongArrayIndexExpression(long[] array, int index, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(long[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckObjectArrayIndexExpression(object[] array, int index, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(object[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructArrayIndexExpression(S[] array, int index, bool useInterpreter)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(S[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckSByteArrayIndexExpression(sbyte[] array, int index, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(sbyte[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithStringArrayIndexExpression(Sc[] array, int index, bool useInterpreter)
        {
            Expression<Func<Sc>> e =
                Expression.Lambda<Func<Sc>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Sc[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithValueAndStringArrayIndexExpression(Scs[] array, int index, bool useInterpreter)
        {
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Scs[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckShortArrayIndexExpression(short[] array, int index, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(short[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithParametersArrayIndexExpression(Sp[] array, int index, bool useInterpreter)
        {
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Sp[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithStructArrayIndexExpression(Ss[] array, int index, bool useInterpreter)
        {
            Expression<Func<Ss>> e =
                Expression.Lambda<Func<Ss>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Ss[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStringArrayIndexExpression(string[] array, int index, bool useInterpreter)
        {
            Expression<Func<string>> e =
                Expression.Lambda<Func<string>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(string[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckUIntArrayIndexExpression(uint[] array, int index, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(uint[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckULongArrayIndexExpression(ulong[] array, int index, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(ulong[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckUShortArrayIndexExpression(ushort[] array, int index, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(ushort[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericArrayIndexExpression<T>(T[] array, int index, bool useInterpreter)
        {
            Expression<Func<T>> e =
                Expression.Lambda<Func<T>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(T[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithClassRestrictionArrayIndexExpression<Tc>(Tc[] array, int index, bool useInterpreter) where Tc : class
        {
            Expression<Func<Tc>> e =
                Expression.Lambda<Func<Tc>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Tc[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithSubClassRestrictionArrayIndexExpression<Tc>(Tc[] array, int index, bool useInterpreter) where Tc : C
        {
            Expression<Func<Tc>> e =
                Expression.Lambda<Func<Tc>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Tc[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithNewClassRestrictionArrayIndexExpression<Tcn>(Tcn[] array, int index, bool useInterpreter) where Tcn : class, new()
        {
            Expression<Func<Tcn>> e =
                Expression.Lambda<Func<Tcn>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Tcn[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithSubClassNewRestrictionArrayIndexExpression<Tcn>(Tcn[] array, int index, bool useInterpreter) where Tcn : C, new()
        {
            Expression<Func<Tcn>> e =
                Expression.Lambda<Func<Tcn>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Tcn[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithStructRestrictionArrayIndexExpression<Ts>(Ts[] array, int index, bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Ts[])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        #endregion

        #region Check exception array index

        private static void CheckExceptionBoolArrayIndex(bool[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckBoolArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckBoolArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionByteArrayIndex(byte[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckByteArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckByteArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionCustomArrayIndex(C[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckCustomArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckCustomArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionCharArrayIndex(char[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckCharArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckCharArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionCustom2ArrayIndex(D[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckCustom2ArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckCustom2ArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionDecimalArrayIndex(decimal[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckDecimalArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckDecimalArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionDelegateArrayIndex(Delegate[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckDelegateArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckDelegateArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionDoubleArrayIndex(double[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckDoubleArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckDoubleArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionEnumArrayIndex(E[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckEnumArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckEnumArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionEnumLongArrayIndex(El[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckEnumLongArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckEnumLongArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionFloatArrayIndex(float[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckFloatArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckFloatArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionFuncArrayIndex(Func<object>[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckFuncArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckFuncArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionInterfaceArrayIndex(I[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckInterfaceArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckInterfaceArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionIEquatableCustomArrayIndex(IEquatable<C>[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckIEquatableCustomArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckIEquatableCustomArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionIEquatableCustom2ArrayIndex(IEquatable<D>[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckIEquatableCustom2ArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckIEquatableCustom2ArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionIntArrayIndex(int[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckIntArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckIntArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionLongArrayIndex(long[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckLongArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckLongArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionObjectArrayIndex(object[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckObjectArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckObjectArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionStructArrayIndex(S[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionSByteArrayIndex(sbyte[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckSByteArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckSByteArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionStructWithStringArrayIndex(Sc[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructWithStringArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructWithStringArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionStructWithValueAndStringArrayIndex(Scs[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructWithValueAndStringArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructWithValueAndStringArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionShortArrayIndex(short[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckShortArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckShortArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionStructWithParametersArrayIndex(Sp[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructWithParametersArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructWithParametersArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionStructWithStructArrayIndex(Ss[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructWithStructArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructWithStructArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionStringArrayIndex(string[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStringArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStringArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionUIntArrayIndex(uint[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckUIntArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckUIntArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionULongArrayIndex(ulong[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckULongArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckULongArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionUShortArrayIndex(ushort[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckUShortArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckUShortArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionGenericArrayIndex<T>(T[] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionGenericWithClassRestrictionArrayIndex<Tc>(Tc[] array, int index, bool useInterpreter) where Tc : class
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithClassRestrictionArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithClassRestrictionArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionGenericWithSubClassRestrictionArrayIndex<Tc>(Tc[] array, int index, bool useInterpreter) where Tc : C
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithSubClassRestrictionArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithSubClassRestrictionArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionGenericWithNewClassRestrictionArrayIndex<Tcn>(Tcn[] array, int index, bool useInterpreter) where Tcn : class, new()
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithNewClassRestrictionArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithNewClassRestrictionArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionGenericWithSubClassNewRestrictionArrayIndex<Tcn>(Tcn[] array, int index, bool useInterpreter) where Tcn : C, new()
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithSubClassNewRestrictionArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithSubClassNewRestrictionArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionGenericWithStructRestrictionArrayIndex<Ts>(Ts[] array, int index, bool useInterpreter) where Ts : struct
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithStructRestrictionArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithStructRestrictionArrayIndexExpression(array, index, useInterpreter));
        }

        #endregion

        #region ToString

        [Fact]
        public static void ToStringTest()
        {
            BinaryExpression e = Expression.ArrayIndex(Expression.Parameter(typeof(int[]), "xs"), Expression.Parameter(typeof(int), "i"));
            Assert.Equal("xs[i]", e.ToString());
        }

        #endregion

        [Fact]
        public static void ArrayIndexNotArray()
        {
            Expression intExp = Expression.Constant(1);
            AssertExtensions.Throws<ArgumentException>("array", () => Expression.ArrayIndex(intExp, intExp));
        }

        [Fact]
        public static void ArrayIndexNullArray()
        {
            AssertExtensions.Throws<ArgumentNullException>("array", () => Expression.ArrayIndex(null, Expression.Constant(0)));
        }

        [Fact]
        public static void ArrayIndexNullIndices()
        {
            Expression array = Expression.Constant(new[] {1, 2});
            AssertExtensions.Throws<ArgumentNullException>("index", () => Expression.ArrayIndex(array, default(Expression)));
        }

        [Fact]
        public static void ArrayIndexWrongRank()
        {
            Expression array = Expression.Constant(new[,] { { 1, 2 }, { 2, 1 } });
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.ArrayIndex(array, Expression.Constant(2)));
        }

        [Fact]
        public static void ArrayIndexWrongType()
        {
            Expression array = Expression.Constant(new[] {1, 2});
            AssertExtensions.Throws<ArgumentException>("index", () => Expression.ArrayIndex(array, Expression.Constant(2L)));
        }

        [Fact]
        public static void UnreadableArray()
        {
            Expression array = Expression.Property(null, typeof(Unreadable<int[]>), nameof(Unreadable<int>.WriteOnly));
            AssertExtensions.Throws<ArgumentException>("array", () => Expression.ArrayIndex(array, Expression.Constant(0)));
        }

        [Fact]
        public static void UnreadableIndex()
        {
            Expression array = Expression.Constant(new[]  { 1, 2 });
            Expression index = Expression.Property(null, typeof(Unreadable<int>), nameof(Unreadable<int>.WriteOnly));
            AssertExtensions.Throws<ArgumentException>("index", () => Expression.ArrayIndex(array, index));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        [ActiveIssue("https://github.com/mono/mono/issues/14930", TestRuntimes.Mono)]
        [ClassData(typeof(CompilationTypes))]
        public static void NonZeroBasedOneDimensionalArrayIndex(bool useInterpreter)
        {
            Array arrayObj = Array.CreateInstance(typeof(int), new[] { 3 }, new[] { -1 });
            arrayObj.SetValue(5, -1);
            arrayObj.SetValue(6, 0);
            arrayObj.SetValue(7, 1);
            ConstantExpression array = Expression.Constant(arrayObj);
            BinaryExpression indexM1 = Expression.ArrayIndex(array, Expression.Constant(-1));
            BinaryExpression index0 = Expression.ArrayIndex(array, Expression.Constant(0));
            BinaryExpression index1 = Expression.ArrayIndex(array, Expression.Constant(1));
            Func<bool> testValues = Expression.Lambda<Func<bool>>(
                Expression.And(
                    Expression.Equal(indexM1, Expression.Constant(5)),
                    Expression.And(
                        Expression.Equal(index0, Expression.Constant(6)),
                        Expression.Equal(index1, Expression.Constant(7))))).Compile(useInterpreter);
            Assert.True(testValues());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        [ActiveIssue("https://github.com/mono/mono/issues/14930", TestRuntimes.Mono)]
        [ClassData(typeof(CompilationTypes))]
        public static void NonZeroBasedOneDimensionalArrayIndexMethod(bool useInterpreter)
        {
            Array arrayObj = Array.CreateInstance(typeof(int), new[] { 3 }, new[] { -1 });
            arrayObj.SetValue(5, -1);
            arrayObj.SetValue(6, 0);
            arrayObj.SetValue(7, 1);
            ConstantExpression array = Expression.Constant(arrayObj);
            MethodCallExpression indexM1 = Expression.ArrayIndex(array, new [] { Expression.Constant(-1)});
            MethodCallExpression index0 = Expression.ArrayIndex(array, new[] { Expression.Constant(0)});
            MethodCallExpression index1 = Expression.ArrayIndex(array, new[] { Expression.Constant(1)});
            Func<bool> testValues = Expression.Lambda<Func<bool>>(
                Expression.And(
                    Expression.Equal(indexM1, Expression.Constant(5)),
                    Expression.And(
                        Expression.Equal(index0, Expression.Constant(6)),
                        Expression.Equal(index1, Expression.Constant(7))))).Compile(useInterpreter);
            Assert.True(testValues());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void HighRankArrayIndex(bool useInterpreter)
        {
            string[,,,,,,,,,] arrayObj = {{{{{{{{{{"hugz"}}}}}}}}}};
            ConstantExpression array = Expression.Constant(arrayObj);
            Func<string> func = Expression.Lambda<Func<string>>(
                Expression.ArrayIndex(array, Enumerable.Repeat(Expression.Constant(0), 10))).Compile(useInterpreter);
            Assert.Equal("hugz", func());
        }


    }
}
