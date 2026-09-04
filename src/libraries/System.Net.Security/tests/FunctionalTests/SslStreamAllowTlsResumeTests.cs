// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Microsoft.DotNet.XUnitExtensions;

using System.Net.Test.Common;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
    public class SslStreamTlsResumeTests
    {
        private static FieldInfo connectionInfo = typeof(SslStream).GetField(
                                    "_connectionInfo",
                                    BindingFlags.Instance | BindingFlags.NonPublic);

        private static PropertyInfo tlsResumed =
                                    typeof(SslStream).Assembly.GetType("System.Net.Security.SslConnectionInfo")
                                    ?.GetProperty("TlsResumed");

        private bool CheckResumeFlag(SslStream ssl)
        {
            Assert.True(connectionInfo != null, "Could not find the SslStream._connectionInfo field via reflection");
            Assert.True(tlsResumed != null, "Could not find the SslConnectionInfo.TlsResumed property via reflection");

            // SslConnectionInfo.TlsResumed is an internal property we read via reflection to
            // validate whether the handshake resumed a previous session.
            object info = connectionInfo.GetValue(ssl);
            return (bool)tlsResumed.GetValue(info);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ClientDisableTlsResume_Succeeds(bool testClient)
        {
            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null, false)
                };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    EnabledSslProtocols = SslProtocols.Tls12,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                };

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions));

            Assert.True(client.IsAuthenticated);
            Assert.True(client.IsEncrypted);
            await client.ShutdownAsync();
            await server.ShutdownAsync();
            client.Dispose();
            server.Dispose();

            // create new TLS to the same server. This should resume TLS.
            (client, server) = TestHelper.GetConnectedSslStreams();
            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions));

            //Assert.True(CheckResumeFlag(client));
            if (!CheckResumeFlag(client))
            {
                throw new SkipTestException("Unable to resume test session");
            }
            Assert.True(CheckResumeFlag(server));
            await client.ShutdownAsync();
            await server.ShutdownAsync();
            client.Dispose();
            server.Dispose();

            // Disable TLS resumption and try it again.
            if (testClient)
            {
                clientOptions.AllowTlsResume = false;
            }
            else
            {
                serverOptions.AllowTlsResume = false;
            }

            // We do multiple loops to also cover credential cache.
            for (int i=0; i < 3; i++)
            {
                (client, server) = TestHelper.GetConnectedSslStreams();
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                    client.AuthenticateAsClientAsync(clientOptions),
                                    server.AuthenticateAsServerAsync(serverOptions));

                Assert.False(CheckResumeFlag(client), $"TLS session resumed in round ${i}");
                Assert.False(CheckResumeFlag(server), $"TLS session resumed in round ${i}");
                await client.ShutdownAsync();
                await server.ShutdownAsync();
                client.Dispose();
                server.Dispose();
            }

            // TLS resume still should be possible
            if (testClient)
            {
                clientOptions.AllowTlsResume = true;
            }
            else
            {
                serverOptions.AllowTlsResume = true;
            }

            // On Windows it may take extra round to refresh the session cache.
            (client, server) = TestHelper.GetConnectedSslStreams();
            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions),
                                server.AuthenticateAsServerAsync(serverOptions));
            client.Dispose();
            server.Dispose();

            (client, server) = TestHelper.GetConnectedSslStreams();
            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions),
                                server.AuthenticateAsServerAsync(serverOptions));

            Assert.True(CheckResumeFlag(client));
            Assert.True(CheckResumeFlag(server));
            client.Dispose();
            server.Dispose();
        }

        [Theory]
        [MemberData(nameof(SslProtocolsData))]
        public Task NoClientCert_DefaultValue_ResumeSucceeds(SslProtocols sslProtocol)
        {
            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    EnabledSslProtocols = sslProtocol,
                    ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null, false)
                };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    EnabledSslProtocols = sslProtocol,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                };

            return ResumeSucceedsInternal(serverOptions, clientOptions);
        }

        public static TheoryData<SslProtocols> SslProtocolsData()
        {
            var data = new TheoryData<SslProtocols>();

            foreach (SslProtocols protocol in SslProtocolSupport.EnumerateSupportedProtocols(SslProtocols.Tls12 | SslProtocols.Tls13, true))
            {
                data.Add(protocol);
            }

            return data;
        }

        public enum ClientCertSource
        {
            ClientCertificate,
            SelectionCallback,
            CertificateContext
        }

        public static TheoryData<SslProtocols, bool, ClientCertSource> ClientCertTestData()
        {
            var data = new TheoryData<SslProtocols, bool, ClientCertSource>();

            foreach (SslProtocols protocol in SslProtocolSupport.EnumerateSupportedProtocols(SslProtocols.Tls12 | SslProtocols.Tls13, true))
            foreach (bool certRequired in new[] { true, false })
            foreach (ClientCertSource source in Enum.GetValues(typeof(ClientCertSource)))
            {
                data.Add(protocol, certRequired, source);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(ClientCertTestData))]
        public Task ClientCert_DefaultValue_ResumeSucceeds(SslProtocols sslProtocol, bool certificateRequired, ClientCertSource certSource)
        {
            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    EnabledSslProtocols = sslProtocol,
                    ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null, false),
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                    ClientCertificateRequired = certificateRequired,
                };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    EnabledSslProtocols = sslProtocol,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                };

            X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate();

            switch (certSource)
            {
                case ClientCertSource.ClientCertificate:
                    clientOptions.ClientCertificates = new X509CertificateCollection() { clientCertificate };
                    break;
                case ClientCertSource.SelectionCallback:
                    clientOptions.LocalCertificateSelectionCallback = delegate { return clientCertificate; };
                    break;
                case ClientCertSource.CertificateContext:
                    clientOptions.ClientCertificateContext = SslStreamCertificateContext.Create(clientCertificate, new());
                    break;
            }

            return ResumeSucceedsInternal(serverOptions, clientOptions);
        }

        private async Task ResumeSucceedsInternal(SslServerAuthenticationOptions serverOptions, SslClientAuthenticationOptions clientOptions)
        {
            // no resume on the first run
            await RunConnectionAsync(serverOptions, clientOptions, false);

            for (int i = 0; i < 3; i++)
            {
                // create new TLS to the same server. This should resume TLS.
                await RunConnectionAsync(serverOptions, clientOptions, true);
            }
        }

        private async Task RunConnectionAsync(SslServerAuthenticationOptions serverOptions, SslClientAuthenticationOptions clientOptions, bool? expectResume)
        {
            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions));

                if (expectResume.HasValue)
                {
                    Assert.Equal(expectResume.Value, CheckResumeFlag(client));
                    Assert.Equal(expectResume.Value, CheckResumeFlag(server));
                }

                await TestHelper.PingPong(client, server);

                await client.ShutdownAsync();
                await server.ShutdownAsync();
            }
        }

        [Theory]
        [MemberData(nameof(SslProtocolsData))]
        public Task ClientChangeCert_NoResume(SslProtocols sslProtocol)
        {
            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    EnabledSslProtocols = sslProtocol,
                    ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null, false),
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                    ClientCertificateRequired = true,
                };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    EnabledSslProtocols = sslProtocol,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                    ClientCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetClientCertificate(), null, false)
                };

            return TestNoResumeAfterChange(serverOptions, clientOptions,
                (clientOps, _) => clientOps.ClientCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetSelfSignedClientCertificate(), null, false),
                (clientOps, _) => clientOps.ClientCertificateContext = null);
        }

        [Theory]
        [MemberData(nameof(SslProtocolsData))]
        public Task DifferentHost_NoResume(SslProtocols sslProtocol)
        {
            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    EnabledSslProtocols = sslProtocol,
                    ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null, false)
                };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    EnabledSslProtocols = sslProtocol,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                    ClientCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetClientCertificate(), null, false)
                };

            return TestNoResumeAfterChange(serverOptions, clientOptions,
                (clientOps, _) => clientOps.TargetHost = Guid.NewGuid().ToString("N"));
        }

        [Fact]
        public Task DifferentProtocol_NoResume()
        {
            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null, false)
                };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    EnabledSslProtocols = SslProtocols.Tls12,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                };

            return TestNoResumeAfterChange(serverOptions, clientOptions,
                (clientOps, _) => clientOps.EnabledSslProtocols = SslProtocols.None);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public Task DifferentRevocationCheckMode_NoResume()
        {
            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null, false)
                };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                    CertificateRevocationCheckMode = X509RevocationMode.Offline,
                };

            return TestNoResumeAfterChange(serverOptions, clientOptions,
                (clientOps, _) => clientOps.CertificateRevocationCheckMode = X509RevocationMode.NoCheck);
        }

        [Fact]
        public Task DifferentEncryptionPolicy_NoResume()
        {
            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    EnabledSslProtocols = SslProtocols.Tls12,
                    ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null, false)
                };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                };
#pragma warning disable SYSLIB0040 // 'AllowNoEncryption' is obsolete
            return TestNoResumeAfterChange(serverOptions, clientOptions,
                (clientOps, _) => clientOps.EncryptionPolicy = EncryptionPolicy.AllowNoEncryption);
#pragma warning restore SYSLIB0040 // 'AllowNoEncryption' is obsolete
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)] // CipherSuitesPolicy is suppoted only on Linux
        public Task DifferentCipherSuitesPolicy_NoResume()
        {
            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null, false)
                };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                };

            return TestNoResumeAfterChange(serverOptions, clientOptions,
                (clientOps, _) => clientOps.CipherSuitesPolicy = new CipherSuitesPolicy(new[] { TlsCipherSuite.TLS_AES_128_GCM_SHA256 }));
        }

        private async Task TestNoResumeAfterChange(SslServerAuthenticationOptions serverOptions, SslClientAuthenticationOptions clientOptions, params Action<SslClientAuthenticationOptions, SslServerAuthenticationOptions>[] updateOptions)
        {
            // confirm sessions are resumable and prime for resumption
            await RunConnectionAsync(serverOptions, clientOptions, false);
            await RunConnectionAsync(serverOptions, clientOptions, true);

            foreach (Action<SslClientAuthenticationOptions, SslServerAuthenticationOptions> update in updateOptions)
            {
                update(clientOptions, serverOptions);

                // after changing options, the session should not be resumed
                await RunConnectionAsync(serverOptions, clientOptions, false);
            }
        }

        // By default the peer certificate is not re-validated on a resumed handshake, so the
        // RemoteCertificateValidationCallback is not invoked. Setting the
        // System.Net.Security.RevalidateCertificateOnTlsResume switch restores the previous
        // behavior of running the callback on every resumption. This applies to both the client
        // (validating the server certificate) and the server (validating the client certificate).
        // Toggle the switch via its environment variable in a child process so the cached switch
        // value is picked up.
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task ResumedHandshake_RevalidateSwitch_ControlsCallback(bool revalidateOnResume, bool testServer)
        {
            // Reserve a unique temp path for the skip marker and remove it up-front, so the
            // marker exists only if the child process explicitly creates it to signal that the
            // environment could not establish session resumption. In that case the parent treats
            // the run as skipped rather than a failure (matching the SkipTestException pattern
            // used elsewhere in this file).
            string skipMarker = Path.GetTempFileName();
            File.Delete(skipMarker);

            var psi = new ProcessStartInfo();
            psi.Environment["DOTNET_SYSTEM_NET_SECURITY_REVALIDATECERTIFICATEONTLSRESUME"] = revalidateOnResume ? "1" : "0";
            psi.Environment["SSLSTREAM_RESUME_SKIP_MARKER"] = skipMarker;

            try
            {
                await RemoteExecutor.Invoke(async (revalidateStr, testServerStr) =>
                {
                    bool revalidate = bool.Parse(revalidateStr);
                    bool onServer = bool.Parse(testServerStr);

                    FieldInfo connectionInfoField = typeof(SslStream).GetField("_connectionInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.True(connectionInfoField != null, "Could not find the SslStream._connectionInfo field via reflection");

                    Type connectionInfoType = typeof(SslStream).Assembly.GetType("System.Net.Security.SslConnectionInfo");
                    Assert.True(connectionInfoType != null, "Could not find the System.Net.Security.SslConnectionInfo type via reflection");

                    PropertyInfo tlsResumedProperty = connectionInfoType.GetProperty("TlsResumed");
                    Assert.True(tlsResumedProperty != null, "Could not find the SslConnectionInfo.TlsResumed property via reflection");

                    bool IsResumed(SslStream ssl) => (bool)tlsResumedProperty.GetValue(connectionInfoField.GetValue(ssl));

                    int clientCallbackCount = 0;
                    int serverCallbackCount = 0;

                    var serverOptions = new SslServerAuthenticationOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls12,
                        ServerCertificateContext = SslStreamCertificateContext.Create(Configuration.Certificates.GetServerCertificate(), null, false),
                        ClientCertificateRequired = onServer,
                        RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                        {
                            serverCallbackCount++;
                            return true;
                        },
                    };
                    var clientOptions = new SslClientAuthenticationOptions
                    {
                        TargetHost = Guid.NewGuid().ToString("N"),
                        EnabledSslProtocols = SslProtocols.Tls12,
                        CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                        RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                        {
                            clientCallbackCount++;
                            return true;
                        },
                    };

                    if (onServer)
                    {
                        clientOptions.ClientCertificates = new X509CertificateCollection() { Configuration.Certificates.GetClientCertificate() };
                    }

                    // The callback under test is the one that validates the peer certificate on the
                    // side identified by 'onServer'.
                    int MeasuredCount() => onServer ? serverCallbackCount : clientCallbackCount;
                    void ResetMeasuredCount()
                    {
                        clientCallbackCount = 0;
                        serverCallbackCount = 0;
                    }

                    async Task<bool> ConnectAsync()
                    {
                        (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
                        using (client)
                        using (server)
                        {
                            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions),
                                server.AuthenticateAsServerAsync(serverOptions));

                            bool resumed = IsResumed(client);

                            await client.ShutdownAsync();
                            await server.ShutdownAsync();
                            return resumed;
                        }
                    }

                    // Prime the session cache with a full handshake; the callback always runs here.
                    Assert.False(await ConnectAsync());
                    Assert.True(MeasuredCount() > 0, "Validation callback was not invoked on the initial full handshake");

                    // Establish a resumed session and measure whether the callback runs on it.
                    bool measuredResume = false;
                    for (int i = 0; i < 5 && !measuredResume; i++)
                    {
                        ResetMeasuredCount();
                        measuredResume = await ConnectAsync();
                    }

                    if (!measuredResume)
                    {
                        // The environment could not establish resumption; signal a skip to the parent.
                        File.WriteAllText(Environment.GetEnvironmentVariable("SSLSTREAM_RESUME_SKIP_MARKER"), "skip");
                        return;
                    }

                    if (revalidate)
                    {
                        Assert.True(MeasuredCount() > 0, "Validation callback should run on resumption when RevalidateCertificateOnTlsResume is set");
                    }
                    else
                    {
                        Assert.True(MeasuredCount() == 0, "Validation callback should not run on resumption by default");
                    }
                }, revalidateOnResume.ToString(), testServer.ToString(), new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();

                if (File.Exists(skipMarker))
                {
                    throw new SkipTestException("Unable to establish TLS session resumption");
                }
            }
            finally
            {
                if (File.Exists(skipMarker))
                {
                    File.Delete(skipMarker);
                }
            }
        }
    }
}
