// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Compression;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the span-based compression codecs added in .NET 11 on top of the native zlib-ng and zstd libraries:
/// <see cref="DeflateEncoder"/>/<see cref="DeflateDecoder"/>, <see cref="ZLibEncoder"/>/<see cref="ZLibDecoder"/>,
/// <see cref="GZipEncoder"/>/<see cref="GZipDecoder"/> and <see cref="ZstandardEncoder"/>/<see cref="ZstandardDecoder"/>. Native code
/// writes straight into the caller's span, so every destination sits against a guard page and is sized exactly and one short.
/// <para>Decompress mode treats the input as compressed data: the one-shot TryDecompress, the streaming Decompress with tiny
/// fuzzer-chosen buffers and the matching Stream class must agree, an exactly sized destination must work and a one-short one
/// must fail cleanly, and for zstd <see cref="ZstandardDecoder.TryGetMaxDecompressedLength"/> must bound the real output.</para>
/// <para>Compress mode treats the input as plain data: TryCompress into GetMaxCompressedLength bytes must succeed for every quality
/// and window, streaming Compress with tiny buffers and interleaved Flush calls must round-trip, and each Flush must make all the
/// input so far decodable.</para>
/// </summary>
/// <remarks>Input layout: [0] codec and mode, [1] quality, [2] window, [3] chunk seed, [4..] data.</remarks>
internal sealed class CompressionCodecsFuzzer : IFuzzer
{
    private const int MaxOutput = 16 * 1024;
    private const int MaxPlain = 6 * 1024;

    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.IO.Compression"];
    public string[] TargetCoreLibPrefixes => [];

    private abstract class Codec
    {
        public abstract string Name { get; }
        public abstract bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int written);
        public abstract IDecoder CreateDecoder();
        public abstract IEncoder CreateEncoder(int quality, int window);
        public abstract bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int written, int quality, int window);
        public abstract long GetMaxCompressedLength(long length);
        public abstract Stream CreateDecompressionStream(Stream inner);
        public abstract int QualityFrom(byte b);
        public abstract int WindowFrom(byte b);

        // zstd's decoder stops after one frame (Reset continues with the next); zlib formats are one stream.
        public virtual bool MultiFrame => false;
    }

    private interface IDecoder : IDisposable
    {
        OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int consumed, out int written);
        void Reset();
    }

    private interface IEncoder : IDisposable
    {
        OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int consumed, out int written, bool isFinalBlock);
        OperationStatus Flush(Span<byte> destination, out int written);
    }

    private sealed class DeflateCodec : Codec
    {
        public override string Name => "Deflate";
        public override bool TryDecompress(ReadOnlySpan<byte> s, Span<byte> d, out int w) => DeflateDecoder.TryDecompress(s, d, out w);
        public override IDecoder CreateDecoder() => new Dec(new DeflateDecoder());
        public override IEncoder CreateEncoder(int q, int w) => new Enc(new DeflateEncoder(q, w));
        public override bool TryCompress(ReadOnlySpan<byte> s, Span<byte> d, out int w, int q, int win) => DeflateEncoder.TryCompress(s, d, out w, q, win);
        public override long GetMaxCompressedLength(long length) => DeflateEncoder.GetMaxCompressedLength(length);
        public override Stream CreateDecompressionStream(Stream inner) => new DeflateStream(inner, CompressionMode.Decompress);
        public override int QualityFrom(byte b) => b % 11 - 1; // -1 (default) .. 9
        public override int WindowFrom(byte b) => b % 9 == 8 ? -1 : 8 + b % 8; // 8..15 or -1

        private sealed class Dec(DeflateDecoder d) : IDecoder
        {
            public OperationStatus Decompress(ReadOnlySpan<byte> s, Span<byte> t, out int c, out int w) => d.Decompress(s, t, out c, out w);
            public void Reset() => d.Reset();
            public void Dispose() => d.Dispose();
        }

        private sealed class Enc(DeflateEncoder e) : IEncoder
        {
            public OperationStatus Compress(ReadOnlySpan<byte> s, Span<byte> t, out int c, out int w, bool f) => e.Compress(s, t, out c, out w, f);
            public OperationStatus Flush(Span<byte> t, out int w) => e.Flush(t, out w);
            public void Dispose() => e.Dispose();
        }
    }

    private sealed class ZLibCodec : Codec
    {
        public override string Name => "ZLib";
        public override bool TryDecompress(ReadOnlySpan<byte> s, Span<byte> d, out int w) => ZLibDecoder.TryDecompress(s, d, out w);
        public override IDecoder CreateDecoder() => new Dec(new ZLibDecoder());
        public override IEncoder CreateEncoder(int q, int w) => new Enc(new ZLibEncoder(q, w));
        public override bool TryCompress(ReadOnlySpan<byte> s, Span<byte> d, out int w, int q, int win) => ZLibEncoder.TryCompress(s, d, out w, q, win);
        public override long GetMaxCompressedLength(long length) => ZLibEncoder.GetMaxCompressedLength(length);
        public override Stream CreateDecompressionStream(Stream inner) => new ZLibStream(inner, CompressionMode.Decompress);
        public override int QualityFrom(byte b) => b % 11 - 1;
        public override int WindowFrom(byte b) => b % 9 == 8 ? -1 : 8 + b % 8;

        private sealed class Dec(ZLibDecoder d) : IDecoder
        {
            public OperationStatus Decompress(ReadOnlySpan<byte> s, Span<byte> t, out int c, out int w) => d.Decompress(s, t, out c, out w);
            public void Reset() => d.Reset();
            public void Dispose() => d.Dispose();
        }

        private sealed class Enc(ZLibEncoder e) : IEncoder
        {
            public OperationStatus Compress(ReadOnlySpan<byte> s, Span<byte> t, out int c, out int w, bool f) => e.Compress(s, t, out c, out w, f);
            public OperationStatus Flush(Span<byte> t, out int w) => e.Flush(t, out w);
            public void Dispose() => e.Dispose();
        }
    }

    private sealed class GZipCodec : Codec
    {
        public override string Name => "GZip";
        public override bool TryDecompress(ReadOnlySpan<byte> s, Span<byte> d, out int w) => GZipDecoder.TryDecompress(s, d, out w);
        public override IDecoder CreateDecoder() => new Dec(new GZipDecoder());
        public override IEncoder CreateEncoder(int q, int w) => new Enc(new GZipEncoder(q, w));
        public override bool TryCompress(ReadOnlySpan<byte> s, Span<byte> d, out int w, int q, int win) => GZipEncoder.TryCompress(s, d, out w, q, win);
        public override long GetMaxCompressedLength(long length) => GZipEncoder.GetMaxCompressedLength(length);
        public override Stream CreateDecompressionStream(Stream inner) => new GZipStream(inner, CompressionMode.Decompress);
        public override int QualityFrom(byte b) => b % 11 - 1;
        public override int WindowFrom(byte b) => b % 9 == 8 ? -1 : 8 + b % 8;

        private sealed class Dec(GZipDecoder d) : IDecoder
        {
            public OperationStatus Decompress(ReadOnlySpan<byte> s, Span<byte> t, out int c, out int w) => d.Decompress(s, t, out c, out w);
            public void Reset() => d.Reset();
            public void Dispose() => d.Dispose();
        }

        private sealed class Enc(GZipEncoder e) : IEncoder
        {
            public OperationStatus Compress(ReadOnlySpan<byte> s, Span<byte> t, out int c, out int w, bool f) => e.Compress(s, t, out c, out w, f);
            public OperationStatus Flush(Span<byte> t, out int w) => e.Flush(t, out w);
            public void Dispose() => e.Dispose();
        }
    }

    private sealed class ZstdCodec : Codec
    {
        public override string Name => "Zstandard";
        public override bool MultiFrame => true;
        public override bool TryDecompress(ReadOnlySpan<byte> s, Span<byte> d, out int w) => ZstandardDecoder.TryDecompress(s, d, out w);
        public override IDecoder CreateDecoder() => new Dec(new ZstandardDecoder());
        public override IEncoder CreateEncoder(int q, int w) => new Enc(new ZstandardEncoder(q, w));
        public override bool TryCompress(ReadOnlySpan<byte> s, Span<byte> d, out int w, int q, int win) => ZstandardEncoder.TryCompress(s, d, out w, q, win);
        public override long GetMaxCompressedLength(long length) => ZstandardEncoder.GetMaxCompressedLength(length);
        public override Stream CreateDecompressionStream(Stream inner) => new ZstandardStream(inner, CompressionMode.Decompress);

        // Negative levels are valid for zstd (down to ZSTD_minCLevel); 0 means the default.
        public override int QualityFrom(byte b) => (b % 8) switch { 0 => 0, 1 => -5, 2 => -1, 3 => 1, 4 => 3, 5 => 9, 6 => 19, _ => ZstandardCompressionOptions.MinQuality };
        public override int WindowFrom(byte b) => b % 4 == 0 ? 0 : 10 + b % 14; // 0 (default) or 10..23

        private sealed class Dec(ZstandardDecoder d) : IDecoder
        {
            public OperationStatus Decompress(ReadOnlySpan<byte> s, Span<byte> t, out int c, out int w) => d.Decompress(s, t, out c, out w);
            public void Reset() => d.Reset();
            public void Dispose() => d.Dispose();
        }

        private sealed class Enc(ZstandardEncoder e) : IEncoder
        {
            public OperationStatus Compress(ReadOnlySpan<byte> s, Span<byte> t, out int c, out int w, bool f) => e.Compress(s, t, out c, out w, f);
            public OperationStatus Flush(Span<byte> t, out int w) => e.Flush(t, out w);
            public void Dispose() => e.Dispose();
        }
    }

    private static readonly Codec[] s_codecs = [new DeflateCodec(), new ZLibCodec(), new GZipCodec(), new ZstdCodec()];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return;
        }

        Codec codec = s_codecs[bytes[0] % s_codecs.Length];
        bool compressMode = (bytes[0] & 0x80) != 0;
        int quality = codec.QualityFrom(bytes[1]);
        int window = codec.WindowFrom(bytes[2]);
        uint seed = bytes[3] * 2654435761u + 1;
        byte[] data = bytes.Slice(4).ToArray();

        if (compressMode)
        {
            if (data.Length <= MaxPlain)
            {
                CheckCompression(codec, data, quality, window, seed);
            }
        }
        else
        {
            CheckDecompression(codec, data, seed);
        }
    }

    // ---------------------------------------------------------------- decompression

    private static void CheckDecompression(Codec codec, byte[] data, uint seed)
    {
        string context = $"{codec.Name} input {Convert.ToHexString(data.AsSpan(0, Math.Min(data.Length, 48)))} ({data.Length} bytes)";

        // One-shot into a roomy guard-paged buffer.
        byte[]? oneShot = null;
        using (PooledBoundedMemory<byte> roomy = PooledBoundedMemory<byte>.Rent(MaxOutput, PoisonPagePlacement.After))
        {
            if (codec.TryDecompress(data, roomy.Span, out int written))
            {
                Check(written >= 0 && written <= MaxOutput, $"TryDecompress reported {written} bytes written: {context}");
                oneShot = roomy.Span.Slice(0, written).ToArray();
            }
            else
            {
                Check(written == 0, $"failed TryDecompress reported {written} bytes written: {context}");
            }
        }

        // Streaming with tiny buffers, placing each destination chunk against a guard page.
        (bool done, int consumed, byte[] streamed) = StreamingDecompress(codec, data, ref seed);

        if (codec is ZstdCodec && oneShot is not null && ZstandardDecoder.TryGetMaxDecompressedLength(data, out long bound))
        {
            Check(bound >= oneShot.Length, $"TryGetMaxDecompressedLength={bound} but TryDecompress produced {oneShot.Length} bytes: {context}");
        }

        if (oneShot is null)
        {
            return;
        }

        // A complete stream that fits: streaming must finish, consume everything and produce the same bytes.
        Check(done && consumed == data.Length, $"TryDecompress succeeded but streaming Decompress finished={done} after {consumed} bytes: {context}");
        Check(streamed.AsSpan().SequenceEqual(oneShot), $"streaming Decompress produced {streamed.Length} bytes, TryDecompress {oneShot.Length}, or different bytes: {context}");

        // The Stream class must agree as well.
        byte[]? viaStream = null;
        try
        {
            using Stream stream = codec.CreateDecompressionStream(new MemoryStream(data));
            viaStream = ReadAll(stream, ref seed);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            Check(false, $"TryDecompress succeeded but the stream threw {ex.GetType().Name} ({ex.Message}): {context}");
        }

        Check(viaStream!.AsSpan().SequenceEqual(oneShot), $"stream produced {viaStream!.Length} bytes, TryDecompress {oneShot.Length}, or different bytes: {context}");

        // Exactly sized destination (both guard-page placements) must work; one short must fail without writing past the end.
        foreach (PoisonPagePlacement placement in new[] { PoisonPagePlacement.After, PoisonPagePlacement.Before })
        {
            using (PooledBoundedMemory<byte> exact = PooledBoundedMemory<byte>.Rent(oneShot.Length, placement))
            {
                if (oneShot.Length == 0 && codec is not ZstdCodec && !s_strict)
                {
                    break; // Finding 55: the zlib decoders refuse an empty destination even when the output is empty.
                }

                bool ok = codec.TryDecompress(data, exact.Span, out int written);
                Check(ok && written == oneShot.Length && exact.Span.SequenceEqual(oneShot),
                    $"TryDecompress into exactly {oneShot.Length} bytes ({placement}) returned {ok}/{written}: {context}");
            }

            if (oneShot.Length > 0)
            {
                using PooledBoundedMemory<byte> small = PooledBoundedMemory<byte>.Rent(oneShot.Length - 1, placement);
                bool ok = codec.TryDecompress(data, small.Span, out int written);
                Check(!ok && written == 0, $"TryDecompress into {oneShot.Length - 1} bytes ({placement}) returned {ok}/{written}: {context}");
            }
        }
    }

    private static (bool Done, int Consumed, byte[] Output) StreamingDecompress(Codec codec, byte[] data, ref uint seed)
    {
        using IDecoder decoder = codec.CreateDecoder();
        var output = new List<byte>();
        int offset = 0;
        bool sawDone = false;

        for (int iterations = 0; iterations < 100_000; iterations++)
        {
            int available = data.Length - offset;
            int inChunk = Math.Min(available, NextChunk(ref seed, 64));
            int outChunk = NextChunk(ref seed, 96);

            using PooledBoundedMemory<byte> input = PooledBoundedMemory<byte>.Rent(data.AsSpan(offset, inChunk), PoisonPagePlacement.After);
            using PooledBoundedMemory<byte> dest = PooledBoundedMemory<byte>.Rent(outChunk, (seed & 1) == 0 ? PoisonPagePlacement.After : PoisonPagePlacement.Before);

            OperationStatus status;
            int consumed, written;
            try
            {
                status = decoder.Decompress(input.Span, dest.Span, out consumed, out written);
            }
            catch (Exception ex) when (codec is ZstdCodec && ex is InvalidDataException or IOException)
            {
                // By design, zstd throws for a frame that names a dictionary or needs a bigger window instead of returning InvalidData.
                return (false, offset, output.ToArray());
            }

            Check(consumed >= 0 && consumed <= inChunk && written >= 0 && written <= outChunk,
                $"{codec.Name} Decompress consumed {consumed}/{inChunk}, wrote {written}/{outChunk}");

            output.AddRange(dest.Span.Slice(0, written).ToArray());
            offset += consumed;
            if (output.Count > MaxOutput)
            {
                return (false, offset, output.ToArray());
            }

            switch (status)
            {
                case OperationStatus.Done:
                    sawDone = true;
                    if (codec.MultiFrame && offset < data.Length)
                    {
                        decoder.Reset(); // zstd: the next frame
                        sawDone = false;
                        continue;
                    }

                    return (true, offset, output.ToArray());

                case OperationStatus.InvalidData:
                    return (false, offset, output.ToArray());

                case OperationStatus.NeedMoreData:
                    if (offset == data.Length && written == 0)
                    {
                        return (false, offset, output.ToArray()); // truncated
                    }

                    break;

                case OperationStatus.DestinationTooSmall:
                    break;
            }

            if (consumed == 0 && written == 0 && offset == data.Length && status != OperationStatus.DestinationTooSmall)
            {
                return (sawDone, offset, output.ToArray());
            }
        }

        throw new InvalidOperationException($"{codec.Name} streaming Decompress made no progress");
    }

    // ---------------------------------------------------------------- compression

    private static void CheckCompression(Codec codec, byte[] data, int quality, int window, uint seed)
    {
        string context = $"{codec.Name} quality {quality} window {window}, {data.Length} input bytes";

        // TryCompress into GetMaxCompressedLength bytes must always succeed.
        long max = codec.GetMaxCompressedLength(data.Length);
        Check(max >= data.Length && max < int.MaxValue, $"GetMaxCompressedLength({data.Length}) = {max}: {context}");
        byte[] compressed;
        using (PooledBoundedMemory<byte> dest = PooledBoundedMemory<byte>.Rent((int)max, PoisonPagePlacement.After))
        {
            bool ok = codec.TryCompress(data, dest.Span, out int written, quality, window);
            Check(ok && written > 0 && written <= max, $"TryCompress into GetMaxCompressedLength = {max} bytes returned {ok}/{written}: {context}");
            compressed = dest.Span.Slice(0, written).ToArray();
        }

        CheckDecodesTo(codec, compressed, data, "TryCompress", context);

        // Exactly sized and one-short destinations: whatever succeeds must round-trip, and nothing may write past the end.
        foreach (int size in new[] { compressed.Length, compressed.Length - 1, compressed.Length / 2 })
        {
            if (size < 0)
            {
                continue;
            }

            using PooledBoundedMemory<byte> dest = PooledBoundedMemory<byte>.Rent(size, (seed & 2) == 0 ? PoisonPagePlacement.After : PoisonPagePlacement.Before);
            bool ok = codec.TryCompress(data, dest.Span, out int written, quality, window);
            Check(ok ? written > 0 && written <= size : written == 0, $"TryCompress into {size} bytes returned {ok}/{written}: {context}");
            if (ok)
            {
                CheckDecodesTo(codec, dest.Span.Slice(0, written).ToArray(), data, $"TryCompress into {size} bytes", context);
            }
        }

        // Streaming with tiny buffers and interleaved flushes; after each flush everything so far must be decodable.
        using IEncoder encoder = codec.CreateEncoder(quality, window);
        var output = new List<byte>();
        int offset = 0;
        for (int iterations = 0; ; iterations++)
        {
            Check(iterations < 1_000_000, $"streaming Compress made no progress: {context}");
            bool final = offset == data.Length;
            int inChunk = Math.Min(data.Length - offset, NextChunk(ref seed, 200));
            int outChunk = NextChunk(ref seed, 48);

            using PooledBoundedMemory<byte> input = PooledBoundedMemory<byte>.Rent(data.AsSpan(offset, inChunk), PoisonPagePlacement.After);
            using PooledBoundedMemory<byte> dest = PooledBoundedMemory<byte>.Rent(outChunk, (seed & 1) == 0 ? PoisonPagePlacement.After : PoisonPagePlacement.Before);
            OperationStatus status = encoder.Compress(input.Span, dest.Span, out int consumed, out int written, final);
            Check(consumed >= 0 && consumed <= inChunk && written >= 0 && written <= outChunk,
                $"Compress consumed {consumed}/{inChunk}, wrote {written}/{outChunk}: {context}");
            Check(status is OperationStatus.Done or OperationStatus.DestinationTooSmall, $"Compress returned {status}: {context}");
            output.AddRange(dest.Span.Slice(0, written).ToArray());
            offset += consumed;

            if (final && status == OperationStatus.Done)
            {
                break;
            }

            if (!final && (seed & 0x70) == 0 && consumed > 0)
            {
                // Flush until done, then everything consumed so far must decode.
                for (int flushes = 0; ; flushes++)
                {
                    Check(flushes < 100_000, $"Flush made no progress: {context}");
                    using PooledBoundedMemory<byte> flushDest = PooledBoundedMemory<byte>.Rent(NextChunk(ref seed, 48), PoisonPagePlacement.After);
                    OperationStatus flushStatus = encoder.Flush(flushDest.Span, out int flushed);
                    Check(flushed >= 0 && flushed <= flushDest.Span.Length, $"Flush wrote {flushed}/{flushDest.Span.Length}: {context}");
                    output.AddRange(flushDest.Span.Slice(0, flushed).ToArray());
                    if (flushStatus == OperationStatus.Done)
                    {
                        break;
                    }

                    Check(flushStatus == OperationStatus.DestinationTooSmall, $"Flush returned {flushStatus}: {context}");
                }

                CheckFlushedPrefix(codec, output.ToArray(), data.AsSpan(0, offset).ToArray(), context);
            }
        }

        CheckDecodesTo(codec, output.ToArray(), data, "streaming Compress", context);
    }

    private static void CheckDecodesTo(Codec codec, byte[] compressed, byte[] expected, string what, string context)
    {
        byte[] buffer = new byte[expected.Length + 16];
        bool ok = codec.TryDecompress(compressed, buffer, out int written);
        Check(ok && buffer.AsSpan(0, written).SequenceEqual(expected),
            $"{what} output ({compressed.Length} bytes) doesn't decompress back: TryDecompress {ok}/{written} of {expected.Length}: {context}");
    }

    // A flushed but unfinished stream decodes, as far as it goes, to exactly the data compressed so far.
    private static void CheckFlushedPrefix(Codec codec, byte[] compressed, byte[] expected, string context)
    {
        using IDecoder decoder = codec.CreateDecoder();
        byte[] buffer = new byte[expected.Length + 16];
        OperationStatus status = decoder.Decompress(compressed, buffer, out int consumed, out int written);
        Check(status is OperationStatus.NeedMoreData or OperationStatus.Done && consumed == compressed.Length && buffer.AsSpan(0, written).SequenceEqual(expected),
            $"after Flush, decoding {compressed.Length} bytes gave {status}, consumed {consumed}, {written} of {expected.Length} bytes: {context}");
    }

    private static byte[] ReadAll(Stream stream, ref uint seed)
    {
        var output = new MemoryStream();
        byte[] buffer = new byte[97];
        while (output.Length <= MaxOutput)
        {
            int read = stream.Read(buffer, 0, NextChunk(ref seed, buffer.Length));
            if (read == 0)
            {
                break;
            }

            output.Write(buffer, 0, read);
        }

        return output.ToArray();
    }

    private static int NextChunk(ref uint seed, int max)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return 1 + (int)(seed % (uint)max);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
