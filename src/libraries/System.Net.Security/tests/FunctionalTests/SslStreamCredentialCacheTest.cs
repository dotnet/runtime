// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.IO;
using System.Net.Test.Common;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamCredentialCacheTest
    {
        public enum CachedCredentialScenario
        {
            Anonymous,
            ClientCertificate,
            DisableTlsResume,
            RejectCertificate,
        }

        public static TheoryData<SslProtocols, bool, bool, CachedCredentialScenario> CachedCredentialEvictionData()
        {
            TheoryData<SslProtocols, bool, bool, CachedCredentialScenario> data = new();
            foreach (SslProtocols protocol in SslProtocolSupport.EnumerateSupportedProtocols(SslProtocols.Tls12 | SslProtocols.Tls13))
            {
                foreach (bool isServer in new[] { false, true })
                {
                    foreach (bool useLegacyHandshake in new[] { false, true })
                    {
                        foreach (CachedCredentialScenario scenario in Enum.GetValues<CachedCredentialScenario>())
                        {
                            if (!isServer || scenario != CachedCredentialScenario.DisableTlsResume)
                            {
                                data.Add(protocol, isServer, useLegacyHandshake, scenario);
                            }
                        }
                    }
                }
            }

            return data;
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(CachedCredentialEvictionData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task SslStream_CachedCredentialEvictedBeforeHandshake_UsesRetainedCredential(
            SslProtocols protocol, bool isServer, bool useLegacyHandshake, CachedCredentialScenario scenario)
        {
            await RemoteExecutor.Invoke(async (protocolString, isServerString, useLegacyHandshakeString, scenarioString) =>
            {
                SslProtocols protocol = (SslProtocols)int.Parse(protocolString);
                bool isServer = bool.Parse(isServerString);
                CachedCredentialScenario scenario = Enum.Parse<CachedCredentialScenario>(scenarioString);
                AppContext.SetSwitch("System.Net.Security.UseLegacySslStreamHandshake", bool.Parse(useLegacyHandshakeString));

                using X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate();
                using X509Certificate2 clientCertificate = scenario == CachedCredentialScenario.ClientCertificate
                    ? Configuration.Certificates.GetClientCertificate()
                    : null;
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = certificate,
                    EnabledSslProtocols = protocol,
                };
                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = Guid.NewGuid().ToString("N"),
                    EnabledSslProtocols = protocol,
                    AllowTlsResume = scenario != CachedCredentialScenario.DisableTlsResume,
                    RemoteCertificateValidationCallback = AllowAnyCertificate,
                };
                int certificateSelections = 0;
                if (scenario == CachedCredentialScenario.ClientCertificate)
                {
                    serverOptions.ClientCertificateRequired = true;
                    serverOptions.RemoteCertificateValidationCallback = AllowAnyCertificate;
                    clientOptions.LocalCertificateSelectionCallback = delegate
                    {
                        return Interlocked.Increment(ref certificateSelections) == 1 ? null : clientCertificate;
                    };
                }

                (SslStream warmupClient, SslStream warmupServer) = TestHelper.GetConnectedSslStreams();
                using (warmupClient)
                using (warmupServer)
                {
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        warmupClient.AuthenticateAsClientAsync(clientOptions),
                        warmupServer.AuthenticateAsServerAsync(serverOptions));
                }

                certificateSelections = 0;
                if (scenario == CachedCredentialScenario.RejectCertificate)
                {
                    clientOptions.TargetHost = Guid.NewGuid().ToString("N");
                    clientOptions.RemoteCertificateValidationCallback = delegate { return false; };
                }

                Type cacheType = typeof(SslStream).Assembly.GetType("System.Net.Security.SslSessionsCache", throwOnError: true);
                FieldInfo cacheField = cacheType.GetField("s_cachedCreds", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.NotNull(cacheField);
                IDictionary cache = Assert.IsAssignableFrom<IDictionary>(cacheField.GetValue(null));
                Assert.NotEmpty(cache);
                PropertyInfo targetProperty = cacheField.FieldType.GenericTypeArguments[1].GetProperty(
                    "Target", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(targetProperty);
                var evictedCredentials = new List<SafeHandle>();

                (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
                using (client)
                using (server)
                using (var listener = new TestEventListener("Private.InternalDiagnostics.System.Net.Security", EventLevel.Verbose))
                {
                    // The synchronous cache-hit event runs after lookup but before the first SSPI call.
                    // Evicting here deterministically reproduces concurrent cache scavenging.
                    int evicted = 0;
                    Task clientTask = isServer ? client.AuthenticateAsClientAsync(clientOptions) : null;
                    await listener.RunWithCallbackAsync(ev =>
                    {
                        if (ev.EventName == "Info" &&
                            ev.Payload[1] is "TryCachedCredential" &&
                            ev.Payload[2] is string message &&
                            message.StartsWith("Found a cached Handle", StringComparison.Ordinal) &&
                            Interlocked.Exchange(ref evicted, 1) == 0)
                        {
                            foreach (IDisposable reference in cache.Values)
                            {
                                evictedCredentials.Add((SafeHandle)targetProperty.GetValue(reference));
                                reference.Dispose();
                            }
                            cache.Clear();
                        }
                    }, async () =>
                    {
                        clientTask ??= client.AuthenticateAsClientAsync(clientOptions);
                        Task serverTask = server.AuthenticateAsServerAsync(serverOptions);
                        if (scenario == CachedCredentialScenario.RejectCertificate)
                        {
                            Exception handshakeException = await Record.ExceptionAsync(() =>
                                TestConfiguration.WhenAllOrAnyFailedWithTimeout(clientTask, serverTask));
                            Assert.True(handshakeException is AuthenticationException or IOException, handshakeException?.ToString());
                            await Assert.ThrowsAnyAsync<AuthenticationException>(() =>
                                clientTask.WaitAsync(TestConfiguration.PassingTestTimeout));
                            Exception serverException = await Record.ExceptionAsync(() =>
                                serverTask.WaitAsync(TestConfiguration.PassingTestTimeout));
                            if (serverException is not null)
                            {
                                Assert.Contains(serverException.GetType(), new[] { typeof(AuthenticationException), typeof(IOException) });
                            }
                            Assert.False(client.IsAuthenticated);
                        }
                        else
                        {
                            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(clientTask, serverTask);
                        }
                    });

                    Assert.Equal(1, evicted);
                    if (scenario != CachedCredentialScenario.RejectCertificate)
                    {
                        Assert.Equal(protocol, client.SslProtocol);
                        Assert.Equal(protocol, server.SslProtocol);
                        await TestHelper.PingPong(client, server);
                    }
                    if (scenario == CachedCredentialScenario.ClientCertificate)
                    {
                        Assert.InRange(certificateSelections, 2, int.MaxValue);
                        Assert.True(client.IsMutuallyAuthenticated);
                        Assert.True(server.IsMutuallyAuthenticated);
                        Assert.Equal(clientCertificate, server.RemoteCertificate);
                    }
                }

                Assert.NotEmpty(evictedCredentials);
                Assert.All(evictedCredentials, credential => Assert.True(credential.IsClosed));
            }, ((int)protocol).ToString(), isServer.ToString(), useLegacyHandshake.ToString(), scenario.ToString()).DisposeAsync();
        }

        [Fact]
        public async Task SslStream_SameCertUsedForClientAndServer_Ok()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new SslStream(stream1, true, AllowAnyCertificate))
            using (var server = new SslStream(stream2, true, AllowAnyCertificate))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                // Using the same certificate for server and client auth.
                X509Certificate2Collection clientCertificateCollection =
                    new X509Certificate2Collection(certificate);

                Task t1 = server.AuthenticateAsServerAsync(certificate, true, false);
                Task t2 = client.AuthenticateAsClientAsync(
                                            certificate.GetNameInfo(X509NameType.SimpleName, false),
                                            clientCertificateCollection, false);


                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);

                if (Capability.IsTrustedRootCertificateInstalled())
                {
                    // https://technet.microsoft.com/en-us/library/hh831771.aspx#BKMK_Changes2012R2
                    // On Windows, the "Management of trusted issuers for client authentication" is configured
                    // such that the behavior to send the Trusted Issuers List by default is off.

                    Assert.True(client.IsMutuallyAuthenticated);
                    Assert.True(server.IsMutuallyAuthenticated);
                }
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task SslStream_ClientCertificateContext_DoesNotPolluteAnonymousCredentialCache(SslProtocols protocol)
        {
            await RemoteExecutor.Invoke(async protocolString =>
            {
                SslProtocols protocol = (SslProtocols)int.Parse(protocolString);
                using X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate();
                using X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate();

                var serverOptions = new SslServerAuthenticationOptions
                {
                    ClientCertificateRequired = true,
                    EnabledSslProtocols = protocol,
                    RemoteCertificateValidationCallback = AllowAnyCertificate,
                    ServerCertificateContext = SslStreamCertificateContext.Create(serverCertificate, null, false),
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    ClientCertificateContext = SslStreamCertificateContext.Create(clientCertificate, null, false),
                    EnabledSslProtocols = protocol,
                    RemoteCertificateValidationCallback = AllowAnyCertificate,
                    TargetHost = Guid.NewGuid().ToString("N"),
                };

                await RunConnectionAsync(clientOptions, serverOptions, clientCertificate);

                clientOptions.ClientCertificateContext = null;
                clientOptions.TargetHost = Guid.NewGuid().ToString("N");

                await RunConnectionAsync(clientOptions, serverOptions, expectedClientCertificate: null);
            }, ((int)protocol).ToString()).DisposeAsync();

            static async Task RunConnectionAsync(
                SslClientAuthenticationOptions clientOptions,
                SslServerAuthenticationOptions serverOptions,
                X509Certificate2? expectedClientCertificate)
            {
                (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
                using (client)
                using (server)
                {
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions));

                    if (expectedClientCertificate is null)
                    {
                        Assert.Null(server.RemoteCertificate);
                    }
                    else
                    {
                        Assert.Equal(expectedClientCertificate, server.RemoteCertificate);
                    }
                }
            }
        }

        private static bool AllowAnyCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
