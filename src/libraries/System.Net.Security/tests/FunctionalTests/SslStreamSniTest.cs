// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamSniTest
    {
        [Theory]
        [MemberData(nameof(HostNameData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task SslStream_ClientSendsSNIServerReceives_Ok(string hostName)
        {
            X509Certificate serverCert = Configuration.Certificates.GetSelfSignedServerCertificate();

            await WithVirtualConnection(async (server, client) =>
                {
                    Task clientJob = Task.Run(() => {
                        client.AuthenticateAsClient(hostName);
                    });

                    SslServerAuthenticationOptions options = DefaultServerOptions();

                    int timesCallbackCalled = 0;
                    options.ServerCertificateSelectionCallback = (sender, actualHostName) =>
                    {
                        timesCallbackCalled++;
                        Assert.Equal(hostName, actualHostName);
                        return serverCert;
                    };

                    await TaskTimeoutExtensions.WhenAllOrAnyFailed(new[] { clientJob, server.AuthenticateAsServerAsync(options, CancellationToken.None) });

                    Assert.Equal(1, timesCallbackCalled);
                },
                (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
                {
                    Assert.Equal(serverCert, certificate);
                    return true;
                }
            );
        }

        [Theory]
        [MemberData(nameof(HostNameData))]
        public async Task SslStream_ServerCallbackAndLocalCertificateSelectionSet_Throws(string hostName)
        {
            X509Certificate serverCert = Configuration.Certificates.GetSelfSignedServerCertificate();

            int timesCallbackCalled = 0;

            var selectionCallback = new LocalCertificateSelectionCallback((object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] issuers) =>
            {
                Assert.True(false, "LocalCertificateSelectionCallback called when AuthenticateAsServerAsync was expected to fail.");
                return null;
            });

            var validationCallback = new RemoteCertificateValidationCallback((object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                Assert.Equal(serverCert, certificate);
                return true;
            });

            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (SslStream server = new SslStream(stream1, false, null, selectionCallback),
                             client = new SslStream(stream2, leaveInnerStreamOpen: false, validationCallback))
            {
                Task clientJob = Task.Run(() => {
                    client.AuthenticateAsClient(hostName);
                    Assert.True(false, "RemoteCertificateValidationCallback called when AuthenticateAsServerAsync was expected to fail.");
                });

                SslServerAuthenticationOptions options = DefaultServerOptions();
                options.ServerCertificateSelectionCallback = (sender, actualHostName) =>
                {
                    timesCallbackCalled++;
                    Assert.Equal(hostName, actualHostName);
                    return serverCert;
                };

                await Assert.ThrowsAsync<InvalidOperationException>(() => server.AuthenticateAsServerAsync(options, CancellationToken.None));

                Assert.Equal(0, timesCallbackCalled);
            }
        }

        [Theory]
        [MemberData(nameof(HostNameData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task SslStream_ServerCallbackNotSet_UsesLocalCertificateSelection(string hostName)
        {
            X509Certificate serverCert = Configuration.Certificates.GetSelfSignedServerCertificate();

            int timesCallbackCalled = 0;

            var selectionCallback = new LocalCertificateSelectionCallback((object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] issuers) =>
            {
                Assert.Equal(string.Empty, targetHost);
                Assert.True(localCertificates.Contains(serverCert));
                timesCallbackCalled++;
                return serverCert;
            });

            var validationCallback = new RemoteCertificateValidationCallback((object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                Assert.Equal(serverCert, certificate);
                return true;
            });

            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (SslStream server = new SslStream(stream1, false, null, selectionCallback),
                             client = new SslStream(stream2, leaveInnerStreamOpen: false, validationCallback))
            {
                Task clientJob = Task.Run(() => {
                    client.AuthenticateAsClient(hostName);
                });

                SslServerAuthenticationOptions options = DefaultServerOptions();
                options.ServerCertificate = serverCert;

                await TaskTimeoutExtensions.WhenAllOrAnyFailed(new[] { clientJob, server.AuthenticateAsServerAsync(options, CancellationToken.None) });

                Assert.Equal(1, timesCallbackCalled);
            }
        }

        [Fact]
        [SkipOnCoreClr("System.Net.Tests are flaky and/or long running: https://github.com/dotnet/runtime/issues/131", ~RuntimeConfiguration.Release)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/131", TestRuntimes.Mono)] // System.Net.Tests are flaky and/or long running
        public async Task SslStream_NoSniFromClient_CallbackReturnsNull()
        {
            await WithVirtualConnection(async (server, client) =>
            {
                Task clientJob = Task.Run(() => {
                    Assert.Throws<IOException>(() =>
                        client.AuthenticateAsClient("test")
                    );
                });

                int timesCallbackCalled = 0;
                SslServerAuthenticationOptions options = DefaultServerOptions();
                options.ServerCertificateSelectionCallback = (sender, actualHostName) =>
                {
                    timesCallbackCalled++;
                    return null;
                };

                var cts = new CancellationTokenSource();
                await Assert.ThrowsAsync<AuthenticationException>(WithAggregateExceptionUnwrapping(async () =>
                    await server.AuthenticateAsServerAsync(options, cts.Token)
                ));

                // to break connection so that client is not waiting
                server.Dispose();

                Assert.Equal(1, timesCallbackCalled);

                await clientJob;
            },
            (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                return true;
            });
        }

        private static Func<Task> WithAggregateExceptionUnwrapping(Func<Task> a)
        {
            return async () => {
                try
                {
                    await a();
                }
                catch (AggregateException e)
                {
                    throw e.InnerException;
                }
            };
        }

        private static SslServerAuthenticationOptions DefaultServerOptions()
        {
            return new SslServerAuthenticationOptions()
            {
                ClientCertificateRequired = false,
                EnabledSslProtocols = SslProtocols.None,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
            };
        }

        private async Task WithVirtualConnection(Func<SslStream, SslStream, Task> serverClientConnection, RemoteCertificateValidationCallback clientCertValidate)
        {
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (SslStream server = new SslStream(serverStream, leaveInnerStreamOpen: false),
                             client = new SslStream(clientStream, leaveInnerStreamOpen: false, clientCertValidate))
            {
                await serverClientConnection(server, client);
            }
        }

        public static IEnumerable<object[]> HostNameData()
        {
            yield return new object[] { "a" };
            yield return new object[] { "test" };
            // max allowed hostname length is 63
            yield return new object[] { new string('a', 63) };
            yield return new object[] { "\u017C\u00F3\u0142\u0107 g\u0119\u015Bl\u0105 ja\u017A\u0144. \u7EA2\u70E7. \u7167\u308A\u713C\u304D" };
        }
    }
}
