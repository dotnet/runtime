// Finding: ZstandardDecoder.TryDecompress accepts Zstandard frames that ZstandardDecoder.Decompress and ZstandardStream reject,
// so the same bytes decode or fail depending on which .NET API reads them. The frame below declares a 1 KB window and then
// carries an 8254-byte RLE block. The Zstandard format (RFC 8878, section 3.1.1.2) caps a block at Block_Maximum_Size =
// min(Window_Size, 128 KB), so the frame is invalid. The streaming decoder checks that ("Block Size Exceeds Maximum",
// zstd_decompress.c) and returns InvalidData, and ZstandardStream throws InvalidDataException. The one-shot path in the bundled
// zstd 1.5.7 (ZSTD_decompressFrame) never compares raw/RLE block sizes with blockSizeMax, so TryDecompress returns 8255 bytes.
// The comparison is upstream's, but .NET exposes both paths as equivalent APIs. A component that validates with one and
// decodes with the other (a proxy checking a zstd Content-Encoding body one-shot, a backend streaming it) sees different data.
// No memory-safety impact: the one-shot writes stay within the destination.
// It also breaks ZstandardDecoder.TryGetMaxDecompressedLength, which bounds the output by the window-derived block maximum:
// for this frame it reports 2048 bytes (two blocks of at most 1 KB) while TryDecompress produces 8255.
// Related, by design: the one-shot path also ignores the maxWindowLog2 limit, since it doesn't allocate a window.
// Run: dotnet run 56-Zstandard-OneShotAcceptsOversizedBlock.cs
using System.Buffers;
using System.IO.Compression;

byte[] frame =
[
    0x28, 0xB5, 0x2F, 0xFD, // magic
    0x00,                   // frame header descriptor: no content size, no checksum, no dictionary
    0x00,                   // window descriptor: 2^10 = 1 KB window
    0xF2, 0x01, 0x01,       // block header: not last, RLE, size 8254 (larger than the 1 KB window)
    0x00,                   // RLE byte
    0x09, 0x00, 0x00,       // block header: last, raw, size 1
    0x61,
];

bool oneShot = ZstandardDecoder.TryDecompress(frame, new byte[16384], out int written);

using var decoder = new ZstandardDecoder();
OperationStatus streaming = decoder.Decompress(frame, new byte[16384], out int consumed, out int streamedBytes);

string viaStream;
try
{
    using var zstd = new ZstandardStream(new MemoryStream(frame), CompressionMode.Decompress);
    viaStream = $"read {zstd.CopyToCount()} bytes";
}
catch (InvalidDataException ex)
{
    viaStream = $"InvalidDataException: {ex.Message}";
}

bool gotBound = ZstandardDecoder.TryGetMaxDecompressedLength(frame, out long bound);
Console.WriteLine($"ZstandardDecoder.TryDecompress: {oneShot}, {written} bytes");
Console.WriteLine($"TryGetMaxDecompressedLength:    {gotBound}, {bound} bytes");
Console.WriteLine($"ZstandardDecoder.Decompress:    {streaming}, consumed {consumed}, wrote {streamedBytes}");
Console.WriteLine($"ZstandardStream:                {viaStream}");
Console.WriteLine("Expected: all three reject the frame (or all accept it).");
Console.WriteLine(oneShot && streaming == OperationStatus.InvalidData ? "REPRODUCED" : "NOT REPRODUCED");

static class StreamExtensions
{
    public static long CopyToCount(this Stream stream)
    {
        var sink = new MemoryStream();
        stream.CopyTo(sink);
        return sink.Length;
    }
}
