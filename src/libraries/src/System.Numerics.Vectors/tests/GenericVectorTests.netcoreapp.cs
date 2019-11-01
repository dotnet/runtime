// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public partial class GenericVectorTests
    {
        #region Constructor Tests

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
        #endregion

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

            new Random().NextBytes(MemoryMarshal.AsBytes(destination));
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
        #endregion
    }
}
