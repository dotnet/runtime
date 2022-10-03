Cross-Platform Cryptography
===========================

Cryptographic operations in .NET are performed by existing system libraries.
As with most technological decisions, there are various pros and cons.
Since the system already has a vested interest in making the cryptography libraries safe from security vulnerabilities,
and already has an update mechanism that system administrators should be using, .NET gets to benefit from this reliability.
Users who have requirements to use FIPS-validated algorithm implementations also get that benefit for free (when the system
libraries are FIPS-validated, of course).
The biggest con is that not all system libraries offer the same capabilities.
While the core capabilities are present across the various platforms, there are some rough edges.

## Hash Algorithms

Hash algorithms, and HMAC algorithms, are very standard bytes-in-bytes-out operations.
All hash algorithm (and HMAC) classes in .NET Core defer to the system libraries (including the \*Managed classes).

While the various system libraries may have different performance, there should not be concerns of compatibility.

In the future there is a possibility that new hash algorithms may be added to .NET Core before one (or more) supported platforms have system support for the algorithm.
This would result in a `PlatformNotSupportedException` when invoking the `Create()` method for the algorithm.

## Symmetric Encryption

The underlying ciphers and chaining are performed by the system libraries.

| Cipher + Mode | Windows 8 - 10     | Windows 7            | Linux              | macOS              |
|---------------|--------------------|----------------------|--------------------|--------------------|
| AES-CBC       | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark: |
| AES-ECB       | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark: |
| AES-CFB8*     | :white_check_mark: | :white_check_mark:** | :white_check_mark: | :white_check_mark: |
| AES-CFB128*   | :white_check_mark: | :x:                  | :white_check_mark: | :white_check_mark: |
| 3DES-CBC      | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark: |
| 3DES-ECB      | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark: |
| 3DES-CFB8*    | :white_check_mark: | :white_check_mark:** | :white_check_mark: | :white_check_mark: |
| 3DES-CFB64*   | :white_check_mark: | :x:                  | :white_check_mark: | :white_check_mark: |
| DES-CBC       | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark: |
| DES-ECB       | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark: |
| DES-CFB8*     | :white_check_mark: | :x:                  | :white_check_mark: | :white_check_mark: |

\* Available starting in .NET 5.

** Available starting in .NET 6.

In the future there is a possibility that new ciphers may be added to .NET Core before one (or more) supported platforms have system support for it.
This would result in a `PlatformNotSupportedException` when invoking the `Create()` method for the algorithm.

In the future there is a possibility that new cipher/chaining modes may be added to .NET Core before one (or more) supported platforms have system support for it.
This would result in a `PlatformNotSupportedException` when invoking the `CreateEncryptor()` or `CreateDecryptor()` methods for the algorithm (or overloads to those methods).

In the future there is a possibility that new cipher/chaining modes may be added to .NET Core and that these new modes may not apply to all symmetric algorithms.
This would likely result in a `NotSupportedException` when using the set-accessor of the `Mode` property on the `SymmetricAlgorithm` object, but this prediction is subject to change.

## Authenticated Encryption

Authenticated Encryption (AE) support is provided for AES-CCM and AES-GCM via `System.Security.Cryptography.AesCcm` and `System.Security.Cryptography.AesGcm`, respectively.

On macOS the system libraries do not support AES-CCM or AES-GCM for 3rd party code, so the AesCcm and AesGcm classes use OpenSSL for support.
Users on macOS need to obtain an appropriate copy of OpenSSL (libcrypto) for these types to function, and it must be in a path that the system would load a library from by default.
Obtaining OpenSSL from a package manager, such as Homebrew, is recommended (but not required).
The libcrypto.0.9.7.dylib and libcrypto.0.9.8.dylib libraries included in macOS are from older versions of OpenSSL, and will not be used.
The libcrypto.35.dylib, libcrypto.41.dylib, and libcrypto.42.dylib are LibreSSL, and will not be used.

On Windows and Linux the implementations of AES-CCM and AES-GCM are provided by the system libraries.

### AES-GCM

#### Key Sizes

AES-GCM works with 128, 192, and 256-bit keys.

#### Nonce Sizes

The AesGcm class only supports 96-bit (12 byte) nonces.

#### Tag Sizes

The AesGcm class supports creating (or processing) 96, 104, 112, 120, and 128-bit (12-16 bytes) tags.

### AES-CCM

#### Key Sizes

AES-CCM works with 128, 192, and 256-bit keys.

#### Nonce Sizes

The AesCcm class supports 56, 64, 72, 80, 88, 96, and 104-bit nonces (7-13 bytes).

#### Tag Sizes

The AesCcm class supports creating (or processing) 32, 48, 64, 80, 96, 112, and 128-bit (4, 8, 10, 12, 14, or 16-byte) tags.

## Asymmetric Cryptography

### RSA

RSA key generation is performed by the system libraries, and is subject to size limitations and performance characteristics thereof.
RSA key operations are performed by the system libraries, and the types of key that may be loaded are subject to system requirements.

.NET Core does not expose "raw" (unpadded) RSA operations, and .NET Core relies on the system libraries for encryption (and decryption) padding.
Not all platforms support the same padding options.

| Padding Mode | Windows (CNG) | Linux (OpenSSL) | macOS | Windows (CAPI) |
|--------------|---------------|-----------------|-------|----------------|
| PKCS1 Encryption | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| OAEP - SHA-1 | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| OAEP - SHA-2 (SHA256, SHA384, SHA512) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :x: |
| PKCS1 Signature (MD5, SHA-1) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| PKCS1 Signature (SHA-2) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :question: |
| PSS | :white_check_mark: | :white_check_mark: | :white_check_mark: | :x: |

Windows CAPI is capable of PKCS1 signature with a SHA-2 algorithm, but the individual RSA object may be loaded in a CSP which does not support it.

#### RSA on Windows

 * Windows CNG is used on Windows whenever `new RSACng()` is used.
 * Windows CAPI is used on Windows whenever `new RSACryptoServiceProvider()` is used.
 * The object returned by `RSA.Create()` is internally powered by Windows CNG, but this is an implementation detail subject to change.
 * The `GetRSAPublicKey()` extension method for X509Certificate2 will currently always return an RSACng instance, but this could change as the platform evolves.
 * The `GetRSAPrivateKey()` extension method for X509Certificate2 will currently prefer an RSACng instance, but if RSACng cannot open the key RSACryptoServiceProvider will be attempted.
   * In the future other providers could be preferred over RSACng.

#### Native Interop

.NET Core exposes types to allow programs to interoperate with the system libraries upon which the .NET cryptography code is layered.
The types involved do not translate between platforms, and should only be directly used when necessary.

| Type | Windows | Linux | macOS |
|------|---------|-------|-------|
| RSACryptoServiceProvider | :white_check_mark: | :question: | :question: |
| RSACng | :white_check_mark: | :x: | :x: |
| RSAOpenSsl | :x: | :white_check_mark: | :question: |

RSAOpenSsl on macOS works if OpenSSL is installed in the system and an appropriate libcrypto dylib can be found via dynamic library loading, otherwise exceptions will be thrown.

On non-Windows systems RSACryptoServiceProvider can be used for compatibility with existing programs, but a `PlatformNotSupportedException` will be thrown from any method which requires system interop, such as opening a named key.

### ECDSA

ECDSA key generation is performed by the system libraries, and is subject to size limitations and performance characteristics thereof.
ECDSA key curves are defined by the system libraries, and are subject to the limitations thereof.

| EC Curve | Windows 10 | Windows 7 - 8.1 | Linux | macOS |
|----------|------------|-------|-------|-----------------|
| NIST P-256 (secp256r1) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| NIST P-384 (secp384r1) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| NIST P-521 (secp521r1) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| brainpool curves (as named curves) | :white_check_mark: | :x: | :question: | :x: |
| other named curves | :question: | :x: | :question: | :x: |
| explicit curves | :white_check_mark: | :x: | :white_check_mark: | :x: |
| Export or import as explicit | :white_check_mark: | :x: | :white_check_mark: | :x: |

Support for named curves was added to Windows CNG in Windows 10, and is not available in prior OSes, with the exception of the three curves which had special support in Windows 7.
See [CNG Named Elliptic Curves](https://msdn.microsoft.com/en-us/library/windows/desktop/mt632245(v=vs.85).aspx) for the expected support.

Not all Linux distributions have support for the same named curves.

Exporting with explicit curve parameters requires system library support which is not available on macOS or older versions of Windows.

#### Native Interop

.NET Core exposes types to allow programs to interoperate with the system libraries upon which the .NET cryptography code is layered.
The types involved do not translate between platforms, and should only be directly used when necessary.

| Type | Windows | Linux | macOS |
|------|---------|-------|-------|
| ECDsaCng | :white_check_mark: | :x: | :x: |
| ECDsaOpenSsl | :x: | :white_check_mark: | :question: |

ECDsaOpenSsl on macOS works if OpenSSL is installed in the system and an appropriate libcrypto dylib can be found via dynamic library loading, otherwise exceptions will be raised.

### ECDiffieHellman (ECDH)

ECDH key generation is performed by the system libraries, and is subject to size limitations and performance characteristics thereof.

The ECDiffieHellman class will not return the "raw" value of the ECDH computation, all returned data is done in terms of key derivation functions.
* HASH(Z)
* HASH(prepend || Z || append)
* HMAC(key, Z)
* HMAC(key, prepend || Z || append)
* HMAC(Z, Z)
* HMAC(Z, prepend || Z || append)
* Tls11Prf(label, seed)

ECDH key curves are defined by the system libraries, and are subject to the limitations thereof.

| EC Curve | Windows 10 | Windows 7 - 8.1 | Linux | macOS |
|----------|------------|-------|-------|-----------------|
| NIST P-256 (secp256r1) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| NIST P-384 (secp384r1) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| NIST P-521 (secp521r1) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| brainpool curves (as named curves) | :white_check_mark: | :x: | :question: | :x: |
| other named curves | :question: | :x: | :question: | :x: |
| explicit curves | :white_check_mark: | :x: | :white_check_mark: | :x: |
| Export or import as explicit | :white_check_mark: | :x: | :white_check_mark: | :x: |

Support for named curves was added to Windows CNG in Windows 10, and is not available in prior OSes, with the exception of the three curves which had special support in Windows 7.
See [CNG Named Elliptic Curves](https://msdn.microsoft.com/en-us/library/windows/desktop/mt632245(v=vs.85).aspx) for the expected support.

Not all Linux distributions have support for the same named curves.

Exporting with explicit curve parameters requires system library support which is not available on macOS or older versions of Windows.

#### Native Interop

.NET Core exposes types to allow programs to interoperate with the system libraries upon which the .NET cryptography code is layered.
The types involved do not translate between platforms, and should only be directly used when necessary.

| Type | Windows | Linux | macOS |
|------|---------|-------|-------|
| ECDiffieHellmanCng | :white_check_mark: | :x: | :x: |
| ECDiffieHellmanOpenSsl | :x: | :white_check_mark: | :question: |

ECDiffieHellmanOpenSsl on macOS works if OpenSSL is installed in the system and an appropriate libcrypto dylib can be found via dynamic library loading, otherwise exceptions will be raised.

### DSA

DSA key generation is performed by the system libraries, and is subject to size limitations and performance characteristics thereof.

| Function | Windows CNG | Linux | macOS | Windows CAPI |
|----------|-------------|-------|-------|--------------|
| Key creation (<= 1024 bits) | :white_check_mark: | :white_check_mark: | :x: | :white_check_mark: |
| Key creation (> 1024 bits) | :white_check_mark: | :white_check_mark: | :x: | :x: |
| Loading keys (<= 1024 bits) | :white_check_mark: | :white_check_mark:  | :white_check_mark: | :white_check_mark: |
| Loading keys (> 1024 bits) | :white_check_mark: | :white_check_mark: | :question: | :x: |
| FIPS 186-2 | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| FIPS 186-3 (SHA-2 signatures) | :white_check_mark: | :white_check_mark: | :x: | :x: |

macOS seems to be capable of loading DSA keys whose size exceeds 1024-bit, but does not perform FIPS 186-3 behaviors with those keys, so the behavior of those keys is undefined.

#### DSA on Windows

 * Windows CNG is used on Windows whenever `new DSACng()` is used.
 * Windows CAPI is used on Windows whenever `new DSACryptoServiceProvider()` is used.
 * The object returned by `DSA.Create()` is internally powered by Windows CNG, but this is an implementation detail subject to change.
 * The `GetDSAPublicKey()` extension method for X509Certificate2 will currently always return an DSACng instance, but this could change as the platform evolves.
 * The `GetDSAPrivateKey()` extension method for X509Certiicate2 will currently prefer an DSACng instance, but if DSACng cannot open the key DSACryptoServiceProvider will be attempted.
   * In the future other providers could be preferred over DSACng.

#### Native Interop

.NET Core exposes types to allow programs to interoperate with the system libraries upon which the .NET cryptography code is layered.
The types involved do not translate between platforms, and should only be directly used when necessary.

| Type | Windows | Linux | macOS |
|------|---------|-------|-------|
| DSACryptoServiceProvider | :white_check_mark: | :question: | :question: |
| DSACng | :white_check_mark: | :x: | :x: |
| DSAOpenSsl | :x: | :white_check_mark: | :question: |

DSAOpenSsl on macOS works if OpenSSL is installed in the system and an appropriate libcrypto dylib can be found via dynamic library loading, otherwise exceptions will be raised.

On non-Windows systems RSACryptoServiceProvider can be used for compatibility with existing programs, but a `PlatformNotSupportedException` will be thrown from any method which requires system interop, such as opening a named key.

## X.509 Certificates

The majority of support for X.509 certificates in .NET Core comes from system libraries.
All certificates are required to be loaded by the underlying system library to be loaded into an `X509Certificate2` instance in .NET Core (or an `X509Certificate` instance).

### Reading a PKCS12/PFX

| Scenario | Windows | Linux | macOS |
|----------|---------|-------|-------|
| Empty | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| One certificate, no private key | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| One certificate, with private key | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| Multiple certificates, no private keys | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| Multiple certificates, one private key | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| Multiple certificates, multiple private keys | :white_check_mark: | :x: | :white_check_mark: |

### Writing a PKCS12/PFX

| Scenario | Windows | Linux | macOS |
|----------|---------|-------|-------|
| Empty | :white_check_mark: | :white_check_mark: | :x: |
| One certificate, no private key | :white_check_mark: | :white_check_mark: | :x: |
| One certificate, with private key | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| Multiple certificates, no private keys | :white_check_mark: | :white_check_mark: | :x: |
| Multiple certificates, one private key | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| Multiple certificates, multiple private keys | :white_check_mark: | :x: | :white_check_mark: |
| Ephemeral loading | :white_check_mark: | :white_check_mark: | :x: |

macOS cannot load certificate private keys without a keychain object, which requires writing to disk.
Keychains are created automatically for PFX loading, and are deleted when no longer in use.
Since the `X509KeyStorageFlags.EphemeralKeySet` option means that the private key should not be written to disk, asserting that flag on macOS results in a `PlatformNotSupportedException`.

### Writing a PKCS7 certificate collection

Windows and Linux both emit DER-encoded PKCS7 blobs. macOS emits indefinite-length-CER-encoded PKCS7 blobs.

### X509Store

On Windows the X509Store class is a representation of the Windows Certificate Store APIs, and work the same as they did on .NET Framework.
On Linux the X509Store class is a projection of system trust decisions (read-only), user trust decisions (read-write), and user key storage (read-write).
On macOS the X509Store class is a projection of system trust decisions (read-only), user trust decisions (read-only), and user key storage (read-write).

| Scenario | Windows | Linux | macOS |
|----------|---------|-------|-------|
| Open CurrentUser\My (ReadOnly) | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| Open CurrentUser\My (ReadWrite) | :white_check_mark: | :white_check_mark:  | :white_check_mark: |
| Open CurrentUser\My (ExistingOnly) | :white_check_mark: | :question: | :white_check_mark: |
| Open LocalMachine\My | :white_check_mark: | `CryptographicException` | :white_check_mark: |
| Open CurrentUser\Root (ReadOnly) | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| Open CurrentUser\Root (ReadWrite) | :white_check_mark: | :white_check_mark: | `CryptographicException` |
| Open CurrentUser\Root (ExistingOnly) | :white_check_mark: | :question: | :white_check_mark: (if ReadOnly) |
| Open LocalMachine\Root (ReadOnly) | :white_check_mark:  | :white_check_mark:  | :white_check_mark: |
| Open LocalMachine\Root (ReadWrite) | :white_check_mark: | `CryptographicException` | `CryptographicException` |
| Open LocalMachine\Root (ExistingOnly) | :white_check_mark: | :question: | :white_check_mark:  (if ReadOnly) |
| Open CurrentUser\Disallowed (ReadOnly) | :white_check_mark: | :question: | :white_check_mark: |
| Open CurrentUser\Disallowed (ReadWrite) | :white_check_mark: | :question: | `CryptographicException` |
| Open CurrentUser\Disallowed (ExistingOnly) | :white_check_mark: | :question: | :white_check_mark: (if ReadOnly) |
| Open LocalMachine\Disallowed (ReadOnly) | :white_check_mark: | `CryptographicException` | :white_check_mark: |
| Open LocalMachine\Disallowed (ReadWrite) | :white_check_mark: | `CryptographicException` | `CryptographicException` |
| Open LocalMachine\Disallowed (ExistingOnly) | :white_check_mark: | `CryptographicException` | :white_check_mark: (if ReadOnly) |
| Open non-existent store (ExistingOnly) | `CryptographicException` | `CryptographicException` | `CryptographicException` |
| Open CurrentUser non-existent store (ReadWrite)  | :white_check_mark: | :white_check_mark: | `CryptographicException` |
| Open LocalMachine non-existent store (ReadWrite)  | :white_check_mark: | `CryptographicException` | `CryptographicException` |

On Linux stores are created on first-write, and no user stores exist by default, so opening CurrentUser\My with ExistingOnly may fail.

On Linux the Disallowed store is not used in chain building, and attempting to add contents to it will result in a `CryptographicException` being thrown.
A `CryptographicException` will be thrown when opening the Disallowed store on Linux if it has already acquired contents.

The LocalMachine\Root store on Linux is an interpretation of the CA bundle in the default path for OpenSSL.
The LocalMachine\Intermediate store on Linux is an interpretation of the CA bundle in the default path for OpenSSL.
The CurrentUser\Intermediate store on Linux is used as a cache when downloading intermediate CAs by their Authority Information Access records on successful X509Chain builds.

On macOS the CurrentUser\My store is the user's default keychain (login.keychain, by default).
The LocalMachine\My store is System.keychain.
The CurrentUser\Root store on macOS is an interpretation of the SecTrustSettings results for the user trust domain.
The LocalMachine\Root store on macOS is an interpretation of the SecTrustSettings results for the admin and system trust domains.
The CurrentUser\Disallowed and LocalMachine\Disallowed stores are interpretations of the appropriate SecTrustSettings results for certificates whose trust is set to Always Deny.
Custom store creation on macOS with the X509Store API is supported only for CurrentUser location. It will create a new keychain with no password in the user's keychain
directory (~/Library/Keychains). To create a keychain with password a P/Invoke to SecKeychainCreate could be used. Similarly, SecKeychainOpen could be used to open keychains
in different locations. The resulting `IntPtr` can be passed to `new X509Store(IntPtr)` to obtain a read/write-capable store (subject to the current user's permissions).

### X509Chain

macOS does not support Offline CRL utilization, so `X509RevocationMode.Offline` is treated as `X509RevocationMode.Online`.
macOS does not support a user-initiated timeout on CRL/OCSP/AIA downloading, so `X509ChainPolicy.UrlRetrievalTimeout` is ignored.
