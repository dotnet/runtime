// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;

using Xunit;

namespace System.Net.Security.Tests
{
    public class TransportContextTest
    {
        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public bool AllowAnyServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            return true;  // allow everything
        }

        [Fact]
        public void TransportContext_ConnectToServerWithSsl_GetExpectedChannelBindings()
        {
            using (var testServer = new DummyTcpServer(
                new IPEndPoint(IPAddress.Loopback, 0), EncryptionPolicy.RequireEncryption))
            using (var client = new TcpClient())
            {
                client.Connect(testServer.RemoteEndPoint);

                using (var sslStream = new SslStream(client.GetStream(), false, AllowAnyServerCertificate, null, EncryptionPolicy.RequireEncryption))
                {
                    sslStream.AuthenticateAsClient("localhost", null, SslProtocols.Tls, false);

                    TransportContext context = sslStream.TransportContext;
                    CheckTransportContext(context);
                }
            }
        }

        public void CheckTransportContext(TransportContext context)
        {
            var cbt1 = context.GetChannelBinding(ChannelBindingKind.Endpoint);
            var cbt2 = context.GetChannelBinding(ChannelBindingKind.Unique);
            var cbt3 = context.GetChannelBinding(ChannelBindingKind.Unknown);

            CheckChannelBinding(cbt1);
            CheckChannelBinding(cbt2);
            CheckChannelBinding(cbt3);

            Assert.True(cbt1 != null, "ChannelBindingKind.Endpoint token data should be returned from SCHANNEL.");
            Assert.True(cbt2 != null, "ChannelBindingKind.Unique token data should be returned from SCHANNEL.");
            Assert.True(cbt3 == null, "ChannelBindingKind.Unknown token data should not be returned from SCHANNEL since it does not map to a defined context attribute.");
        }

        public void CheckChannelBinding(ChannelBinding channelBinding)
        {
            if (channelBinding != null)
            {
                Assert.True(!channelBinding.IsInvalid, "Channel binding token should be marked as a valid SafeHandle.");
                Assert.True(channelBinding.Size > 0, "Number of bytes in a valid channel binding token should be greater than zero.");
                var bytes = new byte[channelBinding.Size];
                Marshal.Copy(channelBinding.DangerousGetHandle(), bytes, 0, channelBinding.Size);
                Assert.Equal(channelBinding.Size, bytes.Length);
            }
        }
    }
}
