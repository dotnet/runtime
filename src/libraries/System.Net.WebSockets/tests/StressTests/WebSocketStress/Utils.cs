// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Channels;

namespace WebSocketStress;

internal static class Utils
{
    public static string OobEndpointPath { get; } = Path.Combine(Directory.GetCurrentDirectory(), "oob_socket");

    public static Random NextRandom(this Random random) => new Random(Seed: random.Next());

    public static UInt128 GetConnectionId(int workerId, ulong jobId) => new UInt128((ulong)workerId, jobId);

    public static (int workerId, ulong jobId) GetWorkerAndJobId(UInt128 connectionId)
    {
        UInt128 jobId = connectionId & (UInt128)ulong.MaxValue;
        UInt128 workerId = connectionId >> 64;
        return ((int)workerId, (ulong)jobId);
    }

    public static bool NextBoolean(this Random random, double probability = 0.5)
    {
        if (probability < 0 || probability > 1)
            throw new ArgumentOutOfRangeException(nameof(probability));

        return random.NextDouble() < probability;
    }

    public static ValueTask WriteAsync(this WebSocket ws, Memory<byte> data, CancellationToken cancellationToken)
    {
        if (data.Length == 0)
        {
            throw new Exception("must not be.");
        }
        return ws.SendAsync(data, WebSocketMessageType.Binary, endOfMessage: false, cancellationToken);
    }

    public static async Task WhenAllThrowOnFirstException(CancellationToken token, params Func<CancellationToken, Task>[] tasks)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        Exception? firstException = null;

        await Task.WhenAll(tasks.Select(RunOne));

        if (firstException != null)
        {
            ExceptionDispatchInfo.Capture(firstException).Throw();
        }

        async Task RunOne(Func<CancellationToken, Task> task)
        {
            try
            {
                await Task.Run(() => task(cts.Token));
            }
            catch (Exception e)
            {
                if (Interlocked.CompareExchange(ref firstException, e, null) == null)
                {
                    cts.Cancel();
                }
            }
        }
    }
}

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
        int size = random.Next(1, maxLength);
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

    public override string ToString()
    {
        StringBuilder bld = new StringBuilder();
        foreach (byte b in AsSpan())
        {
            bld.Append($"{b}|");
        }
        return bld.ToString(); 
    }
}

public class DataMismatchException : Exception
{
    public DataMismatchException(string message) : base(message) { }
}

// Serializes data segment using the following format: <length>,<checksum>,<data>
internal class DataSegmentSerializer
{
    private static readonly Encoding s_encoding = Encoding.ASCII;

    private readonly byte[] _buffer = new byte[32];
    private readonly char[] _charBuffer = new char[32];
    private static readonly byte[] s_comma = [(byte)','];

    private readonly Log _log;

    public DataSegmentSerializer(Log log)
    {
        _log = log;
    }

    public async Task SerializeAsync(WebSocket ws, DataSegment segment, Random? random = null, CancellationToken token = default)
    {
        // length
        int l = s_encoding.GetBytes(segment.Length.ToString(), _buffer);
        await ws.WriteAsync(_buffer.AsMemory(0, l), token);
        await ws.WriteAsync(s_comma, token);
        // checksum
        l = s_encoding.GetBytes(segment.Checksum.ToString(), _buffer);
        await ws.WriteAsync(_buffer.AsMemory(0, l), token);
        await ws.WriteAsync(s_comma, token);
        // payload
        Memory<byte> source = segment.AsMemory();

        // write the entire segment outright if not given random instance
        if (random == null)
        {
            await ws.WriteAsync(source, token);
            return;
        }

        //// randomize chunking otherwise
        while (source.Length > 0)
        {
            int chunkSize = random.Next(source.Length) + 1;
            Memory<byte> chunk = source.Slice(0, chunkSize);
            source = source.Slice(chunkSize);
            await ws.WriteAsync(chunk, token);

            // randomized delay
            //if (random.NextBoolean(probability: 0.05))
            //{
            //    if (random.NextBoolean(probability: 0.7))
            //    {
            //        await Task.Delay(random.Next(60));
            //    }
            //    else
            //    {
            //        Thread.SpinWait(random.Next(1000));
            //    }
            //}
        }

        _log.Verbose($"serialized L={segment.Length}|{source.Length} C={segment.Checksum}");
    }

    public DataSegment Deserialize(ReadOnlySequence<byte> buffer)
    {
        StringBuilder logBld = new StringBuilder();

        try
        {
            logBld.Append($"deserializing original buffer.Length={buffer.Length}");
            // length
            SequencePosition? pos = buffer.PositionOf((byte)',');
            if (pos == null)
            {
                throw new DataMismatchException($"%{_log.ConnectionId} should contain comma-separated values (length)");
            }

            ReadOnlySequence<byte> lengthBytes = buffer.Slice(0, pos.Value);
            int numSize = s_encoding.GetChars(lengthBytes.ToArray(), _charBuffer);
            int length = int.Parse(_charBuffer.AsSpan(0, numSize));
            logBld.Append($", L={length}");

            buffer = buffer.Slice(buffer.GetPosition(1, pos.Value));

            // checksum
            pos = buffer.PositionOf((byte)',');
            if (pos == null)
            {
                throw new DataMismatchException($"%{_log.ConnectionId} should contain comma-separated values (checksum)");
            }

            ReadOnlySequence<byte> checksumBytes = buffer.Slice(0, pos.Value);
            numSize = s_encoding.GetChars(checksumBytes.ToArray(), _charBuffer);
            if (numSize == 0)
            {
                throw new DataMismatchException($"%{_log.ConnectionId} numSize == 0");
            }

            ulong checksum = ulong.Parse(_charBuffer.AsSpan(0, numSize));
            logBld.Append($", C={checksum}");
            buffer = buffer.Slice(buffer.GetPosition(1, pos.Value));

            // payload
            if (length != (int)buffer.Length)
            {
                throw new DataMismatchException($"%{_log.ConnectionId} declared length does not match payload length: length={length}, buffer.Length={buffer.Length}, C={checksum}");
            }

            var chunk = new DataSegment((int)buffer.Length);
            buffer.CopyTo(chunk.AsSpan());

            if (checksum != chunk.Checksum)
            {
                chunk.Return();
                throw new DataMismatchException($"%{_log.ConnectionId} declared checksum doesn't match payload checksum");
            }

            return chunk;
        }
        finally
        {
            _log.Verbose(logBld.ToString());
        }
        
    }
}

internal record class Log(string Type, UInt128 ConnectionId)
{
    private static StreamWriter s_fileWriter;

    private static Channel<string> s_messagesChannel = Channel.CreateUnbounded<string>();

    static Log()
    {
        if (File.Exists("./log.txt")) File.Delete("./log.txt");
        s_fileWriter = new StreamWriter("./log.txt");
        _ = ProcessMessagesAsync();
    }


    public void WriteLine(string s)
    {
        string m = GetMessage(s);
        //Console.WriteLine(m);
        _ = s_messagesChannel.Writer.WriteAsync(m);
    }

    public void Verbose(string s)
    {
        //string m = GetMessage(s);
        //_ = s_messagesChannel.Writer.WriteAsync(m);
    }

    private string GetMessage(object s) => $"[{Type}] %{ConnectionId} {s}";

    public static void Close()
    {
        s_fileWriter.Flush();
        s_fileWriter.Close();
    }

    private static async Task ProcessMessagesAsync()
    {
        await foreach (string message in s_messagesChannel.Reader.ReadAllAsync())
        {
            await s_fileWriter.WriteLineAsync(message);
        }
    }
}