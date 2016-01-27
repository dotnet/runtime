// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    public class SslStreamAllowedProtocolsTest
    {
        [Theory]
        [ClassData(typeof(SslProtocolSupport.UnsupportedSslProtocolsTestData))]
        public async Task SslStream_AuthenticateAsClient_NotSupported_Fails(SslProtocols protocol)
        {
            SslStream stream = new SslStream(new FakeStream());
            await Assert.ThrowsAsync<NotSupportedException>(
                () => stream.AuthenticateAsClientAsync("host", null, protocol, false));
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public async Task SslStream_AuthenticateAsClient_Supported_Success(SslProtocols protocol)
        {
            SslStream stream = new SslStream(new FakeStream());
            await stream.AuthenticateAsClientAsync("host", null, protocol, false);
        }

        [Fact]
        public async Task SslStream_AuthenticateAsClient_Invalid_Fails()
        {
            SslStream stream = new SslStream(new FakeStream());
            await Assert.ThrowsAsync<NotSupportedException>(
                () => stream.AuthenticateAsClientAsync("host", null, (SslProtocols)4096, false));
        }

        [Fact]
        public async Task SslStream_AuthenticateAsClient_AllUnsuported_Fails()
        {
            SslStream stream = new SslStream(new FakeStream());
            await Assert.ThrowsAsync<NotSupportedException>(
                () => stream.AuthenticateAsClientAsync(
                    "host",
                    null,
                    SslProtocolSupport.UnsupportedSslProtocols,
                    false));
        }

        [Fact]
        public async Task SslStream_AuthenticateAsClient_AllSupported_Success()
        {
            SslStream stream = new SslStream(new FakeStream());
            await stream.AuthenticateAsClientAsync(
                "host",
                null,
                SslProtocolSupport.SupportedSslProtocols,
                false);
        }

        [Fact]
        public async Task SslStream_AuthenticateAsClient_None_Success()
        {
            SslStream stream = new SslStream(new FakeStream());
            await stream.AuthenticateAsClientAsync("host", null, SslProtocols.None, false);
        }

        [Fact]
        public async Task SslStream_AuthenticateAsClient_Default_Success()
        {
            SslStream stream = new SslStream(new FakeStream());
            await stream.AuthenticateAsClientAsync("host", null, SslProtocolSupport.DefaultSslProtocols, false);
        }

        private class FakeStream : Stream
        {
            public override bool CanRead
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool CanSeek
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool CanWrite
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override long Length
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }
    }
}

