# Security Design for Post Quantum Cryptography (PQC) Primitive Types

## Affected Types

This document covers the following types:

* FIPS 203: Modular-Lattice Based Key Encapsulation Mechanism (ML-KEM)
  * System.Security.Cryptography.MLKem
  * System.Security.Cryptography.MLKemCng
  * System.Security.Cryptography.MLKemOpenSsl
* FIPS 204: Modular-Lattice Based Digital Signature Algorithm (ML-DSA)
  * System.Security.Cryptography.MLDsa
  * System.Security.Cryptography.MLDsaCng
  * System.Security.Cryptography.MLDsaOpenSsl
* FIPS 205: Stateless Hash-Based Digital Signature Algorithm (SLH-DSA)
  * System.Security.Cryptography.SlhDsa
  * System.Security.Cryptography.SlhDsaCng[^1]
  * System.Security.Cryptography.SlhDsaOpenSsl
* IETF LAMPS PQ Composite Sigs
  * System.Security.Cryptography.CompositeMLDsa
  * System.Security.Cryptography.CompositeMLDsaCng[^1]
  * System.Security.Cryptography.CompositeMLDsaOpenSsl[^1]

Any future public/private-key algorithms are expected to be designed along these same principles.

[^1]: This class does not yet exist because of a lack of underlying platform support, but when available the design will be unchanged.

## Basic Class Structure

* Each of the primitives has a base class defining the behavior of that algorithm. (e.g. System.Security.Cryptography.CompositeMLDsa)
* The base class is IDisposable.
  * Once disposed, the object instance is permanently unusable.
* The base class provides static (and thus non-inheritable) methods to generate a new key, or to import a key from a supported file format.
  * The key generation routine does not provide any defaults
    * FUTURE: If a successor version of a specification adds new options, the existing key generation routines may be required to default to the previous iteration's equivalent selection.
  * These key and import routines do not expose the underlying provider, that is held as an implementation detail.
    * The provider will always be a "natural" choice for the operating environment. (e.g. Windows CNG BCrypt on Windows, OpenSSL on Linux)
* Object instances have no mutable state, except for being IDisposable.
* The base class models all of the functionality from the represented algorithm. (e.g. Encapsulate and Decapsulate for ML-KEM)
* The base class models public and private key export routines for all well-defined key formats for the algorithm.
* The base class provides no `public virtual` members (or `public abstract`).
  * Inputs are validated to the extent possible, then dispatched to `protected virtual` or `protected abstract` members for processing.  (This is an aspect of the "Template Method Pattern")
* When a provider has a model for a persisted or externalized key, there is a derived type to interact with that model.
* All platform types use SafeHandle to avoid UAF
* All P/Invokes are done via `internal` methods accepting Span or ReadOnlySpan, and only break it down to pointer and length at the interop boundary.
* No `virtual` or `abstract` member will be invoked on a disposed instance.
  * Virtual methods may be invoked concurrently with disposal.
* Overloads of functions will exist for ease of use and other such design factors, but all funnel into a singular virtual/abstract method.
  * This is an aspect of the "Template Method Pattern"
* Encrypted PKCS#8 PrivateKey export is non-virtual, the base class guarantees that the caller-specified PBE parameters are respected.
  * Derived types can only export unencrypted PKCS#8, the platform performs the encryption.
* ExportSubjectPublicKeyInfo is never virtual, but always written by the base class in terms of the underlying public key format.
* The PKCS#8 export uses the Try-Write pattern to enable derived types that want to support writing attributes.
* No explicit thread-safety is utilized by these types, other than SafeHandles for managing the lifetime of native resources.
  * Any concurrency issues which result in native memory violations will be fixed.
  * Any concurrency issues that merely result in data corruption will not.

```C#
public abstract class ALGORITHM : IDisposable
{
    public static ALGORITHM GenerateKey(ALGORITHM_GENERATION_OPTIONS);
    public static ALGORITHM ImportPkcs8PrivateKey(...);
    public static ALGORITHM ImportEncryptedPkcs8PrivateKey(...);
    public static ALGORITHM ImportSubjectPublicKeyInfo(...);
    public static ALGORITHM ImportFromPem(...);
    public static ALGORITHM ImportFromEncryptedPem(...);
    public static ALGORITHM ImportALGORITHMSPECIFICPRIVATEKEYFORMAT(...);
    public static ALGORITHM ImportALGORITHMSPECIFICPUBLICKEYFORMAT(...);
    
    protected ALGORITHM(ALGORITHM_IDENTIFICATION);
    
    public void Dispose();
    protected virtual void Dispose(bool disposing);

    public ALGORITHM_IDENTIFICATION ...;
    // e.g. public MLDsaAlgorithm Algorithm { get; } for ML-DSA
    // to identify ML-DSA-44, ML-DSA-65, or ML-DSA-87, and information such as
    // the size of a signature.

    public ALGORITHM_PUBLIC_ANSWER ALGORITHM_PUBLIC_OP(...);
    protected abstract ALGORITHM_PUBLIC_ANSWER ALGORITHM_PUBLIC_OPCore(...);
    
    public ALGORITHM_PRIVATE_ANSWER ALGORITHM_PRIVATE_OP(...);
    protected abstract ALGORITHM_PRIVATE_ANSWER ALGORITHM_PRIVATE_OPCore(...);
    
    public byte[] ExportSubjectPublicKeyInfo();
    public byte[] ExportPkcs8PrivateKey();
    public byte[] ExportEncryptedPkcs8PrivateKey(...);
    public byte[] ExportALGORITHMSPECIFICPRIVATEKEYFORMAT(...);
    public byte[] ExportALGORITHMSPECIFICPUBLICKEYFORMAT(...);
    protected abstract void ExportALGORITHMSPECIFICPRIVATEKEYFORMATCore(Span<byte> destination, ...);
    protected abstract void ExportALGORITHMSPECIFICPUBLICKEYFORMATCore(Span<byte> destination, ...);
}
```

### Derived Class Structure

* Derived Classes are only used to model the underlying representation of the key.
* In addition to inherited/overriden behavior, the derived type SHOULD only expose two members:
  * A constructor, accepting the underlying key representation.
  * A method, producing the underlying key representation.
* The constructor MUST validate that the input is valid in context.

```C#
public sealed class ALGORITHMCng : ALGORITHM
{
    public ALGORITHMCng(CngKey key);
    public CngKey GetCngKey();
}

public sealed class ALGORITHMOpenSsl : ALGORITHM
{
    public ALGORITHMOpenSsl(SafeEvpPKeyHandle key);
    public SafeEvpPKeyHandle DuplicateKeyHandle();
}
```

## Key Import

### PEM

The PEM import routines for the PQC primitives follow the precedent of PEM import for AsymmetricAlgorithm types:

* PEM import will ignore content outside of a valid PEM encapsulation boundary, adhering to IETF RFC 7468.
* PEM import will ignore any content utilizing a label that is not known to the algorithm
* `ImportFromPem` considers `ENCRYPTED PRIVATE KEY` as a known label.
* PEM import will fail if not exactly one known label is encountered (i.e. zero, or more than one)
* `ImportFromPem` will fail if the only known label encountered is `ENCRYPTED PRIVATE KEY`.
  * Neither a NULL nor an empty password will be attempted.
* `ImportFromEncryptedPem` considers only `ENCRYPTED PRIVATE KEY` to be valid.
* As all currently known formats use BER encoding, PEM import will fail if the Base64-encoded data is other than a single BER-encoded value.
  * i.e. Non-BER will fail.
  * i.e. BER followed by trailing data will fail.
  * Some key formats require the more specific DER encoding, but the same payload restrictions apply.
* PEM import is valid for both public key formats and private key formats.
* `ImportFromEncryptedPem` defers work to `ImportEncryptedPkcs8PrivateKey` after decode.
* `ImportFromPem` defers work to an appropriate method, based on the label, after decode.
  * Thus there is no fuzzy matching, e.g. "BEGIN PUBLIC KEY" is only routed to ImportSubjectPublicKeyInfo

### Encrypted PKCS#8

Encrypted PKCS#8 imports follow in the precedent from AsymmetricAlgorithm.

* Decryption is done by .NET, not the underlying platform.
* Any symmetric algorithm that is supported by .NET on the current platform is allowable.
* Iteration-based KDFs are limited to 600,000 iterations.
* The decrypted value must be exactly one BER-encoded value.
* The decrypted value is then processed by ImportPkcs8PrivateKey.

### PKCS#8 PrivateKey

* Attributes are always ignored on import.
  * At this time, there are no known attributes which would apply to an ephemeral key for any of these algorithms.
  * The only attribute known to be used by any algorithm on any .NET platform is that Windows uses 2.5.29.15 (`id-ce-keyUsage`) to restrict RSA keys to sign-only or encrypt-only, or ECC keys to sign-only (to distinguish EC-DSA from EC-DH).
    * None of the PQC key classes have multiple usages, so the attribute as a restriction would not make sense.

## Algorithm/Class-Specific

### ML-KEM

```C#
public abstract partial class MLKem : IDisposable
{
    protected MLKem(MLKemAlgorithm algorithm);
    public MLKemAlgorithm Algorithm { get; }

    protected virtual void Dispose(bool disposing) { }
    protected abstract void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret);
    protected abstract void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret);
    protected abstract void ExportDecapsulationKeyCore(Span<byte> destination);
    protected abstract void ExportEncapsulationKeyCore(Span<byte> destination);
    protected abstract void ExportPrivateSeedCore(Span<byte> destination);
    protected abstract bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten);

    public static MLKem GenerateKey(MLKemAlgorithm algorithm);
    public static MLKem ImportDecapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source);
    public static MLKem ImportEncapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source);
    public static MLKem ImportPrivateSeed(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source);
}
public sealed partial class MLKemAlgorithm : IEquatable<MLKemAlgorithm>
{
    internal MLKemAlgorithm();
    public int CiphertextSizeInBytes { get; }
    public int DecapsulationKeySizeInBytes { get; }
    public int EncapsulationKeySizeInBytes { get; }
    public static MLKemAlgorithm MLKem1024 { get; }
    public static MLKemAlgorithm MLKem512 { get; }
    public static MLKemAlgorithm MLKem768 { get; }
    public string Name { get; }
    public int PrivateSeedSizeInBytes { get; }
    public int SharedSecretSizeInBytes { get; }
    public override bool Equals([NotNullWhen(true)] object? obj);
    public bool Equals([NotNullWhen(true)] MLKemAlgorithm? other);
    public override int GetHashCode();
    public static bool operator ==(MLKemAlgorithm? left, MLKemAlgorithm? right);
    public static bool operator !=(MLKemAlgorithm? left, MLKemAlgorithm? right);
    public override string ToString();
}
```

* As the ciphertext and shared secret have deterministic lengths for each key, the base class guarantees that the parameters are precisely sized when calling the virtual Encapsulate and Decapsulate methods, reducing risk of OOB-write (Encapsulate, Decapsulate) or OOB-read (Decapsulate)
* As the data size is known for the Private Seed, Decapsulation Key, and Encapsulation Key; the base class guarantees that the key parameter to the export virtual methods (for those formats) is exactly the correct size, reducing risk of OOB-write.
  * Import is not a virtual method.
* The key instances produced from MLKem.GenerateKey or an MLKem.Import routine will always perform PKCS#8 export based on the seed, when available, and the expanded encapsulation key otherwise.  Never the "both" choice.
* PKCS#8 import verifies that the seed and encapsulation key are a match when the "both" choice was utilized.
* Importing an Encapsulation Key or a Decapsulation Key requires the caller to specify the MLKemAlgorithm, rather than determining it from the input length, to avoid potential future ambiguity should a future parameter set have a key size matching a current one.

### ML-DSA

```C#
public abstract partial class MLDsa : IDisposable
{
    protected MLDsa(MLDsaAlgorithm algorithm);
    public MLDsaAlgorithm Algorithm { get; }

    protected virtual void Dispose(bool disposing) { }
    protected abstract void ExportMLDsaPrivateSeedCore(Span<byte> destination);
    protected abstract void ExportMLDsaPublicKeyCore(Span<byte> destination);
    protected abstract void ExportMLDsaPrivateKeyCore(Span<byte> destination);
    protected abstract void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination);
    protected abstract void SignMuCore(ReadOnlySpan<byte> externalMu, Span<byte> destination);
    protected abstract void SignPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, Span<byte> destination);
    protected abstract bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten);
    protected abstract bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature);
    protected abstract bool VerifyMuCore(ReadOnlySpan<byte> externalMu, ReadOnlySpan<byte> signature);
    protected abstract bool VerifyPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, ReadOnlySpan<byte> signature);
    
    public static MLDsa GenerateKey(MLDsaAlgorithm algorithm);
    public static MLDsa ImportMLDsaPrivateSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
    public static MLDsa ImportMLDsaPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
    public static MLDsa ImportMLDsaPrivateKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
}

public sealed partial class MLDsaAlgorithm : IEquatable<MLDsaAlgorithm>
{
    internal MLDsaAlgorithm();
    public static MLDsaAlgorithm MLDsa44 { get; }
    public static MLDsaAlgorithm MLDsa65 { get; }
    public static MLDsaAlgorithm MLDsa87 { get; }
    public string Name { get; }
    public int MuSizeInBytes { get; }
    public int PrivateSeedSizeInBytes { get; }
    public int PublicKeySizeInBytes { get; }
    public int PrivateKeySizeInBytes { get; }
    public int SignatureSizeInBytes { get; }
    public override bool Equals([NotNullWhen(true)] object? obj);
    public bool Equals([NotNullWhen(true)] MLDsaAlgorithm? other);
    public override int GetHashCode();
    public static bool operator ==(MLDsaAlgorithm? left, MLDsaAlgorithm? right);
    public static bool operator !=(MLDsaAlgorithm? left, MLDsaAlgorithm? right);
    public override string ToString();
}
```

* As the signature size is deterministic for each key, the base class guarantees that the signature parameter to the sign and verify virtual methods is exactly the correct size, reducing risk of OOB-write (sign) or OOB-read (verify).
* As the size of the mu value is deterministic for each key, the base class guarantees that the mu parameter to the sign and verify virtual methods is exactly the correct size, reducing risk of OOB-write (sign) or OOB-read (verify)
* As the data size is known for the Private Seed, Private Key, and Public Key; the base class guarantees that the key parameter to the export virtual methods (for those formats) is exactly the correct size, reducing risk of OOB-write.
  * Import is not a virtual method.
* The key instances produced from MLDsa.GenerateKey or an MLDsa.Import routine will always perform PKCS#8 export based on the seed, when available, and the expanded private key otherwise.  Never the "both" choice.
* PKCS#8 import verifies that the seed and private key are a match when the "both" choice was utilized.
* Importing a Private Key or a Public Key requires the caller to specify the MLDsaAlgorithm, rather than determining it from the input length, to avoid potential future ambiguity should a future parameter set have a key size matching a current one.
* To maximize compatibility, SignPreHash rejects calls when the algorithm OID is known, and the provided hash is not of the right length.
* To be consistent across platforms, SignPreHash rejects calls when the algorithm OID is unknown.

### SLH-DSA

```C#
public abstract partial class SlhDsa : IDisposable
{
    protected SlhDsa(SlhDsaAlgorithm algorithm);
    public SlhDsaAlgorithm Algorithm { get; }

    protected virtual void Dispose(bool disposing) { }
    protected abstract void ExportSlhDsaPublicKeyCore(Span<byte> destination);
    protected abstract void ExportSlhDsaPrivateKeyCore(Span<byte> destination);
    protected abstract void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination);
    protected abstract void SignPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, Span<byte> destination);
    protected abstract bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature);
    protected abstract bool VerifyPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, ReadOnlySpan<byte> signature);
    protected virtual bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten);

    public static SlhDsa GenerateKey(SlhDsaAlgorithm algorithm);
    public static SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
    public static SlhDsa ImportSlhDsaPrivateKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
}
public sealed partial class SlhDsaAlgorithm : IEquatable<SlhDsaAlgorithm>
{
    internal SlhDsaAlgorithm();
    public string Name { get; }
    public int PublicKeySizeInBytes { get; }
    public int PrivateKeySizeInBytes { get; }
    public int SignatureSizeInBytes { get; }
    public static SlhDsaAlgorithm SlhDsaSha2_128f { get; }
    public static SlhDsaAlgorithm SlhDsaSha2_128s { get; }
    public static SlhDsaAlgorithm SlhDsaSha2_192f { get; }
    public static SlhDsaAlgorithm SlhDsaSha2_192s { get; }
    public static SlhDsaAlgorithm SlhDsaSha2_256f { get; }
    public static SlhDsaAlgorithm SlhDsaSha2_256s { get; }
    public static SlhDsaAlgorithm SlhDsaShake128f { get; }
    public static SlhDsaAlgorithm SlhDsaShake128s { get; }
    public static SlhDsaAlgorithm SlhDsaShake192f { get; }
    public static SlhDsaAlgorithm SlhDsaShake192s { get; }
    public static SlhDsaAlgorithm SlhDsaShake256f { get; }
    public static SlhDsaAlgorithm SlhDsaShake256s { get; }
    public override bool Equals([NotNullWhen(true)] object? obj);
    public bool Equals([NotNullWhen(true)] SlhDsaAlgorithm? other);
    public override int GetHashCode();
    public static bool operator ==(SlhDsaAlgorithm? left, SlhDsaAlgorithm? right);
    public static bool operator !=(SlhDsaAlgorithm? left, SlhDsaAlgorithm? right);
    public override string ToString();
}
```

* As the signature size is deterministic for each key, the base class guarantees that the signature parameter to the sign and verify virtual methods is exactly the correct size, reducing risk of OOB-write (sign) or OOB-read (verify).
* As the data size is known for the Private Key and Public Key; the base class guarantees that the key parameter to the export virtual methods (for those formats) is exactly the correct size, reducing risk of OOB-write.
  * Import is not a virtual method.
* Private Key and Public Key are already ambiguous on size, but rather than encode that the sizes can be distinguished with a "SHA-2 or SHAKE" boolean and a "small or fast" boolean, they require the specific SlhDsaAlgorithm to avoid other future ambiguity.
* To maximize compatibility, SignPreHash rejects calls when the algorithm OID is known, and the provided hash is not of the right length.
* To be consistent across platforms, SignPreHash rejects calls when the algorithm OID is unknown.

### Composite ML-DSA

```C#
public abstract partial class CompositeMLDsa : IDisposable
{
    protected CompositeMLDsa(CompositeMLDsaAlgorithm algorithm);
    public CompositeMLDsaAlgorithm Algorithm { get; }

    protected virtual void Dispose(bool disposing) { }
    protected abstract int ExportCompositeMLDsaPrivateKeyCore(Span<byte> destination);
    protected abstract int ExportCompositeMLDsaPublicKeyCore(Span<byte> destination);
    protected abstract bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten);
    protected abstract int SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination, out int bytesWritten);
    protected abstract bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature);

    public static CompositeMLDsa GenerateKey(CompositeMLDsaAlgorithm algorithm);
    public static CompositeMLDsa ImportCompositeMLDsaPrivateKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
    public static CompositeMLDsa ImportCompositeMLDsaPublicKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
}
public sealed partial class CompositeMLDsaAlgorithm : IEquatable<CompositeMLDsaAlgorithm>
{
    internal CompositeMLDsaAlgorithm();
    public int MaxSignatureSizeInBytes { get; }
    public static CompositeMLDsaAlgorithm MLDsa44WithECDsaP256 { get; }
    public static CompositeMLDsaAlgorithm MLDsa44WithEd25519 { get; }
    public static CompositeMLDsaAlgorithm MLDsa44WithRSA2048Pkcs15 { get; }
    public static CompositeMLDsaAlgorithm MLDsa44WithRSA2048Pss { get; }
    public static CompositeMLDsaAlgorithm MLDsa65WithECDsaBrainpoolP256r1 { get; }
    public static CompositeMLDsaAlgorithm MLDsa65WithECDsaP256 { get; }
    public static CompositeMLDsaAlgorithm MLDsa65WithECDsaP384 { get; }
    public static CompositeMLDsaAlgorithm MLDsa65WithEd25519 { get; }
    public static CompositeMLDsaAlgorithm MLDsa65WithRSA3072Pkcs15 { get; }
    public static CompositeMLDsaAlgorithm MLDsa65WithRSA3072Pss { get; }
    public static CompositeMLDsaAlgorithm MLDsa65WithRSA4096Pkcs15 { get; }
    public static CompositeMLDsaAlgorithm MLDsa65WithRSA4096Pss { get; }
    public static CompositeMLDsaAlgorithm MLDsa87WithECDsaBrainpoolP384r1 { get; }
    public static CompositeMLDsaAlgorithm MLDsa87WithECDsaP384 { get; }
    public static CompositeMLDsaAlgorithm MLDsa87WithECDsaP521 { get; }
    public static CompositeMLDsaAlgorithm MLDsa87WithEd448 { get; }
    public static CompositeMLDsaAlgorithm MLDsa87WithRSA3072Pss { get; }
    public static CompositeMLDsaAlgorithm MLDsa87WithRSA4096Pss { get; }
    public string Name { get; }
    public override bool Equals([NotNullWhen(true)] object? obj);
    public bool Equals([NotNullWhen(true)] CompositeMLDsaAlgorithm? other);
    public override int GetHashCode();
    public static bool operator ==(CompositeMLDsaAlgorithm? left, CompositeMLDsaAlgorithm? right);
    public static bool operator !=(CompositeMLDsaAlgorithm? left, CompositeMLDsaAlgorithm? right);
    public override string ToString();
}
```

* Signature sizes are not deterministic for every algorithm, but all algorithms have a maximum size. The derived type will only ever see a maximum signature size buffer.
* Signatures which are provably too small, or provably too large, will be rejected by Verify without invoking the derived type
  * The derived type is responsible for avoiding OOB-read on variable length signatures that are smaller than the maximum.
* Public and private key exports do not have deterministic size, but each key has a maximum size that the platform supports.  The derived will never see a buffer smaller than the maximum size.
* RSA-based composite key raw public and raw private forms do not encode PKCS1 vs PSS signature scheme, so composite keys are not self-distinguishing.  Therefore the caller must specify the intended CompositeMLDsaAlgorithm at time of import.

# Indirect Usage

## X.509 Public Key Certificates

Certificates in .NET have two concurrent representations: the .NET projection and the underlying "native" representation.
Users on any platform that restricts certificate loading/parsing to known subject public key (and/or CA signature) algorithms, and does not support these PQC types,
will be unable to interact with certificates utilizing these algorithms.
Users on platforms that do not support these PQC algorithms, but do support loading certificates with unknown algorithms,
will be able to load such a certificate, but will not be able to use the certificates in X.509 Chain Building, CMS, et cetera.

### Chain Building

Chain building is performed by the underlying OS.
If a chain involves a signature based on any of these algorithms, and the OS supports them, the OS will validate the signature;
there is no mechanism by which to limit the signature algorithms that can be utilized during a chain build.

### PKCS#12 Personal Information Exchange (PFX)

Platforms that support the algorithms in certificates generally support loading their keys from a PFX.
PFX private key matching involves loading the key and comparing its public key representation against the candidate certificate's SubjectPublicKeyInfo.
There is no mechanism by which to limit the loadable algorithms during PFX key matching.

Callers who wish to avoid the use of these algorithms in a PKCS#12/PFX load can use the `Pkcs12Info` class to
read the metadata out of a PFX as a pre-load filter.
Additionally, callers can avoid private key loading and matching by asserting Pkcs12LoaderLimits.IgnorePrivateKeys as true.

### PKCS#7/CMS SignedData

The .NET SignedCms class can find signer certificates from the embedded certificates collection.
The signature verification methods will evaluate the signature using whatever certificate matches the target SignerInfo value,
whether that certificate come from a caller provided collection or the embedded collection.

Callers who wish to avoid the use of these algorithms in SignedCms can either inspect the `SignedCms.Certificates` collection,
or see the automatically matched certificate on `SignerInfo.Certificate` before calling a CheckSignature routine.

### PKCS#7/CMS EnvelopedData

The only algorithm in this document relevant to CMS EnvelopedData is ML-KEM.
ML-KEM is represented in EnvelopedData via a RecipientInfo of type OtherRecipientInfo,
with an oriType value of id-ori-kem (1.2.840.113549.1.9.16.13.3).

On Windows, the .NET EnvelopedCms class defers work to the Windows CryptMsg\* routines.
However, the EnvelopedCms projection has a representation of each RecipientInfo to track certificate matching for KEK decryption.
The Windows PAL is only capable of representing CMSG_KEY_TRANS_RECIPIENT (KeyTransRecipient) and CMSG_KEY_AGREE_RECIPIENT (KeyAgreeRecipient),
a recipient of any other kind will fail to load.

On other platforms, the .NET EnvelopedCms class uses a managed implementation.
The managed implementation will fail to load an EnvelopedData structure containing a RecipientInfo other than KeyTransRecipientInfo or KeyAgreeRecipientInfo.

As no .NET platforms can even load an EnvelopedCms/CMS EnvelopedData value without an exception,
there is no possible indirect use of these algorithms with this type.

## TLS (SslStream, HttpClient, et al)

When .NET is acting in the TLS Client role, and mutual authentication is not being employed,
.NET is mostly "not involved" in the TLS handshake.
Therefore, the underlying connection could be authenticated by a TLS Server Certificate which
either (or both) contains a PQC SubjectPublicKeyInfo value, or is signed by a CA that utilizes a PQC algorithm.

Callers who wish to block such a handshake must make use of the server certificate verification callback and inspect the chain themselves.

When .NET is acting in the TLS Server role, or mutual authentication is being employed in the client role,
.NET is "slightly involved" in conveying the certificate and private key to the underlying system TLS libraries.
Low-touch platforms, like Windows, it is possible to have an X509Certificate2 instance that has a known private key
where .NET itself is not capable of understanding the private key, yet the certificate works in TLS.
High-touch platforms, like Linux, require .NET to understand the algorithm to understand how to forward on the private key.

Either way, the caller provides the identity certificate, and it is up to the caller to decide what certificate(s) is/are acceptable before doing so.
