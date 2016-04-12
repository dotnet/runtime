// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ObjectNullableArrayBoundsTests
    {
        private const int MaxArraySize = 0X7FEFFFFF;

        #region NullableByte sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCustomArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCustom2ArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyDelegateArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyEnumArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyLongEnumArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyFuncArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyInterfaceArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyObjectArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyStringArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithNullableByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithNullableByteSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithNullableByteSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithNullableByteSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithNullableByteSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableByteSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableByteSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableByteSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableByteSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableByteSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region NullableInt sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCustomArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCustom2ArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyDelegateArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyEnumArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyLongEnumArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyFuncArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyInterfaceArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyObjectArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStringArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithNullableIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithNullableIntSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithNullableIntSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithNullableIntSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithNullableIntSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableIntSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableIntSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableIntSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableIntSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableIntSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region NullableLong sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCustomArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCustom2ArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyDelegateArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyEnumArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongEnumArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyFuncArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyInterfaceArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyObjectArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStringArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithNullableLongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithNullableLongSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithNullableLongSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithNullableLongSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithNullableLongSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableLongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableLongSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableLongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableLongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableLongSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableLongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableLongSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableLongSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableLongSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region NullableSByte sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCustomArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCustom2ArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyDelegateArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyEnumArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyLongEnumArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyFuncArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyInterfaceArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyObjectArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStringArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithNullableSByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithNullableSByteSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithNullableSByteSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithNullableSByteSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithNullableSByteSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableSByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableSByteSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableSByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableSByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableSByteSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableSByteSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableSByteSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableSByteSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableSByteSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region NullableShort sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCustomArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCustom2ArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyDelegateArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyEnumArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyLongEnumArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyFuncArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyInterfaceArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyObjectArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStringArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithNullableShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithNullableShortSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithNullableShortSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithNullableShortSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithNullableShortSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableShortSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableShortSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableShortSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableShortSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableShortSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region NullableUInt sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCustomArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCustom2ArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyDelegateArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyEnumArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyLongEnumArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyFuncArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyInterfaceArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyObjectArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyStringArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithNullableUIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithNullableUIntSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithNullableUIntSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithNullableUIntSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithNullableUIntSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableUIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableUIntSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableUIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableUIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableUIntSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableUIntSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableUIntSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableUIntSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableUIntSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region NullableULong sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCustomArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCustom2ArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyDelegateArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyEnumArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyLongEnumArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyFuncArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyInterfaceArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyObjectArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyStringArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithNullableULongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithNullableULongSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithNullableULongSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithNullableULongSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithNullableULongSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableULongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableULongSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableULongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableULongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableULongSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableULongSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableULongSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableULongSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableULongSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region NullableUShort sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCustomArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCustom2ArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyDelegateArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyEnumArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyLongEnumArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyFuncArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyInterfaceArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2ArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyObjectArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyStringArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfCustomWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithNullableUShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfEnumWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithNullableUShortSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfObjectWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithNullableUShortSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithNullableUShortSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithNullableUShortSize<Scs>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableUShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableUShortSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableUShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableUShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableUShortSize<object>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableUShortSize<C>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableUShortSize<E>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableUShortSize<S>(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableUShortSize<Scs>(size, useInterpreter);
            }
        }

        #endregion

        #region NullableByte verifiers

        private static void VerifyArrayGenerator<T>(Func<T[]> func, long? size)
        {
            if (!size.HasValue)
            {
                Assert.Throws<InvalidOperationException>(() => func());
            }
            else if ((ulong)size.GetValueOrDefault() > int.MaxValue)
            {
                Assert.Throws<OverflowException>(() => func());
            }
            else if (size.GetValueOrDefault() > MaxArraySize)
            {
                Assert.Throws<OutOfMemoryException>(() => func());
            }
            else
            {
                Assert.Equal(new T[size.GetValueOrDefault()], func());
            }
        }

        private static void VerifyArrayGenerator<T>(Func<T[]> func, ulong? size)
        {
            VerifyArrayGenerator(func, (long?)size);
        }

        private static void VerifyCustomArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion
        #region NullableInt verifiers

        private static void VerifyCustomArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion
        #region NullableLong verifiers

        private static void VerifyCustomArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion
        #region NullableSByte verifiers

        private static void VerifyCustomArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion
        #region NullableShort verifiers

        private static void VerifyCustomArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion
        #region NullableUInt verifiers

        private static void VerifyCustomArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion
        #region NullableULong verifiers

        private static void VerifyCustomArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion
        #region NullableUShort verifiers

        private static void VerifyCustomArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCustom2ArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDelegateArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyEnumArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongEnumArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFuncArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyInterfaceArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustomArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyObjectArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStringArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithNullableByteSize<T>(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithNullableIntSize<T>(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithNullableLongSize<T>(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithNullableSByteSize<T>(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithNullableShortSize<T>(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithNullableUIntSize<T>(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithNullableULongSize<T>(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericArrayWithNullableUShortSize<T>(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableByteSize<Tc>(byte? size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableIntSize<Tc>(int? size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableLongSize<Tc>(long? size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableSByteSize<Tc>(sbyte? size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableShortSize<Tc>(short? size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableUIntSize<Tc>(uint? size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableULongSize<Tc>(ulong? size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableUShortSize<Tc>(ushort? size, bool useInterpreter) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableByteSize<TC>(byte? size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableIntSize<TC>(int? size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableLongSize<TC>(long? size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);

        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableSByteSize<TC>(sbyte? size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableShortSize<TC>(short? size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableUIntSize<TC>(uint? size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableULongSize<TC>(ulong? size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableUShortSize<TC>(ushort? size, bool useInterpreter) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableByteSize<Tcn>(byte? size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableIntSize<Tcn>(int? size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableLongSize<Tcn>(long? size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableSByteSize<Tcn>(sbyte? size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableShortSize<Tcn>(short? size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableUIntSize<Tcn>(uint? size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableULongSize<Tcn>(ulong? size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableUShortSize<Tcn>(ushort? size, bool useInterpreter) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableByteSize<TCn>(byte? size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableIntSize<TCn>(int? size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableLongSize<TCn>(long? size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableSByteSize<TCn>(sbyte? size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableShortSize<TCn>(short? size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableUIntSize<TCn>(uint? size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableULongSize<TCn>(ulong? size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableUShortSize<TCn>(ushort? size, bool useInterpreter) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableByteSize<Ts>(byte? size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableIntSize<Ts>(int? size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableLongSize<Ts>(long? size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableSByteSize<Ts>(sbyte? size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableShortSize<Ts>(short? size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableUIntSize<Ts>(uint? size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableULongSize<Ts>(ulong? size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableUShortSize<Ts>(ushort? size, bool useInterpreter) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion
    }
}
