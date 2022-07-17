// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    public class TransportContextTest
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task TransportContext_ConnectToServerWithSsl_GetExpectedChannelBindings()
        {
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            {
                using (var client = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.RequireEncryption))
                using (var server = new SslStream(serverStream))
                {
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync("localhost", null, SslProtocols.None, false),
                        server.AuthenticateAsServerAsync(TestConfiguration.ServerCertificate));

                    TransportContext context = client.TransportContext;
                    CheckTransportContext(context);
                }
            }
        }

        private static void CheckTransportContext(TransportContext context)
        {
            using ChannelBinding cbt1 = context.GetChannelBinding(ChannelBindingKind.Endpoint);
            using ChannelBinding cbt2 = context.GetChannelBinding(ChannelBindingKind.Unique);
            using ChannelBinding cbt3 = context.GetChannelBinding(ChannelBindingKind.Unknown);

            CheckChannelBinding(ChannelBindingKind.Endpoint, cbt1);
            CheckChannelBinding(ChannelBindingKind.Unique, cbt2);
            CheckChannelBinding(ChannelBindingKind.Unknown, cbt3);

            Assert.True(cbt1 != null, "ChannelBindingKind.Endpoint token data should be returned.");

            if (OperatingSystem.IsMacOS())
            {
                Assert.True(cbt2 == null, "ChannelBindingKind.Unique token data is not expected on OSX platform.");
            }
            else
            {
                Assert.True(cbt2 != null, "ChannelBindingKind.Unique token data should be returned.");
            }

            Assert.True(cbt3 == null, "ChannelBindingKind.Unknown token data should not be returned.");
        }

        private static void CheckChannelBinding(ChannelBindingKind kind, ChannelBinding channelBinding)
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
