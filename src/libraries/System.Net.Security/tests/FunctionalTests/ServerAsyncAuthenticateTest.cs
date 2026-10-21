// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class ServerAsyncAuthenticateTest : IDisposable
    {
        private readonly ITestOutputHelper _log;
        private readonly ITestOutputHelper _logVerbose;
        private readonly X509Certificate2 _serverCertificate;

        public ServerAsyncAuthenticateTest(ITestOutputHelper output)
        {
            _log = output;
            _logVerbose = VerboseTestLogging.GetInstance();
            _serverCertificate = Configuration.Certificates.GetServerCertificate();
        }

        public void Dispose()
        {
            _serverCertificate.Dispose();
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public async Task ServerAsyncAuthenticate_EachSupportedProtocol_Success(SslProtocols protocol)
        {
            await ServerAsyncSslHelper(protocol, protocol);
        }

        [Theory]
        [MemberData(nameof(ProtocolMismatchData))]
        public async Task ServerAsyncAuthenticate_MismatchProtocols_Fails(
            SslProtocols clientProtocol,
            SslProtocols serverProtocol)
        {
            Exception e = await Record.ExceptionAsync(
                () =>
                {
                    return ServerAsyncSslHelper(
                        clientProtocol,
                        serverProtocol,
                        expectedToFail: true);
                });

            Assert.NotNull(e);

            if (OperatingSystem.IsAndroid())
            {
                // On Android running on x64 or x86 the server side sometimes throws IOException instead of AuthenticationException
                Assert.True(e is IOException || e is AuthenticationException, $"Unexpected exception type: {e.GetType()}");
            }
            else
            {
                Assert.IsType<AuthenticationException>(e);
            }
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public async Task ServerAsyncAuthenticate_AllClientVsIndividualServerSupportedProtocols_Success(
            SslProtocols serverProtocol)
        {
            await ServerAsyncSslHelper(SslProtocolSupport.SupportedSslProtocols, serverProtocol);
        }

        [Fact]
        public async Task ServerAsyncAuthenticate_SimpleSniOptions_Success()
        {
            var state = new object();
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate };
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false) };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        Assert.Equal(server, stream);
                        Assert.Equal(clientOptions.TargetHost, clientHelloInfo.ServerName);
                        Assert.True(object.ReferenceEquals(state, userState));
                        return new ValueTask<SslServerAuthenticationOptions>(serverOptions);
                    },
                    state, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }

        [Theory]
        [InlineData(1, int.MaxValue)]
        [InlineData(2, int.MaxValue)]
        [InlineData(3, int.MaxValue)]
        [InlineData(4, int.MaxValue)]
        [InlineData(58, int.MaxValue)]
        [InlineData(128, int.MaxValue)]
        [InlineData(1, 1)]
        [InlineData(58, 1)]
        public Task ServerAsyncAuthenticate_FragmentedClientHello_OptionsCallbackGetsCompleteHello(int fragmentSize, int maxReadSize)
        {
            byte[] hello = TlsFrameHelperTests.FragmentHello(TlsFrameHelperTests.ClientHello, fragmentSize);
            return AssertClientHelloOptions(hello, maxReadSize, "10.211.55.2", SslProtocols.Tls13);
        }

        [Theory]
        [InlineData(58)]
        [InlineData(16 * 1024)]
        public Task ServerAsyncAuthenticate_FragmentedClientHello_LargeHello(int fragmentSize)
        {
            byte[] hello = TlsFrameHelperTests.FragmentHello(TlsFrameHelperTests.CreatePaddedClientHello(20 * 1024), fragmentSize);
            return AssertClientHelloOptions(hello, int.MaxValue, new string('a', 19), SslProtocols.Tls12);
        }

        private static async Task AssertClientHelloOptions(byte[] hello, int maxReadSize, string serverName, SslProtocols protocol)
        {
            using var input = new MemoryStream(hello);
            using var transport = new DelegateDelegatingStream(input)
            {
                ReadAsyncMemoryFunc = (buffer, token) =>
                    input.ReadAsync(buffer.Slice(0, Math.Min(buffer.Length, maxReadSize)), token)
            };
            using var server = new SslStream(transport);
            var expectedException = new InvalidOperationException();
            int callbackCount = 0;

            InvalidOperationException actualException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                server.AuthenticateAsServerAsync((_, info, _, _) =>
                {
                    callbackCount++;
                    Assert.Equal(serverName, info.ServerName);
                    Assert.True((info.SslProtocols & protocol) == protocol);
                    throw expectedException;
                }, null));

            Assert.Same(expectedException, actualException);
            Assert.Equal(1, callbackCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ServerAsyncAuthenticate_FragmentedClientHello_IncompleteDoesNotInvokeCallback(bool cancel)
        {
            const int FirstRecordLength = TlsFrameHelper.HeaderSize + 58;
            byte[] hello = TlsFrameHelperTests.FragmentHello(TlsFrameHelperTests.ClientHello, 58);
            using var input = new MemoryStream(hello.AsSpan(0, FirstRecordLength).ToArray());
            var readRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var transport = new DelegateDelegatingStream(input) { ReadAsyncMemoryFunc = Read };
            using var server = new SslStream(transport);
            using var cts = new CancellationTokenSource(TestConfiguration.PassingTestTimeout);
            bool callbackInvoked = false;
            Task authentication = server.AuthenticateAsServerAsync((_, _, _, _) =>
            {
                callbackInvoked = true;
                throw new InvalidOperationException();
            }, null, cts.Token);

            try
            {
                if (cancel)
                {
                    await Task.WhenAny(readRequested.Task, authentication).WaitAsync(TestConfiguration.PassingTestTimeout);
                    Assert.False(authentication.IsCompleted);
                    cts.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => authentication);
                }
                else
                {
                    await Assert.ThrowsAsync<IOException>(() => authentication);
                }

                Assert.False(callbackInvoked);
            }
            finally
            {
                cts.Cancel();
            }

            async ValueTask<int> Read(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                int read = input.Read(buffer.Span);
                if (read == 0 && !buffer.IsEmpty && cancel)
                {
                    readRequested.TrySetResult();
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }

                return read;
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task ServerAsyncAuthenticate_FragmentedClientHello_InvalidFramingDoesNotInvokeCallback(int invalidKind)
        {
            const int SecondRecordOffset = TlsFrameHelper.HeaderSize + 58;
            byte[] hello = TlsFrameHelperTests.FragmentHello(TlsFrameHelperTests.ClientHello, 58);
            switch (invalidKind)
            {
                case 0:
                    hello[SecondRecordOffset + 3] = 0;
                    hello[SecondRecordOffset + 4] = 0;
                    break;
                case 1:
                    hello[SecondRecordOffset] = (byte)TlsContentType.ChangeCipherSpec;
                    break;
                default:
                    hello[6] = 0xFF;
                    hello[7] = 0xFF;
                    hello[8] = 0xFF;
                    break;
            }

            using var input = new MemoryStream(hello);
            using var server = new SslStream(input);
            bool callbackInvoked = false;
            await Assert.ThrowsAsync<AuthenticationException>(() => server.AuthenticateAsServerAsync((_, _, _, _) =>
            {
                callbackInvoked = true;
                throw new InvalidOperationException();
            }, null));
            Assert.False(callbackInvoked);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public async Task ReceiveHandshakeFrame_Reauthentication_DoesNotParseCiphertext()
        {
            // An encrypted TLS 1.2 handshake record can coincidentally start with ClientHello's type.
            byte[] record = [22, 3, 3, 0, 4, 1, 0xFF, 0xFF, 0xFF];
            using var input = new MemoryStream(record);
            using var server = new SslStream(input);
            const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type streamType = typeof(SslStream);
            object options = streamType.GetField("_sslAuthenticationOptions", InstanceFlags)!.GetValue(server)!;
            options.GetType().GetProperty("IsServer", InstanceFlags)!.SetValue(options, true);
            streamType.GetField("_isReAuthentication", InstanceFlags)!.SetValue(server, true);
            Type adapter = streamType.Assembly.GetType("System.SyncReadWriteAdapter", throwOnError: true)!;
            MethodInfo receive = streamType.GetMethod("ReceiveHandshakeFrameAsync", InstanceFlags)!.MakeGenericMethod(adapter);
            ValueTask<int> receiveTask = Assert.IsType<ValueTask<int>>(receive.Invoke(server, [CancellationToken.None]));

            Assert.Equal(record.Length, await receiveTask);
        }

        public static IEnumerable<object[]> FragmentedClientHelloData()
        {
            foreach (SslProtocols protocol in SslProtocolSupport.EnumerateSupportedProtocols(SslProtocols.Tls12 | SslProtocols.Tls13, true))
            {
                foreach (ServerCertificateSource certificateSource in Enum.GetValues<ServerCertificateSource>())
                {
                    foreach (bool useAsync in new[] { false, true })
                    {
                        if (!useAsync && certificateSource == ServerCertificateSource.OptionsCallback)
                        {
                            continue;
                        }

                        yield return new object[] { protocol, certificateSource, useAsync, 58, int.MaxValue };
                        yield return new object[] { protocol, certificateSource, useAsync, -1, 1 };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(FragmentedClientHelloData))]
        // These backends support managed server-side TLS 1.3 selection.
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux | TestPlatforms.FreeBSD)]
        public async Task ServerAuthenticate_FragmentedClientHello_HandshakeSucceeds(
            SslProtocols protocol, ServerCertificateSource certificateSource, bool useAsync, int fragmentSize, int maxReadSize)
        {
            const string HostName = "fragmented.example";
            (Stream clientTransport, Stream serverTransport) = TestHelper.GetConnectedStreams();
            bool fragmented = false;
            using var clientWrapper = new DelegateDelegatingStream(clientTransport)
            {
                WriteAsyncMemoryFunc = (buffer, token) =>
                {
                    if (!fragmented && !buffer.IsEmpty)
                    {
                        TlsFrameHeader header = default;
                        Assert.True(TlsFrameHelper.TryGetFrameHeader(buffer.Span, ref header));
                        Assert.Equal(buffer.Length, header.Length);
                        int size = fragmentSize;
                        if (size == -1)
                        {
                            int nameOffset = buffer.Span.IndexOf(Encoding.ASCII.GetBytes(HostName));
                            Assert.True(nameOffset >= TlsFrameHelper.HeaderSize);
                            size = nameOffset + 2 - TlsFrameHelper.HeaderSize;
                        }

                        buffer = TlsFrameHelperTests.FragmentHello(buffer.Span, size);
                        fragmented = true;
                    }

                    return clientTransport.WriteAsync(buffer, token);
                }
            };
            using var serverWrapper = new DelegateDelegatingStream(serverTransport)
            {
                ReadSpanFunc = buffer => serverTransport.Read(buffer.Slice(0, Math.Min(buffer.Length, maxReadSize))),
                ReadAsyncMemoryFunc = (buffer, token) =>
                    serverTransport.ReadAsync(buffer.Slice(0, Math.Min(buffer.Length, maxReadSize)), token)
            };
            using var client = new SslStream(clientWrapper);
            using var server = new SslStream(serverWrapper);
            int callbackCount = 0;
            string? observedName = null;
            SslProtocols observedProtocols = SslProtocols.None;
            var serverOptions = new SslServerAuthenticationOptions
            {
                EnabledSslProtocols = protocol,
                AllowTlsResume = false
            };
            if (certificateSource == ServerCertificateSource.SelectionCallback)
            {
                serverOptions.ServerCertificateSelectionCallback = (_, name) =>
                {
                    callbackCount++;
                    observedName = name;
                    return _serverCertificate;
                };
            }
            else
            {
                serverOptions.ServerCertificate = _serverCertificate;
            }

            Task clientTask = client.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = HostName,
                EnabledSslProtocols = protocol,
                AllowTlsResume = false,
                RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate
            });
            Task serverTask;
            if (certificateSource == ServerCertificateSource.OptionsCallback)
            {
                serverTask = server.AuthenticateAsServerAsync((_, info, _, _) =>
                {
                    callbackCount++;
                    observedName = info.ServerName;
                    observedProtocols = info.SslProtocols;
                    return new ValueTask<SslServerAuthenticationOptions>(serverOptions);
                }, null);
            }
            else
            {
                serverTask = useAsync
                    ? server.AuthenticateAsServerAsync(serverOptions)
                    : Task.Run(() => server.AuthenticateAsServer(serverOptions));
            }

            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(clientTask, serverTask);

            Assert.True(fragmented);
            Assert.Equal(HostName, server.TargetHostName);
            Assert.Equal(certificateSource == ServerCertificateSource.Direct ? 0 : 1, callbackCount);
            if (certificateSource != ServerCertificateSource.Direct)
            {
                Assert.Equal(HostName, observedName);
            }
            if (certificateSource == ServerCertificateSource.OptionsCallback)
            {
                Assert.True((observedProtocols & protocol) == protocol);
            }

            await TestHelper.PingPong(client, server);
        }

        [Theory]
        [MemberData(nameof(SupportedProtocolData))]
        public async Task ServerAsyncAuthenticate_SniSetVersion_Success(SslProtocols version)
        {
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate, EnabledSslProtocols = version };
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false), EnabledSslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 };
#pragma warning restore SYSLIB0039
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        Assert.Equal(server, stream);
                        Assert.Equal(clientOptions.TargetHost, clientHelloInfo.ServerName);
                        return new ValueTask<SslServerAuthenticationOptions>(serverOptions);
                    },
                    null, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
                // Verify that the SNI callback can impact version.
                Assert.Equal(version, client.SslProtocol);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls13))]
        public async Task ServerAsyncAuthenticate_SniCallback_ReceivesSupportedVersionsFromExtension()
        {
            // Regression test: ensure that when a ServerOptionDelegate (SNI callback) is used,
            // the supported_versions extension from the ClientHello is parsed and exposed via
            // SslClientHelloInfo.SslProtocols. TLS 1.3 advertises its version only via that
            // extension (the record-layer / legacy_version field still reports TLS 1.2), so
            // without parsing the extension the callback would not see Tls13.
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate, EnabledSslProtocols = SslProtocols.Tls13 };
            var clientOptions = new SslClientAuthenticationOptions()
            {
                TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false),
                EnabledSslProtocols = SslProtocols.Tls13,
            };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            SslProtocols observedProtocols = SslProtocols.None;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        observedProtocols = clientHelloInfo.SslProtocols;
                        return new ValueTask<SslServerAuthenticationOptions>(serverOptions);
                    },
                    null, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }

            Assert.True((observedProtocols & SslProtocols.Tls13) == SslProtocols.Tls13,
                $"Expected SslClientHelloInfo.SslProtocols to include Tls13, got '{observedProtocols}'.");
        }

        private async Task<SslServerAuthenticationOptions> FailedTask()
        {
            await Task.Yield();
            throw new InvalidOperationException("foo");
        }

        private async Task<SslServerAuthenticationOptions> OptionsTask(SslServerAuthenticationOptions value)
        {
            await Task.Yield();
            return value;
        }

        [Fact]
        public async Task ServerAsyncAuthenticate_AsyncOptions_Success()
        {
            var state = new object();
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate };
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false) };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        Assert.Equal(server, stream);
                        Assert.Equal(clientOptions.TargetHost, clientHelloInfo.ServerName);
                        Assert.True(object.ReferenceEquals(state, userState));
                        return new ValueTask<SslServerAuthenticationOptions>(OptionsTask(serverOptions));
                    },
                    state, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ServerAsyncAuthenticate_FailingOptionCallback_Throws(bool useAsync)
        {
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate };
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false) };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        if (useAsync)
                        {
                            return new ValueTask<SslServerAuthenticationOptions>(FailedTask());
                        }

                        throw new InvalidOperationException("foo");
                    },
                    null, CancellationToken.None);
                await Assert.ThrowsAsync<InvalidOperationException>(() => t2);
            }
        }

        [Fact]
        public async Task ServerAsyncAuthenticate_VerificationDelegate_Success()
        {
            bool validationCallbackCalled = false;
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate, ClientCertificateRequired = true, };
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false), AllowTlsResume = false };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            serverOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                validationCallbackCalled = true;
                return true;
            };

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        Assert.Equal(server, stream);
                        Assert.Equal(clientOptions.TargetHost, clientHelloInfo.ServerName);
                        return new ValueTask<SslServerAuthenticationOptions>(OptionsTask(serverOptions));
                    },
                    null, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
                Assert.True(validationCallbackCalled);
            }
        }

        [Fact]
        public async Task ServerAsyncAuthenticate_ConstructorVerificationDelegate_Success()
        {
            bool validationCallbackCalled = false;
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate, ClientCertificateRequired = true, };
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false), AllowTlsResume = false };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            var client = new SslStream(clientStream);
            var server = new SslStream(serverStream, false, (sender, certificate, chain, sslPolicyErrors) => { validationCallbackCalled = true; return true; });

            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        Assert.Equal(server, stream);
                        Assert.Equal(clientOptions.TargetHost, clientHelloInfo.ServerName);
                        return new ValueTask<SslServerAuthenticationOptions>(OptionsTask(serverOptions));
                    },
                    null, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
                Assert.True(validationCallbackCalled);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ServerAsyncAuthenticate_NoCertificate_Throws(bool useAsync)
        {
            var serverOptions = new SslServerAuthenticationOptions();
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false) };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        if (useAsync)
                        {
                            return new ValueTask<SslServerAuthenticationOptions>(serverOptions);
                        }

                        return new ValueTask<SslServerAuthenticationOptions>(OptionsTask(serverOptions));
                    },
                    null, CancellationToken.None);
                await Assert.ThrowsAsync<System.NotSupportedException>(() => t2);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ServerAsyncAuthenticate_InvalidHello_Throws(bool close)
        {
            (NetworkStream client, NetworkStream server) = TestHelper.GetConnectedTcpStreams();
            using (client)
            using (SslStream ssl = new SslStream(server))
            {
                byte[] buffer = new byte[182];
                buffer[0] = 178;
                buffer[1] = 0;
                buffer[2] = 0;
                buffer[3] = 1;
                buffer[4] = 133;
                buffer[5] = 166;

                Task t1 = ssl.AuthenticateAsServerAsync(_serverCertificate, false, false);
                Task t2 = client.WriteAsync(buffer).AsTask();
                if (close)
                {
                    await t2.WaitAsync(TestConfiguration.PassingTestTimeout);
                    client.Socket.Shutdown(SocketShutdown.Send);
                }
                else
                {
                    // Write enough data to full frame size
                    buffer = new byte[13000];
                    t2 = client.WriteAsync(buffer).AsTask();
                    await t2.WaitAsync(TestConfiguration.PassingTestTimeout);
                }

                await Assert.ThrowsAsync<AuthenticationException>(() => t1);
            }
        }

        public enum ServerCertificateSource
        {
            Direct,
            SelectionCallback,
            OptionsCallback
        }

        public static IEnumerable<object[]> EmptyHandshakeRecordData()
        {
            foreach (ServerCertificateSource certificateSource in Enum.GetValues<ServerCertificateSource>())
            {
                foreach (bool useAsync in new[] { false, true })
                {
                    if (!useAsync && certificateSource == ServerCertificateSource.OptionsCallback)
                    {
                        continue;
                    }

                    yield return new object[] { certificateSource, useAsync, int.MaxValue, false };
                    yield return new object[] { certificateSource, useAsync, 1, false };
                    yield return new object[] { certificateSource, useAsync, int.MaxValue, true };
                }
            }
        }

        [Theory]
        [MemberData(nameof(EmptyHandshakeRecordData))]
        public Task ServerAuthenticate_EmptyHandshakeRecord_ThrowsIOException(
            ServerCertificateSource certificateSource, bool useAsync, int maxReadSize, bool trailingData) =>
            AuthenticateEmptyHandshakeRecord(_serverCertificate, certificateSource, useAsync, maxReadSize, trailingData);

        internal static async Task AuthenticateEmptyHandshakeRecord(
            X509Certificate2 certificate, ServerCertificateSource certificateSource, bool useAsync, int maxReadSize, bool trailingData)
        {
            byte[] record = trailingData
                ? [0x16, 0x03, 0x01, 0x00, 0x00, 0x01]
                : [0x16, 0x03, 0x01, 0x00, 0x00];
            using var input = new MemoryStream(record);
            using var transport = new DelegateDelegatingStream(input)
            {
                ReadSpanFunc = Read,
                ReadAsyncMemoryFunc = (buffer, _) => new ValueTask<int>(Read(buffer.Span))
            };
            using var server = new SslStream(transport);
            bool callbackInvoked = false;
            var options = new SslServerAuthenticationOptions
            {
                // Exercise managed framing rather than Apple's Network Framework handshake.
                EnabledSslProtocols = SslProtocols.Tls12
            };
            if (certificateSource == ServerCertificateSource.SelectionCallback)
            {
                options.ServerCertificateSelectionCallback = (_, _) =>
                {
                    callbackInvoked = true;
                    return certificate;
                };
            }
            else
            {
                options.ServerCertificate = certificate;
            }

            if (certificateSource == ServerCertificateSource.OptionsCallback)
            {
                await Assert.ThrowsAsync<IOException>(() => server.AuthenticateAsServerAsync((_, _, _, _) =>
                {
                    callbackInvoked = true;
                    return new ValueTask<SslServerAuthenticationOptions>(options);
                }, null));
            }
            else if (useAsync)
            {
                await Assert.ThrowsAsync<IOException>(() => server.AuthenticateAsServerAsync(options));
            }
            else
            {
                Assert.Throws<IOException>(() => server.AuthenticateAsServer(options));
            }

            Assert.False(callbackInvoked);
            Assert.False(server.IsAuthenticated);

            int Read(Span<byte> buffer)
            {
                // Reject the record without an additional read that could mask the failure with EOF.
                Assert.True(buffer.IsEmpty || input.Position < input.Length);
                return input.Read(buffer.Slice(0, Math.Min(buffer.Length, maxReadSize)));
            }
        }

        [Fact]
        public async Task ServerAsyncAuthenticate_EmptyHandshakeRecordWithoutEof_ThrowsIOException()
        {
            (Stream client, Stream server) = TestHelper.GetConnectedStreams();
            using (client)
            using (var ssl = new SslStream(server))
            {
                await client.WriteAsync(new byte[] { 0x16, 0x03, 0x01, 0x00, 0x00 });
                await Assert.ThrowsAsync<IOException>(() =>
                    ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                    {
                        ServerCertificate = _serverCertificate,
                        EnabledSslProtocols = SslProtocols.Tls12
                    })
                        .WaitAsync(TestConfiguration.PassingTestTimeout));
                Assert.False(ssl.IsAuthenticated);
            }
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
                        yield return new object[] { clientProtocol, serverProtocol };
                    }
                }
            }
        }

        public static IEnumerable<Object[]> SupportedProtocolData()
        {
            if (PlatformDetection.SupportsTls11)
            {
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                yield return new object[] { SslProtocols.Tls11 };
#pragma warning restore SYSLIB0039
            }

            if (PlatformDetection.SupportsTls12)
            {
                yield return new object[] { SslProtocols.Tls12 };
            }
        }
        #region Helpers

        private async Task ServerAsyncSslHelper(
            SslProtocols clientSslProtocols,
            SslProtocols serverSslProtocols,
            bool expectedToFail = false)
        {
            _log.WriteLine(
                "Server: " + serverSslProtocols + "; Client: " + clientSslProtocols +
                " expectedToFail: " + expectedToFail);

            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();

            using (SslStream sslServerStream = new SslStream(
                serverStream,
                false,
                AllowEmptyClientCertificate))
            using (SslStream sslClientStream = new SslStream(
                clientStream,
                false,
                delegate
                {
                    // Allow any certificate from the server.
                    // Note that simply ignoring exceptions from AuthenticateAsClientAsync() is not enough
                    // because in Mono, certificate validation is performed during the handshake and a failure
                    // would result in the connection being terminated before the handshake completed, thus
                    // making the server-side AuthenticateAsServerAsync() fail as well.
                    return true;
                }))
            {
                // Use a different SNI for each connection to prevent TLS 1.3 renegotiation issue: https://github.com/dotnet/runtime/issues/47378
                string serverName = TestHelper.GetTestSNIName(nameof(ServerAsyncSslHelper), clientSslProtocols, serverSslProtocols);

                _log.WriteLine("Connected on {0} {1} ({2} {3})", clientStream.Socket.LocalEndPoint, clientStream.Socket.RemoteEndPoint, clientStream.Socket.Handle, serverStream.Socket.Handle);
                _log.WriteLine("client SslStream#{0} server SslStream#{1}", sslClientStream.GetHashCode(), sslServerStream.GetHashCode());

                _logVerbose.WriteLine("ServerAsyncAuthenticateTest.AuthenticateAsClientAsync start.");
                Task clientAuthentication = sslClientStream.AuthenticateAsClientAsync(
                    serverName,
                    null,
                    clientSslProtocols,
                    false);

                _logVerbose.WriteLine("ServerAsyncAuthenticateTest.AuthenticateAsServerAsync start.");
                Task serverAuthentication = sslServerStream.AuthenticateAsServerAsync(
                    _serverCertificate,
                    true,
                    serverSslProtocols,
                    false);

                try
                {
                    await clientAuthentication.WaitAsync(TestConfiguration.PassingTestTimeout);
                    _logVerbose.WriteLine("ServerAsyncAuthenticateTest.clientAuthentication complete.");
                }
                catch (AuthenticationException ex)
                {
                    // Ignore client-side errors: we're only interested in server-side behavior.
                    _log.WriteLine("Client exception : " + ex);
                    clientStream.Socket.Shutdown(SocketShutdown.Send);
                }

                await serverAuthentication.WaitAsync(TestConfiguration.PassingTestTimeout);
                _logVerbose.WriteLine("ServerAsyncAuthenticateTest.serverAuthentication complete.");

#pragma warning disable SYSLIB0058 // Use NegotiatedCipherSuite.
                _log.WriteLine(
                    "Server({0}) authenticated with encryption cipher: {1} {2}-bit strength",
                    serverStream.Socket.LocalEndPoint,
                    sslServerStream.CipherAlgorithm,
                    sslServerStream.CipherStrength);

                Assert.True(
                    sslServerStream.CipherAlgorithm != CipherAlgorithmType.Null,
                    "Cipher algorithm should not be NULL");

                Assert.True(sslServerStream.CipherStrength > 0, "Cipher strength should be greater than 0");
#pragma warning restore SYSLIB0058 // Use NegotiatedCipherSuite.
            }
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        private bool AllowEmptyClientCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            Assert.True(
                (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.RemoteCertificateNotAvailable,
                "Client didn't supply a cert, the server required one, yet sslPolicyErrors is " + sslPolicyErrors);
            return true;  // allow everything
        }

        #endregion Helpers
    }
}
