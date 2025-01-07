## About

<!-- A description of the package and where one can find more documentation -->

Serves as the foundation for creating file providers in .NET, offering core abstractions to develop custom file providers capable of fetching files from various sources.

## Key Features

<!-- The key features of this package -->

* Core abstractions for creating and managing file providers.
* Flexibility to develop custom file providers for fetching files from distinct sources.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

This package is typically used with an implementation of the file provider abstractions, such as `Microsoft.Extensions.FileProviders.Composite` or `Microsoft.Extensions.FileProviders.Physical`.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.FileProviders.IFileProvider`
* `Microsoft.Extensions.FileProviders.IDirectoryContents`
* `Microsoft.Extensions.FileProviders.IFileInfo`
* `Microsoft.Extensions.FileProviders.NullFileProvider`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/aspnet/core/fundamentals/file-providers)
* [Detect changes with change tokens](https://learn.microsoft.com/aspnet/core/fundamentals/change-tokens)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.fileproviders)

## Related Packages

<!-- The related packages associated with this package -->

* File provider for physical files: [Microsoft.Extensions.FileProviders.Physical](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Physical/)
* File provider for files in embedded resources: [Microsoft.Extensions.FileProviders.Embedded](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Embedded/)
* Composite file and directory providers: [Microsoft.Extensions.FileProviders.Composite](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Composite/)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.FileProviders.Abstractions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
