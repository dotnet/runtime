// Finding: BrotliStream.Read documents InvalidDataException for data in an invalid format (and throws it for truncated or
// invalid streams at the end), but corrupt data in the middle of the stream throws InvalidOperationException("Decoder ran into
// invalid data") from BrotliStream.TryDecompress. Code that catches InvalidDataException for untrusted input crashes.
// Run: dotnet run 20-BrotliStream-InvalidDataException.cs
using System.IO.Compression;

byte[] corrupt = Convert.FromHexString("702262223A2274657874227D03"); // found by BrotliFuzzer
string outcome;
try
{
    using var brotli = new BrotliStream(new MemoryStream(corrupt), CompressionMode.Decompress);
    brotli.CopyTo(Stream.Null);
    outcome = "no exception";
}
catch (Exception ex) { outcome = ex.GetType().Name + ": " + ex.Message; }

Console.WriteLine($"BrotliStream over {Convert.ToHexString(corrupt)} -> {outcome}; documented InvalidDataException");
Console.WriteLine(outcome.StartsWith(nameof(InvalidOperationException), StringComparison.Ordinal) ? "REPRODUCED: corrupt data surfaces as InvalidOperationException." : "NOT REPRODUCED");
