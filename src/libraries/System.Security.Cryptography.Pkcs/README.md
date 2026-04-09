# System.Security.Cryptography.Pkcs

This assembly provides support for non-primitive cryptographic operations based on ASN.1 BER/CER/DER encoded values.
The "Pkcs" assembly and namespace names come from the ["Public Key Cryptography Standards"](https://en.wikipedia.org/wiki/PKCS) specification set, though not all types in this assembly correspond to a PKCS document.

* Cryptographic Message Syntax (CMS, the successor to PKCS#7): [SignedCms](https://learn.microsoft.com/dotnet/api/system.security.cryptography.pkcs.signedcms), [EnvelopedCms](https://learn.microsoft.com/dotnet/api/system.security.cryptography.pkcs.envelopedcms)
* Private Key Information Syntax (PKCS#8): [Pkcs8PrivateKeyInfo](https://learn.microsoft.com/dotnet/api/system.security.cryptography.pkcs.pkcs8privatekeyinfo)
* Various object attributes (PKCS#9): [Pkcs9AttributeObject](https://learn.microsoft.com/dotnet/api/system.security.cryptography.pkcs.pkcs9attributeobject)-derived types
* "Personal inFormation eXchange" (PFX, PKCS#12): [Pkcs12Info](https://learn.microsoft.com/dotnet/api/system.security.cryptography.pkcs.pkcs12info), [Pkcs12Builder](https://learn.microsoft.com/dotnet/api/system.security.cryptography.pkcs.pkcs12builder)
* IETF RFC 3161 Timestamp Tokens (not a PKCS document): [Rfc3161TimestampToken](https://learn.microsoft.com/dotnet/api/system.security.cryptography.pkcs.rfc3161timestamptoken)

Some other elements from the PKCS series exist in [System.Security.Cryptography](../System.Security.Cryptography/):
* PKCS#1 is [RSA](https://learn.microsoft.com/dotnet/api/system.security.cryptography.rsa)
* PKCS#10 is the export/import format for [CertificateRequest](https://learn.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.certificaterequest)

## Contribution Bar

- [x] [We consider new features, new APIs and performance changes](../README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../README.md#secondary-bars)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is:issue+is:open+label:area-System.Security+label:%22help+wanted%22) issues.

## Source

* The source code for this assembly is in the [src](src/) subdirectory.
* Crytographic primitives are in the [System.Security.Cryptography](../System.Security.Cryptography/) assembly.
* Lower-level ASN.1 BER/CER/DER parsing is in the [System.Formats.Asn1](../System.Formats.Asn1/) assembly.

## Deployment

The library is shipped as a [NuGet package](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs).
