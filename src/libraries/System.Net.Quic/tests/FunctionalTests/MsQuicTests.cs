// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    [ConditionalClass(typeof(MsQuicTests), nameof(IsMsQuicSupported))]
    public class MsQuicTests : MsQuicTestBase
    {
        public static bool IsMsQuicSupported => QuicImplementationProviders.MsQuic.IsSupported;

        private static ReadOnlyMemory<byte> s_data = Encoding.UTF8.GetBytes("Hello world!");

        [Fact]
        public async Task UnidirectionalAndBidirectionalStreamCountsWork()
        {
            using QuicListener listener = CreateQuicListener();
            using QuicConnection clientConnection = CreateQuicConnection(listener.ListenEndPoint);

            ValueTask clientTask = clientConnection.ConnectAsync();
            using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
            await clientTask;
            Assert.Equal(100, serverConnection.GetRemoteAvailableBidirectionalStreamCount());
            Assert.Equal(100, serverConnection.GetRemoteAvailableUnidirectionalStreamCount());
        }

        [Fact]
        public async Task UnidirectionalAndBidirectionalChangeValues()
        {
            using QuicListener listener = CreateQuicListener();

            QuicClientConnectionOptions options = new QuicClientConnectionOptions()
            {
                MaxBidirectionalStreams = 10,
                MaxUnidirectionalStreams = 20,
                RemoteEndPoint = listener.ListenEndPoint,
                ClientAuthenticationOptions = GetSslClientAuthenticationOptions()
            };

            using QuicConnection clientConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);

            ValueTask clientTask = clientConnection.ConnectAsync();
            using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
            await clientTask;
            Assert.Equal(100, clientConnection.GetRemoteAvailableBidirectionalStreamCount());
            Assert.Equal(100, clientConnection.GetRemoteAvailableUnidirectionalStreamCount());
            Assert.Equal(10, serverConnection.GetRemoteAvailableBidirectionalStreamCount());
            Assert.Equal(20, serverConnection.GetRemoteAvailableUnidirectionalStreamCount());
        }

        [Fact]
        [OuterLoop("May take several seconds")]
        public async Task SetListenerTimeoutWorksWithSmallTimeout()
        {
            var quicOptions = new QuicListenerOptions();
            quicOptions.IdleTimeout = TimeSpan.FromSeconds(10);
            quicOptions.ServerAuthenticationOptions = GetSslServerAuthenticationOptions();
            quicOptions.ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0);

            using QuicListener listener = new QuicListener(QuicImplementationProviders.MsQuic, quicOptions);

            QuicClientConnectionOptions options = new QuicClientConnectionOptions()
            {
                RemoteEndPoint = listener.ListenEndPoint,
                ClientAuthenticationOptions = GetSslClientAuthenticationOptions(),
            };

            using QuicConnection clientConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            ValueTask clientTask = clientConnection.ConnectAsync();
            using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
            await clientTask;

            await Assert.ThrowsAsync<QuicOperationAbortedException>(async () => await serverConnection.AcceptStreamAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(100)));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
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

        // will induce failure (byte mixing) in QuicStreamTests_MsQuicProvider.LargeDataSentAndReceived if run in parallel with it
        [Fact]
        public async Task CallDifferentWriteMethodsWorks()
        {
            using QuicListener listener = CreateQuicListener();
            using QuicConnection clientConnection = CreateQuicConnection(listener.ListenEndPoint);

            ValueTask clientTask = clientConnection.ConnectAsync();
            using QuicConnection serverConnection = await listener.AcceptConnectionAsync();
            await clientTask;

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
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49157")]
        public async Task CloseAsync_ByServer_AcceptThrows()
        {
            await RunClientServer(
                clientConnection =>
                {
                    return Task.CompletedTask;
                },
                async serverConnection =>
                {
                    var acceptTask = serverConnection.AcceptStreamAsync();
                    await serverConnection.CloseAsync(errorCode: 0);
                    // make sure 
                    await Assert.ThrowsAsync<QuicOperationAbortedException>(() => acceptTask.AsTask());
                });
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
    }
}
