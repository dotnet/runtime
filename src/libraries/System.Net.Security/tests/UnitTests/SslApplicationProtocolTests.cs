// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Assert.Equal(new SslApplicationProtocol(new byte[] { 0x68, 0x32 }), SslApplicationProtocol.Http2);
            Assert.Equal(new SslApplicationProtocol(new byte[] { 0x68, 0x74, 0x74, 0x70, 0x2f, 0x31, 0x2e, 0x31 }), SslApplicationProtocol.Http11);
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

            Assert.Throws<ArgumentNullException>(() => { new SslApplicationProtocol((byte[])null); });
            Assert.Throws<ArgumentNullException>(() => { new SslApplicationProtocol((string)null); });
            Assert.Throws<ArgumentException>(() => { new SslApplicationProtocol(new byte[] { }); });
            Assert.Throws<ArgumentException>(() => { new SslApplicationProtocol(string.Empty); });
            Assert.Throws<ArgumentException>(() => { new SslApplicationProtocol(Encoding.UTF8.GetBytes(new string('a', 256))); });
            Assert.Throws<ArgumentException>(() => { new SslApplicationProtocol(new string('a', 256)); });
            Assert.Throws<EncoderFallbackException>(() => { new SslApplicationProtocol("\uDC00"); });
        }

        [Fact]
        public void Constructor_ByteArray_Copies()
        {
            byte[] expected = Encoding.UTF8.GetBytes("hello");
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
            const string expected = "hello";
            SslApplicationProtocol protocol = new SslApplicationProtocol(expected);
            Assert.Equal(expected, protocol.ToString());

            byte[] bytes = new byte[] { 0x0B, 0xEE };
            protocol = new SslApplicationProtocol(bytes);
            Assert.Equal("0x0b 0xee", protocol.ToString());

            protocol = default;
            Assert.Null(protocol.ToString());
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
