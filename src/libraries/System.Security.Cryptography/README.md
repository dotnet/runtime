# System.Security.Cryptography

This assembly provides the core cryptographic support for .NET, including
hashing (e.g. [SHA256](https://learn.microsoft.com/dotnet/api/system.security.cryptography.sha256)),
symmetric-key message authentication (e.g. [HMACSHA256](https://learn.microsoft.com/dotnet/api/system.security.cryptography.hmacsha256)),
symmetric-key encryption (e.g. [Aes](https://learn.microsoft.com/dotnet/api/system.security.cryptography.aes)),
symmetric-key authenticated encryption (e.g. [AesGcm](https://learn.microsoft.com/dotnet/api/system.security.cryptography.aesgcm)),
asymmetric (public/private key) cryptography (e.g. [RSA](https://learn.microsoft.com/dotnet/api/system.security.cryptography.rsa), [ECDsa](https://learn.microsoft.com/dotnet/api/system.security.cryptography.ecdsa), or [ECDiffieHellman](https://learn.microsoft.com/dotnet/api/system.security.cryptography.ecdiffiehellman)),
and X.509 public-key certificates ([X509Certificate2](https://learn.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)).

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.security.cryptography and https://learn.microsoft.com/dotnet/api/system.security.cryptography.x509certificates

## Contribution Bar

- [x] [We consider new features, new APIs and performance changes](/src/libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](/src/libraries/README.md#secondary-bars)

When contributing to this area, please consider:

- We do not provide implementations for primitive cryptographic algorithms, and do not want to.
  - Some exceptions have been made in the past, but we do not anticipate any future exceptions.
- We generally do not add API support for an algorithm unless it is supported by at least two of
  - Microsoft Windows (via CNG)
  - OpenSSL
  - Apple macOS (via CommonCrypto or CryptoKit)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22+label%3Aarea-System.Security) issues.

## Source

* The managed code for core cryptographic support is in the [src](src/) subdirectory.
* Core cryptography involves a shim layer for performing [interop](/docs/coding-guidelines/interop-guidelines.md) on most of our operating systems.
  * Apple (macOS, iOS, et cetera): [System.Security.Cryptography.Native.Apple](/src/native/libs/System.Security.Cryptography.Native.Apple/)
  * Android: [System.Security.Cryptography.Native.Android](/src/native/libs/System.Security.Cryptography.Native.Android/)
  * Linux and "other UNIX" (via OpenSSL): [System.Security.Cryptography.Native](/src/native/libs/System.Security.Cryptography.Native/)
* Higher level cryptographic components may be in other assemblies, such as
  * [System.Security.Cryptography.Pkcs](../System.Security.Cryptography.Pkcs/)
  * [System.Security.Cryptography.Cose](../System.Security.Cryptography.Cose/)
* The lower-level ASN.1 BER/CER/DER parser is a separate library, [System.Formats.Asn1](../System.Formats.Asn1/)

## Deployment

The System.Security.Cryptography assembly is part of the shared framework, and ships with every new release of .NET.
