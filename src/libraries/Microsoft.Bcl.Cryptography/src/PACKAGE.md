## About

This library provides some cryptographic types and functionality for .NET Standard and .NET Framework. This library is not necessary nor recommended when targeting versions of .NET that include the relevant support.

## Key Features

* Enables the use of some cryptographic functionality on older .NET platforms.

## How to Use

This package should only be used by platforms where the desired functionality is not built-in.

```C#
using System.Security.Cryptography;

internal static class Program
{
    private static void Main()
    {
        byte[] key = LoadKey();
        SP800108HmacCounterKdf kbkdf = new(key, HashAlgorithmName.SHA256);
        byte[] derivedKey = kbkdf.DeriveKey("label"u8, "context"u8, derivedKeyLengthInBytes: 32);
    }
}
```

## Main Types

The main types provided by this library are:

* `System.Security.Cryptography.SP800108HmacCounterKdf`

## Additional Documentation

* [API documentation](https://learn.microsoft.com/dotnet/api/System.Security.Cryptography)

## Feedback & Contributing

Microsoft.Bcl.Cryptography is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
