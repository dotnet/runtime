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
        public void ConstructorWithSpanByte() => TestConstructorWithSpan<Byte>();
        [Fact]
        public void ConstructorWithSpanSByte() => TestConstructorWithSpan<SByte>();
        [Fact]
        public void ConstructorWithSpanUInt16() => TestConstructorWithSpan<UInt16>();
        [Fact]
        public void ConstructorWithSpanInt16() => TestConstructorWithSpan<Int16>();
        [Fact]
        public void ConstructorWithSpanUInt32() => TestConstructorWithSpan<UInt32>();
        [Fact]
        public void ConstructorWithSpanInt32() => TestConstructorWithSpan<Int32>();
        [Fact]
        public void ConstructorWithSpanUInt64() => TestConstructorWithSpan<UInt64>();
        [Fact]
        public void ConstructorWithSpanInt64() => TestConstructorWithSpan<Int64>();
        [Fact]
        public void ConstructorWithSpanSingle() => TestConstructorWithSpan<Single>();
        [Fact]
        public void ConstructorWithSpanDouble() => TestConstructorWithSpan<Double>();

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
        public void SpanBasedConstructorWithLessElements_Byte() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<Byte>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_SByte() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<SByte>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_UInt16() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<UInt16>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_Int16() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<Int16>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_UInt32() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<UInt32>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_Int32() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<Int32>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_UInt64() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<UInt64>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_Int64() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<Int64>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_Single() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<Single>());
        [Fact]
        public void SpanBasedConstructorWithLessElements_Double() => Assert.Throws<IndexOutOfRangeException>(() => TestSpanBasedConstructorWithLessElements<Double>());

        private void TestSpanBasedConstructorWithLessElements<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>(Vector<T>.Count - 1).ToArray();
            var vector = new Vector<T>(new Span<T>(values));
        }

        #endregion Tests for Span based constructor

        #region Tests for Array based constructor

        [Fact]
        public void ArrayBasedConstructor_Byte() => TestArrayBasedConstructor<Byte>();
        [Fact]
        public void ArrayBasedConstructor_SByte() => TestArrayBasedConstructor<SByte>();
        [Fact]
        public void ArrayBasedConstructor_UInt16() => TestArrayBasedConstructor<UInt16>();
        [Fact]
        public void ArrayBasedConstructor_Int16() => TestArrayBasedConstructor<Int16>();
        [Fact]
        public void ArrayBasedConstructor_UInt32() => TestArrayBasedConstructor<UInt32>();
        [Fact]
        public void ArrayBasedConstructor_Int32() => TestArrayBasedConstructor<Int32>();
        [Fact]
        public void ArrayBasedConstructor_UInt64() => TestArrayBasedConstructor<UInt64>();
        [Fact]
        public void ArrayBasedConstructor_Int64() => TestArrayBasedConstructor<Int64>();
        [Fact]
        public void ArrayBasedConstructor_Single() => TestArrayBasedConstructor<Single>();
        [Fact]
        public void ArrayBasedConstructor_Double() => TestArrayBasedConstructor<Double>();

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
        public void ArrayIndexBasedConstructor_Byte() => TestArrayIndexBasedConstructor<Byte>();
        [Fact]
        public void ArrayIndexBasedConstructor_SByte() => TestArrayIndexBasedConstructor<SByte>();
        [Fact]
        public void ArrayIndexBasedConstructor_UInt16() => TestArrayIndexBasedConstructor<UInt16>();
        [Fact]
        public void ArrayIndexBasedConstructor_Int16() => TestArrayIndexBasedConstructor<Int16>();
        [Fact]
        public void ArrayIndexBasedConstructor_UInt32() => TestArrayIndexBasedConstructor<UInt32>();
        [Fact]
        public void ArrayIndexBasedConstructor_Int32() => TestArrayIndexBasedConstructor<Int32>();
        [Fact]
        public void ArrayIndexBasedConstructor_UInt64() => TestArrayIndexBasedConstructor<UInt64>();
        [Fact]
        public void ArrayIndexBasedConstructor_Int64() => TestArrayIndexBasedConstructor<Int64>();
        [Fact]
        public void ArrayIndexBasedConstructor_Single() => TestArrayIndexBasedConstructor<Single>();
        [Fact]
        public void ArrayIndexBasedConstructor_Double() => TestArrayIndexBasedConstructor<Double>();

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
        public void ArrayBasedConstructorWithLessElements_Byte() => TestArrayBasedConstructorWithLessElements<Byte>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_SByte() => TestArrayBasedConstructorWithLessElements<SByte>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_UInt16() => TestArrayBasedConstructorWithLessElements<UInt16>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_Int16() => TestArrayBasedConstructorWithLessElements<Int16>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_UInt32() => TestArrayBasedConstructorWithLessElements<UInt32>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_Int32() => TestArrayBasedConstructorWithLessElements<Int32>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_UInt64() => TestArrayBasedConstructorWithLessElements<UInt64>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_Int64() => TestArrayBasedConstructorWithLessElements<Int64>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_Single() => TestArrayBasedConstructorWithLessElements<Single>();
        [Fact]
        public void ArrayBasedConstructorWithLessElements_Double() => TestArrayBasedConstructorWithLessElements<Double>();

        private void TestArrayBasedConstructorWithLessElements<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>(Vector<T>.Count - 1).ToArray();
            Assert.Throws<IndexOutOfRangeException>(() => new Vector<T>(values));
        }

        [Fact]
        public void ArrayIndexBasedConstructorLessElements_Byte() => TestArrayIndexBasedConstructorLessElements<Byte>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_SByte() => TestArrayIndexBasedConstructorLessElements<SByte>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_UInt16() => TestArrayIndexBasedConstructorLessElements<UInt16>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_Int16() => TestArrayIndexBasedConstructorLessElements<Int16>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_UInt32() => TestArrayIndexBasedConstructorLessElements<UInt32>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_Int32() => TestArrayIndexBasedConstructorLessElements<Int32>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_UInt64() => TestArrayIndexBasedConstructorLessElements<UInt64>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_Int64() => TestArrayIndexBasedConstructorLessElements<Int64>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_Single() => TestArrayIndexBasedConstructorLessElements<Single>();
        [Fact]
        public void ArrayIndexBasedConstructorLessElements_Double() => TestArrayIndexBasedConstructorLessElements<Double>();

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
            Assert.Throws<NotSupportedException>(() => new Vector<T>(new Span<T>(new T[4])));
        }

        #endregion Tests for constructors using unsupported types

        #endregion Constructor Tests
    }
}
