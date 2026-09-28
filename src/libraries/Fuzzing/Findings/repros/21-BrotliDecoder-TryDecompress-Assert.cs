// Finding: BrotliDecoder.TryDecompress asserts Debug.Assert(success ? availableOutput <= destination.Length : availableOutput == 0),
// but when the native decoder fails part-way it reports the partial output, which the method's own remarks allow ("destination may
// ... contain partially decompressed data"). Release builds return false with bytesWritten > 0 as documented; Debug/Checked builds
// of System.IO.Compression.Brotli abort the process on such input.
// Run: dotnet run 21-BrotliDecoder-TryDecompress-Assert.cs   (shows the partial output; the assert needs a Debug/Checked build)
using System.IO.Compression;

byte[] corrupt = Convert.FromHexString("7001107B2261223A5B312C3203"); // found by BrotliFuzzer
byte[] destination = new byte[1024];
bool ok = BrotliDecoder.TryDecompress(corrupt, destination, out int written);
Console.WriteLine($"TryDecompress({Convert.ToHexString(corrupt)}) -> {ok}, bytesWritten {written}");
Console.WriteLine(!ok && written > 0 ? "REPRODUCED: a failed TryDecompress reports partial output, which the Debug.Assert says can't happen." : "NOT REPRODUCED");
