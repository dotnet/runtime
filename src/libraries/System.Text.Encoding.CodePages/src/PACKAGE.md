## About

`System.Text.Encoding.CodePages` enable creating single and double bytes encodings for code pages that otherwise are available only in the desktop .NET Framework.

## Key Features

* Support single and double byte encodings for code pages that are not available in .NET Core.

## How to Use

```C#
using System.Text;

// Register the CodePages encoding provider at application startup to enable using single and double byte encodings.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Now can create single and double byte encodings for code pages that are not available in .NET Core.
Encoding windows1252Encoding = Encoding.GetEncoding(1252); // Western European (Windows)
byte[] encodedBytes = windows1252Encoding.GetBytes("String to encode");

```

## Main Types

The main types provided by this library are:

* `CodePagesEncodingProvider`

## Additional Documentation

* [API documentation](https://learn.microsoft.com/dotnet/api/system.text.codepagesencodingprovider)

## Related Packages

* [System.Text.Encodings.Web](https://www.nuget.org/packages/System.Text.Encodings.Web)

## Feedback & Contributing

System.Text.Encoding.CodePages is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).