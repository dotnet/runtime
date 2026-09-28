## About

Provides `Index` and `Range` types to simplify slicing operations on collections for .NET Framework and .NET Standard 2.0.
Provides `Base64Url` for encoding data in a URL-safe manner on older .NET platforms.
Provides `Utf8` for converting chunked data between UTF-8 and UTF-16 encodings on .NET Framework and .NET Standard 2.0.

This library is not necessary nor recommended when targeting versions of .NET that include the relevant support.

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
