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
        private const int MaxArraySize = 0X7FEFFFFF;

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

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyBoolArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCharArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyDecimalArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyDoubleArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyFloatArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyIntArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyLongArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyStructArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifySByteArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyStructWithStringArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyShortArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyStructWithValueArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyUIntArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyULongArrayWithByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithByteSize(bool useInterpreter)
        {
            foreach (byte size in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyUShortArrayWithByteSize(size, useInterpreter);
            }
        }

        #endregion

        #region Int sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyBoolArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyByteArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCharArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyDecimalArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyDoubleArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyFloatArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyLongArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifySByteArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithStringArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyShortArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyStructWithValueArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyUIntArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyULongArrayWithIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithIntSize(bool useInterpreter)
        {
            foreach (int size in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyUShortArrayWithIntSize(size, useInterpreter);
            }
        }

        #endregion

        #region Long sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyBoolArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyByteArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCharArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyDecimalArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyDoubleArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyFloatArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyIntArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifySByteArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithStringArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyShortArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyStructWithValueArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyUIntArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyULongArrayWithLongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithLongSize(bool useInterpreter)
        {
            foreach (long size in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyUShortArrayWithLongSize(size, useInterpreter);
            }
        }

        #endregion

        #region SByte sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyBoolArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyByteArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCharArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyDecimalArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyDoubleArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyFloatArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyIntArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyLongArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithStringArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyShortArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyStructWithValueArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyUIntArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyULongArrayWithSByteSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithSByteSize(bool useInterpreter)
        {
            foreach (sbyte size in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyUShortArrayWithSByteSize(size, useInterpreter);
            }
        }

        #endregion

        #region Short sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyBoolArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyByteArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCharArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyDecimalArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyDoubleArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyFloatArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyIntArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyLongArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifySByteArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithStringArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyStructWithValueArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyUIntArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyULongArrayWithShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithShortSize(bool useInterpreter)
        {
            foreach (short size in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyUShortArrayWithShortSize(size, useInterpreter);
            }
        }

        #endregion

        #region UInt sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyBoolArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyByteArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCharArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyDecimalArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyDoubleArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyFloatArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyIntArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyLongArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyStructArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifySByteArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyStructWithStringArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyShortArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyStructWithValueArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyULongArrayWithUIntSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithUIntSize(bool useInterpreter)
        {
            foreach (uint size in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUShortArrayWithUIntSize(size, useInterpreter);
            }
        }

        #endregion

        #region ULong sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyBoolArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyByteArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCharArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyDecimalArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyDoubleArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyFloatArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyIntArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyLongArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyStructArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifySByteArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyStructWithStringArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyShortArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyStructWithValueArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyUIntArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongArrayWithULongSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithULongSize(bool useInterpreter)
        {
            foreach (ulong size in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyUShortArrayWithULongSize(size, useInterpreter);
            }
        }

        #endregion

        #region UShort sized arrays

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyBoolArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyByteArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCharArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyDecimalArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyDoubleArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyFloatArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyIntArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyLongArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyStructArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifySByteArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyStructWithStringArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndValueArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyStructWithStringAndValueArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyShortArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyStructWithTwoValuesArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyStructWithValueArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUIntArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyULongArrayWithUShortSize(size, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayWithUShortSize(bool useInterpreter)
        {
            foreach (ushort size in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortArrayWithUShortSize(size, useInterpreter);
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

        private static void VerifyBoolArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithByteSize(byte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithIntSize(int size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithLongSize(long size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithSByteSize(sbyte size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithShortSize(short size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithUIntSize(uint size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithULongSize(ulong size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayBounds(typeof(bool),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyByteArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayBounds(typeof(byte),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyCharArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayBounds(typeof(char),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDecimalArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayBounds(typeof(decimal),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyDoubleArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayBounds(typeof(double),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyFloatArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayBounds(typeof(float),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyIntArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayBounds(typeof(int),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyLongArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayBounds(typeof(long),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayBounds(typeof(S),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifySByteArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayBounds(typeof(sbyte),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayBounds(typeof(Sc),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithStringAndValueArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayBounds(typeof(Scs),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyShortArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayBounds(typeof(short),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithTwoValuesArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayBounds(typeof(Sp),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyStructWithValueArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayBounds(typeof(Ss),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUIntArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayBounds(typeof(uint),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyULongArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayBounds(typeof(ulong),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        private static void VerifyUShortArrayWithUShortSize(ushort size, bool useInterpreter)
        {
            // generate the expression
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayBounds(typeof(ushort),
                        Expression.Constant(size, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);

            VerifyArrayGenerator(f, size);
        }

        #endregion

        #endregion

        [Fact]
        public static void NullType()
        {
            Assert.Throws<ArgumentNullException>("type", () => Expression.NewArrayBounds(null, Expression.Constant(2)));
        }

        [Fact]
        public static void VoidType()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.NewArrayBounds(typeof(void), Expression.Constant(2)));
        }

        [Fact]
        public static void NullBounds()
        {
            Assert.Throws<ArgumentNullException>("bounds", () => Expression.NewArrayBounds(typeof(int), default(Expression[])));
            Assert.Throws<ArgumentNullException>("bounds", () => Expression.NewArrayBounds(typeof(int), default(IEnumerable<Expression>)));
        }

        [Fact]
        public static void NoBounds()
        {
            Assert.Throws<ArgumentException>("bounds", () => Expression.NewArrayBounds(typeof(int)));
        }

        [Fact]
        public static void NullBound()
        {
            Assert.Throws<ArgumentNullException>("bounds[0]", () => Expression.NewArrayBounds(typeof(int), new Expression[] { null, null }));
            Assert.Throws<ArgumentNullException>("bounds[0]", () => Expression.NewArrayBounds(typeof(int), new List<Expression> { null, null }));
        }

        [Fact]
        public static void NonIntegralBounds()
        {
            Assert.Throws<ArgumentException>("bounds[0]", () => Expression.NewArrayBounds(typeof(int), Expression.Constant(2.0)));
        }

        [Fact]
        public static void ByRefType()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.NewArrayBounds(typeof(int).MakeByRefType(), Expression.Constant(2)));
        }

        [Fact]
        public static void PointerType()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.NewArrayBounds(typeof(int).MakePointerType(), Expression.Constant(2)));
        }

        [Fact]
        public static void GenericType()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.NewArrayBounds(typeof(List<>), Expression.Constant(2)));
        }

        [Fact]
        public static void TypeContainsGenericParameters()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.NewArrayBounds(typeof(List<>.Enumerator), Expression.Constant(2)));
            Assert.Throws<ArgumentException>("type", () => Expression.NewArrayBounds(typeof(List<>).MakeGenericType(typeof(List<>)), Expression.Constant(2)));
        }
    }
}
