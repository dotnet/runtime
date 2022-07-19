// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    public class ClientAsyncAuthenticateTest
    {
        private readonly ITestOutputHelper _log;

        public ClientAsyncAuthenticateTest(ITestOutputHelper output)
        {
            _log = output;
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task ClientAsyncAuthenticate_ServerRequireEncryption_ConnectWithEncryption()
        {
            await ClientAsyncSslHelper(EncryptionPolicy.RequireEncryption);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task ClientAsyncAuthenticate_ConnectionInfoInCallback_DoesNotThrow()
        {
            await ClientAsyncSslHelper(EncryptionPolicy.RequireEncryption, SslProtocols.Tls12, SslProtocolSupport.DefaultSslProtocols, AllowAnyServerCertificateAndVerifyConnectionInfo);
        }

        [Fact]
        public async Task ClientAsyncAuthenticate_ServerNoEncryption_NoConnect()
        {
            // Don't use Tls13 since we are trying to use NullEncryption
            await Assert.ThrowsAsync<AuthenticationException>(
                () => ClientAsyncSslHelper(
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                    EncryptionPolicy.NoEncryption,
#pragma warning restore SYSLIB0040
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                    SslProtocolSupport.DefaultSslProtocols, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12));
#pragma warning restore SYSLIB0039
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task ClientAsyncAuthenticate_EachSupportedProtocol_Success(SslProtocols protocol)
        {
            await ClientAsyncSslHelper(protocol, protocol);
        }

        [Theory]
        [MemberData(nameof(ProtocolMismatchData))]
        public async Task ClientAsyncAuthenticate_MismatchProtocols_Fails(
            SslProtocols clientProtocol,
            SslProtocols serverProtocol,
            Type expectedException)
        {
            Exception e = await Record.ExceptionAsync(() => ClientAsyncSslHelper(clientProtocol, serverProtocol));
            Assert.NotNull(e);
            Assert.IsAssignableFrom(expectedException, e);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task ClientAsyncAuthenticate_AllServerAllClient_Success()
        {
            await ClientAsyncSslHelper(
                SslProtocolSupport.SupportedSslProtocols,
                SslProtocolSupport.SupportedSslProtocols);
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task ClientAsyncAuthenticate_AllServerVsIndividualClientSupportedProtocols_Success(
            SslProtocols clientProtocol)
        {
            await ClientAsyncSslHelper(clientProtocol, SslProtocolSupport.SupportedSslProtocols);
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task ClientAsyncAuthenticate_IndividualServerVsAllClientSupportedProtocols_Success(
            SslProtocols serverProtocol)
        {
            await ClientAsyncSslHelper(SslProtocolSupport.SupportedSslProtocols, serverProtocol);
            // Cached Tls creds fail when used against Tls servers of higher versions.
            // Servers are not expected to dynamically change versions.
        }

        public static IEnumerable<object[]> ProtocolMismatchData()
        {
            var supportedProtocols = new SslProtocolSupport.SupportedSslProtocolsTestData();

            foreach (var serverProtocols in supportedProtocols)
            {
                foreach (var clientProtocols in supportedProtocols)
                {
                    SslProtocols serverProtocol = (SslProtocols)serverProtocols[0];
                    SslProtocols clientProtocol = (SslProtocols)clientProtocols[0];

                    if (clientProtocol != serverProtocol)
                    {
                        yield return new object[] { clientProtocol, serverProtocol, typeof(AuthenticationException) };
                    }
                }
            }
        }

        #region Helpers

        private Task ClientAsyncSslHelper(EncryptionPolicy encryptionPolicy)
        {
            return ClientAsyncSslHelper(encryptionPolicy, SslProtocolSupport.DefaultSslProtocols, SslProtocolSupport.DefaultSslProtocols);
        }

        private Task ClientAsyncSslHelper(SslProtocols clientSslProtocols, SslProtocols serverSslProtocols)
        {
            return ClientAsyncSslHelper(EncryptionPolicy.RequireEncryption, clientSslProtocols, serverSslProtocols);
        }

        private async Task ClientAsyncSslHelper(
            EncryptionPolicy encryptionPolicy,
            SslProtocols clientSslProtocols,
            SslProtocols serverSslProtocols,
            RemoteCertificateValidationCallback certificateCallback = null)
        {
            _log.WriteLine("Server: " + serverSslProtocols + "; Client: " + clientSslProtocols);

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();

            using (client)
            using (server)
            {
                // Use a different SNI for each connection to prevent TLS 1.3 renegotiation issue: https://github.com/dotnet/runtime/issues/47378
                string serverName = TestHelper.GetTestSNIName(nameof(ClientAsyncSslHelper), clientSslProtocols, serverSslProtocols);

                Task serverTask = default;
                try
                {
                    Task clientTask = client.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                        {
                            EnabledSslProtocols = clientSslProtocols,
                            RemoteCertificateValidationCallback = AllowAnyServerCertificate,
                            TargetHost = serverName });
                    serverTask = server.AuthenticateAsServerAsync( new SslServerAuthenticationOptions
                        {
                            EncryptionPolicy = encryptionPolicy,
                            EnabledSslProtocols = serverSslProtocols,
                            ServerCertificate = TestConfiguration.ServerCertificate,
                            CertificateRevocationCheckMode = X509RevocationMode.NoCheck });

                    await clientTask.WaitAsync(TestConfiguration.PassingTestTimeout);

                    _log.WriteLine("Client authenticated to server with encryption cipher: {0} {1}-bit strength",
                            client.CipherAlgorithm, client.CipherStrength);
                    Assert.True(client.CipherAlgorithm != CipherAlgorithmType.Null, "Cipher algorithm should not be NULL");
                    Assert.True(client.CipherStrength > 0, "Cipher strength should be greater than 0");
                }
                finally
                {
                    // make sure we signal server in case of client failures
                    client.Close();
                    try
                    {
                        await serverTask;
                    }
                    catch (Exception ex)
                    {
                        // We generally don't care about server but can log exception to help diagnose test failures
                        _log.WriteLine(ex.ToString());
                    }
                }
            }
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        private bool AllowAnyServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            return true;  // allow everything
        }

        private bool AllowAnyServerCertificateAndVerifyConnectionInfo(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            SslStream stream = (SslStream)sender;

            Assert.NotEqual(SslProtocols.None, stream.SslProtocol);
            Assert.NotEqual(CipherAlgorithmType.None, stream.CipherAlgorithm);

            return true;  // allow everything
        }

        #endregion Helpers
    }
}
