// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Net.Security.Tests
{
    public class SslApplicationProtocolTests
    {
        [Fact]
        public void Constants_Values_AreCorrect()
        {
            Assert.Equal(new SslApplicationProtocol("h3"u8.ToArray()), SslApplicationProtocol.Http3);
            Assert.Equal(new SslApplicationProtocol("h2"u8.ToArray()), SslApplicationProtocol.Http2);
            Assert.Equal(new SslApplicationProtocol("http/1.1"u8.ToArray()), SslApplicationProtocol.Http11);
        }

        [Fact]
        public void Constructor_Overloads_Succeeds()
        {
            const string hello = "hello";
            byte[] expected = Encoding.UTF8.GetBytes(hello);
            SslApplicationProtocol byteProtocol = new SslApplicationProtocol(expected);
            SslApplicationProtocol stringProtocol = new SslApplicationProtocol(hello);
            Assert.Equal(byteProtocol, stringProtocol);

            SslApplicationProtocol defaultProtocol = default;
            Assert.True(defaultProtocol.Protocol.IsEmpty);

            AssertExtensions.Throws<ArgumentNullException>("protocol", () => { new SslApplicationProtocol((byte[])null); });
            AssertExtensions.Throws<ArgumentNullException>("protocol", () => { new SslApplicationProtocol((string)null); });
            AssertExtensions.Throws<ArgumentException>("protocol", () => { new SslApplicationProtocol(new byte[] { }); });
            AssertExtensions.Throws<ArgumentException>("protocol", () => { new SslApplicationProtocol(string.Empty); });
            AssertExtensions.Throws<ArgumentException>("protocol", () => { new SslApplicationProtocol(Encoding.UTF8.GetBytes(new string('a', 256))); });
            AssertExtensions.Throws<ArgumentException>("protocol", () => { new SslApplicationProtocol(new string('a', 256)); });
            Assert.Throws<EncoderFallbackException>(() => { new SslApplicationProtocol("\uDC00"); });
        }

        [Fact]
        public void Constructor_ByteArray_Copies()
        {
            byte[] expected = "hello"u8.ToArray();
            SslApplicationProtocol byteProtocol = new SslApplicationProtocol(expected);

            ArraySegment<byte> arraySegment;
            Assert.True(MemoryMarshal.TryGetArray(byteProtocol.Protocol, out arraySegment));
            Assert.Equal(expected, arraySegment.Array);
            Assert.NotSame(expected, arraySegment.Array);
        }

        [Theory]
        [MemberData(nameof(Protocol_Equality_TestData))]
        public void Equality_Tests_Succeeds(SslApplicationProtocol left, SslApplicationProtocol right)
        {
            Assert.Equal(left, right);
            Assert.True(left == right);
            Assert.False(left != right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(Protocol_InEquality_TestData))]
        public void InEquality_Tests_Succeeds(SslApplicationProtocol left, SslApplicationProtocol right)
        {
            Assert.NotEqual(left, right);
            Assert.True(left != right);
            Assert.False(left == right);
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Fact]
        public void ToString_Rendering_Succeeds()
        {
            Assert.Equal("http/1.1", SslApplicationProtocol.Http11.ToString());
            Assert.Equal("h2", SslApplicationProtocol.Http2.ToString());
            Assert.Equal("h3", SslApplicationProtocol.Http3.ToString());
            Assert.Equal("hello", new SslApplicationProtocol("hello").ToString());
            Assert.Equal("0x0b 0xee", new SslApplicationProtocol(new byte[] { 0x0B, 0xEE }).ToString());
            Assert.Equal(string.Empty, default(SslApplicationProtocol).ToString());
        }

        public static IEnumerable<object[]> Protocol_Equality_TestData()
        {
            yield return new object[] { new SslApplicationProtocol("hello"), new SslApplicationProtocol("hello") };
            yield return new object[] { new SslApplicationProtocol(new byte[] { 0x42 }), new SslApplicationProtocol(new byte[] { 0x42 }) };
            yield return new object[] { null, null };
            yield return new object[] { default, default };
            yield return new object[] { null, default };
            yield return new object[] { default, null };
        }

        public static IEnumerable<object[]> Protocol_InEquality_TestData()
        {
            yield return new object[] { new SslApplicationProtocol("hello"), new SslApplicationProtocol("world") };
            yield return new object[] { new SslApplicationProtocol(new byte[] { 0x42 }), new SslApplicationProtocol(new byte[] { 0x52, 0x62 }) };
            yield return new object[] { null, new SslApplicationProtocol(new byte[] { 0x42 }) };
            yield return new object[] { new SslApplicationProtocol(new byte[] { 0x42 }), null };
        }
    }
}
