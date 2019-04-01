// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ArrayLengthTests
    {
        #region Bool tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayLengthTest(bool useInterpreter)
        {
            CheckBoolArrayLengthExpression(GenerateBoolArray(0), useInterpreter);
            CheckBoolArrayLengthExpression(GenerateBoolArray(1), useInterpreter);
            CheckBoolArrayLengthExpression(GenerateBoolArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionBoolArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckBoolArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Byte tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayLengthTest(bool useInterpreter)
        {
            CheckByteArrayLengthExpression(GenerateByteArray(0), useInterpreter);
            CheckByteArrayLengthExpression(GenerateByteArray(1), useInterpreter);
            CheckByteArrayLengthExpression(GenerateByteArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionByteArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckByteArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Custom tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayLengthTest(bool useInterpreter)
        {
            CheckCustomArrayLengthExpression(GenerateCustomArray(0), useInterpreter);
            CheckCustomArrayLengthExpression(GenerateCustomArray(1), useInterpreter);
            CheckCustomArrayLengthExpression(GenerateCustomArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionCustomArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckCustomArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Char tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayLengthTest(bool useInterpreter)
        {
            CheckCharArrayLengthExpression(GenerateCharArray(0), useInterpreter);
            CheckCharArrayLengthExpression(GenerateCharArray(1), useInterpreter);
            CheckCharArrayLengthExpression(GenerateCharArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionCharArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckCharArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Custom2 tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayLengthTest(bool useInterpreter)
        {
            CheckCustom2ArrayLengthExpression(GenerateCustom2Array(0), useInterpreter);
            CheckCustom2ArrayLengthExpression(GenerateCustom2Array(1), useInterpreter);
            CheckCustom2ArrayLengthExpression(GenerateCustom2Array(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionCustom2ArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckCustom2ArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Decimal tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayLengthTest(bool useInterpreter)
        {
            CheckDecimalArrayLengthExpression(GenerateDecimalArray(0), useInterpreter);
            CheckDecimalArrayLengthExpression(GenerateDecimalArray(1), useInterpreter);
            CheckDecimalArrayLengthExpression(GenerateDecimalArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionDecimalArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckDecimalArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Delegate tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayLengthTest(bool useInterpreter)
        {
            CheckDelegateArrayLengthExpression(GenerateDelegateArray(0), useInterpreter);
            CheckDelegateArrayLengthExpression(GenerateDelegateArray(1), useInterpreter);
            CheckDelegateArrayLengthExpression(GenerateDelegateArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionDelegateArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckDelegateArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region double tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckdoubleArrayLengthTest(bool useInterpreter)
        {
            CheckDoubleArrayLengthExpression(GeneratedoubleArray(0), useInterpreter);
            CheckDoubleArrayLengthExpression(GeneratedoubleArray(1), useInterpreter);
            CheckDoubleArrayLengthExpression(GeneratedoubleArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptiondoubleArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckDoubleArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Enum tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayLengthTest(bool useInterpreter)
        {
            CheckEnumArrayLengthExpression(GenerateEnumArray(0), useInterpreter);
            CheckEnumArrayLengthExpression(GenerateEnumArray(1), useInterpreter);
            CheckEnumArrayLengthExpression(GenerateEnumArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionEnumArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckEnumArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region EnumLong tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumLongArrayLengthTest(bool useInterpreter)
        {
            CheckEnumLongArrayLengthExpression(GenerateEnumLongArray(0), useInterpreter);
            CheckEnumLongArrayLengthExpression(GenerateEnumLongArray(1), useInterpreter);
            CheckEnumLongArrayLengthExpression(GenerateEnumLongArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionEnumLongArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckEnumLongArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Float tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayLengthTest(bool useInterpreter)
        {
            CheckFloatArrayLengthExpression(GenerateFloatArray(0), useInterpreter);
            CheckFloatArrayLengthExpression(GenerateFloatArray(1), useInterpreter);
            CheckFloatArrayLengthExpression(GenerateFloatArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionFloatArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckFloatArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Func tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayLengthTest(bool useInterpreter)
        {
            CheckFuncArrayLengthExpression(GenerateFuncArray(0), useInterpreter);
            CheckFuncArrayLengthExpression(GenerateFuncArray(1), useInterpreter);
            CheckFuncArrayLengthExpression(GenerateFuncArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionFuncArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckFuncArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Interface tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayLengthTest(bool useInterpreter)
        {
            CheckInterfaceArrayLengthExpression(GenerateInterfaceArray(0), useInterpreter);
            CheckInterfaceArrayLengthExpression(GenerateInterfaceArray(1), useInterpreter);
            CheckInterfaceArrayLengthExpression(GenerateInterfaceArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionInterfaceArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckInterfaceArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region IEquatable tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableArrayLengthTest(bool useInterpreter)
        {
            CheckIEquatableArrayLengthExpression(GenerateIEquatableArray(0), useInterpreter);
            CheckIEquatableArrayLengthExpression(GenerateIEquatableArray(1), useInterpreter);
            CheckIEquatableArrayLengthExpression(GenerateIEquatableArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionIEquatableArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckIEquatableArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region IEquatable2 tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatable2ArrayLengthTest(bool useInterpreter)
        {
            CheckIEquatable2ArrayLengthExpression(GenerateIEquatable2Array(0), useInterpreter);
            CheckIEquatable2ArrayLengthExpression(GenerateIEquatable2Array(1), useInterpreter);
            CheckIEquatable2ArrayLengthExpression(GenerateIEquatable2Array(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionIEquatable2ArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckIEquatable2ArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Int tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayLengthTest(bool useInterpreter)
        {
            CheckIntArrayLengthExpression(GenerateIntArray(0), useInterpreter);
            CheckIntArrayLengthExpression(GenerateIntArray(1), useInterpreter);
            CheckIntArrayLengthExpression(GenerateIntArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionIntArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckIntArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Long tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayLengthTest(bool useInterpreter)
        {
            CheckLongArrayLengthExpression(GenerateLongArray(0), useInterpreter);
            CheckLongArrayLengthExpression(GenerateLongArray(1), useInterpreter);
            CheckLongArrayLengthExpression(GenerateLongArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionLongArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckLongArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Object tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayLengthTest(bool useInterpreter)
        {
            CheckObjectArrayLengthExpression(GenerateObjectArray(0), useInterpreter);
            CheckObjectArrayLengthExpression(GenerateObjectArray(1), useInterpreter);
            CheckObjectArrayLengthExpression(GenerateObjectArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionObjectArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckObjectArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Struct tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayLengthTest(bool useInterpreter)
        {
            CheckStructArrayLengthExpression(GenerateStructArray(0), useInterpreter);
            CheckStructArrayLengthExpression(GenerateStructArray(1), useInterpreter);
            CheckStructArrayLengthExpression(GenerateStructArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckStructArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region SByte tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayLengthTest(bool useInterpreter)
        {
            CheckSByteArrayLengthExpression(GenerateSByteArray(0), useInterpreter);
            CheckSByteArrayLengthExpression(GenerateSByteArray(1), useInterpreter);
            CheckSByteArrayLengthExpression(GenerateSByteArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionSByteArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckSByteArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region StructWithString tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayLengthTest(bool useInterpreter)
        {
            CheckStructWithStringArrayLengthExpression(GenerateStructWithStringArray(0), useInterpreter);
            CheckStructWithStringArrayLengthExpression(GenerateStructWithStringArray(1), useInterpreter);
            CheckStructWithStringArrayLengthExpression(GenerateStructWithStringArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithStringArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckStructWithStringArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region StructWithStringAndStruct tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndStructArrayLengthTest(bool useInterpreter)
        {
            CheckStructWithStringAndStructArrayLengthExpression(GenerateStructWithStringAndStructArray(0), useInterpreter);
            CheckStructWithStringAndStructArrayLengthExpression(GenerateStructWithStringAndStructArray(1), useInterpreter);
            CheckStructWithStringAndStructArrayLengthExpression(GenerateStructWithStringAndStructArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithStringAndStructArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckStructWithStringAndStructArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Short tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayLengthTest(bool useInterpreter)
        {
            CheckShortArrayLengthExpression(GenerateShortArray(0), useInterpreter);
            CheckShortArrayLengthExpression(GenerateShortArray(1), useInterpreter);
            CheckShortArrayLengthExpression(GenerateShortArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionShortArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckShortArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region StructWithTwoFields tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoFieldsArrayLengthTest(bool useInterpreter)
        {
            CheckStructWithTwoFieldsArrayLengthExpression(GenerateStructWithTwoFieldsArray(0), useInterpreter);
            CheckStructWithTwoFieldsArrayLengthExpression(GenerateStructWithTwoFieldsArray(1), useInterpreter);
            CheckStructWithTwoFieldsArrayLengthExpression(GenerateStructWithTwoFieldsArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithTwoFieldsArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckStructWithTwoFieldsArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region StructWithValue tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayLengthTest(bool useInterpreter)
        {
            CheckStructWithValueArrayLengthExpression(GenerateStructWithValueArray(0), useInterpreter);
            CheckStructWithValueArrayLengthExpression(GenerateStructWithValueArray(1), useInterpreter);
            CheckStructWithValueArrayLengthExpression(GenerateStructWithValueArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStructWithValueArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckStructWithValueArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region String tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayLengthTest(bool useInterpreter)
        {
            CheckStringArrayLengthExpression(GenerateStringArray(0), useInterpreter);
            CheckStringArrayLengthExpression(GenerateStringArray(1), useInterpreter);
            CheckStringArrayLengthExpression(GenerateStringArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionStringArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckStringArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region UInt tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayLengthTest(bool useInterpreter)
        {
            CheckUIntArrayLengthExpression(GenerateUIntArray(0), useInterpreter);
            CheckUIntArrayLengthExpression(GenerateUIntArray(1), useInterpreter);
            CheckUIntArrayLengthExpression(GenerateUIntArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionUIntArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckUIntArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region ULong tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayLengthTest(bool useInterpreter)
        {
            CheckULongArrayLengthExpression(GenerateULongArray(0), useInterpreter);
            CheckULongArrayLengthExpression(GenerateULongArray(1), useInterpreter);
            CheckULongArrayLengthExpression(GenerateULongArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionULongArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckULongArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region UShort tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayLengthTest(bool useInterpreter)
        {
            CheckUShortArrayLengthExpression(GenerateUShortArray(0), useInterpreter);
            CheckUShortArrayLengthExpression(GenerateUShortArray(1), useInterpreter);
            CheckUShortArrayLengthExpression(GenerateUShortArray(5), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionUShortArrayLengthTest(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckUShortArrayLengthExpression(null, useInterpreter));
        }

        #endregion

        #region Generic tests

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomArrayLengthTest(bool useInterpreter)
        {
            CheckGenericArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericEnumArrayLengthTest(bool useInterpreter)
        {
            CheckGenericArrayLengthTestHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericEnumArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayLengthTestHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericObjectArrayLengthTest(bool useInterpreter)
        {
            CheckGenericArrayLengthTestHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericObjectArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayLengthTestHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructArrayLengthTest(bool useInterpreter)
        {
            CheckGenericArrayLengthTestHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericStructArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayLengthTestHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStringAndFieldArrayLengthTest(bool useInterpreter)
        {
            CheckGenericArrayLengthTestHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericStructWithStringAndFieldArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericArrayLengthTestHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithClassRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomWithClassRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassRestrictionArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericObjectWithClassRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionArrayLengthTestHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericObjectWithClassRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassRestrictionArrayLengthTestHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithClassAndNewRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithClassAndNewRestrictionArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomWithClassAndNewRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassAndNewRestrictionArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericObjectWithClassAndNewRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithClassAndNewRestrictionArrayLengthTestHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericObjectWithClassAndNewRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithClassAndNewRestrictionArrayLengthTestHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithSubClassRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithSubClassRestrictionArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomWithSubClassRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithSubClassRestrictionArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithSubClassAndNewRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithSubClassAndNewRestrictionArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericCustomWithSubClassAndNewRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayLengthTestHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericEnumWithStructRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayLengthTestHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericEnumWithStructRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStructRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayLengthTestHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericStructWithStructRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStringAndFieldWithStructRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayLengthTestHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckExceptionGenericStructWithStringAndFieldWithStructRestrictionArrayLengthTest(bool useInterpreter)
        {
            CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        public static void CheckGenericArrayLengthTestHelper<T>(bool useInterpreter)
        {
            CheckGenericArrayLengthExpression<T>(GenerateGenericArray<T>(0), useInterpreter);
            CheckGenericArrayLengthExpression<T>(GenerateGenericArray<T>(1), useInterpreter);
            CheckGenericArrayLengthExpression<T>(GenerateGenericArray<T>(5), useInterpreter);
        }

        public static void CheckExceptionGenericArrayLengthTestHelper<T>(bool useInterpreter)
        {
            Assert.Throws<NullReferenceException>(() => CheckGenericArrayLengthExpression<T>(null, useInterpreter));
        }

        public static void CheckGenericWithClassRestrictionArrayLengthTestHelper<Tc>(bool useInterpreter) where Tc : class
        {
            CheckGenericWithClassRestrictionArrayLengthExpression<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(0), useInterpreter);
            CheckGenericWithClassRestrictionArrayLengthExpression<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(1), useInterpreter);
            CheckGenericWithClassRestrictionArrayLengthExpression<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithClassRestrictionArrayLengthTestHelper<Tc>(bool useInterpreter) where Tc : class
        {
            Assert.Throws<NullReferenceException>(() => CheckGenericWithClassRestrictionArrayLengthExpression<Tc>(null, useInterpreter));
        }

        public static void CheckGenericWithClassAndNewRestrictionArrayLengthTestHelper<Tcn>(bool useInterpreter) where Tcn : class, new()
        {
            CheckGenericWithClassAndNewRestrictionArrayLengthExpression<Tcn>(GenerateGenericWithClassAndNewRestrictionArray<Tcn>(0), useInterpreter);
            CheckGenericWithClassAndNewRestrictionArrayLengthExpression<Tcn>(GenerateGenericWithClassAndNewRestrictionArray<Tcn>(1), useInterpreter);
            CheckGenericWithClassAndNewRestrictionArrayLengthExpression<Tcn>(GenerateGenericWithClassAndNewRestrictionArray<Tcn>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithClassAndNewRestrictionArrayLengthTestHelper<Tcn>(bool useInterpreter) where Tcn : class, new()
        {
            Assert.Throws<NullReferenceException>(() => CheckGenericWithClassAndNewRestrictionArrayLengthExpression<Tcn>(null, useInterpreter));
        }

        public static void CheckGenericWithSubClassRestrictionArrayLengthTestHelper<TC>(bool useInterpreter) where TC : C
        {
            CheckGenericWithSubClassRestrictionArrayLengthExpression<TC>(GenerateGenericWithSubClassRestrictionArray<TC>(0), useInterpreter);
            CheckGenericWithSubClassRestrictionArrayLengthExpression<TC>(GenerateGenericWithSubClassRestrictionArray<TC>(1), useInterpreter);
            CheckGenericWithSubClassRestrictionArrayLengthExpression<TC>(GenerateGenericWithSubClassRestrictionArray<TC>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithSubClassRestrictionArrayLengthTestHelper<TC>(bool useInterpreter) where TC : C
        {
            Assert.Throws<NullReferenceException>(() => CheckGenericWithSubClassRestrictionArrayLengthExpression<TC>(null, useInterpreter));
        }

        public static void CheckGenericWithSubClassAndNewRestrictionArrayLengthTestHelper<TCn>(bool useInterpreter) where TCn : C, new()
        {
            CheckGenericWithSubClassAndNewRestrictionArrayLengthExpression<TCn>(GenerateGenericWithSubClassAndNewRestrictionArray<TCn>(0), useInterpreter);
            CheckGenericWithSubClassAndNewRestrictionArrayLengthExpression<TCn>(GenerateGenericWithSubClassAndNewRestrictionArray<TCn>(1), useInterpreter);
            CheckGenericWithSubClassAndNewRestrictionArrayLengthExpression<TCn>(GenerateGenericWithSubClassAndNewRestrictionArray<TCn>(5), useInterpreter);
        }

        public static void CheckExceptionGenericWithSubClassAndNewRestrictionArrayLengthTestHelper<TCn>(bool useInterpreter) where TCn : C, new()
        {
            Assert.Throws<NullReferenceException>(() => CheckGenericWithSubClassAndNewRestrictionArrayLengthExpression<TCn>(null, useInterpreter));
        }

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

        private static double[] GeneratedoubleArray(int size)
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

        private static IEquatable<C>[] GenerateIEquatableArray(int size)
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            IEquatable<C>[] result = new IEquatable<C>[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static IEquatable<D>[] GenerateIEquatable2Array(int size)
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

        private static Scs[] GenerateStructWithStringAndStructArray(int size)
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

        private static Sp[] GenerateStructWithTwoFieldsArray(int size)
        {
            Sp[] array = new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) };
            Sp[] result = new Sp[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static Ss[] GenerateStructWithValueArray(int size)
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
            T[] array = new T[] { default(T) };
            T[] result = new T[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
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

        private static Tcn[] GenerateGenericWithClassAndNewRestrictionArray<Tcn>(int size) where Tcn : class, new()
        {
            Tcn[] array = new Tcn[] { null, default(Tcn), new Tcn() };
            Tcn[] result = new Tcn[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static TC[] GenerateGenericWithSubClassRestrictionArray<TC>(int size) where TC : C
        {
            TC[] array = new TC[] { null, default(TC), (TC)new C() };
            TC[] result = new TC[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = array[i % array.Length];
            }

            return result;
        }

        private static TCn[] GenerateGenericWithSubClassAndNewRestrictionArray<TCn>(int size) where TCn : C, new()
        {
            TCn[] array = new TCn[] { null, default(TCn), new TCn(), (TCn)new C() };
            TCn[] result = new TCn[size];
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

        #region Check length expression

        private static void CheckBoolArrayLengthExpression(bool[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(bool[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckByteArrayLengthExpression(byte[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(byte[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckCustomArrayLengthExpression(C[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(C[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckCharArrayLengthExpression(char[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(char[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckCustom2ArrayLengthExpression(D[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(D[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckDecimalArrayLengthExpression(decimal[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(decimal[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckDelegateArrayLengthExpression(Delegate[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Delegate[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckDoubleArrayLengthExpression(double[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(double[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckEnumArrayLengthExpression(E[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(E[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckEnumLongArrayLengthExpression(El[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(El[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckFloatArrayLengthExpression(float[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(float[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckFuncArrayLengthExpression(Func<object>[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Func<object>[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckInterfaceArrayLengthExpression(I[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(I[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckIEquatableArrayLengthExpression(IEquatable<C>[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(IEquatable<C>[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckIEquatable2ArrayLengthExpression(IEquatable<D>[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(IEquatable<D>[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckIntArrayLengthExpression(int[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(int[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckLongArrayLengthExpression(long[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(long[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckObjectArrayLengthExpression(object[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(object[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructArrayLengthExpression(S[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(S[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckSByteArrayLengthExpression(sbyte[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(sbyte[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructWithStringArrayLengthExpression(Sc[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Sc[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructWithStringAndStructArrayLengthExpression(Scs[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Scs[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckShortArrayLengthExpression(short[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(short[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructWithTwoFieldsArrayLengthExpression(Sp[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Sp[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructWithValueArrayLengthExpression(Ss[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Ss[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckStringArrayLengthExpression(string[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(string[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckUIntArrayLengthExpression(uint[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(uint[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckULongArrayLengthExpression(ulong[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(ulong[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckUShortArrayLengthExpression(ushort[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(ushort[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericArrayLengthExpression<T>(T[] array, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(T[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithClassRestrictionArrayLengthExpression<Tc>(Tc[] array, bool useInterpreter) where Tc : class
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Tc[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithClassAndNewRestrictionArrayLengthExpression<Tcn>(Tcn[] array, bool useInterpreter) where Tcn : class, new()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Tcn[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithSubClassRestrictionArrayLengthExpression<TC>(TC[] array, bool useInterpreter) where TC : C
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(TC[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithSubClassAndNewRestrictionArrayLengthExpression<TCn>(TCn[] array, bool useInterpreter) where TCn : C, new()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(TCn[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithStructRestrictionArrayLengthExpression<Ts>(Ts[] array, bool useInterpreter) where Ts : struct
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Ts[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(array.Length, f());
        }

        #endregion

        #region ToString

        [Fact]
        public static void ToStringTest()
        {
            UnaryExpression e = Expression.ArrayLength(Expression.Parameter(typeof(int[]), "xs"));
            Assert.Equal("ArrayLength(xs)", e.ToString());
        }

        #endregion

        [Fact]
        public static void NullArray()
        {
            AssertExtensions.Throws<ArgumentNullException>("array", () => Expression.ArrayLength(null));
        }

        [Fact]
        public static void IsNotArray()
        {
            Expression notArray = Expression.Constant(8);
            AssertExtensions.Throws<ArgumentException>("array", () => Expression.ArrayLength(notArray));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ArrayTypeArrayAllowed(bool useInterpreter)
        {
            Array arr = new[] { 1, 2, 3 };
            Func<int> func =
                Expression.Lambda<Func<int>>(Expression.ArrayLength(Expression.Constant(arr))).Compile(useInterpreter);
            Assert.Equal(3, func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ArrayExplicitlyTypeArrayNotAllowed(bool useInterpreter)
        {
            Array arr = new[] { 1, 2, 3 };
            Expression arrayExpression = Expression.Constant(arr, typeof(Array));
            AssertExtensions.Throws<ArgumentException>("array", () => Expression.ArrayLength(arrayExpression));
        }

        [Fact]
        public static void ArrayTypeArrayNotAllowedIfNotSZArray()
        {
            Array arr = new[,] { { 1, 2, 3 }, { 1, 2, 2 } };
            AssertExtensions.Throws<ArgumentException>("array", () => Expression.ArrayLength(Expression.Constant(arr)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        public static void ArrayTypeArrayNotAllowedIfNonZeroBoundArray()
        {
            Array arr = Array.CreateInstance(typeof(int), new[] { 3 }, new[] { -1 });
            AssertExtensions.Throws<ArgumentException>("array", () => Expression.ArrayLength(Expression.Constant(arr)));
        }

        [Fact]
        public static void UnreadableArray()
        {
            Expression array = Expression.Property(null, typeof(Unreadable<int[]>), nameof(Unreadable<int>.WriteOnly));
            AssertExtensions.Throws<ArgumentException>("array", () => Expression.ArrayLength(array));
        }

        public static IEnumerable<object[]> TestArrays()
            => Enumerable.Range(0, 6).Select(i => new object[] {new int[i * i]});

        [Theory, PerCompilationType(nameof(TestArrays))]
        public static void MakeUnaryArrayLength(int[] array, bool useInterpreter)
        {
            Expression<Func<int>> lambda = Expression.Lambda<Func<int>>(
                Expression.MakeUnary(ExpressionType.ArrayLength, Expression.Constant(array), null));
            Func<int> func = lambda.Compile(useInterpreter);
            Assert.Equal(array.Length, func());
        }
    }
}
