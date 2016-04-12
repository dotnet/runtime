// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class NullableArrayBoundsTests
    {
        private const int MaxArraySize = 0X7FEFFFFF;

        #region Test methods

        #region NullableByte sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyBoolArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyByteArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCharArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyDecimalArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyDoubleArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyFloatArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyIntArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyLongArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyStructArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifySByteArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyShortArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyUIntArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyULongArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithNullableByteSize(bool useInterpreter)
        {
            foreach (byte? size in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyUShortArrayWithNullableByteSize(size, useInterpreter);
            }
        }

        #endregion

        #region NullableInt sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyBoolArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyByteArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCharArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyDecimalArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyDoubleArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyFloatArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyLongArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifySByteArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyShortArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyUIntArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyULongArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithNullableIntSize(bool useInterpreter)
        {
            foreach (int? size in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyUShortArrayWithNullableIntSize(size, useInterpreter);
            }
        }

        #endregion

        #region NullableLong sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyBoolArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyByteArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCharArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyDecimalArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyDoubleArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyFloatArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyIntArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifySByteArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyShortArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyUIntArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyULongArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithNullableLongSize(bool useInterpreter)
        {
            foreach (long? size in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyUShortArrayWithNullableLongSize(size, useInterpreter);
            }
        }

        #endregion

        #region NullableSByte sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyBoolArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyByteArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCharArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyDecimalArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyDoubleArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyFloatArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyIntArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyLongArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyShortArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyUIntArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyULongArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithNullableSByteSize(bool useInterpreter)
        {
            foreach (sbyte? size in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyUShortArrayWithNullableSByteSize(size, useInterpreter);
            }
        }

        #endregion

        #region NullableShort sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyBoolArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyByteArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCharArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyDecimalArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyDoubleArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyFloatArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyIntArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyLongArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifySByteArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyUIntArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyULongArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithNullableShortSize(bool useInterpreter)
        {
            foreach (short? size in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyUShortArrayWithNullableShortSize(size, useInterpreter);
            }
        }

        #endregion

        #region NullableUInt sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyBoolArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyByteArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCharArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyDecimalArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyDoubleArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyFloatArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyIntArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyLongArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyStructArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifySByteArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyShortArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyUIntArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyULongArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithNullableUIntSize(bool useInterpreter)
        {
            foreach (uint? size in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyUShortArrayWithNullableUIntSize(size, useInterpreter);
            }
        }

        #endregion

        #region NullableULong sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyBoolArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyByteArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCharArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyDecimalArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyDoubleArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyFloatArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyIntArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyLongArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyStructArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifySByteArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyShortArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyUIntArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyULongArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithNullableULongSize(bool useInterpreter)
        {
            foreach (ulong? size in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyUShortArrayWithNullableULongSize(size, useInterpreter);
            }
        }

        #endregion

        #region NullableUShort sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyBoolArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyByteArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCharArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyDecimalArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyDoubleArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyFloatArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyIntArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyLongArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyStructArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifySByteArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyStructWithStringArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyShortArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyStructWithValueArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyUIntArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyULongArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithNullableUShortSize(bool useInterpreter)
        {
            foreach (ushort? size in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyUShortArrayWithNullableUShortSize(size, useInterpreter);
            }
        }

        #endregion

        #endregion

        #region Verify methods

        #region  verifiers

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

        private static void VerifyBoolArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithNullableByteSize(byte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithNullableIntSize(int? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithNullableLongSize(long? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);
            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithNullableSByteSize(sbyte? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithNullableShortSize(short? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithNullableUIntSize(uint? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithNullableULongSize(ulong? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithNullableUShortSize(ushort? size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #endregion
    }
}
