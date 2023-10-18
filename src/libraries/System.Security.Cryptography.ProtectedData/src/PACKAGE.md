## About

<!-- A description of the package and where one can find more documentation -->

System.Security.Cryptography.ProtectedData offers a simplified interface for utilizing Microsoft Windows DPAPI's [CryptProtectData](https://learn.microsoft.com/windows/win32/api/dpapi/nf-dpapi-cryptprotectdata) and [CryptUnprotectData](https://learn.microsoft.com/windows/win32/api/dpapi/nf-dpapi-cryptunprotectdata) functions.

This library is primarily aimed at ensuring the security of critical data, like user credentials or sensitive information, either while storing or transmitting.

**Note**: Since it relies on Windows DPAPI, this package is only suited for applications running on Windows platforms.

## Key Features

<!-- The key features of this package -->

* Built upon the robust and secure Windows Data Protection API (DPAPI).
* Data can be protected either for current process or for any process on the machine.
* Scope of protection can be defined either to the current user or the local machine.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Utilizing this package is quite simple, and it mainly revolves around two methods: `Protect` and `Unprotect`.

Here, `originalData` is the data you want to protect, `optionalEntropy` is an additional byte array used to increase encryption complexity, and `DataProtectionScope` specifies whether the data protection should apply to the current user or the machine.

To protect:

```csharp
using System.Security.Cryptography;

byte[] originalData = new byte[] { ... };
byte[] optionalEntropy = new byte[] { ... };

// To Encrypt data
byte[] encryptedData = ProtectedData.Protect(
    originalData,
    optionalEntropy,
    DataProtectionScope.CurrentUser);
```

To unprotect:

```csharp
using System.Security.Cryptography;

byte[] encryptedData = new byte[] { ... };
byte[] optionalEntropy = new byte[] { ... };

// To Decrypt data
byte[] originalData = ProtectedData.Unprotect(
    encryptedData,
    optionalEntropy,
    DataProtectionScope.CurrentUser);
```

## Main Types

<!-- The main types provided in this library -->

The main type provided by this library is:

* `System.Security.Cryptography.ProtectedData`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/standard/security/how-to-use-data-protection)
* [API documentation](https://learn.microsoft.com/dotnet/api/system.security.cryptography.protecteddata)
* [CNG DPAPI](https://learn.microsoft.com/windows/win32/seccng/cng-dpapi)

## Related Packages

<!-- The related packages associated with this package -->

For more complex cryptographic operations or cross-platform support, consider using:

* [System.Security.Cryptography.Algorithms](https://www.nuget.org/packages/System.Security.Cryptography.Algorithms/)
* [System.Security.Cryptography.Pkcs](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Security.Cryptography.ProtectedData is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
