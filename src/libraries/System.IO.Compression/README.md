# System.IO.Compression
Contains the source and tests of assembly System.IO.Compression, that includes types widely used on the compression space, as well as types for zip manipulation, and implementations of the Deflate and Gzip algorithms.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.io.compression.

## System.IO.Compression.Native source
[/src/native/libs/System.IO.Compression.Native](/src/native/libs/System.IO.Compression.Native) contains the PAL (Platform Abstraction Layer) of the zlib library used by System.IO.Compression.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](/src/libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](/src/libraries/README.md#secondary-bars)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aopen+is%3Aissue+label%3Aarea-System.IO.Compression+label%3A%22help+wanted%22) issues.

## Deployment
`System.IO.Compression` is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.0.

## See also
 - [`System.IO.Compression.Brotli`](../System.IO.Compression.Brotli#readme)
 - [`System.IO.Compression.ZipFile`](../System.IO.Compression.ZipFile#readme)