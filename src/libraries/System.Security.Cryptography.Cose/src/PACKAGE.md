## About

<!-- A description of the package and where one can find more documentation -->

Provides support for CBOR Object Signing and Encryption (COSE) as defined in [RFC 9052](https://www.rfc-editor.org/rfc/rfc9052) and [RFC 8152](https://www.rfc-editor.org/rfc/rfc8152).
COSE is a standard for creating digitally signed, encrypted, and MAC'd data structures using the Concise Binary Object Representation (CBOR) format.

## Key Features

<!-- The key features of this package -->

* Create and validate single-signer COSE messages (`COSE_Sign1`).
* Create and validate multi-signer COSE messages (`COSE_Sign`).
* Sign and verify messages with embedded or detached content.
* Support for ECDSA (ES256, ES384, ES512) and RSA (PS256, PS384, PS512) algorithms.
* Configure protected and unprotected header parameters using `CoseHeaderMap`.
* Support for synchronous and asynchronous streaming signing and verification.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Signing and verifying an embedded message with a single signer (`COSE_Sign1`):

```csharp
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Cose;
using System.Text;

// Generate or load a signing key
using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
CoseSigner signer = new(ecdsa, HashAlgorithmName.SHA256);

// Sign content with embedded payload
byte[] message = Encoding.UTF8.GetBytes("Hello, COSE!");
byte[] encoded = CoseSign1Message.SignEmbedded(message, signer);

// Decode and verify the message
CoseSign1Message sign1Message = CoseMessage.DecodeSign1(encoded);
bool isValid = sign1Message.VerifyEmbedded(ecdsa);

Console.WriteLine($"Valid signature: {isValid}");
if (sign1Message.Content.HasValue)
{
    Console.WriteLine($"Payload: {Encoding.UTF8.GetString(sign1Message.Content.Value.Span)}");
}
```

Signing and verifying detached content:

```csharp
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Cose;
using System.Text;

using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
CoseSigner signer = new(ecdsa, HashAlgorithmName.SHA256);

byte[] message = Encoding.UTF8.GetBytes("Detached payload data");

// Sign with detached content (payload is not included in the encoded message)
byte[] encoded = CoseSign1Message.SignDetached(message, signer);

// Decode and verify by providing the detached payload
CoseSign1Message sign1Message = CoseMessage.DecodeSign1(encoded);
bool isValid = sign1Message.VerifyDetached(ecdsa, message);

Console.WriteLine($"Valid detached signature: {isValid}");
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Security.Cryptography.Cose.CoseSign1Message`
* `System.Security.Cryptography.Cose.CoseMultiSignMessage`
* `System.Security.Cryptography.Cose.CoseMessage`
* `System.Security.Cryptography.Cose.CoseSigner`
* `System.Security.Cryptography.Cose.CoseSignature`
* `System.Security.Cryptography.Cose.CoseHeaderMap`
* `System.Security.Cryptography.Cose.CoseHeaderLabel`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.security.cryptography.cose)
* [RFC 9052: CBOR Object Signing and Encryption (COSE): Structures and Process](https://www.rfc-editor.org/rfc/rfc9052)
* [RFC 9053: CBOR Object Signing and Encryption (COSE): Initial Algorithms](https://www.rfc-editor.org/rfc/rfc9053)

## Related Packages

* [System.Formats.Cbor](https://www.nuget.org/packages/System.Formats.Cbor/)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Security.Cryptography.Cose is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
