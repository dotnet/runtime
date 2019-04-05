// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
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

    public class NegotiatedCipherSuiteTest
    {
        private static bool IsKnownPlatformSupportingTls13 => PlatformDetection.IsUbuntu1810OrHigher;
        private static bool Tls13Supported { get; set; } = ProtocolsSupported(SslProtocols.Tls13);

        private static HashSet<TlsCipherSuite> s_tls13CipherSuiteLookup = new HashSet<TlsCipherSuite>(GetTls13CipherSuites());
        private static HashSet<TlsCipherSuite> s_tls12CipherSuiteLookup = new HashSet<TlsCipherSuite>(GetTls12CipherSuites());
        private static HashSet<TlsCipherSuite> s_tls10And11CipherSuiteLookup = new HashSet<TlsCipherSuite>(GetTls10And11CipherSuites());

        private static Dictionary<SslProtocols, HashSet<TlsCipherSuite>> _protocolCipherSuiteLookup = new Dictionary<SslProtocols, HashSet<TlsCipherSuite>>()
        {
            { SslProtocols.Tls12, s_tls12CipherSuiteLookup },
            { SslProtocols.Tls11, s_tls10And11CipherSuiteLookup },
            { SslProtocols.Tls, s_tls10And11CipherSuiteLookup },
        };

        [ConditionalFact(nameof(IsKnownPlatformSupportingTls13))]
        public void Tls13IsSupported_GetValue_ReturnsTrue()
        {
            // Validate that flag used in this file works correctly
            Assert.True(Tls13Supported);
        }

        [ConditionalFact(nameof(Tls13Supported))]
        public void NegotiatedCipherSuite_SslProtocolIsTls13_ShouldBeTls13()
        {
            var p = new ConnectionParams()
            {
                SslProtocols = SslProtocols.Tls13
            };

            NegotiatedParams ret = ConnectAndGetNegotiatedParams(p, p);
            ret.Succeeded();

            Assert.True(
                s_tls13CipherSuiteLookup.Contains(ret.CipherSuite),
                $"`{ret.CipherSuite}` is not recognized as TLS 1.3 cipher suite");
        }

        [Theory]
        [InlineData(SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls12)]
        public void NegotiatedCipherSuite_SslProtocolIsLowerThanTls13_ShouldMatchTheProtocol(SslProtocols protocol)
        {
            var p = new ConnectionParams()
            {
                SslProtocols = protocol
            };

            NegotiatedParams ret = ConnectAndGetNegotiatedParams(p, p);
            ret.Succeeded();

            Assert.True(
                _protocolCipherSuiteLookup[protocol].Contains(ret.CipherSuite),
                $"`{ret.CipherSuite}` is not recognized as {protocol} cipher suite");
        }

        [Fact]
        public void NegotiatedCipherSuite_BeforeNegotiationStarted_ShouldThrow()
        {
            using (var ms = new MemoryStream())
            using (var server = new SslStream(ms, leaveInnerStreamOpen: false))
            {
                Assert.Throws<InvalidOperationException>(() => server.NegotiatedCipherSuite);
            }
        }

        private static bool ProtocolsSupported(SslProtocols protocols)
        {
            var defaultParams = new ConnectionParams();
            defaultParams.SslProtocols = protocols;
            NegotiatedParams ret = ConnectAndGetNegotiatedParams(defaultParams, defaultParams);
            return ret.HasSucceeded && protocols.HasFlag(ret.Protocol);
        }

        private static IEnumerable<TlsCipherSuite> GetTls13CipherSuites()
        {
            // https://tools.ietf.org/html/rfc8446#appendix-B.4
            yield return TlsCipherSuite.TLS_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256;
            yield return TlsCipherSuite.TLS_AES_128_CCM_SHA256;
            yield return TlsCipherSuite.TLS_AES_128_CCM_8_SHA256;
        }

        private static IEnumerable<TlsCipherSuite> GetTls12CipherSuites()
        {
            // openssl ciphers -tls1_2 -s --stdname -v
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA;
        }

        private static IEnumerable<TlsCipherSuite> GetTls10And11CipherSuites()
        {
            // openssl ciphers -tls1_1 -s --stdname -v
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA;
            yield return TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA;
            yield return TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA;

            // rfc5289 values (OSX)
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384;
            yield return TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384;
            yield return TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256;
            yield return TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384;
            yield return TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256;
            yield return TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384;
        }

        private static async Task<Exception> WaitForSecureConnection(VirtualNetwork connection, Func<Task> server, Func<Task> client)
        {
            Task serverTask = null;
            Task clientTask = null;

            // check if failed synchronously
            try
            {
                serverTask = server();
                clientTask = client();
            }
            catch (Exception e)
            {
                connection.BreakConnection();

                if (!(e is AuthenticationException || e is Win32Exception))
                {
                    throw;
                }

                if (serverTask != null)
                {
                    // i.e. for server we used DEFAULT options but for client we chose not supported cipher suite
                    //      this will cause client to fail synchronously while server awaits connection
                    try
                    {
                        // since we broke connection the server should finish
                        await serverTask;
                    }
                    catch (AuthenticationException) { }
                    catch (Win32Exception) { }
                    catch (VirtualNetwork.VirtualNetworkConnectionBroken) { }
                }

                return e;
            }

            // Since we got here it means client and server have at least 1 choice
            // of cipher suite
            // Now we expect both sides to fail or both to succeed

            Exception failure = null;

            try
            {
                await serverTask.ConfigureAwait(false);
            }
            catch (Exception e) when (e is AuthenticationException || e is Win32Exception)
            {
                failure = e;

                // avoid client waiting for server's response
                connection.BreakConnection();
            }

            try
            {
                await clientTask.ConfigureAwait(false);

                // Fail if server has failed but client has succeeded
                Assert.Null(failure);
            }
            catch (Exception e) when (e is VirtualNetwork.VirtualNetworkConnectionBroken || e is AuthenticationException || e is Win32Exception)
            {
                // Fail if server has succeeded but client has failed
                Assert.NotNull(failure);

                if (e.GetType() != typeof(VirtualNetwork.VirtualNetworkConnectionBroken))
                {
                    failure = new AggregateException(new Exception[] { failure, e });
                }
            }

            return failure;
        }

        private static NegotiatedParams ConnectAndGetNegotiatedParams(ConnectionParams serverParams, ConnectionParams clientParams)
        {
            VirtualNetwork vn = new VirtualNetwork();
            using (VirtualNetworkStream serverStream = new VirtualNetworkStream(vn, isServer: true),
                                        clientStream = new VirtualNetworkStream(vn, isServer: false))
            using (SslStream server = new SslStream(serverStream, leaveInnerStreamOpen: false),
                             client = new SslStream(clientStream, leaveInnerStreamOpen: false))
            {
                var serverOptions = new SslServerAuthenticationOptions();
                serverOptions.ServerCertificate = Configuration.Certificates.GetSelfSignedServerCertificate();
                serverOptions.EncryptionPolicy = serverParams.EncryptionPolicy;
                serverOptions.EnabledSslProtocols = serverParams.SslProtocols;

                var clientOptions = new SslClientAuthenticationOptions();
                clientOptions.EncryptionPolicy = clientParams.EncryptionPolicy;
                clientOptions.EnabledSslProtocols = clientParams.SslProtocols;
                clientOptions.TargetHost = "test";
                clientOptions.RemoteCertificateValidationCallback =
                    new RemoteCertificateValidationCallback((object sender,
                                                             X509Certificate certificate,
                                                             X509Chain chain,
                                                             SslPolicyErrors sslPolicyErrors) => {
                                                                 return true;
                                                             });

                Func<Task> serverTask = () => server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);
                Func<Task> clientTask = () => client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);

                Exception failure = WaitForSecureConnection(vn, serverTask, clientTask).Result;

                if (failure == null)
                {
                    // send some bytes, make sure they can talk
                    byte[] data = new byte[] { 1, 2, 3 };
                    server.WriteAsync(data, 0, data.Length);

                    for (int i = 0; i < data.Length; i++)
                    {
                        Assert.Equal(data[i], client.ReadByte());
                    }

                    return new NegotiatedParams(server, client);
                }
                else
                {
                    return new NegotiatedParams(failure);
                }
            }
        }

        private class ConnectionParams
        {
            public EncryptionPolicy EncryptionPolicy = EncryptionPolicy.RequireEncryption;
            public SslProtocols SslProtocols = SslProtocols.None;
        }

        private class NegotiatedParams
        {
            private Exception _failure;

            public bool HasSucceeded => _failure == null;
            public SslProtocols Protocol { get; private set; }
            public TlsCipherSuite CipherSuite { get; private set; }

            public NegotiatedParams(Exception failure)
            {
                _failure = failure;
            }

            public NegotiatedParams(SslStream serverStream, SslStream clientStream)
            {
                _failure = null;
                CipherSuite = serverStream.NegotiatedCipherSuite;
                Protocol = serverStream.SslProtocol;

                Assert.Equal(CipherSuite, clientStream.NegotiatedCipherSuite);
                Assert.Equal(Protocol, clientStream.SslProtocol);
            }

            public void Failed()
            {
                Assert.NotNull(_failure);
            }

            public void Succeeded()
            {
                if (!HasSucceeded)
                {
                    // for better error message we throw
                    throw _failure;
                }
            }

            public void CheckCipherSuite(TlsCipherSuite expectedCipherSuite)
            {
                Assert.Equal(expectedCipherSuite, CipherSuite);
            }

            public override string ToString()
            {
                // Only for debugging

                if (HasSucceeded)
                {
                    return $"[{Protocol}, {CipherSuite}]";
                }
                else
                {
                    return $"[Failed: {_failure.ToString()}]";
                }
            }
        }
    }
}
