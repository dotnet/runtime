// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborWriterTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A
        // Additional pairs generated using http://cbor.me/

        [Theory]
        [InlineData(new object[] { }, "80")]
        [InlineData(new object[] { 42 }, "81182a")]
        [InlineData(new object[] { 1, 2, 3 }, "83010203")]
        [InlineData(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 }, "98190102030405060708090a0b0c0d0e0f101112131415161718181819")]
        [InlineData(new object[] { 1, -1, "", new byte[] { 7 } }, "840120604107")]
        [InlineData(new object[] { "lorem", "ipsum", "dolor" }, "83656c6f72656d65697073756d65646f6c6f72")]
        public static void WriteArray_SimpleValues_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter();
            ArrayWriterHelper.WriteArray(writer, values);
            byte[] actualEncoding = writer.ToArray();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(new object[] { new object[] { } }, "8180")]
        [InlineData(new object[] { 1, new object[] { 2, 3 }, new object[] { 4, 5 } }, "8301820203820405")]
        [InlineData(new object[] { "", new object[] { new object[] { }, new object[] { 1, new byte[] { 10 } } } }, "826082808201410a")]
        public static void WriteArray_NestedValues_HappyPath(object[] values, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter();
            ArrayWriterHelper.WriteArray(writer, values);
            byte[] actualEncoding = writer.ToArray();
            AssertHelper.HexEqual(expectedEncoding, actualEncoding);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public static void WriteArray_DefiniteLengthExceeded_ShouldThrowInvalidOperationException(int definiteLength)
        {
            using var writer = new CborWriter();
            writer.WriteStartArray(definiteLength);
            for (int i = 0; i < definiteLength; i++)
            {
                writer.WriteInt64(i);
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteInt64(0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public static void WriteArray_DefiniteLengthExceeded_WithNestedData_ShouldThrowInvalidOperationException(int definiteLength)
        {
            using var writer = new CborWriter();
            writer.WriteStartArray(definiteLength);
            for (int i = 0; i < definiteLength; i++)
            {
                writer.WriteStartArray(definiteLength: 1);
                writer.WriteInt64(i);
                writer.WriteEndArray();
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteInt64(0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public static void EndWriteArray_DefiniteLengthNotMet_ShouldThrowInvalidOperationException(int definiteLength)
        {
            using var writer = new CborWriter();
            writer.WriteStartArray(definiteLength);
            for (int i = 1; i < definiteLength; i++)
            {
                writer.WriteInt64(i);
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteEndArray());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public static void EndWriteArray_DefiniteLengthNotMet_WithNestedData_ShouldThrowInvalidOperationException(int definiteLength)
        {
            using var writer = new CborWriter();
            writer.WriteStartArray(definiteLength);
            for (int i = 1; i < definiteLength; i++)
            {
                writer.WriteStartArray(definiteLength: 1);
                writer.WriteInt64(i);
                writer.WriteEndArray();
            }

            Assert.Throws<InvalidOperationException>(() => writer.WriteEndArray());
        }

        [Fact]
        public static void EndWriteArray_ImbalancedCall_ShouldThrowInvalidOperationException()
        {
            using var writer = new CborWriter();
            Assert.Throws<InvalidOperationException>(() => writer.WriteEndArray());
        }
    }

    static class ArrayWriterHelper
    {
        public static void WriteArray(CborWriter writer, params object[] values)
        {
            writer.WriteStartArray(values.Length);
            foreach (object value in values)
            {
                switch (value)
                {
                    case int i: writer.WriteInt64(i); break;
                    case string s: writer.WriteTextString(s); break;
                    case byte[] b: writer.WriteByteString(b); break;
                    case object[] nested: ArrayWriterHelper.WriteArray(writer, nested); break;
                    default: throw new ArgumentException($"Unrecognized argument type {value.GetType()}");
                };
            }
            writer.WriteEndArray();
        }
    }
}
