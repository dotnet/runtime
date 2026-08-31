## About

<!-- A description of the package and where one can find more documentation -->

Provides support for matching file system names/paths using [glob patterns](https://en.wikipedia.org/wiki/Glob_(programming)).

## Key Features

<!-- The key features of this package -->

* Contains the `Matcher` type, which can be used to match files in the file system based on user-defined patterns.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Get all matching files:

```c#
using Microsoft.Extensions.FileSystemGlobbing;

Matcher matcher = new();
matcher.AddIncludePatterns(new[] { "*.txt", "*.asciidoc", "*.md" });

string searchDirectory = "../starting-folder/";

IEnumerable<string> matchingFiles = matcher.GetResultsInFullPath(searchDirectory);

// Use matchingFiles if there are any found.
// The files in this collection are fully qualified file system paths.
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.FileSystemGlobbing.Matcher`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/file-globbing)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.filesystemglobbing)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.FileSystemGlobbing is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).