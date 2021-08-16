// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Sdk;

namespace System.Numerics.Tests
{
    /// <summary>
    ///  Vector{T} tests that use random number generation and a unified generic test structure
    /// </summary>
    public class GenericVectorTests
    {
        // Static constructor in top-level class\
        static System.Numerics.Vector<float> dummy;
        static GenericVectorTests()
        {
            dummy = System.Numerics.Vector<float>.One;
        }

        #region Constructor Tests

        [Fact]
        public void ConstructorByte() { TestConstructor<byte>(); }
        [Fact]
        public void ConstructorSByte() { TestConstructor<sbyte>(); }
        [Fact]
        public void ConstructorUInt16() { TestConstructor<ushort>(); }
        [Fact]
        public void ConstructorInt16() { TestConstructor<short>(); }
        [Fact]
        public void ConstructorUInt32() { TestConstructor<uint>(); }
        [Fact]
        public void ConstructorInt32() { TestConstructor<int>(); }
        [Fact]
        public void ConstructorUInt64() { TestConstructor<ulong>(); }
        [Fact]
        public void ConstructorInt64() { TestConstructor<long>(); }
        [Fact]
        public void ConstructorSingle() { TestConstructor<float>(); }
        [Fact]
        public void ConstructorDouble() { TestConstructor<double>(); }

        private void TestConstructor<T>() where T : struct
        {
            Assert.Throws<NullReferenceException>(() => new Vector<T>((T[])null));

            T[] values = GenerateRandomValuesForVector<T>();
            var vector = new Vector<T>(values);
            ValidateVector(
                vector,
                (index, val) =>
                {
                    Assert.Equal(values[index], val);
                });
        }

        [Fact]
        public void ConstructorWithOffsetByte() { TestConstructorWithOffset<byte>(); }
        [Fact]
        public void ConstructorWithOffsetSByte() { TestConstructorWithOffset<sbyte>(); }
        [Fact]
        public void ConstructorWithOffsetUInt16() { TestConstructorWithOffset<ushort>(); }
        [Fact]
        public void ConstructorWithOffsetInt16() { TestConstructorWithOffset<short>(); }
        [Fact]
        public void ConstructorWithOffsetUInt32() { TestConstructorWithOffset<uint>(); }
        [Fact]
        public void ConstructorWithOffsetInt32() { TestConstructorWithOffset<int>(); }
        [Fact]
        public void ConstructorWithOffsetUInt64() { TestConstructorWithOffset<ulong>(); }
        [Fact]
        public void ConstructorWithOffsetInt64() { TestConstructorWithOffset<long>(); }
        [Fact]
        public void ConstructorWithOffsetSingle() { TestConstructorWithOffset<float>(); }
        [Fact]
        public void ConstructorWithOffsetDouble() { TestConstructorWithOffset<double>(); }
        private void TestConstructorWithOffset<T>() where T : struct
        {
            Assert.Throws<NullReferenceException>(() => new Vector<T>((T[])null, 0));

            int offsetAmount = Util.GenerateSingleValue<int>(2, 250);
            T[] values = new T[offsetAmount].Concat(GenerateRandomValuesForVector<T>()).ToArray();
            var vector = new Vector<T>(values, offsetAmount);
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(values[index + offsetAmount], val);
                });
        }

        [Fact]
        public void ConstructorConstantValueByte() { TestConstructorConstantValue<byte>(); }
        [Fact]
        public void ConstructorConstantValueSByte() { TestConstructorConstantValue<sbyte>(); }
        [Fact]
        public void ConstructorConstantValueUInt16() { TestConstructorConstantValue<ushort>(); }
        [Fact]
        public void ConstructorConstantValueInt16() { TestConstructorConstantValue<short>(); }
        [Fact]
        public void ConstructorConstantValueUInt32() { TestConstructorConstantValue<uint>(); }
        [Fact]
        public void ConstructorConstantValueInt32() { TestConstructorConstantValue<int>(); }
        [Fact]
        public void ConstructorConstantValueUInt64() { TestConstructorConstantValue<ulong>(); }
        [Fact]
        public void ConstructorConstantValueInt64() { TestConstructorConstantValue<long>(); }
        [Fact]
        public void ConstructorConstantValueSingle() { TestConstructorConstantValue<float>(); }
        [Fact]
        public void ConstructorConstantValueDouble() { TestConstructorConstantValue<double>(); }
        private void TestConstructorConstantValue<T>() where T : struct
        {
            T constantValue = Util.GenerateSingleValue<T>(GetMinValue<T>(), GetMaxValue<T>());
            var vector = new Vector<T>(constantValue);
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(val, constantValue);
                });
        }

        [Fact]
        public void ConstructorDefaultByte() { TestConstructorDefault<byte>(); }
        [Fact]
        public void ConstructorDefaultSByte() { TestConstructorDefault<sbyte>(); }
        [Fact]
        public void ConstructorDefaultUInt16() { TestConstructorDefault<ushort>(); }
        [Fact]
        public void ConstructorDefaultInt16() { TestConstructorDefault<short>(); }
        [Fact]
        public void ConstructorDefaultUInt32() { TestConstructorDefault<uint>(); }
        [Fact]
        public void ConstructorDefaultInt32() { TestConstructorDefault<int>(); }
        [Fact]
        public void ConstructorDefaultUInt64() { TestConstructorDefault<ulong>(); }
        [Fact]
        public void ConstructorDefaultInt64() { TestConstructorDefault<long>(); }
        [Fact]
        public void ConstructorDefaultSingle() { TestConstructorDefault<float>(); }
        [Fact]
        public void ConstructorDefaultDouble() { TestConstructorDefault<double>(); }
        private void TestConstructorDefault<T>() where T : struct
        {
            var vector = new Vector<T>();
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(val, (T)(dynamic)0);
                });
        }

        [Fact]
        public void ConstructorExceptionByte() { TestConstructorArrayTooSmallException<byte>(); }
        [Fact]
        public void ConstructorExceptionSByte() { TestConstructorArrayTooSmallException<sbyte>(); }
        [Fact]
        public void ConstructorExceptionUInt16() { TestConstructorArrayTooSmallException<ushort>(); }
        [Fact]
        public void ConstructorExceptionInt16() { TestConstructorArrayTooSmallException<short>(); }
        [Fact]
        public void ConstructorExceptionUInt32() { TestConstructorArrayTooSmallException<uint>(); }
        [Fact]
        public void ConstructorExceptionInt32() { TestConstructorArrayTooSmallException<int>(); }
        [Fact]
        public void ConstructorExceptionUInt64() { TestConstructorArrayTooSmallException<ulong>(); }
        [Fact]
        public void ConstructorExceptionInt64() { TestConstructorArrayTooSmallException<long>(); }
        [Fact]
        public void ConstructorExceptionSingle() { TestConstructorArrayTooSmallException<float>(); }
        [Fact]
        public void ConstructorExceptionDouble() { TestConstructorArrayTooSmallException<double>(); }
        private void TestConstructorArrayTooSmallException<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>().Skip(1).ToArray();
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var vector = new Vector<T>(values);
            });
        }

        #region Tests for Span based constructor
        [Fact]
        public void ConstructorWithSpanByte() => TestConstructorWithSpan<byte>();
        [Fact]
        public void ConstructorWithSpanSByte() => TestConstructorWithSpan<sbyte>();
        [Fact]
        public void ConstructorWithSpanUInt16() => TestConstructorWithSpan<ushort>();
        [Fact]
        public void ConstructorWithSpanInt16() => TestConstructorWithSpan<short>();
        [Fact]
        public void ConstructorWithSpanUInt32() => TestConstructorWithSpan<uint>();
        [Fact]
        public void ConstructorWithSpanInt32() => TestConstructorWithSpan<int>();
        [Fact]
        public void ConstructorWithSpanUInt64() => TestConstructorWithSpan<ulong>();
        [Fact]
        public void ConstructorWithSpanInt64() => TestConstructorWithSpan<long>();
        [Fact]
        public void ConstructorWithSpanSingle() => TestConstructorWithSpan<float>();
        [Fact]
        public void ConstructorWithSpanDouble() => TestConstructorWithSpan<double>();

        private void TestConstructorWithSpan<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>().ToArray();
            Vector<T> vector;

            // Span<T> ctor
            vector = new Vector<T>(new Span<T>(values));
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(values[index], val);
                });

            // ReadOnlySpan<T> ctor
            vector = new Vector<T>(new ReadOnlySpan<T>(values));
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(values[index], val);
                });

            // ReadOnlySpan<byte> ctor
            vector = new Vector<T>(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(values)));
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(values[index], val);
                });
        }

        [Fact]
        public void ReadOnlySpanBasedConstructorWithLessElements_Byte() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanBasedConstructorWithLessElements<byte>());
        [Fact]
        public void ReadOnlySpanBasedConstructorWithLessElements_SByte() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanBasedConstructorWithLessElements<sbyte>());
        [Fact]
        public void ReadOnlySpanBasedConstructorWithLessElements_UInt16() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanBasedConstructorWithLessElements<ushort>());
        [Fact]
        public void ReadOnlySpanBasedConstructorWithLessElements_Int16() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanBasedConstructorWithLessElements<short>());
        [Fact]
        public void ReadOnlySpanBasedConstructorWithLessElements_UInt32() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanBasedConstructorWithLessElements<uint>());
        [Fact]
        public void ReadOnlySpanBasedConstructorWithLessElements_Int32() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanBasedConstructorWithLessElements<int>());
        [Fact]
        public void ReadOnlySpanBasedConstructorWithLessElements_UInt64() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanBasedConstructorWithLessElements<ulong>());
        [Fact]
        public void ReadOnlySpanBasedConstructorWithLessElements_Int64() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanBasedConstructorWithLessElements<long>());
        [Fact]
        public void ReadOnlySpanBasedConstructorWithLessElements_Single() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanBasedConstructorWithLessElements<float>());
        [Fact]
        public void ReadOnlySpanBasedConstructorWithLessElements_Double() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanBasedConstructorWithLessElements<double>());

        private void TestReadOnlySpanBasedConstructorWithLessElements<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>(Vector<T>.Count - 1).ToArray();
            var vector = new Vector<T>(new ReadOnlySpan<T>(values));
        }

        [Fact]
        public void ReadOnlySpanByteBasedConstructorWithLessElements_Byte() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanByteBasedConstructorWithLessElements<byte>());
        [Fact]
        public void ReadOnlySpanByteBasedConstructorWithLessElements_SByte() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanByteBasedConstructorWithLessElements<sbyte>());
        [Fact]
        public void ReadOnlySpanByteBasedConstructorWithLessElements_UInt16() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanByteBasedConstructorWithLessElements<ushort>());
        [Fact]
        public void ReadOnlySpanByteBasedConstructorWithLessElements_Int16() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanByteBasedConstructorWithLessElements<short>());
        [Fact]
        public void ReadOnlySpanByteBasedConstructorWithLessElements_UInt32() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanByteBasedConstructorWithLessElements<uint>());
        [Fact]
        public void ReadOnlySpanByteBasedConstructorWithLessElements_Int32() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanByteBasedConstructorWithLessElements<int>());
        [Fact]
        public void ReadOnlySpanByteBasedConstructorWithLessElements_UInt64() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanByteBasedConstructorWithLessElements<ulong>());
        [Fact]
        public void ReadOnlySpanByteBasedConstructorWithLessElements_Int64() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanByteBasedConstructorWithLessElements<long>());
        [Fact]
        public void ReadOnlySpanByteBasedConstructorWithLessElements_Single() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanByteBasedConstructorWithLessElements<float>());
        [Fact]
        public void ReadOnlySpanByteBasedConstructorWithLessElements_Double() => Assert.Throws<IndexOutOfRangeException>(() => TestReadOnlySpanByteBasedConstructorWithLessElements<double>());

        private void TestReadOnlySpanByteBasedConstructorWithLessElements<T>() where T : struct
        {
            byte[] values = GenerateRandomValuesForVector<byte>(Vector<byte>.Count - 1).ToArray();
            var vector = new Vector<T>(new ReadOnlySpan<byte>(values));
        }

        [Fact]
        public void SpanBasedConstructorWithLessElements_Byte() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<byte>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_SByte() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<sbyte>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_UInt16() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<ushort>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_Int16() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<short>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_UInt32() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<uint>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_Int32() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<int>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_UInt64() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<ulong>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_Int64() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<long>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_Single() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<float>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_Double() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<double>());

        private void TestSpanBasedConstructorWithLessElements<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>(Vector<T>.Count - 1).ToArray();
            var vector = new Vector<T>(new Span<T>(values));
        }

        #endregion Tests for Span based constructor

        #region Tests for Array based constructor

        [Fact]
        public void ArrayBasedConstructor_Byte() => TestArrayBasedConstructor<byte>();
        [Fact]
        public void ArrayBasedConstructor_SByte() => TestArrayBasedConstructor<sbyte>();
        [Fact]
        public void ArrayBasedConstructor_UInt16() => TestArrayBasedConstructor<ushort>();
        [Fact]
        public void ArrayBasedConstructor_Int16() => TestArrayBasedConstructor<short>();
        [Fact]
        public void ArrayBasedConstructor_UInt32() => TestArrayBasedConstructor<uint>();
        [Fact]
        public void ArrayBasedConstructor_Int32() => TestArrayBasedConstructor<int>();
        [Fact]
        public void ArrayBasedConstructor_UInt64() => TestArrayBasedConstructor<ulong>();
        [Fact]
        public void ArrayBasedConstructor_Int64() => TestArrayBasedConstructor<long>();
        [Fact]
        public void ArrayBasedConstructor_Single() => TestArrayBasedConstructor<float>();
        [Fact]
        public void ArrayBasedConstructor_Double() => TestArrayBasedConstructor<double>();

        private void TestArrayBasedConstructor<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>(Vector<T>.Count).ToArray();
            var vector = new Vector<T>(values);
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(values[index], val);
                });
        }

        [Fact]
        public void ArrayIndexBasedConstructor_Byte() => TestArrayIndexBasedConstructor<byte>();
        [Fact]
        public void ArrayIndexBasedConstructor_SByte() => TestArrayIndexBasedConstructor<sbyte>();
        [Fact]
        public void ArrayIndexBasedConstructor_UInt16() => TestArrayIndexBasedConstructor<ushort>();
        [Fact]
        public void ArrayIndexBasedConstructor_Int16() => TestArrayIndexBasedConstructor<short>();
        [Fact]
        public void ArrayIndexBasedConstructor_UInt32() => TestArrayIndexBasedConstructor<uint>();
        [Fact]
        public void ArrayIndexBasedConstructor_Int32() => TestArrayIndexBasedConstructor<int>();
        [Fact]
        public void ArrayIndexBasedConstructor_UInt64() => TestArrayIndexBasedConstructor<ulong>();
        [Fact]
        public void ArrayIndexBasedConstructor_Int64() => TestArrayIndexBasedConstructor<long>();
        [Fact]
        public void ArrayIndexBasedConstructor_Single() => TestArrayIndexBasedConstructor<float>();
        [Fact]
        public void ArrayIndexBasedConstructor_Double() => TestArrayIndexBasedConstructor<double>();

        private void TestArrayIndexBasedConstructor<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>(Vector<T>.Count * 2).ToArray();
            int offset = Vector<T>.Count - 1;
            var vector = new Vector<T>(values, offset);
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(values[offset + index], val);
                });
        }

        [Fact]
        public void ArrayBasedConstructorWithLessElements_Byte() => TestArrayBasedConstructorWithLessElements<byte>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_SByte() => TestArrayBasedConstructorWithLessElements<sbyte>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_UInt16() => TestArrayBasedConstructorWithLessElements<ushort>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_Int16() => TestArrayBasedConstructorWithLessElements<short>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_UInt32() => TestArrayBasedConstructorWithLessElements<uint>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_Int32() => TestArrayBasedConstructorWithLessElements<int>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_UInt64() => TestArrayBasedConstructorWithLessElements<ulong>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_Int64() => TestArrayBasedConstructorWithLessElements<long>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_Single() => TestArrayBasedConstructorWithLessElements<float>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_Double() => TestArrayBasedConstructorWithLessElements<double>();

        private void TestArrayBasedConstructorWithLessElements<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>(Vector<T>.Count - 1).ToArray();
            Assert.Throws<IndexOutOfRangeException>(() => new Vector<T>(values));
        }

        [Fact]
        public void ArrayIndexBasedConstructorLessElements_Byte() => TestArrayIndexBasedConstructorLessElements<byte>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_SByte() => TestArrayIndexBasedConstructorLessElements<sbyte>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_UInt16() => TestArrayIndexBasedConstructorLessElements<ushort>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_Int16() => TestArrayIndexBasedConstructorLessElements<short>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_UInt32() => TestArrayIndexBasedConstructorLessElements<uint>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_Int32() => TestArrayIndexBasedConstructorLessElements<int>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_UInt64() => TestArrayIndexBasedConstructorLessElements<ulong>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_Int64() => TestArrayIndexBasedConstructorLessElements<long>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_Single() => TestArrayIndexBasedConstructorLessElements<float>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_Double() => TestArrayIndexBasedConstructorLessElements<double>();

        private void TestArrayIndexBasedConstructorLessElements<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>(Vector<T>.Count * 2).ToArray();
            Assert.Throws<IndexOutOfRangeException>(() => new Vector<T>(values, Vector<T>.Count + 1));
        }

        #endregion Tests for Array based constructor

        #region Tests for constructors using unsupported types

        [Fact]
        public void ConstructorWithUnsupportedTypes_Guid() => TestConstructorWithUnsupportedTypes<Guid>();
        [Fact]
        public void ConstructorWithUnsupportedTypes_DateTime() => TestConstructorWithUnsupportedTypes<DateTime>();
        [Fact]
        public void ConstructorWithUnsupportedTypes_Char() => TestConstructorWithUnsupportedTypes<Char>();

        private void TestConstructorWithUnsupportedTypes<T>() where T : struct
        {
            Assert.Throws<NotSupportedException>(() => new Vector<T>(new ReadOnlySpan<byte>(new byte[4])));
            Assert.Throws<NotSupportedException>(() => new Vector<T>(new ReadOnlySpan<T>(new T[4])));
            Assert.Throws<NotSupportedException>(() => new Vector<T>(new Span<T>(new T[4])));
        }

        #endregion Tests for constructors using unsupported types

        #endregion Constructor Tests

        #region Indexer Tests

        [Fact]
        public void IndexerOutOfRangeByte() { TestIndexerOutOfRange<byte>(); }
        [Fact]
        public void IndexerOutOfRangeSByte() { TestIndexerOutOfRange<sbyte>(); }
        [Fact]
        public void IndexerOutOfRangeUInt16() { TestIndexerOutOfRange<ushort>(); }
        [Fact]
        public void IndexerOutOfRangeInt16() { TestIndexerOutOfRange<short>(); }
        [Fact]
        public void IndexerOutOfRangeUInt32() { TestIndexerOutOfRange<uint>(); }
        [Fact]
        public void IndexerOutOfRangeInt32() { TestIndexerOutOfRange<int>(); }
        [Fact]
        public void IndexerOutOfRangeUInt64() { TestIndexerOutOfRange<ulong>(); }
        [Fact]
        public void IndexerOutOfRangeInt64() { TestIndexerOutOfRange<long>(); }
        [Fact]
        public void IndexerOutOfRangeSingle() { TestIndexerOutOfRange<float>(); }
        [Fact]
        public void IndexerOutOfRangeDouble() { TestIndexerOutOfRange<double>(); }
        private void TestIndexerOutOfRange<T>() where T : struct
        {
            Vector<T> vector = Vector<T>.One;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                T value = vector[Vector<T>.Count];
            });
        }
        #endregion

        #region Static Member Tests
        [Fact]
        public void StaticOneVectorByte() { TestStaticOneVector<byte>(); }
        [Fact]
        public void StaticOneVectorSByte() { TestStaticOneVector<sbyte>(); }
        [Fact]
        public void StaticOneVectorUInt16() { TestStaticOneVector<ushort>(); }
        [Fact]
        public void StaticOneVectorInt16() { TestStaticOneVector<short>(); }
        [Fact]
        public void StaticOneVectorUInt32() { TestStaticOneVector<uint>(); }
        [Fact]
        public void StaticOneVectorInt32() { TestStaticOneVector<int>(); }
        [Fact]
        public void StaticOneVectorUInt64() { TestStaticOneVector<ulong>(); }
        [Fact]
        public void StaticOneVectorInt64() { TestStaticOneVector<long>(); }
        [Fact]
        public void StaticOneVectorSingle() { TestStaticOneVector<float>(); }
        [Fact]
        public void StaticOneVectorDouble() { TestStaticOneVector<double>(); }
        private void TestStaticOneVector<T>() where T : struct
        {
            Vector<T> vector = Vector<T>.One;
            T oneValue = Util.One<T>();
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(oneValue, val);
                });
        }

        [Fact]
        public void StaticZeroVectorByte() { TestStaticZeroVector<byte>(); }
        [Fact]
        public void StaticZeroVectorSByte() { TestStaticZeroVector<sbyte>(); }
        [Fact]
        public void StaticZeroVectorUInt16() { TestStaticZeroVector<ushort>(); }
        [Fact]
        public void StaticZeroVectorInt16() { TestStaticZeroVector<short>(); }
        [Fact]
        public void StaticZeroVectorUInt32() { TestStaticZeroVector<uint>(); }
        [Fact]
        public void StaticZeroVectorInt32() { TestStaticZeroVector<int>(); }
        [Fact]
        public void StaticZeroVectorUInt64() { TestStaticZeroVector<ulong>(); }
        [Fact]
        public void StaticZeroVectorInt64() { TestStaticZeroVector<long>(); }
        [Fact]
        public void StaticZeroVectorSingle() { TestStaticZeroVector<float>(); }
        [Fact]
        public void StaticZeroVectorDouble() { TestStaticZeroVector<double>(); }
        private void TestStaticZeroVector<T>() where T : struct
        {
            Vector<T> vector = Vector<T>.Zero;
            T zeroValue = Util.Zero<T>();
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(zeroValue, val);
                });
        }
        #endregion

        #region CopyTo (array) Tests
        [Fact]
        public void CopyToByte() { TestCopyTo<byte>(); }
        [Fact]
        public void CopyToSByte() { TestCopyTo<sbyte>(); }
        [Fact]
        public void CopyToUInt16() { TestCopyTo<ushort>(); }
        [Fact]
        public void CopyToInt16() { TestCopyTo<short>(); }
        [Fact]
        public void CopyToUInt32() { TestCopyTo<uint>(); }
        [Fact]
        public void CopyToInt32() { TestCopyTo<int>(); }
        [Fact]
        public void CopyToUInt64() { TestCopyTo<ulong>(); }
        [Fact]
        public void CopyToInt64() { TestCopyTo<long>(); }
        [Fact]
        public void CopyToSingle() { TestCopyTo<float>(); }
        [Fact]
        public void CopyToDouble() { TestCopyTo<double>(); }
        private void TestCopyTo<T>() where T : struct
        {
            var initialValues = GenerateRandomValuesForVector<T>();
            var vector = new Vector<T>(initialValues);
            T[] array = new T[Vector<T>.Count];

            Assert.Throws<NullReferenceException>(() => vector.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => vector.CopyTo(array, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => vector.CopyTo(array, array.Length));
            AssertExtensions.Throws<ArgumentException>(null, () => vector.CopyTo(array, array.Length - 1));

            vector.CopyTo(array);
            for (int g = 0; g < array.Length; g++)
            {
                Assert.Equal(initialValues[g], array[g]);
                Assert.Equal(vector[g], array[g]);
            }
        }

        [Fact]
        public void CopyToWithOffsetByte() { TestCopyToWithOffset<byte>(); }
        [Fact]
        public void CopyToWithOffsetSByte() { TestCopyToWithOffset<sbyte>(); }
        [Fact]
        public void CopyToWithOffsetUInt16() { TestCopyToWithOffset<ushort>(); }
        [Fact]
        public void CopyToWithOffsetInt16() { TestCopyToWithOffset<short>(); }
        [Fact]
        public void CopyToWithOffsetUInt32() { TestCopyToWithOffset<uint>(); }
        [Fact]
        public void CopyToWithOffsetInt32() { TestCopyToWithOffset<int>(); }
        [Fact]
        public void CopyToWithOffsetUInt64() { TestCopyToWithOffset<ulong>(); }
        [Fact]
        public void CopyToWithOffsetInt64() { TestCopyToWithOffset<long>(); }
        [Fact]
        public void CopyToWithOffsetSingle() { TestCopyToWithOffset<float>(); }
        [Fact]
        public void CopyToWithOffsetDouble() { TestCopyToWithOffset<double>(); }
        private void TestCopyToWithOffset<T>() where T : struct
        {
            int offset = Util.GenerateSingleValue<int>(5, 500);
            var initialValues = GenerateRandomValuesForVector<T>();
            var vector = new Vector<T>(initialValues);
            T[] array = new T[Vector<T>.Count + offset];
            vector.CopyTo(array, offset);
            for (int g = 0; g < initialValues.Length; g++)
            {
                Assert.Equal(initialValues[g], array[g + offset]);
                Assert.Equal(vector[g], array[g + offset]);
            }
        }
        #endregion CopyTo (array) Tests

        #region CopyTo (span) Tests
        [Fact]
        public void CopyToSpanByte() { TestCopyToSpan<byte>(); }
        [Fact]
        public void CopyToSpanSByte() { TestCopyToSpan<sbyte>(); }
        [Fact]
        public void CopyToSpanUInt16() { TestCopyToSpan<ushort>(); }
        [Fact]
        public void CopyToSpanInt16() { TestCopyToSpan<short>(); }
        [Fact]
        public void CopyToSpanUInt32() { TestCopyToSpan<uint>(); }
        [Fact]
        public void CopyToSpanInt32() { TestCopyToSpan<int>(); }
        [Fact]
        public void CopyToSpanUInt64() { TestCopyToSpan<ulong>(); }
        [Fact]
        public void CopyToSpanInt64() { TestCopyToSpan<long>(); }
        [Fact]
        public void CopyToSpanSingle() { TestCopyToSpan<float>(); }
        [Fact]
        public void CopyToSpanDouble() { TestCopyToSpan<double>(); }
        private void TestCopyToSpan<T>() where T : struct
        {
            T[] initialValues = GenerateRandomValuesForVector<T>();
            var vector = new Vector<T>(initialValues);
            Span<T> destination = new T[Vector<T>.Count];

            Assert.Throws<ArgumentException>(() => vector.CopyTo(new Span<T>(new T[Vector<T>.Count - 1])));

            // CopyTo(Span<T>) method
            vector.CopyTo(destination);
            for (int g = 0; g < destination.Length; g++)
            {
                Assert.Equal(initialValues[g], destination[g]);
                Assert.Equal(vector[g], destination[g]);
            }

            destination.Clear();

            Assert.Throws<ArgumentException>(() => vector.CopyTo(new Span<byte>(new byte[Vector<byte>.Count - 1])));

            // CopyTo(Span<byte>) method
            vector.CopyTo(MemoryMarshal.AsBytes(destination));
            for (int g = 0; g < destination.Length; g++)
            {
                Assert.Equal(initialValues[g], destination[g]);
                Assert.Equal(vector[g], destination[g]);
            }

        }
        #endregion CopyTo (span) Tests

        #region TryCopyTo (span) Tests
        [Fact]
        public void TryCopyToSpanByte() { TestTryCopyToSpan<byte>(); }
        [Fact]
        public void TryCopyToSpanSByte() { TestTryCopyToSpan<sbyte>(); }
        [Fact]
        public void TryCopyToSpanUInt16() { TestTryCopyToSpan<ushort>(); }
        [Fact]
        public void TryCopyToSpanInt16() { TestTryCopyToSpan<short>(); }
        [Fact]
        public void TryCopyToSpanUInt32() { TestTryCopyToSpan<uint>(); }
        [Fact]
        public void TryCopyToSpanInt32() { TestTryCopyToSpan<int>(); }
        [Fact]
        public void TryCopyToSpanUInt64() { TestTryCopyToSpan<ulong>(); }
        [Fact]
        public void TryCopyToSpanInt64() { TestTryCopyToSpan<long>(); }
        [Fact]
        public void TryCopyToSpanSingle() { TestTryCopyToSpan<float>(); }
        [Fact]
        public void TryCopyToSpanDouble() { TestTryCopyToSpan<double>(); }
        private void TestTryCopyToSpan<T>() where T : struct
        {
            T[] initialValues = GenerateRandomValuesForVector<T>();
            var vector = new Vector<T>(initialValues);
            Span<T> destination = new T[Vector<T>.Count];

            // Fill the destination vector with random data; this allows
            // us to check that we didn't overwrite any part of the destination
            // if it was too small to contain the entire output.

            Random.Shared.NextBytes(MemoryMarshal.AsBytes(destination));
            T[] destinationCopy = destination.ToArray();

            Assert.False(vector.TryCopyTo(destination.Slice(1)));
            Assert.Equal<T>(destination.ToArray(), destinationCopy.ToArray());

            // TryCopyTo(Span<T>) method
            Assert.True(vector.TryCopyTo(destination));
            for (int g = 0; g < destination.Length; g++)
            {
                Assert.Equal(initialValues[g], destination[g]);
                Assert.Equal(vector[g], destination[g]);
            }

            destination.Clear();

            Assert.False(vector.TryCopyTo(new byte[Vector<byte>.Count - 1]));

            // CopyTo(Span<byte>) method
            Assert.True(vector.TryCopyTo(MemoryMarshal.AsBytes(destination)));
            for (int g = 0; g < destination.Length; g++)
            {
                Assert.Equal(initialValues[g], destination[g]);
                Assert.Equal(vector[g], destination[g]);
            }

        }
        #endregion TryCopyTo (span) Tests

        #region EqualsTests
        [Fact]
        public void EqualsObjectByte() { TestEqualsObject<byte>(); }
        [Fact]
        public void EqualsObjectSByte() { TestEqualsObject<sbyte>(); }
        [Fact]
        public void EqualsObjectUInt16() { TestEqualsObject<ushort>(); }
        [Fact]
        public void EqualsObjectInt16() { TestEqualsObject<short>(); }
        [Fact]
        public void EqualsObjectUInt32() { TestEqualsObject<uint>(); }
        [Fact]
        public void EqualsObjectInt32() { TestEqualsObject<int>(); }
        [Fact]
        public void EqualsObjectUInt64() { TestEqualsObject<ulong>(); }
        [Fact]
        public void EqualsObjectInt64() { TestEqualsObject<long>(); }
        [Fact]
        public void EqualsObjectSingle() { TestEqualsObject<float>(); }
        [Fact]
        public void EqualsObjectDouble() { TestEqualsObject<double>(); }
        private void TestEqualsObject<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector1 = new Vector<T>(values);

            const string stringObject = "This is not a Vector<T> object.";
            DateTime dateTimeObject = DateTime.UtcNow;

            Assert.False(vector1.Equals(stringObject));
            Assert.False(vector1.Equals(dateTimeObject));
            Assert.True(vector1.Equals((object)vector1));

            if (typeof(T) != typeof(int))
            {
                Vector<int> intVector = new Vector<int>(GenerateRandomValuesForVector<int>());
                Assert.False(vector1.Equals(intVector));
                Assert.False(intVector.Equals(vector1));
            }
            else
            {
                Vector<float> floatVector = new Vector<float>(GenerateRandomValuesForVector<float>());
                Assert.False(vector1.Equals(floatVector));
                Assert.False(floatVector.Equals(vector1));
            }
        }

        [Fact]
        public void EqualsVectorByte() { TestEqualsVector<byte>(); }
        [Fact]
        public void EqualsVectorSByte() { TestEqualsVector<sbyte>(); }
        [Fact]
        public void EqualsVectorUInt16() { TestEqualsVector<ushort>(); }
        [Fact]
        public void EqualsVectorInt16() { TestEqualsVector<short>(); }
        [Fact]
        public void EqualsVectorUInt32() { TestEqualsVector<uint>(); }
        [Fact]
        public void EqualsVectorInt32() { TestEqualsVector<int>(); }
        [Fact]
        public void EqualsVectorUInt64() { TestEqualsVector<ulong>(); }
        [Fact]
        public void EqualsVectorInt64() { TestEqualsVector<long>(); }
        [Fact]
        public void EqualsVectorSingle() { TestEqualsVector<float>(); }
        [Fact]
        public void EqualsVectorDouble() { TestEqualsVector<double>(); }
        private void TestEqualsVector<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector1 = new Vector<T>(values);
            Vector<T> vector2 = new Vector<T>(values);

            Assert.True(vector1.Equals(vector2));
            Assert.True(vector2.Equals(vector1));

            Assert.True(Vector<T>.Zero.Equals(Vector<T>.Zero));
            Assert.True(Vector<T>.One.Equals(Vector<T>.One));

            Assert.True(Vector<T>.Zero.Equals(new Vector<T>(Util.Zero<T>())));
            Assert.True(Vector<T>.One.Equals(new Vector<T>(Util.One<T>())));

            Assert.False(Vector<T>.Zero.Equals(Vector<T>.One));
            Assert.False(Vector<T>.Zero.Equals(new Vector<T>(Util.One<T>())));
        }
        #endregion

        #region System.Object Overloads
        [Fact]
        public void GetHashCodeByte() { TestGetHashCode<byte>(); }
        [Fact]
        public void GetHashCodeSByte() { TestGetHashCode<sbyte>(); }
        [Fact]
        public void GetHashCodeUInt16() { TestGetHashCode<ushort>(); }
        [Fact]
        public void GetHashCodeInt16() { TestGetHashCode<short>(); }
        [Fact]
        public void GetHashCodeUInt32() { TestGetHashCode<uint>(); }
        [Fact]
        public void GetHashCodeInt32() { TestGetHashCode<int>(); }
        [Fact]
        public void GetHashCodeUInt64() { TestGetHashCode<ulong>(); }
        [Fact]
        public void GetHashCodeInt64() { TestGetHashCode<long>(); }
        [Fact]
        public void GetHashCodeSingle() { TestGetHashCode<float>(); }
        [Fact]
        public void GetHashCodeDouble() { TestGetHashCode<double>(); }
        private void TestGetHashCode<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> v = new Vector<T>(values);
            Assert.Equal(v.GetHashCode(), v.GetHashCode());
        }

        [Fact]
        public void ToStringGeneralByte() { TestToString<byte>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralSByte() { TestToString<sbyte>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralUInt16() { TestToString<ushort>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralInt16() { TestToString<short>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralUInt32() { TestToString<uint>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralInt32() { TestToString<int>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralUInt64() { TestToString<ulong>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralInt64() { TestToString<long>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralSingle() { TestToString<float>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralDouble() { TestToString<double>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyByte() { TestToString<byte>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencySByte() { TestToString<sbyte>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyUInt16() { TestToString<ushort>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyInt16() { TestToString<short>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyUInt32() { TestToString<uint>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyInt32() { TestToString<int>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyUInt64() { TestToString<ulong>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyInt64() { TestToString<long>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencySingle() { TestToString<float>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyDouble() { TestToString<double>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialByte() { TestToString<byte>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialSByte() { TestToString<sbyte>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialUInt16() { TestToString<ushort>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialInt16() { TestToString<short>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialUInt32() { TestToString<uint>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialInt32() { TestToString<int>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialUInt64() { TestToString<ulong>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialInt64() { TestToString<long>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialSingle() { TestToString<float>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialDouble() { TestToString<double>("E3", CultureInfo.CurrentCulture); }

        private void TestToString<T>(string format, IFormatProvider provider) where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            Vector<T> v1 = new Vector<T>(values1);
            string result = v1.ToString(format, provider);
            string cultureSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator + " ";

            string expected = "<";
            for (int g = 0; g < Vector<T>.Count - 1; g++)
            {
                expected += ((IFormattable)v1[g]).ToString(format, provider);
                expected += cultureSeparator;
            }
            expected += ((IFormattable)v1[Vector<T>.Count - 1]).ToString(format, provider);
            expected += ">";
            Assert.Equal(expected, result);
        }
        #endregion System.Object Overloads

        #region Arithmetic Operator Tests
        [Fact]
        public void AdditionByte() { TestAddition<byte>(); }
        [Fact]
        public void AdditionSByte() { TestAddition<sbyte>(); }
        [Fact]
        public void AdditionUInt16() { TestAddition<ushort>(); }
        [Fact]
        public void AdditionInt16() { TestAddition<short>(); }
        [Fact]
        public void AdditionUInt32() { TestAddition<uint>(); }
        [Fact]
        public void AdditionInt32() { TestAddition<int>(); }
        [Fact]
        public void AdditionUInt64() { TestAddition<ulong>(); }
        [Fact]
        public void AdditionInt64() { TestAddition<long>(); }
        [Fact]
        public void AdditionSingle() { TestAddition<float>(); }
        [Fact]
        public void AdditionDouble() { TestAddition<double>(); }
        private void TestAddition<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var sum = v1 + v2;
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Add(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void AdditionOverflowByte() { TestAdditionOverflow<byte>(); }
        [Fact]
        public void AdditionOverflowSByte() { TestAdditionOverflow<sbyte>(); }
        [Fact]
        public void AdditionOverflowUInt16() { TestAdditionOverflow<ushort>(); }
        [Fact]
        public void AdditionOverflowInt16() { TestAdditionOverflow<short>(); }
        [Fact]
        public void AdditionOverflowUInt32() { TestAdditionOverflow<uint>(); }
        [Fact]
        public void AdditionOverflowInt32() { TestAdditionOverflow<int>(); }
        [Fact]
        public void AdditionOverflowUInt64() { TestAdditionOverflow<ulong>(); }
        [Fact]
        public void AdditionOverflowInt64() { TestAdditionOverflow<long>(); }
        private void TestAdditionOverflow<T>() where T : struct
        {
            T maxValue = GetMaxValueExact<T>();
            Vector<T> maxValueVector = new Vector<T>(maxValue);
            Vector<T> secondVector = new Vector<T>(GenerateRandomValuesForVector<T>());
            Vector<T> sum = maxValueVector + secondVector;

            T minValue = GetMinValueExact<T>();
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Subtract(Util.Add(secondVector[index], minValue), (T)(dynamic)1), sum[index]);
                });
        }

        [Fact]
        public void SubtractionByte() { TestSubtraction<byte>(); }
        [Fact]
        public void SubtractionSByte() { TestSubtraction<sbyte>(); }
        [Fact]
        public void SubtractionUInt16() { TestSubtraction<ushort>(); }
        [Fact]
        public void SubtractionInt16() { TestSubtraction<short>(); }
        [Fact]
        public void SubtractionUInt32() { TestSubtraction<uint>(); }
        [Fact]
        public void SubtractionInt32() { TestSubtraction<int>(); }
        [Fact]
        public void SubtractionUInt64() { TestSubtraction<ulong>(); }
        [Fact]
        public void SubtractionInt64() { TestSubtraction<long>(); }
        [Fact]
        public void SubtractionSingle() { TestSubtraction<float>(); }
        [Fact]
        public void SubtractionDouble() { TestSubtraction<double>(); }
        private void TestSubtraction<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var sum = v1 - v2;
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Subtract(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void SubtractionOverflowByte() { TestSubtractionOverflow<byte>(); }
        [Fact]
        public void SubtractionOverflowSByte() { TestSubtractionOverflow<sbyte>(); }
        [Fact]
        public void SubtractionOverflowUInt16() { TestSubtractionOverflow<ushort>(); }
        [Fact]
        public void SubtractionOverflowInt16() { TestSubtractionOverflow<short>(); }
        [Fact]
        public void SubtractionOverflowUInt32() { TestSubtractionOverflow<uint>(); }
        [Fact]
        public void SubtractionOverflowInt32() { TestSubtractionOverflow<int>(); }
        [Fact]
        public void SubtractionOverflowUInt64() { TestSubtractionOverflow<ulong>(); }
        [Fact]
        public void SubtractionOverflowInt64() { TestSubtractionOverflow<long>(); }
        private void TestSubtractionOverflow<T>() where T : struct
        {
            T minValue = GetMinValueExact<T>();
            Vector<T> minValueVector = new Vector<T>(minValue);
            Vector<T> secondVector = new Vector<T>(GenerateRandomValuesForVector<T>());
            Vector<T> difference = minValueVector - secondVector;

            T maxValue = GetMaxValueExact<T>();
            ValidateVector(difference,
                (index, val) =>
                {
                    Assert.Equal(Util.Add(Util.Subtract(maxValue, secondVector[index]), (T)(dynamic)1), val);
                });
        }

        [Fact]
        public void MultiplicationByte() { TestMultiplication<byte>(); }
        [Fact]
        public void MultiplicationSByte() { TestMultiplication<sbyte>(); }
        [Fact]
        public void MultiplicationUInt16() { TestMultiplication<ushort>(); }
        [Fact]
        public void MultiplicationInt16() { TestMultiplication<short>(); }
        [Fact]
        public void MultiplicationUInt32() { TestMultiplication<uint>(); }
        [Fact]
        public void MultiplicationInt32() { TestMultiplication<int>(); }
        [Fact]
        public void MultiplicationUInt64() { TestMultiplication<ulong>(); }
        [Fact]
        public void MultiplicationInt64() { TestMultiplication<long>(); }
        [Fact]
        public void MultiplicationSingle() { TestMultiplication<float>(); }
        [Fact]
        public void MultiplicationDouble() { TestMultiplication<double>(); }
        private void TestMultiplication<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var sum = v1 * v2;
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Multiply(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void MultiplicationWithScalarByte() { TestMultiplicationWithScalar<byte>(); }
        [Fact]
        public void MultiplicationWithScalarSByte() { TestMultiplicationWithScalar<sbyte>(); }
        [Fact]
        public void MultiplicationWithScalarUInt16() { TestMultiplicationWithScalar<ushort>(); }
        [Fact]
        public void MultiplicationWithScalarInt16() { TestMultiplicationWithScalar<short>(); }
        [Fact]
        public void MultiplicationWithScalarUInt32() { TestMultiplicationWithScalar<uint>(); }
        [Fact]
        public void MultiplicationWithScalarInt32() { TestMultiplicationWithScalar<int>(); }
        [Fact]
        public void MultiplicationWithScalarUInt64() { TestMultiplicationWithScalar<ulong>(); }
        [Fact]
        public void MultiplicationWithScalarInt64() { TestMultiplicationWithScalar<long>(); }
        [Fact]
        public void MultiplicationWithScalarSingle() { TestMultiplicationWithScalar<float>(); }
        [Fact]
        public void MultiplicationWithScalarDouble() { TestMultiplicationWithScalar<double>(); }
        private void TestMultiplicationWithScalar<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>();
            T factor = Util.GenerateSingleValue<T>(GetMinValue<T>(), GetMaxValue<T>());
            var vector = new Vector<T>(values);
            var product1 = vector * factor;
            ValidateVector(product1,
                (index, val) =>
                {
                    T expected = Util.Multiply(values[index], factor);
                    Assert.Equal(expected, val);
                });

            var product2 = factor * vector;
            ValidateVector(product2,
                (index, val) =>
                {
                    T expected = Util.Multiply(values[index], factor);
                    Assert.Equal(expected, val);
                });
        }

        [Fact]
        public void DivisionByte() { TestDivision<byte>(); }
        [Fact]
        public void DivisionSByte() { TestDivision<sbyte>(); }
        [Fact]
        public void DivisionUInt16() { TestDivision<ushort>(); }
        [Fact]
        public void DivisionInt16() { TestDivision<short>(); }
        [Fact]
        public void DivisionUInt32() { TestDivision<uint>(); }
        [Fact]
        public void DivisionInt32() { TestDivision<int>(); }
        [Fact]
        public void DivisionUInt64() { TestDivision<ulong>(); }
        [Fact]
        public void DivisionInt64() { TestDivision<long>(); }
        [Fact]
        public void DivisionSingle() { TestDivision<float>(); }
        [Fact]
        public void DivisionDouble() { TestDivision<double>(); }
        private void TestDivision<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            values1 = values1.Select(val => val.Equals(Util.Zero<T>()) ? Util.One<T>() : val).ToArray(); // Avoid divide-by-zero
            T[] values2 = GenerateRandomValuesForVector<T>();
            values2 = values2.Select(val => val.Equals(Util.Zero<T>()) ? Util.One<T>() : val).ToArray(); // Avoid divide-by-zero
            // I replace all Zero's with One's above to avoid Divide-by-zero.

            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var sum = v1 / v2;
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Divide(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void DivisionByZeroExceptionByte() { TestDivisionByZeroException<byte>(); }
        [Fact]
        public void DivisionByZeroExceptionSByte() { TestDivisionByZeroException<sbyte>(); }
        [Fact]
        public void DivisionByZeroExceptionUInt16() { TestDivisionByZeroException<ushort>(); }
        [Fact]
        public void DivisionByZeroExceptionInt16() { TestDivisionByZeroException<short>(); }
        [Fact]
        public void DivisionByZeroExceptionInt32() { TestDivisionByZeroException<int>(); }
        [Fact]
        public void DivisionByZeroExceptionInt64() { TestDivisionByZeroException<long>(); }
        private void TestDivisionByZeroException<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            Vector<T> vector = new Vector<T>(values1);
            Assert.Throws<DivideByZeroException>(() =>
            {
                var result = vector / Vector<T>.Zero;
            });
        }

        [Fact]
        public void UnaryMinusByte() { TestUnaryMinus<byte>(); }
        [Fact]
        public void UnaryMinusSByte() { TestUnaryMinus<sbyte>(); }
        [Fact]
        public void UnaryMinusUInt16() { TestUnaryMinus<ushort>(); }
        [Fact]
        public void UnaryMinusInt16() { TestUnaryMinus<short>(); }
        [Fact]
        public void UnaryMinusUInt32() { TestUnaryMinus<uint>(); }
        [Fact]
        public void UnaryMinusInt32() { TestUnaryMinus<int>(); }
        [Fact]
        public void UnaryMinusUInt64() { TestUnaryMinus<ulong>(); }
        [Fact]
        public void UnaryMinusInt64() { TestUnaryMinus<long>(); }
        [Fact]
        public void UnaryMinusSingle() { TestUnaryMinus<float>(); }
        [Fact]
        public void UnaryMinusDouble() { TestUnaryMinus<double>(); }
        private void TestUnaryMinus<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector = new Vector<T>(values);
            var negated = -vector;
            ValidateVector(negated,
                (index, value) =>
                {
                    T expected = Util.Subtract(Util.Zero<T>(), values[index]);
                    Assert.Equal(expected, value);
                });
        }
        #endregion

        #region Bitwise Operator Tests
        [Fact]
        public void BitwiseAndOperatorByte() { TestBitwiseAndOperator<byte>(); }
        [Fact]
        public void BitwiseAndOperatorSByte() { TestBitwiseAndOperator<sbyte>(); }
        [Fact]
        public void BitwiseAndOperatorUInt16() { TestBitwiseAndOperator<ushort>(); }
        [Fact]
        public void BitwiseAndOperatorInt16() { TestBitwiseAndOperator<short>(); }
        [Fact]
        public void BitwiseAndOperatorUInt32() { TestBitwiseAndOperator<uint>(); }
        [Fact]
        public void BitwiseAndOperatorInt32() { TestBitwiseAndOperator<int>(); }
        [Fact]
        public void BitwiseAndOperatorUInt64() { TestBitwiseAndOperator<ulong>(); }
        [Fact]
        public void BitwiseAndOperatorInt64() { TestBitwiseAndOperator<long>(); }
        [Fact]
        public void BitwiseAndOperatorSingle() { TestBitwiseAndOperator<float>(); }
        [Fact]
        public void BitwiseAndOperatorDouble() { TestBitwiseAndOperator<double>(); }
        private void TestBitwiseAndOperator<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            Vector<T> randomVector = new Vector<T>(values1);
            Vector<T> zeroVector = Vector<T>.Zero;

            Vector<T> selfAnd = randomVector & randomVector;
            Assert.Equal(randomVector, selfAnd);

            Vector<T> zeroAnd = randomVector & zeroVector;
            Assert.Equal(zeroVector, zeroAnd);
        }

        [Fact]
        public void BitwiseOrOperatorByte() { TestBitwiseOrOperator<byte>(); }
        [Fact]
        public void BitwiseOrOperatorSByte() { TestBitwiseOrOperator<sbyte>(); }
        [Fact]
        public void BitwiseOrOperatorUInt16() { TestBitwiseOrOperator<ushort>(); }
        [Fact]
        public void BitwiseOrOperatorInt16() { TestBitwiseOrOperator<short>(); }
        [Fact]
        public void BitwiseOrOperatorUInt32() { TestBitwiseOrOperator<uint>(); }
        [Fact]
        public void BitwiseOrOperatorInt32() { TestBitwiseOrOperator<int>(); }
        [Fact]
        public void BitwiseOrOperatorUInt64() { TestBitwiseOrOperator<ulong>(); }
        [Fact]
        public void BitwiseOrOperatorInt64() { TestBitwiseOrOperator<long>(); }
        private void TestBitwiseOrOperator<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            Vector<T> randomVector = new Vector<T>(values1);
            Vector<T> zeroVector = Vector<T>.Zero;

            Vector<T> selfOr = randomVector | randomVector;
            Assert.Equal(randomVector, selfOr);

            Vector<T> zeroOr = randomVector | zeroVector;
            Assert.Equal(randomVector, zeroOr);

            Vector<T> allOnesVector = new Vector<T>(GetValueWithAllOnesSet<T>());
            Vector<T> allOnesOrZero = zeroVector | allOnesVector;
            Assert.Equal(allOnesVector, allOnesOrZero);
        }

        [Fact]
        public void BitwiseXorOperatorByte() { TestBitwiseXorOperator<byte>(); }
        [Fact]
        public void BitwiseXorOperatorSByte() { TestBitwiseXorOperator<sbyte>(); }
        [Fact]
        public void BitwiseXorOperatorUInt16() { TestBitwiseXorOperator<ushort>(); }
        [Fact]
        public void BitwiseXorOperatorInt16() { TestBitwiseXorOperator<short>(); }
        [Fact]
        public void BitwiseXorOperatorUInt32() { TestBitwiseXorOperator<uint>(); }
        [Fact]
        public void BitwiseXorOperatorInt32() { TestBitwiseXorOperator<int>(); }
        [Fact]
        public void BitwiseXorOperatorUInt64() { TestBitwiseXorOperator<ulong>(); }
        [Fact]
        public void BitwiseXorOperatorInt64() { TestBitwiseXorOperator<long>(); }
        private void TestBitwiseXorOperator<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> randomVector1 = new Vector<T>(values1);
            Vector<T> randomVector2 = new Vector<T>(values2);

            Vector<T> result = randomVector1 ^ randomVector2;
            ValidateVector(result,
                (index, val) =>
                {
                    T expected = Util.Xor(values1[index], values2[index]);
                    Assert.Equal(expected, val);
                });
        }

        [Fact]
        public void BitwiseOnesComplementOperatorByte() { TestBitwiseOnesComplementOperator<byte>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorSByte() { TestBitwiseOnesComplementOperator<sbyte>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorUInt16() { TestBitwiseOnesComplementOperator<ushort>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorInt16() { TestBitwiseOnesComplementOperator<short>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorUInt32() { TestBitwiseOnesComplementOperator<uint>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorInt32() { TestBitwiseOnesComplementOperator<int>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorUInt64() { TestBitwiseOnesComplementOperator<ulong>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorInt64() { TestBitwiseOnesComplementOperator<long>(); }
        private void TestBitwiseOnesComplementOperator<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            Vector<T> randomVector1 = new Vector<T>(values1);

            Vector<T> result = ~randomVector1;
            ValidateVector(result,
                (index, val) =>
                {
                    T expected = Util.OnesComplement(values1[index]);
                    Assert.Equal(expected, val);
                });
        }

        [Fact]
        public void BitwiseAndNotByte() { TestBitwiseAndNot<byte>(); }
        [Fact]
        public void BitwiseAndNotSByte() { TestBitwiseAndNot<sbyte>(); }
        [Fact]
        public void BitwiseAndNotUInt16() { TestBitwiseAndNot<ushort>(); }
        [Fact]
        public void BitwiseAndNotInt16() { TestBitwiseAndNot<short>(); }
        [Fact]
        public void BitwiseAndNotUInt32() { TestBitwiseAndNot<uint>(); }
        [Fact]
        public void BitwiseAndNotInt32() { TestBitwiseAndNot<int>(); }
        [Fact]
        public void BitwiseAndNotUInt64() { TestBitwiseAndNot<ulong>(); }
        [Fact]
        public void BitwiseAndNotInt64() { TestBitwiseAndNot<long>(); }
        private void TestBitwiseAndNot<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> randomVector1 = new Vector<T>(values1);
            Vector<T> randomVector2 = new Vector<T>(values2);

            Vector<T> result = Vector.AndNot(randomVector1, randomVector2);
            Vector<T> result2 = randomVector1 & ~randomVector2;
            ValidateVector(result,
                (index, val) =>
                {
                    T expected = Util.AndNot(values1[index], values2[index]);
                    Assert.Equal(expected, val);
                    Assert.Equal(expected, result2[index]);
                    Assert.Equal(result2[index], val);
                });
        }
        #endregion

        #region Comparison Tests
        [Fact]
        public void VectorGreaterThanByte() { TestVectorGreaterThan<byte>(); }
        [Fact]
        public void VectorGreaterThanSByte() { TestVectorGreaterThan<sbyte>(); }
        [Fact]
        public void VectorGreaterThanUInt16() { TestVectorGreaterThan<ushort>(); }
        [Fact]
        public void VectorGreaterThanInt16() { TestVectorGreaterThan<short>(); }
        [Fact]
        public void VectorGreaterThanUInt32() { TestVectorGreaterThan<uint>(); }
        [Fact]
        public void VectorGreaterThanInt32() { TestVectorGreaterThan<int>(); }
        [Fact]
        public void VectorGreaterThanUInt64() { TestVectorGreaterThan<ulong>(); }
        [Fact]
        public void VectorGreaterThanInt64() { TestVectorGreaterThan<long>(); }
        [Fact]
        public void VectorGreaterThanSingle() { TestVectorGreaterThan<float>(); }
        [Fact]
        public void VectorGreaterThanDouble() { TestVectorGreaterThan<double>(); }
        private void TestVectorGreaterThan<T>() where T : struct
        {
            var values1 = GenerateRandomValuesForVector<T>();
            var values2 = GenerateRandomValuesForVector<T>();
            var vec1 = new Vector<T>(values1);
            var vec2 = new Vector<T>(values2);

            var result = Vector.GreaterThan<T>(vec1, vec2);
            ValidateVector(result,
                (index, val) =>
                {
                    bool isGreater = Util.GreaterThan(values1[index], values2[index]);
                    T expected = isGreater ? GetValueWithAllOnesSet<T>() : Util.Zero<T>();
                    Assert.Equal(expected, result[index]);
                });
        }

        [Fact]
        public void GreaterThanOrEqualByte() { TestVectorGreaterThanOrEqual<byte>(); }
        [Fact]
        public void GreaterThanOrEqualSByte() { TestVectorGreaterThanOrEqual<sbyte>(); }
        [Fact]
        public void GreaterThanOrEqualUInt16() { TestVectorGreaterThanOrEqual<ushort>(); }
        [Fact]
        public void GreaterThanOrEqualInt16() { TestVectorGreaterThanOrEqual<short>(); }
        [Fact]
        public void GreaterThanOrEqualUInt32() { TestVectorGreaterThanOrEqual<uint>(); }
        [Fact]
        public void GreaterThanOrEqualInt32() { TestVectorGreaterThanOrEqual<int>(); }
        [Fact]
        public void GreaterThanOrEqualUInt64() { TestVectorGreaterThanOrEqual<ulong>(); }
        [Fact]
        public void GreaterThanOrEqualInt64() { TestVectorGreaterThanOrEqual<long>(); }
        [Fact]
        public void GreaterThanOrEqualSingle() { TestVectorGreaterThanOrEqual<float>(); }
        [Fact]
        public void GreaterThanOrEqualDouble() { TestVectorGreaterThanOrEqual<double>(); }
        private void TestVectorGreaterThanOrEqual<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            Vector<T> result = Vector.GreaterThanOrEqual<T>(vec1, vec2);
            ValidateVector(result,
                (index, val) =>
                {
                    bool isGreaterOrEqual = Util.GreaterThanOrEqual(values1[index], values2[index]);
                    T expected = isGreaterOrEqual ? GetValueWithAllOnesSet<T>() : Util.Zero<T>();
                    Assert.Equal(expected, result[index]);
                });
        }

        [Fact]
        public void GreaterThanAnyByte() { TestVectorGreaterThanAny<byte>(); }
        [Fact]
        public void GreaterThanAnySByte() { TestVectorGreaterThanAny<sbyte>(); }
        [Fact]
        public void GreaterThanAnyUInt16() { TestVectorGreaterThanAny<ushort>(); }
        [Fact]
        public void GreaterThanAnyInt16() { TestVectorGreaterThanAny<short>(); }
        [Fact]
        public void GreaterThanAnyUInt32() { TestVectorGreaterThanAny<uint>(); }
        [Fact]
        public void GreaterThanAnyInt32() { TestVectorGreaterThanAny<int>(); }
        [Fact]
        public void GreaterThanAnyUInt64() { TestVectorGreaterThanAny<ulong>(); }
        [Fact]
        public void GreaterThanAnyInt64() { TestVectorGreaterThanAny<long>(); }
        [Fact]
        public void GreaterThanAnySingle() { TestVectorGreaterThanAny<float>(); }
        [Fact]
        public void GreaterThanAnyDouble() { TestVectorGreaterThanAny<double>(); }
        private void TestVectorGreaterThanAny<T>() where T : struct
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)(g + 10);
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = unchecked((T)(dynamic)(g * 5 + 9));
            }
            Vector<T> vec2 = new Vector<T>(values2);

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (T)(dynamic)(g + 12);
            }
            Vector<T> vec3 = new Vector<T>(values3);

            Assert.True(Vector.GreaterThanAny(vec1, vec2));
            Assert.True(Vector.GreaterThanAny(vec2, vec1));
            Assert.True(Vector.GreaterThanAny(vec3, vec1));
            Assert.True(Vector.GreaterThanAny(vec2, vec3));
            Assert.False(Vector.GreaterThanAny(vec1, vec3));
        }

        [Fact]
        public void GreaterThanAllByte() { TestVectorGreaterThanAll<byte>(); }
        [Fact]
        public void GreaterThanAllSByte() { TestVectorGreaterThanAll<sbyte>(); }
        [Fact]
        public void GreaterThanAllUInt16() { TestVectorGreaterThanAll<ushort>(); }
        [Fact]
        public void GreaterThanAllInt16() { TestVectorGreaterThanAll<short>(); }
        [Fact]
        public void GreaterThanAllUInt32() { TestVectorGreaterThanAll<uint>(); }
        [Fact]
        public void GreaterThanAllInt32() { TestVectorGreaterThanAll<int>(); }
        [Fact]
        public void GreaterThanAllUInt64() { TestVectorGreaterThanAll<ulong>(); }
        [Fact]
        public void GreaterThanAllInt64() { TestVectorGreaterThanAll<long>(); }
        [Fact]
        public void GreaterThanAllSingle() { TestVectorGreaterThanAll<float>(); }
        [Fact]
        public void GreaterThanAllDouble() { TestVectorGreaterThanAll<double>(); }
        private void TestVectorGreaterThanAll<T>() where T : struct
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)(g + 10);
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = unchecked((T)(dynamic)(g * 5 + 9));
            }
            Vector<T> vec2 = new Vector<T>(values2);

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (T)(dynamic)(g + 12);
            }
            Vector<T> vec3 = new Vector<T>(values3);

            Assert.False(Vector.GreaterThanAll(vec1, vec2));
            Assert.False(Vector.GreaterThanAll(vec2, vec1));
            Assert.True(Vector.GreaterThanAll(vec3, vec1));
            Assert.False(Vector.GreaterThanAll(vec1, vec3));
        }

        [Fact]
        public void GreaterThanOrEqualAnyByte() { TestVectorGreaterThanOrEqualAny<byte>(); }
        [Fact]
        public void GreaterThanOrEqualAnySByte() { TestVectorGreaterThanOrEqualAny<sbyte>(); }
        [Fact]
        public void GreaterThanOrEqualAnyUInt16() { TestVectorGreaterThanOrEqualAny<ushort>(); }
        [Fact]
        public void GreaterThanOrEqualAnyInt16() { TestVectorGreaterThanOrEqualAny<short>(); }
        [Fact]
        public void GreaterThanOrEqualAnyUInt32() { TestVectorGreaterThanOrEqualAny<uint>(); }
        [Fact]
        public void GreaterThanOrEqualAnyInt32() { TestVectorGreaterThanOrEqualAny<int>(); }
        [Fact]
        public void GreaterThanOrEqualAnyUInt64() { TestVectorGreaterThanOrEqualAny<ulong>(); }
        [Fact]
        public void GreaterThanOrEqualAnyInt64() { TestVectorGreaterThanOrEqualAny<long>(); }
        [Fact]
        public void GreaterThanOrEqualAnySingle() { TestVectorGreaterThanOrEqualAny<float>(); }
        [Fact]
        public void GreaterThanOrEqualAnyDouble() { TestVectorGreaterThanOrEqualAny<double>(); }
        private void TestVectorGreaterThanOrEqualAny<T>() where T : struct
        {
            int maxT = GetMaxValue<T>();
            double maxStep = (double)maxT / (double)Vector<T>.Count;
            double halfStep = maxStep / 2;

            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)(g * halfStep);
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = (T)(dynamic)(g * maxStep);
            }
            Vector<T> vec2 = new Vector<T>(values2);

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (T)(dynamic)((g + 1) * maxStep);
            }
            Vector<T> vec3 = new Vector<T>(values3);

            Assert.True(Vector.GreaterThanOrEqualAny(vec1, vec2));
            Assert.True(Vector.GreaterThanOrEqualAny(vec2, vec1));
            Assert.True(Vector.GreaterThanOrEqualAny(vec3, vec1));
            Assert.True(Vector.GreaterThanOrEqualAny(vec3, vec2));
            Assert.False(Vector.GreaterThanOrEqualAny(vec1, vec3));
            Assert.False(Vector.GreaterThanOrEqualAny(vec2, vec3));

            Assert.True(Vector.GreaterThanOrEqualAny(vec1, vec1));
            Assert.True(Vector.GreaterThanOrEqualAny(vec2, vec2));
            Assert.True(Vector.GreaterThanOrEqualAny(vec3, vec3));
        }

        [Fact]
        public void GreaterThanOrEqualAllByte() { TestVectorGreaterThanOrEqualAll<byte>(); }
        [Fact]
        public void GreaterThanOrEqualAllSByte() { TestVectorGreaterThanOrEqualAll<sbyte>(); }
        [Fact]
        public void GreaterThanOrEqualAllUInt16() { TestVectorGreaterThanOrEqualAll<ushort>(); }
        [Fact]
        public void GreaterThanOrEqualAllInt16() { TestVectorGreaterThanOrEqualAll<short>(); }
        [Fact]
        public void GreaterThanOrEqualAllUInt32() { TestVectorGreaterThanOrEqualAll<uint>(); }
        [Fact]
        public void GreaterThanOrEqualAllInt32() { TestVectorGreaterThanOrEqualAll<int>(); }
        [Fact]
        public void GreaterThanOrEqualAllUInt64() { TestVectorGreaterThanOrEqualAll<ulong>(); }
        [Fact]
        public void GreaterThanOrEqualAllInt64() { TestVectorGreaterThanOrEqualAll<long>(); }
        [Fact]
        public void GreaterThanOrEqualAllSingle() { TestVectorGreaterThanOrEqualAll<float>(); }
        [Fact]
        public void GreaterThanOrEqualAllDouble() { TestVectorGreaterThanOrEqualAll<double>(); }
        private void TestVectorGreaterThanOrEqualAll<T>() where T : struct
        {
            int maxT = GetMaxValue<T>();
            double maxStep = (double)maxT / (double)Vector<T>.Count;
            double halfStep = maxStep / 2;

            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)(g * halfStep);
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = (T)(dynamic)(g * maxStep);
            }
            Vector<T> vec2 = new Vector<T>(values2);

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (T)(dynamic)((g + 1) * maxStep);
            }
            Vector<T> vec3 = new Vector<T>(values3);

            Assert.False(Vector.GreaterThanOrEqualAll(vec1, vec2));
            Assert.True(Vector.GreaterThanOrEqualAll(vec2, vec1));
            Assert.True(Vector.GreaterThanOrEqualAll(vec3, vec1));
            Assert.True(Vector.GreaterThanOrEqualAll(vec3, vec2));
            Assert.False(Vector.GreaterThanOrEqualAll(vec1, vec3));

            Assert.True(Vector.GreaterThanOrEqualAll(vec1, vec1));
            Assert.True(Vector.GreaterThanOrEqualAll(vec2, vec2));
            Assert.True(Vector.GreaterThanOrEqualAll(vec3, vec3));
        }

        [Fact]
        public void LessThanByte() { TestVectorLessThan<byte>(); }
        [Fact]
        public void LessThanSByte() { TestVectorLessThan<sbyte>(); }
        [Fact]
        public void LessThanUInt16() { TestVectorLessThan<ushort>(); }
        [Fact]
        public void LessThanInt16() { TestVectorLessThan<short>(); }
        [Fact]
        public void LessThanUInt32() { TestVectorLessThan<uint>(); }
        [Fact]
        public void LessThanInt32() { TestVectorLessThan<int>(); }
        [Fact]
        public void LessThanUInt64() { TestVectorLessThan<ulong>(); }
        [Fact]
        public void LessThanInt64() { TestVectorLessThan<long>(); }
        [Fact]
        public void LessThanSingle() { TestVectorLessThan<float>(); }
        [Fact]
        public void LessThanDouble() { TestVectorLessThan<double>(); }
        private void TestVectorLessThan<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            var result = Vector.LessThan<T>(vec1, vec2);
            ValidateVector(result,
                (index, val) =>
                {
                    bool isLess = Util.LessThan(values1[index], values2[index]);
                    T expected = isLess ? GetValueWithAllOnesSet<T>() : Util.Zero<T>();
                    Assert.Equal(expected, result[index]);
                });
        }

        [Fact]
        public void LessThanOrEqualByte() { TestVectorLessThanOrEqual<byte>(); }
        [Fact]
        public void LessThanOrEqualSByte() { TestVectorLessThanOrEqual<sbyte>(); }
        [Fact]
        public void LessThanOrEqualUInt16() { TestVectorLessThanOrEqual<ushort>(); }
        [Fact]
        public void LessThanOrEqualInt16() { TestVectorLessThanOrEqual<short>(); }
        [Fact]
        public void LessThanOrEqualUInt32() { TestVectorLessThanOrEqual<uint>(); }
        [Fact]
        public void LessThanOrEqualInt32() { TestVectorLessThanOrEqual<int>(); }
        [Fact]
        public void LessThanOrEqualUInt64() { TestVectorLessThanOrEqual<ulong>(); }
        [Fact]
        public void LessThanOrEqualInt64() { TestVectorLessThanOrEqual<long>(); }
        [Fact]
        public void LessThanOrEqualSingle() { TestVectorLessThanOrEqual<float>(); }
        [Fact]
        public void LessThanOrEqualDouble() { TestVectorLessThanOrEqual<double>(); }
        private void TestVectorLessThanOrEqual<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            var result = Vector.LessThanOrEqual<T>(vec1, vec2);
            ValidateVector(result,
                (index, val) =>
                {
                    bool isLessOrEqual = Util.LessThanOrEqual(values1[index], values2[index]);
                    T expected = isLessOrEqual ? GetValueWithAllOnesSet<T>() : Util.Zero<T>();
                    Assert.Equal(expected, result[index]);
                });
        }

        [Fact]
        public void LessThanAnyByte() { TestVectorLessThanAny<byte>(); }
        [Fact]
        public void LessThanAnySByte() { TestVectorLessThanAny<sbyte>(); }
        [Fact]
        public void LessThanAnyUInt16() { TestVectorLessThanAny<ushort>(); }
        [Fact]
        public void LessThanAnyInt16() { TestVectorLessThanAny<short>(); }
        [Fact]
        public void LessThanAnyUInt32() { TestVectorLessThanAny<uint>(); }
        [Fact]
        public void LessThanAnyInt32() { TestVectorLessThanAny<int>(); }
        [Fact]
        public void LessThanAnyUInt64() { TestVectorLessThanAny<ulong>(); }
        [Fact]
        public void LessThanAnyInt64() { TestVectorLessThanAny<long>(); }
        [Fact]
        public void LessThanAnySingle() { TestVectorLessThanAny<float>(); }
        [Fact]
        public void LessThanAnyDouble() { TestVectorLessThanAny<double>(); }
        private void TestVectorLessThanAny<T>() where T : struct
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)g;
            }
            Vector<T> vec1 = new Vector<T>(values1);
            values1[0] = Util.Add(values1[0], Util.One<T>());
            Vector<T> vec2 = new Vector<T>(values1);

            Assert.False(Vector.LessThanAny(vec1, vec1));
            Assert.True(Vector.LessThanAny(vec1, vec2));
        }

        [Fact]
        public void LessThanAllByte() { TestVectorLessThanAll<byte>(); }
        [Fact]
        public void LessThanAllSByte() { TestVectorLessThanAll<sbyte>(); }
        [Fact]
        public void LessThanAllUInt16() { TestVectorLessThanAll<ushort>(); }
        [Fact]
        public void LessThanAllInt16() { TestVectorLessThanAll<short>(); }
        [Fact]
        public void LessThanAllUInt32() { TestVectorLessThanAll<uint>(); }
        [Fact]
        public void LessThanAllInt32() { TestVectorLessThanAll<int>(); }
        [Fact]
        public void LessThanAllUInt64() { TestVectorLessThanAll<ulong>(); }
        [Fact]
        public void LessThanAllInt64() { TestVectorLessThanAll<long>(); }
        [Fact]
        public void LessThanAllSingle() { TestVectorLessThanAll<float>(); }
        [Fact]
        public void LessThanAllDouble() { TestVectorLessThanAll<double>(); }
        private void TestVectorLessThanAll<T>() where T : struct
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)g;
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = (T)(dynamic)(g + 25);
            }
            Vector<T> vec2 = new Vector<T>(values2);

            Assert.True(Vector.LessThanAll(vec1, vec2));
            Assert.True(Vector.LessThanAll(Vector<T>.Zero, Vector<T>.One));

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (g < Vector<T>.Count / 2) ? Util.Zero<T>() : Util.One<T>();
            }
            Vector<T> vec3 = new Vector<T>(values3);
            Assert.False(Vector.LessThanAll(vec3, Vector<T>.One));
        }

        [Fact]
        public void LessThanOrEqualAnyByte() { TestVectorLessThanOrEqualAny<byte>(); }
        [Fact]
        public void LessThanOrEqualAnySByte() { TestVectorLessThanOrEqualAny<sbyte>(); }
        [Fact]
        public void LessThanOrEqualAnyUInt16() { TestVectorLessThanOrEqualAny<ushort>(); }
        [Fact]
        public void LessThanOrEqualAnyInt16() { TestVectorLessThanOrEqualAny<short>(); }
        [Fact]
        public void LessThanOrEqualAnyUInt32() { TestVectorLessThanOrEqualAny<uint>(); }
        [Fact]
        public void LessThanOrEqualAnyInt32() { TestVectorLessThanOrEqualAny<int>(); }
        [Fact]
        public void LessThanOrEqualAnyUInt64() { TestVectorLessThanOrEqualAny<ulong>(); }
        [Fact]
        public void LessThanOrEqualAnyInt64() { TestVectorLessThanOrEqualAny<long>(); }
        [Fact]
        public void LessThanOrEqualAnySingle() { TestVectorLessThanOrEqualAny<float>(); }
        [Fact]
        public void LessThanOrEqualAnyDouble() { TestVectorLessThanOrEqualAny<double>(); }
        private void TestVectorLessThanOrEqualAny<T>() where T : struct
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)g;
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = (T)(dynamic)(g * 2);
            }
            Vector<T> vec2 = new Vector<T>(values2);

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (T)(dynamic)(g + 2);
            }
            Vector<T> vec3 = new Vector<T>(values3);

            Assert.True(Vector.LessThanOrEqualAny(vec1, vec2));
            Assert.True(Vector.LessThanOrEqualAny(vec2, vec1));

            Assert.False(Vector.LessThanOrEqualAny(vec3, vec1));
            Assert.True(Vector.LessThanOrEqualAny(vec1, vec3));
            Assert.True(Vector.LessThanOrEqualAny(vec2, vec3));

            Assert.True(Vector.LessThanOrEqualAny(vec1, vec1));
            Assert.True(Vector.LessThanOrEqualAny(vec2, vec2));
        }

        [Fact]
        public void LessThanOrEqualAllByte() { TestVectorLessThanOrEqualAll<byte>(); }
        [Fact]
        public void LessThanOrEqualAllSByte() { TestVectorLessThanOrEqualAll<sbyte>(); }
        [Fact]
        public void LessThanOrEqualAllUInt16() { TestVectorLessThanOrEqualAll<ushort>(); }
        [Fact]
        public void LessThanOrEqualAllInt16() { TestVectorLessThanOrEqualAll<short>(); }
        [Fact]
        public void LessThanOrEqualAllUInt32() { TestVectorLessThanOrEqualAll<uint>(); }
        [Fact]
        public void LessThanOrEqualAllInt32() { TestVectorLessThanOrEqualAll<int>(); }
        [Fact]
        public void LessThanOrEqualAllUInt64() { TestVectorLessThanOrEqualAll<ulong>(); }
        [Fact]
        public void LessThanOrEqualAllInt64() { TestVectorLessThanOrEqualAll<long>(); }
        [Fact]
        public void LessThanOrEqualAllSingle() { TestVectorLessThanOrEqualAll<float>(); }
        [Fact]
        public void LessThanOrEqualAllDouble() { TestVectorLessThanOrEqualAll<double>(); }
        private void TestVectorLessThanOrEqualAll<T>() where T : struct
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)g;
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = (T)(dynamic)(g * 2);
            }
            Vector<T> vec2 = new Vector<T>(values2);

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (T)(dynamic)(g + 2);
            }
            Vector<T> vec3 = new Vector<T>(values3);

            Assert.True(Vector.LessThanOrEqualAll(vec1, vec2));
            Assert.False(Vector.LessThanOrEqualAll(vec2, vec1));

            Assert.False(Vector.LessThanOrEqualAll(vec3, vec1));
            Assert.True(Vector.LessThanOrEqualAll(vec1, vec3));

            Assert.True(Vector.LessThanOrEqualAll(vec1, vec1));
            Assert.True(Vector.LessThanOrEqualAll(vec2, vec2));
        }

        [Fact]
        public void VectorEqualsByte() { TestVectorEquals<byte>(); }
        [Fact]
        public void VectorEqualsSByte() { TestVectorEquals<sbyte>(); }
        [Fact]
        public void VectorEqualsUInt16() { TestVectorEquals<ushort>(); }
        [Fact]
        public void VectorEqualsInt16() { TestVectorEquals<short>(); }
        [Fact]
        public void VectorEqualsUInt32() { TestVectorEquals<uint>(); }
        [Fact]
        public void VectorEqualsInt32() { TestVectorEquals<int>(); }
        [Fact]
        public void VectorEqualsUInt64() { TestVectorEquals<ulong>(); }
        [Fact]
        public void VectorEqualsInt64() { TestVectorEquals<long>(); }
        [Fact]
        public void VectorEqualsSingle() { TestVectorEquals<float>(); }
        [Fact]
        public void VectorEqualsDouble() { TestVectorEquals<double>(); }
        private void TestVectorEquals<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2;
            do
            {
                values2 = GenerateRandomValuesForVector<T>();
            }
            while (Util.AnyEqual(values1, values2));

            Array.Copy(values1, values2, Vector<T>.Count / 2);
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            Vector<T> result = Vector.Equals(vec1, vec2);
            for (int g = 0; g < Vector<T>.Count / 2; g++)
            {
                Assert.Equal(GetValueWithAllOnesSet<T>(), result[g]);
            }
            for (int g = Vector<T>.Count / 2; g < Vector<T>.Count; g++)
            {
                Assert.Equal((T)(dynamic)0, result[g]);
            }
        }

        [Fact]
        public void VectorEqualsAnyByte() { TestVectorEqualsAny<byte>(); }
        [Fact]
        public void VectorEqualsAnySByte() { TestVectorEqualsAny<sbyte>(); }
        [Fact]
        public void VectorEqualsAnyUInt16() { TestVectorEqualsAny<ushort>(); }
        [Fact]
        public void VectorEqualsAnyInt16() { TestVectorEqualsAny<short>(); }
        [Fact]
        public void VectorEqualsAnyUInt32() { TestVectorEqualsAny<uint>(); }
        [Fact]
        public void VectorEqualsAnyInt32() { TestVectorEqualsAny<int>(); }
        [Fact]
        public void VectorEqualsAnyUInt64() { TestVectorEqualsAny<ulong>(); }
        [Fact]
        public void VectorEqualsAnyInt64() { TestVectorEqualsAny<long>(); }
        [Fact]
        public void VectorEqualsAnySingle() { TestVectorEqualsAny<float>(); }
        [Fact]
        public void VectorEqualsAnyDouble() { TestVectorEqualsAny<double>(); }
        private void TestVectorEqualsAny<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2;
            do
            {
                values2 = GenerateRandomValuesForVector<T>();
            }
            while (Util.AnyEqual(values1, values2));

            Array.Copy(values1, values2, Vector<T>.Count / 2);
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            bool result = Vector.EqualsAny(vec1, vec2);
            Assert.True(result);

            do
            {
                values2 = GenerateRandomValuesForVector<T>();
            }
            while (Util.AnyEqual(values1, values2));

            vec2 = new Vector<T>(values2);
            result = Vector.EqualsAny(vec1, vec2);
            Assert.False(result);
        }

        [Fact]
        public void VectorEqualsAllByte() { TestVectorEqualsAll<byte>(); }
        [Fact]
        public void VectorEqualsAllSByte() { TestVectorEqualsAll<sbyte>(); }
        [Fact]
        public void VectorEqualsAllUInt16() { TestVectorEqualsAll<ushort>(); }
        [Fact]
        public void VectorEqualsAllInt16() { TestVectorEqualsAll<short>(); }
        [Fact]
        public void VectorEqualsAllUInt32() { TestVectorEqualsAll<uint>(); }
        [Fact]
        public void VectorEqualsAllInt32() { TestVectorEqualsAll<int>(); }
        [Fact]
        public void VectorEqualsAllUInt64() { TestVectorEqualsAll<ulong>(); }
        [Fact]
        public void VectorEqualsAllInt64() { TestVectorEqualsAll<long>(); }
        [Fact]
        public void VectorEqualsAllSingle() { TestVectorEqualsAll<float>(); }
        [Fact]
        public void VectorEqualsAllDouble() { TestVectorEqualsAll<double>(); }
        private void TestVectorEqualsAll<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2;
            do
            {
                values2 = GenerateRandomValuesForVector<T>();
            }
            while (Util.AnyEqual(values1, values2));

            Array.Copy(values1, values2, Vector<T>.Count / 2);
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            bool result = Vector.EqualsAll(vec1, vec2);
            Assert.False(result);

            result = Vector.EqualsAny(vec1, vec1);
            Assert.True(result);
        }
        #endregion

        #region Selection Tests
        [Fact]
        public void ConditionalSelectByte() { TestConditionalSelect<byte>(); }
        [Fact]
        public void ConditionalSelectSByte() { TestConditionalSelect<sbyte>(); }
        [Fact]
        public void ConditionalSelectUInt16() { TestConditionalSelect<ushort>(); }
        [Fact]
        public void ConditionalSelectInt16() { TestConditionalSelect<short>(); }
        [Fact]
        public void ConditionalSelectUInt32() { TestConditionalSelect<uint>(); }
        [Fact]
        public void ConditionalSelectInt32() { TestConditionalSelect<int>(); }
        [Fact]
        public void ConditionalSelectUInt64() { TestConditionalSelect<ulong>(); }
        [Fact]
        public void ConditionalSelectInt64() { TestConditionalSelect<long>(); }
        [Fact]
        public void ConditionalSelectSingle() { TestConditionalSelect<float>(); }
        [Fact]
        public void ConditionalSelectDouble() { TestConditionalSelect<double>(); }
        private void TestConditionalSelect<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            // Using Greater Than mask
            Vector<T> mask = Vector.GreaterThan(vec1, vec2);
            Vector<T> result = Vector.ConditionalSelect(mask, vec1, vec2);
            ValidateVector(result,
                (index, val) =>
                {
                    bool isGreater = Util.GreaterThan(values1[index], values2[index]);
                    T expected = isGreater ? values1[index] : values2[index];
                    Assert.Equal(expected, val);
                });

            // Using Less Than Or Equal mask
            Vector<T> mask2 = Vector.LessThanOrEqual(vec1, vec2);
            Vector<T> result2 = Vector.ConditionalSelect(mask2, vec1, vec2);
            ValidateVector(result2,
                (index, val) =>
                {
                    bool isLessOrEqual = Util.LessThanOrEqual(values1[index], values2[index]);
                    T expected = isLessOrEqual ? values1[index] : values2[index];
                    Assert.Equal(expected, val);
                });
        }
        #endregion

        #region Vector Tests
        [Fact]
        public void DotProductByte() { TestDotProduct<byte>(); }
        [Fact]
        public void DotProductSByte() { TestDotProduct<sbyte>(); }
        [Fact]
        public void DotProductUInt16() { TestDotProduct<ushort>(); }
        [Fact]
        public void DotProductInt16() { TestDotProduct<short>(); }
        [Fact]
        public void DotProductUInt32() { TestDotProduct<uint>(); }
        [Fact]
        public void DotProductInt32() { TestDotProduct<int>(); }
        [Fact]
        public void DotProductUInt64() { TestDotProduct<ulong>(); }
        [Fact]
        public void DotProductInt64() { TestDotProduct<long>(); }
        [Fact]
        public void DotProductSingle() { TestDotProduct<float>(); }
        [Fact]
        public void DotProductDouble() { TestDotProduct<double>(); }
        private void TestDotProduct<T>() where T : struct
        {
            T[] values1 = Util.GenerateRandomValues<T>(Vector<T>.Count);
            T[] values2 = Util.GenerateRandomValues<T>(Vector<T>.Count);
            Vector<T> vector1 = new Vector<T>(values1);
            Vector<T> vector2 = new Vector<T>(values2);

            T dotProduct = Vector.Dot(vector1, vector2);
            T expected = Util.Zero<T>();
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                expected = Util.Add(expected, Util.Multiply(values1[g], values2[g]));
            }
            Assert.Equal(expected, dotProduct);
        }

        [Fact]
        public void MaxByte() { TestMax<byte>(); }
        [Fact]
        public void MaxSByte() { TestMax<sbyte>(); }
        [Fact]
        public void MaxUInt16() { TestMax<ushort>(); }
        [Fact]
        public void MaxInt16() { TestMax<short>(); }
        [Fact]
        public void MaxUInt32() { TestMax<uint>(); }
        [Fact]
        public void MaxInt32() { TestMax<int>(); }
        [Fact]
        public void MaxUInt64() { TestMax<ulong>(); }
        [Fact]
        public void MaxInt64() { TestMax<long>(); }
        [Fact]
        public void MaxSingle() { TestMax<float>(); }
        [Fact]
        public void MaxDouble() { TestMax<double>(); }
        private void TestMax<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> vector1 = new Vector<T>(values1);
            Vector<T> vector2 = new Vector<T>(values2);

            Vector<T> maxVector = Vector.Max(vector1, vector2);
            ValidateVector(maxVector,
                (index, val) =>
                {
                    T expected = Util.GreaterThan(values1[index], values2[index]) ? values1[index] : values2[index];
                    Assert.Equal(expected, val);
                });
        }

        [Fact]
        public void MinByte() { TestMin<byte>(); }
        [Fact]
        public void MinSByte() { TestMin<sbyte>(); }
        [Fact]
        public void MinUInt16() { TestMin<ushort>(); }
        [Fact]
        public void MinInt16() { TestMin<short>(); }
        [Fact]
        public void MinUInt32() { TestMin<uint>(); }
        [Fact]
        public void MinInt32() { TestMin<int>(); }
        [Fact]
        public void MinUInt64() { TestMin<ulong>(); }
        [Fact]
        public void MinInt64() { TestMin<long>(); }
        [Fact]
        public void MinSingle() { TestMin<float>(); }
        [Fact]
        public void MinDouble() { TestMin<double>(); }
        private void TestMin<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> vector1 = new Vector<T>(values1);
            Vector<T> vector2 = new Vector<T>(values2);

            Vector<T> minVector = Vector.Min(vector1, vector2);
            ValidateVector(minVector,
                (index, val) =>
                {
                    T expected = Util.LessThan(values1[index], values2[index]) ? values1[index] : values2[index];
                    Assert.Equal(expected, val);
                });
        }

        [Fact]
        public void SquareRootByte() { TestSquareRoot<byte>(-1); }
        [Fact]
        public void SquareRootSByte() { TestSquareRoot<sbyte>(-1); }
        [Fact]
        public void SquareRootUInt16() { TestSquareRoot<ushort>(-1); }
        [Fact]
        public void SquareRootInt16() { TestSquareRoot<short>(-1); }
        [Fact]
        public void SquareRootUInt32() { TestSquareRoot<uint>(-1); }
        [Fact]
        public void SquareRootInt32() { TestSquareRoot<int>(-1); }
        [Fact]
        public void SquareRootUInt64() { TestSquareRoot<ulong>(-1); }
        [Fact]
        public void SquareRootInt64() { TestSquareRoot<long>(-1); }
        [Fact]
        public void SquareRootSingle() { TestSquareRoot<float>(6); }
        [Fact]
        public void SquareRootDouble() { TestSquareRoot<double>(15); }
        private void TestSquareRoot<T>(int precision = -1) where T : struct, IEquatable<T>
        {
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector = new Vector<T>(values);

            Vector<T> squareRootVector = Vector.SquareRoot(vector);
            ValidateVector(squareRootVector,
                (index, val) =>
                {
                    T expected = Util.Sqrt(values[index]);
                    AssertEqual(expected, val, $"SquareRoot( {FullString(values[index])} )", precision);
                });
        }

        [Fact]
        public void CeilingSingle()
        {
            float[] values = GenerateRandomValuesForVector<float>();
            Vector<float> vector = new Vector<float>(values);

            Vector<float> ceilVector = Vector.Ceiling(vector);
            ValidateVector(ceilVector,
                (index, val) =>
                {
                    float expected = MathF.Ceiling(values[index]);
                    AssertEqual(expected, val, $"Ceiling( {FullString(values[index])} )", -1);
                });
        }

        [Fact]
        public void CeilingDouble()
        {
            double[] values = GenerateRandomValuesForVector<double>();
            Vector<double> vector = new Vector<double>(values);

            Vector<double> ceilVector = Vector.Ceiling(vector);
            ValidateVector(ceilVector,
                (index, val) =>
                {
                    double expected = Math.Ceiling(values[index]);
                    AssertEqual(expected, val, $"Ceiling( {FullString(values[index])} )", -1);
                });
        }

        [Fact]
        public void FloorSingle()
        {
            float[] values = GenerateRandomValuesForVector<float>();
            Vector<float> vector = new Vector<float>(values);

            Vector<float> ceilVector = Vector.Floor(vector);
            ValidateVector(ceilVector,
                (index, val) =>
                {
                    float expected = MathF.Floor(values[index]);
                    AssertEqual(expected, val, $"Ceiling( {FullString(values[index])} )", -1);
                });
        }

        [Fact]
        public void FloorDouble()
        {
            double[] values = GenerateRandomValuesForVector<double>();
            Vector<double> vector = new Vector<double>(values);

            Vector<double> ceilVector = Vector.Floor(vector);
            ValidateVector(ceilVector,
                (index, val) =>
                {
                    double expected = Math.Floor(values[index]);
                    AssertEqual(expected, val, $"Ceiling( {FullString(values[index])} )", -1);
                });
        }

        [Fact]
        public void AbsByte() { TestAbs<byte>(); }
        [Fact]
        public void AbsSByte() { TestAbs<sbyte>(); }
        [Fact]
        public void AbsUInt16() { TestAbs<ushort>(); }
        [Fact]
        public void AbsInt16() { TestAbs<short>(); }
        [Fact]
        public void AbsUInt32() { TestAbs<uint>(); }
        [Fact]
        public void AbsInt32() { TestAbs<int>(); }
        [Fact]
        public void AbsUInt64() { TestAbs<ulong>(); }
        [Fact]
        public void AbsInt64() { TestAbs<long>(); }
        [Fact]
        public void AbsSingle() { TestAbs<float>(); }
        [Fact]
        public void AbsDouble() { TestAbs<double>(); }
        private void TestAbs<T>() where T : struct
        {
            T[] values = Util.GenerateRandomValues<T>(Vector<T>.Count, GetMinValue<T>() + 1, GetMaxValue<T>());
            Vector<T> vector = new Vector<T>(values);
            Vector<T> AbsVector = Vector.Abs(vector);
            ValidateVector(AbsVector,
                (index, val) =>
                {
                    T expected = Util.Abs(values[index]);
                    Assert.Equal(expected, val);
                });
        }

        #endregion

        #region Reflection Tests
        // These tests ensure that, when invoked through reflection, methods behave as expected. There are potential
        // oddities when intrinsic methods are invoked through reflection which could have unexpected effects for the developer.
        [Fact]
        public void MultiplicationReflectionByte() { TestMultiplicationReflection<byte>(); }
        [Fact]
        public void MultiplicationReflectionSByte() { TestMultiplicationReflection<sbyte>(); }
        [Fact]
        public void MultiplicationReflectionUInt16() { TestMultiplicationReflection<ushort>(); }
        [Fact]
        public void MultiplicationReflectionInt16() { TestMultiplicationReflection<short>(); }
        [Fact]
        public void MultiplicationReflectionUInt32() { TestMultiplicationReflection<uint>(); }
        [Fact]
        public void MultiplicationReflectionInt32() { TestMultiplicationReflection<int>(); }
        [Fact]
        public void MultiplicationReflectionUInt64() { TestMultiplicationReflection<ulong>(); }
        [Fact]
        public void MultiplicationReflectionInt64() { TestMultiplicationReflection<long>(); }
        [Fact]
        public void MultiplicationReflectionSingle() { TestMultiplicationReflection<float>(); }
        [Fact]
        public void MultiplicationReflectionDouble() { TestMultiplicationReflection<double>(); }
        private void TestMultiplicationReflection<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var multOperatorMethod = typeof(Vector<T>).GetTypeInfo().GetDeclaredMethods("op_Multiply")
                .Where(mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(Vector<T>), typeof(Vector<T>) }))
                .Single();
            Vector<T> sum = (Vector<T>)multOperatorMethod.Invoke(null, new object[] { v1, v2 });
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Multiply(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void AdditionReflectionByte() { TestAdditionReflection<byte>(); }
        [Fact]
        public void AdditionReflectionSByte() { TestAdditionReflection<sbyte>(); }
        [Fact]
        public void AdditionReflectionUInt16() { TestAdditionReflection<ushort>(); }
        [Fact]
        public void AdditionReflectionInt16() { TestAdditionReflection<short>(); }
        [Fact]
        public void AdditionReflectionUInt32() { TestAdditionReflection<uint>(); }
        [Fact]
        public void AdditionReflectionInt32() { TestAdditionReflection<int>(); }
        [Fact]
        public void AdditionReflectionUInt64() { TestAdditionReflection<ulong>(); }
        [Fact]
        public void AdditionReflectionInt64() { TestAdditionReflection<long>(); }
        [Fact]
        public void AdditionReflectionSingle() { TestAdditionReflection<float>(); }
        [Fact]
        public void AdditionReflectionDouble() { TestAdditionReflection<double>(); }
        private void TestAdditionReflection<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var addOperatorMethod = typeof(Vector<T>).GetTypeInfo().GetDeclaredMethods("op_Addition")
                .Where(mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(Vector<T>), typeof(Vector<T>) }))
                .Single();
            Vector<T> sum = (Vector<T>)addOperatorMethod.Invoke(null, new object[] { v1, v2 });
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Add(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void DivisionReflectionByte() { TestDivisionReflection<byte>(); }
        [Fact]
        public void DivisionReflectionSByte() { TestDivisionReflection<sbyte>(); }
        [Fact]
        public void DivisionReflectionUInt16() { TestDivisionReflection<ushort>(); }
        [Fact]
        public void DivisionReflectionInt16() { TestDivisionReflection<short>(); }
        [Fact]
        public void DivisionReflectionUInt32() { TestDivisionReflection<uint>(); }
        [Fact]
        public void DivisionReflectionInt32() { TestDivisionReflection<int>(); }
        [Fact]
        public void DivisionReflectionUInt64() { TestDivisionReflection<ulong>(); }
        [Fact]
        public void DivisionReflectionInt64() { TestDivisionReflection<long>(); }
        [Fact]
        public void DivisionReflectionSingle() { TestDivisionReflection<float>(); }
        [Fact]
        public void DivisionReflectionDouble() { TestDivisionReflection<double>(); }
        private void TestDivisionReflection<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            values1 = values1.Select(val => val.Equals(Util.Zero<T>()) ? Util.One<T>() : val).ToArray(); // Avoid divide-by-zero
            T[] values2 = GenerateRandomValuesForVector<T>();
            values2 = values2.Select(val => val.Equals(Util.Zero<T>()) ? Util.One<T>() : val).ToArray(); // Avoid divide-by-zero
            // I replace all Zero's with One's above to avoid Divide-by-zero.

            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var divideOperatorMethod = typeof(Vector<T>).GetTypeInfo().GetDeclaredMethods("op_Division")
                .Where(mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(Vector<T>), typeof(Vector<T>) }))
                .Single();
            Vector<T> sum = (Vector<T>)divideOperatorMethod.Invoke(null, new object[] { v1, v2 });
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Divide(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void ConstructorSingleValueReflectionByte() { TestConstructorSingleValueReflection<byte>(); }
        [Fact]
        public void ConstructorSingleValueReflectionSByte() { TestConstructorSingleValueReflection<sbyte>(); }
        [Fact]
        public void ConstructorSingleValueReflectionUInt16() { TestConstructorSingleValueReflection<ushort>(); }
        [Fact]
        public void ConstructorSingleValueReflectionInt16() { TestConstructorSingleValueReflection<short>(); }
        [Fact]
        public void ConstructorSingleValueReflectionUInt32() { TestConstructorSingleValueReflection<uint>(); }
        [Fact]
        public void ConstructorSingleValueReflectionInt32() { TestConstructorSingleValueReflection<int>(); }
        [Fact]
        public void ConstructorSingleValueReflectionUInt64() { TestConstructorSingleValueReflection<ulong>(); }
        [Fact]
        public void ConstructorSingleValueReflectionInt64() { TestConstructorSingleValueReflection<long>(); }
        [Fact]
        public void ConstructorSingleValueReflectionSingle() { TestConstructorSingleValueReflection<float>(); }
        [Fact]
        public void ConstructorSingleValueReflectionDouble() { TestConstructorSingleValueReflection<double>(); }
        private void TestConstructorSingleValueReflection<T>() where T : struct
        {
            ConstructorInfo constructor = typeof(Vector<T>).GetTypeInfo().DeclaredConstructors
                .Where(ci => ci.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(T) }))
                .Single();
            T constantValue = Util.GenerateSingleValue<T>();
            Vector<T> vec = (Vector<T>)constructor.Invoke(new object[] { constantValue });
            ValidateVector(vec, (index, value) =>
                {
                    for (int g = 0; g < Vector<T>.Count; g++)
                    {
                        Assert.Equal(constantValue, vec[g]);
                    }
                });
        }

        [Fact]
        public void ConstructorArrayReflectionByte() { TestConstructorArrayReflection<byte>(); }
        [Fact]
        public void ConstructorArrayReflectionSByte() { TestConstructorArrayReflection<sbyte>(); }
        [Fact]
        public void ConstructorArrayReflectionUInt16() { TestConstructorArrayReflection<ushort>(); }
        [Fact]
        public void ConstructorArrayReflectionInt16() { TestConstructorArrayReflection<short>(); }
        [Fact]
        public void ConstructorArrayReflectionUInt32() { TestConstructorArrayReflection<uint>(); }
        [Fact]
        public void ConstructorArrayReflectionInt32() { TestConstructorArrayReflection<int>(); }
        [Fact]
        public void ConstructorArrayReflectionUInt64() { TestConstructorArrayReflection<ulong>(); }
        [Fact]
        public void ConstructorArrayReflectionInt64() { TestConstructorArrayReflection<long>(); }
        [Fact]
        public void ConstructorArrayReflectionSingle() { TestConstructorArrayReflection<float>(); }
        [Fact]
        public void ConstructorArrayReflectionDouble() { TestConstructorArrayReflection<double>(); }
        private void TestConstructorArrayReflection<T>() where T : struct
        {
            ConstructorInfo constructor = typeof(Vector<T>).GetTypeInfo().DeclaredConstructors
                .Where(ci => ci.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(T[]) }))
                .Single();
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vec = (Vector<T>)constructor.Invoke(new object[] { values });
            ValidateVector(vec, (index, value) =>
                {
                    for (int g = 0; g < Vector<T>.Count; g++)
                    {
                        Assert.Equal(values[g], vec[g]);
                    }
                });
        }

        [Fact]
        public void CopyToReflectionByte() { TestCopyToReflection<byte>(); }
        [Fact]
        public void CopyToReflectionSByte() { TestCopyToReflection<sbyte>(); }
        [Fact]
        public void CopyToReflectionUInt16() { TestCopyToReflection<ushort>(); }
        [Fact]
        public void CopyToReflectionInt16() { TestCopyToReflection<short>(); }
        [Fact]
        public void CopyToReflectionUInt32() { TestCopyToReflection<uint>(); }
        [Fact]
        public void CopyToReflectionInt32() { TestCopyToReflection<int>(); }
        [Fact]
        public void CopyToReflectionUInt64() { TestCopyToReflection<ulong>(); }
        [Fact]
        public void CopyToReflectionInt64() { TestCopyToReflection<long>(); }
        [Fact]
        public void CopyToReflectionSingle() { TestCopyToReflection<float>(); }
        [Fact]
        public void CopyToReflectionDouble() { TestCopyToReflection<double>(); }
        private void TestCopyToReflection<T>() where T : struct
        {
            MethodInfo copyToMethod = typeof(Vector<T>).GetTypeInfo().GetDeclaredMethods("CopyTo")
                .Where(mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(T[]) }))
                .Single();
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector = new Vector<T>(values);
            T[] array = new T[Vector<T>.Count];
            copyToMethod.Invoke(vector, new object[] { array });
            for (int g = 0; g < array.Length; g++)
            {
                Assert.Equal(values[g], array[g]);
                Assert.Equal(vector[g], array[g]);
            }
        }

        [Fact]
        public void CopyToWithOffsetReflectionByte() { TestCopyToWithOffsetReflection<byte>(); }
        [Fact]
        public void CopyToWithOffsetReflectionSByte() { TestCopyToWithOffsetReflection<sbyte>(); }
        [Fact]
        public void CopyToWithOffsetReflectionUInt16() { TestCopyToWithOffsetReflection<ushort>(); }
        [Fact]
        public void CopyToWithOffsetReflectionInt16() { TestCopyToWithOffsetReflection<short>(); }
        [Fact]
        public void CopyToWithOffsetReflectionUInt32() { TestCopyToWithOffsetReflection<uint>(); }
        [Fact]
        public void CopyToWithOffsetReflectionInt32() { TestCopyToWithOffsetReflection<int>(); }
        [Fact]
        public void CopyToWithOffsetReflectionUInt64() { TestCopyToWithOffsetReflection<ulong>(); }
        [Fact]
        public void CopyToWithOffsetReflectionInt64() { TestCopyToWithOffsetReflection<long>(); }
        [Fact]
        public void CopyToWithOffsetReflectionSingle() { TestCopyToWithOffsetReflection<float>(); }
        [Fact]
        public void CopyToWithOffsetReflectionDouble() { TestCopyToWithOffsetReflection<double>(); }
        private void TestCopyToWithOffsetReflection<T>() where T : struct
        {
            MethodInfo copyToMethod = typeof(Vector<T>).GetTypeInfo().GetDeclaredMethods("CopyTo")
                .Where(mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(T[]), typeof(int) }))
                .Single();
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector = new Vector<T>(values);
            int offset = Util.GenerateSingleValue<int>();
            T[] array = new T[Vector<T>.Count + offset];
            copyToMethod.Invoke(vector, new object[] { array, offset });
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                Assert.Equal(values[g], array[g + offset]);
                Assert.Equal(vector[g], array[g + offset]);
            }
        }

        [Fact]
        public void CountViaReflectionConsistencyByte() { TestCountViaReflectionConsistency<byte>(); }
        [Fact]
        public void CountViaReflectionConsistencySByte() { TestCountViaReflectionConsistency<sbyte>(); }
        [Fact]
        public void CountViaReflectionConsistencyUInt16() { TestCountViaReflectionConsistency<ushort>(); }
        [Fact]
        public void CountViaReflectionConsistencyInt16() { TestCountViaReflectionConsistency<short>(); }
        [Fact]
        public void CountViaReflectionConsistencyUInt32() { TestCountViaReflectionConsistency<uint>(); }
        [Fact]
        public void CountViaReflectionConsistencyInt32() { TestCountViaReflectionConsistency<int>(); }
        [Fact]
        public void CountViaReflectionConsistencyUInt64() { TestCountViaReflectionConsistency<ulong>(); }
        [Fact]
        public void CountViaReflectionConsistencyInt64() { TestCountViaReflectionConsistency<long>(); }
        [Fact]
        public void CountViaReflectionConsistencySingle() { TestCountViaReflectionConsistency<float>(); }
        [Fact]
        public void CountViaReflectionConsistencyDouble() { TestCountViaReflectionConsistency<double>(); }
        private void TestCountViaReflectionConsistency<T>() where T : struct
        {
            MethodInfo countMethod = typeof(Vector<T>).GetTypeInfo().GetDeclaredProperty("Count").GetMethod;
            int valueFromReflection = (int)countMethod.Invoke(null, null);
            int valueFromNormalCall = Vector<T>.Count;
            Assert.Equal(valueFromNormalCall, valueFromReflection);
        }
        #endregion Reflection Tests

        #region Same-Size Conversions
        [Fact]
        public void ConvertInt32ToSingle()
        {
            int[] source = GenerateRandomValuesForVector<int>();
            Vector<int> sourceVec = new Vector<int>(source);
            Vector<float> targetVec = Vector.ConvertToSingle(sourceVec);
            for (int i = 0; i < Vector<float>.Count; i++)
            {
                Assert.Equal(unchecked((float)source[i]), targetVec[i]);
            }
        }

        [Fact]
        public void ConvertUInt32ToSingle()
        {
            uint[] source = GenerateRandomValuesForVector<uint>();
            Vector<uint> sourceVec = new Vector<uint>(source);
            Vector<float> targetVec = Vector.ConvertToSingle(sourceVec);
            for (int i = 0; i < Vector<float>.Count; i++)
            {
                Assert.Equal(unchecked((float)source[i]), targetVec[i]);
            }
        }

        [Fact]
        public void ConvertInt64ToDouble()
        {
            long[] source = GenerateRandomValuesForVector<long>();
            Vector<long> sourceVec = new Vector<long>(source);
            Vector<double> targetVec = Vector.ConvertToDouble(sourceVec);
            for (int i = 0; i < Vector<double>.Count; i++)
            {
                Assert.Equal(unchecked((double)source[i]), targetVec[i]);
            }
        }

        [Fact]
        public void ConvertUInt64ToDouble()
        {
            ulong[] source = GenerateRandomValuesForVector<ulong>();
            Vector<ulong> sourceVec = new Vector<ulong>(source);
            Vector<double> targetVec = Vector.ConvertToDouble(sourceVec);
            for (int i = 0; i < Vector<double>.Count; i++)
            {
                Assert.Equal(unchecked((double)source[i]), targetVec[i]);
            }
        }

        [Fact]
        public void ConvertSingleToInt32()
        {
            float[] source = GenerateRandomValuesForVector<float>();
            Vector<float> sourceVec = new Vector<float>(source);
            Vector<int> targetVec = Vector.ConvertToInt32(sourceVec);
            for (int i = 0; i < Vector<int>.Count; i++)
            {
                Assert.Equal(unchecked((int)source[i]), targetVec[i]);
            }
        }

        [Fact]
        public void ConvertSingleToUInt32()
        {
            float[] source = GenerateRandomValuesForVector<float>();
            Vector<float> sourceVec = new Vector<float>(source);
            Vector<uint> targetVec = Vector.ConvertToUInt32(sourceVec);
            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                Assert.Equal(unchecked((uint)source[i]), targetVec[i]);
            }
        }

        [Fact]
        public void ConvertDoubleToInt64()
        {
            double[] source = GenerateRandomValuesForVector<double>();
            Vector<double> sourceVec = new Vector<double>(source);
            Vector<long> targetVec = Vector.ConvertToInt64(sourceVec);
            for (int i = 0; i < Vector<long>.Count; i++)
            {
                Assert.Equal(unchecked((long)source[i]), targetVec[i]);
            }
        }

        [Fact]
        public void ConvertDoubleToUInt64()
        {
            double[] source = GenerateRandomValuesForVector<double>();
            Vector<double> sourceVec = new Vector<double>(source);
            Vector<ulong> targetVec = Vector.ConvertToUInt64(sourceVec);
            for (int i = 0; i < Vector<ulong>.Count; i++)
            {
                Assert.Equal(unchecked((ulong)source[i]), targetVec[i]);
            }
        }

        #endregion Same-Size Conversions

        #region Narrow / Widen
        [Fact]
        public void WidenByte()
        {
            byte[] source = GenerateRandomValuesForVector<byte>();
            Vector<byte> sourceVec = new Vector<byte>(source);
            Vector<ushort> dest1;
            Vector<ushort> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((ushort)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((ushort)source[index + Vector<ushort>.Count], val);
            });
        }

        [Fact]
        public void WidenUInt16()
        {
            ushort[] source = GenerateRandomValuesForVector<ushort>();
            Vector<ushort> sourceVec = new Vector<ushort>(source);
            Vector<uint> dest1;
            Vector<uint> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((uint)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((uint)source[index + Vector<uint>.Count], val);
            });
        }

        [Fact]
        public void WidenUInt32()
        {
            uint[] source = GenerateRandomValuesForVector<uint>();
            Vector<uint> sourceVec = new Vector<uint>(source);
            Vector<ulong> dest1;
            Vector<ulong> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((ulong)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((ulong)source[index + Vector<ulong>.Count], val);
            });
        }

        [Fact]
        public void WidenSByte()
        {
            sbyte[] source = GenerateRandomValuesForVector<sbyte>();
            Vector<sbyte> sourceVec = new Vector<sbyte>(source);
            Vector<short> dest1;
            Vector<short> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((short)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((short)source[index + Vector<short>.Count], val);
            });
        }

        [Fact]
        public void WidenInt16()
        {
            short[] source = GenerateRandomValuesForVector<short>();
            Vector<short> sourceVec = new Vector<short>(source);
            Vector<int> dest1;
            Vector<int> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((int)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((int)source[index + Vector<int>.Count], val);
            });
        }

        [Fact]
        public void WidenInt32()
        {
            int[] source = GenerateRandomValuesForVector<int>();
            Vector<int> sourceVec = new Vector<int>(source);
            Vector<long> dest1;
            Vector<long> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((long)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((long)source[index + Vector<long>.Count], val);
            });
        }

        [Fact]
        public void WidenSingle()
        {
            float[] source = GenerateRandomValuesForVector<float>();
            Vector<float> sourceVec = new Vector<float>(source);
            Vector<double> dest1;
            Vector<double> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((double)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((double)source[index + Vector<double>.Count], val);
            });
        }


        [Fact]
        public void NarrowUInt16()
        {
            ushort[] source1 = GenerateRandomValuesForVector<ushort>();
            ushort[] source2 = GenerateRandomValuesForVector<ushort>();
            Vector<ushort> sourceVec1 = new Vector<ushort>(source1);
            Vector<ushort> sourceVec2 = new Vector<ushort>(source2);
            Vector<byte> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<ushort>.Count; i++)
            {
                Assert.Equal(unchecked((byte)source1[i]), dest[i]);
            }
            for (int i = 0; i < Vector<ushort>.Count; i++)
            {
                Assert.Equal(unchecked((byte)source2[i]), dest[i + Vector<ushort>.Count]);
            }
        }

        [Fact]
        public void NarrowUInt32()
        {
            uint[] source1 = GenerateRandomValuesForVector<uint>();
            uint[] source2 = GenerateRandomValuesForVector<uint>();
            Vector<uint> sourceVec1 = new Vector<uint>(source1);
            Vector<uint> sourceVec2 = new Vector<uint>(source2);
            Vector<ushort> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                Assert.Equal(unchecked((ushort)source1[i]), dest[i]);
            }
            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                Assert.Equal(unchecked((ushort)source2[i]), dest[i + Vector<uint>.Count]);
            }
        }

        [Fact]
        public void NarrowUInt64()
        {
            ulong[] source1 = GenerateRandomValuesForVector<ulong>();
            ulong[] source2 = GenerateRandomValuesForVector<ulong>();
            Vector<ulong> sourceVec1 = new Vector<ulong>(source1);
            Vector<ulong> sourceVec2 = new Vector<ulong>(source2);
            Vector<uint> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<ulong>.Count; i++)
            {
                Assert.Equal(unchecked((uint)source1[i]), dest[i]);
            }
            for (int i = 0; i < Vector<ulong>.Count; i++)
            {
                Assert.Equal(unchecked((uint)source2[i]), dest[i + Vector<ulong>.Count]);
            }
        }

        [Fact]
        public void NarrowInt16()
        {
            short[] source1 = GenerateRandomValuesForVector<short>();
            short[] source2 = GenerateRandomValuesForVector<short>();
            Vector<short> sourceVec1 = new Vector<short>(source1);
            Vector<short> sourceVec2 = new Vector<short>(source2);
            Vector<sbyte> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<short>.Count; i++)
            {
                Assert.Equal(unchecked((sbyte)source1[i]), dest[i]);
            }
            for (int i = 0; i < Vector<short>.Count; i++)
            {
                Assert.Equal(unchecked((sbyte)source2[i]), dest[i + Vector<short>.Count]);
            }
        }

        [Fact]
        public void NarrowInt32()
        {
            int[] source1 = GenerateRandomValuesForVector<int>();
            int[] source2 = GenerateRandomValuesForVector<int>();
            Vector<int> sourceVec1 = new Vector<int>(source1);
            Vector<int> sourceVec2 = new Vector<int>(source2);
            Vector<short> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                Assert.Equal(unchecked((short)source1[i]), dest[i]);
            }
            for (int i = 0; i < Vector<int>.Count; i++)
            {
                Assert.Equal(unchecked((short)source2[i]), dest[i + Vector<int>.Count]);
            }
        }

        [Fact]
        public void NarrowInt64()
        {
            long[] source1 = GenerateRandomValuesForVector<long>();
            long[] source2 = GenerateRandomValuesForVector<long>();
            Vector<long> sourceVec1 = new Vector<long>(source1);
            Vector<long> sourceVec2 = new Vector<long>(source2);
            Vector<int> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<long>.Count; i++)
            {
                Assert.Equal(unchecked((int)source1[i]), dest[i]);
            }
            for (int i = 0; i < Vector<long>.Count; i++)
            {
                Assert.Equal(unchecked((int)source2[i]), dest[i + Vector<long>.Count]);
            }
        }

        [Fact]
        public void NarrowDouble()
        {
            double[] source1 = GenerateRandomValuesForVector<double>();
            double[] source2 = GenerateRandomValuesForVector<double>();
            Vector<double> sourceVec1 = new Vector<double>(source1);
            Vector<double> sourceVec2 = new Vector<double>(source2);
            Vector<float> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<double>.Count; i++)
            {
                Assert.Equal(unchecked((float)source1[i]), dest[i]);
            }
            for (int i = 0; i < Vector<double>.Count; i++)
            {
                Assert.Equal(unchecked((float)source2[i]), dest[i + Vector<double>.Count]);
            }
        }

        #endregion Narrow / Widen

        #region Explicit Cast/As
        [Fact]
        public void TestCastByteToInt() => TestCastToInt<byte>();

        [Fact]
        public void TestCastSByteToInt() => TestCastToInt<sbyte>();

        [Fact]
        public void TestCastInt16ToInt() => TestCastToInt<short>();

        [Fact]
        public void TestCastUInt16ToInt() => TestCastToInt<ushort>();

        [Fact]
        public void TestCastInt32ToInt() => TestCastToInt<int>();

        [Fact]
        public void TestCastUInt32ToInt() => TestCastToInt<uint>();

        [Fact]
        public void TestCastInt64ToInt() => TestCastToInt<long>();

        [Fact]
        public void TestCastUInt64ToInt() => TestCastToInt<ulong>();

        [Fact]
        public void TestCastSingleToInt() => TestCastToInt<float>();

        [Fact]
        public void TestCastDoubleToInt() => TestCastToInt<double>();

        private unsafe void TestCastToInt<T>() where T : unmanaged
        {
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector1 = new Vector<T>(values);
            Vector<int> vector2 = (Vector<int>)vector1;

            var vector1Bytes = new byte[sizeof(T) * Vector<T>.Count];
            vector1.CopyTo(vector1Bytes);
            var vector2Bytes = new byte[sizeof(int) * Vector<int>.Count];
            vector2.CopyTo(vector2Bytes);

            Assert.Equal(vector1Bytes, vector2Bytes);
        }

        [Fact]
        public void TestAsIntToByte() => TestAs<int, byte>();

        [Fact]
        public void TestAsIntToSByte() => TestAs<int, sbyte>();

        [Fact]
        public void TestAsIntToInt16() => TestAs<int, short>();

        [Fact]
        public void TestAsIntToUInt16() => TestAs<int, ushort>();

        [Fact]
        public void TestAsIntToInt32() => TestAs<int, int>();

        [Fact]
        public void TestAsIntToUInt32() => TestAs<int, uint>();

        [Fact]
        public void TestAsIntToInt64() => TestAs<int, long>();

        [Fact]
        public void TestAsIntToUInt64() => TestAs<int, ulong>();

        [Fact]
        public void TestAsIntToSingle() => TestAs<int, float>();

        [Fact]
        public void TestAsIntToDouble() => TestAs<int, double>();

        private unsafe void TestAs<TFrom, TTo>() where TFrom : unmanaged where TTo : unmanaged
        {
            TFrom[] values = GenerateRandomValuesForVector<TFrom>();
            Vector<TFrom> vector1 = new Vector<TFrom>(values);
            Vector<TTo> vector2 = vector1.As<TFrom, TTo>();

            var vector1Bytes = new byte[sizeof(TFrom) * Vector<TFrom>.Count];
            vector1.CopyTo(vector1Bytes);
            var vector2Bytes = new byte[sizeof(TTo) * Vector<TTo>.Count];
            vector2.CopyTo(vector2Bytes);

            Assert.Equal(vector1Bytes, vector2Bytes);
        }
        #endregion

        #region Sum

        [Fact]
        public void SumInt32() => TestSum<int>(x => x.Aggregate((a, b) => a + b));

        [Fact]
        public void SumInt64() => TestSum<long>(x => x.Aggregate((a, b) => a + b));

        [Fact]
        public void SumSingle() => TestSum<float>(x => x.Aggregate((a, b) => a + b));

        [Fact]
        public void SumDouble() => TestSum<double>(x => x.Aggregate((a, b) => a + b));

        [Fact]
        public void SumUInt32() => TestSum<uint>(x => x.Aggregate((a, b) => a + b));

        [Fact]
        public void SumUInt64() => TestSum<ulong>(x => x.Aggregate((a, b) => a + b));

        [Fact]
        public void SumByte() => TestSum<byte>(x => x.Aggregate((a, b) => (byte)(a + b)));

        [Fact]
        public void SumSByte() => TestSum<sbyte>(x => x.Aggregate((a, b) => (sbyte)(a + b)));

        [Fact]
        public void SumInt16() => TestSum<short>(x => x.Aggregate((a, b) => (short)(a + b)));

        [Fact]
        public void SumUInt16() => TestSum<ushort>(x => x.Aggregate((a, b) => (ushort)(a + b)));

        private static void TestSum<T>(Func<T[], T> expected) where T : struct, IEquatable<T>
        {
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector = new(values);
            T sum = Vector.Sum(vector);

            AssertEqual(expected(values), sum, "Sum");
        }

        #endregion

        #region Helper Methods
        private static void AssertEqual<T>(T expected, T actual, string operation, int precision = -1) where T : IEquatable<T>
        {
            if (typeof(T) == typeof(float))
            {
                if (!IsDiffTolerable((float)(object)expected, (float)(object)actual, precision))
                {
                    throw new XunitException($"AssertEqual failed for operation {operation}. Expected: {expected,10:G9}, Actual: {actual,10:G9}.");
                }
            }
            else if (typeof(T) == typeof(double))
            {
                if (!IsDiffTolerable((double)(object)expected, (double)(object)actual, precision))
                {
                    throw new XunitException($"AssertEqual failed for operation {operation}. Expected: {expected,20:G17}, Actual: {actual,20:G17}.");
                }
            }
            else
            {
                if (!expected.Equals(actual))
                {
                    throw new XunitException($"AssertEqual failed for operation {operation}. Expected: {expected}, Actual: {actual}.");
                }
            }
        }

        private static bool IsDiffTolerable(double d1, double d2, int precision)
        {
            if (double.IsNaN(d1))
            {
                return double.IsNaN(d2);
            }
            if (double.IsInfinity(d1) || double.IsInfinity(d2))
            {
                return AreSameInfinity(d1, d2);
            }

            double diffRatio = (d1 - d2) / d1;
            diffRatio *= Math.Pow(10, precision);
            return Math.Abs(diffRatio) < 1;
        }

        private static bool IsDiffTolerable(float f1, float f2, int precision)
        {
            if (float.IsNaN(f1))
            {
                return float.IsNaN(f2);
            }
            if (float.IsInfinity(f1) || float.IsInfinity(f2))
            {
                return AreSameInfinity(f1, f2);
            }

            float diffRatio = (f1 - f2) / f1;
            diffRatio *= MathF.Pow(10, precision);
            return Math.Abs(diffRatio) < 1;
        }

        private static string FullString<T>(T value)
        {
            if (typeof(T) == typeof(float))
            {
                return ((float)(object)value).ToString("G9");
            }
            else if (typeof(T) == typeof(double))
            {
                return ((double)(object)value).ToString("G17");
            }
            else
            {
                return value.ToString();
            }
        }

        private static bool AreSameInfinity(double d1, double d2)
        {
            return
                double.IsNegativeInfinity(d1) == double.IsNegativeInfinity(d2) &&
                double.IsPositiveInfinity(d1) == double.IsPositiveInfinity(d2);
        }

        private static void ValidateVector<T>(Vector<T> vector, Action<int, T> indexValidationFunc) where T : struct
        {
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                indexValidationFunc(g, vector[g]);
            }
        }

        internal static T[] GenerateRandomValuesForVector<T>(int? numValues = null) where T : struct
        {
            int minValue = GetMinValue<T>();
            int maxValue = GetMaxValue<T>();
            return Util.GenerateRandomValues<T>(numValues ?? Vector<T>.Count, minValue, maxValue);
        }

        internal static int GetMinValue<T>() where T : struct
        {
            if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
            {
                return int.MinValue;
            }
            else if (typeof(T) == typeof(byte))
            {
                return byte.MinValue;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return sbyte.MinValue;
            }
            else if (typeof(T) == typeof(short))
            {
                return short.MinValue;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return ushort.MinValue;
            }
            throw new NotSupportedException();
        }

        internal static T GetMinValueExact<T>() where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)byte.MinValue;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)sbyte.MinValue;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)ushort.MinValue;
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)short.MinValue;
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)int.MinValue;
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)long.MinValue;
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.MinValue;
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)double.MinValue;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)uint.MinValue;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)ulong.MinValue;
            }
            throw new NotSupportedException();
        }

        internal static int GetMaxValue<T>() where T : struct
        {
            if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
            {
                return int.MaxValue;
            }
            else if (typeof(T) == typeof(byte))
            {
                return byte.MaxValue;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return sbyte.MaxValue;
            }
            else if (typeof(T) == typeof(short))
            {
                return short.MaxValue;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return ushort.MaxValue;
            }
            throw new NotSupportedException();
        }

        internal static T GetMaxValueExact<T>() where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)byte.MaxValue;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)sbyte.MaxValue;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)ushort.MaxValue;
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)short.MaxValue;
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)int.MaxValue;
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)long.MaxValue;
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.MaxValue;
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)double.MaxValue;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)uint.MaxValue;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)ulong.MaxValue;
            }
            throw new NotSupportedException();
        }

        internal static T GetValueWithAllOnesSet<T>() where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)byte.MaxValue;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)-1;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)ushort.MaxValue;
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)-1;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)uint.MaxValue;
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)-1;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)ulong.MaxValue;
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)-1;
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)BitConverter.Int32BitsToSingle(-1);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)BitConverter.Int64BitsToDouble(-1);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        #endregion
    }
}
