## About

Provides `Index` and `Range` types to simplify slicing operations on collections for .NET Framework and .NET Standard 2.0.
Provides `Base64Url` for encoding data in a URL-safe manner on older .NET platforms.
Provides `Utf8` for converting chunked data between UTF-8 and UTF-16 encodings on .NET Framework and .NET Standard 2.0.

This library is not necessary nor recommended when targeting versions of .NET that include the relevant support.

## Target Framework Support

The APIs provided by this package are included natively in the following target frameworks:

* `Index` and `Range`: .NET Core 3.0+, .NET 5+, and .NET Standard 2.1+
* `System.Text.Unicode.Utf8`: .NET 5+
* `System.Buffers.Text.Base64Url`: .NET 9+

## When You May Still Need This Package

While this package is generally not needed for modern .NET applications, there are scenarios where it may still be required:

### Multi-Targeting Scenarios

If your library multi-targets and includes target frameworks that lack native support for these APIs (such as .NET Framework or .NET Standard 2.0), you should reference this package for **all** target frameworks. This ensures consistent behavior and prevents issues where transitive dependencies might not have access to these types.

For example, a library targeting both `netstandard2.0` and `net8.0` should include the package reference for both targets, even though `net8.0` has native support.

### Transitive Dependency Considerations

In some cases, you may need to add a direct reference to this package to resolve issues with transitive dependencies that expect these types to be available. This typically occurs when:

- A dependency library multi-targets but doesn't include this package for all target frameworks
- You're consuming a library that relies on these types but targets an older framework

In such cases, adding a top-level reference to Microsoft.Bcl.Memory can resolve missing type issues and ensure proper type forwarding.

## Key Features

<!-- The key features of this package -->

* Enables the use of `Index` and `Range` types on .NET Framework and .NET Standard 2.0.
* Provides `Base64Url` encoding, decoding, and validation for URL-safe data processing on older .NET platforms.
* Provides `Utf8` encoding, decoding, and validation for chunked data between UTF-8 and UTF-16 on .NET Framework and .NET Standard 2.0.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The `Index` and `Range` types simplify working with slices of arrays, strings, or other collections.

```csharp
string[] words = ["The", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog"];

// Use Index to reference the last element
Console.WriteLine(words[^1]);
// Output: "dog"

// Use Range to reference a slice
string[] phrase = words[1..4];
Console.WriteLine(string.Join(" ", phrase));
// Output: "quick brown fox"
```

`Base64Url` encoding is a URL-safe version of Base64, commonly used in web applications, such as JWT tokens.

```csharp
using System.Buffers.Text;
using System.Text;

// Original data
byte[] data = Encoding.UTF8.GetBytes("Hello World!");

Span<byte> encoded = new byte[Base64Url.GetEncodedLength(data.Length)];
Base64Url.EncodeToUtf8(data, encoded, out int _, out int bytesWritten);

string encodedString = Base64Url.EncodeToString(data);  
Console.WriteLine($"Encoded: {encodedString}");
// Encoded: SGVsbG8gV29ybGQh

Span<byte> decoded = new byte[data.Length];
Base64Url.DecodeFromUtf8(encoded[..bytesWritten], decoded, out _, out bytesWritten);

string decodedString = Encoding.UTF8.GetString(decoded[..bytesWritten]);
Console.WriteLine($"Decoded: {decodedString}");
// Decoded: Hello World!
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Index`
* `System.Range`
* `System.Buffers.Text.Base64Url`
* `System.Text.Unicode.Utf8`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

API documentation

* [System.Index](https://learn.microsoft.com/dotnet/api/system.index)
* [System.Range](https://learn.microsoft.com/dotnet/api/system.range)
* [System.Buffers.Text.Base64Url](https://learn.microsoft.com/dotnet/api/system.buffers.text.base64url)
* [System.Text.Unicode.Utf8](https://learn.microsoft.com/dotnet/api/system.text.unicode.utf8)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Bcl.Memory is released as open source under the [MIT license](https://licenses.nuget.org/MIT).
Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
