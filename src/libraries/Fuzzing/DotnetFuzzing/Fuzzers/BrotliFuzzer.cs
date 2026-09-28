// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Compression;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the managed Brotli APIs. The input is decompressed three ways (one-shot <see cref="BrotliDecoder.TryDecompress"/>,
/// streaming <see cref="BrotliDecoder.Decompress"/> with fuzzer-chosen buffer sizes, and <see cref="BrotliStream"/> over a
/// chunked stream), which must agree; <see cref="OperationStatus"/> results must be consistent; and the input must round-trip
/// through <see cref="BrotliEncoder.TryCompress(ReadOnlySpan{byte}, Span{byte}, out int, int, int)"/>, streaming
/// <see cref="BrotliEncoder.Compress"/> and <see cref="BrotliStream"/> compression.
/// </summary>
/// <remarks>Input layout: [0] chunk seed, [1] quality/window, [2..] data.</remarks>
internal sealed class BrotliFuzzer : IFuzzer
{
    private const int MaxOutput = 1 << 20;

    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers):
    // * BrotliStream.Read throws InvalidOperationException ("Decoder ran into invalid data") for corrupt data, although
    //   InvalidDataException is documented. Run against a Release build: Debug builds also trip a wrong Debug.Assert in
    //   BrotliDecoder.TryDecompress when a failed decompression reports partial output.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.IO.Compression.Brotli"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
        {
            return;
        }

        uint seed = bytes[0] * 2654435761u + 1;
        int quality = bytes[1] % 12;
        int window = 10 + (bytes[1] >> 4) % 15;
        byte[] data = bytes.Slice(2).ToArray();

        CheckDecompression(data, seed);
        CheckRoundTrip(data, quality, window, seed);
    }

    private static void CheckDecompression(byte[] data, uint seed)
    {
        // One-shot into a buffer big enough for anything we accept.
        byte[] oneShotBuffer = new byte[MaxOutput];
        bool oneShotOk = BrotliDecoder.TryDecompress(data, oneShotBuffer, out int oneShotWritten);
        Check(oneShotWritten >= 0 && oneShotWritten <= MaxOutput, $"TryDecompress wrote {oneShotWritten}");

        // Streaming with small, varying buffers.
        (OperationStatus status, byte[] output) streaming = StreamingDecompress(data, ref seed);

        // BrotliStream over a stream that returns data in small chunks.
        byte[]? streamOutput = null;
        Exception? streamError = null;
        try
        {
            using var brotli = new BrotliStream(new ChunkedStream(data, seed), CompressionMode.Decompress);
            streamOutput = ReadAll(brotli, ref seed);
        }
        catch (InvalidDataException ex)
        {
            streamError = ex;
        }
        catch (IOException ex)
        {
            streamError = ex;
        }
        catch (InvalidOperationException ex) when (!s_strict)
        {
            streamError = ex;
        }

        string context = $"input {Convert.ToHexString(data.AsSpan(0, Math.Min(data.Length, 64)))} ({data.Length} bytes)";
        if (oneShotOk)
        {
            // A complete, valid stream that fits: streaming must finish with the same bytes, and so must BrotliStream.
            Check(streaming.status == OperationStatus.Done && streaming.output.AsSpan().SequenceEqual(oneShotBuffer.AsSpan(0, oneShotWritten)),
                $"TryDecompress succeeded with {oneShotWritten} bytes, streaming Decompress ended {streaming.status} with {streaming.output.Length} bytes; {context}");
            Check(streamOutput is not null && streamOutput.AsSpan().StartsWith(oneShotBuffer.AsSpan(0, oneShotWritten)),
                $"TryDecompress succeeded with {oneShotWritten} bytes, BrotliStream gave {(streamOutput is null ? streamError!.GetType().Name + ": " + streamError.Message : streamOutput.Length + " bytes")}; {context}");
        }
        else if (streaming.status == OperationStatus.Done && streaming.output.Length <= MaxOutput)
        {
            Check(false, $"streaming Decompress finished with {streaming.output.Length} bytes but TryDecompress failed; {context}");
        }
    }

    private static (OperationStatus, byte[]) StreamingDecompress(byte[] data, ref uint seed)
    {
        using var decoder = new BrotliDecoder();
        var output = new List<byte>();
        ReadOnlySpan<byte> source = data;
        byte[] buffer = new byte[64];
        int stalls = 0;
        while (true)
        {
            int take = Math.Min(source.Length, Next(ref seed, 1, 32));
            int room = Next(ref seed, 1, buffer.Length);
            OperationStatus status = decoder.Decompress(source.Slice(0, take), buffer.AsSpan(0, room), out int consumed, out int written);
            Check(consumed >= 0 && consumed <= take && written >= 0 && written <= room, $"Decompress consumed {consumed}/{take}, wrote {written}/{room}");
            output.AddRange(buffer.AsSpan(0, written));
            source = source.Slice(consumed);

            switch (status)
            {
                case OperationStatus.Done:
                    return (status, [.. output]);
                case OperationStatus.InvalidData:
                    return (status, [.. output]);
                case OperationStatus.NeedMoreData when source.IsEmpty:
                    return (status, [.. output]);
            }

            stalls = consumed == 0 && written == 0 ? stalls + 1 : 0;
            Check(stalls < 64, $"Decompress made no progress 64 times in a row ({status})");
            if (output.Count > MaxOutput)
            {
                return (OperationStatus.DestinationTooSmall, [.. output]);
            }
        }
    }

    private static void CheckRoundTrip(byte[] data, int quality, int window, uint seed)
    {
        byte[] compressed = new byte[BrotliEncoder.GetMaxCompressedLength(data.Length)];
        Check(BrotliEncoder.TryCompress(data, compressed, out int compressedLength, quality, window), $"TryCompress failed for {data.Length} bytes");
        Check(Decompresses(compressed.AsSpan(0, compressedLength), data), $"TryCompress(quality {quality}, window {window}) output doesn't decompress to the input");

        // Streaming compression with small buffers and occasional flushes.
        using var encoder = new BrotliEncoder(quality, window);
        var streamed = new List<byte>();
        ReadOnlySpan<byte> source = data;
        byte[] buffer = new byte[32];
        int stalls = 0;
        while (true)
        {
            int take = Math.Min(source.Length, Next(ref seed, 0, 40));
            bool final = take == source.Length;
            OperationStatus status = encoder.Compress(source.Slice(0, take), buffer, out int consumed, out int written, isFinalBlock: final);
            streamed.AddRange(buffer.AsSpan(0, written));
            source = source.Slice(consumed);
            if (status == OperationStatus.Done && final && source.IsEmpty)
            {
                break;
            }

            if (Next(ref seed, 0, 8) == 0)
            {
                OperationStatus flushStatus;
                do
                {
                    flushStatus = encoder.Flush(buffer, out int flushed);
                    streamed.AddRange(buffer.AsSpan(0, flushed));
                }
                while (flushStatus == OperationStatus.DestinationTooSmall);
            }

            stalls = consumed == 0 && written == 0 ? stalls + 1 : 0;
            Check(stalls < 64 && status != OperationStatus.InvalidData, $"Compress stalled or failed ({status})");
        }

        Check(Decompresses([.. streamed], data), $"streaming Compress(quality {quality}, window {window}) output doesn't decompress to the input");

        var memory = new MemoryStream();
        using (var brotli = new BrotliStream(memory, CompressionLevel.Fastest, leaveOpen: true))
        {
            ReadOnlySpan<byte> rest = data;
            while (!rest.IsEmpty)
            {
                int take = Math.Min(rest.Length, Next(ref seed, 1, 50));
                brotli.Write(rest.Slice(0, take));
                rest = rest.Slice(take);
            }
        }

        Check(Decompresses(memory.ToArray(), data), "BrotliStream compression output doesn't decompress to the input");
    }

    private static bool Decompresses(ReadOnlySpan<byte> compressed, byte[] expected)
    {
        byte[] output = new byte[expected.Length + 16];
        return BrotliDecoder.TryDecompress(compressed, output, out int written) && output.AsSpan(0, written).SequenceEqual(expected);
    }

    private static byte[] ReadAll(Stream stream, ref uint seed)
    {
        var output = new MemoryStream();
        byte[] buffer = new byte[64];
        int read;
        while ((read = stream.Read(buffer, 0, Next(ref seed, 1, buffer.Length))) > 0)
        {
            output.Write(buffer, 0, read);
            if (output.Length > MaxOutput)
            {
                break;
            }
        }

        return output.ToArray();
    }

    private static int Next(ref uint state, int min, int max)
    {
        state = state * 1103515245 + 12345;
        return min + (int)((state >> 16) % (uint)(max - min + 1));
    }

    private sealed class ChunkedStream(byte[] data, uint seed) : Stream
    {
        private int _position;
        private uint _state = seed;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int size = Math.Min(Math.Min(count, Next(ref _state, 1, 17)), data.Length - _position);
            data.AsSpan(_position, size).CopyTo(buffer.AsSpan(offset));
            _position += size;
            return size;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
