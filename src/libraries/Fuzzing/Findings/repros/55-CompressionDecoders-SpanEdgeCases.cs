// Finding: DeflateDecoder, ZLibDecoder and GZipDecoder (new span-based APIs in .NET 11) can't decode a valid stream whose
// payload is empty into an empty destination. TryDecompress returns false, and the streaming Decompress returns
// DestinationTooSmall forever, even though nothing needs to be written. DeflateDecoder.Decompress starts with
//     if (destination.IsEmpty && source.Length > 0) return OperationStatus.DestinationTooSmall;
// so zlib never gets to see the end-of-stream marker. BrotliDecoder and ZstandardDecoder.TryDecompress accept the same case.
// A caller that sizes the output from a stored length (for example a length-prefixed record whose length is 0) gets a failure
// for perfectly valid data. ZstandardDecoder's streaming Decompress has the same early DestinationTooSmall return.
// A second edge case in the same new APIs: ZstandardDecoder.Decompress checks its "finished" flag before EnsureNotDisposed, so
// once a frame has been decoded, calling Decompress after Dispose returns Done instead of throwing ObjectDisposedException
// (DeflateDecoder throws, as documented). Low severity, but these are new APIs, so the edge cases are cheap to fix now.
// Run: dotnet run 55-DeflateDecoder-EmptyOutputEmptyDestination.cs
using System.Buffers;
using System.IO.Compression;

bool reproduced = false;
byte[] scratch = new byte[64];

// Compress an empty payload with each encoder, then decode it into an empty destination.
{
    DeflateEncoder.TryCompress(ReadOnlySpan<byte>.Empty, scratch, out int n);
    byte[] compressed = scratch[..n];
    bool ok = DeflateDecoder.TryDecompress(compressed, Span<byte>.Empty, out int written);
    using var decoder = new DeflateDecoder();
    OperationStatus status = decoder.Decompress(compressed, Span<byte>.Empty, out int consumed, out _);
    Console.WriteLine($"Deflate: {n}-byte stream of empty data -> TryDecompress(empty destination) = {ok}; Decompress = {status}, consumed {consumed}");
    reproduced |= !ok;
}
{
    ZLibEncoder.TryCompress(ReadOnlySpan<byte>.Empty, scratch, out int n);
    bool ok = ZLibDecoder.TryDecompress(scratch[..n], Span<byte>.Empty, out _);
    Console.WriteLine($"ZLib:    {n}-byte stream of empty data -> TryDecompress(empty destination) = {ok}");
    reproduced |= !ok;
}
{
    GZipEncoder.TryCompress(ReadOnlySpan<byte>.Empty, scratch, out int n);
    bool ok = GZipDecoder.TryDecompress(scratch[..n], Span<byte>.Empty, out _);
    Console.WriteLine($"GZip:    {n}-byte stream of empty data -> TryDecompress(empty destination) = {ok}");
    reproduced |= !ok;
}
{
    BrotliEncoder.TryCompress(ReadOnlySpan<byte>.Empty, scratch, out int n);
    bool ok = BrotliDecoder.TryDecompress(scratch[..n], Span<byte>.Empty, out _);
    Console.WriteLine($"Brotli:  {n}-byte stream of empty data -> TryDecompress(empty destination) = {ok} (for comparison)");
}
if (!OperatingSystem.IsBrowser())
{
    ZstandardEncoder.TryCompress(ReadOnlySpan<byte>.Empty, scratch, out int n);
    bool ok = ZstandardDecoder.TryDecompress(scratch[..n], Span<byte>.Empty, out _);
    Console.WriteLine($"Zstd:    {n}-byte stream of empty data -> TryDecompress(empty destination) = {ok} (for comparison)");
}

// Use after Dispose.
{
    ZstandardEncoder.TryCompress("hi"u8, scratch, out int n);
    var decoder = new ZstandardDecoder();
    decoder.Decompress(scratch.AsSpan(0, n), new byte[16], out _, out _);
    decoder.Dispose();
    string after;
    try { after = decoder.Decompress(scratch.AsSpan(0, n), new byte[16], out _, out _).ToString(); }
    catch (ObjectDisposedException) { after = "ObjectDisposedException"; }
    Console.WriteLine($"ZstandardDecoder.Decompress after Dispose (a frame was already decoded): {after}");
    reproduced |= after != "ObjectDisposedException";
}

Console.WriteLine("Expected: decoding an empty payload into an empty destination succeeds, as it does for Brotli and Zstandard, and a disposed decoder throws ObjectDisposedException.");
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");
