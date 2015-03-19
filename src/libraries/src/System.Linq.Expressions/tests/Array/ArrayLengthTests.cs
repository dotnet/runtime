// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Array
{
    public static unsafe class ArrayLengthTests
    {
        #region Bool tests

        [Fact]
        public static void CheckBoolArrayLengthTest()
        {
            CheckBoolArrayLengthExpression(GenerateBoolArray(0));
            CheckBoolArrayLengthExpression(GenerateBoolArray(1));
            CheckBoolArrayLengthExpression(GenerateBoolArray(5));
        }

        [Fact]
        public static void CheckExceptionBoolArrayLengthTest()
        {
            CheckExceptionBoolArrayLength(null);
        }

        #endregion

        #region Byte tests

        [Fact]
        public static void CheckByteArrayLengthTest()
        {
            CheckByteArrayLengthExpression(GenerateByteArray(0));
            CheckByteArrayLengthExpression(GenerateByteArray(1));
            CheckByteArrayLengthExpression(GenerateByteArray(5));
        }

        [Fact]
        public static void CheckExceptionByteArrayLengthTest()
        {
            CheckExceptionByteArrayLength(null);
        }

        #endregion

        #region Custom tests

        [Fact]
        public static void CheckCustomArrayLengthTest()
        {
            CheckCustomArrayLengthExpression(GenerateCustomArray(0));
            CheckCustomArrayLengthExpression(GenerateCustomArray(1));
            CheckCustomArrayLengthExpression(GenerateCustomArray(5));
        }

        [Fact]
        public static void CheckExceptionCustomArrayLengthTest()
        {
            CheckExceptionCustomArrayLength(null);
        }

        #endregion

        #region Char tests

        [Fact]
        public static void CheckCharArrayLengthTest()
        {
            CheckCharArrayLengthExpression(GenerateCharArray(0));
            CheckCharArrayLengthExpression(GenerateCharArray(1));
            CheckCharArrayLengthExpression(GenerateCharArray(5));
        }

        [Fact]
        public static void CheckExceptionCharArrayLengthTest()
        {
            CheckExceptionCharArrayLength(null);
        }

        #endregion

        #region Custom2 tests

        [Fact]
        public static void CheckCustom2ArrayLengthTest()
        {
            CheckCustom2ArrayLengthExpression(GenerateCustom2Array(0));
            CheckCustom2ArrayLengthExpression(GenerateCustom2Array(1));
            CheckCustom2ArrayLengthExpression(GenerateCustom2Array(5));
        }

        [Fact]
        public static void CheckExceptionCustom2ArrayLengthTest()
        {
            CheckExceptionCustom2ArrayLength(null);
        }

        #endregion

        #region Decimal tests

        [Fact]
        public static void CheckDecimalArrayLengthTest()
        {
            CheckDecimalArrayLengthExpression(GenerateDecimalArray(0));
            CheckDecimalArrayLengthExpression(GenerateDecimalArray(1));
            CheckDecimalArrayLengthExpression(GenerateDecimalArray(5));
        }

        [Fact]
        public static void CheckExceptionDecimalArrayLengthTest()
        {
            CheckExceptionDecimalArrayLength(null);
        }

        #endregion

        #region Delegate tests

        [Fact]
        public static void CheckDelegateArrayLengthTest()
        {
            CheckDelegateArrayLengthExpression(GenerateDelegateArray(0));
            CheckDelegateArrayLengthExpression(GenerateDelegateArray(1));
            CheckDelegateArrayLengthExpression(GenerateDelegateArray(5));
        }

        [Fact]
        public static void CheckExceptionDelegateArrayLengthTest()
        {
            CheckExceptionDelegateArrayLength(null);
        }

        #endregion

        #region double tests

        [Fact]
        public static void CheckdoubleArrayLengthTest()
        {
            CheckdoubleArrayLengthExpression(GeneratedoubleArray(0));
            CheckdoubleArrayLengthExpression(GeneratedoubleArray(1));
            CheckdoubleArrayLengthExpression(GeneratedoubleArray(5));
        }

        [Fact]
        public static void CheckExceptiondoubleArrayLengthTest()
        {
            CheckExceptiondoubleArrayLength(null);
        }

        #endregion

        #region Enum tests

        [Fact]
        public static void CheckEnumArrayLengthTest()
        {
            CheckEnumArrayLengthExpression(GenerateEnumArray(0));
            CheckEnumArrayLengthExpression(GenerateEnumArray(1));
            CheckEnumArrayLengthExpression(GenerateEnumArray(5));
        }

        [Fact]
        public static void CheckExceptionEnumArrayLengthTest()
        {
            CheckExceptionEnumArrayLength(null);
        }

        #endregion

        #region EnumLong tests

        [Fact]
        public static void CheckEnumLongArrayLengthTest()
        {
            CheckEnumLongArrayLengthExpression(GenerateEnumLongArray(0));
            CheckEnumLongArrayLengthExpression(GenerateEnumLongArray(1));
            CheckEnumLongArrayLengthExpression(GenerateEnumLongArray(5));
        }

        [Fact]
        public static void CheckExceptionEnumLongArrayLengthTest()
        {
            CheckExceptionEnumLongArrayLength(null);
        }

        #endregion

        #region Float tests

        [Fact]
        public static void CheckFloatArrayLengthTest()
        {
            CheckFloatArrayLengthExpression(GenerateFloatArray(0));
            CheckFloatArrayLengthExpression(GenerateFloatArray(1));
            CheckFloatArrayLengthExpression(GenerateFloatArray(5));
        }

        [Fact]
        public static void CheckExceptionFloatArrayLengthTest()
        {
            CheckExceptionFloatArrayLength(null);
        }

        #endregion

        #region Func tests

        [Fact]
        public static void CheckFuncArrayLengthTest()
        {
            CheckFuncArrayLengthExpression(GenerateFuncArray(0));
            CheckFuncArrayLengthExpression(GenerateFuncArray(1));
            CheckFuncArrayLengthExpression(GenerateFuncArray(5));
        }

        [Fact]
        public static void CheckExceptionFuncArrayLengthTest()
        {
            CheckExceptionFuncArrayLength(null);
        }

        #endregion

        #region Interface tests

        [Fact]
        public static void CheckInterfaceArrayLengthTest()
        {
            CheckInterfaceArrayLengthExpression(GenerateInterfaceArray(0));
            CheckInterfaceArrayLengthExpression(GenerateInterfaceArray(1));
            CheckInterfaceArrayLengthExpression(GenerateInterfaceArray(5));
        }

        [Fact]
        public static void CheckExceptionInterfaceArrayLengthTest()
        {
            CheckExceptionInterfaceArrayLength(null);
        }

        #endregion

        #region IEquatable tests

        [Fact]
        public static void CheckIEquatableArrayLengthTest()
        {
            CheckIEquatableArrayLengthExpression(GenerateIEquatableArray(0));
            CheckIEquatableArrayLengthExpression(GenerateIEquatableArray(1));
            CheckIEquatableArrayLengthExpression(GenerateIEquatableArray(5));
        }

        [Fact]
        public static void CheckExceptionIEquatableArrayLengthTest()
        {
            CheckExceptionIEquatableArrayLength(null);
        }

        #endregion

        #region IEquatable2 tests

        [Fact]
        public static void CheckIEquatable2ArrayLengthTest()
        {
            CheckIEquatable2ArrayLengthExpression(GenerateIEquatable2Array(0));
            CheckIEquatable2ArrayLengthExpression(GenerateIEquatable2Array(1));
            CheckIEquatable2ArrayLengthExpression(GenerateIEquatable2Array(5));
        }

        [Fact]
        public static void CheckExceptionIEquatable2ArrayLengthTest()
        {
            CheckExceptionIEquatable2ArrayLength(null);
        }

        #endregion

        #region Int tests

        [Fact]
        public static void CheckIntArrayLengthTest()
        {
            CheckIntArrayLengthExpression(GenerateIntArray(0));
            CheckIntArrayLengthExpression(GenerateIntArray(1));
            CheckIntArrayLengthExpression(GenerateIntArray(5));
        }

        [Fact]
        public static void CheckExceptionIntArrayLengthTest()
        {
            CheckExceptionIntArrayLength(null);
        }

        #endregion

        #region Long tests

        [Fact]
        public static void CheckLongArrayLengthTest()
        {
            CheckLongArrayLengthExpression(GenerateLongArray(0));
            CheckLongArrayLengthExpression(GenerateLongArray(1));
            CheckLongArrayLengthExpression(GenerateLongArray(5));
        }

        [Fact]
        public static void CheckExceptionLongArrayLengthTest()
        {
            CheckExceptionLongArrayLength(null);
        }

        #endregion

        #region Object tests

        [Fact]
        public static void CheckObjectArrayLengthTest()
        {
            CheckObjectArrayLengthExpression(GenerateObjectArray(0));
            CheckObjectArrayLengthExpression(GenerateObjectArray(1));
            CheckObjectArrayLengthExpression(GenerateObjectArray(5));
        }

        [Fact]
        public static void CheckExceptionObjectArrayLengthTest()
        {
            CheckExceptionObjectArrayLength(null);
        }

        #endregion

        #region Struct tests

        [Fact]
        public static void CheckStructArrayLengthTest()
        {
            CheckStructArrayLengthExpression(GenerateStructArray(0));
            CheckStructArrayLengthExpression(GenerateStructArray(1));
            CheckStructArrayLengthExpression(GenerateStructArray(5));
        }

        [Fact]
        public static void CheckExceptionStructArrayLengthTest()
        {
            CheckExceptionStructArrayLength(null);
        }

        #endregion

        #region SByte tests

        [Fact]
        public static void CheckSByteArrayLengthTest()
        {
            CheckSByteArrayLengthExpression(GenerateSByteArray(0));
            CheckSByteArrayLengthExpression(GenerateSByteArray(1));
            CheckSByteArrayLengthExpression(GenerateSByteArray(5));
        }

        [Fact]
        public static void CheckExceptionSByteArrayLengthTest()
        {
            CheckExceptionSByteArrayLength(null);
        }

        #endregion

        #region StructWithString tests

        [Fact]
        public static void CheckStructWithStringArrayLengthTest()
        {
            CheckStructWithStringArrayLengthExpression(GenerateStructWithStringArray(0));
            CheckStructWithStringArrayLengthExpression(GenerateStructWithStringArray(1));
            CheckStructWithStringArrayLengthExpression(GenerateStructWithStringArray(5));
        }

        [Fact]
        public static void CheckExceptionStructWithStringArrayLengthTest()
        {
            CheckExceptionStructWithStringArrayLength(null);
        }

        #endregion

        #region StructWithStringAndStruct tests

        [Fact]
        public static void CheckStructWithStringAndStructArrayLengthTest()
        {
            CheckStructWithStringAndStructArrayLengthExpression(GenerateStructWithStringAndStructArray(0));
            CheckStructWithStringAndStructArrayLengthExpression(GenerateStructWithStringAndStructArray(1));
            CheckStructWithStringAndStructArrayLengthExpression(GenerateStructWithStringAndStructArray(5));
        }

        [Fact]
        public static void CheckExceptionStructWithStringAndStructArrayLengthTest()
        {
            CheckExceptionStructWithStringAndStructArrayLength(null);
        }

        #endregion

        #region Short tests

        [Fact]
        public static void CheckShortArrayLengthTest()
        {
            CheckShortArrayLengthExpression(GenerateShortArray(0));
            CheckShortArrayLengthExpression(GenerateShortArray(1));
            CheckShortArrayLengthExpression(GenerateShortArray(5));
        }

        [Fact]
        public static void CheckExceptionShortArrayLengthTest()
        {
            CheckExceptionShortArrayLength(null);
        }

        #endregion

        #region StructWithTwoFields tests

        [Fact]
        public static void CheckStructWithTwoFieldsArrayLengthTest()
        {
            CheckStructWithTwoFieldsArrayLengthExpression(GenerateStructWithTwoFieldsArray(0));
            CheckStructWithTwoFieldsArrayLengthExpression(GenerateStructWithTwoFieldsArray(1));
            CheckStructWithTwoFieldsArrayLengthExpression(GenerateStructWithTwoFieldsArray(5));
        }

        [Fact]
        public static void CheckExceptionStructWithTwoFieldsArrayLengthTest()
        {
            CheckExceptionStructWithTwoFieldsArrayLength(null);
        }

        #endregion

        #region StructWithValue tests

        [Fact]
        public static void CheckStructWithValueArrayLengthTest()
        {
            CheckStructWithValueArrayLengthExpression(GenerateStructWithValueArray(0));
            CheckStructWithValueArrayLengthExpression(GenerateStructWithValueArray(1));
            CheckStructWithValueArrayLengthExpression(GenerateStructWithValueArray(5));
        }

        [Fact]
        public static void CheckExceptionStructWithValueArrayLengthTest()
        {
            CheckExceptionStructWithValueArrayLength(null);
        }

        #endregion

        #region String tests

        [Fact]
        public static void CheckStringArrayLengthTest()
        {
            CheckStringArrayLengthExpression(GenerateStringArray(0));
            CheckStringArrayLengthExpression(GenerateStringArray(1));
            CheckStringArrayLengthExpression(GenerateStringArray(5));
        }

        [Fact]
        public static void CheckExceptionStringArrayLengthTest()
        {
            CheckExceptionStringArrayLength(null);
        }

        #endregion

        #region UInt tests

        [Fact]
        public static void CheckUIntArrayLengthTest()
        {
            CheckUIntArrayLengthExpression(GenerateUIntArray(0));
            CheckUIntArrayLengthExpression(GenerateUIntArray(1));
            CheckUIntArrayLengthExpression(GenerateUIntArray(5));
        }

        [Fact]
        public static void CheckExceptionUIntArrayLengthTest()
        {
            CheckExceptionUIntArrayLength(null);
        }

        #endregion

        #region ULong tests

        [Fact]
        public static void CheckULongArrayLengthTest()
        {
            CheckULongArrayLengthExpression(GenerateULongArray(0));
            CheckULongArrayLengthExpression(GenerateULongArray(1));
            CheckULongArrayLengthExpression(GenerateULongArray(5));
        }

        [Fact]
        public static void CheckExceptionULongArrayLengthTest()
        {
            CheckExceptionULongArrayLength(null);
        }

        #endregion

        #region UShort tests

        [Fact]
        public static void CheckUShortArrayLengthTest()
        {
            CheckUShortArrayLengthExpression(GenerateUShortArray(0));
            CheckUShortArrayLengthExpression(GenerateUShortArray(1));
            CheckUShortArrayLengthExpression(GenerateUShortArray(5));
        }

        [Fact]
        public static void CheckExceptionUShortArrayLengthTest()
        {
            CheckExceptionUShortArrayLength(null);
        }

        #endregion

        #region Generic tests

        [Fact]
        public static void CheckGenericCustomArrayLengthTest()
        {
            CheckGenericArrayLengthTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericCustomArrayLengthTest()
        {
            CheckExceptionGenericArrayLengthTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericEnumArrayLengthTest()
        {
            CheckGenericArrayLengthTestHelper<E>();
        }

        [Fact]
        public static void CheckExceptionGenericEnumArrayLengthTest()
        {
            CheckExceptionGenericArrayLengthTestHelper<E>();
        }

        [Fact]
        public static void CheckGenericObjectArrayLengthTest()
        {
            CheckGenericArrayLengthTestHelper<object>();
        }

        [Fact]
        public static void CheckExceptionGenericObjectArrayLengthTest()
        {
            CheckExceptionGenericArrayLengthTestHelper<object>();
        }

        [Fact]
        public static void CheckGenericStructArrayLengthTest()
        {
            CheckGenericArrayLengthTestHelper<S>();
        }

        [Fact]
        public static void CheckExceptionGenericStructArrayLengthTest()
        {
            CheckExceptionGenericArrayLengthTestHelper<S>();
        }

        [Fact]
        public static void CheckGenericStructWithStringAndFieldArrayLengthTest()
        {
            CheckGenericArrayLengthTestHelper<Scs>();
        }

        [Fact]
        public static void CheckExceptionGenericStructWithStringAndFieldArrayLengthTest()
        {
            CheckExceptionGenericArrayLengthTestHelper<Scs>();
        }

        [Fact]
        public static void CheckGenericCustomWithClassRestrictionArrayLengthTest()
        {
            CheckGenericWithClassRestrictionArrayLengthTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericCustomWithClassRestrictionArrayLengthTest()
        {
            CheckExceptionGenericWithClassRestrictionArrayLengthTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericObjectWithClassRestrictionArrayLengthTest()
        {
            CheckGenericWithClassRestrictionArrayLengthTestHelper<object>();
        }

        [Fact]
        public static void CheckExceptionGenericObjectWithClassRestrictionArrayLengthTest()
        {
            CheckExceptionGenericWithClassRestrictionArrayLengthTestHelper<object>();
        }

        [Fact]
        public static void CheckGenericCustomWithClassAndNewRestrictionArrayLengthTest()
        {
            CheckGenericWithClassAndNewRestrictionArrayLengthTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericCustomWithClassAndNewRestrictionArrayLengthTest()
        {
            CheckExceptionGenericWithClassAndNewRestrictionArrayLengthTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericObjectWithClassAndNewRestrictionArrayLengthTest()
        {
            CheckGenericWithClassAndNewRestrictionArrayLengthTestHelper<object>();
        }

        [Fact]
        public static void CheckExceptionGenericObjectWithClassAndNewRestrictionArrayLengthTest()
        {
            CheckExceptionGenericWithClassAndNewRestrictionArrayLengthTestHelper<object>();
        }

        [Fact]
        public static void CheckGenericCustomWithSubClassRestrictionArrayLengthTest()
        {
            CheckGenericWithSubClassRestrictionArrayLengthTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericCustomWithSubClassRestrictionArrayLengthTest()
        {
            CheckExceptionGenericWithSubClassRestrictionArrayLengthTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericCustomWithSubClassAndNewRestrictionArrayLengthTest()
        {
            CheckGenericWithSubClassAndNewRestrictionArrayLengthTestHelper<C>();
        }

        [Fact]
        public static void CheckExceptionGenericCustomWithSubClassAndNewRestrictionArrayLengthTest()
        {
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayLengthTestHelper<C>();
        }

        [Fact]
        public static void CheckGenericEnumWithStructRestrictionArrayLengthTest()
        {
            CheckGenericWithStructRestrictionArrayLengthTestHelper<E>();
        }

        [Fact]
        public static void CheckExceptionGenericEnumWithStructRestrictionArrayLengthTest()
        {
            CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<E>();
        }

        [Fact]
        public static void CheckGenericStructWithStructRestrictionArrayLengthTest()
        {
            CheckGenericWithStructRestrictionArrayLengthTestHelper<S>();
        }

        [Fact]
        public static void CheckExceptionGenericStructWithStructRestrictionArrayLengthTest()
        {
            CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<S>();
        }

        [Fact]
        public static void CheckGenericStructWithStringAndFieldWithStructRestrictionArrayLengthTest()
        {
            CheckGenericWithStructRestrictionArrayLengthTestHelper<Scs>();
        }

        [Fact]
        public static void CheckExceptionGenericStructWithStringAndFieldWithStructRestrictionArrayLengthTest()
        {
            CheckExceptionGenericWithStructRestrictionArrayLengthTestHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        public static void CheckGenericArrayLengthTestHelper<T>()
        {
            CheckGenericArrayLengthExpression<T>(GenerateGenericArray<T>(0));
            CheckGenericArrayLengthExpression<T>(GenerateGenericArray<T>(1));
            CheckGenericArrayLengthExpression<T>(GenerateGenericArray<T>(5));
        }

        public static void CheckExceptionGenericArrayLengthTestHelper<T>()
        {
            CheckExceptionGenericArrayLength<T>(null);
        }

        public static void CheckGenericWithClassRestrictionArrayLengthTestHelper<Tc>() where Tc : class
        {
            CheckGenericWithClassRestrictionArrayLengthExpression<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(0));
            CheckGenericWithClassRestrictionArrayLengthExpression<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(1));
            CheckGenericWithClassRestrictionArrayLengthExpression<Tc>(GenerateGenericWithClassRestrictionArray<Tc>(5));
        }

        public static void CheckExceptionGenericWithClassRestrictionArrayLengthTestHelper<Tc>() where Tc : class
        {
            CheckExceptionGenericWithClassRestrictionArrayLength<Tc>(null);
        }

        public static void CheckGenericWithClassAndNewRestrictionArrayLengthTestHelper<Tcn>() where Tcn : class, new()
        {
            CheckGenericWithClassAndNewRestrictionArrayLengthExpression<Tcn>(GenerateGenericWithClassAndNewRestrictionArray<Tcn>(0));
            CheckGenericWithClassAndNewRestrictionArrayLengthExpression<Tcn>(GenerateGenericWithClassAndNewRestrictionArray<Tcn>(1));
            CheckGenericWithClassAndNewRestrictionArrayLengthExpression<Tcn>(GenerateGenericWithClassAndNewRestrictionArray<Tcn>(5));
        }

        public static void CheckExceptionGenericWithClassAndNewRestrictionArrayLengthTestHelper<Tcn>() where Tcn : class, new()
        {
            CheckExceptionGenericWithClassAndNewRestrictionArrayLength<Tcn>(null);
        }

        public static void CheckGenericWithSubClassRestrictionArrayLengthTestHelper<TC>() where TC : C
        {
            CheckGenericWithSubClassRestrictionArrayLengthExpression<TC>(GenerateGenericWithSubClassRestrictionArray<TC>(0));
            CheckGenericWithSubClassRestrictionArrayLengthExpression<TC>(GenerateGenericWithSubClassRestrictionArray<TC>(1));
            CheckGenericWithSubClassRestrictionArrayLengthExpression<TC>(GenerateGenericWithSubClassRestrictionArray<TC>(5));
        }

        public static void CheckExceptionGenericWithSubClassRestrictionArrayLengthTestHelper<TC>() where TC : C
        {
            CheckExceptionGenericWithSubClassRestrictionArrayLength<TC>(null);
        }

        public static void CheckGenericWithSubClassAndNewRestrictionArrayLengthTestHelper<TCn>() where TCn : C, new()
        {
            CheckGenericWithSubClassAndNewRestrictionArrayLengthExpression<TCn>(GenerateGenericWithSubClassAndNewRestrictionArray<TCn>(0));
            CheckGenericWithSubClassAndNewRestrictionArrayLengthExpression<TCn>(GenerateGenericWithSubClassAndNewRestrictionArray<TCn>(1));
            CheckGenericWithSubClassAndNewRestrictionArrayLengthExpression<TCn>(GenerateGenericWithSubClassAndNewRestrictionArray<TCn>(5));
        }

        public static void CheckExceptionGenericWithSubClassAndNewRestrictionArrayLengthTestHelper<TCn>() where TCn : C, new()
        {
            CheckExceptionGenericWithSubClassAndNewRestrictionArrayLength<TCn>(null);
        }

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

        private static void CheckBoolArrayLengthExpression(bool[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(bool[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckByteArrayLengthExpression(byte[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(byte[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckCustomArrayLengthExpression(C[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(C[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckCharArrayLengthExpression(char[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(char[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckCustom2ArrayLengthExpression(D[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(D[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckDecimalArrayLengthExpression(decimal[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(decimal[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckDelegateArrayLengthExpression(Delegate[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Delegate[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckdoubleArrayLengthExpression(double[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(double[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckEnumArrayLengthExpression(E[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(E[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckEnumLongArrayLengthExpression(El[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(El[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckFloatArrayLengthExpression(float[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(float[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckFuncArrayLengthExpression(Func<object>[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Func<object>[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckInterfaceArrayLengthExpression(I[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(I[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckIEquatableArrayLengthExpression(IEquatable<C>[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(IEquatable<C>[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckIEquatable2ArrayLengthExpression(IEquatable<D>[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(IEquatable<D>[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckIntArrayLengthExpression(int[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(int[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckLongArrayLengthExpression(long[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(long[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckObjectArrayLengthExpression(object[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(object[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructArrayLengthExpression(S[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(S[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckSByteArrayLengthExpression(sbyte[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(sbyte[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructWithStringArrayLengthExpression(Sc[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Sc[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructWithStringAndStructArrayLengthExpression(Scs[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Scs[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckShortArrayLengthExpression(short[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(short[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructWithTwoFieldsArrayLengthExpression(Sp[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Sp[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckStructWithValueArrayLengthExpression(Ss[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Ss[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckStringArrayLengthExpression(string[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(string[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckUIntArrayLengthExpression(uint[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(uint[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckULongArrayLengthExpression(ulong[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(ulong[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckUShortArrayLengthExpression(ushort[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(ushort[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericArrayLengthExpression<T>(T[] array)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(T[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithClassRestrictionArrayLengthExpression<Tc>(Tc[] array) where Tc : class
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Tc[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithClassAndNewRestrictionArrayLengthExpression<Tcn>(Tcn[] array) where Tcn : class, new()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Tcn[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithSubClassRestrictionArrayLengthExpression<TC>(TC[] array) where TC : C
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(TC[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithSubClassAndNewRestrictionArrayLengthExpression<TCn>(TCn[] array) where TCn : C, new()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(TCn[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        private static void CheckGenericWithStructRestrictionArrayLengthExpression<Ts>(Ts[] array) where Ts : struct
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ArrayLength(Expression.Constant(array, typeof(Ts[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(array.Length, f());
        }

        #endregion

        #region Check exception array length

        private static void CheckExceptionBoolArrayLength(bool[] array)
        {
            bool success = true;
            try
            {
                CheckBoolArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionByteArrayLength(byte[] array)
        {
            bool success = true;
            try
            {
                CheckByteArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionCustomArrayLength(C[] array)
        {
            bool success = true;
            try
            {
                CheckCustomArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionCharArrayLength(char[] array)
        {
            bool success = true;
            try
            {
                CheckCharArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionCustom2ArrayLength(D[] array)
        {
            bool success = true;
            try
            {
                CheckCustom2ArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionDecimalArrayLength(decimal[] array)
        {
            bool success = true;
            try
            {
                CheckDecimalArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionDelegateArrayLength(Delegate[] array)
        {
            bool success = true;
            try
            {
                CheckDelegateArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptiondoubleArrayLength(double[] array)
        {
            bool success = true;
            try
            {
                CheckdoubleArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionEnumArrayLength(E[] array)
        {
            bool success = true;
            try
            {
                CheckEnumArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionEnumLongArrayLength(El[] array)
        {
            bool success = true;
            try
            {
                CheckEnumLongArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionFloatArrayLength(float[] array)
        {
            bool success = true;
            try
            {
                CheckFloatArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionFuncArrayLength(Func<object>[] array)
        {
            bool success = true;
            try
            {
                CheckFuncArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionInterfaceArrayLength(I[] array)
        {
            bool success = true;
            try
            {
                CheckInterfaceArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionIEquatableArrayLength(IEquatable<C>[] array)
        {
            bool success = true;
            try
            {
                CheckIEquatableArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionIEquatable2ArrayLength(IEquatable<D>[] array)
        {
            bool success = true;
            try
            {
                CheckIEquatable2ArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionIntArrayLength(int[] array)
        {
            bool success = true;
            try
            {
                CheckIntArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionLongArrayLength(long[] array)
        {
            bool success = true;
            try
            {
                CheckLongArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionObjectArrayLength(object[] array)
        {
            bool success = true;
            try
            {
                CheckObjectArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructArrayLength(S[] array)
        {
            bool success = true;
            try
            {
                CheckStructArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionSByteArrayLength(sbyte[] array)
        {
            bool success = true;
            try
            {
                CheckSByteArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructWithStringArrayLength(Sc[] array)
        {
            bool success = true;
            try
            {
                CheckStructWithStringArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructWithStringAndStructArrayLength(Scs[] array)
        {
            bool success = true;
            try
            {
                CheckStructWithStringAndStructArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionShortArrayLength(short[] array)
        {
            bool success = true;
            try
            {
                CheckShortArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructWithTwoFieldsArrayLength(Sp[] array)
        {
            bool success = true;
            try
            {
                CheckStructWithTwoFieldsArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStructWithValueArrayLength(Ss[] array)
        {
            bool success = true;
            try
            {
                CheckStructWithValueArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionStringArrayLength(string[] array)
        {
            bool success = true;
            try
            {
                CheckStringArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionUIntArrayLength(uint[] array)
        {
            bool success = true;
            try
            {
                CheckUIntArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionULongArrayLength(ulong[] array)
        {
            bool success = true;
            try
            {
                CheckULongArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionUShortArrayLength(ushort[] array)
        {
            bool success = true;
            try
            {
                CheckUShortArrayLengthExpression(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericArrayLength<T>(T[] array)
        {
            bool success = true;
            try
            {
                CheckGenericArrayLengthExpression<T>(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithClassRestrictionArrayLength<Tc>(Tc[] array) where Tc : class
        {
            bool success = true;
            try
            {
                CheckGenericWithClassRestrictionArrayLengthExpression<Tc>(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithClassAndNewRestrictionArrayLength<Tcn>(Tcn[] array) where Tcn : class, new()
        {
            bool success = true;
            try
            {
                CheckGenericWithClassAndNewRestrictionArrayLengthExpression<Tcn>(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithSubClassRestrictionArrayLength<TC>(TC[] array) where TC : C
        {
            bool success = true;
            try
            {
                CheckGenericWithSubClassRestrictionArrayLengthExpression<TC>(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithSubClassAndNewRestrictionArrayLength<TCn>(TCn[] array) where TCn : C, new()
        {
            bool success = true;
            try
            {
                CheckGenericWithSubClassAndNewRestrictionArrayLengthExpression<TCn>(array); // expect to fail
                success = false;
            }
            catch
            {
            }

            Assert.True(success);
        }

        private static void CheckExceptionGenericWithStructRestrictionArrayLength<Ts>(Ts[] array) where Ts : struct
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
