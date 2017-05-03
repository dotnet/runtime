// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ArrayArrayIndexTests
    {
        #region Boolean tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayArrayIndexTest(bool useInterpreter)
        {
            CheckBoolArrayArrayIndex(GenerateBoolArrayArray(0), useInterpreter);
            CheckBoolArrayArrayIndex(GenerateBoolArrayArray(1), useInterpreter);
            CheckBoolArrayArrayIndex(GenerateBoolArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionBoolArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionBoolArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionBoolArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionBoolArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionBoolArrayArrayIndex(GenerateBoolArrayArray(0), -1, useInterpreter);
            CheckExceptionBoolArrayArrayIndex(GenerateBoolArrayArray(0), 0, useInterpreter);
            CheckExceptionBoolArrayArrayIndex(GenerateBoolArrayArray(1), -1, useInterpreter);
            CheckExceptionBoolArrayArrayIndex(GenerateBoolArrayArray(1), 1, useInterpreter);
            CheckExceptionBoolArrayArrayIndex(GenerateBoolArrayArray(5), -1, useInterpreter);
            CheckExceptionBoolArrayArrayIndex(GenerateBoolArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Byte tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayArrayIndexTest(bool useInterpreter)
        {
            CheckByteArrayArrayIndex(GenerateByteArrayArray(0), useInterpreter);
            CheckByteArrayArrayIndex(GenerateByteArrayArray(1), useInterpreter);
            CheckByteArrayArrayIndex(GenerateByteArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionByteArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionByteArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionByteArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionByteArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionByteArrayArrayIndex(GenerateByteArrayArray(0), -1, useInterpreter);
            CheckExceptionByteArrayArrayIndex(GenerateByteArrayArray(0), 0, useInterpreter);
            CheckExceptionByteArrayArrayIndex(GenerateByteArrayArray(1), -1, useInterpreter);
            CheckExceptionByteArrayArrayIndex(GenerateByteArrayArray(1), 1, useInterpreter);
            CheckExceptionByteArrayArrayIndex(GenerateByteArrayArray(5), -1, useInterpreter);
            CheckExceptionByteArrayArrayIndex(GenerateByteArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Custom tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayArrayIndexTest(bool useInterpreter)
        {
            CheckCustomArrayArrayIndex(GenerateCustomArrayArray(0), useInterpreter);
            CheckCustomArrayArrayIndex(GenerateCustomArrayArray(1), useInterpreter);
            CheckCustomArrayArrayIndex(GenerateCustomArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionCustomArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionCustomArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionCustomArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionCustomArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionCustomArrayArrayIndex(GenerateCustomArrayArray(0), -1, useInterpreter);
            CheckExceptionCustomArrayArrayIndex(GenerateCustomArrayArray(0), 0, useInterpreter);
            CheckExceptionCustomArrayArrayIndex(GenerateCustomArrayArray(1), -1, useInterpreter);
            CheckExceptionCustomArrayArrayIndex(GenerateCustomArrayArray(1), 1, useInterpreter);
            CheckExceptionCustomArrayArrayIndex(GenerateCustomArrayArray(5), -1, useInterpreter);
            CheckExceptionCustomArrayArrayIndex(GenerateCustomArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Char tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayArrayIndexTest(bool useInterpreter)
        {
            CheckCharArrayArrayIndex(GenerateCharArrayArray(0), useInterpreter);
            CheckCharArrayArrayIndex(GenerateCharArrayArray(1), useInterpreter);
            CheckCharArrayArrayIndex(GenerateCharArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionCharArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionCharArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionCharArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionCharArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionCharArrayArrayIndex(GenerateCharArrayArray(0), -1, useInterpreter);
            CheckExceptionCharArrayArrayIndex(GenerateCharArrayArray(0), 0, useInterpreter);
            CheckExceptionCharArrayArrayIndex(GenerateCharArrayArray(1), -1, useInterpreter);
            CheckExceptionCharArrayArrayIndex(GenerateCharArrayArray(1), 1, useInterpreter);
            CheckExceptionCharArrayArrayIndex(GenerateCharArrayArray(5), -1, useInterpreter);
            CheckExceptionCharArrayArrayIndex(GenerateCharArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Custom2 tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayArrayIndexTest(bool useInterpreter)
        {
            CheckCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(0), useInterpreter);
            CheckCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(1), useInterpreter);
            CheckCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionCustom2ArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionCustom2ArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionCustom2ArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionCustom2ArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(0), -1, useInterpreter);
            CheckExceptionCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(0), 0, useInterpreter);
            CheckExceptionCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(1), -1, useInterpreter);
            CheckExceptionCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(1), 1, useInterpreter);
            CheckExceptionCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(5), -1, useInterpreter);
            CheckExceptionCustom2ArrayArrayIndex(GenerateCustom2ArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Decimal tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayArrayIndexTest(bool useInterpreter)
        {
            CheckDecimalArrayArrayIndex(GenerateDecimalArrayArray(0), useInterpreter);
            CheckDecimalArrayArrayIndex(GenerateDecimalArrayArray(1), useInterpreter);
            CheckDecimalArrayArrayIndex(GenerateDecimalArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionDecimalArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionDecimalArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionDecimalArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionDecimalArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionDecimalArrayArrayIndex(GenerateDecimalArrayArray(0), -1, useInterpreter);
            CheckExceptionDecimalArrayArrayIndex(GenerateDecimalArrayArray(0), 0, useInterpreter);
            CheckExceptionDecimalArrayArrayIndex(GenerateDecimalArrayArray(1), -1, useInterpreter);
            CheckExceptionDecimalArrayArrayIndex(GenerateDecimalArrayArray(1), 1, useInterpreter);
            CheckExceptionDecimalArrayArrayIndex(GenerateDecimalArrayArray(5), -1, useInterpreter);
            CheckExceptionDecimalArrayArrayIndex(GenerateDecimalArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Delegate tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayArrayIndexTest(bool useInterpreter)
        {
            CheckDelegateArrayArrayIndex(GenerateDelegateArrayArray(0), useInterpreter);
            CheckDelegateArrayArrayIndex(GenerateDelegateArrayArray(1), useInterpreter);
            CheckDelegateArrayArrayIndex(GenerateDelegateArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionDelegateArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionDelegateArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionDelegateArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionDelegateArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionDelegateArrayArrayIndex(GenerateDelegateArrayArray(0), -1, useInterpreter);
            CheckExceptionDelegateArrayArrayIndex(GenerateDelegateArrayArray(0), 0, useInterpreter);
            CheckExceptionDelegateArrayArrayIndex(GenerateDelegateArrayArray(1), -1, useInterpreter);
            CheckExceptionDelegateArrayArrayIndex(GenerateDelegateArrayArray(1), 1, useInterpreter);
            CheckExceptionDelegateArrayArrayIndex(GenerateDelegateArrayArray(5), -1, useInterpreter);
            CheckExceptionDelegateArrayArrayIndex(GenerateDelegateArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region double tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckdoubleArrayArrayIndexTest(bool useInterpreter)
        {
            CheckdoubleArrayArrayIndex(GeneratedoubleArrayArray(0), useInterpreter);
            CheckdoubleArrayArrayIndex(GeneratedoubleArrayArray(1), useInterpreter);
            CheckdoubleArrayArrayIndex(GeneratedoubleArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptiondoubleArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptiondoubleArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptiondoubleArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptiondoubleArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptiondoubleArrayArrayIndex(GeneratedoubleArrayArray(0), -1, useInterpreter);
            CheckExceptiondoubleArrayArrayIndex(GeneratedoubleArrayArray(0), 0, useInterpreter);
            CheckExceptiondoubleArrayArrayIndex(GeneratedoubleArrayArray(1), -1, useInterpreter);
            CheckExceptiondoubleArrayArrayIndex(GeneratedoubleArrayArray(1), 1, useInterpreter);
            CheckExceptiondoubleArrayArrayIndex(GeneratedoubleArrayArray(5), -1, useInterpreter);
            CheckExceptiondoubleArrayArrayIndex(GeneratedoubleArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Enum tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayArrayIndexTest(bool useInterpreter)
        {
            CheckEnumArrayArrayIndex(GenerateEnumArrayArray(0), useInterpreter);
            CheckEnumArrayArrayIndex(GenerateEnumArrayArray(1), useInterpreter);
            CheckEnumArrayArrayIndex(GenerateEnumArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionEnumArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionEnumArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionEnumArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionEnumArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionEnumArrayArrayIndex(GenerateEnumArrayArray(0), -1, useInterpreter);
            CheckExceptionEnumArrayArrayIndex(GenerateEnumArrayArray(0), 0, useInterpreter);
            CheckExceptionEnumArrayArrayIndex(GenerateEnumArrayArray(1), -1, useInterpreter);
            CheckExceptionEnumArrayArrayIndex(GenerateEnumArrayArray(1), 1, useInterpreter);
            CheckExceptionEnumArrayArrayIndex(GenerateEnumArrayArray(5), -1, useInterpreter);
            CheckExceptionEnumArrayArrayIndex(GenerateEnumArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region EnumLong tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckEnumLongArrayArrayIndexTest(bool useInterpreter)
        {
            CheckEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(0), useInterpreter);
            CheckEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(1), useInterpreter);
            CheckEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionEnumLongArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionEnumLongArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionEnumLongArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionEnumLongArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(0), -1, useInterpreter);
            CheckExceptionEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(0), 0, useInterpreter);
            CheckExceptionEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(1), -1, useInterpreter);
            CheckExceptionEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(1), 1, useInterpreter);
            CheckExceptionEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(5), -1, useInterpreter);
            CheckExceptionEnumLongArrayArrayIndex(GenerateEnumLongArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Float tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayArrayIndexTest(bool useInterpreter)
        {
            CheckFloatArrayArrayIndex(GenerateFloatArrayArray(0), useInterpreter);
            CheckFloatArrayArrayIndex(GenerateFloatArrayArray(1), useInterpreter);
            CheckFloatArrayArrayIndex(GenerateFloatArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionFloatArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionFloatArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionFloatArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionFloatArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionFloatArrayArrayIndex(GenerateFloatArrayArray(0), -1, useInterpreter);
            CheckExceptionFloatArrayArrayIndex(GenerateFloatArrayArray(0), 0, useInterpreter);
            CheckExceptionFloatArrayArrayIndex(GenerateFloatArrayArray(1), -1, useInterpreter);
            CheckExceptionFloatArrayArrayIndex(GenerateFloatArrayArray(1), 1, useInterpreter);
            CheckExceptionFloatArrayArrayIndex(GenerateFloatArrayArray(5), -1, useInterpreter);
            CheckExceptionFloatArrayArrayIndex(GenerateFloatArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Func tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayArrayIndexTest(bool useInterpreter)
        {
            CheckFuncArrayArrayIndex(GenerateFuncArrayArray(0), useInterpreter);
            CheckFuncArrayArrayIndex(GenerateFuncArrayArray(1), useInterpreter);
            CheckFuncArrayArrayIndex(GenerateFuncArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionFuncArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionFuncArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionFuncArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionFuncArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionFuncArrayArrayIndex(GenerateFuncArrayArray(0), -1, useInterpreter);
            CheckExceptionFuncArrayArrayIndex(GenerateFuncArrayArray(0), 0, useInterpreter);
            CheckExceptionFuncArrayArrayIndex(GenerateFuncArrayArray(1), -1, useInterpreter);
            CheckExceptionFuncArrayArrayIndex(GenerateFuncArrayArray(1), 1, useInterpreter);
            CheckExceptionFuncArrayArrayIndex(GenerateFuncArrayArray(5), -1, useInterpreter);
            CheckExceptionFuncArrayArrayIndex(GenerateFuncArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Interface tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayArrayIndexTest(bool useInterpreter)
        {
            CheckInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(0), useInterpreter);
            CheckInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(1), useInterpreter);
            CheckInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionInterfaceArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionInterfaceArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionInterfaceArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionInterfaceArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(0), -1, useInterpreter);
            CheckExceptionInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(0), 0, useInterpreter);
            CheckExceptionInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(1), -1, useInterpreter);
            CheckExceptionInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(1), 1, useInterpreter);
            CheckExceptionInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(5), -1, useInterpreter);
            CheckExceptionInterfaceArrayArrayIndex(GenerateInterfaceArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region IEquatable tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableArrayArrayIndexTest(bool useInterpreter)
        {
            CheckIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(0), useInterpreter);
            CheckIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(1), useInterpreter);
            CheckIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionIEquatableArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionIEquatableArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionIEquatableArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionIEquatableArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(0), -1, useInterpreter);
            CheckExceptionIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(0), 0, useInterpreter);
            CheckExceptionIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(1), -1, useInterpreter);
            CheckExceptionIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(1), 1, useInterpreter);
            CheckExceptionIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(5), -1, useInterpreter);
            CheckExceptionIEquatableArrayArrayIndex(GenerateIEquatableArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region IEquatable2 tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatable2ArrayArrayIndexTest(bool useInterpreter)
        {
            CheckIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(0), useInterpreter);
            CheckIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(1), useInterpreter);
            CheckIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionIEquatable2ArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionIEquatable2ArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionIEquatable2ArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionIEquatable2ArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(0), -1, useInterpreter);
            CheckExceptionIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(0), 0, useInterpreter);
            CheckExceptionIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(1), -1, useInterpreter);
            CheckExceptionIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(1), 1, useInterpreter);
            CheckExceptionIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(5), -1, useInterpreter);
            CheckExceptionIEquatable2ArrayArrayIndex(GenerateIEquatable2ArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Int tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayArrayIndexTest(bool useInterpreter)
        {
            CheckIntArrayArrayIndex(GenerateIntArrayArray(0), useInterpreter);
            CheckIntArrayArrayIndex(GenerateIntArrayArray(1), useInterpreter);
            CheckIntArrayArrayIndex(GenerateIntArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionIntArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionIntArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionIntArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionIntArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionIntArrayArrayIndex(GenerateIntArrayArray(0), -1, useInterpreter);
            CheckExceptionIntArrayArrayIndex(GenerateIntArrayArray(0), 0, useInterpreter);
            CheckExceptionIntArrayArrayIndex(GenerateIntArrayArray(1), -1, useInterpreter);
            CheckExceptionIntArrayArrayIndex(GenerateIntArrayArray(1), 1, useInterpreter);
            CheckExceptionIntArrayArrayIndex(GenerateIntArrayArray(5), -1, useInterpreter);
            CheckExceptionIntArrayArrayIndex(GenerateIntArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Long tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayArrayIndexTest(bool useInterpreter)
        {
            CheckLongArrayArrayIndex(GenerateLongArrayArray(0), useInterpreter);
            CheckLongArrayArrayIndex(GenerateLongArrayArray(1), useInterpreter);
            CheckLongArrayArrayIndex(GenerateLongArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionLongArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionLongArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionLongArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionLongArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionLongArrayArrayIndex(GenerateLongArrayArray(0), -1, useInterpreter);
            CheckExceptionLongArrayArrayIndex(GenerateLongArrayArray(0), 0, useInterpreter);
            CheckExceptionLongArrayArrayIndex(GenerateLongArrayArray(1), -1, useInterpreter);
            CheckExceptionLongArrayArrayIndex(GenerateLongArrayArray(1), 1, useInterpreter);
            CheckExceptionLongArrayArrayIndex(GenerateLongArrayArray(5), -1, useInterpreter);
            CheckExceptionLongArrayArrayIndex(GenerateLongArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Object tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayArrayIndexTest(bool useInterpreter)
        {
            CheckObjectArrayArrayIndex(GenerateObjectArrayArray(0), useInterpreter);
            CheckObjectArrayArrayIndex(GenerateObjectArrayArray(1), useInterpreter);
            CheckObjectArrayArrayIndex(GenerateObjectArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionObjectArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionObjectArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionObjectArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionObjectArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionObjectArrayArrayIndex(GenerateObjectArrayArray(0), -1, useInterpreter);
            CheckExceptionObjectArrayArrayIndex(GenerateObjectArrayArray(0), 0, useInterpreter);
            CheckExceptionObjectArrayArrayIndex(GenerateObjectArrayArray(1), -1, useInterpreter);
            CheckExceptionObjectArrayArrayIndex(GenerateObjectArrayArray(1), 1, useInterpreter);
            CheckExceptionObjectArrayArrayIndex(GenerateObjectArrayArray(5), -1, useInterpreter);
            CheckExceptionObjectArrayArrayIndex(GenerateObjectArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Struct tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayArrayIndexTest(bool useInterpreter)
        {
            CheckStructArrayArrayIndex(GenerateStructArrayArray(0), useInterpreter);
            CheckStructArrayArrayIndex(GenerateStructArrayArray(1), useInterpreter);
            CheckStructArrayArrayIndex(GenerateStructArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionStructArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionStructArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionStructArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionStructArrayArrayIndex(GenerateStructArrayArray(0), -1, useInterpreter);
            CheckExceptionStructArrayArrayIndex(GenerateStructArrayArray(0), 0, useInterpreter);
            CheckExceptionStructArrayArrayIndex(GenerateStructArrayArray(1), -1, useInterpreter);
            CheckExceptionStructArrayArrayIndex(GenerateStructArrayArray(1), 1, useInterpreter);
            CheckExceptionStructArrayArrayIndex(GenerateStructArrayArray(5), -1, useInterpreter);
            CheckExceptionStructArrayArrayIndex(GenerateStructArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region SByte tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayArrayIndexTest(bool useInterpreter)
        {
            CheckSByteArrayArrayIndex(GenerateSByteArrayArray(0), useInterpreter);
            CheckSByteArrayArrayIndex(GenerateSByteArrayArray(1), useInterpreter);
            CheckSByteArrayArrayIndex(GenerateSByteArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionSByteArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionSByteArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionSByteArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionSByteArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionSByteArrayArrayIndex(GenerateSByteArrayArray(0), -1, useInterpreter);
            CheckExceptionSByteArrayArrayIndex(GenerateSByteArrayArray(0), 0, useInterpreter);
            CheckExceptionSByteArrayArrayIndex(GenerateSByteArrayArray(1), -1, useInterpreter);
            CheckExceptionSByteArrayArrayIndex(GenerateSByteArrayArray(1), 1, useInterpreter);
            CheckExceptionSByteArrayArrayIndex(GenerateSByteArrayArray(5), -1, useInterpreter);
            CheckExceptionSByteArrayArrayIndex(GenerateSByteArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region StructWithString tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayArrayIndexTest(bool useInterpreter)
        {
            CheckStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(0), useInterpreter);
            CheckStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(1), useInterpreter);
            CheckStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithStringArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionStructWithStringArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionStructWithStringArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionStructWithStringArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(0), -1, useInterpreter);
            CheckExceptionStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(0), 0, useInterpreter);
            CheckExceptionStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(1), -1, useInterpreter);
            CheckExceptionStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(1), 1, useInterpreter);
            CheckExceptionStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(5), -1, useInterpreter);
            CheckExceptionStructWithStringArrayArrayIndex(GenerateStructWithStringArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region StructWithStringAndStruct tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndStructArrayArrayIndexTest(bool useInterpreter)
        {
            CheckStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(0), useInterpreter);
            CheckStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(1), useInterpreter);
            CheckStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithStringAndStructArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionStructWithStringAndStructArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(0), -1, useInterpreter);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(0), 0, useInterpreter);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(1), -1, useInterpreter);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(1), 1, useInterpreter);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(5), -1, useInterpreter);
            CheckExceptionStructWithStringAndStructArrayArrayIndex(GenerateStructWithStringAndStructArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Short tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayArrayIndexTest(bool useInterpreter)
        {
            CheckShortArrayArrayIndex(GenerateShortArrayArray(0), useInterpreter);
            CheckShortArrayArrayIndex(GenerateShortArrayArray(1), useInterpreter);
            CheckShortArrayArrayIndex(GenerateShortArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionShortArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionShortArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionShortArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionShortArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionShortArrayArrayIndex(GenerateShortArrayArray(0), -1, useInterpreter);
            CheckExceptionShortArrayArrayIndex(GenerateShortArrayArray(0), 0, useInterpreter);
            CheckExceptionShortArrayArrayIndex(GenerateShortArrayArray(1), -1, useInterpreter);
            CheckExceptionShortArrayArrayIndex(GenerateShortArrayArray(1), 1, useInterpreter);
            CheckExceptionShortArrayArrayIndex(GenerateShortArrayArray(5), -1, useInterpreter);
            CheckExceptionShortArrayArrayIndex(GenerateShortArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region StructWithTwoFields tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoFieldsArrayArrayIndexTest(bool useInterpreter)
        {
            CheckStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(0), useInterpreter);
            CheckStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(1), useInterpreter);
            CheckStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithTwoFieldsArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(0), -1, useInterpreter);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(0), 0, useInterpreter);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(1), -1, useInterpreter);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(1), 1, useInterpreter);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(5), -1, useInterpreter);
            CheckExceptionStructWithTwoFieldsArrayArrayIndex(GenerateStructWithTwoFieldsArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region StructWithValue tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayArrayIndexTest(bool useInterpreter)
        {
            CheckStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(0), useInterpreter);
            CheckStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(1), useInterpreter);
            CheckStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithValueArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionStructWithValueArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionStructWithValueArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionStructWithValueArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(0), -1, useInterpreter);
            CheckExceptionStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(0), 0, useInterpreter);
            CheckExceptionStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(1), -1, useInterpreter);
            CheckExceptionStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(1), 1, useInterpreter);
            CheckExceptionStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(5), -1, useInterpreter);
            CheckExceptionStructWithValueArrayArrayIndex(GenerateStructWithValueArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region String tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayArrayIndexTest(bool useInterpreter)
        {
            CheckStringArrayArrayIndex(GenerateStringArrayArray(0), useInterpreter);
            CheckStringArrayArrayIndex(GenerateStringArrayArray(1), useInterpreter);
            CheckStringArrayArrayIndex(GenerateStringArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStringArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionStringArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionStringArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionStringArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionStringArrayArrayIndex(GenerateStringArrayArray(0), -1, useInterpreter);
            CheckExceptionStringArrayArrayIndex(GenerateStringArrayArray(0), 0, useInterpreter);
            CheckExceptionStringArrayArrayIndex(GenerateStringArrayArray(1), -1, useInterpreter);
            CheckExceptionStringArrayArrayIndex(GenerateStringArrayArray(1), 1, useInterpreter);
            CheckExceptionStringArrayArrayIndex(GenerateStringArrayArray(5), -1, useInterpreter);
            CheckExceptionStringArrayArrayIndex(GenerateStringArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region UInt tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayArrayIndexTest(bool useInterpreter)
        {
            CheckUIntArrayArrayIndex(GenerateUIntArrayArray(0), useInterpreter);
            CheckUIntArrayArrayIndex(GenerateUIntArrayArray(1), useInterpreter);
            CheckUIntArrayArrayIndex(GenerateUIntArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionUIntArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionUIntArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionUIntArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionUIntArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionUIntArrayArrayIndex(GenerateUIntArrayArray(0), -1, useInterpreter);
            CheckExceptionUIntArrayArrayIndex(GenerateUIntArrayArray(0), 0, useInterpreter);
            CheckExceptionUIntArrayArrayIndex(GenerateUIntArrayArray(1), -1, useInterpreter);
            CheckExceptionUIntArrayArrayIndex(GenerateUIntArrayArray(1), 1, useInterpreter);
            CheckExceptionUIntArrayArrayIndex(GenerateUIntArrayArray(5), -1, useInterpreter);
            CheckExceptionUIntArrayArrayIndex(GenerateUIntArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region ULong tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayArrayIndexTest(bool useInterpreter)
        {
            CheckULongArrayArrayIndex(GenerateULongArrayArray(0), useInterpreter);
            CheckULongArrayArrayIndex(GenerateULongArrayArray(1), useInterpreter);
            CheckULongArrayArrayIndex(GenerateULongArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionULongArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionULongArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionULongArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionULongArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionULongArrayArrayIndex(GenerateULongArrayArray(0), -1, useInterpreter);
            CheckExceptionULongArrayArrayIndex(GenerateULongArrayArray(0), 0, useInterpreter);
            CheckExceptionULongArrayArrayIndex(GenerateULongArrayArray(1), -1, useInterpreter);
            CheckExceptionULongArrayArrayIndex(GenerateULongArrayArray(1), 1, useInterpreter);
            CheckExceptionULongArrayArrayIndex(GenerateULongArrayArray(5), -1, useInterpreter);
            CheckExceptionULongArrayArrayIndex(GenerateULongArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region UShort tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayArrayIndexTest(bool useInterpreter)
        {
            CheckUShortArrayArrayIndex(GenerateUShortArrayArray(0), useInterpreter);
            CheckUShortArrayArrayIndex(GenerateUShortArrayArray(1), useInterpreter);
            CheckUShortArrayArrayIndex(GenerateUShortArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionUShortArrayArrayIndexTest(bool useInterpreter)
        {
            // null arrays
            CheckExceptionUShortArrayArrayIndex(null, -1, useInterpreter);
            CheckExceptionUShortArrayArrayIndex(null, 0, useInterpreter);
            CheckExceptionUShortArrayArrayIndex(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionUShortArrayArrayIndex(GenerateUShortArrayArray(0), -1, useInterpreter);
            CheckExceptionUShortArrayArrayIndex(GenerateUShortArrayArray(0), 0, useInterpreter);
            CheckExceptionUShortArrayArrayIndex(GenerateUShortArrayArray(1), -1, useInterpreter);
            CheckExceptionUShortArrayArrayIndex(GenerateUShortArrayArray(1), 1, useInterpreter);
            CheckExceptionUShortArrayArrayIndex(GenerateUShortArrayArray(5), -1, useInterpreter);
            CheckExceptionUShortArrayArrayIndex(GenerateUShortArrayArray(5), 5, useInterpreter);
        }

        #endregion

        #region Generic tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomArrayArrayIndexTest(bool useInterpreter)
        {
            CheckGenericArrayArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomArrayArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericEnumArrayArrayIndexTest(bool useInterpreter)
        {
            CheckGenericArrayArrayIndexTestHelper<E>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericEnumArrayArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayArrayIndexTestHelper<E>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericObjectArrayArrayIndexTest(bool useInterpreter)
        {
            CheckGenericArrayArrayIndexTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericObjectArrayArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayArrayIndexTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructArrayArrayIndexTest(bool useInterpreter)
        {
            CheckGenericArrayArrayIndexTestHelper<S>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericStructArrayArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayArrayIndexTestHelper<S>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStringAndFieldArrayArrayIndexTest(bool useInterpreter)
        {
            CheckGenericArrayArrayIndexTestHelper<Scs>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericStructWithStringAndFieldArrayArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayArrayIndexTestHelper<Scs>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithClassRestrictionArrayArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionArrayArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomWithClassRestrictionArrayArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassRestrictionArrayArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericObjectWithClassRestrictionArrayArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionArrayArrayIndexTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericObjectWithClassRestrictionArrayArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassRestrictionArrayArrayIndexTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithClassAndNewRestrictionArrayArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithClassAndNewRestrictionArrayArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomWithClassAndNewRestrictionArrayArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericObjectWithClassAndNewRestrictionArrayArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithClassAndNewRestrictionArrayArrayIndexTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericObjectWithClassAndNewRestrictionArrayArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndexTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithSubClassRestrictionArrayArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithSubClassRestrictionArrayArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomWithSubClassRestrictionArrayArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithSubClassAndNewRestrictionArrayArrayIndexTest(bool useInterpreter)
        {
            CheckGenericWithSubClassAndNewRestrictionArrayArrayIndexTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomWithSubClassAndNewRestrictionArrayArrayIndexTest(bool useInterpreter)
        {
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndexTestHelper<C>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        public static void CheckGenericArrayArrayIndexTestHelper<T>(bool useInterpreter)
        {
            CheckGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(0), useInterpreter);
            CheckGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(1), useInterpreter);
            CheckGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(5), useInterpreter);
        }

        public static void CheckExceptionGenericArrayArrayIndexTestHelper<T>(bool useInterpreter)
        {
            // null arrays
            CheckExceptionGenericArrayArrayIndex<T>(null, -1, useInterpreter);
            CheckExceptionGenericArrayArrayIndex<T>(null, 0, useInterpreter);
            CheckExceptionGenericArrayArrayIndex<T>(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(0), -1, useInterpreter);
            CheckExceptionGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(0), 0, useInterpreter);
            CheckExceptionGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(1), -1, useInterpreter);
            CheckExceptionGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(1), 1, useInterpreter);
            CheckExceptionGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(5), -1, useInterpreter);
            CheckExceptionGenericArrayArrayIndex<T>(GenerateGenericArrayArray<T>(5), 5, useInterpreter);
        }

        public static void CheckGenericWithClassRestrictionArrayArrayIndexTestHelper<Tc>(bool useInterpreter) where Tc : class
        {
            CheckGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(0), useInterpreter);
            CheckGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(1), useInterpreter);
            CheckGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithClassRestrictionArrayArrayIndexTestHelper<Tc>(bool useInterpreter) where Tc : class
        {
            // null arrays
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(null, -1, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(null, 0, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(0), -1, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(0), 0, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(1), -1, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(1), 1, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(5), -1, useInterpreter);
            CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(5), 5, useInterpreter);
        }

        public static void CheckGenericWithClassAndNewRestrictionArrayArrayIndexTestHelper<Tcn>(bool useInterpreter) where Tcn : class, new()
        {
            CheckGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(0), useInterpreter);
            CheckGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(1), useInterpreter);
            CheckGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndexTestHelper<Tcn>(bool useInterpreter) where Tcn : class, new()
        {
            // null arrays
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(null, -1, useInterpreter);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(null, 0, useInterpreter);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(0), -1, useInterpreter);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(0), 0, useInterpreter);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(1), -1, useInterpreter);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(1), 1, useInterpreter);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(5), -1, useInterpreter);
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(5), 5, useInterpreter);
        }

        public static void CheckGenericWithSubClassRestrictionArrayArrayIndexTestHelper<TC>(bool useInterpreter) where TC : C
        {
            CheckGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(0), useInterpreter);
            CheckGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(1), useInterpreter);
            CheckGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithSubClassRestrictionArrayArrayIndexTestHelper<TC>(bool useInterpreter) where TC : C
        {
            // null arrays
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(null, -1, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(null, 0, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(0), -1, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(0), 0, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(1), -1, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(1), 1, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(5), -1, useInterpreter);
            CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(5), 5, useInterpreter);
        }

        public static void CheckGenericWithSubClassAndNewRestrictionArrayArrayIndexTestHelper<TCn>(bool useInterpreter) where TCn : C, new()
        {
            CheckGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(0), useInterpreter);
            CheckGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(1), useInterpreter);
            CheckGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndexTestHelper<TCn>(bool useInterpreter) where TCn : C, new()
        {
            // null arrays
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(null, -1, useInterpreter);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(null, 0, useInterpreter);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(null, 1, useInterpreter);

            // index out of bounds
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(0), -1, useInterpreter);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(0), 0, useInterpreter);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(1), -1, useInterpreter);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(1), 1, useInterpreter);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(5), -1, useInterpreter);
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(5), 5, useInterpreter);
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

        private static void CheckBoolArrayArrayIndex(bool[][] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckBoolArrayArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckByteArrayArrayIndex(byte[][] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckByteArrayArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckCustomArrayArrayIndex(C[][] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckCustomArrayArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckCharArrayArrayIndex(char[][] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckCharArrayArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckCustom2ArrayArrayIndex(D[][] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckCustom2ArrayArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckDecimalArrayArrayIndex(decimal[][] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckDecimalArrayArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckDelegateArrayArrayIndex(Delegate[][] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckDelegateArrayArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckdoubleArrayArrayIndex(double[][] array, bool useInterpreter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckDoubleArrayArrayIndexExpression(array, i, useInterpreter);
            }

            Assert.True(success);
        }

        private static void CheckEnumArrayArrayIndex(E[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckEnumArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckEnumLongArrayArrayIndex(El[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckEnumLongArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckFloatArrayArrayIndex(float[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckFloatArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckFuncArrayArrayIndex(Func<object>[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckFuncArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckInterfaceArrayArrayIndex(I[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckInterfaceArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckIEquatableArrayArrayIndex(IEquatable<C>[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckIEquatableArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckIEquatable2ArrayArrayIndex(IEquatable<D>[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckIEquatable2ArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckIntArrayArrayIndex(int[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckIntArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckLongArrayArrayIndex(long[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckLongArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckObjectArrayArrayIndex(object[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckObjectArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckStructArrayArrayIndex(S[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckSByteArrayArrayIndex(sbyte[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckSByteArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckStructWithStringArrayArrayIndex(Sc[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithStringArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckStructWithStringAndStructArrayArrayIndex(Scs[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithStringAndStructArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckShortArrayArrayIndex(short[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckShortArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckStructWithTwoFieldsArrayArrayIndex(Sp[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithTwoFieldsArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckStructWithValueArrayArrayIndex(Ss[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStructWithValueArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckStringArrayArrayIndex(string[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckStringArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckUIntArrayArrayIndex(uint[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckUIntArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckULongArrayArrayIndex(ulong[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckULongArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckUShortArrayArrayIndex(ushort[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckUShortArrayArrayIndexExpression(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithCustomArrayArrayIndex<T>(T[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithCustomArrayArrayIndexExpression<T>(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckGenericArrayArrayIndex<T>(T[][] array, bool useInterpeter)
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckArrayArrayIndexExpression<T>(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithClassRestrictionArrayArrayIndex<Tc>(Tc[][] array, bool useInterpeter) where Tc : class
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithClassRestrictionArrayArrayIndexExpression<Tc>(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(Tcn[][] array, bool useInterpeter) where Tcn : class, new()
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithClassAndNewRestrictionArrayArrayIndexExpression<Tcn>(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithSubClassRestrictionArrayArrayIndex<TC>(TC[][] array, bool useInterpeter) where TC : C
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithSubClassRestrictionArrayArrayIndexExpression<TC>(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        private static void CheckGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(TCn[][] array, bool useInterpeter) where TCn : C, new()
        {
            bool success = true;
            for (int i = 0; i < array.Length; i++)
            {
                success &= CheckGenericWithSubClassAndNewRestrictionArrayArrayIndexExpression<TCn>(array, i, useInterpeter);
            }

            Assert.True(success);
        }

        #endregion

        #region Check index expression

        private static bool CheckBoolArrayArrayIndexExpression(bool[][] array, int index, bool useInterpreter)
        {
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(bool[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckByteArrayArrayIndexExpression(byte[][] array, int index, bool useInterpreter)
        {
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(byte[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckCustomArrayArrayIndexExpression(C[][] array, int index, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(C[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckCharArrayArrayIndexExpression(char[][] array, int index, bool useInterpreter)
        {
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(char[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckCustom2ArrayArrayIndexExpression(D[][] array, int index, bool useInterpreter)
        {
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(D[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckDecimalArrayArrayIndexExpression(decimal[][] array, int index, bool useInterpreter)
        {
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(decimal[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckDelegateArrayArrayIndexExpression(Delegate[][] array, int index, bool useInterpreter)
        {
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Delegate[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckDoubleArrayArrayIndexExpression(double[][] array, int index, bool useInterpreter)
        {
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(double[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckEnumArrayArrayIndexExpression(E[][] array, int index, bool useInterpreter)
        {
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(E[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckEnumLongArrayArrayIndexExpression(El[][] array, int index, bool useInterpreter)
        {
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(El[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckFloatArrayArrayIndexExpression(float[][] array, int index, bool useInterpreter)
        {
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(float[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckFuncArrayArrayIndexExpression(Func<object>[][] array, int index, bool useInterpreter)
        {
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Func<object>[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckInterfaceArrayArrayIndexExpression(I[][] array, int index, bool useInterpreter)
        {
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(I[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckIEquatableArrayArrayIndexExpression(IEquatable<C>[][] array, int index, bool useInterpreter)
        {
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(IEquatable<C>[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckIEquatable2ArrayArrayIndexExpression(IEquatable<D>[][] array, int index, bool useInterpreter)
        {
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(IEquatable<D>[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckIntArrayArrayIndexExpression(int[][] array, int index, bool useInterpreter)
        {
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(int[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckLongArrayArrayIndexExpression(long[][] array, int index, bool useInterpreter)
        {
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(long[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckObjectArrayArrayIndexExpression(object[][] array, int index, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(object[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructArrayArrayIndexExpression(S[][] array, int index, bool useInterpreter)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(S[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckSByteArrayArrayIndexExpression(sbyte[][] array, int index, bool useInterpreter)
        {
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(sbyte[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithStringArrayArrayIndexExpression(Sc[][] array, int index, bool useInterpreter)
        {
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Sc[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithStringAndStructArrayArrayIndexExpression(Scs[][] array, int index, bool useInterpreter)
        {
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Scs[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckShortArrayArrayIndexExpression(short[][] array, int index, bool useInterpreter)
        {
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(short[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithTwoFieldsArrayArrayIndexExpression(Sp[][] array, int index, bool useInterpreter)
        {
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Sp[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStructWithValueArrayArrayIndexExpression(Ss[][] array, int index, bool useInterpreter)
        {
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Ss[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckStringArrayArrayIndexExpression(string[][] array, int index, bool useInterpreter)
        {
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(string[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckUIntArrayArrayIndexExpression(uint[][] array, int index, bool useInterpreter)
        {
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(uint[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckULongArrayArrayIndexExpression(ulong[][] array, int index, bool useInterpreter)
        {
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(ulong[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckUShortArrayArrayIndexExpression(ushort[][] array, int index, bool useInterpreter)
        {
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(ushort[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithCustomArrayArrayIndexExpression<T>(T[][] array, int index, bool useInterpreter)
        {
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(T[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckArrayArrayIndexExpression<T>(T[][] array, int index, bool useInterpreter)
        {
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(T[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithClassRestrictionArrayArrayIndexExpression<Tc>(Tc[][] array, int index, bool useInterpreter) where Tc : class
        {
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Tc[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithClassAndNewRestrictionArrayArrayIndexExpression<Tcn>(Tcn[][] array, int index, bool useInterpreter) where Tcn : class, new()
        {
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(Tcn[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithSubClassRestrictionArrayArrayIndexExpression<TC>(TC[][] array, int index, bool useInterpreter) where TC : C
        {
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(TC[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        private static bool CheckGenericWithSubClassAndNewRestrictionArrayArrayIndexExpression<TCn>(TCn[][] array, int index, bool useInterpreter) where TCn : C, new()
        {
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.ArrayIndex(Expression.Constant(array, typeof(TCn[][])),
                        Expression.Constant(index, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);
            return object.Equals(f(), array[index]);
        }

        #endregion

        #region Check exception array index

        private static void CheckExceptionBoolArrayArrayIndex(bool[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckBoolArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckBoolArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionByteArrayArrayIndex(byte[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckByteArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckByteArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionCustomArrayArrayIndex(C[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckCustomArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckCustomArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionCharArrayArrayIndex(char[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckCharArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckCharArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionCustom2ArrayArrayIndex(D[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckCustom2ArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckCustom2ArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionDecimalArrayArrayIndex(decimal[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckDecimalArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckDecimalArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionDelegateArrayArrayIndex(Delegate[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckDelegateArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckDelegateArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptiondoubleArrayArrayIndex(double[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckDoubleArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckDoubleArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionEnumArrayArrayIndex(E[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckEnumArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckEnumArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionEnumLongArrayArrayIndex(El[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckEnumLongArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckEnumLongArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionFloatArrayArrayIndex(float[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckFloatArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckFloatArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionFuncArrayArrayIndex(Func<object>[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckFuncArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckFuncArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionInterfaceArrayArrayIndex(I[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckInterfaceArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckInterfaceArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionIEquatableArrayArrayIndex(IEquatable<C>[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckIEquatableArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckIEquatableArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionIEquatable2ArrayArrayIndex(IEquatable<D>[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckIEquatable2ArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckIEquatable2ArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionIntArrayArrayIndex(int[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckIntArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckIntArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionLongArrayArrayIndex(long[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckLongArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckLongArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionObjectArrayArrayIndex(object[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckObjectArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckObjectArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionStructArrayArrayIndex(S[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionSByteArrayArrayIndex(sbyte[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckSByteArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckSByteArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionStructWithStringArrayArrayIndex(Sc[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructWithStringArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructWithStringArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionStructWithStringAndStructArrayArrayIndex(Scs[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructWithStringAndStructArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructWithStringAndStructArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionShortArrayArrayIndex(short[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckShortArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckShortArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionStructWithTwoFieldsArrayArrayIndex(Sp[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructWithTwoFieldsArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructWithTwoFieldsArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionStructWithValueArrayArrayIndex(Ss[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructWithValueArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructWithValueArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionStringArrayArrayIndex(string[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStringArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStringArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionUIntArrayArrayIndex(uint[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckUIntArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckUIntArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionULongArrayArrayIndex(ulong[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckULongArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckULongArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionUShortArrayArrayIndex(ushort[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckUShortArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckUShortArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionGenericWithCustomArrayArrayIndex<T>(T[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithCustomArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithCustomArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionGenericArrayArrayIndex<T>(T[][] array, int index, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionGenericWithClassRestrictionArrayArrayIndex<Tc>(Tc[][] array, int index, bool useInterpreter) where Tc : class
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithClassRestrictionArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithClassRestrictionArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionGenericWithClassAndNewRestrictionArrayArrayIndex<Tcn>(Tcn[][] array, int index, bool useInterpreter) where Tcn : class, new()
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithClassAndNewRestrictionArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithClassAndNewRestrictionArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionGenericWithSubClassRestrictionArrayArrayIndex<TC>(TC[][] array, int index, bool useInterpreter) where TC : C
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithSubClassRestrictionArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithSubClassRestrictionArrayArrayIndexExpression(array, index, useInterpreter));
        }

        private static void CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayIndex<TCn>(TCn[][] array, int index, bool useInterpreter) where TCn : C, new()
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithSubClassAndNewRestrictionArrayArrayIndexExpression(array, index, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithSubClassAndNewRestrictionArrayArrayIndexExpression(array, index, useInterpreter));
        }

        #endregion

        [Fact]
        public static void ArrayIndexNotArray()
        {
            Expression intExp = Expression.Constant(1);
            AssertExtensions.Throws<ArgumentException>("array", () => Expression.ArrayIndex(intExp, intExp, intExp));
            AssertExtensions.Throws<ArgumentException>("array", () => Expression.ArrayIndex(intExp, Enumerable.Repeat(intExp, 1)));
        }

        [Fact]
        public static void ArrayIndexNullArray()
        {
            AssertExtensions.Throws<ArgumentNullException>("array", () => Expression.ArrayIndex(null));
            AssertExtensions.Throws<ArgumentNullException>(
                "array", () => Expression.ArrayIndex(null, Enumerable.Empty<Expression>()));
        }

        [Fact]
        public static void ArrayIndexNullIndices()
        {
            Expression array = Expression.Constant(new[,] { { 1, 2 }, { 2, 1 } });
            AssertExtensions.Throws<ArgumentNullException>("indexes", () => Expression.ArrayIndex(array, default(Expression[])));
            AssertExtensions.Throws<ArgumentNullException>("indexes[1]",
                () => Expression.ArrayIndex(array, Expression.Constant(1), null));
        }

        [Fact]
        public static void ArrayIndexWrongRank()
        {
            Expression array = Expression.Constant(new[,] { { 1, 2 }, { 2, 1 } });
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.ArrayIndex(array, new[] { Expression.Constant(2) }));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                Expression.ArrayIndex(array, Expression.Constant(2), Expression.Constant(1), Expression.Constant(2)));
        }

        [Fact]
        public static void ArrayIndexWrongType()
        {
            Expression array = Expression.Constant(new[,] { { 1, 2 }, { 2, 1 } });
            AssertExtensions.Throws<ArgumentException>("indexes[0]", () => Expression.ArrayIndex(array, Expression.Constant(2L), Expression.Constant(1)));
        }

        [Fact]
        public static void UnreadableArray()
        {
            Expression array = Expression.Property(null, typeof(Unreadable<int[,]>), nameof(Unreadable<int[,]>.WriteOnly));
            AssertExtensions.Throws<ArgumentException>("array", () => Expression.ArrayIndex(array, Expression.Constant(0), Expression.Constant(0)));
        }

        [Fact]
        public static void UnreadableIndex()
        {
            Expression array = Expression.Constant(new[,] { { 1, 2 }, { 2, 1 } });
            Expression index = Expression.Property(null, typeof(Unreadable<int>), nameof(Unreadable<int>.WriteOnly));
            AssertExtensions.Throws<ArgumentException>("indexes[0]", () => Expression.ArrayIndex(array, index, index));
        }
    }
}
