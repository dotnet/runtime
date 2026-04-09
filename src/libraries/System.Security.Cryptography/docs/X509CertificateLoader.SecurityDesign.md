# Security Design for X509CertificateLoader

## Summary

The _raison d'etre_ of `X509CertificateLoader` is to separate the potentially expensive work of processing a PKCS#12/PFX from the relatively unexpensive work of processing other certificate formats.

## General Security Posture

`X509CertificateLoader` is designed to be used in making a trust decision, therefore it is expected to generally operate on the untrusted side of a trust boundary.
Therefore, it treats all input (other than control settings like `Pkcs12LoaderLimits`) as potentially hostile.

## File Formats

The now-legacy constructors (`X509Certificate2..ctor(byte[])`, `X509Certificate..ctor(byte[])`, and similar input-processing mechanisms) employed content-sniffing to determine if the input was X.509, PKCS#7 SignedData, PKCS#12 PFX, a Windows SerializedCert, a Windows SerializedStore, or an Authenticode-signed file.
While most of these formats can be processed in _O(n)_ time, based on the length of the input, the PKCS#12 PFX format contains work-factor values within it, meaning that no running-time assumptions can be made for a context-free call to `new X509Certificate2(bytes)`.

Rather than single PKCS#12 PFX out as the one thing to remove, the design of X509CertificateLoader is to limit each method group to the support of a single file format.

## File Encodings

Both the X.509 file format and the PKCS#7 SignedData format support a binary (DER/BER) and a textual (PEM) representation.
None of the rest of the formats supported by the now-legacy constructors support a textual representation.

### Encoding-Sniffing

Any method group on `X509CertificateLoader` which is processing a format that supports both a PEM and a BER/DER encoding will sniff the encoding, rather than force users to be cognizant of the difference (if requested by users, we may in the future allow single-encoding variants of these methods).

If the first byte of the provided data (either file or in-memory representation) is `0x30` then the data will be interpreted in the binary encoding, otherwise it will be interpreted in the PEM encoding.

Callers implementing a protocol or file format where only one of the two encodings is acceptable will need to do their own validation.

### Binary Encoding

The now-legacy constructors and the `SignedCms.Decode` method all behave by reading data starting at the first byte, and stop at the last _semantic_ byte (as determined by the payload contents).
While strictness is a virtue, so is compatibility, and it is in the interest of compatibility (not rejecting inputs that are identified as the correct format by `X509Certificate2.GetCertContentType`) that `X509CertificateLoader` functions in the same manner.

That is to say, supposing the byte sequence `30 02 05 00` were a valid representation of a format supported by `X509CertificateLoader` (which it is not), the loader will have identicial behavior for that input and for `30 02 05 00 BE EF`, silently discarding the two trailing bytes.

Callers implementing a protocol or file format where the trailing data is unacceptable will need to perform their own content-length validation.

#### BER, CER, or DER

Each file format uses the binary encoding most appropriate to that format.
For example, ITU-T X.509 and IETF RFC 3280 both indicate that the `Certificate` data type must always use the ASN.1 Distinguished Encoding Rules (DER) restrictions; therefore `LoadCertificate` requires a DER encoding.
Alternatively, IETF RFC 7292 describes PKCS#12 PFX as being in the relaxed Basic Encoding Rules (BER) form, so `LoadPkcs12` only requires BER (whenever a nested structure within the PKCS#12 PFX requires a DER encoding, the DER encoding will be used).

Callers implementing a protocol or file format where either Canonical Encoding Rules (CER) or DER are required where the format is specified only as BER will need to perform their own restricted-encoding validation.

### Textual Encoding

For each file format that has a textual encoding, once `X509CertificateLoader` has moved into loading the textual form it does so by treating the input as ASCII and finding the first validly encoded PEM envelope with a label appropriate to the data type.
A PEM envelope's validity is determined by the IETF RFC 7468 "lax" profile, which does not permit attributes.

So, `LoadCertificate` will skip over the PKCS7 content and only try loading the CERTIFICATE content in a payload like

```
-----BEGIN PKCS7-----
base64==
-----END PKCS7-----
-----BEGIN CERTIFICATE-----
base64==
-----END CERTIFICATE-----
```

The textual encoding loaders will ignore any text outside the relevant envelope, and as they treat the data as 8-bit ASCII rather than UTF-8, there is no notion of "invalid" data outside the envelope.

## PKCS#12 PFX Considerations

IETF RFC 7292 describes the file format for a PKCS#12 PFX.
This format has a number of visible aspects that increase the cost of work, a few non-obvious aspects that increase the cost of work, and a few items that manifest as "quirks" on Windows.
The general stance of `X509CertificateLoader` is to allow the caller to specify limits on anything that controls the amount of work that will be done while loading the PKCS#12 PFX, other than the length of the payload itself (which is already available to the caller).
As most callers will have no real idea as to what these limits should be, the default experience when using this API will be to make use of the limits represented in `Pkcs12LoaderLimits.Default`.
These defaults are a balance of retaining support for expected "normal" inputs and constraining work to reduce the impact of malicious inputs, and may change over time.

The `Pkcs12LoaderLimits` class, the mechanism that allows callers to control the amount of work performed, exposes a special sentinel value, `DangerousNoLimits`.
If the caller provides the `DangerousNoLimits` value then .NET's PFX loader is bypassed, and the contents are sent directly to `PFXImportCertStore` (on Windows).
Callers can also provide a value that is property-wise equivalent to `DangerousNoLimits`, but such a value will still execute the .NET PFX loader, albiet with no upper bound on the amount of work to perform.

Whenever a PKCS#12 PFX load is being rejected for exceeding the work limit, an instance of the dedicated exception type `Pkcs12LoadLimitExceededException` (which derives from `CryptographicException`) is thrown.
This exception will contain the name of the property whose limit was exceeded in its message (or, if multiple limits were exceeded simultaneously, the limit deemed most appropriate), but this is purely for diagnostic purposes.

### RFC 7292 Section 4, MacData

The MacData structure contains a field, `iterations`, that specifies the number of iterations to use when running the Key Derivation Function (KDF) to turn the user-provided password into the MAC key.
Callers can limit the number of iterations via the `MacIterationLimit` property, which defaults (as of .NET 9) to 300,000 iterations.

The MacData structure also, via the `mac` field, can specify the hash algorithm underlying the PKCS12 KDF algorithm.
While the performance of the KDF utilizing SHA-1 may be different than when utilizing SHA-2-512, `X509CertificateLoader` considers them be be close enough in their performance characteristics that no filtering or weighting is done on the algorithm.

### RFC 7292 Section 4.1, AuthenticatedSafe

The PKCS#12 PFX AuthenticatedSafe top-level structure allows for the SafeContents values it contains to have one of three confidentiality modes: plaintext (PKCS#7/CMS Data), password (PKCS#7/CMS EncryptedData), and public key (PKCS#7/CMS EnvelopedData).
Public-key decryption requires a different API shape than password-based decryption, and as such is not currently supported.

The password-based confidentiality mode allows specifying a symmetric encryption algorithm, a KDF algorithm, and the hash algorithm that underlies the chosen KDF algorithm.
As with MacData, it is acknowledged that not all algorithms have the exact same performance characteristics, but as all of the choices are _O(n)_ with respect to the amount of data to process, and _n_ is bounded by the length of the payload, there are no controls to restrict or weight any algorithm choices.

Password-based confidentiality also specifies the number of iterations to run for the chosen KDF.
Callers can limit the number of iterations for the KDF on a single encrypted SafeContents via the `IndividualKdfIterationLimit` property (which .NET 9 defaults to 300,000).
As there can be more than one password-based SafeContents, callers can limit the total number of iterations across all KDFs via the `TotalKdfIterationLimit` property (which .NET 9 defaults to 1,000,000).
The `TotalKdfIterationLimit` property is also shared with ShroudedKeyBag entries, there are not separate controls for totals for SafeContents vs ShroudedKeyBags.  If either the `IndividualKdfIterationLimit` or the `TotalKdfIterationLimit` would be exceeded when decrypting a SafeContents value, an exception is thrown BEFORE doing the work associated with the limit.

Callers also have the option of skipping encrypted SafeContents altogether by setting `IgnoreEncryptedAuthSafes` to `true`.
When skipped in this manner, the KDF iteration counts are not evaluated for either the individual limit nor the total limit, and the SafeContents value is not decrypted.

### RFC 7292 Section 4.2, SafeBag

IETF RFC 7292 defines 6 types of SafeBag, as well as leaving it as an open set.
Of these 6 types, `X509CertificateLoader` only ever evaluates 3: KeyBag (PKCS#8 PrivateKeyInfo), PKCS8ShroudedKeyBag (PKCS#8 EncryptedPrivateKeyInfo), and CertBag.
`X509CertificateLoader` emphatically does not traverse into SafeContentsBag.

The three supported SafeBags are all tested to ensure that any attributes are unique.
SafeBag attributes are encoded in a manner similar to the X.500 Attribute type, a collection of values that each have an Object Identifier and a set of values.
The `X509CertificateLoader` attribute filter requires a) that no two PKCS12Attribute values on the same SafeBag have the same `attrId` value, and b) that no PKCS12Attribute value has more than one value in `attrValues` (an empty set is permitted).

SafeBags that are skipped because `IgnoreEncryptedAuthSafes` is asserted are not checked for duplicate attributes.
SafeBags of a supported type that are skipped for any other reason, such as `IgnorePrivateKeys` being asserted, are still checked for duplicates and the load operation will fail if duplicates are found.

Within `X509CertificateLoader`, the duplicate attribute filter can be disabled via the (non-public) `AllowDuplicateAttributes` property.
This property is set to `true` when loading from the legacy constructors, and can be indirectly specified by callers, such as with the `DangerousNoLimits` value.

#### RFC 7292 Section 4.2.1, KeyBag

`X509CertificateLoader` discards all attributes off of a KeyBag by default, except for 1.2.840.113549.1.9.21 (PKCS#9 LocalKeyId) and 1.3.6.1.4.1.311.17.2 (Microsoft `szOID_LOCAL_MACHINE_KEYSET`).

Callers can selectively enable preservation of 1.2.840.113549.1.9.20 (PKCS#9 FriendlyName) with `PreserveKeyName`, 1.3.6.1.4.1.311.17.1 (Microsoft `szOID_PKCS_12_KEY_PROVIDER_NAME_ATTR`) with `PreserveStorageProvider`, or any attribute without an explicit mapped property with `PreserveUnknownAttributes`.

The Microsoft `szOID_LOCAL_MACHINE_KEYSET` is not controlled via the `Pkcs12LoaderLimits` type, but rather by the `X509KeyStorageFlags` enumeration.
Callers who specify `X509KeyStorageFlags.MachineKeySet` instruct the PKCS#12 PFX loader to treat _all_ keys as if this empty attribute-set were present.
Callers who specify `X509KeyStorageFlags.UserKeySet` instruct the loader to treat _all_ keys as if this empty attribute-set were absent.
The behavior of specifying both flags is not determined by .NET, but rather Windows PFXImportCertStore.
If neither flag is sepecified (the default), then it is considered on a per-key basis.

When called via the now-legacy constructors, `PreserveKeyName` and `PerserveStorageProvider` are set to `true`, otherwise their default is `false`.

For the impact of these attributes, see below.

KeyBag values can be entirely disregarded, if the caller so wishes, by setting `IgnorePrivateKeys` to `true`.

#### RFC 7292 Section 4.2.2, PKCS8ShroudedKeyBag

PKCS8ShroudedKeyBag contains a PKCS#8 EncryptedPrivateKeyInfo value.
EncryptedPrivateKeyInfo specifies the symmetric encryption algorithm, the KDF algorithm, and the hash underlying the KDF algorithm.
As with the treatment of these specifiers when processing encrypted SafeContents values in the AuthenticationSafe value, `X509CertificateLoader` does not allow restricting the algorithms and does not consider any algorithm choices to be more or less expensive than any other.

EncryptedPrivateKeyInfo also specifies the number of iterations to run along with the KDF.
As with encrypted SafeContents values within the AuthenticationSafe value, callers can control the KDF iteration limit with both `IndividualKdfIterationLimit` and `TotalKdfIterationLimit`.
`TotalKdfIterationLimit` is shared across all encrypted SafeContents values and all PKCS8ShroudedKeyBag values.

Attributes are filtered off of PKCS8ShroudedKeyBag exactly as they are for KeyBag.

PKCS8ShroudedKeyBag values can be entirely disregarded, if the caller so wishes, by setting `IgnorePrivateKeys` to `true`.

#### RFC 7292 Section 4.2.3, CertBag

In addition to X.509 public key certificates, the CertBag type can also hold an SDSI certificate (or a future certificate type).
CertBags with a certId value other than 1.2.840.113549.1.9.22.1 (X.509 public key certificate) are ignored (but are still subject to duplicate attribute rejection).

`X509CertificateLoader` discards all attributes off of a CertBag by default, except for 1.2.840.113549.1.9.21 (PKCS#9 LocalKeyId).

Callers can selectively enable preservation of 1.2.840.113549.1.9.20 (PKCS#9 FriendlyName) with `PreserveCertificateAlias`, or any attribute without an explicit mapped property with `PreserveUnknownAttributes`.

When called via the now-legacy constructors, `PreserveCertificateAlias` is set to `true`, otherwise the default is `false`.

#### Combining Certificates and Keys

As part of loading a PKCS#12 PFX, users want the `X509Certificate2` instances to know about their corresponding private key (whenever it was also present in the payload).
While loading each individual certificate is _O(n)_ work (as a function of the length of the encoded certificate), and loading each individual key is _O(n)_ work (as a function of the length of the encoded key) (except PKCS8ShroudedKeyInfo, above),
the algorithm for matching keys to certificates is fundamentally _O(c * k)_ work.
To constrain this work, `Pkcs12LoaderLimits` has two options, `MaxKeys` and `MaxCertificates`.

In .NET 9, direct usage of `X509CertificateLoader` defaults both of these values to 200.
When importing a certificate via the now-legacy constructors, these settings are both treated as **unlimited**, although the underlying loader may impose limits of its own.

#### The Effects of Attributes on Windows

When `PreserveStorageProvider` is asserted, `X509CertificateLoader` allows the 1.3.6.1.4.1.311.17.1 (Microsoft `szOID_PKCS_12_KEY_PROVIDER_NAME_ATTR`) attribute to be passed down for keys.
Windows uses this attribute to determine which CAPI Cryptographic Storage Provider or CNG Key Storage Provider to utilize for the key import process.
When the attribute is missing, or identifies a CSP/KSP that is not found, Windows chooses a default provider (for all current OS versions the default is the CNG Microsoft Software Key Storage Provider).
By excluding this value from import by default, `X509CertificateLoader` implicitly upgrades keys that would otherwise have been imported into CAPI, and also prevents the contents from specifying any "nuisance" values, such as trying to save an RSA key into a legacy DSA CSP.

When `PreserveKeyName` is asserted, `X509CertificateLoader` allows the 1.2.840.113549.1.9.20 (PKCS#9 FriendlyName) attribute to be passed down for keys.
Windows uses this attribute as the preferred value to name any key loaded without `X509KeyStorageFlags.EphemeralKeySet`.
If the key name is already in use for the specified provider, or the attribute is not present, Windows will generate a random key name.
The built-in software key storage providers do not claim the key name atomically, which means that the same PKCS#12 PFX being loaded in parallel (or two loads otherwise specifying the same key name) can end up in situations where certificate instances in multiple processes both claim ownership over a given key and will erase the key out from under each other; this race condition can also result in confusing states where an RSA certificate in one process gets matched against an ECDSA key from a different import.
By excluding this value from import by default, `X509CertificateLoader` reduces the chances of such collision to the chances of `CreateUuid` returning the same answer across processes (effectively zero).

Because `X509CertificateLoader` does not filter out 1.3.6.1.4.1.311.17.2 (Microsoft `szOID_LOCAL_MACHINE_KEYSET`) by default, importing a PKCS#12 PFX on Windows defaults to creating each key in the user scope or machine scope based on the payload contents.
Unexpectedly saving the key into the machine scope can result in over-sharing on a multi-user system, or failure to import (e.g. Access Denied).
Unexpectedly saving the key into the user scope can result in difficult to understand operational errors, where a system administrator sees a key in the LocalMachine\My store behaving properly, but it fails when running in a web service.
Callers can force all keys to go into the user scope, or the machine scope, by specifying `X509KeyStorageFlags.UserKeySet` or `X509KeyStorageFlags.MachineKeySet`, respectively.

When `PreserveCertificateAlias` is asserted, `X509CertificateLoader` allows the 1.2.840.113549.1.9.20 (PKCS#9 FriendlyName) attribute to be passed down for certificates.
When this attribute is present, Windows assigns the value to the `CERT_FRIENDLY_NAME_PROP_ID` property for the certificate, which manifests in .NET via `X509Certificate2.FriendlyName`

### Key Persistence

`X509CertificateLoader` supports three different models of persistence:

* **Ephemeral**: The key is not written to disk at all.
  * Any call to cert.GetRSAPrivateKey() (or other algorithms) will keep the key alive in memory until the returned object is disposed or finalized, otherwise
  * The key is erased from memory when the certificate is disposed or finalized.
  * This mode is selected when the caller specifies `X509KeyStorageFlags.EphemeralKeySet`
  * This mode is always used on Linux, iOS, and Android, as there is no key persistence model there.
  * This mode does not work on macOS (exception when requested).
* **Persisted**: The key is written to disk, and stays written until manually (or externally) removed.
  * This mode is selected when the caller specifies `X509KeyStorageFlags.PersistKeySet`
  * This mode is not supported on Linux (option is ignored), iOS (exception when requested), or Android (exception when requested).
  * On macOS this causes keys to be written into the user's default keychain instead of a temporary keychain.
* **Default**, also known as Temporary, also known as Ephemerally-Persisted, also known as "Perphemeral": The key is written to disk, then cleaned up as part of garbage collection.
  * Key cleanup requires that the certificate instance is either disposed or finalized.
    * As .NET Core / .NET 5+ does not guarantee finalization of objects between Main exiting and process termination, key cleanup is not guaranteed.
    * Pending key cleanup will not occur if the process terminates abnormally, including full system/kernel panic and power-loss.
  * On Linux, iOS, and Android this mode does not exist, they always use Ephemeral.
  * This mode is selected when neither `EphemeralKeySet` nor `PersistedKeySet` are specified.

In .NET 8 and older on Windows, key cleanup was sometimes tracked by the managed instance (`X509Certificate(2)` constructor) and sometimes by a property associated with the underlying `CERT_CONTEXT` value (`X509Certificate(2)Collection.Import`).
When the cleanup was tracked by a `CERT_CONTEXT` value it meant every independent managed instances tracking the same `CERT_CONTEXT` value attemped to clean up the key, which occasionally caused confusion from premature key erasure.

`X509CertificateLoader` always tracks the cleanup on the instance returned from `LoadPkcs12` or the individual instances returned with `LoadPkcs12Collection`.
As the legacy import routines are written in terms of X509CertificateLoader in .NET 9, they have changed to only tracking the cleanup on the instance(s) returned from import.

### Single-Certificate Loading

The `LoadPkcs12Collection`/`LoadPkcs12CollectionFromFile` methods load all of the X.509 public key certificates from a PKCS#12 PFX and produce them as a collection.

The `LoadPkcs12`/`LoadPkcs12FromFile` methods do more or less the same amount of work to process the PKCS#12 PFX data, then only return a single certificate instance:
the first instance (using the enumerated order of the collection import) which has an associated private key,
or (if no certificates had associated private keys) the first instance,
or (if there were no certificates at all) the method throws an exception.

Unless importing with `EphemeralKeySet`, this may result in multiple keys being written to disk.
Any such keys that were not associated with the return value will be erased from disk before the method returns.

### KDF Iteration Limits vs. Unreferenced Keys

`X509CertificateLoader` follows the "lazy decryption" scheme utilized by Windows 10 PFXImportCertStore.
If a PKCS#12 PFX contains one CertBag value and two PKCS8ShroudedKeyBag values, and the CertBag shares a PKCS#9 LocalKeyId value with only one of the PKCS8ShroudedKeyBag values (and the private key contained therein is properly matched to the public key embedded in the certificate), then the second encrypted key will never be decrypted.
However, the presence of this key counts against both the `IndividualKdfIterationLimit` and the `TotalKdfIterationLimit`, as the work-counting phase is done before key-matching or key-decryption.

### Zero-Length Passwords

The KDF defined for PKCS#12 makes a distinction between a null password and an empty password, where most callers fail to see a distinction (particularly when the password is presented as `ReadOnlySpan<char>`).
As such, most API that deals with reading from a PKCS#12 PFX (e.g. the now-legacy constructors, Windows' PFXImportCertStore, and OpenSSL's `PKCS12_parse`) will use whichever of the two versions works first.

`X509CertificateLoader` handles this state by always trying the input as-provided first, then will make one allowance that the file was built using the other zero-length password.
In the event of this password mulligan, the first work done with the wrong password is not counted toward "total" work limits.

If a PKCS#12 PFX has been created with password-based integrity (the optional MAC, which is applied by almost every PFX generator) then whichever of the two zero-length forms validates the MAC will be used for the remainder of the import process.
Thus, when a MAC is present, the decryption work is still bounded by `TotalKdfIterationLimit` but the MAC work is bounded by `2 * MacIterationLimit`.

If a PKCS#12 PFX lacks a MAC, but has one or more encrypted SafeContents values within the PFX AuthenticatedSafe; then decryption of the first AuthenticatedSafe will try both passwords (assuming the first one fails).
If neither zero-length password works the import process will fail, but if the second one succeeded the import process will continue.
In the event that the second password worked, the decryption phase is bounded by `TotalKdfIterationLimit + IndividualKdfIterationLimit`, because `TotalKdfIterationLimit` represents the ideal interpretation of the file, not the actual work done.

In the degenerate case where a PKCS#12 PFX lack a MAC, has only unencrypted SafeContents values, and contains PKCS8ShroudedKeyBag values; the total KDF iteration count instead becomes bounded by `2 * TotalKdfIterationLimit`.
While the expected upper bound is `TotalKdfIterationLimit + IndividualKdfIterationLimit`, the PKCS8ShroudedKeyBag values may be provided as-is to a backend PKCS#12 importer (such as Windows' PFXImportCertStore), and .NET cannot guarantee that the backend performs a single global fallback versus attempting both passwords for every single encrypted item.
Callers which are unwilling to accept work up to double `TotalKdfIterationLimit` for a single import need to either reject zero-length passwords, or inspect the contents of the PKCS#12 PFX manually (such as via the `Pkcs12Info` class) to ensure that it is not in this degenerate state.

## Loading From Files

Partially based on the history of `new X509Certificate2(path)` supporting extracting the signer certificate from Authenticode-signed files, `X509CertificateLoader` is generally sensitive to the notion that it may be asked to open a large file that is not valid for the requested type.
In general, `X509CertificateLoader` tries to avoid temporary buffers that will be allocated via the .NET Large Object Heap.

### LoadCertificateFromFile

* Windows: The file path is passed into CryptQueryObject, loading happens in Windows.
* Linux: The file is opened with `BIO_new_file`, then read with `d2i_X509_bio`. If that fails, `PEM_read_bio_X509_AUX` is attempted.
* macOS:
  * If the file exceeds 1 MiB (1,048,576 bytes) it is opened as a memory-mapped file and processed as bytes.
  * Otherwise, the file is read into a CryptoPool-rented array and processed as bytes.
* iOS: Large files are not expected, the file is read into a CryptoPool-rented array and processed as bytes.
* Android: Large files are not expected, the file is read into a CryptoPool-rented array and processed as bytes.

### LoadPkcs12FromFile / LoadPkcs12CollectionFromFile

The first few bytes of the file are read.
If the initial bytes do not plausibly represent a BER encoded structure that would fit within the file length, an exception is thrown without further I/O.
If the initial bytes indicate a total structure size in excess of 1 MiB (1,048,576 bytes) the file is re-opened as a memory-mapped file (without closing it) and processed as bytes;
otherwise, the file is read into a CryptoPool-rented array and processed as bytes.

## Assumptions

### Immutability of Input Data

Loading is not always done in a single pass.
Data passed to the functions accepting byte-based input is expected to be unchanging from the beginning of the import process to the end of the import process.
Modification to the data concurrent with the import process has no defined behavior.

### Caller-checked Lengths

Even linear work is eventually expensive, so callers are expected to have reasonable ingestion size limits in place for potentially hostile data.
This includes both the length of the data to interpret (or the size of the file when specified by path), and of a password provided for importing a PKCS#12 PFX.

### Underlying Performance

Except in regards to payload-specified work discussed in relation to the processing of PKCS#12 PFX data or otherwise described herein,
the system libraries and other .NET libraries utilized in performing these load operations are expected to have _O(n)_ performance based on the length of input provided.

`HashSet<string>` is used to detect duplicate attribute values, it is assumed to have _O(n log n)_ performance.
20 MiB of short-prefix OIDs (2.0, 2.1, 2.2, ..., 2.3847491) can be tested in ~530ms,
20 MiB of long-prefix OIDs (1.2.840.113549.1.9.0, 1.2.840.113549.1.9.1, ... 1.2.840.113549.1.9.1614464) can be tested in ~480ms.
As the expected maximum number of attributes on a single item is 4, and hostile inputs should have a length check, no special limit is needed on Pkcs12LoaderLimits.

# Appendix A: API Technical Impact

## X509CertificateLoader

* LoadCertificate: Loads a single X.509 public key certificate, DER or PEM, based on input data.
* LoadCertificateFromFile: Loads a single X.509 public key certificate, DER or PEM, based on an input file path.
* LoadPkcs12: Loads a PKCS#12 PFX from input data and extracts a single X.509 public key certificate, possibly with a private key.
* LoadPkcs12FromFile: Loads a PKCS#12 PFX from an input path and extracts a single X.509 public key certificate, possibly with a private key.
* LoadPkcs12: Loads a PKCS#12 PFX from input data and extracts all X.509 public key certificates, each possibly with a private key.
* LoadPkcs12FromFile: Loads a PKCS#12 PFX from an input path and extracts all X.509 public key certificates, each possibly with a private key.

## X509KeyStorageFlags to PKCS#12 routines

* DefaultKeySet: No semantic meaning, just a named zero-value.
* UserKeySet: Suppresses `szOID_LOCAL_MACHINE_KEYSET` on all PKCS8ShroudedKeyBag and KeyBag items.
  * Windows: Calls to PFXImportCertStore will be performed with `CRYPT_USER_KEYSET` set.
  * Other: Ignored.
* MachineKeySet: Treats all PKCS8ShroudedKeyBag and KeyBag items as if `szOID_LOCAL_MACHINE_KEYSET` was present, even when not.
  * Windows: Calls to PFXImportCertStore will be performed with `CRYPT_USER_KEYSET` set.
  * Other: Ignored.
* Exportable:
  * Windows: Calls to PFXImportCertStore will be performed with `CRYPT_EXPORTABLE` set.
  * Linux: Ignored, keys are always exportable.
  * macOS: Keys will be imported as exportable.
  * iOS: Not supported, throws.
  * Android: Ignored, keys are always exportable.
* UserProtected:
  * Windows: Calls to PFXImportCertStore will be performed with `CRYPT_USER_PROTECTED` set.
  * Other: Ignored.
* PersistKeySet: Keys are written to disk, and stay on disk after the associated X509Certificate2 instance is freed.
  * Linux: Ignored.
  * Android: Not supported, throws.
  * Other: performs as described.
* EphemeralKeySet: Keys are never written to disk, in memory lifetime is the maximum of the associated X509Certificate2 instance or any reference to the key (whichever is longer).
  * macOS: Not supported, throws.
  * iOS: Not supported, throws.
  * Other: performs as described.

## Pkcs12LoaderLimits to PKCS#12 routines

* static Defaults: Provides an immutable instance of the limits utilized when no other limits are specified.
  * Is defined in terms of the behavior of the default constructor for the type.
* static DangerousNoLimits: Provides an immutable instance of a lack of limits. Has special ReferenceEquals semantics on Windows to jump straight to PFXImportCertStore with no .NET-side filtering.
* MacIterationLimit: Specifies the maximum number of permissible iterations for the KDF for the PFX MAC (if present), or null for no limit.  Default is 300,000.
* IndividualKdfIterationLimit: Specifies the maximum number of permissible iterations for the KDF to a single PKCS#8 EncryptedPrivateKey or a single encrypted SafeContents value, or null for no limit.  Default is 300,000.
* TotalKdfIterationLimit: Specifies the maximum number of permissible iterations for the KDFs across all PKCS#8 EncryptedPrivateKey and encrypted SafeContents values, or null for no limit.  Default is 1,000,000.
* MaxKeys: Specifies the maximum total number of PKCS8ShroudedKeyBag and KeyBag values permitted, or null for no limit.  Default is 200.
* MaxCertificates: Specifies the maximum number of supported CertBag values permitted, or null for no limit.  Default is 200.
* PreserveStorageProvider: `true` to respect `szOID_PKCS_12_KEY_PROVIDER_NAME_ATTR` on PKCS8ShroudedKeyBag and KeyBag values; otherwise, `false`.  Default is `false`.
  * Has no effect on platforms other than Windows.
* PreserveKeyName: `true` to respect PKCS#9 FriendlyName on PKCS8ShroudedKeyBag and KeyBag values; otherwise, `false`.  Default is `false`.
  * Has no effect on platforms other than Windows.
* PreserveCertificateAlias: `true` to respect PKCS#9 FriendlyName on CertBag values; otherwise, `false`.  Default is `false`.
  * Has no effect on platforms other than Windows.
* PreserveUnknownAttributes: `true` to pass any attribute without a dedicated option to the underlying loader; `false` to remove them prior to sending to the underlying loader.  Default is `false`.
  * There are currently no known attributes which would produce any effect from this setting.
* IgnorePrivateKeys: `true` to ignore any PKCS8ShroudedKeyBag or KeyBag values; otherwise, `false`.  Default is `false`.
* IgnoreEncryptedAuthSafes: `true` to ignore any SafeContents values within the PFX Authenticated Safe that are encrypted; otherwise, `false`.  Default is `false`.

