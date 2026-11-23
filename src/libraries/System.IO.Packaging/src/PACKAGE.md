## About

`System.IO.Packaging` reads and writes packages that follow the Open Packaging Conventions (OPC). A package groups named parts and their relationships into a single ZIP-based container.

## Key Features

* Opens and creates packages using files or streams.
* Reads and writes individual parts with their own content types.
* Manages relationships between parts and other resources.

## How to Use

Create a package in memory, write a text part, and reopen the package to read it. Dispose the first package to write its contents to the stream before reopening it.

```csharp
using System;
using System.IO;
using System.IO.Packaging;

using var buffer = new MemoryStream();
Uri partUri = PackUriHelper.CreatePartUri(new Uri("message.txt", UriKind.Relative));

using (Package package = Package.Open(buffer, FileMode.Create, FileAccess.ReadWrite))
{
    PackagePart part = package.CreatePart(partUri, "text/plain");
    using var writer = new StreamWriter(part.GetStream(FileMode.Create, FileAccess.Write));
    writer.Write("Hello from a package.");
}

buffer.Position = 0;
using Package reopened = Package.Open(buffer, FileMode.Open, FileAccess.Read);
using var reader = new StreamReader(reopened.GetPart(partUri).GetStream(FileMode.Open, FileAccess.Read));
string message = reader.ReadToEnd();
Console.WriteLine(message);
```

## Main Types

* `Package`: Opens and creates packages.
* `PackagePart`: Represents a data stream in a package and its content type.
* `PackageRelationship`: Represents a link from a package or a part to another resource.
* `PackUriHelper`: Creates and works with package and part URIs.

## Additional Documentation

* [API documentation](https://learn.microsoft.com/dotnet/api/system.io.packaging)

## Feedback & Contributing

This library is [MIT licensed](https://licenses.nuget.org/MIT). Report issues or contribute at the [.NET Runtime repository](https://github.com/dotnet/runtime).
