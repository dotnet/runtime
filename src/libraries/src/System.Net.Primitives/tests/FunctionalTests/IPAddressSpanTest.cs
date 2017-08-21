// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Sockets;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class IPAddressSpanTest
    {
        public static readonly object[][] IpAddresses =
        {
            new object[] { new byte[] { 0x8f, 0x18, 0x14, 0x24 } },
            new object[] { new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 } },
        };

        [Theory]
        [MemberData(nameof(IpAddresses))]
        public static void TryWriteBytes_RightSizeBuffer_Success(byte[] address)
        {
            var result = new byte[address.Length];
            IPAddress ip = new IPAddress(address);

            Assert.True(ip.TryWriteBytes(new Span<byte>(result), out int bytesWritten));
            Assert.Equal(address, result);
            Assert.Equal(address.Length, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(IpAddresses))]
        public static void TryWriteBytes_LargerBuffer_Success(byte[] address)
        {
            var result = new byte[address.Length + 1];
            IPAddress ip = new IPAddress(address);

            Assert.True(ip.TryWriteBytes(new Span<byte>(result), out int bytesWritten));
            Assert.Equal<byte>(address, result.AsSpan().Slice(0, bytesWritten).ToArray());
            Assert.Equal(address.Length, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(IpAddresses))]
        public static void TryWriteBytes_TooSmallBuffer_Failure(byte[] address)
        {
            int bufferSize = address.Length - 1;
            var result = new byte[bufferSize];
            IPAddress ip = new IPAddress(address);

            Assert.False(ip.TryWriteBytes(new Span<byte>(result), out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal<byte>(new byte[bufferSize], result);
        }
    }
}
