# System.Security.Cryptography.Cose

This assembly provides support for CBOR Object Signing and Encryption (COSE), initially defined in [IETF RFC 8152](https://www.ietf.org/rfc/rfc8152.html).

The primary types in this assembly are

* Signing
  * Single Signer (`COSE_Sign1`): [CoseSign1Message](https://learn.microsoft.com/dotnet/api/system.security.cryptography.cose.cosesign1message)
  * Multi-Signer (`COSE_Sign`): [CoseMultiSignMessage](https://learn.microsoft.com/dotnet/api/system.security.cryptography.cose.cosemultisignmessage)

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.security.cryptography.cose

## Contribution Bar

- [x] [We consider new features, new APIs and performance changes](../README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../README.md#secondary-bars)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is:issue+is:open+label:area-System.Security+label:%22help+wanted%22) issues.

## Source

* The source code for this assembly is in the [src](src/) subdirectory.
* Crytographic primitives are in the [System.Security.Cryptography](../System.Security.Cryptography/) assembly.
* Lower-level CBOR parsing is in the [System.Formats.Cbor](../System.Formats.Cbor/) assembly.

## Deployment

The library is shipped as a [NuGet package](https://www.nuget.org/packages/System.Security.Cryptography.Cose).
