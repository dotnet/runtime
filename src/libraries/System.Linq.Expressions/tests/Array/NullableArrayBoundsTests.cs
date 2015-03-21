// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Array
{
    public static unsafe class NullableArrayBoundsTests
    {
        #region Test methods

        #region NullableByte sized arrays

        [Fact]
        public static void CheckBoolArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyBoolArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyByteArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCharArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyDecimalArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyDoubleArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyFloatArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyIntArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyLongArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyStructArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifySByteArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyShortArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyUIntArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyULongArrayWithNullableByteSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithNullableByteSize()
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyUShortArrayWithNullableByteSize(size);
            }
        }

        #endregion

        #region NullableInt sized arrays

        [Fact]
        public static void CheckBoolArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyBoolArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyByteArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCharArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyDecimalArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyDoubleArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyFloatArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyLongArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifySByteArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyShortArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyUIntArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyULongArrayWithNullableIntSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithNullableIntSize()
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyUShortArrayWithNullableIntSize(size);
            }
        }

        #endregion

        #region NullableLong sized arrays

        [Fact]
        public static void CheckBoolArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyBoolArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyByteArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCharArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyDecimalArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyDoubleArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyFloatArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyIntArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifySByteArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyShortArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyUIntArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyULongArrayWithNullableLongSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithNullableLongSize()
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyUShortArrayWithNullableLongSize(size);
            }
        }

        #endregion

        #region NullableSByte sized arrays

        [Fact]
        public static void CheckBoolArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyBoolArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyByteArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCharArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyDecimalArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyDoubleArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyFloatArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyIntArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyLongArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyShortArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyUIntArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyULongArrayWithNullableSByteSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithNullableSByteSize()
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyUShortArrayWithNullableSByteSize(size);
            }
        }

        #endregion

        #region NullableShort sized arrays

        [Fact]
        public static void CheckBoolArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyBoolArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyByteArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCharArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyDecimalArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyDoubleArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyFloatArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyIntArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyLongArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifySByteArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyUIntArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyULongArrayWithNullableShortSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithNullableShortSize()
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyUShortArrayWithNullableShortSize(size);
            }
        }

        #endregion

        #region NullableUInt sized arrays

        [Fact]
        public static void CheckBoolArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyBoolArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyByteArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCharArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyDecimalArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyDoubleArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyFloatArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyIntArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyLongArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyStructArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifySByteArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyShortArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyUIntArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyULongArrayWithNullableUIntSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithNullableUIntSize()
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyUShortArrayWithNullableUIntSize(size);
            }
        }

        #endregion

        #region NullableULong sized arrays

        [Fact]
        public static void CheckBoolArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyBoolArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyByteArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCharArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyDecimalArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyDoubleArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyFloatArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyIntArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyLongArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyStructArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifySByteArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyShortArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyUIntArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyULongArrayWithNullableULongSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithNullableULongSize()
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyUShortArrayWithNullableULongSize(size);
            }
        }

        #endregion

        #region NullableUShort sized arrays

        [Fact]
        public static void CheckBoolArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyBoolArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyByteArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCharArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyDecimalArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyDoubleArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyFloatArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyIntArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyLongArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyStructArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifySByteArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyShortArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyUIntArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyULongArrayWithNullableUShortSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithNullableUShortSize()
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyUShortArrayWithNullableUShortSize(size);
            }
        }

        #endregion

        #endregion

        #region Verify methods

        #region  verifiers

        private static void VerifyBoolArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile();

            // get the array
            bool[] result = null;
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
            bool[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new bool[(long)size];
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

        private static void VerifyByteArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile();

            // get the array
            byte[] result = null;
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
            byte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new byte[(long)size];
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

        private static void VerifyCharArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile();

            // get the array
            char[] result = null;
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
            char[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new char[(long)size];
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

        private static void VerifyDecimalArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile();

            // get the array
            decimal[] result = null;
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
            decimal[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new decimal[(long)size];
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

        private static void VerifyDoubleArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile();

            // get the array
            double[] result = null;
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
            double[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new double[(long)size];
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

        private static void VerifyFloatArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile();

            // get the array
            float[] result = null;
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
            float[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new float[(long)size];
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

        private static void VerifyIntArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile();

            // get the array
            int[] result = null;
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
            int[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new int[(long)size];
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

        private static void VerifyLongArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile();

            // get the array
            long[] result = null;
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
            long[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new long[(long)size];
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

        private static void VerifyStructArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();

            // get the array
            S[] result = null;
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
            S[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new S[(long)size];
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

        private static void VerifySByteArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile();

            // get the array
            sbyte[] result = null;
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
            sbyte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new sbyte[(long)size];
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

        private static void VerifyStructWithStringArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile();

            // get the array
            Sc[] result = null;
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
            Sc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sc[(long)size];
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

        private static void VerifyStructWithStringAndValueArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile();

            // get the array
            Scs[] result = null;
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
            Scs[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Scs[(long)size];
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

        private static void VerifyShortArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile();

            // get the array
            short[] result = null;
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
            short[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new short[(long)size];
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

        private static void VerifyStructWithTwoValuesArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile();

            // get the array
            Sp[] result = null;
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
            Sp[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sp[(long)size];
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

        private static void VerifyStructWithValueArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile();

            // get the array
            Ss[] result = null;
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
            Ss[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ss[(long)size];
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

        private static void VerifyUIntArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile();

            // get the array
            uint[] result = null;
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
            uint[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new uint[(long)size];
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

        private static void VerifyULongArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile();

            // get the array
            ulong[] result = null;
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
            ulong[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ulong[(long)size];
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

        private static void VerifyUShortArrayWithNullableByteSize(byte? size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile();

            // get the array
            ushort[] result = null;
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
            ushort[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ushort[(long)size];
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

        #region  verifiers

        private static void VerifyBoolArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile();

            // get the array
            bool[] result = null;
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
            bool[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new bool[(long)size];
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

        private static void VerifyByteArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile();

            // get the array
            byte[] result = null;
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
            byte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new byte[(long)size];
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

        private static void VerifyCharArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile();

            // get the array
            char[] result = null;
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
            char[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new char[(long)size];
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

        private static void VerifyDecimalArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile();

            // get the array
            decimal[] result = null;
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
            decimal[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new decimal[(long)size];
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

        private static void VerifyDoubleArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile();

            // get the array
            double[] result = null;
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
            double[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new double[(long)size];
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

        private static void VerifyFloatArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile();

            // get the array
            float[] result = null;
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
            float[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new float[(long)size];
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

        private static void VerifyIntArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile();

            // get the array
            int[] result = null;
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
            int[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new int[(long)size];
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

        private static void VerifyLongArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile();

            // get the array
            long[] result = null;
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
            long[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new long[(long)size];
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

        private static void VerifyStructArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();

            // get the array
            S[] result = null;
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
            S[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new S[(long)size];
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

        private static void VerifySByteArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile();

            // get the array
            sbyte[] result = null;
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
            sbyte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new sbyte[(long)size];
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

        private static void VerifyStructWithStringArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile();

            // get the array
            Sc[] result = null;
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
            Sc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sc[(long)size];
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

        private static void VerifyStructWithStringAndValueArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile();

            // get the array
            Scs[] result = null;
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
            Scs[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Scs[(long)size];
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

        private static void VerifyShortArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile();

            // get the array
            short[] result = null;
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
            short[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new short[(long)size];
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

        private static void VerifyStructWithTwoValuesArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile();

            // get the array
            Sp[] result = null;
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
            Sp[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sp[(long)size];
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

        private static void VerifyStructWithValueArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile();

            // get the array
            Ss[] result = null;
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
            Ss[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ss[(long)size];
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

        private static void VerifyUIntArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile();

            // get the array
            uint[] result = null;
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
            uint[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new uint[(long)size];
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

        private static void VerifyULongArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile();

            // get the array
            ulong[] result = null;
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
            ulong[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ulong[(long)size];
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

        private static void VerifyUShortArrayWithNullableIntSize(int? size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile();

            // get the array
            ushort[] result = null;
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
            ushort[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ushort[(long)size];
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

        #region  verifiers

        private static void VerifyBoolArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile();

            // get the array
            bool[] result = null;
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
            bool[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new bool[(long)size];
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

        private static void VerifyByteArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile();

            // get the array
            byte[] result = null;
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
            byte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new byte[(long)size];
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

        private static void VerifyCharArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile();

            // get the array
            char[] result = null;
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
            char[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new char[(long)size];
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

        private static void VerifyDecimalArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile();

            // get the array
            decimal[] result = null;
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
            decimal[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new decimal[(long)size];
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

        private static void VerifyDoubleArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile();

            // get the array
            double[] result = null;
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
            double[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new double[(long)size];
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

        private static void VerifyFloatArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile();

            // get the array
            float[] result = null;
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
            float[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new float[(long)size];
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

        private static void VerifyIntArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile();

            // get the array
            int[] result = null;
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
            int[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new int[(long)size];
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

        private static void VerifyLongArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile();

            // get the array
            long[] result = null;
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
            long[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new long[(long)size];
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

        private static void VerifyStructArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();

            // get the array
            S[] result = null;
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
            S[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new S[(long)size];
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

        private static void VerifySByteArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile();

            // get the array
            sbyte[] result = null;
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
            sbyte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new sbyte[(long)size];
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

        private static void VerifyStructWithStringArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile();

            // get the array
            Sc[] result = null;
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
            Sc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sc[(long)size];
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

        private static void VerifyStructWithStringAndValueArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile();

            // get the array
            Scs[] result = null;
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
            Scs[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Scs[(long)size];
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

        private static void VerifyShortArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile();

            // get the array
            short[] result = null;
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
            short[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new short[(long)size];
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

        private static void VerifyStructWithTwoValuesArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile();

            // get the array
            Sp[] result = null;
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
            Sp[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sp[(long)size];
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

        private static void VerifyStructWithValueArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile();

            // get the array
            Ss[] result = null;
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
            Ss[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ss[(long)size];
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

        private static void VerifyUIntArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile();

            // get the array
            uint[] result = null;
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
            uint[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new uint[(long)size];
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

        private static void VerifyULongArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile();

            // get the array
            ulong[] result = null;
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
            ulong[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ulong[(long)size];
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

        private static void VerifyUShortArrayWithNullableLongSize(long? size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile();

            // get the array
            ushort[] result = null;
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
            ushort[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ushort[(long)size];
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

        #region  verifiers

        private static void VerifyBoolArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile();

            // get the array
            bool[] result = null;
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
            bool[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new bool[(long)size];
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

        private static void VerifyByteArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile();

            // get the array
            byte[] result = null;
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
            byte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new byte[(long)size];
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

        private static void VerifyCharArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile();

            // get the array
            char[] result = null;
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
            char[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new char[(long)size];
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

        private static void VerifyDecimalArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile();

            // get the array
            decimal[] result = null;
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
            decimal[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new decimal[(long)size];
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

        private static void VerifyDoubleArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile();

            // get the array
            double[] result = null;
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
            double[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new double[(long)size];
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

        private static void VerifyFloatArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile();

            // get the array
            float[] result = null;
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
            float[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new float[(long)size];
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

        private static void VerifyIntArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile();

            // get the array
            int[] result = null;
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
            int[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new int[(long)size];
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

        private static void VerifyLongArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile();

            // get the array
            long[] result = null;
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
            long[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new long[(long)size];
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

        private static void VerifyStructArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();

            // get the array
            S[] result = null;
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
            S[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new S[(long)size];
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

        private static void VerifySByteArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile();

            // get the array
            sbyte[] result = null;
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
            sbyte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new sbyte[(long)size];
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

        private static void VerifyStructWithStringArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile();

            // get the array
            Sc[] result = null;
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
            Sc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sc[(long)size];
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

        private static void VerifyStructWithStringAndValueArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile();

            // get the array
            Scs[] result = null;
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
            Scs[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Scs[(long)size];
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

        private static void VerifyShortArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile();

            // get the array
            short[] result = null;
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
            short[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new short[(long)size];
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

        private static void VerifyStructWithTwoValuesArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile();

            // get the array
            Sp[] result = null;
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
            Sp[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sp[(long)size];
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

        private static void VerifyStructWithValueArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile();

            // get the array
            Ss[] result = null;
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
            Ss[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ss[(long)size];
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

        private static void VerifyUIntArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile();

            // get the array
            uint[] result = null;
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
            uint[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new uint[(long)size];
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

        private static void VerifyULongArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile();

            // get the array
            ulong[] result = null;
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
            ulong[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ulong[(long)size];
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

        private static void VerifyUShortArrayWithNullableSByteSize(sbyte? size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile();

            // get the array
            ushort[] result = null;
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
            ushort[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ushort[(long)size];
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

        #region  verifiers

        private static void VerifyBoolArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile();

            // get the array
            bool[] result = null;
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
            bool[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new bool[(long)size];
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

        private static void VerifyByteArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile();

            // get the array
            byte[] result = null;
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
            byte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new byte[(long)size];
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

        private static void VerifyCharArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile();

            // get the array
            char[] result = null;
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
            char[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new char[(long)size];
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

        private static void VerifyDecimalArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile();

            // get the array
            decimal[] result = null;
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
            decimal[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new decimal[(long)size];
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

        private static void VerifyDoubleArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile();

            // get the array
            double[] result = null;
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
            double[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new double[(long)size];
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

        private static void VerifyFloatArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile();

            // get the array
            float[] result = null;
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
            float[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new float[(long)size];
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

        private static void VerifyIntArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile();

            // get the array
            int[] result = null;
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
            int[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new int[(long)size];
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

        private static void VerifyLongArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile();

            // get the array
            long[] result = null;
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
            long[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new long[(long)size];
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

        private static void VerifyStructArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();

            // get the array
            S[] result = null;
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
            S[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new S[(long)size];
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

        private static void VerifySByteArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile();

            // get the array
            sbyte[] result = null;
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
            sbyte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new sbyte[(long)size];
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

        private static void VerifyStructWithStringArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile();

            // get the array
            Sc[] result = null;
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
            Sc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sc[(long)size];
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

        private static void VerifyStructWithStringAndValueArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile();

            // get the array
            Scs[] result = null;
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
            Scs[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Scs[(long)size];
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

        private static void VerifyShortArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile();

            // get the array
            short[] result = null;
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
            short[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new short[(long)size];
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

        private static void VerifyStructWithTwoValuesArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile();

            // get the array
            Sp[] result = null;
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
            Sp[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sp[(long)size];
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

        private static void VerifyStructWithValueArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile();

            // get the array
            Ss[] result = null;
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
            Ss[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ss[(long)size];
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

        private static void VerifyUIntArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile();

            // get the array
            uint[] result = null;
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
            uint[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new uint[(long)size];
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

        private static void VerifyULongArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile();

            // get the array
            ulong[] result = null;
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
            ulong[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ulong[(long)size];
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

        private static void VerifyUShortArrayWithNullableShortSize(short? size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile();

            // get the array
            ushort[] result = null;
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
            ushort[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ushort[(long)size];
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

        #region  verifiers

        private static void VerifyBoolArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile();

            // get the array
            bool[] result = null;
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
            bool[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new bool[(long)size];
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

        private static void VerifyByteArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile();

            // get the array
            byte[] result = null;
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
            byte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new byte[(long)size];
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

        private static void VerifyCharArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile();

            // get the array
            char[] result = null;
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
            char[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new char[(long)size];
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

        private static void VerifyDecimalArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile();

            // get the array
            decimal[] result = null;
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
            decimal[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new decimal[(long)size];
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

        private static void VerifyDoubleArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile();

            // get the array
            double[] result = null;
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
            double[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new double[(long)size];
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

        private static void VerifyFloatArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile();

            // get the array
            float[] result = null;
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
            float[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new float[(long)size];
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

        private static void VerifyIntArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile();

            // get the array
            int[] result = null;
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
            int[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new int[(long)size];
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

        private static void VerifyLongArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile();

            // get the array
            long[] result = null;
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
            long[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new long[(long)size];
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

        private static void VerifyStructArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();

            // get the array
            S[] result = null;
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
            S[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new S[(long)size];
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

        private static void VerifySByteArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile();

            // get the array
            sbyte[] result = null;
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
            sbyte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new sbyte[(long)size];
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

        private static void VerifyStructWithStringArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile();

            // get the array
            Sc[] result = null;
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
            Sc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sc[(long)size];
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

        private static void VerifyStructWithStringAndValueArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile();

            // get the array
            Scs[] result = null;
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
            Scs[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Scs[(long)size];
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

        private static void VerifyShortArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile();

            // get the array
            short[] result = null;
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
            short[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new short[(long)size];
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

        private static void VerifyStructWithTwoValuesArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile();

            // get the array
            Sp[] result = null;
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
            Sp[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sp[(long)size];
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

        private static void VerifyStructWithValueArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile();

            // get the array
            Ss[] result = null;
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
            Ss[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ss[(long)size];
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

        private static void VerifyUIntArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile();

            // get the array
            uint[] result = null;
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
            uint[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new uint[(long)size];
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

        private static void VerifyULongArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile();

            // get the array
            ulong[] result = null;
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
            ulong[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ulong[(long)size];
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

        private static void VerifyUShortArrayWithNullableUIntSize(uint? size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile();

            // get the array
            ushort[] result = null;
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
            ushort[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ushort[(long)size];
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

        #region  verifiers

        private static void VerifyBoolArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile();

            // get the array
            bool[] result = null;
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
            bool[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new bool[(long)size];
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

        private static void VerifyByteArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile();

            // get the array
            byte[] result = null;
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
            byte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new byte[(long)size];
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

        private static void VerifyCharArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile();

            // get the array
            char[] result = null;
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
            char[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new char[(long)size];
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

        private static void VerifyDecimalArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile();

            // get the array
            decimal[] result = null;
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
            decimal[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new decimal[(long)size];
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

        private static void VerifyDoubleArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile();

            // get the array
            double[] result = null;
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
            double[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new double[(long)size];
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

        private static void VerifyFloatArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile();

            // get the array
            float[] result = null;
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
            float[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new float[(long)size];
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

        private static void VerifyIntArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile();

            // get the array
            int[] result = null;
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
            int[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new int[(long)size];
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

        private static void VerifyLongArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile();

            // get the array
            long[] result = null;
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
            long[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new long[(long)size];
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

        private static void VerifyStructArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();

            // get the array
            S[] result = null;
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
            S[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new S[(long)size];
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

        private static void VerifySByteArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile();

            // get the array
            sbyte[] result = null;
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
            sbyte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new sbyte[(long)size];
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

        private static void VerifyStructWithStringArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile();

            // get the array
            Sc[] result = null;
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
            Sc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sc[(long)size];
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

        private static void VerifyStructWithStringAndValueArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile();

            // get the array
            Scs[] result = null;
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
            Scs[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Scs[(long)size];
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

        private static void VerifyShortArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile();

            // get the array
            short[] result = null;
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
            short[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new short[(long)size];
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

        private static void VerifyStructWithTwoValuesArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile();

            // get the array
            Sp[] result = null;
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
            Sp[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sp[(long)size];
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

        private static void VerifyStructWithValueArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile();

            // get the array
            Ss[] result = null;
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
            Ss[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ss[(long)size];
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

        private static void VerifyUIntArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile();

            // get the array
            uint[] result = null;
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
            uint[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new uint[(long)size];
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

        private static void VerifyULongArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile();

            // get the array
            ulong[] result = null;
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
            ulong[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ulong[(long)size];
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

        private static void VerifyUShortArrayWithNullableULongSize(ulong? size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile();

            // get the array
            ushort[] result = null;
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
            ushort[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ushort[(long)size];
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

        #region  verifiers

        private static void VerifyBoolArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile();

            // get the array
            bool[] result = null;
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
            bool[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new bool[(long)size];
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

        private static void VerifyByteArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile();

            // get the array
            byte[] result = null;
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
            byte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new byte[(long)size];
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

        private static void VerifyCharArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile();

            // get the array
            char[] result = null;
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
            char[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new char[(long)size];
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

        private static void VerifyDecimalArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile();

            // get the array
            decimal[] result = null;
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
            decimal[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new decimal[(long)size];
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

        private static void VerifyDoubleArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile();

            // get the array
            double[] result = null;
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
            double[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new double[(long)size];
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

        private static void VerifyFloatArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile();

            // get the array
            float[] result = null;
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
            float[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new float[(long)size];
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

        private static void VerifyIntArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile();

            // get the array
            int[] result = null;
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
            int[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new int[(long)size];
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

        private static void VerifyLongArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile();

            // get the array
            long[] result = null;
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
            long[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new long[(long)size];
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

        private static void VerifyStructArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();

            // get the array
            S[] result = null;
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
            S[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new S[(long)size];
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

        private static void VerifySByteArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile();

            // get the array
            sbyte[] result = null;
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
            sbyte[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new sbyte[(long)size];
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

        private static void VerifyStructWithStringArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile();

            // get the array
            Sc[] result = null;
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
            Sc[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sc[(long)size];
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

        private static void VerifyStructWithStringAndValueArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile();

            // get the array
            Scs[] result = null;
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
            Scs[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Scs[(long)size];
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

        private static void VerifyShortArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile();

            // get the array
            short[] result = null;
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
            short[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new short[(long)size];
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

        private static void VerifyStructWithTwoValuesArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile();

            // get the array
            Sp[] result = null;
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
            Sp[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Sp[(long)size];
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

        private static void VerifyStructWithValueArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile();

            // get the array
            Ss[] result = null;
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
            Ss[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new Ss[(long)size];
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

        private static void VerifyUIntArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile();

            // get the array
            uint[] result = null;
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
            uint[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new uint[(long)size];
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

        private static void VerifyULongArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile();

            // get the array
            ulong[] result = null;
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
            ulong[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ulong[(long)size];
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

        private static void VerifyUShortArrayWithNullableUShortSize(ushort? size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile();

            // get the array
            ushort[] result = null;
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
            ushort[] expected = null;
            Exception expectedEx = null;
            try
            {
                expected = new ushort[(long)size];
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

        #endregion
    }
}
