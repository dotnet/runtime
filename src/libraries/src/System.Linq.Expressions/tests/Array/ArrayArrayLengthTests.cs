// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ArrayArrayLengthTests
    {
        #region Boolean tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayArrayLengthTest(bool useInterpreter)
        {
            CheckBoolArrayArrayLengthExpression(GenerateBoolArrayArray(0), useInterpreter);
            CheckBoolArrayArrayLengthExpression(GenerateBoolArrayArray(1), useInterpreter);
            CheckBoolArrayArrayLengthExpression(GenerateBoolArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionBoolArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionBoolArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Byte tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayArrayLengthTest(bool useInterpreter)
        {
            CheckByteArrayArrayLengthExpression(GenerateByteArrayArray(0), useInterpreter);
            CheckByteArrayArrayLengthExpression(GenerateByteArrayArray(1), useInterpreter);
            CheckByteArrayArrayLengthExpression(GenerateByteArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionByteArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionByteArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Custom tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayArrayLengthTest(bool useInterpreter)
        {
            CheckCustomArrayArrayLengthExpression(GenerateCustomArrayArray(0), useInterpreter);
            CheckCustomArrayArrayLengthExpression(GenerateCustomArrayArray(1), useInterpreter);
            CheckCustomArrayArrayLengthExpression(GenerateCustomArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionCustomArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionCustomArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Char tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayArrayLengthTest(bool useInterpreter)
        {
            CheckCharArrayArrayLengthExpression(GenerateCharArrayArray(0), useInterpreter);
            CheckCharArrayArrayLengthExpression(GenerateCharArrayArray(1), useInterpreter);
            CheckCharArrayArrayLengthExpression(GenerateCharArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionCharArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionCharArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Custom2 tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayArrayLengthTest(bool useInterpreter)
        {
            CheckCustom2ArrayArrayLengthExpression(GenerateCustom2ArrayArray(0), useInterpreter);
            CheckCustom2ArrayArrayLengthExpression(GenerateCustom2ArrayArray(1), useInterpreter);
            CheckCustom2ArrayArrayLengthExpression(GenerateCustom2ArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionCustom2ArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionCustom2ArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Decimal tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayArrayLengthTest(bool useInterpreter)
        {
            CheckDecimalArrayArrayLengthExpression(GenerateDecimalArrayArray(0), useInterpreter);
            CheckDecimalArrayArrayLengthExpression(GenerateDecimalArrayArray(1), useInterpreter);
            CheckDecimalArrayArrayLengthExpression(GenerateDecimalArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionDecimalArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionDecimalArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Delegate tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayArrayLengthTest(bool useInterpreter)
        {
            CheckDelegateArrayArrayLengthExpression(GenerateDelegateArrayArray(0), useInterpreter);
            CheckDelegateArrayArrayLengthExpression(GenerateDelegateArrayArray(1), useInterpreter);
            CheckDelegateArrayArrayLengthExpression(GenerateDelegateArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionDelegateArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionDelegateArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Double tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayArrayLengthTest(bool useInterpreter)
        {
            CheckDoubleArrayArrayLengthExpression(GenerateDoubleArrayArray(0), useInterpreter);
            CheckDoubleArrayArrayLengthExpression(GenerateDoubleArrayArray(1), useInterpreter);
            CheckDoubleArrayArrayLengthExpression(GenerateDoubleArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionDoubleArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionDoubleArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Enum tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayArrayLengthTest(bool useInterpreter)
        {
            CheckEnumArrayArrayLengthExpression(GenerateEnumArrayArray(0), useInterpreter);
            CheckEnumArrayArrayLengthExpression(GenerateEnumArrayArray(1), useInterpreter);
            CheckEnumArrayArrayLengthExpression(GenerateEnumArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionEnumArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionEnumArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region EnumLong tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckEnumLongArrayArrayLengthTest(bool useInterpreter)
        {
            CheckEnumLongArrayArrayLengthExpression(GenerateEnumLongArrayArray(0), useInterpreter);
            CheckEnumLongArrayArrayLengthExpression(GenerateEnumLongArrayArray(1), useInterpreter);
            CheckEnumLongArrayArrayLengthExpression(GenerateEnumLongArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionEnumLongArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionEnumLongArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Float tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayArrayLengthTest(bool useInterpreter)
        {
            CheckFloatArrayArrayLengthExpression(GenerateFloatArrayArray(0), useInterpreter);
            CheckFloatArrayArrayLengthExpression(GenerateFloatArrayArray(1), useInterpreter);
            CheckFloatArrayArrayLengthExpression(GenerateFloatArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionFloatArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionFloatArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Func tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayArrayLengthTest(bool useInterpreter)
        {
            CheckFuncArrayArrayLengthExpression(GenerateFuncArrayArray(0), useInterpreter);
            CheckFuncArrayArrayLengthExpression(GenerateFuncArrayArray(1), useInterpreter);
            CheckFuncArrayArrayLengthExpression(GenerateFuncArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionFuncArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionFuncArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Interface tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayArrayLengthTest(bool useInterpreter)
        {
            CheckInterfaceArrayArrayLengthExpression(GenerateInterfaceArrayArray(0), useInterpreter);
            CheckInterfaceArrayArrayLengthExpression(GenerateInterfaceArrayArray(1), useInterpreter);
            CheckInterfaceArrayArrayLengthExpression(GenerateInterfaceArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionInterfaceArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionInterfaceArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region IEquatableCustom tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayArrayLengthTest(bool useInterpreter)
        {
            CheckIEquatableCustomArrayArrayLengthExpression(GenerateIEquatableCustomArrayArray(0), useInterpreter);
            CheckIEquatableCustomArrayArrayLengthExpression(GenerateIEquatableCustomArrayArray(1), useInterpreter);
            CheckIEquatableCustomArrayArrayLengthExpression(GenerateIEquatableCustomArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionIEquatableCustomArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionIEquatableCustomArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region IEquatableCustom2 tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayArrayLengthTest(bool useInterpreter)
        {
            CheckIEquatableCustom2ArrayArrayLengthExpression(GenerateIEquatableCustom2ArrayArray(0), useInterpreter);
            CheckIEquatableCustom2ArrayArrayLengthExpression(GenerateIEquatableCustom2ArrayArray(1), useInterpreter);
            CheckIEquatableCustom2ArrayArrayLengthExpression(GenerateIEquatableCustom2ArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionIEquatableCustom2ArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionIEquatableCustom2ArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Int tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayArrayLengthTest(bool useInterpreter)
        {
            CheckIntArrayArrayLengthExpression(GenerateIntArrayArray(0), useInterpreter);
            CheckIntArrayArrayLengthExpression(GenerateIntArrayArray(1), useInterpreter);
            CheckIntArrayArrayLengthExpression(GenerateIntArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionIntArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionIntArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Long tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayArrayLengthTest(bool useInterpreter)
        {
            CheckLongArrayArrayLengthExpression(GenerateLongArrayArray(0), useInterpreter);
            CheckLongArrayArrayLengthExpression(GenerateLongArrayArray(1), useInterpreter);
            CheckLongArrayArrayLengthExpression(GenerateLongArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionLongArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionLongArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Object tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayArrayLengthTest(bool useInterpreter)
        {
            CheckObjectArrayArrayLengthExpression(GenerateObjectArrayArray(0), useInterpreter);
            CheckObjectArrayArrayLengthExpression(GenerateObjectArrayArray(1), useInterpreter);
            CheckObjectArrayArrayLengthExpression(GenerateObjectArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionObjectArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionObjectArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Struct tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayArrayLengthTest(bool useInterpreter)
        {
            CheckStructArrayArrayLengthExpression(GenerateStructArrayArray(0), useInterpreter);
            CheckStructArrayArrayLengthExpression(GenerateStructArrayArray(1), useInterpreter);
            CheckStructArrayArrayLengthExpression(GenerateStructArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionStructArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region SByte tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayArrayLengthTest(bool useInterpreter)
        {
            CheckSByteArrayArrayLengthExpression(GenerateSByteArrayArray(0), useInterpreter);
            CheckSByteArrayArrayLengthExpression(GenerateSByteArrayArray(1), useInterpreter);
            CheckSByteArrayArrayLengthExpression(GenerateSByteArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionSByteArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionSByteArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region StructWithString tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayArrayLengthTest(bool useInterpreter)
        {
            CheckStructWithStringArrayArrayLengthExpression(GenerateStructWithStringArrayArray(0), useInterpreter);
            CheckStructWithStringArrayArrayLengthExpression(GenerateStructWithStringArrayArray(1), useInterpreter);
            CheckStructWithStringArrayArrayLengthExpression(GenerateStructWithStringArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithStringArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionStructWithStringArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region StructWithStringAndValue tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayArrayLengthTest(bool useInterpreter)
        {
            CheckStructWithStringAndValueArrayArrayLengthExpression(GenerateStructWithStringAndValueArrayArray(0), useInterpreter);
            CheckStructWithStringAndValueArrayArrayLengthExpression(GenerateStructWithStringAndValueArrayArray(1), useInterpreter);
            CheckStructWithStringAndValueArrayArrayLengthExpression(GenerateStructWithStringAndValueArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithStringAndValueArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionStructWithStringAndValueArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Short tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayArrayLengthTest(bool useInterpreter)
        {
            CheckShortArrayArrayLengthExpression(GenerateShortArrayArray(0), useInterpreter);
            CheckShortArrayArrayLengthExpression(GenerateShortArrayArray(1), useInterpreter);
            CheckShortArrayArrayLengthExpression(GenerateShortArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionShortArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionShortArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region StructWithTwoValues tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayArrayLengthTest(bool useInterpreter)
        {
            CheckStructWithTwoValuesArrayArrayLengthExpression(GenerateStructWithTwoValuesArrayArray(0), useInterpreter);
            CheckStructWithTwoValuesArrayArrayLengthExpression(GenerateStructWithTwoValuesArrayArray(1), useInterpreter);
            CheckStructWithTwoValuesArrayArrayLengthExpression(GenerateStructWithTwoValuesArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithTwoValuesArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionStructWithTwoValuesArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region StructWithValue tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayArrayLengthTest(bool useInterpreter)
        {
            CheckStructWithValueArrayArrayLengthExpression(GenerateStructWithValueArrayArray(0), useInterpreter);
            CheckStructWithValueArrayArrayLengthExpression(GenerateStructWithValueArrayArray(1), useInterpreter);
            CheckStructWithValueArrayArrayLengthExpression(GenerateStructWithValueArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithValueArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionStructWithValueArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region String tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayArrayLengthTest(bool useInterpreter)
        {
            CheckStringArrayArrayLengthExpression(GenerateStringArrayArray(0), useInterpreter);
            CheckStringArrayArrayLengthExpression(GenerateStringArrayArray(1), useInterpreter);
            CheckStringArrayArrayLengthExpression(GenerateStringArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStringArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionStringArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region UInt tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayArrayLengthTest(bool useInterpreter)
        {
            CheckUIntArrayArrayLengthExpression(GenerateUIntArrayArray(0), useInterpreter);
            CheckUIntArrayArrayLengthExpression(GenerateUIntArrayArray(1), useInterpreter);
            CheckUIntArrayArrayLengthExpression(GenerateUIntArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionUIntArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionUIntArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region ULong tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayArrayLengthTest(bool useInterpreter)
        {
            CheckULongArrayArrayLengthExpression(GenerateULongArrayArray(0), useInterpreter);
            CheckULongArrayArrayLengthExpression(GenerateULongArrayArray(1), useInterpreter);
            CheckULongArrayArrayLengthExpression(GenerateULongArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionULongArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionULongArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region UShort tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayArrayLengthTest(bool useInterpreter)
        {
            CheckUShortArrayArrayLengthExpression(GenerateUShortArrayArray(0), useInterpreter);
            CheckUShortArrayArrayLengthExpression(GenerateUShortArrayArray(1), useInterpreter);
            CheckUShortArrayArrayLengthExpression(GenerateUShortArrayArray(5), useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionUShortArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionUShortArrayArrayLength(null, useInterpreter);
        }

        #endregion

        #region Generic tests

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericArrayArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericEnumArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericArrayArrayLengthTestHelper<E>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericEnumArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayArrayLengthTestHelper<E>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericObjectArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericArrayArrayLengthTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericObjectArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayArrayLengthTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericArrayArrayLengthTestHelper<S>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericStructArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayArrayLengthTestHelper<S>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStringAndValueArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericArrayArrayLengthTestHelper<Scs>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericStructWithStringAndValueArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayArrayLengthTestHelper<Scs>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithClassRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionArrayArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomWithClassRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassRestrictionArrayArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericObjectWithClassRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionArrayArrayLengthTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericObjectWithClassRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassRestrictionArrayArrayLengthTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithSubClassRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithSubClassRestrictionArrayArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomWithSubClassRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithSubClassRestrictionArrayArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithClassAndNewRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithClassAndNewRestrictionArrayArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomWithClassAndNewRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericObjectWithClassAndNewRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithClassAndNewRestrictionArrayArrayLengthTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericObjectWithClassAndNewRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayLengthTestHelper<object>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithSubClassAndNewRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithSubClassAndNewRestrictionArrayArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomWithSubClassAndNewRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericEnumWithStructRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayArrayLengthTestHelper<E>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericEnumWithStructRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithStructRestrictionArrayArrayLengthTestHelper<E>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStructRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayArrayLengthTestHelper<S>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericStructWithStructRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithStructRestrictionArrayArrayLengthTestHelper<S>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStringAndValueWithStructRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayArrayLengthTestHelper<Scs>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericStructWithStringAndValueWithStructRestrictionArrayArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithStructRestrictionArrayArrayLengthTestHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        public static void CheckGenericArrayArrayLengthTestHelper<T>(bool useInterpreter)
        {
            CheckGenericArrayArrayLengthExpression<T>(GenerateGenericArrayArray<T>(0), useInterpreter);
            CheckGenericArrayArrayLengthExpression<T>(GenerateGenericArrayArray<T>(1), useInterpreter);
            CheckGenericArrayArrayLengthExpression<T>(GenerateGenericArrayArray<T>(5), useInterpreter);
        }

        public static void CheckExceptionGenericArrayArrayLengthTestHelper<T>(bool useInterpreter)
        {
            CheckExceptionGenericArrayArrayLength<T>(null, useInterpreter);
        }

        public static void CheckGenericWithClassRestrictionArrayArrayLengthTestHelper<Tc>(bool useInterpreter) where Tc : class
        {
            CheckGenericWithClassRestrictionArrayArrayLengthExpression<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(0), useInterpreter);
            CheckGenericWithClassRestrictionArrayArrayLengthExpression<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(1), useInterpreter);
            CheckGenericWithClassRestrictionArrayArrayLengthExpression<Tc>(GenerateGenericWithClassRestrictionArrayArray<Tc>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithClassRestrictionArrayArrayLengthTestHelper<Tc>(bool useInterpreter) where Tc : class
        {
            CheckExceptionGenericWithClassRestrictionArrayArrayLength<Tc>(null, useInterpreter);
        }

        public static void CheckGenericWithSubClassRestrictionArrayArrayLengthTestHelper<TC>(bool useInterpreter) where TC : C
        {
            CheckGenericWithSubClassRestrictionArrayArrayLengthExpression<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(0), useInterpreter);
            CheckGenericWithSubClassRestrictionArrayArrayLengthExpression<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(1), useInterpreter);
            CheckGenericWithSubClassRestrictionArrayArrayLengthExpression<TC>(GenerateGenericWithSubClassRestrictionArrayArray<TC>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithSubClassRestrictionArrayArrayLengthTestHelper<TC>(bool useInterpreter) where TC : C
        {
            CheckExceptionGenericWithSubClassRestrictionArrayArrayLength<TC>(null, useInterpreter);
        }

        public static void CheckGenericWithClassAndNewRestrictionArrayArrayLengthTestHelper<Tcn>(bool useInterpreter) where Tcn : class, new()
        {
            CheckGenericWithClassAndNewRestrictionArrayArrayLengthExpression<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(0), useInterpreter);
            CheckGenericWithClassAndNewRestrictionArrayArrayLengthExpression<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(1), useInterpreter);
            CheckGenericWithClassAndNewRestrictionArrayArrayLengthExpression<Tcn>(GenerateGenericWithClassAndNewRestrictionArrayArray<Tcn>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithClassAndNewRestrictionArrayArrayLengthTestHelper<Tcn>(bool useInterpreter) where Tcn : class, new()
        {
            CheckExceptionGenericWithClassAndNewRestrictionArrayArrayLength<Tcn>(null, useInterpreter);
        }

        public static void CheckGenericWithSubClassAndNewRestrictionArrayArrayLengthTestHelper<TCn>(bool useInterpreter) where TCn : C, new()
        {
            CheckGenericWithSubClassAndNewRestrictionArrayArrayLengthExpression<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(0), useInterpreter);
            CheckGenericWithSubClassAndNewRestrictionArrayArrayLengthExpression<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(1), useInterpreter);
            CheckGenericWithSubClassAndNewRestrictionArrayArrayLengthExpression<TCn>(GenerateGenericWithSubClassAndNewRestrictionArrayArray<TCn>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayLengthTestHelper<TCn>(bool useInterpreter) where TCn : C, new()
        {
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayLength<TCn>(null, useInterpreter);
        }

        public static void CheckGenericWithStructRestrictionArrayArrayLengthTestHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            CheckGenericWithStructRestrictionArrayArrayLengthExpression<Ts>(GenerateGenericWithStructRestrictionArrayArray<Ts>(0), useInterpreter);
            CheckGenericWithStructRestrictionArrayArrayLengthExpression<Ts>(GenerateGenericWithStructRestrictionArrayArray<Ts>(1), useInterpreter);
            CheckGenericWithStructRestrictionArrayArrayLengthExpression<Ts>(GenerateGenericWithStructRestrictionArrayArray<Ts>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithStructRestrictionArrayArrayLengthTestHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            CheckExceptionGenericWithStructRestrictionArrayArrayLength<Ts>(null, useInterpreter);
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

        private static double[][] GenerateDoubleArrayArray(int size)
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

        private static IEquatable<C>[][] GenerateIEquatableCustomArrayArray(int size)
        {
            IEquatable<C>[][] array = new IEquatable<C>[][] { null, new IEquatable<C>[0], new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) }, new IEquatable<C>[100] };
            IEquatable<C>[][] result = new IEquatable<C>[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static IEquatable<D>[][] GenerateIEquatableCustom2ArrayArray(int size)
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

        private static Scs[][] GenerateStructWithStringAndValueArrayArray(int size)
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

        private static Sp[][] GenerateStructWithTwoValuesArrayArray(int size)
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

        private static Ts[][] GenerateGenericWithStructRestrictionArrayArray<Ts>(int size) where Ts : struct
        {
            Ts[][] array = new Ts[][] { null, new Ts[0], new Ts[] { default(Ts), new Ts() }, new Ts[100] };
            Ts[][] result = new Ts[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        #endregion

        #region Check length expression

        private static void CheckBoolArrayArrayLengthExpression(bool[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(bool[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckByteArrayArrayLengthExpression(byte[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(byte[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckCustomArrayArrayLengthExpression(C[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(C[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckCharArrayArrayLengthExpression(char[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(char[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckCustom2ArrayArrayLengthExpression(D[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(D[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckDecimalArrayArrayLengthExpression(decimal[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(decimal[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckDelegateArrayArrayLengthExpression(Delegate[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Delegate[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckDoubleArrayArrayLengthExpression(double[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(double[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckEnumArrayArrayLengthExpression(E[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(E[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckEnumLongArrayArrayLengthExpression(El[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(El[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckFloatArrayArrayLengthExpression(float[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(float[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckFuncArrayArrayLengthExpression(Func<object>[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Func<object>[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckInterfaceArrayArrayLengthExpression(I[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(I[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckIEquatableCustomArrayArrayLengthExpression(IEquatable<C>[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(IEquatable<C>[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckIEquatableCustom2ArrayArrayLengthExpression(IEquatable<D>[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(IEquatable<D>[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckIntArrayArrayLengthExpression(int[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(int[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckLongArrayArrayLengthExpression(long[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(long[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckObjectArrayArrayLengthExpression(object[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(object[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructArrayArrayLengthExpression(S[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(S[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckSByteArrayArrayLengthExpression(sbyte[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(sbyte[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructWithStringArrayArrayLengthExpression(Sc[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Sc[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructWithStringAndValueArrayArrayLengthExpression(Scs[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Scs[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckShortArrayArrayLengthExpression(short[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(short[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructWithTwoValuesArrayArrayLengthExpression(Sp[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Sp[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructWithValueArrayArrayLengthExpression(Ss[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Ss[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckStringArrayArrayLengthExpression(string[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(string[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckUIntArrayArrayLengthExpression(uint[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(uint[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckULongArrayArrayLengthExpression(ulong[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(ulong[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckUShortArrayArrayLengthExpression(ushort[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(ushort[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericArrayArrayLengthExpression<T>(T[][] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(T[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithClassRestrictionArrayArrayLengthExpression<Tc>(Tc[][] array, bool useInterpreter) where Tc : class
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Tc[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithSubClassRestrictionArrayArrayLengthExpression<TC>(TC[][] array, bool useInterpreter) where TC : C
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(TC[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithClassAndNewRestrictionArrayArrayLengthExpression<Tcn>(Tcn[][] array, bool useInterpreter) where Tcn : class, new()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Tcn[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithSubClassAndNewRestrictionArrayArrayLengthExpression<TCn>(TCn[][] array, bool useInterpreter) where TCn : C, new()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(TCn[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithStructRestrictionArrayArrayLengthExpression<Ts>(Ts[][] array, bool useInterpreter) where Ts : struct
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Ts[][]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        #endregion

        #region Check exception array length

        private static void CheckExceptionBoolArrayArrayLength(bool[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckBoolArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckBoolArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionByteArrayArrayLength(byte[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckByteArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckByteArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionCustomArrayArrayLength(C[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckCustomArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckCustomArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionCharArrayArrayLength(char[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckCharArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckCharArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionCustom2ArrayArrayLength(D[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckCustom2ArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckCustom2ArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionDecimalArrayArrayLength(decimal[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckDecimalArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckDecimalArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionDelegateArrayArrayLength(Delegate[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckDelegateArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckDelegateArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionDoubleArrayArrayLength(double[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckDoubleArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckDoubleArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionEnumArrayArrayLength(E[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckEnumArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckEnumArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionEnumLongArrayArrayLength(El[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckEnumLongArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckEnumLongArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionFloatArrayArrayLength(float[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckFloatArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckFloatArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionFuncArrayArrayLength(Func<object>[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckFuncArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckFuncArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionInterfaceArrayArrayLength(I[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckInterfaceArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckInterfaceArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionIEquatableCustomArrayArrayLength(IEquatable<C>[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckIEquatableCustomArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckIEquatableCustomArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionIEquatableCustom2ArrayArrayLength(IEquatable<D>[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckIEquatableCustom2ArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckIEquatableCustom2ArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionIntArrayArrayLength(int[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckIntArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckIntArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionLongArrayArrayLength(long[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckLongArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckLongArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionObjectArrayArrayLength(object[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckObjectArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckObjectArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionStructArrayArrayLength(S[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionSByteArrayArrayLength(sbyte[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckSByteArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckSByteArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionStructWithStringArrayArrayLength(Sc[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructWithStringArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructWithStringArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionStructWithStringAndValueArrayArrayLength(Scs[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructWithStringAndValueArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructWithStringAndValueArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionShortArrayArrayLength(short[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckShortArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckShortArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionStructWithTwoValuesArrayArrayLength(Sp[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructWithTwoValuesArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructWithTwoValuesArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionStructWithValueArrayArrayLength(Ss[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStructWithValueArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStructWithValueArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionStringArrayArrayLength(string[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckStringArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckStringArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionUIntArrayArrayLength(uint[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckUIntArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckUIntArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionULongArrayArrayLength(ulong[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckULongArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckULongArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionUShortArrayArrayLength(ushort[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckUShortArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckUShortArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionGenericArrayArrayLength<T>(T[][] array, bool useInterpreter)
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionGenericWithClassRestrictionArrayArrayLength<Tc>(Tc[][] array, bool useInterpreter) where Tc : class
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithClassRestrictionArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithClassRestrictionArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionGenericWithSubClassRestrictionArrayArrayLength<TC>(TC[][] array, bool useInterpreter) where TC : C
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithSubClassRestrictionArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithSubClassRestrictionArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionGenericWithClassAndNewRestrictionArrayArrayLength<Tcn>(Tcn[][] array, bool useInterpreter) where Tcn : class, new()
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithClassAndNewRestrictionArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithClassAndNewRestrictionArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionGenericWithSubClassAndNewRestrictionArrayArrayLength<TCn>(TCn[][] array, bool useInterpreter) where TCn : C, new()
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithSubClassAndNewRestrictionArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithSubClassAndNewRestrictionArrayArrayLengthExpression(array, useInterpreter));
        }

        private static void CheckExceptionGenericWithStructRestrictionArrayArrayLength<Ts>(Ts[][] array, bool useInterpreter) where Ts : struct
        {
            if (array == null)
                Assert.Throws<NullReferenceException>(() => CheckGenericWithStructRestrictionArrayArrayLengthExpression(array, useInterpreter));
            else
                Assert.Throws<IndexOutOfRangeException>(() => CheckGenericWithStructRestrictionArrayArrayLengthExpression(array, useInterpreter));
        }

        #endregion

        #region Regression tests

        [Fact]
        public static void ArrayLength_MultiDimensionalOf1()
        {
            foreach (var e in new Expression[] { Expression.Parameter(typeof(int).MakeArrayType(1)), Expression.Constant(new int[2, 2]) })
            {
                AssertExtensions.Throws<ArgumentException>("array", () => Expression.ArrayLength(e));
            }
        }

        #endregion
    }
}
