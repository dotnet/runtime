// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
            var valueSpan = new Span<T>(values);

            var vector = new Vector<T>(valueSpan);
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(values[index], val);
                });
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
        public void ConstructorWithUnsupportedTypes_Char() => TestConstructorWithUnsupportedTypes<char>();

        private void TestConstructorWithUnsupportedTypes<T>() where T : struct
        {
            Assert.Throws<NotSupportedException>(() => new Vector<T>(new Span<T>(new T[4])));
        }

        #endregion Tests for constructors using unsupported types

        #endregion Constructor Tests
    }
}
