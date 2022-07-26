// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Cbor;
using System.Linq;
using Test.Cryptography;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public class CoseHeaderValueTests
    {
        [Theory]
        [InlineData(int.MaxValue + 1L)]
        [InlineData(int.MinValue - 1L)]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        public void GetValueAsInt32Overflows(long value)
        {
            var writer = new CborWriter();
            writer.WriteInt64(value);

            CoseHeaderValue headerValue = CoseHeaderValue.FromEncodedValue(writer.Encode());
            Exception ex = Assert.Throws<InvalidOperationException>(() => headerValue.GetValueAsInt32());
            Assert.IsType<OverflowException>(ex.InnerException);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData((int)ECDsaAlgorithm.ES256)]
        public void GetValueAsInt32Succeeds(int value)
        {
            var writer = new CborWriter();
            writer.WriteInt32(value);

            CoseHeaderValue headerValue = CoseHeaderValue.FromEncodedValue(writer.Encode());
            Assert.Equal(value, headerValue.GetValueAsInt32());
        }

        [Theory]
        [InlineData("")]
        [InlineData("foo")]
        [InlineData(ContentTypeDummyValue)]
        public void GetValueAsStringSucceeds(string value)
        {
            var writer = new CborWriter();
            writer.WriteTextString(value);

            CoseHeaderValue headerValue = CoseHeaderValue.FromEncodedValue(writer.Encode());
            Assert.Equal(value, headerValue.GetValueAsString());
        }

        [Theory]
        [InlineData("")]
        [InlineData("foo")]
        [InlineData(ContentTypeDummyValue)]
        public void GetValueAsStringBeingIndefiniteLengthSucceeds(string value)
        {
            Verify(0);
            Verify(1);
            Verify(10);

            void Verify(int repetitions)
            {
                var writer = new CborWriter();
                writer.WriteStartIndefiniteLengthTextString();
                for (int i = 0; i < repetitions; i++)
                {
                    writer.WriteTextString(value);
                }
                writer.WriteEndIndefiniteLengthTextString();

                CoseHeaderValue headerValue = CoseHeaderValue.FromEncodedValue(writer.Encode());

                string expectedValue = string.Join("", Enumerable.Repeat(value, repetitions));
                Assert.Equal(expectedValue, headerValue.GetValueAsString());
            }
        }

        [Theory]
        [InlineData(ContentTestCase.Empty)]
        [InlineData(ContentTestCase.Small)]
        [InlineData(ContentTestCase.Large)]
        public void GetValueAsBytesSucceeds(ContentTestCase @case)
        {
            byte[] content = GetDummyContent(@case);
            var writer = new CborWriter();
            writer.WriteByteString(content);

            CoseHeaderValue headerValue = CoseHeaderValue.FromEncodedValue(writer.Encode());
            AssertExtensions.SequenceEqual(content, headerValue.GetValueAsBytes());

            Span<byte> buffer = new byte[content.Length];
            int length = headerValue.GetValueAsBytes(buffer);
            Assert.Equal(content.Length, length);
            AssertExtensions.SequenceEqual(content, buffer);
        }

        [Theory]
        [InlineData(ContentTestCase.Empty)]
        [InlineData(ContentTestCase.Small)]
        [InlineData(ContentTestCase.Large)]
        public void GetValueAsBytesBeingIndefiniteLengthSucceeds(ContentTestCase @case)
        {
            Verify(0);
            Verify(1);
            Verify(10);

            void Verify(int repetitions)
            {
                byte[] content = GetDummyContent(@case);
                var writer = new CborWriter();
                writer.WriteStartIndefiniteLengthByteString();
                for (int i = 0; i < repetitions; i++)
                {
                    writer.WriteByteString(content);
                }
                writer.WriteEndIndefiniteLengthByteString();

                int expectedLength = content.Length * repetitions;

                CoseHeaderValue headerValue = CoseHeaderValue.FromEncodedValue(writer.Encode());
                ReadOnlySpan<byte> result = headerValue.GetValueAsBytes();
                Assert.Equal(expectedLength, result.Length);

                for (int i = 0; i < expectedLength; i += content.Length)
                {
                    AssertExtensions.SequenceEqual(content, result.Slice(i, content.Length));
                }

                Span<byte> buffer = new byte[expectedLength];
                int length = headerValue.GetValueAsBytes(buffer);
                Assert.Equal(expectedLength, length);

                for (int i = 0; i < expectedLength; i+= content.Length)
                {
                    AssertExtensions.SequenceEqual(content, buffer.Slice(i, content.Length));
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetValueAsThrowsTestData))]
        public void GetValueAsThrows(byte[] encodedValue, GetValueAs method)
        {
            CoseHeaderValue headerValue = CoseHeaderValue.FromEncodedValue(encodedValue);

            if (method == GetValueAs.Int32)
            {
                Assert.Throws<InvalidOperationException>(() => headerValue.GetValueAsInt32());
            }
            else if (method == GetValueAs.String)
            {
                Assert.Throws<InvalidOperationException>(() => headerValue.GetValueAsString());
            }
            else if (method == GetValueAs.Bytes)
            {
                Assert.Throws<InvalidOperationException>(() => headerValue.GetValueAsBytes());
            }
            else
            {
                Assert.Equal(GetValueAs.BytesSpan, method);
                Memory<byte> buffer = new byte[1024]; // big enough to not throw ArgumentException.
                Assert.Throws<InvalidOperationException>(() => headerValue.GetValueAsBytes(buffer.Span));
            }
        }

        public static IEnumerable<object[]> GetValueAsThrowsTestData()
        {
            // null
            var writer = new CborWriter();
            writer.WriteNull();
            byte[] encodedNull = writer.Encode();
            yield return new object[] { encodedNull, GetValueAs.Int32 };
            yield return new object[] { encodedNull, GetValueAs.String };
            yield return new object[] { encodedNull, GetValueAs.Bytes };
            yield return new object[] { encodedNull, GetValueAs.BytesSpan };
        }

        public enum GetValueAs
        {
            Int32,
            String,
            Bytes,
            BytesSpan
        }

        [Theory]
        [InlineData(0)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData((int)ECDsaAlgorithm.ES256)]
        public void FromInt32Succeeds(int value)
        {
            var writer = new CborWriter();
            writer.WriteInt32(value);

            CoseHeaderValue headerValue = CoseHeaderValue.FromInt32(value);
            AssertExtensions.SequenceEqual(writer.Encode(), headerValue.EncodedValue.Span);
            Assert.Equal(value, headerValue.GetValueAsInt32());
        }

        [Theory]
        [InlineData("")]
        [InlineData("foo")]
        [InlineData(ContentTypeDummyValue)]
        public void FromStringSucceeds(string value)
        {
            var writer = new CborWriter();
            writer.WriteTextString(value);

            CoseHeaderValue headerValue = CoseHeaderValue.FromString(value);
            AssertExtensions.SequenceEqual(writer.Encode(), headerValue.EncodedValue.Span);
            Assert.Equal(value, headerValue.GetValueAsString());
        }

        [Fact]
        public void FromStringThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("value", () => CoseHeaderValue.FromString(null!));
        }

        [Theory]
        [InlineData(ContentTestCase.Empty)]
        [InlineData(ContentTestCase.Small)]
        [InlineData(ContentTestCase.Large)]
        public void FromBytesSucceeds(ContentTestCase @case)
        {
            byte[] content = GetDummyContent(@case);
            var writer = new CborWriter();
            writer.WriteByteString(content);
            byte[] encodedBytes = writer.Encode();

            CoseHeaderValue headerValue = CoseHeaderValue.FromBytes(content);
            AssertExtensions.SequenceEqual(encodedBytes, headerValue.EncodedValue.Span);
            AssertExtensions.SequenceEqual(content, headerValue.GetValueAsBytes());

            headerValue = CoseHeaderValue.FromBytes(content.AsSpan());
            AssertExtensions.SequenceEqual(encodedBytes, headerValue.EncodedValue.Span);

            Span<byte> buffer = new byte[content.Length];
            int length = headerValue.GetValueAsBytes(buffer);
            AssertExtensions.SequenceEqual(content, buffer);
            Assert.Equal(content.Length, length);
        }

        [Theory]
        [InlineData(ContentTestCase.Small)]
        [InlineData(ContentTestCase.Large)]
        public void FromBytesThrowsBufferTooSmall(ContentTestCase @case)
        {
            byte[] content = GetDummyContent(@case);
            CoseHeaderValue headerValue = CoseHeaderValue.FromBytes(content.AsSpan());
            Memory<byte> buffer = new byte[content.Length - 1];
            Assert.Throws<ArgumentException>(() => headerValue.GetValueAsBytes(buffer.Span));
        }

        [Fact]
        public void FromBytesThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("value",() => CoseHeaderValue.FromBytes(null!));
        }

        [Theory]
        [MemberData(nameof(AllCborTypesTestData))]
        public void FromEncodedValue(byte[] encodedValue)
        {
            var headerValue = CoseHeaderValue.FromEncodedValue(encodedValue);
            AssertExtensions.SequenceEqual(encodedValue, headerValue.EncodedValue.Span);

            // make sure is readable.
            var reader = new CborReader(headerValue.EncodedValue);
            reader.SkipValue();

            headerValue = CoseHeaderValue.FromEncodedValue(encodedValue.AsSpan());
            AssertExtensions.SequenceEqual(encodedValue, headerValue.EncodedValue.Span);

            // make sure is readable.
            reader = new CborReader(headerValue.EncodedValue);
            reader.SkipValue();
        }

        public static IEnumerable<object[]> AllCborTypesTestData()
        {
            foreach (byte[] encodedValue in AllCborTypes())
            {
                yield return new object[] { encodedValue };
            }
        }

        [Fact]
        public void FromEncodedValueThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("encodedValue", () => CoseHeaderValue.FromEncodedValue(null!));
        }

        [Fact]
        public void CoseHeaderValue_GetHashCode()
        {
            CoseHeaderValue value1 = default;
            CoseHeaderValue value2 = new CoseHeaderValue();
            int value1HashCode = value1.GetHashCode();
            Assert.Equal(value1HashCode, value2.GetHashCode());

            value1 = CoseHeaderValue.FromInt32(0);
            value2 = CoseHeaderValue.FromInt32(0);
            Assert.Equal(value1.GetHashCode(), value2.GetHashCode());

            value1 = default;
            Assert.NotEqual(value1.GetHashCode(), value2.GetHashCode());

            value1 = new CoseHeaderValue();
            Assert.NotEqual(value1.GetHashCode(), value2.GetHashCode());
        }

        [Fact]
        public void CoseHeaderValue_Equals()
        {
            Assert.True(default(CoseHeaderValue).Equals(default), "default(CoseHeaderValue).Equals(default)");

            Assert.True(default(CoseHeaderValue).Equals(new CoseHeaderValue()), "default(CoseHeaderValue).Equals(new CoseHeaderValue()");

            CoseHeaderValue value1 = CoseHeaderValue.FromInt32(0);
            CoseHeaderValue value2 = CoseHeaderValue.FromInt32(0);
            Assert.True(value1.Equals(value2), "CoseHeaderValue.FromInt32(0) - value1.Equals(value2)");

            value1 = CoseHeaderValue.FromString("foo");
            value2 = CoseHeaderValue.FromString("foo");
            Assert.True(value1.Equals(value2), "CoseHeaderValue.FromString(\"foo\") - value1.Equals(value2)");

            byte[] bytes = "foo"u8.ToArray();
            value1 = CoseHeaderValue.FromBytes(bytes);
            value2 = CoseHeaderValue.FromBytes(bytes);
            Assert.True(value1.Equals(value2), "CoseHeaderValue.FromBytes(bytes) - value1.Equals(value2)");

            value1 = CoseHeaderValue.FromBytes(bytes.AsSpan());
            value2 = CoseHeaderValue.FromBytes(bytes.AsSpan());
            Assert.True(value1.Equals(value2), "CoseHeaderValue.FromBytes(bytes.AsSpan()) - value1.Equals(value2)");

            byte[] encodedValue = ByteUtils.HexToByteArray("80"); // empty array
            value1 = CoseHeaderValue.FromEncodedValue(encodedValue);
            value2 = CoseHeaderValue.FromEncodedValue(encodedValue);
            Assert.True(value1.Equals(value2), "CoseHeaderValue.FromEncodedValue(encodedValue) - value1.Equals(value2)");

            value1 = CoseHeaderValue.FromString("foo");
            value2 = CoseHeaderValue.FromString("bar");
            Assert.False(value1.Equals(value2), "CoseHeaderValue.FromString(\"foo\").Equals(CoseHeaderValue.FromString(\"bar\"))");

            value1 = CoseHeaderValue.FromInt32(0);
            value2 = default;
            Assert.False(value1.Equals(value2), "CoseHeaderValue.FromInt32(0).Equals(default)");
        }

        [Fact]
        public void CoseHeaderValue_op_Equality()
        {
            Assert.True(default(CoseHeaderValue) == default, "default(CoseHeaderValue) == default");

            Assert.True(default(CoseHeaderValue) == new CoseHeaderValue(), "default(CoseHeaderValue) == new CoseHeaderValue(");

            CoseHeaderValue value1 = CoseHeaderValue.FromInt32(0);
            CoseHeaderValue value2 = CoseHeaderValue.FromInt32(0);
            Assert.True(value1 == value2, "CoseHeaderValue.FromInt32(0) - value1 == value2");

            value1 = CoseHeaderValue.FromString("foo");
            value2 = CoseHeaderValue.FromString("foo");
            Assert.True(value1 == value2, "CoseHeaderValue.FromString(\"foo\") - value1 == value2");

            byte[] bytes = "foo"u8.ToArray();
            value1 = CoseHeaderValue.FromBytes(bytes);
            value2 = CoseHeaderValue.FromBytes(bytes);
            Assert.True(value1 == value2, "CoseHeaderValue.FromBytes(bytes) - value1 == value2");

            value1 = CoseHeaderValue.FromBytes(bytes.AsSpan());
            value2 = CoseHeaderValue.FromBytes(bytes.AsSpan());
            Assert.True(value1 == value2, "CoseHeaderValue.FromBytes(bytes.AsSpan()) - value1 == value2");

            byte[] encodedValue = ByteUtils.HexToByteArray("80"); // empty array
            value1 = CoseHeaderValue.FromEncodedValue(encodedValue);
            value2 = CoseHeaderValue.FromEncodedValue(encodedValue);
            Assert.True(value1 == value2, "CoseHeaderValue.FromEncodedValue(encodedValue) - value1 == value2");

            value1 = CoseHeaderValue.FromString("foo");
            value2 = CoseHeaderValue.FromString("bar");
            Assert.False(value1 == value2, "CoseHeaderValue.FromString(\"foo\") == CoseHeaderValue.FromString(\"bar\")");

            value1 = CoseHeaderValue.FromInt32(0);
            value2 = default;
            Assert.False(value1 == value2, "CoseHeaderValue.FromInt32(0) == default");
        }

        [Fact]
        public void CoseHeaderValue_op_Inequality()
        {
            Assert.False(default(CoseHeaderValue) != default, "default(CoseHeaderValue) != default");

            Assert.False(default(CoseHeaderValue) != new CoseHeaderValue(), "default(CoseHeaderValue) != new CoseHeaderValue(");

            CoseHeaderValue value1 = CoseHeaderValue.FromInt32(0);
            CoseHeaderValue value2 = CoseHeaderValue.FromInt32(0);
            Assert.False(value1 != value2, "CoseHeaderValue.FromInt32(0) - value1 != value2");

            value1 = CoseHeaderValue.FromString("foo");
            value2 = CoseHeaderValue.FromString("foo");
            Assert.False(value1 != value2, "CoseHeaderValue.FromString(\"foo\") - value1 != value2");

            byte[] bytes = "foo"u8.ToArray();
            value1 = CoseHeaderValue.FromBytes(bytes);
            value2 = CoseHeaderValue.FromBytes(bytes);
            Assert.False(value1 != value2, "CoseHeaderValue.FromBytes(bytes) - value1 != value2");

            value1 = CoseHeaderValue.FromBytes(bytes.AsSpan());
            value2 = CoseHeaderValue.FromBytes(bytes.AsSpan());
            Assert.False(value1 != value2, "CoseHeaderValue.FromBytes(bytes.AsSpan()) - value1 != value2");

            byte[] encodedValue = ByteUtils.HexToByteArray("80"); // empty array
            value1 = CoseHeaderValue.FromEncodedValue(encodedValue);
            value2 = CoseHeaderValue.FromEncodedValue(encodedValue);
            Assert.False(value1 != value2, "CoseHeaderValue.FromEncodedValue(encodedValue) - value1 != value2");

            value1 = CoseHeaderValue.FromString("foo");
            value2 = CoseHeaderValue.FromString("bar");
            Assert.True(value1 != value2, "CoseHeaderValue.FromString(\"foo\") != (CoseHeaderValue.FromString(\"bar\")");

            value1 = CoseHeaderValue.FromInt32(0);
            value2 = default;
            Assert.True(value1 != value2, "CoseHeaderValue.FromInt32(0) != default");
        }
    }
}
