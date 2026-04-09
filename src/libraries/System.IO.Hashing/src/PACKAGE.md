## About

<!-- A description of the package and where one can find more documentation -->

System.IO.Hashing offers a variety of hash code algorithms.

Hash code algorithms are pivotal for generating unique values for objects based on their content, facilitating object comparisons, and detecting content alterations.
The namespace encompasses algorithms like CRC-32, CRC-64, xxHash3, xxHash32, xxHash64, and xxHash128, all engineered for swift and efficient hash code generation, with xxHash being an "Extremely fast hash algorithm".

**Warning**: The hash functions provided by System.IO.Hashing are not suitable for security purposes such as handling passwords or verifying untrusted content.
For such security-critical applications, consider using cryptographic hash functions provided by the [System.Security.Cryptography](https://learn.microsoft.com/dotnet/api/system.security.cryptography) namespace.

## Key Features

<!-- The key features of this package -->

* Variety of hash code algorithms including CRC-32, CRC-64, xxHash3, xxHash32, xxHash64, and xxHash128.
* Implementations of CRC-32 and CRC-64 algorithms, as used in IEEE 802.3, and described in ECMA-182, Annex B respectively.
* Implementations of XxHash32 for generating 32-bit hashes, XxHash3 and XxHash64 for generating 64-bit hashes, and xxHash128 for generating 128-bit hashes.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Creating hash codes is straightforward.
Call the `Hash` method with the content to be hashed.

Here is a practical example:

```csharp
using System;
using System.IO.Hashing;

byte[] data = new byte[] { 1, 2, 3, 4 };

byte[] crc32Value = Crc32.Hash(data);
Console.WriteLine($"CRC-32 Hash: {BitConverter.ToString(crc32Value)}");
// CRC-32 Hash: CD-FB-3C-B6

byte[] crc64Value = Crc64.Hash(data);
Console.WriteLine($"CRC-64 Hash: {BitConverter.ToString(crc64Value)}");
// CRC-64 Hash: 58-8D-5A-D4-2A-70-1D-B2

byte[] xxHash3Value = XxHash3.Hash(data);
Console.WriteLine($"XxHash3 Hash: {BitConverter.ToString(xxHash3Value)}");
// XxHash3 Hash: 98-8B-7B-90-33-AC-46-22

byte[] xxHash32Value = XxHash32.Hash(data);
Console.WriteLine($"XxHash32 Hash: {BitConverter.ToString(xxHash32Value)}");
// XxHash32 Hash: FE-96-D1-9C

byte[] xxHash64Value = XxHash64.Hash(data);
Console.WriteLine($"XxHash64 Hash: {BitConverter.ToString(xxHash64Value)}");
// XxHash64 Hash: 54-26-20-E3-A2-A9-2E-D1

byte[] xxHash128Value = XxHash128.Hash(data);
Console.WriteLine($"XxHash128 Hash: {BitConverter.ToString(xxHash128Value)}");
// XxHash128 Hash: 49-A0-48-99-59-7A-35-67-53-76-53-A0-D9-95-5B-86
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.IO.Hashing.Crc32`
* `System.IO.Hashing.Crc64`
* `System.IO.Hashing.XxHash3`
* `System.IO.Hashing.XxHash32`
* `System.IO.Hashing.XxHash64`
* `System.IO.Hashing.XxHash128`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.io.hashing)
* [xxHash - Extremely fast hash algorithm](https://github.com/Cyan4973/xxHash/blob/release/doc/xxhash_spec.md)

## Related Packages

<!-- The related packages associated with this package -->

Cryptographic services, including secure encryption and decryption of data: [System.Security.Cryptography](https://learn.microsoft.com/dotnet/api/system.security.cryptography)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.IO.Hashing is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
