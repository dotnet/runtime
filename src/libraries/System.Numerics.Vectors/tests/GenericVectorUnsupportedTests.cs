// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Numerics.Tests
{
    public class GenericVectorUnsupportedTests
    {
        [Fact]
        public void CountTest()
        {
            Assert.Throws<NotSupportedException>(() => Vector<bool>.Count);
        }

        [Fact]
        public void ZeroTest()
        {
            Assert.Throws<NotSupportedException>(() => Vector<bool>.Zero);
        }

        [Fact]
        public void OneTest()
        {
            Assert.Throws<NotSupportedException>(() => Vector<bool>.One);
        }

        [Fact]
        public void ConstructorValueTest()
        {
            Assert.Throws<NotSupportedException>(() => new Vector<bool>(false));
        }

        [Fact]
        public void ConstructorArrayTest()
        {
            bool[] values = new bool[32];
            Assert.Throws<NotSupportedException>(() => new Vector<bool>(values));
        }

        [Fact]
        public void ConstructorArrayIndexTest()
        {
            bool[] values = new bool[32];
            Assert.Throws<NotSupportedException>(() => new Vector<bool>(values, 1));
        }

        [Fact]
        public void ConstructorReadOnlySpanByteTest()
        {
            Assert.Throws<NotSupportedException>(() => {
                ReadOnlySpan<byte> values = stackalloc byte[32];
                _ = new Vector<bool>(values);
            });
        }

        [Fact]
        public void ConstructorReadOnlySpanTTest()
        {
            Assert.Throws<NotSupportedException>(() => {
                ReadOnlySpan<bool> values = stackalloc bool[32];
                _ = new Vector<bool>(values);
            });
        }

        [Fact]
        public void ConstructorSpanTTest()
        {
            Assert.Throws<NotSupportedException>(() => {
                Span<bool> values = stackalloc bool[32];
                _ = new Vector<bool>(values);
            });
        }

        [Fact]
        public void CopyToSpanByteTest()
        {
            Assert.Throws<NotSupportedException>(() => {
                Vector<bool> vector = default;
                Span<byte> destination = stackalloc byte[32];
                vector.CopyTo(destination);
            });
        }

        [Fact]
        public void CopyToSpanTTest()
        {
            Assert.Throws<NotSupportedException>(() => {
                Vector<bool> vector = default;
                Span<bool> destination = stackalloc bool[32];
                vector.CopyTo(destination);
            });
        }

        [Fact]
        public void CopyToArrayTest()
        {
            Vector<bool> vector = default;
            bool[] destination = new bool[32];
            Assert.Throws<NotSupportedException>(() => vector.CopyTo(destination));
        }

        [Fact]
        public void CopyToArrayIndexTest()
        {
            Vector<bool> vector = default;
            bool[] destination = new bool[32];
            Assert.Throws<NotSupportedException>(() => vector.CopyTo(destination, 1));
        }

        [Fact]
        public void IndexerTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => vector[0]);
        }

        [Fact]
        public void EqualsObjectTest()
        {
            Vector<bool> vector1 = default;
            Vector<bool> vector2 = default;
            Assert.Throws<NotSupportedException>(() => vector1.Equals((object)vector2));
        }

        [Fact]
        public void EqualsVectorTest()
        {
            Vector<bool> vector1 = default;
            Vector<bool> vector2 = default;
            Assert.Throws<NotSupportedException>(() => vector1.Equals(vector2));
        }

        [Fact]
        public void GetHashCodeTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => vector.GetHashCode());
        }

        [Fact]
        public void ToStringTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => vector.ToString());
        }

        [Fact]
        public void ToStringFormatTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => vector.ToString("G"));
        }

        [Fact]
        public void ToStringFormatFormatProviderTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => vector.ToString("G", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void TryCopyToSpanByteTest()
        {
            Assert.Throws<NotSupportedException>(() => {
                Vector<bool> vector = default;
                Span<byte> destination = stackalloc byte[32];
                vector.TryCopyTo(destination);
            });
        }

        [Fact]
        public void TryCopyToSpanTTest()
        {
            Assert.Throws<NotSupportedException>(() => {
                Vector<bool> vector = default;
                Span<bool> destination = stackalloc bool[32];
                vector.TryCopyTo(destination);
            });
        }

        [Fact]
        public void OpAdditionTest()
        {
            Vector<bool> vector1 = default;
            Vector<bool> vector2 = default;
            Assert.Throws<NotSupportedException>(() => vector1 + vector2);
        }

        [Fact]
        public void OpSubtractionTest()
        {
            Vector<bool> vector1 = default;
            Vector<bool> vector2 = default;
            Assert.Throws<NotSupportedException>(() => vector1 - vector2);
        }

        [Fact]
        public void OpMultiplicationTest()
        {
            Vector<bool> vector1 = default;
            Vector<bool> vector2 = default;
            Assert.Throws<NotSupportedException>(() => vector1 * vector2);
        }

        [Fact]
        public void OpMultiplicationByScalarTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => vector * false);
            Assert.Throws<NotSupportedException>(() => false * vector);
        }

        [Fact]
        public void OpDivideTest()
        {
            Vector<bool> vector1 = default;
            Vector<bool> vector2 = default;
            Assert.Throws<NotSupportedException>(() => vector1 / vector2);
        }

        [Fact]
        public void OpNegateTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => -vector);
        }

        [Fact]
        public void OpBitwiseAndTest()
        {
            Vector<bool> vector1 = default;
            Vector<bool> vector2 = default;
            Assert.Throws<NotSupportedException>(() => vector1 & vector2);
        }

        [Fact]
        public void OpBitwiseOrTest()
        {
            Vector<bool> vector1 = default;
            Vector<bool> vector2 = default;
            Assert.Throws<NotSupportedException>(() => vector1 | vector2);
        }

        [Fact]
        public void OpBitwiseXorTest()
        {
            Vector<bool> vector1 = default;
            Vector<bool> vector2 = default;
            Assert.Throws<NotSupportedException>(() => vector1 ^ vector2);
        }

        [Fact]
        public void OpOnesComplementTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => ~vector);
        }

        [Fact]
        public void OpEqualsTest()
        {
            Vector<bool> vector1 = default;
            Vector<bool> vector2 = default;
            Assert.Throws<NotSupportedException>(() => vector1 == vector2);
        }

        [Fact]
        public void OpNotEqualsTest()
        {
            Vector<bool> vector1 = default;
            Vector<bool> vector2 = default;
            Assert.Throws<NotSupportedException>(() => vector1 != vector2);
        }

        [Fact]
        public void ToVectorByteTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => (Vector<byte>)vector);
        }

        [Fact]
        public void ToVectorSByteTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => (Vector<sbyte>)vector);
        }

        [Fact]
        public void ToVectorInt16Test()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => (Vector<short>)vector);
        }

        [Fact]
        public void ToVectorUInt16Test()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => (Vector<ushort>)vector);
        }

        [Fact]
        public void ToVectorInt32Test()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => (Vector<int>)vector);
        }

        [Fact]
        public void ToVectorUInt32Test()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => (Vector<uint>)vector);
        }

        [Fact]
        public void ToVectorInt64Test()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => (Vector<long>)vector);
        }

        [Fact]
        public void ToVectorUInt64Test()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => (Vector<ulong>)vector);
        }

        [Fact]
        public void ToVectorSingleTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => (Vector<float>)vector);
        }

        [Fact]
        public void ToVectorDoubleTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => (Vector<double>)vector);
        }

        [Fact]
        public void AsFromTest()
        {
            Vector<bool> vector = default;
            Assert.Throws<NotSupportedException>(() => vector.As<bool, int>());
        }

        [Fact]
        public void AsToTest()
        {
            Vector<int> vector = default;
            Assert.Throws<NotSupportedException>(() => vector.As<int, bool>());
        }

        [Fact]
        public void IsNotSupportedBoolean() => TestIsNotSupported<bool>();

        [Fact]
        public void IsNotSupportedChar() => TestIsNotSupported<char>();

        [Fact]
        public void IsNotSupportedHalf() => TestIsNotSupported<Half>();

        [Fact]
        public void IsNotSupportedInt128() => TestIsNotSupported<Int128>();

        [Fact]
        public void IsNotSupportedUInt128() => TestIsNotSupported<UInt128>();

        private static void TestIsNotSupported<T>()
            where T : struct
        {
            Assert.False(Vector<T>.IsSupported);

            MethodInfo methodInfo = typeof(Vector<T>).GetProperty("IsSupported", BindingFlags.Public | BindingFlags.Static).GetMethod;
            Assert.False((bool)methodInfo.Invoke(null, null));
        }
    }
}
