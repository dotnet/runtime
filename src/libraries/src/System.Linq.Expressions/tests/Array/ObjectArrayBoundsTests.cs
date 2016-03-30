// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ObjectArrayBoundsTests
    {
        private const int MaxArraySize = 0X7FEFFFFF;

        #region Test methods

        #region Byte sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCustomArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCustom2ArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyDelegateArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyEnumArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyLongEnumArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyFuncArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyInterfaceArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyIEquatableCustomArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyObjectArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyStringArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithByteSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithByteSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithByteSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithByteSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithByteSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithByteSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithByteSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithByteSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithByteSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region Int sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCustomArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCustom2ArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyDelegateArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyEnumArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyLongEnumArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyFuncArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyInterfaceArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIEquatableCustomArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyObjectArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStringArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithIntSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithIntSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithIntSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithIntSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithIntSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithIntSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithIntSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithIntSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithIntSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region Long sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCustomArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCustom2ArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyDelegateArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyEnumArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongEnumArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyFuncArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyInterfaceArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyIEquatableCustomArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyObjectArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStringArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithLongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithLongSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithLongSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithLongSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithLongSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithLongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithLongSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithLongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithLongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithLongSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithLongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithLongSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithLongSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithLongSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region SByte sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCustomArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCustom2ArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyDelegateArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyEnumArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyLongEnumArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyFuncArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyInterfaceArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyIEquatableCustomArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyObjectArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStringArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithSByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithSByteSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithSByteSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithSByteSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithSByteSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithSByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithSByteSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithSByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithSByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithSByteSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithSByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithSByteSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithSByteSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithSByteSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region Short sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCustomArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCustom2ArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyDelegateArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyEnumArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyLongEnumArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyFuncArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyInterfaceArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyIEquatableCustomArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyObjectArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStringArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithShortSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithShortSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithShortSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithShortSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithShortSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithShortSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithShortSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithShortSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithShortSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region UInt sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCustomArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCustom2ArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyDelegateArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyEnumArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyLongEnumArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyFuncArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyInterfaceArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyIEquatableCustomArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyObjectArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyStringArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithUIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithUIntSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithUIntSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithUIntSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithUIntSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithUIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithUIntSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithUIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithUIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithUIntSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithUIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithUIntSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithUIntSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithUIntSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region ULong sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCustomArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCustom2ArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyDelegateArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyEnumArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyLongEnumArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyFuncArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyInterfaceArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyIEquatableCustomArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyObjectArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyStringArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithULongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithULongSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithULongSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithULongSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithULongSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithULongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithULongSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithULongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithULongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithULongSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithULongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithULongSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithULongSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithULongSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region UShort sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCustomArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCustom2ArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyDelegateArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyEnumArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyLongEnumArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyFuncArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyInterfaceArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyIEquatableCustomArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyObjectArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyStringArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithUShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithUShortSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithUShortSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithUShortSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithUShortSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithUShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithUShortSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithUShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithUShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithUShortSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithUShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithUShortSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithUShortSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithUShortSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #endregion

        #region Verify methods

        #region  verifiers

        private static void VerifyArrayGenerator<T>(Func<T[]> func, long size)
        {
            if ((ulong)size > int.MaxValue)
            {
                Assert.Throws<OverflowException>(() => func());
            }
            else if (size > MaxArraySize)
            {
                Assert.Throws<OutOfMemoryException>(() => func());
            }
            else
            {
                Assert.Equal(new T[size], func());
            }
        }

        private static void VerifyArrayGenerator<T>(Func<T[]> func, ulong size)
        {
            VerifyArrayGenerator(func, (long)size);
        }

        private static void VerifyCustomArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyCustomArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyCustomArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyCustomArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyCustomArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyCustomArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyCustomArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyCustomArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithByteSize<T>(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithIntSize<T>(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithLongSize<T>(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithSByteSize<T>(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithShortSize<T>(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithUIntSize<T>(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithULongSize<T>(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithUShortSize<T>(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithByteSize<Tc>(byte size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithIntSize<Tc>(int size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithLongSize<Tc>(long size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithSByteSize<Tc>(sbyte size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithShortSize<Tc>(short size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithUIntSize<Tc>(uint size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithULongSize<Tc>(ulong size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithUShortSize<Tc>(ushort size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithByteSize<TC>(byte size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithIntSize<TC>(int size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithLongSize<TC>(long size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithSByteSize<TC>(sbyte size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithShortSize<TC>(short size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithUIntSize<TC>(uint size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithULongSize<TC>(ulong size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithUShortSize<TC>(ushort size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithByteSize<Tcn>(byte size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithIntSize<Tcn>(int size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithLongSize<Tcn>(long size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithSByteSize<Tcn>(sbyte size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithShortSize<Tcn>(short size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithUIntSize<Tcn>(uint size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithULongSize<Tcn>(ulong size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithUShortSize<Tcn>(ushort size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithByteSize<TCn>(byte size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithIntSize<TCn>(int size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithLongSize<TCn>(long size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithSByteSize<TCn>(sbyte size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithShortSize<TCn>(short size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithUIntSize<TCn>(uint size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithULongSize<TCn>(ulong size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithUShortSize<TCn>(ushort size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithByteSize<Ts>(byte size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithIntSize<Ts>(int size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithLongSize<Ts>(long size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithSByteSize<Ts>(sbyte size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithShortSize<Ts>(short size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithUIntSize<Ts>(uint size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithULongSize<Ts>(ulong size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithUShortSize<Ts>(ushort size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #endregion
    }
}
