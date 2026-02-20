# System.IO.Compression.Zstandard
This library provides support for Zstandard compression and decompression operations in .NET. Zstandard (zstd) is a fast compression algorithm that provides high compression ratios and is particularly effective for real-time compression scenarios.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aopen+is%3Aissue+label%3Aarea-System.IO.Compression+label%3A%22help+wanted%22) issues. We don't have a specific label for Zstandard issues so you will need to read through the list and see if something is available.

## Usage

```csharp
// Compression
using var input = new FileStream("input.txt", FileMode.Open);
using var output = new FileStream("compressed.zst", FileMode.Create);
using var zstdStream = new ZstandardStream(output, CompressionMode.Compress);
input.CopyTo(zstdStream);

// Decompression  
using var compressed = new FileStream("compressed.zst", FileMode.Open);
using var decompressed = new FileStream("output.txt", FileMode.Create);
using var zstdStream = new ZstandardStream(compressed, CompressionMode.Decompress);
zstdStream.CopyTo(decompressed);

// Using custom options
var options = new ZstandardCompressionOptions { Quality = 9 };
using var zstdStream = new ZstandardStream(output, options);
```

## Platform Support
This library is supported on all platforms supported by .NET except for browser and WASI.

## Deployment
`System.IO.Compression.Zstandard` is included in the shared framework since .NET 11.0.

## See also
 - [`System.IO.Compression`](../System.IO.Compression#readme)
