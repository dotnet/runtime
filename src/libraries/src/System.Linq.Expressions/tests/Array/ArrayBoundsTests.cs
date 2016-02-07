// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ArrayBoundsTests
    {
        private class BogusCollection<T> : IList<T>
        {
            public T this[int index]
            {
                get { return default(T); }

                set { throw new NotSupportedException(); }
            }

            public int Count
            {
                get { return -1; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public void Add(T item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(T item)
            {
                return false;
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
            }

            public IEnumerator<T> GetEnumerator()
            {
                return Enumerable.Empty<T>().GetEnumerator();
            }

            public int IndexOf(T item)
            {
                return -1;
            }

            public void Insert(int index, T item)
            {
                throw new NotSupportedException();
            }

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class BogusReadOnlyCollection<T> : ReadOnlyCollection<T>
        {
            public BogusReadOnlyCollection()
                : base(new BogusCollection<T>())
            {

            }
        }

        #region Test methods

        #region Byte sized arrays

        [Fact]
        public static void CheckBoolArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyBoolArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCharArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyDecimalArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyDoubleArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyFloatArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyIntArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyLongArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyStructArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifySByteArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyStructWithStringArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyShortArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyStructWithValueArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyUIntArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyULongArrayWithByteSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithByteSize()
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyUShortArrayWithByteSize(size);
            }
        }

        #endregion

        #region Int sized arrays

        [Fact]
        public static void CheckBoolArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyBoolArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyByteArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCharArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyDecimalArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyDoubleArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyFloatArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyLongArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifySByteArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithStringArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyShortArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithValueArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyUIntArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyULongArrayWithIntSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithIntSize()
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyUShortArrayWithIntSize(size);
            }
        }

        #endregion

        #region Long sized arrays

        [Fact]
        public static void CheckBoolArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyBoolArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyByteArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCharArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyDecimalArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyDoubleArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyFloatArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyIntArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifySByteArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithStringArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyShortArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithValueArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyUIntArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyULongArrayWithLongSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithLongSize()
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyUShortArrayWithLongSize(size);
            }
        }

        #endregion

        #region SByte sized arrays

        [Fact]
        public static void CheckBoolArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyBoolArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyByteArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCharArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyDecimalArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyDoubleArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyFloatArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyIntArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyLongArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithStringArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyShortArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithValueArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyUIntArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyULongArrayWithSByteSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithSByteSize()
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyUShortArrayWithSByteSize(size);
            }
        }

        #endregion

        #region Short sized arrays

        [Fact]
        public static void CheckBoolArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyBoolArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyByteArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCharArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyDecimalArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyDoubleArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyFloatArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyIntArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyLongArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifySByteArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithStringArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithValueArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyUIntArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyULongArrayWithShortSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithShortSize()
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyUShortArrayWithShortSize(size);
            }
        }

        #endregion

        #region UInt sized arrays

        [Fact]
        public static void CheckBoolArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyBoolArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyByteArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCharArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyDecimalArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyDoubleArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyFloatArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyIntArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyLongArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyStructArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifySByteArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyStructWithStringArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyShortArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyStructWithValueArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyULongArrayWithUIntSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithUIntSize()
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUShortArrayWithUIntSize(size);
            }
        }

        #endregion

        #region ULong sized arrays

        [Fact]
        public static void CheckBoolArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyBoolArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyByteArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCharArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyDecimalArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyDoubleArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyFloatArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyIntArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyLongArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyStructArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifySByteArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyStructWithStringArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyShortArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyStructWithValueArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyUIntArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongArrayWithULongSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithULongSize()
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyUShortArrayWithULongSize(size);
            }
        }

        #endregion

        #region UShort sized arrays

        [Fact]
        public static void CheckBoolArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyBoolArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckByteArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyByteArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckCharArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCharArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckDecimalArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyDecimalArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckDoubleArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyDoubleArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckFloatArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyFloatArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckIntArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyIntArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckLongArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyLongArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyStructArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckSByteArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifySByteArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyStructWithStringArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndValueArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckShortArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyShortArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyStructWithValueArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckUIntArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUIntArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckULongArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyULongArrayWithUShortSize(size);
            }
        }

        [Fact]
        public static void CheckUShortArrayWithUShortSize()
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortArrayWithUShortSize(size);
            }
        }

        #endregion

        [Fact]
        public static void ThrowOnNegativeSizedCollection()
        {
            // This is an obscure case, and it doesn't much matter what is thrown, as long as is thrown before such
            // an edge case could cause more obscure damage. A class derived from ReadOnlyCollection is used to catch
            // assumptions that such a type is safe.
            Assert.ThrowsAny<Exception>(() => Expression.NewArrayBounds(typeof(int), new BogusReadOnlyCollection<Expression>()));
        }

        #endregion

        #region Verify methods

        #region  verifiers

        private static void VerifyBoolArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyByteArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyCharArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyDecimalArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyDoubleArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyFloatArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyIntArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyLongArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyStructArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifySByteArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyStructWithStringArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyStructWithStringAndValueArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyShortArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyStructWithTwoValuesArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyStructWithValueArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyUIntArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyULongArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyUShortArrayWithByteSize(byte size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(byte))),
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

        private static void VerifyBoolArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyByteArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyCharArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyDecimalArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyDoubleArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyFloatArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyIntArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyLongArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyStructArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifySByteArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyStructWithStringArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyStructWithStringAndValueArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyShortArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyStructWithTwoValuesArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyStructWithValueArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyUIntArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyULongArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyUShortArrayWithIntSize(int size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(int))),
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

        private static void VerifyBoolArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyByteArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyCharArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyDecimalArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyDoubleArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyFloatArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyIntArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyLongArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyStructArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifySByteArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyStructWithStringArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyStructWithStringAndValueArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyShortArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyStructWithTwoValuesArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyStructWithValueArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyUIntArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyULongArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyUShortArrayWithLongSize(long size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(long))),
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

        private static void VerifyBoolArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyByteArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyCharArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyDecimalArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyDoubleArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyFloatArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyIntArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyLongArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyStructArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifySByteArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyStructWithStringArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyStructWithStringAndValueArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyShortArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyStructWithTwoValuesArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyStructWithValueArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyUIntArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyULongArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyUShortArrayWithSByteSize(sbyte size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(sbyte))),
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

        private static void VerifyBoolArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyByteArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyCharArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyDecimalArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyDoubleArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyFloatArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyIntArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyLongArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyStructArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifySByteArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyStructWithStringArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyStructWithStringAndValueArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyShortArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyStructWithTwoValuesArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyStructWithValueArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyUIntArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyULongArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyUShortArrayWithShortSize(short size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(short))),
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

        private static void VerifyBoolArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyByteArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyCharArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyDecimalArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyDoubleArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyFloatArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyIntArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyLongArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyStructArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifySByteArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyStructWithStringArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyStructWithStringAndValueArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyShortArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyStructWithTwoValuesArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyStructWithValueArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyUIntArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyULongArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyUShortArrayWithUIntSize(uint size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(uint))),
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

        private static void VerifyBoolArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyByteArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyCharArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyDecimalArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyDoubleArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyFloatArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyIntArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyLongArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyStructArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifySByteArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyStructWithStringArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyStructWithStringAndValueArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyShortArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyStructWithTwoValuesArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyStructWithValueArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyUIntArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyULongArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyUShortArrayWithULongSize(ulong size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(ulong))),
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

        private static void VerifyBoolArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyByteArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyCharArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyDecimalArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyDoubleArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyFloatArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyIntArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyLongArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyStructArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifySByteArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyStructWithStringArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyStructWithStringAndValueArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyShortArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyStructWithTwoValuesArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyStructWithValueArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyUIntArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyULongArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(ushort))),
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

        private static void VerifyUShortArrayWithUShortSize(ushort size)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(ushort))),
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
