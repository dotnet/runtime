// Finding: whether a Zstandard frame decodes or is rejected depends on how its bytes are split across Decompress calls or
// Stream.Read results. The 10-byte frame below (single segment, content size 0, one last RLE block of size 0) decodes to
// nothing when handed to ZstandardDecoder.Decompress whole, but returns InvalidData when fed in chunks of 5, 4 or 1 bytes.
// ZstandardStream decodes it as the first frame of a stream, but throws InvalidDataException when it's a later concatenated
// frame (ZstandardStream feeds each following frame's magic number separately), or when the underlying stream returns data a
// few bytes at a time, as a network stream might.
//
// Root cause (bundled zstd 1.5.7, lib/decompress/zstd_decompress.c): ZSTD_decompressStream takes a single-pass shortcut
// (ZSTD_decompress_usingDDict) when the whole frame is already in the input buffer. Otherwise it decodes block by block, and
// ZSTD_decompressContinue checks the *compressed* block size against blockSizeMax ("Block Size Exceeds Maximum"). For a
// single-segment frame the window, and so blockSizeMax, equals the content size, here 0; an RLE block's compressed size is
// always 1 (the repeated byte), so 1 > 0 fails even though the block decodes to 0 bytes. The one-shot path doesn't make that
// check (compare finding 56, the opposite direction).
// Run: dotnet run 57-Zstandard-ResultDependsOnInputChunking.cs
using System.Buffers;
using System.IO.Compression;

byte[] frame = Convert.FromHexString("28B52FFD" + "20" + "00" + "030000" + "07");
//                                   magic     FHD:   FCS=0 last RLE      RLE byte
//                                             single       block of
//                                             segment      size 0
bool reproduced = false;

foreach (int chunk in new[] { frame.Length, 5, 4, 1 })
{
    using var decoder = new ZstandardDecoder();
    int offset = 0;
    OperationStatus status = OperationStatus.NeedMoreData;
    while (offset < frame.Length)
    {
        status = decoder.Decompress(frame.AsSpan(offset, Math.Min(chunk, frame.Length - offset)), new byte[16], out int consumed, out _);
        offset += consumed;
        if (status is OperationStatus.Done or OperationStatus.InvalidData)
        {
            break;
        }
    }

    Console.WriteLine($"ZstandardDecoder.Decompress, input fed in {chunk}-byte chunks: {status}");
    reproduced |= status == OperationStatus.InvalidData;
}

Console.WriteLine($"ZstandardDecoder.TryDecompress (one-shot): {ZstandardDecoder.TryDecompress(frame, new byte[16], out _)}");
Console.WriteLine($"ZstandardStream, the frame alone:                       {ReadAll(new MemoryStream(frame))}");
Console.WriteLine($"ZstandardStream, the frame twice (concatenated frames): {ReadAll(new MemoryStream([.. frame, .. frame]))}");
Console.WriteLine($"ZstandardStream, underlying stream returns 1 byte/Read: {ReadAll(new TrickleStream(frame))}");
Console.WriteLine("Expected: the same bytes give the same result however they're split.");
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

static string ReadAll(Stream inner)
{
    try
    {
        using var zstd = new ZstandardStream(inner, CompressionMode.Decompress);
        var sink = new MemoryStream();
        zstd.CopyTo(sink);
        return $"decoded, {sink.Length} bytes";
    }
    catch (InvalidDataException ex)
    {
        return $"InvalidDataException ({ex.Message})";
    }
}

sealed class TrickleStream(byte[] data) : MemoryStream(data)
{
    public override int Read(Span<byte> buffer) => base.Read(buffer.Slice(0, Math.Min(1, buffer.Length)));
    public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(1, count));
}
