// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(IsSupported))]
    [Collection("NoParallelTests")]
    public class MsQuicTests : QuicTestBase<MsQuicProviderFactory>
    {
        private static ReadOnlyMemory<byte> s_data = Encoding.UTF8.GetBytes("Hello world!");

        public MsQuicTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task UnidirectionalAndBidirectionalStreamCountsWork()
        {
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();

            Assert.Equal(100, serverConnection.GetRemoteAvailableBidirectionalStreamCount());
            Assert.Equal(100, serverConnection.GetRemoteAvailableUnidirectionalStreamCount());
            serverConnection.Dispose();
            clientConnection.Dispose();
        }

        [Fact]
        public async Task UnidirectionalAndBidirectionalChangeValues()
        {
            QuicClientConnectionOptions listenerOptions = new QuicClientConnectionOptions()
            {
                MaxBidirectionalStreams = 10,
                MaxUnidirectionalStreams = 20,
                ClientAuthenticationOptions = GetSslClientAuthenticationOptions()
            };

            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(listenerOptions);
            Assert.Equal(100, clientConnection.GetRemoteAvailableBidirectionalStreamCount());
            Assert.Equal(100, clientConnection.GetRemoteAvailableUnidirectionalStreamCount());
            Assert.Equal(10, serverConnection.GetRemoteAvailableBidirectionalStreamCount());
            Assert.Equal(20, serverConnection.GetRemoteAvailableUnidirectionalStreamCount());
            serverConnection.Dispose();
            clientConnection.Dispose();
        }

        [Fact]
        public async Task ConnectWithCertificateChain()
        {
            (X509Certificate2 certificate, X509Certificate2Collection chain) = System.Net.Security.Tests.TestHelper.GenerateCertificates("localhost", longChain: true);
            X509Certificate2 rootCA = chain[chain.Count - 1];

            var listenerOptions = new QuicListenerOptions();
            listenerOptions.ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            listenerOptions.ServerAuthenticationOptions = GetSslServerAuthenticationOptions();
            listenerOptions.ServerAuthenticationOptions.ServerCertificateContext = SslStreamCertificateContext.Create(certificate, chain);
            listenerOptions.ServerAuthenticationOptions.ServerCertificate = null;

            QuicClientConnectionOptions clientOptions = CreateQuicClientOptions();
            clientOptions.ClientAuthenticationOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                Assert.Equal(certificate.Subject, cert.Subject);
                Assert.Equal(certificate.Issuer, cert.Issuer);
                // We should get full chain without root CA.
                // With trusted root, we should be able to build chain.
                chain.ChainPolicy.CustomTrustStore.Add(rootCA);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                bool ret = chain.Build(certificate);
                if (!ret)
                {
                    _output.WriteLine("Chain build failed with {0} elements", chain.ChainElements);
                    foreach (X509ChainElement element in chain.ChainElements)
                    {
                        _output.WriteLine("Element subject {0} and issuer {1}", element.Certificate.Subject, element.Certificate.Issuer);
                        _output.WriteLine("Element status len {0}", element.ChainElementStatus.Length);
                        foreach (X509ChainStatus status in element.ChainElementStatus)
                        {
                            _output.WriteLine($"Status:  {status.Status}: {status.StatusInformation}");
                        }
                    }
                }

                return ret;
            };

            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(clientOptions, listenerOptions);
            Assert.Equal(certificate, clientConnection.RemoteCertificate);
            Assert.Null(serverConnection.RemoteCertificate);
            serverConnection.Dispose();
            clientConnection.Dispose();
        }

        [Fact]
        public async Task CertificateCallbackThrowPropagates()
        {
            using CancellationTokenSource cts = new CancellationTokenSource(PassingTestTimeout);
            X509Certificate? receivedCertificate = null;

            var listenerOptions = new QuicListenerOptions();
            listenerOptions.ListenEndPoint = new IPEndPoint(Socket.OSSupportsIPv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback, 0);
            listenerOptions.ServerAuthenticationOptions = GetSslServerAuthenticationOptions();
            using QuicListener listener = new QuicListener(QuicImplementationProviders.MsQuic, listenerOptions);

            QuicClientConnectionOptions clientOptions = CreateQuicClientOptions();
            clientOptions.RemoteEndPoint = listener.ListenEndPoint;
            clientOptions.ClientAuthenticationOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                receivedCertificate = cert;
                throw new ArithmeticException("foobar");
            };

            clientOptions.ClientAuthenticationOptions.TargetHost = "foobar1";
            QuicConnection clientConnection = new QuicConnection(QuicImplementationProviders.MsQuic, clientOptions);

            Task<QuicConnection> serverTask = listener.AcceptConnectionAsync(cts.Token).AsTask();
            await Assert.ThrowsAsync<ArithmeticException>(() => clientConnection.ConnectAsync(cts.Token).AsTask());
            QuicConnection serverConnection = await serverTask;

            Assert.Equal(listenerOptions.ServerAuthenticationOptions.ServerCertificate, receivedCertificate);

            clientConnection.Dispose();
            serverConnection.Dispose();
        }

        [Fact]
        public async Task ConnectWithCertificateCallback()
        {
            X509Certificate2 c1 = System.Net.Test.Common.Configuration.Certificates.GetServerCertificate();
            X509Certificate2 c2 = System.Net.Test.Common.Configuration.Certificates.GetClientCertificate(); // This 'wrong' certificate but should be sufficient
            X509Certificate2 expectedCertificate = c1;

            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(PassingTestTimeout);
            string? receivedHostName = null;
            X509Certificate? receivedCertificate = null;

            var listenerOptions = new QuicListenerOptions();
            listenerOptions.ListenEndPoint = new IPEndPoint(Socket.OSSupportsIPv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback, 0);
            listenerOptions.ServerAuthenticationOptions = GetSslServerAuthenticationOptions();
            listenerOptions.ServerAuthenticationOptions.ServerCertificate = null;
            listenerOptions.ServerAuthenticationOptions.ServerCertificateSelectionCallback = (sender, hostName) =>
            {
                receivedHostName = hostName;
                if (hostName == "foobar1")
                {
                    return c1;
                }
                else if (hostName == "foobar2")
                {
                    return c2;
                }

                return null;
            };

            using QuicListener listener = new QuicListener(QuicImplementationProviders.MsQuic, listenerOptions);
            QuicClientConnectionOptions clientOptions = CreateQuicClientOptions();
            clientOptions.ClientAuthenticationOptions.TargetHost = "foobar1";
            clientOptions.ClientAuthenticationOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                receivedCertificate = cert;
                return true;
            };

            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(clientOptions, listener);
            Assert.Equal(clientOptions.ClientAuthenticationOptions.TargetHost, receivedHostName);
            Assert.Equal(c1, receivedCertificate);
            clientConnection.Dispose();
            serverConnection.Dispose();

            // This should fail when callback return null.
            clientOptions.ClientAuthenticationOptions.TargetHost = "foobar3";
            clientConnection = new QuicConnection(QuicImplementationProviders.MsQuic, clientOptions);
            Task clientTask = clientConnection.ConnectAsync(cts.Token).AsTask();

            await Assert.ThrowsAsync<QuicException>(() => clientTask);
            Assert.Equal(clientOptions.ClientAuthenticationOptions.TargetHost, receivedHostName);
            clientConnection.Dispose();

            // Do this last to make sure Listener is still functional.
            clientOptions.ClientAuthenticationOptions.TargetHost = "foobar2";
            expectedCertificate = c2;

            (clientConnection, serverConnection) = await CreateConnectedQuicConnection(clientOptions, listener);
            Assert.Equal(clientOptions.ClientAuthenticationOptions.TargetHost, receivedHostName);
            Assert.Equal(c2, receivedCertificate);
            clientConnection.Dispose();
            serverConnection.Dispose();
        }

        [Fact]
        public async Task ConnectWithCertificateForDifferentName_Throws()
        {
            (X509Certificate2 certificate, _) = System.Net.Security.Tests.TestHelper.GenerateCertificates("localhost");

            var quicOptions = new QuicListenerOptions();
            quicOptions.ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            quicOptions.ServerAuthenticationOptions = GetSslServerAuthenticationOptions();
            quicOptions.ServerAuthenticationOptions.ServerCertificate = certificate;
            using QuicListener listener = new QuicListener(QuicImplementationProviders.MsQuic, quicOptions);

            QuicClientConnectionOptions clientOptions = CreateQuicClientOptions();
            clientOptions.RemoteEndPoint = listener.ListenEndPoint;
            // Use different target host on purpose to get RemoteCertificateNameMismatch ssl error.
            clientOptions.ClientAuthenticationOptions.TargetHost = "loopback";
            clientOptions.ClientAuthenticationOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                Assert.Equal(certificate.Subject, cert.Subject);
                Assert.Equal(certificate.Issuer, cert.Issuer);
                Assert.Equal(SslPolicyErrors.RemoteCertificateNameMismatch, errors & SslPolicyErrors.RemoteCertificateNameMismatch);
                return SslPolicyErrors.None == errors;
            };

            using QuicConnection clientConnection = new QuicConnection(QuicImplementationProviders.MsQuic, clientOptions);
            ValueTask clientTask = clientConnection.ConnectAsync();

            using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
            await Assert.ThrowsAsync<AuthenticationException>(async () => await clientTask);
        }

        [Theory]
        [InlineData("127.0.0.1", true)]
        [InlineData("::1", true)]
        [InlineData("127.0.0.1", false)]
        [InlineData("::1", false)]
        public async Task ConnectWithCertificateForLoopbackIP_IndicatesExpectedError(string ipString, bool expectsError)
        {
            var ipAddress = IPAddress.Parse(ipString);
            (X509Certificate2 certificate, _) = System.Net.Security.Tests.TestHelper.GenerateCertificates(expectsError ? "badhost" : "localhost");

            var listenerOptions = new QuicListenerOptions();
            listenerOptions.ListenEndPoint = new IPEndPoint(ipAddress, 0);
            listenerOptions.ServerAuthenticationOptions = GetSslServerAuthenticationOptions();
            listenerOptions.ServerAuthenticationOptions.ServerCertificate = certificate;

            QuicClientConnectionOptions clientOptions = CreateQuicClientOptions();
            clientOptions.ClientAuthenticationOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                Assert.Equal(certificate.Subject, cert.Subject);
                Assert.Equal(certificate.Issuer, cert.Issuer);
                Assert.Equal(expectsError ? SslPolicyErrors.RemoteCertificateNameMismatch : SslPolicyErrors.None, errors & SslPolicyErrors.RemoteCertificateNameMismatch);
                return true;
            };

            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(clientOptions, listenerOptions);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task ConnectWithClientCertificate()
        {
            bool clientCertificateOK = false;

            var listenerOptions = new QuicListenerOptions();
            listenerOptions.ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            listenerOptions.ServerAuthenticationOptions = GetSslServerAuthenticationOptions();
            listenerOptions.ServerAuthenticationOptions.ClientCertificateRequired = true;
            listenerOptions.ServerAuthenticationOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                _output.WriteLine("client certificate {0}", cert);
                Assert.NotNull(cert);
                Assert.Equal(ClientCertificate.Thumbprint, ((X509Certificate2)cert).Thumbprint);

                clientCertificateOK = true;
                return true;
            };

            using QuicListener listener = new QuicListener(QuicImplementationProviders.MsQuic, listenerOptions);
            QuicClientConnectionOptions clientOptions = CreateQuicClientOptions();
            clientOptions.ClientAuthenticationOptions.ClientCertificates = new X509CertificateCollection() { ClientCertificate };
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(clientOptions, listener);

            // Verify functionality of the connections.
            await PingPong(clientConnection, serverConnection);
            // check we completed the client certificate verification.
            Assert.True(clientCertificateOK);
            Assert.Equal(ClientCertificate, serverConnection.RemoteCertificate);

            await serverConnection.CloseAsync(0);
            clientConnection.Dispose();
            serverConnection.Dispose();
        }

        [Fact]
        public async Task WaitForAvailableUnidirectionStreamsAsyncWorks()
        {
            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            listenerOptions.MaxUnidirectionalStreams = 1;
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(null, listenerOptions);

            // No stream opened yet, should return immediately.
            Assert.True(clientConnection.WaitForAvailableUnidirectionalStreamsAsync().IsCompletedSuccessfully);

            // Open one stream, should wait till it closes.
            QuicStream stream = clientConnection.OpenUnidirectionalStream();
            ValueTask waitTask = clientConnection.WaitForAvailableUnidirectionalStreamsAsync();
            Assert.False(waitTask.IsCompleted);
            Assert.Throws<QuicException>(() => clientConnection.OpenUnidirectionalStream());
            // Close the streams, the waitTask should finish as a result.
            stream.Dispose();
            QuicStream newStream = await serverConnection.AcceptStreamAsync();
            newStream.Dispose();

            await waitTask.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            clientConnection.Dispose();
            serverConnection.Dispose();
        }

        [Fact]
        public async Task WaitForAvailableBidirectionStreamsAsyncWorks()
        {
            QuicListenerOptions listenerOptions = CreateQuicListenerOptions();
            listenerOptions.MaxBidirectionalStreams = 1;
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(null, listenerOptions);

            // No stream opened yet, should return immediately.
            Assert.True(clientConnection.WaitForAvailableBidirectionalStreamsAsync().IsCompletedSuccessfully);

            // Open one stream, should wait till it closes.
            QuicStream stream = clientConnection.OpenBidirectionalStream();
            ValueTask waitTask = clientConnection.WaitForAvailableBidirectionalStreamsAsync();
            Assert.False(waitTask.IsCompleted);
            Assert.Throws<QuicException>(() => clientConnection.OpenBidirectionalStream());

            // Close the streams, the waitTask should finish as a result.
            stream.Dispose();
            QuicStream newStream = await serverConnection.AcceptStreamAsync();
            newStream.Dispose();
            await waitTask.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            clientConnection.Dispose();
            serverConnection.Dispose();
        }

        [Fact]
        [OuterLoop("May take several seconds")]
        public async Task SetListenerTimeoutWorksWithSmallTimeout()
        {
            var listenerOptions = new QuicListenerOptions();
            listenerOptions.IdleTimeout = TimeSpan.FromSeconds(1);
            listenerOptions.ServerAuthenticationOptions = GetSslServerAuthenticationOptions();
            listenerOptions.ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0);

            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(null, listenerOptions);
            await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await serverConnection.AcceptStreamAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(100)));
            serverConnection.Dispose();
            clientConnection.Dispose();
        }

        [Theory]
        [MemberData(nameof(WriteData))]
        public async Task WriteTests(int[][] writes, WriteType writeType)
        {
            await RunClientServer(
                async clientConnection =>
                {
                    await using QuicStream stream = clientConnection.OpenUnidirectionalStream();

                    foreach (int[] bufferLengths in writes)
                    {
                        switch (writeType)
                        {
                            case WriteType.SingleBuffer:
                                foreach (int bufferLength in bufferLengths)
                                {
                                    await stream.WriteAsync(new byte[bufferLength]);
                                }
                                break;
                            case WriteType.GatheredBuffers:
                                var buffers = bufferLengths
                                    .Select(bufferLength => new ReadOnlyMemory<byte>(new byte[bufferLength]))
                                    .ToArray();
                                await stream.WriteAsync(buffers);
                                break;
                            case WriteType.GatheredSequence:
                                var firstSegment = new BufferSegment(new byte[bufferLengths[0]]);
                                BufferSegment lastSegment = firstSegment;

                                foreach (int bufferLength in bufferLengths.Skip(1))
                                {
                                    lastSegment = lastSegment.Append(new byte[bufferLength]);
                                }

                                var buffer = new ReadOnlySequence<byte>(firstSegment, 0, lastSegment, lastSegment.Memory.Length);
                                await stream.WriteAsync(buffer);
                                break;
                            default:
                                Debug.Fail("Unknown write type.");
                                break;
                        }
                    }

                    stream.Shutdown();
                    await stream.ShutdownCompleted();
                },
                async serverConnection =>
                {
                    await using QuicStream stream = await serverConnection.AcceptStreamAsync();

                    var buffer = new byte[4096];
                    int receivedBytes = 0, totalBytes = 0;

                    while ((receivedBytes = await stream.ReadAsync(buffer)) != 0)
                    {
                        totalBytes += receivedBytes;
                    }

                    int expectedTotalBytes = writes.SelectMany(x => x).Sum();
                    Assert.Equal(expectedTotalBytes, totalBytes);

                    stream.Shutdown();
                    await stream.ShutdownCompleted();
                });
        }

        public static IEnumerable<object[]> WriteData()
        {
            var bufferSizes = new[] { 1, 502, 15_003, 1_000_004 };
            var r = new Random();

            return
                from bufferCount in new[] { 1, 2, 3, 10 }
                from writeType in Enum.GetValues<WriteType>()
                let writes =
                    Enumerable.Range(0, 5)
                    .Select(_ =>
                        Enumerable.Range(0, bufferCount)
                        .Select(_ => bufferSizes[r.Next(bufferSizes.Length)])
                        .ToArray())
                    .ToArray()
                select new object[] { writes, writeType };
        }

        public enum WriteType
        {
            SingleBuffer,
            GatheredBuffers,
            GatheredSequence
        }

        [Fact]
        public async Task CallDifferentWriteMethodsWorks()
        {
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();

            ReadOnlyMemory<byte> helloWorld = Encoding.ASCII.GetBytes("Hello world!");
            ReadOnlySequence<byte> ros = CreateReadOnlySequenceFromBytes(helloWorld.ToArray());

            Assert.False(ros.IsSingleSegment);
            using QuicStream clientStream = clientConnection.OpenBidirectionalStream();
            ValueTask writeTask = clientStream.WriteAsync(ros);
            using QuicStream serverStream = await serverConnection.AcceptStreamAsync();

            await writeTask;
            byte[] memory = new byte[24];
            int res = await serverStream.ReadAsync(memory);
            Assert.Equal(12, res);
            ReadOnlyMemory<ReadOnlyMemory<byte>> romrom = new ReadOnlyMemory<ReadOnlyMemory<byte>>(new ReadOnlyMemory<byte>[] { helloWorld, helloWorld });

            await clientStream.WriteAsync(romrom);

            res = await serverStream.ReadAsync(memory);
            Assert.Equal(24, res);
            clientConnection.Dispose();
            serverConnection.Dispose();
        }

        [Fact]
        public async Task CloseAsync_ByServer_AcceptThrows()
        {
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();

            using (clientConnection)
            using (serverConnection)
            {
                var acceptTask = serverConnection.AcceptStreamAsync();
                await serverConnection.CloseAsync(errorCode: 0);
                // make sure we throw
                await Assert.ThrowsAsync<QuicOperationAbortedException>(() => acceptTask.AsTask());
            }
        }

        internal static ReadOnlySequence<byte> CreateReadOnlySequenceFromBytes(byte[] data)
        {
            List<byte[]> segments = new List<byte[]>
            {
                Array.Empty<byte>()
            };

            foreach (var b in data)
            {
                segments.Add(new[] { b });
                segments.Add(Array.Empty<byte>());
            }

            return CreateSegments(segments.ToArray());
        }

        private static ReadOnlySequence<byte> CreateSegments(params byte[][] inputs)
        {
            if (inputs == null || inputs.Length == 0)
            {
                throw new InvalidOperationException();
            }

            int i = 0;

            BufferSegment last = null;
            BufferSegment first = null;

            do
            {
                byte[] s = inputs[i];
                int length = s.Length;
                int dataOffset = length;
                var chars = new byte[length * 2];

                for (int j = 0; j < length; j++)
                {
                    chars[dataOffset + j] = s[j];
                }

                // Create a segment that has offset relative to the OwnedMemory and OwnedMemory itself has offset relative to array
                var memory = new Memory<byte>(chars).Slice(length, length);

                if (first == null)
                {
                    first = new BufferSegment(memory);
                    last = first;
                }
                else
                {
                    last = last.Append(memory);
                }
                i++;
            } while (i < inputs.Length);

            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        internal class BufferSegment : ReadOnlySequenceSegment<byte>
        {
            public BufferSegment(ReadOnlyMemory<byte> memory)
            {
                Memory = memory;
            }

            public BufferSegment Append(ReadOnlyMemory<byte> memory)
            {
                var segment = new BufferSegment(memory)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };
                Next = segment;
                return segment;
            }
        }

        [Fact]
        [OuterLoop("May take several seconds")]
        public async Task ByteMixingOrNativeAVE_MinimalFailingTest()
        {
            const int writeSize = 64 * 1024;
            const int NumberOfWrites = 512;
            byte[] data1 = new byte[writeSize * NumberOfWrites];
            byte[] data2 = new byte[writeSize * NumberOfWrites];
            Array.Fill(data1, (byte)1);
            Array.Fill(data2, (byte)2);

            Task t1 = RunTest(data1);
            Task t2 = RunTest(data2);

            async Task RunTest(byte[] data)
            {
                await RunClientServer(
                    iterations: 20,
                    serverFunction: async connection =>
                    {
                        await using QuicStream stream = await connection.AcceptStreamAsync();

                        byte[] buffer = new byte[data.Length];
                        int bytesRead = await ReadAll(stream, buffer);
                        Assert.Equal(data.Length, bytesRead);
                        AssertExtensions.SequenceEqual(data, buffer);

                        for (int pos = 0; pos < data.Length; pos += writeSize)
                        {
                            await stream.WriteAsync(data[pos..(pos + writeSize)]);
                        }
                        await stream.WriteAsync(Memory<byte>.Empty, endStream: true);

                        await stream.ShutdownCompleted();
                    },
                    clientFunction: async connection =>
                    {
                        await using QuicStream stream = connection.OpenBidirectionalStream();

                        for (int pos = 0; pos < data.Length; pos += writeSize)
                        {
                            await stream.WriteAsync(data[pos..(pos + writeSize)]);
                        }
                        await stream.WriteAsync(Memory<byte>.Empty, endStream: true);

                        byte[] buffer = new byte[data.Length];
                        int bytesRead = await ReadAll(stream, buffer);
                        Assert.Equal(data.Length, bytesRead);
                        AssertExtensions.SequenceEqual(data, buffer);

                        await stream.ShutdownCompleted();
                    }
                );
            }

            await (new[] { t1, t2 }).WhenAllOrAnyFailed(millisecondsTimeout: 1000000);
        }

        [Fact]
        public async Task ManagedAVE_MinimalFailingTest()
        {
            async Task GetStreamIdWithoutStartWorks()
            {
                (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();

                using QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                Assert.Equal(0, clientStream.StreamId);

                // TODO: stream that is opened by client but left unaccepted by server may cause AccessViolationException in its Finalizer
                clientConnection.Dispose();
                serverConnection.Dispose();
            }

            await GetStreamIdWithoutStartWorks().WaitAsync(TimeSpan.FromSeconds(15));

            GC.Collect();
        }

        [Fact]
        public async Task DisposingConnection_OK()
        {
            async Task GetStreamIdWithoutStartWorks()
            {
                (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();

                using QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                Assert.Equal(0, clientStream.StreamId);

                // Dispose all connections before the streams;
                clientConnection.Dispose();
                serverConnection.Dispose();
            }

            await GetStreamIdWithoutStartWorks();

            GC.Collect();
        }

        [Fact]
        public async Task Read_ConnectionAbortedByPeer_Throws()
        {
            const int ExpectedErrorCode = 1234;

            await Task.Run(async () =>
            {
                (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();

                await using QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                await clientStream.WriteAsync(new byte[1]);

                await using QuicStream serverStream = await serverConnection.AcceptStreamAsync();
                await serverStream.ReadAsync(new byte[1]);

                await clientConnection.CloseAsync(ExpectedErrorCode);

                byte[] buffer = new byte[100];
                QuicConnectionAbortedException ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(() => serverStream.ReadAsync(buffer).AsTask());
                Assert.Equal(ExpectedErrorCode, ex.ErrorCode);
            }).WaitAsync(TimeSpan.FromMilliseconds(PassingTestTimeoutMilliseconds));
        }

        [Fact]
        public async Task Read_ConnectionAbortedByUser_Throws()
        {
            await Task.Run(async () =>
            {
                (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();

                await using QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                await clientStream.WriteAsync(new byte[1]);

                await using QuicStream serverStream = await serverConnection.AcceptStreamAsync();
                await serverStream.ReadAsync(new byte[1]);

                await serverConnection.CloseAsync(0);

                byte[] buffer = new byte[100];
                await Assert.ThrowsAsync<QuicOperationAbortedException>(() => serverStream.ReadAsync(buffer).AsTask());
            }).WaitAsync(TimeSpan.FromMilliseconds(PassingTestTimeoutMilliseconds));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BigWrite_SmallRead_Success(bool closeWithData)
        {
            const int size = 100;
            (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();
            using (clientConnection)
            using (serverConnection)
            {
                byte[] buffer = new byte[1] { 42 };

                QuicStream clientStream = clientConnection.OpenBidirectionalStream();
                Task<QuicStream> t = serverConnection.AcceptStreamAsync().AsTask();
                await TaskTimeoutExtensions.WhenAllOrAnyFailed(clientStream.WriteAsync(buffer).AsTask(), t, PassingTestTimeoutMilliseconds);
                QuicStream serverStream = t.Result;
                Assert.Equal(1, await serverStream.ReadAsync(buffer));

                // streams are new established and in good shape.
                using (clientStream)
                using (serverStream)
                {
                    byte[] expected = RandomNumberGenerator.GetBytes(size);
                    byte[] actual = new byte[size];

                    // should be small enough to fit.
                    await serverStream.WriteAsync(expected, closeWithData);

                    // Add delay to have chance to receive the 100b block before ReadAsync starts.
                    await Task.Delay(10);
                    int remaining = size;
                    int readLength;
                    while (remaining > 0)
                    {
                        readLength = await clientStream.ReadAsync(new Memory<byte>(actual, size - remaining, 1));
                        Assert.Equal(1, readLength);
                        remaining--;
                    }

                    Assert.Equal(expected, actual);

                    if (!closeWithData)
                    {
                        serverStream.Shutdown();
                    }

                    readLength = await clientStream.ReadAsync(actual);
                    Assert.Equal(0, readLength);

                    Assert.Equal(expected, actual);
                }
            }
        }
    }
}
