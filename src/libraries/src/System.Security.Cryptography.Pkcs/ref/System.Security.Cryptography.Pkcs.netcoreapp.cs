// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography.Pkcs
{
    public sealed partial class CmsSigner
    {
        public CmsSigner(SubjectIdentifierType signerIdentifierType, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.Security.Cryptography.AsymmetricAlgorithm privateKey) => throw null;
        public System.Security.Cryptography.AsymmetricAlgorithm PrivateKey { get => throw null; set => throw null; }
    }
    public sealed partial class EnvelopedCms
    {
        public void Decrypt(System.Security.Cryptography.Pkcs.RecipientInfo recipientInfo, System.Security.Cryptography.AsymmetricAlgorithm privateKey) { }
    }
    public sealed partial class Pkcs12Builder
    {
        public bool IsSealed { get; }
        public void AddSafeContentsEncrypted(Pkcs12SafeContents safeContents, byte[] passwordBytes, PbeParameters pbeParameters) => throw null;
        public void AddSafeContentsEncrypted(Pkcs12SafeContents safeContents, ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters) => throw null;
        public void AddSafeContentsEncrypted(Pkcs12SafeContents safeContents, string password, PbeParameters pbeParameters) => throw null;
        public void AddSafeContentsEncrypted(Pkcs12SafeContents safeContents, ReadOnlySpan<char> password, PbeParameters pbeParameters) => throw null;
        public void AddSafeContentsUnencrypted(Pkcs12SafeContents safeContents) => throw null;
        public byte[] Encode() => throw null;
        public void SealWithMac(string password, HashAlgorithmName hashAlgorithm, int iterationCount) => throw null;
        public void SealWithMac(ReadOnlySpan<char> password, HashAlgorithmName hashAlgorithm, int iterationCount) => throw null;
        public void SealWithoutIntegrity() => throw null;
        public bool TryEncode(Span<byte> destination, out int bytesWritten) => throw null;
    }
    public sealed partial class Pkcs12CertBag : Pkcs12SafeBag
    {
        public Pkcs12CertBag(Oid certificateType, ReadOnlyMemory<byte> encodedCertificate) : base(null, default) => throw null;
        public bool IsX509Certificate { get; }
        public ReadOnlyMemory<byte> EncodedCertificate { get; }
        public Oid GetCertificateType() => throw null;
        public X509Certificate2 GetCertificate() => throw null;
    }
    public enum Pkcs12ConfidentialityMode
    {
        Unknown = 0,
        None = 1,
        Password = 2,
        PublicKey = 3,
    }
    public sealed partial class Pkcs12Info
    {
        private Pkcs12Info() { }
        public ReadOnlyCollection<Pkcs12SafeContents> AuthenticatedSafe { get; }
        public Pkcs12IntegrityMode IntegrityMode { get; }
        public bool VerifyMac(string password) => throw null;
        public bool VerifyMac(ReadOnlySpan<char> password) => throw null;
        public static Pkcs12Info Decode(ReadOnlyMemory<byte> encodedBytes, out int bytesConsumed, bool skipCopy=false) => throw null;
    }
    public enum Pkcs12IntegrityMode
    {
        Unknown = 0,
        None = 1,
        Password = 2,
        PublicKey = 3,
    }
    public sealed partial class Pkcs12KeyBag : Pkcs12SafeBag
    {
        public Pkcs12KeyBag(ReadOnlyMemory<byte> pkcs8PrivateKey, bool skipCopy = false) : base(null, default) { }
        public ReadOnlyMemory<byte> Pkcs8PrivateKey { get; }
    }
    public abstract partial class Pkcs12SafeBag
    {
        protected Pkcs12SafeBag(string bagIdValue, ReadOnlyMemory<byte> encodedBagValue, bool skipCopy=false) { }
        public CryptographicAttributeObjectCollection Attributes { get; }
        public ReadOnlyMemory<byte> EncodedBagValue { get; }
        public byte[] Encode() => throw null;
        public Oid GetBagId() => throw null;
        public bool TryEncode(Span<byte> destination, out int bytesWritten) => throw null;
    }
    public sealed partial class Pkcs12SafeContents
    {
        public Pkcs12ConfidentialityMode ConfidentialityMode { get; }
        public bool IsReadOnly { get; }
        public void AddSafeBag(Pkcs12SafeBag safeBag) => throw null;
        public Pkcs12CertBag AddCertificate(X509Certificate2 certificate) => throw null;
        public Pkcs12KeyBag AddKeyUnencrypted(AsymmetricAlgorithm key) => throw null;
        public Pkcs12SafeContentsBag AddNestedContents(Pkcs12SafeContents safeContents) => throw null;
        public Pkcs12ShroudedKeyBag AddShroudedKey(AsymmetricAlgorithm key, byte[] passwordBytes, PbeParameters pbeParameters) => throw null;
        public Pkcs12ShroudedKeyBag AddShroudedKey(AsymmetricAlgorithm key, ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters) => throw null;
        public Pkcs12ShroudedKeyBag AddShroudedKey(AsymmetricAlgorithm key, string password, PbeParameters pbeParameters) => throw null;
        public Pkcs12ShroudedKeyBag AddShroudedKey(AsymmetricAlgorithm key, ReadOnlySpan<char> password, PbeParameters pbeParameters) => throw null;
        public Pkcs12SecretBag AddSecret(Oid secretType, ReadOnlyMemory<byte> secretValue) => throw null;
        public void Decrypt(byte[] passwordBytes) => throw null;
        public void Decrypt(ReadOnlySpan<byte> passwordBytes) => throw null;
        public void Decrypt(string password) => throw null;
        public void Decrypt(ReadOnlySpan<char> password) => throw null;
        public IEnumerable<Pkcs12SafeBag> GetBags() => throw null;
    }
    public sealed partial class Pkcs12SafeContentsBag : Pkcs12SafeBag
    {
        private Pkcs12SafeContentsBag() : base(null, default) { }
        public Pkcs12SafeContents SafeContents { get; }
    }
    public sealed partial class Pkcs12SecretBag : Pkcs12SafeBag
    {
        private Pkcs12SecretBag() : base(null, default) { }
        public Oid GetSecretType() => throw null;
        public ReadOnlyMemory<byte> SecretValue { get; }
    }
    public sealed partial class Pkcs12ShroudedKeyBag : Pkcs12SafeBag
    {
        public Pkcs12ShroudedKeyBag(ReadOnlyMemory<byte> encryptedPkcs8PrivateKey, bool skipCopy = false) : base(null, default) { }
        public ReadOnlyMemory<byte> EncryptedPkcs8PrivateKey { get; }
    }
    public sealed partial class Pkcs8PrivateKeyInfo
    {
        public Oid AlgorithmId { get; }
        public ReadOnlyMemory<byte>? AlgorithmParameters { get; }
        public CryptographicAttributeObjectCollection Attributes { get; }
        public ReadOnlyMemory<byte> PrivateKeyBytes { get; }
        public Pkcs8PrivateKeyInfo(Oid algorithmId, ReadOnlyMemory<byte>? algorithmParameters, ReadOnlyMemory<byte> privateKey, bool skipCopies = false) { }
        public static Pkcs8PrivateKeyInfo Create(AsymmetricAlgorithm privateKey) => throw null;
        public static Pkcs8PrivateKeyInfo Decode(ReadOnlyMemory<byte> source, out int bytesRead, bool skipCopy = false) => throw null;
        public byte[] Encode() => throw null;
        public byte[] Encrypt(ReadOnlySpan<char> password, PbeParameters pbeParameters) => throw null;
        public byte[] Encrypt(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters) => throw null;
        public bool TryEncode(Span<byte> destination, out int bytesWritten) => throw null;
        public bool TryEncrypt(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten) => throw null;
        public bool TryEncrypt(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten) => throw null;
        public static Pkcs8PrivateKeyInfo DecryptAndDecode(ReadOnlySpan<char> password, ReadOnlyMemory<byte> source, out int bytesRead) => throw null;
        public static Pkcs8PrivateKeyInfo DecryptAndDecode(ReadOnlySpan<byte> passwordBytes, ReadOnlyMemory<byte> source, out int bytesRead) => throw null;
    }
    public sealed partial class Pkcs9LocalKeyId : Pkcs9AttributeObject
    {
        public ReadOnlyMemory<byte> KeyId { get; }
        public Pkcs9LocalKeyId() => throw null;
        public Pkcs9LocalKeyId(byte[] keyId) => throw null;
        public Pkcs9LocalKeyId(ReadOnlySpan<byte> keyId) => throw null;
    }
    public sealed partial class Rfc3161TimestampRequest
    {
        private Rfc3161TimestampRequest() { }
        public int Version => throw null;
        public ReadOnlyMemory<byte> GetMessageHash() => throw null;
        public Oid HashAlgorithmId => throw null;
        public Oid RequestedPolicyId => throw null;
        public bool RequestSignerCertificate => throw null;
        public ReadOnlyMemory<byte>? GetNonce() => throw null;
        public bool HasExtensions => throw null;
        public X509ExtensionCollection GetExtensions() => throw null;
        public byte[] Encode() => throw null;
        public bool TryEncode(Span<byte> destination, out int bytesWritten) => throw null;
        public Rfc3161TimestampToken ProcessResponse(ReadOnlyMemory<byte> responseBytes, out int bytesConsumed) => throw null;
        public static Rfc3161TimestampRequest CreateFromData(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm, Oid requestedPolicyId = null, ReadOnlyMemory<byte>? nonce = null, bool requestSignerCertificates = false, X509ExtensionCollection extensions = null) => throw null;
        public static Rfc3161TimestampRequest CreateFromHash(ReadOnlyMemory<byte> hash, HashAlgorithmName hashAlgorithm, Oid requestedPolicyId = null, ReadOnlyMemory<byte>? nonce = null, bool requestSignerCertificates = false, X509ExtensionCollection extensions = null) => throw null;
        public static Rfc3161TimestampRequest CreateFromHash(ReadOnlyMemory<byte> hash, Oid hashAlgorithmId, Oid requestedPolicyId = null, ReadOnlyMemory<byte>? nonce = null, bool requestSignerCertificates = false, X509ExtensionCollection extensions = null) => throw null;
        public static Rfc3161TimestampRequest CreateFromSignerInfo(SignerInfo signerInfo, HashAlgorithmName hashAlgorithm, Oid requestedPolicyId = null, ReadOnlyMemory<byte>? nonce = null, bool requestSignerCertificates = false, X509ExtensionCollection extensions = null) => throw null;
        public static bool TryDecode(ReadOnlyMemory<byte> encodedBytes, out Rfc3161TimestampRequest request, out int bytesConsumed) => throw null;
    }
    public sealed partial class Rfc3161TimestampToken
    {
        private Rfc3161TimestampToken() { }
        public Rfc3161TimestampTokenInfo TokenInfo => throw null;
        public SignedCms AsSignedCms() => throw null;
        public bool VerifySignatureForHash(ReadOnlySpan<byte> hash, HashAlgorithmName hashAlgorithm, out X509Certificate2 signerCertificate, X509Certificate2Collection extraCandidates = null) => throw null;
        public bool VerifySignatureForHash(ReadOnlySpan<byte> hash, Oid hashAlgorithmId, out X509Certificate2 signerCertificate, X509Certificate2Collection extraCandidates = null) => throw null;
        public bool VerifySignatureForData(ReadOnlySpan<byte> data, out X509Certificate2 signerCertificate, X509Certificate2Collection extraCandidates = null) => throw null;
        public bool VerifySignatureForSignerInfo(SignerInfo signerInfo, out X509Certificate2 signerCertificate, X509Certificate2Collection extraCandidates = null) => throw null;
        public static bool TryDecode(ReadOnlyMemory<byte> encodedBytes, out Rfc3161TimestampToken token, out int bytesConsumed) => throw null;
    }
    public sealed partial class Rfc3161TimestampTokenInfo
    {
        public Rfc3161TimestampTokenInfo(Oid policyId, Oid hashAlgorithmId, ReadOnlyMemory<byte> messageHash, ReadOnlyMemory<byte> serialNumber, DateTimeOffset timestamp, long? accuracyInMicroseconds=null, bool isOrdering=false, ReadOnlyMemory<byte>? nonce=null, ReadOnlyMemory<byte>? timestampAuthorityName=null, X509ExtensionCollection extensions =null) { throw null; }
        public int Version => throw null;
        public Oid PolicyId=> throw null;
        public Oid HashAlgorithmId => throw null;
        public ReadOnlyMemory<byte> GetMessageHash() { throw null; }
        public ReadOnlyMemory<byte> GetSerialNumber() { throw null; }
        public DateTimeOffset Timestamp => throw null;
        public long? AccuracyInMicroseconds => throw null;
        public bool IsOrdering => throw null;
        public ReadOnlyMemory<byte>? GetNonce() { throw null; }
        public ReadOnlyMemory<byte>? GetTimestampAuthorityName() { throw null; }
        public bool HasExtensions => throw null;
        public X509ExtensionCollection GetExtensions() { throw null; }
        public byte[] Encode() => throw null;
        public bool TryEncode(Span<byte> destination, out int bytesWritten) => throw null;
        public static bool TryDecode(ReadOnlyMemory<byte> encodedBytes, out Rfc3161TimestampTokenInfo timestampTokenInfo, out int bytesConsumed) { throw null; }
    }
    public sealed partial class SignedCms
    {
        public void AddCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) => throw null;
        public void RemoveCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) => throw null;
    }
    public sealed partial class SignerInfo
    {
        public Oid SignatureAlgorithm => throw null;
        public byte[] GetSignature() => throw null;
        public void AddUnsignedAttribute(System.Security.Cryptography.AsnEncodedData asnEncodedData) => throw null;
        public void RemoveUnsignedAttribute(System.Security.Cryptography.AsnEncodedData asnEncodedData) => throw null;
    }
    public sealed partial class SubjectIdentifier
    {
        public bool MatchesCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
    }
}
