## About

Combines multiple `IFileProvider` instances into a single provider, allowing files and directories from multiple sources to be accessed through one interface.

## Key Features

* Searches configured file providers in order and returns the first matching file.
* Merges directory contents from multiple file providers, with the first provider taking precedence for duplicate file names.
* Combines change notifications from the configured file providers.

## How to Use

Use `CompositeFileProvider` when files can come from multiple file providers, such as physical and embedded file providers. Providers are queried in the order they are supplied, so provider ordering determines precedence when the same file exists in more than one provider.

## Main Types

The main type provided by this library is:

* `Microsoft.Extensions.FileProviders.CompositeFileProvider`

## Additional Documentation

* [File providers in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/file-providers)
* [Detect changes with change tokens](https://learn.microsoft.com/aspnet/core/fundamentals/change-tokens)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.fileproviders.compositefileprovider)

## Related Packages

* File provider abstractions: [Microsoft.Extensions.FileProviders.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Abstractions/)
* File provider for physical files: [Microsoft.Extensions.FileProviders.Physical](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Physical/)
* File provider for embedded resources: [Microsoft.Extensions.FileProviders.Embedded](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Embedded/)

## Feedback & Contributing

Microsoft.Extensions.FileProviders.Composite is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
