// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography.Pkcs
{
    public sealed partial class CmsRecipient
    {
        public CmsRecipient(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.Security.Cryptography.RSAEncryptionPadding rsaEncryptionPadding) { }
        public CmsRecipient(System.Security.Cryptography.Pkcs.SubjectIdentifierType recipientIdentifierType, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.Security.Cryptography.RSAEncryptionPadding rsaEncryptionPadding) { }
        public System.Security.Cryptography.RSAEncryptionPadding? RSAEncryptionPadding { get { throw null; } }
    }
    public sealed partial class CmsSigner
    {
        public CmsSigner(System.Security.Cryptography.Pkcs.SubjectIdentifierType signerIdentifierType, System.Security.Cryptography.X509Certificates.X509Certificate2? certificate, System.Security.Cryptography.AsymmetricAlgorithm? privateKey) { }
        public System.Security.Cryptography.AsymmetricAlgorithm? PrivateKey { get { throw null; } set { } }
    }
    public sealed partial class ContentInfo
    {
        public static System.Security.Cryptography.Oid GetContentType(ReadOnlySpan<byte> encodedMessage) { throw null; }
    }
    public sealed partial class EnvelopedCms
    {
        public void Decode(System.ReadOnlySpan<byte> encodedMessage) { }
        public void Decrypt(System.Security.Cryptography.Pkcs.RecipientInfo recipientInfo, System.Security.Cryptography.AsymmetricAlgorithm? privateKey) { }
    }
    public sealed partial class Pkcs12Builder
    {
        public Pkcs12Builder() { }
        public bool IsSealed { get { throw null; } }
        public void AddSafeContentsEncrypted(System.Security.Cryptography.Pkcs.Pkcs12SafeContents safeContents, byte[]? passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters) { }
        public void AddSafeContentsEncrypted(System.Security.Cryptography.Pkcs.Pkcs12SafeContents safeContents, System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters) { }
        public void AddSafeContentsEncrypted(System.Security.Cryptography.Pkcs.Pkcs12SafeContents safeContents, System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters) { }
        public void AddSafeContentsEncrypted(System.Security.Cryptography.Pkcs.Pkcs12SafeContents safeContents, string? password, System.Security.Cryptography.PbeParameters pbeParameters) { }
        public void AddSafeContentsUnencrypted(System.Security.Cryptography.Pkcs.Pkcs12SafeContents safeContents) { }
        public byte[] Encode() { throw null; }
        public void SealWithMac(System.ReadOnlySpan<char> password, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, int iterationCount) { }
        public void SealWithMac(string? password, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, int iterationCount) { }
        public void SealWithoutIntegrity() { }
        public bool TryEncode(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public sealed partial class Pkcs12CertBag : System.Security.Cryptography.Pkcs.Pkcs12SafeBag
    {
        public Pkcs12CertBag(System.Security.Cryptography.Oid certificateType, System.ReadOnlyMemory<byte> encodedCertificate) : base (default(string), default(System.ReadOnlyMemory<byte>), default(bool)) { }
        public System.ReadOnlyMemory<byte> EncodedCertificate { get { throw null; } }
        public bool IsX509Certificate { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 GetCertificate() { throw null; }
        public System.Security.Cryptography.Oid GetCertificateType() { throw null; }
    }
    public enum Pkcs12ConfidentialityMode
    {
        None = 1,
        Password = 2,
        PublicKey = 3,
        Unknown = 0,
    }
    public sealed partial class Pkcs12Info
    {
        internal Pkcs12Info() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Security.Cryptography.Pkcs.Pkcs12SafeContents> AuthenticatedSafe { get { throw null; } }
        public System.Security.Cryptography.Pkcs.Pkcs12IntegrityMode IntegrityMode { get { throw null; } }
        public static System.Security.Cryptography.Pkcs.Pkcs12Info Decode(System.ReadOnlyMemory<byte> encodedBytes, out int bytesConsumed, bool skipCopy = false) { throw null; }
        public bool VerifyMac(System.ReadOnlySpan<char> password) { throw null; }
        public bool VerifyMac(string? password) { throw null; }
    }
    public enum Pkcs12IntegrityMode
    {
        None = 1,
        Password = 2,
        PublicKey = 3,
        Unknown = 0,
    }
    public sealed partial class Pkcs12KeyBag : System.Security.Cryptography.Pkcs.Pkcs12SafeBag
    {
        public Pkcs12KeyBag(System.ReadOnlyMemory<byte> pkcs8PrivateKey, bool skipCopy = false) : base (default(string), default(System.ReadOnlyMemory<byte>), default(bool)) { }
        public System.ReadOnlyMemory<byte> Pkcs8PrivateKey { get { throw null; } }
    }
    public abstract partial class Pkcs12SafeBag
    {
        protected Pkcs12SafeBag(string bagIdValue, System.ReadOnlyMemory<byte> encodedBagValue, bool skipCopy = false) { }
        public System.Security.Cryptography.CryptographicAttributeObjectCollection Attributes { get { throw null; } }
        public System.ReadOnlyMemory<byte> EncodedBagValue { get { throw null; } }
        public byte[] Encode() { throw null; }
        public System.Security.Cryptography.Oid GetBagId() { throw null; }
        public bool TryEncode(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public sealed partial class Pkcs12SafeContents
    {
        public Pkcs12SafeContents() { }
        public System.Security.Cryptography.Pkcs.Pkcs12ConfidentialityMode ConfidentialityMode { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public System.Security.Cryptography.Pkcs.Pkcs12CertBag AddCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        public System.Security.Cryptography.Pkcs.Pkcs12KeyBag AddKeyUnencrypted(System.Security.Cryptography.AsymmetricAlgorithm key) { throw null; }
        public System.Security.Cryptography.Pkcs.Pkcs12SafeContentsBag AddNestedContents(System.Security.Cryptography.Pkcs.Pkcs12SafeContents safeContents) { throw null; }
        public void AddSafeBag(System.Security.Cryptography.Pkcs.Pkcs12SafeBag safeBag) { }
        public System.Security.Cryptography.Pkcs.Pkcs12SecretBag AddSecret(System.Security.Cryptography.Oid secretType, System.ReadOnlyMemory<byte> secretValue) { throw null; }
        public System.Security.Cryptography.Pkcs.Pkcs12ShroudedKeyBag AddShroudedKey(System.Security.Cryptography.AsymmetricAlgorithm key, byte[]? passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public System.Security.Cryptography.Pkcs.Pkcs12ShroudedKeyBag AddShroudedKey(System.Security.Cryptography.AsymmetricAlgorithm key, System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public System.Security.Cryptography.Pkcs.Pkcs12ShroudedKeyBag AddShroudedKey(System.Security.Cryptography.AsymmetricAlgorithm key, System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public System.Security.Cryptography.Pkcs.Pkcs12ShroudedKeyBag AddShroudedKey(System.Security.Cryptography.AsymmetricAlgorithm key, string? password, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public void Decrypt(byte[]? passwordBytes) { }
        public void Decrypt(System.ReadOnlySpan<byte> passwordBytes) { }
        public void Decrypt(System.ReadOnlySpan<char> password) { }
        public void Decrypt(string? password) { }
        public System.Collections.Generic.IEnumerable<System.Security.Cryptography.Pkcs.Pkcs12SafeBag> GetBags() { throw null; }
    }
    public sealed partial class Pkcs12SafeContentsBag : System.Security.Cryptography.Pkcs.Pkcs12SafeBag
    {
        internal Pkcs12SafeContentsBag() : base (default(string), default(System.ReadOnlyMemory<byte>), default(bool)) { }
        public System.Security.Cryptography.Pkcs.Pkcs12SafeContents? SafeContents { get { throw null; } }
    }
    public sealed partial class Pkcs12SecretBag : System.Security.Cryptography.Pkcs.Pkcs12SafeBag
    {
        internal Pkcs12SecretBag() : base (default(string), default(System.ReadOnlyMemory<byte>), default(bool)) { }
        public System.ReadOnlyMemory<byte> SecretValue { get { throw null; } }
        public System.Security.Cryptography.Oid GetSecretType() { throw null; }
    }
    public sealed partial class Pkcs12ShroudedKeyBag : System.Security.Cryptography.Pkcs.Pkcs12SafeBag
    {
        public Pkcs12ShroudedKeyBag(System.ReadOnlyMemory<byte> encryptedPkcs8PrivateKey, bool skipCopy = false) : base (default(string), default(System.ReadOnlyMemory<byte>), default(bool)) { }
        public System.ReadOnlyMemory<byte> EncryptedPkcs8PrivateKey { get { throw null; } }
    }
    public sealed partial class Pkcs8PrivateKeyInfo
    {
        public Pkcs8PrivateKeyInfo(System.Security.Cryptography.Oid algorithmId, System.ReadOnlyMemory<byte>? algorithmParameters, System.ReadOnlyMemory<byte> privateKey, bool skipCopies = false) { }
        public System.Security.Cryptography.Oid AlgorithmId { get { throw null; } }
        public System.ReadOnlyMemory<byte>? AlgorithmParameters { get { throw null; } }
        public System.Security.Cryptography.CryptographicAttributeObjectCollection Attributes { get { throw null; } }
        public System.ReadOnlyMemory<byte> PrivateKeyBytes { get { throw null; } }
        public static System.Security.Cryptography.Pkcs.Pkcs8PrivateKeyInfo Create(System.Security.Cryptography.AsymmetricAlgorithm privateKey) { throw null; }
        public static System.Security.Cryptography.Pkcs.Pkcs8PrivateKeyInfo Decode(System.ReadOnlyMemory<byte> source, out int bytesRead, bool skipCopy = false) { throw null; }
        public static System.Security.Cryptography.Pkcs.Pkcs8PrivateKeyInfo DecryptAndDecode(System.ReadOnlySpan<byte> passwordBytes, System.ReadOnlyMemory<byte> source, out int bytesRead) { throw null; }
        public static System.Security.Cryptography.Pkcs.Pkcs8PrivateKeyInfo DecryptAndDecode(System.ReadOnlySpan<char> password, System.ReadOnlyMemory<byte> source, out int bytesRead) { throw null; }
        public byte[] Encode() { throw null; }
        public byte[] Encrypt(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public byte[] Encrypt(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public bool TryEncode(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TryEncrypt(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TryEncrypt(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public sealed partial class Pkcs9LocalKeyId : System.Security.Cryptography.Pkcs.Pkcs9AttributeObject
    {
        public Pkcs9LocalKeyId() { }
        public Pkcs9LocalKeyId(byte[] keyId) { }
        public Pkcs9LocalKeyId(System.ReadOnlySpan<byte> keyId) { }
        public System.ReadOnlyMemory<byte> KeyId { get { throw null; } }
    }
    public sealed partial class Rfc3161TimestampRequest
    {
        internal Rfc3161TimestampRequest() { }
        public bool HasExtensions { get { throw null; } }
        public System.Security.Cryptography.Oid HashAlgorithmId { get { throw null; } }
        public System.Security.Cryptography.Oid? RequestedPolicyId { get { throw null; } }
        public bool RequestSignerCertificate { get { throw null; } }
        public int Version { get { throw null; } }
        public static System.Security.Cryptography.Pkcs.Rfc3161TimestampRequest CreateFromData(System.ReadOnlySpan<byte> data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.Oid? requestedPolicyId = null, System.ReadOnlyMemory<byte>? nonce = default(System.ReadOnlyMemory<byte>?), bool requestSignerCertificates = false, System.Security.Cryptography.X509Certificates.X509ExtensionCollection? extensions = null) { throw null; }
        public static System.Security.Cryptography.Pkcs.Rfc3161TimestampRequest CreateFromHash(System.ReadOnlyMemory<byte> hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.Oid? requestedPolicyId = null, System.ReadOnlyMemory<byte>? nonce = default(System.ReadOnlyMemory<byte>?), bool requestSignerCertificates = false, System.Security.Cryptography.X509Certificates.X509ExtensionCollection? extensions = null) { throw null; }
        public static System.Security.Cryptography.Pkcs.Rfc3161TimestampRequest CreateFromHash(System.ReadOnlyMemory<byte> hash, System.Security.Cryptography.Oid hashAlgorithmId, System.Security.Cryptography.Oid? requestedPolicyId = null, System.ReadOnlyMemory<byte>? nonce = default(System.ReadOnlyMemory<byte>?), bool requestSignerCertificates = false, System.Security.Cryptography.X509Certificates.X509ExtensionCollection? extensions = null) { throw null; }
        public static System.Security.Cryptography.Pkcs.Rfc3161TimestampRequest CreateFromSignerInfo(System.Security.Cryptography.Pkcs.SignerInfo signerInfo, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.Oid? requestedPolicyId = null, System.ReadOnlyMemory<byte>? nonce = default(System.ReadOnlyMemory<byte>?), bool requestSignerCertificates = false, System.Security.Cryptography.X509Certificates.X509ExtensionCollection? extensions = null) { throw null; }
        public byte[] Encode() { throw null; }
        public System.Security.Cryptography.X509Certificates.X509ExtensionCollection GetExtensions() { throw null; }
        public System.ReadOnlyMemory<byte> GetMessageHash() { throw null; }
        public System.ReadOnlyMemory<byte>? GetNonce() { throw null; }
        public System.Security.Cryptography.Pkcs.Rfc3161TimestampToken ProcessResponse(System.ReadOnlyMemory<byte> responseBytes, out int bytesConsumed) { throw null; }
        public static bool TryDecode(System.ReadOnlyMemory<byte> encodedBytes, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.Pkcs.Rfc3161TimestampRequest? request, out int bytesConsumed) { throw null; }
        public bool TryEncode(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public sealed partial class Rfc3161TimestampToken
    {
        internal Rfc3161TimestampToken() { }
        public System.Security.Cryptography.Pkcs.Rfc3161TimestampTokenInfo TokenInfo { get { throw null; } }
        public System.Security.Cryptography.Pkcs.SignedCms AsSignedCms() { throw null; }
        public static bool TryDecode(System.ReadOnlyMemory<byte> encodedBytes, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.Pkcs.Rfc3161TimestampToken? token, out int bytesConsumed) { throw null; }
        public bool VerifySignatureForData(System.ReadOnlySpan<byte> data, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.X509Certificates.X509Certificate2? signerCertificate, System.Security.Cryptography.X509Certificates.X509Certificate2Collection? extraCandidates = null) { throw null; }
        public bool VerifySignatureForHash(System.ReadOnlySpan<byte> hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.X509Certificates.X509Certificate2? signerCertificate, System.Security.Cryptography.X509Certificates.X509Certificate2Collection? extraCandidates = null) { throw null; }
        public bool VerifySignatureForHash(System.ReadOnlySpan<byte> hash, System.Security.Cryptography.Oid hashAlgorithmId, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.X509Certificates.X509Certificate2? signerCertificate, System.Security.Cryptography.X509Certificates.X509Certificate2Collection? extraCandidates = null) { throw null; }
        public bool VerifySignatureForSignerInfo(System.Security.Cryptography.Pkcs.SignerInfo signerInfo, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.X509Certificates.X509Certificate2? signerCertificate, System.Security.Cryptography.X509Certificates.X509Certificate2Collection? extraCandidates = null) { throw null; }
    }
    public sealed partial class Rfc3161TimestampTokenInfo
    {
        public Rfc3161TimestampTokenInfo(System.Security.Cryptography.Oid policyId, System.Security.Cryptography.Oid hashAlgorithmId, System.ReadOnlyMemory<byte> messageHash, System.ReadOnlyMemory<byte> serialNumber, System.DateTimeOffset timestamp, long? accuracyInMicroseconds = default(long?), bool isOrdering = false, System.ReadOnlyMemory<byte>? nonce = default(System.ReadOnlyMemory<byte>?), System.ReadOnlyMemory<byte>? timestampAuthorityName = default(System.ReadOnlyMemory<byte>?), System.Security.Cryptography.X509Certificates.X509ExtensionCollection? extensions = null) { }
        public long? AccuracyInMicroseconds { get { throw null; } }
        public bool HasExtensions { get { throw null; } }
        public System.Security.Cryptography.Oid HashAlgorithmId { get { throw null; } }
        public bool IsOrdering { get { throw null; } }
        public System.Security.Cryptography.Oid PolicyId { get { throw null; } }
        public System.DateTimeOffset Timestamp { get { throw null; } }
        public int Version { get { throw null; } }
        public byte[] Encode() { throw null; }
        public System.Security.Cryptography.X509Certificates.X509ExtensionCollection GetExtensions() { throw null; }
        public System.ReadOnlyMemory<byte> GetMessageHash() { throw null; }
        public System.ReadOnlyMemory<byte>? GetNonce() { throw null; }
        public System.ReadOnlyMemory<byte> GetSerialNumber() { throw null; }
        public System.ReadOnlyMemory<byte>? GetTimestampAuthorityName() { throw null; }
        public static bool TryDecode(System.ReadOnlyMemory<byte> encodedBytes, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.Pkcs.Rfc3161TimestampTokenInfo? timestampTokenInfo, out int bytesConsumed) { throw null; }
        public bool TryEncode(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public sealed partial class SignedCms
    {
        public void AddCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public void Decode(System.ReadOnlySpan<byte> encodedMessage) { }
        public void RemoveCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
    }
    public sealed partial class SignerInfo
    {
        public System.Security.Cryptography.Oid SignatureAlgorithm { get { throw null; } }
        public byte[] GetSignature() { throw null; }
        public void AddUnsignedAttribute(System.Security.Cryptography.AsnEncodedData unsignedAttribute) { }
        public void RemoveUnsignedAttribute(System.Security.Cryptography.AsnEncodedData unsignedAttribute) { }
    }
    public sealed partial class SubjectIdentifier
    {
        public bool MatchesCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
    }
}
