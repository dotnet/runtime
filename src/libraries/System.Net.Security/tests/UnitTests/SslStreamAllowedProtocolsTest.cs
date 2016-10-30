// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    public abstract class SslStreamAllowedProtocolsTest
    {
        protected abstract void AuthenticateAsClient(
            SslStream stream, bool waitForCompletion,
            string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation);

        [Theory]
        [ClassData(typeof(SslProtocolSupport.UnsupportedSslProtocolsTestData))]
        public void SslStream_AuthenticateAsClientAsync_NotSupported_Fails(SslProtocols protocol)
        {
            SslStream stream = new SslStream(new NotImplementedStream());
            Assert.Throws<NotSupportedException>(() => AuthenticateAsClient(stream, false, "host", null, protocol, false));
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public void SslStream_AuthenticateAsClientAsync_Supported_Success(SslProtocols protocol)
        {
            SslStream stream = new SslStream(new NotImplementedStream());
            AuthenticateAsClient(stream, true, "host", null, protocol, false);
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public void SslStream_AuthenticateAsClient_Supported_Success(SslProtocols protocol)
        {
            SslStream stream = new SslStream(new NotImplementedStream());
            AuthenticateAsClient(stream, true, "host", null, protocol, false);
        }

        [Fact]
        public void SslStream_AuthenticateAsClientAsync_Invalid_Fails()
        {
            SslStream stream = new SslStream(new NotImplementedStream());
            Assert.Throws<NotSupportedException>(() => AuthenticateAsClient(stream, false, "host", null, (SslProtocols)4096, false));
        }

        [Fact]
        public void SslStream_AuthenticateAsClient_Invalid_Fails()
        {
            SslStream stream = new SslStream(new NotImplementedStream());
            Assert.Throws<NotSupportedException>(() => AuthenticateAsClient(stream, false, "host", null, (SslProtocols)4096, false));
        }

        [Fact]
        public void SslStream_AuthenticateAsClientAsync_AllUnsuported_Fails()
        {
            SslStream stream = new SslStream(new NotImplementedStream());
            Assert.Throws<NotSupportedException>(() => AuthenticateAsClient(stream, false, "host", null, SslProtocolSupport.UnsupportedSslProtocols, false));
        }

        [Fact]
        public void SslStream_AuthenticateAsClientAsync_AllSupported_Success()
        {
            SslStream stream = new SslStream(new NotImplementedStream());
            AuthenticateAsClient(stream, true, "host", null, SslProtocolSupport.SupportedSslProtocols, false);
        }

        [Fact]
        public void SslStream_AuthenticateAsClientAsync_None_Success()
        {
            SslStream stream = new SslStream(new NotImplementedStream());
            AuthenticateAsClient(stream, true, "host", null, SslProtocols.None, false);
        }

        [Fact]
        public void SslStream_AuthenticateAsClientAsync_Default_Success()
        {
            SslStream stream = new SslStream(new NotImplementedStream());
            AuthenticateAsClient(stream, true, "host", null, SslProtocolSupport.DefaultSslProtocols, false);
        }


        public sealed class SslStreamAllowedProtocolsTest_Async : SslStreamAllowedProtocolsTest
        {
            protected override void AuthenticateAsClient(
                SslStream stream, bool waitForCompletion,
                string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
            {
                Task t = stream.AuthenticateAsClientAsync(targetHost, clientCertificates, enabledSslProtocols, checkCertificateRevocation);
                if (waitForCompletion)
                {
                    t.GetAwaiter().GetResult();
                }
            }
        }

        public sealed class SslStreamAllowedProtocolsTest_BeginEnd : SslStreamAllowedProtocolsTest
        {
            protected override void AuthenticateAsClient(
                SslStream stream, bool waitForCompletion,
                string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
            {
                IAsyncResult iar = stream.BeginAuthenticateAsClient(targetHost, clientCertificates, enabledSslProtocols, checkCertificateRevocation, null, null);
                if (waitForCompletion)
                {
                    stream.EndAuthenticateAsClient(iar);
                }
            }
        }

        public sealed class SslStreamAllowedProtocolsTest_Sync : SslStreamAllowedProtocolsTest
        {
            protected override void AuthenticateAsClient(
                SslStream stream, bool waitForCompletion,
                string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
            {
                stream.AuthenticateAsClient(targetHost, clientCertificates, enabledSslProtocols, checkCertificateRevocation);
            }
        }

        private sealed class NotImplementedStream : Stream
        {
            public override bool CanRead { get { throw new NotImplementedException(); } }
            public override bool CanSeek { get { throw new NotImplementedException(); } }
            public override bool CanWrite { get { throw new NotImplementedException(); } }
            public override long Length { get { throw new NotImplementedException(); } }
            public override long Position { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
            public override void Flush() { throw new NotImplementedException(); }
            public override int Read(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }
            public override void SetLength(long value) { throw new NotImplementedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }
        }
    }
}
