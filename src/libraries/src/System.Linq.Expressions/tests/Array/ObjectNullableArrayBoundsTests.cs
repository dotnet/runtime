// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Array
{
    public class ObjectNullableArrayBoundsTests
    {
        #region NullableByte sized arrays

        [Fact]
        public static void CheckCustomArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCustomArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckCustom2ArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCustom2ArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckDelegateArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyDelegateArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckEnumArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyEnumArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckLongEnumArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyLongEnumArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckFuncArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyFuncArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckInterfaceArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyInterfaceArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustomArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustom2ArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckObjectArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyObjectArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckStringArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyStringArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfCustomWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithNullableByteSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfEnumWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithNullableByteSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfObjectWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithNullableByteSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithNullableByteSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericArrayWithNullableByteSize<Scs>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableByteSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableByteSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableByteSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableByteSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableByteSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableByteSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableByteSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableByteSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableByteSize<Scs>(size);
            }
        }

        #endregion

        #region NullableInt sized arrays

        [Fact]
        public static void CheckCustomArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCustomArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckCustom2ArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCustom2ArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckDelegateArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyDelegateArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckEnumArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyEnumArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckLongEnumArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyLongEnumArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckFuncArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyFuncArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckInterfaceArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyInterfaceArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustomArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustom2ArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckObjectArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyObjectArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckStringArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStringArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfCustomWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithNullableIntSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfEnumWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithNullableIntSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfObjectWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithNullableIntSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithNullableIntSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericArrayWithNullableIntSize<Scs>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableIntSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableIntSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableIntSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableIntSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableIntSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableIntSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableIntSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableIntSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableIntSize<Scs>(size);
            }
        }

        #endregion

        #region NullableLong sized arrays

        [Fact]
        public static void CheckCustomArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCustomArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckCustom2ArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCustom2ArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckDelegateArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyDelegateArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckEnumArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyEnumArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckLongEnumArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongEnumArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckFuncArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyFuncArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckInterfaceArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyInterfaceArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustomArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustom2ArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckObjectArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyObjectArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckStringArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStringArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfCustomWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithNullableLongSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfEnumWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithNullableLongSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfObjectWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithNullableLongSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithNullableLongSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericArrayWithNullableLongSize<Scs>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableLongSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableLongSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableLongSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableLongSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableLongSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableLongSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableLongSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableLongSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableLongSize<Scs>(size);
            }
        }

        #endregion

        #region NullableSByte sized arrays

        [Fact]
        public static void CheckCustomArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCustomArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckCustom2ArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCustom2ArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckDelegateArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyDelegateArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckEnumArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyEnumArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckLongEnumArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyLongEnumArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckFuncArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyFuncArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckInterfaceArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyInterfaceArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustomArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustom2ArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckObjectArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyObjectArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckStringArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStringArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfCustomWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithNullableSByteSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfEnumWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithNullableSByteSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfObjectWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithNullableSByteSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithNullableSByteSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericArrayWithNullableSByteSize<Scs>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableSByteSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableSByteSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableSByteSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableSByteSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableSByteSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableSByteSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableSByteSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableSByteSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableSByteSize<Scs>(size);
            }
        }

        #endregion

        #region NullableShort sized arrays

        [Fact]
        public static void CheckCustomArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCustomArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckCustom2ArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCustom2ArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckDelegateArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyDelegateArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckEnumArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyEnumArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckLongEnumArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyLongEnumArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckFuncArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyFuncArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckInterfaceArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyInterfaceArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustomArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustom2ArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckObjectArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyObjectArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckStringArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStringArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfCustomWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithNullableShortSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfEnumWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithNullableShortSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfObjectWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithNullableShortSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithNullableShortSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericArrayWithNullableShortSize<Scs>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableShortSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableShortSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableShortSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableShortSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableShortSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableShortSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableShortSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableShortSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableShortSize<Scs>(size);
            }
        }

        #endregion

        #region NullableUInt sized arrays

        [Fact]
        public static void CheckCustomArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCustomArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckCustom2ArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCustom2ArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckDelegateArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyDelegateArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckEnumArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyEnumArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckLongEnumArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyLongEnumArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckFuncArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyFuncArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckInterfaceArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyInterfaceArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustomArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustom2ArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckObjectArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyObjectArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckStringArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyStringArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfCustomWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithNullableUIntSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfEnumWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithNullableUIntSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfObjectWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithNullableUIntSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithNullableUIntSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericArrayWithNullableUIntSize<Scs>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableUIntSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableUIntSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableUIntSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableUIntSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableUIntSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableUIntSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableUIntSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableUIntSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableUIntSize<Scs>(size);
            }
        }

        #endregion

        #region NullableULong sized arrays

        [Fact]
        public static void CheckCustomArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCustomArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckCustom2ArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCustom2ArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckDelegateArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyDelegateArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckEnumArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyEnumArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckLongEnumArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyLongEnumArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckFuncArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyFuncArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckInterfaceArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyInterfaceArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustomArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustom2ArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckObjectArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyObjectArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckStringArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyStringArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfCustomWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithNullableULongSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfEnumWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithNullableULongSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfObjectWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithNullableULongSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithNullableULongSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericArrayWithNullableULongSize<Scs>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableULongSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableULongSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableULongSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableULongSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableULongSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableULongSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableULongSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableULongSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableULongSize<Scs>(size);
            }
        }

        #endregion

        #region NullableUShort sized arrays

        [Fact]
        public static void CheckCustomArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCustomArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckCustom2ArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCustom2ArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckDelegateArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyDelegateArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckEnumArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyEnumArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckLongEnumArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyLongEnumArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckFuncArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyFuncArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckInterfaceArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyInterfaceArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustomArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyIEquatableCustomArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckIEquatableCustom2ArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyIEquatableCustom2ArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckObjectArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyObjectArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckStringArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyStringArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfCustomWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithNullableUShortSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfEnumWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithNullableUShortSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfObjectWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithNullableUShortSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithNullableUShortSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericArrayOfStructWithStringAndValueWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericArrayWithNullableUShortSize<Scs>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfCustomWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableUShortSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionArrayOfObjectWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithClassRestrictionArrayWithNullableUShortSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassRestrictionArrayOfCustomWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithSubClassRestrictionArrayWithNullableUShortSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfCustomWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableUShortSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionArrayOfObjectWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithClassAndNewRestrictionArrayWithNullableUShortSize<object>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithSubClassAndNewRestrictionArrayOfCustomWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableUShortSize<C>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfEnumWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableUShortSize<E>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableUShortSize<S>(size);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionArrayOfStructWithStringAndValueWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyGenericWithStructRestrictionArrayWithNullableUShortSize<Scs>(size);
            }
        }

        #endregion

        #region NullableByte verifiers

        private static void VerifyCustomArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // get the array
            C[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            C[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new C[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyCustom2ArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile();

            // get the array
            D[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            D[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new D[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyDelegateArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile();

            // get the array
            Delegate[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Delegate[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Delegate[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyEnumArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile();

            // get the array
            E[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            E[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new E[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyLongEnumArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile();

            // get the array
            El[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            El[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new El[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyFuncArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile();

            // get the array
            Func<object>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Func<object>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Func<object>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyInterfaceArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile();

            // get the array
            I[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            I[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new I[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustomArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile();

            // get the array
            IEquatable<C>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<C>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<C>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile();

            // get the array
            IEquatable<D>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<D>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<D>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyObjectArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // get the array
            object[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            object[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new object[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyStringArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile();

            // get the array
            string[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            string[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new string[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        #endregion
        #region NullableInt verifiers

        private static void VerifyCustomArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // get the array
            C[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            C[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new C[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyCustom2ArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile();

            // get the array
            D[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            D[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new D[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyDelegateArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile();

            // get the array
            Delegate[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Delegate[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Delegate[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyEnumArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile();

            // get the array
            E[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            E[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new E[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyLongEnumArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile();

            // get the array
            El[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            El[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new El[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyFuncArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile();

            // get the array
            Func<object>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Func<object>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Func<object>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyInterfaceArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile();

            // get the array
            I[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            I[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new I[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustomArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile();

            // get the array
            IEquatable<C>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<C>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<C>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile();

            // get the array
            IEquatable<D>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<D>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<D>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyObjectArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // get the array
            object[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            object[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new object[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyStringArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile();

            // get the array
            string[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            string[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new string[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        #endregion
        #region NullableLong verifiers

        private static void VerifyCustomArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // get the array
            C[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            C[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new C[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyCustom2ArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile();

            // get the array
            D[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            D[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new D[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyDelegateArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile();

            // get the array
            Delegate[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Delegate[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Delegate[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyEnumArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile();

            // get the array
            E[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            E[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new E[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyLongEnumArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile();

            // get the array
            El[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            El[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new El[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyFuncArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile();

            // get the array
            Func<object>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Func<object>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Func<object>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyInterfaceArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile();

            // get the array
            I[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            I[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new I[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustomArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile();

            // get the array
            IEquatable<C>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<C>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<C>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile();

            // get the array
            IEquatable<D>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<D>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<D>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyObjectArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // get the array
            object[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            object[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new object[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyStringArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile();

            // get the array
            string[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            string[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new string[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        #endregion
        #region NullableSByte verifiers

        private static void VerifyCustomArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // get the array
            C[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            C[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new C[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyCustom2ArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile();

            // get the array
            D[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            D[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new D[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyDelegateArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile();

            // get the array
            Delegate[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Delegate[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Delegate[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyEnumArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile();

            // get the array
            E[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            E[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new E[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyLongEnumArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile();

            // get the array
            El[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            El[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new El[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyFuncArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile();

            // get the array
            Func<object>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Func<object>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Func<object>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyInterfaceArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile();

            // get the array
            I[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            I[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new I[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustomArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile();

            // get the array
            IEquatable<C>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<C>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<C>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile();

            // get the array
            IEquatable<D>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<D>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<D>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyObjectArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // get the array
            object[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            object[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new object[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyStringArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile();

            // get the array
            string[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            string[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new string[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        #endregion
        #region NullableShort verifiers

        private static void VerifyCustomArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // get the array
            C[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            C[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new C[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyCustom2ArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile();

            // get the array
            D[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            D[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new D[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyDelegateArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile();

            // get the array
            Delegate[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Delegate[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Delegate[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyEnumArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile();

            // get the array
            E[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            E[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new E[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyLongEnumArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile();

            // get the array
            El[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            El[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new El[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyFuncArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile();

            // get the array
            Func<object>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Func<object>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Func<object>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyInterfaceArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile();

            // get the array
            I[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            I[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new I[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustomArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile();

            // get the array
            IEquatable<C>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<C>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<C>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile();

            // get the array
            IEquatable<D>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<D>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<D>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyObjectArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // get the array
            object[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            object[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new object[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyStringArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile();

            // get the array
            string[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            string[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new string[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        #endregion
        #region NullableUInt verifiers

        private static void VerifyCustomArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // get the array
            C[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            C[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new C[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyCustom2ArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile();

            // get the array
            D[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            D[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new D[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyDelegateArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile();

            // get the array
            Delegate[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Delegate[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Delegate[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyEnumArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile();

            // get the array
            E[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            E[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new E[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyLongEnumArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile();

            // get the array
            El[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            El[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new El[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyFuncArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile();

            // get the array
            Func<object>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Func<object>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Func<object>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyInterfaceArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile();

            // get the array
            I[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            I[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new I[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustomArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile();

            // get the array
            IEquatable<C>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<C>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<C>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile();

            // get the array
            IEquatable<D>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<D>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<D>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyObjectArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // get the array
            object[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            object[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new object[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyStringArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile();

            // get the array
            string[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            string[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new string[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        #endregion
        #region NullableULong verifiers

        private static void VerifyCustomArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // get the array
            C[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            C[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new C[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyCustom2ArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile();

            // get the array
            D[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            D[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new D[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyDelegateArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile();

            // get the array
            Delegate[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Delegate[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Delegate[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyEnumArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile();

            // get the array
            E[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            E[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new E[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyLongEnumArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile();

            // get the array
            El[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            El[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new El[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyFuncArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile();

            // get the array
            Func<object>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Func<object>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Func<object>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyInterfaceArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile();

            // get the array
            I[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            I[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new I[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustomArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile();

            // get the array
            IEquatable<C>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<C>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<C>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile();

            // get the array
            IEquatable<D>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<D>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<D>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyObjectArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // get the array
            object[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            object[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new object[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyStringArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile();

            // get the array
            string[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            string[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new string[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        #endregion
        #region NullableUShort verifiers

        private static void VerifyCustomArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayBounds(typeof(C),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // get the array
            C[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            C[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new C[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyCustom2ArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayBounds(typeof(D),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile();

            // get the array
            D[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            D[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new D[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyDelegateArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayBounds(typeof(Delegate),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile();

            // get the array
            Delegate[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Delegate[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Delegate[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyEnumArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayBounds(typeof(E),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile();

            // get the array
            E[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            E[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new E[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyLongEnumArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayBounds(typeof(El),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile();

            // get the array
            El[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            El[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new El[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyFuncArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayBounds(typeof(Func<object>),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile();

            // get the array
            Func<object>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Func<object>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Func<object>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyInterfaceArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayBounds(typeof(I),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile();

            // get the array
            I[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            I[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new I[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustomArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<C>),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile();

            // get the array
            IEquatable<C>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<C>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<C>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyIEquatableCustom2ArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayBounds(typeof(IEquatable<D>),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile();

            // get the array
            IEquatable<D>[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            IEquatable<D>[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new IEquatable<D>[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyObjectArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayBounds(typeof(object),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // get the array
            object[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            object[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new object[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyStringArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayBounds(typeof(string),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile();

            // get the array
            string[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            string[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new string[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericArrayWithNullableByteSize<T>(byte? size)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile();

            // get the array
            T[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            T[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new T[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericArrayWithNullableIntSize<T>(int? size)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile();

            // get the array
            T[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            T[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new T[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericArrayWithNullableLongSize<T>(long? size)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile();

            // get the array
            T[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            T[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new T[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericArrayWithNullableSByteSize<T>(sbyte? size)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile();

            // get the array
            T[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            T[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new T[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericArrayWithNullableShortSize<T>(short? size)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile();

            // get the array
            T[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            T[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new T[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericArrayWithNullableUIntSize<T>(uint? size)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile();

            // get the array
            T[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            T[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new T[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericArrayWithNullableULongSize<T>(ulong? size)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile();

            // get the array
            T[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            T[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new T[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericArrayWithNullableUShortSize<T>(ushort? size)
        {
            // generate the expression
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayBounds(typeof(T),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile();

            // get the array
            T[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            T[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new T[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableByteSize<Tc>(byte? size) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile();

            // get the array
            Tc[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tc[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableIntSize<Tc>(int? size) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile();

            // get the array
            Tc[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tc[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableLongSize<Tc>(long? size) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile();

            // get the array
            Tc[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tc[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableSByteSize<Tc>(sbyte? size) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile();

            // get the array
            Tc[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tc[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableShortSize<Tc>(short? size) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile();

            // get the array
            Tc[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tc[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableUIntSize<Tc>(uint? size) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile();

            // get the array
            Tc[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tc[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableULongSize<Tc>(ulong? size) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile();

            // get the array
            Tc[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tc[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassRestrictionArrayWithNullableUShortSize<Tc>(ushort? size) where Tc : class
        {
            // generate the expression
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayBounds(typeof(Tc),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile();

            // get the array
            Tc[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tc[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableByteSize<TC>(byte? size) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile();

            // get the array
            TC[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TC[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TC[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableIntSize<TC>(int? size) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile();

            // get the array
            TC[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TC[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TC[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableLongSize<TC>(long? size) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile();

            // get the array
            TC[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TC[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TC[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableSByteSize<TC>(sbyte? size) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile();

            // get the array
            TC[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TC[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TC[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableShortSize<TC>(short? size) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile();

            // get the array
            TC[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TC[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TC[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableUIntSize<TC>(uint? size) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile();

            // get the array
            TC[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TC[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TC[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableULongSize<TC>(ulong? size) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile();

            // get the array
            TC[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TC[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TC[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassRestrictionArrayWithNullableUShortSize<TC>(ushort? size) where TC : C
        {
            // generate the expression
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayBounds(typeof(TC),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile();

            // get the array
            TC[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TC[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TC[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableByteSize<Tcn>(byte? size) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile();

            // get the array
            Tcn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tcn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tcn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableIntSize<Tcn>(int? size) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile();

            // get the array
            Tcn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tcn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tcn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableLongSize<Tcn>(long? size) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile();

            // get the array
            Tcn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tcn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tcn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableSByteSize<Tcn>(sbyte? size) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile();

            // get the array
            Tcn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tcn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tcn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableShortSize<Tcn>(short? size) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile();

            // get the array
            Tcn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tcn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tcn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableUIntSize<Tcn>(uint? size) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile();

            // get the array
            Tcn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tcn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tcn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableULongSize<Tcn>(ulong? size) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile();

            // get the array
            Tcn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tcn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tcn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayWithNullableUShortSize<Tcn>(ushort? size) where Tcn : class, new()
        {
            // generate the expression
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayBounds(typeof(Tcn),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile();

            // get the array
            Tcn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Tcn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Tcn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableByteSize<TCn>(byte? size) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile();

            // get the array
            TCn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TCn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TCn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableIntSize<TCn>(int? size) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile();

            // get the array
            TCn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TCn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TCn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableLongSize<TCn>(long? size) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile();

            // get the array
            TCn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TCn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TCn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableSByteSize<TCn>(sbyte? size) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile();

            // get the array
            TCn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TCn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TCn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableShortSize<TCn>(short? size) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile();

            // get the array
            TCn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TCn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TCn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableUIntSize<TCn>(uint? size) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile();

            // get the array
            TCn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TCn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TCn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableULongSize<TCn>(ulong? size) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile();

            // get the array
            TCn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TCn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TCn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayWithNullableUShortSize<TCn>(ushort? size) where TCn : C, new()
        {
            // generate the expression
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayBounds(typeof(TCn),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile();

            // get the array
            TCn[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            TCn[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new TCn[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableByteSize<Ts>(byte? size) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile();

            // get the array
            Ts[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Ts[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ts[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableIntSize<Ts>(int? size) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile();

            // get the array
            Ts[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Ts[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ts[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableLongSize<Ts>(long? size) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile();

            // get the array
            Ts[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Ts[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ts[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableSByteSize<Ts>(sbyte? size) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile();

            // get the array
            Ts[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Ts[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ts[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableShortSize<Ts>(short? size) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile();

            // get the array
            Ts[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Ts[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ts[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableUIntSize<Ts>(uint? size) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile();

            // get the array
            Ts[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Ts[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ts[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableULongSize<Ts>(ulong? size) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile();

            // get the array
            Ts[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Ts[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ts[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        private static void VerifyGenericWithStructRestrictionArrayWithNullableUShortSize<Ts>(ushort? size) where Ts : struct
        {
            // generate the expression
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayBounds(typeof(Ts),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile();

            // get the array
            Ts[] result = null;
            Exception creationEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                creationEx = ex;
            }

            // generate expected array
            Ts[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ts[(long)size];
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }

            // if one failed, verify the other did, too
            if (creationEx != null || expectedEx != null)
            {
                Assert.NotNull(creationEx);
                Assert.NotNull(expectedEx);
                Assert.Equal(expectedEx.GetType(), creationEx.GetType());
            }
            else
            {
                // otherwise, verify the contents array
                Assert.Equal(expected.Length, result.Length);
                for (int i = 0; i < result.Length; i++)
                {
                    Assert.Equal(expected[i], result[i]);
                }
            }
        }

        #endregion
    }
}
