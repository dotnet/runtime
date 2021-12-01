// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// SslStream stress scenario
//
// * Client sends sequences of random data, accompanied with length and checksum information.
// * Server echoes back the same data. Both client and server validate integrity of received data.
// * Data is written using randomized combinations of the SslStream.Write* methods.
// * Data is ingested using System.IO.Pipelines.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SslStress.Utils;

namespace SslStress
{
    public struct DataSegment
    {
        private byte[] _buffer;

        public DataSegment(int length)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(length);
            Length = length;
        }

        public int Length { get; }
        public Memory<byte> AsMemory() => new Memory<byte>(_buffer, 0, Length);
        public Span<byte> AsSpan() => new Span<byte>(_buffer, 0, Length);

        public ulong Checksum => CRC.CalculateCRC(AsSpan());
        public void Return()
        {
            byte[] toReturn = _buffer;
            _buffer = null;
            ArrayPool<byte>.Shared.Return(toReturn);
        }

        /// Create and populate a segment with random data
        public static DataSegment CreateRandom(Random random, int maxLength)
        {
            int size = random.Next(0, maxLength);
            var chunk = new DataSegment(size);
            foreach (ref byte b in chunk.AsSpan())
            {
                b = s_bytePool[random.Next(255)];
            }

            return chunk;
        }

        private static readonly byte[] s_bytePool =
            Enumerable
                .Range(0, 256)
                .Select(i => (byte)i)
                .Where(b => b != (byte)'\n')
                .ToArray();
    }

    public class DataMismatchException : Exception
    {
        public DataMismatchException(string message) : base(message) { }
    }

    // Serializes data segment using the following format: <length>,<checksum>,<data>
    public class DataSegmentSerializer
    {
        private static readonly Encoding s_encoding = Encoding.ASCII;

        private readonly byte[] _buffer = new byte[32];
        private readonly char[] _charBuffer = new char[32];

        public async Task SerializeAsync(Stream stream, DataSegment segment, Random? random = null, CancellationToken token = default)
        {
            // length
            int numsize = s_encoding.GetBytes(segment.Length.ToString(), _buffer);
            await stream.WriteAsync(_buffer.AsMemory(0, numsize), token);
            stream.WriteByte((byte)',');
            // checksum
            numsize = s_encoding.GetBytes(segment.Checksum.ToString(), _buffer);
            await stream.WriteAsync(_buffer.AsMemory(0, numsize), token);
            stream.WriteByte((byte)',');
            // payload
            Memory<byte> source = segment.AsMemory();
            // write the entire segment outright if not given random instance
            if (random == null)
            {
                await stream.WriteAsync(source, token);
                return;
            }
            // randomize chunking otherwise
            while (source.Length > 0)
            {
                if (random.NextBoolean(probability: 0.05))
                {
                    stream.WriteByte(source.Span[0]);
                    source = source.Slice(1);
                }
                else
                {
                    // TODO consider non-uniform distribution for chunk sizes
                    int chunkSize = random.Next(source.Length);
                    Memory<byte> chunk = source.Slice(0, chunkSize);
                    source = source.Slice(chunkSize);

                    if (random.NextBoolean(probability: 0.9))
                    {
                        await stream.WriteAsync(chunk, token);
                    }
                    else
                    {
                        stream.Write(chunk.Span);
                    }
                }

                if (random.NextBoolean(probability: 0.3))
                {
                    await stream.FlushAsync(token);
                }

                // randomized delay
                if (random.NextBoolean(probability: 0.05))
                {
                    if (random.NextBoolean(probability: 0.7))
                    {
                        await Task.Delay(random.Next(60));
                    }
                    else
                    {
                        Thread.SpinWait(random.Next(1000));
                    }
                }
            }
        }

        public DataSegment Deserialize(ReadOnlySequence<byte> buffer)
        {
            // length
            SequencePosition? pos = buffer.PositionOf((byte)',');
            if (pos == null)
            {
                throw new DataMismatchException("should contain comma-separated values");
            }

            ReadOnlySequence<byte> lengthBytes = buffer.Slice(0, pos.Value);
            int numSize = s_encoding.GetChars(lengthBytes.ToArray(), _charBuffer);
            int length = int.Parse(_charBuffer.AsSpan(0, numSize));
            buffer = buffer.Slice(buffer.GetPosition(1, pos.Value));

            // checksum
            pos = buffer.PositionOf((byte)',');
            if (pos == null)
            {
                throw new DataMismatchException("should contain comma-separated values");
            }

            ReadOnlySequence<byte> checksumBytes = buffer.Slice(0, pos.Value);
            numSize = s_encoding.GetChars(checksumBytes.ToArray(), _charBuffer);
            ulong checksum = ulong.Parse(_charBuffer.AsSpan(0, numSize));
            buffer = buffer.Slice(buffer.GetPosition(1, pos.Value));

            // payload
            if (length != (int)buffer.Length)
            {
                throw new DataMismatchException("declared length does not match payload length");
            }

            var chunk = new DataSegment((int)buffer.Length);
            buffer.CopyTo(chunk.AsSpan());

            if (checksum != chunk.Checksum)
            {
                chunk.Return();
                throw new DataMismatchException("declared checksum doesn't match payload checksum");
            }

            return chunk;
        }
    }

    // Client implementation:
    //
    // Sends randomly generated data segments and validates data echoed back by the server.
    // Applies backpressure if the difference between sent and received segments is too large.
    public sealed class StressClient : SslClientBase
    {
        public StressClient(Configuration config) : base(config) { }

        protected override async Task HandleConnection(int workerId, long jobId, SslStream stream, TcpClient client, Random random, TimeSpan duration, CancellationToken token)
        {
            // token used for signalling cooperative cancellation; do not pass this to SslStream methods
            using var connectionLifetimeToken = new CancellationTokenSource(duration);

            long messagesInFlight = 0;
            DateTime lastWrite = DateTime.Now;
            DateTime lastRead = DateTime.Now;

            await StressTaskExtensions.WhenAllThrowOnFirstException(token, Sender, Receiver, Monitor);

            async Task Sender(CancellationToken token)
            {
                var serializer = new DataSegmentSerializer();

                while (!token.IsCancellationRequested && !connectionLifetimeToken.IsCancellationRequested)
                {
                    await ApplyBackpressure();

                    DataSegment chunk = DataSegment.CreateRandom(random, _config.MaxBufferLength);
                    try
                    {
                        await serializer.SerializeAsync(stream, chunk, random, token);
                        stream.WriteByte((byte)'\n');
                        await stream.FlushAsync(token);
                        Interlocked.Increment(ref messagesInFlight);
                        lastWrite = DateTime.Now;
                    }
                    finally
                    {
                        chunk.Return();
                    }
                }

                // write an empty line to signal completion to the server
                stream.WriteByte((byte)'\n');
                await stream.FlushAsync(token);

                /// Polls until number of in-flight messages falls below threshold
                async Task ApplyBackpressure()
                {
                    if (Volatile.Read(ref messagesInFlight) > 5000)
                    {
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        bool isLogged = false;

                        while (!token.IsCancellationRequested && !connectionLifetimeToken.IsCancellationRequested && Volatile.Read(ref messagesInFlight) > 2000)
                        {
                            // only log if tx has been suspended for a while
                            if (!isLogged && stopwatch.ElapsedMilliseconds >= 1000)
                            {
                                isLogged = true;
                                lock (Console.Out)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"worker #{workerId}: applying backpressure");
                                    Console.WriteLine();
                                    Console.ResetColor();
                                }
                            }

                            await Task.Delay(20);
                        }

                        if(isLogged)
                        {
                            Console.WriteLine($"worker #{workerId}: resumed tx after {stopwatch.Elapsed}");
                        }
                    }
                }
            }

            async Task Receiver(CancellationToken token)
            {
                var serializer = new DataSegmentSerializer();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                await stream.ReadLinesUsingPipesAsync(Callback, cts.Token, separator: '\n');

                Task Callback(ReadOnlySequence<byte> buffer)
                {
                    if (buffer.Length == 0 && connectionLifetimeToken.IsCancellationRequested)
                    {
                        // server echoed back empty buffer sent by client,
                        // signal cancellation and complete the connection.
                        cts.Cancel();
                        return Task.CompletedTask;
                    }

                    // deserialize to validate the checksum, then discard
                    DataSegment chunk = serializer.Deserialize(buffer);
                    chunk.Return();
                    Interlocked.Decrement(ref messagesInFlight);
                    lastRead = DateTime.Now;
                    return Task.CompletedTask;
                }
            }

            async Task Monitor(CancellationToken token)
            {
                do
                {
                    await Task.Delay(500);

                    if((DateTime.Now - lastWrite) >= TimeSpan.FromSeconds(10))
                    {
                        throw new Exception($"worker #{workerId} job #{jobId} has stopped writing bytes to server");
                    }

                    if((DateTime.Now - lastRead) >= TimeSpan.FromSeconds(10))
                    {
                        throw new Exception($"worker #{workerId} job #{jobId} has stopped receiving bytes from server");
                    }
                }
                while(!token.IsCancellationRequested && !connectionLifetimeToken.IsCancellationRequested);
            }
        }
    }

    // Server implementation:
    //
    // Sets up a pipeline reader which validates checksums and echoes back data.
    public sealed class StressServer : SslServerBase
    {
        public StressServer(Configuration config) : base(config) { }

        protected override async Task HandleConnection(SslStream sslStream, TcpClient client, CancellationToken token)
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            DateTime lastReadTime = DateTime.Now;

            var serializer = new DataSegmentSerializer();

            _ = Task.Run(Monitor);
            await sslStream.ReadLinesUsingPipesAsync(Callback, cts.Token, separator: '\n');

            async Task Callback(ReadOnlySequence<byte> buffer)
            {
                lastReadTime = DateTime.Now;

                if (buffer.Length == 0)
                {
                    // got an empty line, client is closing the connection
                    // echo back the empty line and tear down.
                    sslStream.WriteByte((byte)'\n');
                    await sslStream.FlushAsync(token);
                    cts.Cancel();
                    return;
                }

                DataSegment? chunk = null;
                try
                {
                    chunk = serializer.Deserialize(buffer);
                    await serializer.SerializeAsync(sslStream, chunk.Value, token: token);
                    sslStream.WriteByte((byte)'\n');
                    await sslStream.FlushAsync(token);
                }
                catch (DataMismatchException e)
                {
                    if (_config.LogServer)
                    {
                        lock (Console.Out)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Server: {e.Message}");
                            Console.ResetColor();
                        }
                    }
                }
                finally
                {
                    chunk?.Return();
                }
            }

            async Task Monitor()
            {
                do
                {
                    await Task.Delay(1000);

                    if (DateTime.Now - lastReadTime >= TimeSpan.FromSeconds(10))
                    {
                        cts.Cancel();
                    }

                } while (!cts.IsCancellationRequested);
            }
        }
    }
}
