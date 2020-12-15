// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class CertificateValidationRemoteServer
    {
        [OuterLoop("Uses external servers")]
        [ConditionalTheory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CertificateValidationRemoteServer_EndToEnd_Ok(bool useAsync)
        {
            using (var client = new TcpClient(AddressFamily.InterNetwork))
            {
                try
                {
                    await client.ConnectAsync(Configuration.Security.TlsServer.IdnHost, Configuration.Security.TlsServer.Port);
                }
                catch (Exception ex)
                {
                    // if we cannot connect, skip the test instead of failing.
                    // This test is not trying to test networking.
                    throw new SkipTestException($"Unable to connect to '{Configuration.Security.TlsServer.IdnHost}': {ex.Message}");
                }

                using (SslStream sslStream = new SslStream(client.GetStream(), false, RemoteHttpsCertValidation, null))
                {
                    try
                    {
                        if (useAsync)
                        {
                            await sslStream.AuthenticateAsClientAsync(Configuration.Security.TlsServer.IdnHost);
                        }
                        else
                        {
                            sslStream.AuthenticateAsClient(Configuration.Security.TlsServer.IdnHost);
                        }
                    }
                    catch (IOException ex) when (ex.InnerException is SocketException &&
                      ((SocketException)ex.InnerException).SocketErrorCode == SocketError.ConnectionReset)
                    {
                        // Since we try to verify certificate validation, ignore IO errors
                        // caused most likely by environmental failures.
                        throw new SkipTestException($"Unable to connect to '{Configuration.Security.TlsServer.IdnHost}': {ex.InnerException.Message}");
                    }
                }
            }
        }

        // MacOS has has special validation rules for apple.com and icloud.com
        [ConditionalTheory]
        [OuterLoop("Uses external servers")]
        [InlineData("www.apple.com")]
        [InlineData("www.icloud.com")]
        [PlatformSpecific(TestPlatforms.OSX)]
        public Task CertificateValidationApple_EndToEnd_Ok(string host)
        {
            return EndToEndHelper(host);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls12))]
        [OuterLoop("Uses external servers")]
        [InlineData("api.nuget.org")]
        [InlineData("")]
        public async Task DefaultConnect_EndToEnd_Ok(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                host = Configuration.Security.TlsServer.IdnHost;
            }

            await EndToEndHelper(host);
            // Second try may change the handshake because of TLS resume.
            await EndToEndHelper(host);
        }

        private async Task EndToEndHelper(string host)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(host, 443);
                }
                catch (Exception ex)
                {
                    // if we cannot connect skip the test instead of failing.
                    throw new SkipTestException($"Unable to connect to '{host}': {ex.Message}");
                }

                using (SslStream sslStream = new SslStream(client.GetStream(), false, RemoteHttpsCertValidation, null))
                {
                    await sslStream.AuthenticateAsClientAsync(host);
                }
            }
        }

        private bool RemoteHttpsCertValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Assert.Equal(SslPolicyErrors.None, sslPolicyErrors);

            return true;
        }
    }
}
