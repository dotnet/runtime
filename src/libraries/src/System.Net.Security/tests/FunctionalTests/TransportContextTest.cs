// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
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
        public async Task TransportContext_ConnectToServerWithSsl_GetExpectedChannelBindings()
        {
            using (var testServer = new DummyTcpServer(
                new IPEndPoint(IPAddress.Loopback, 0), EncryptionPolicy.RequireEncryption))
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(testServer.RemoteEndPoint.Address, testServer.RemoteEndPoint.Port);

                using (var sslStream = new SslStream(client.GetStream(), false, AllowAnyServerCertificate, null, EncryptionPolicy.RequireEncryption))
                {
                    await sslStream.AuthenticateAsClientAsync("localhost", null, SslProtocols.Tls, false);

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

            CheckChannelBinding(ChannelBindingKind.Endpoint, cbt1);
            CheckChannelBinding(ChannelBindingKind.Unique, cbt2);
            CheckChannelBinding(ChannelBindingKind.Unknown, cbt3);

            Assert.True(cbt1 != null, "ChannelBindingKind.Endpoint token data should be returned.");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.True(cbt2 == null, "ChannelBindingKind.Unique token data is not expected on OSX platform.");
            }
            else
            {
                Assert.True(cbt2 != null, "ChannelBindingKind.Unique token data should be returned.");
            }

            Assert.True(cbt3 == null, "ChannelBindingKind.Unknown token data should not be returned.");
        }

        public void CheckChannelBinding(ChannelBindingKind kind, ChannelBinding channelBinding)
        {
            if (channelBinding != null)
            {
                const string PrefixEndpoint = "tls-server-end-point:";
                const string PrefixUnique = "tls-unique:";

                Assert.True(!channelBinding.IsInvalid, "Channel binding token should be marked as a valid SafeHandle.");
                Assert.True(channelBinding.Size > 0, "Number of bytes in a valid channel binding token should be greater than zero.");
                var bytes = new byte[channelBinding.Size];
                Marshal.Copy(channelBinding.DangerousGetHandle(), bytes, 0, channelBinding.Size);
                Assert.Equal(channelBinding.Size, bytes.Length);
                
                string cbt = Encoding.ASCII.GetString(bytes);
                string expectedPrefix = (kind == ChannelBindingKind.Endpoint) ? PrefixEndpoint : PrefixUnique;
                Assert.Contains(expectedPrefix, cbt);
            }
        }
    }
}
