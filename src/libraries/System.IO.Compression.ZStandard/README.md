# System.IO.Compression.ZStandard

This library provides support for ZStandard compression and decompression operations in .NET. ZStandard (zstd) is a fast compression algorithm that provides high compression ratios and is particularly effective for real-time compression scenarios.

## Features

- **ZStandardStream**: A stream-based API for compressing and decompressing data using the ZStandard algorithm
- **ZStandardOptions**: Configuration options for controlling compression parameters including compression level
- **High Performance**: Optimized for both compression ratio and speed
- **Wide Compression Level Range**: Supports compression levels from negative values (faster, less compression) to 22 (slower, better compression)

## Usage

```csharp
// Compression
using var input = new FileStream("input.txt", FileMode.Open);
using var output = new FileStream("compressed.zst", FileMode.Create);
using var zstdStream = new ZStandardStream(output, CompressionMode.Compress);
input.CopyTo(zstdStream);

// Decompression  
using var compressed = new FileStream("compressed.zst", FileMode.Open);
using var decompressed = new FileStream("output.txt", FileMode.Create);
using var zstdStream = new ZStandardStream(compressed, CompressionMode.Decompress);
zstdStream.CopyTo(decompressed);

// Using custom options
var options = new ZStandardOptions { CompressionLevel = 9 };
using var zstdStream = new ZStandardStream(output, options, CompressionMode.Compress);
```

## Platform Support

This library is supported on Windows, Linux, and macOS, but not on browser or WebAssembly platforms.
