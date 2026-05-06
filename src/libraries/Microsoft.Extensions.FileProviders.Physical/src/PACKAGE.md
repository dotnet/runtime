## About

<!-- A description of the package and where one can find more documentation -->

Provides an implementation of a physical file provider, facilitating file access and monitoring on the disk. The primary type, [`PhysicalFileProvider`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.fileproviders.physicalfileprovider), enables the lookup of files on disk and can watch for changes either via `FileSystemWatcher` or polling mechanisms.


## Key Features

<!-- The key features of this package -->

* Easy access and monitoring of files on the disk.
* Ability to watch for file changes either by using `FileSystemWatcher` or through polling.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

This library can be used to look up files on disk and monitor file changes effectively.
Below is an example of how to use the `PhysicalFileProvider` to access files on disk and monitor changes:

```c#
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

using var provider = new PhysicalFileProvider(AppContext.BaseDirectory);

Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");

var contents = provider.GetDirectoryContents(string.Empty);
foreach (PhysicalFileInfo fileInfo in contents)
{
    Console.WriteLine(fileInfo.PhysicalPath);
}

var changeToken = provider.Watch("*.txt");
changeToken.RegisterChangeCallback(_ => Console.WriteLine("Text file changed"), null);

Console.ReadLine();
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.FileProviders.PhysicalFileProvider`
* `Microsoft.Extensions.FileProviders.PhysicalDirectoryInfo`
* `Microsoft.Extensions.FileProviders.PhysicalFileInfo`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/aspnet/core/fundamentals/file-providers#physical-file-provider)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.fileproviders.physical)

## Related Packages

<!-- The related packages associated with this package -->

* Abstractions of files and directories: [Microsoft.Extensions.FileProviders.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Abstractions/)
* File system globbing to find files matching a specified pattern: [Microsoft.Extensions.FileSystemGlobbing](https://www.nuget.org/packages/Microsoft.Extensions.FileSystemGlobbing/)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.FileProviders.Physical is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
