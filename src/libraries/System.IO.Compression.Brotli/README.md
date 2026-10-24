# System.IO.Compression.Brotli
Contains the source and tests of assembly System.IO.Compression.Brotli, which includes types used to compress and decompress stream and streamless data using the Brotli algorithm.

## Implementation
The encoder and decoder are a fully managed, safe C# port of the Brotli reference implementation ([google/brotli](https://github.com/google/brotli) v1.2.1); no native code is involved.
- `src/System/IO/Compression/common` - code and data shared by the encoder and decoder (static dictionary, context lookup tables, transforms).
- `src/System/IO/Compression/dec` - the decoder (port of `c/dec`); `BrotliDecoderState` is the decoder state machine.
- `src/System/IO/Compression/enc` - the encoder (port of `c/enc`); `BrotliEncoderState` is the encoder state machine.

Each ported file names its C counterpart in the header, and functions keep the names of the reference implementation (minus the `Brotli` prefix) to make it easy to port upstream changes. For a given quality and window size the encoder produces byte-identical output to the reference implementation, which is a useful property to preserve when porting changes. Not supported: large-window Brotli, custom/shared dictionaries.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aopen+is%3Aissue+label%3Aarea-System.IO.Compression+label%3A%22help+wanted%22) issues. We don't have a specific label for Brotli issues so you will need to read through the list and see if something is available.

## Deployment
`System.IO.Compression.Brotli` is included in the shared framework since .NET Core 2.1 and .NET standard 2.1.

## See also
 - [`System.IO.Compression`](../System.IO.Compression#readme)
