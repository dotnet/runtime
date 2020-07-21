// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography
{
    public sealed partial class CryptographicAttributeObject
    {
        public CryptographicAttributeObject(System.Security.Cryptography.Oid oid) { }
        public CryptographicAttributeObject(System.Security.Cryptography.Oid oid, System.Security.Cryptography.AsnEncodedDataCollection? values) { }
        public System.Security.Cryptography.Oid Oid { get { throw null; } }
        public System.Security.Cryptography.AsnEncodedDataCollection Values { get { throw null; } }
    }
    public sealed partial class CryptographicAttributeObjectCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public CryptographicAttributeObjectCollection() { }
        public CryptographicAttributeObjectCollection(System.Security.Cryptography.CryptographicAttributeObject attribute) { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Security.Cryptography.CryptographicAttributeObject this[int index] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public int Add(System.Security.Cryptography.AsnEncodedData asnEncodedData) { throw null; }
        public int Add(System.Security.Cryptography.CryptographicAttributeObject attribute) { throw null; }
        public void CopyTo(System.Security.Cryptography.CryptographicAttributeObject[] array, int index) { }
        public System.Security.Cryptography.CryptographicAttributeObjectEnumerator GetEnumerator() { throw null; }
        public void Remove(System.Security.Cryptography.CryptographicAttributeObject attribute) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class CryptographicAttributeObjectEnumerator : System.Collections.IEnumerator
    {
        internal CryptographicAttributeObjectEnumerator() { }
        public System.Security.Cryptography.CryptographicAttributeObject Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
}
namespace System.Security.Cryptography.Pkcs
{
    public sealed partial class AlgorithmIdentifier
    {
        public AlgorithmIdentifier() { }
        public AlgorithmIdentifier(System.Security.Cryptography.Oid oid) { }
        public AlgorithmIdentifier(System.Security.Cryptography.Oid oid, int keyLength) { }
        public int KeyLength { get { throw null; } set { } }
        public System.Security.Cryptography.Oid Oid { get { throw null; } set { } }
        public byte[] Parameters { get { throw null; } set { } }
    }
    public sealed partial class CmsRecipient
    {
        public CmsRecipient(System.Security.Cryptography.Pkcs.SubjectIdentifierType recipientIdentifierType, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public CmsRecipient(System.Security.Cryptography.Pkcs.SubjectIdentifierType recipientIdentifierType, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.Security.Cryptography.RSAEncryptionPadding rsaEncryptionPadding) { }
        public CmsRecipient(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public CmsRecipient(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.Security.Cryptography.RSAEncryptionPadding rsaEncryptionPadding) { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate { get { throw null; } }
        public System.Security.Cryptography.Pkcs.SubjectIdentifierType RecipientIdentifierType { get { throw null; } }
        public System.Security.Cryptography.RSAEncryptionPadding? RSAEncryptionPadding { get { throw null; } }
    }
    public sealed partial class CmsRecipientCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public CmsRecipientCollection() { }
        public CmsRecipientCollection(System.Security.Cryptography.Pkcs.CmsRecipient recipient) { }
        public CmsRecipientCollection(System.Security.Cryptography.Pkcs.SubjectIdentifierType recipientIdentifierType, System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Security.Cryptography.Pkcs.CmsRecipient this[int index] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public int Add(System.Security.Cryptography.Pkcs.CmsRecipient recipient) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Security.Cryptography.Pkcs.CmsRecipient[] array, int index) { }
        public System.Security.Cryptography.Pkcs.CmsRecipientEnumerator GetEnumerator() { throw null; }
        public void Remove(System.Security.Cryptography.Pkcs.CmsRecipient recipient) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class CmsRecipientEnumerator : System.Collections.IEnumerator
    {
        internal CmsRecipientEnumerator() { }
        public System.Security.Cryptography.Pkcs.CmsRecipient Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    public sealed partial class CmsSigner
    {
        public CmsSigner() { }
        public CmsSigner(System.Security.Cryptography.CspParameters parameters) { }
        public CmsSigner(System.Security.Cryptography.Pkcs.SubjectIdentifierType signerIdentifierType) { }
        public CmsSigner(System.Security.Cryptography.Pkcs.SubjectIdentifierType signerIdentifierType, System.Security.Cryptography.X509Certificates.X509Certificate2? certificate) { }
        public CmsSigner(System.Security.Cryptography.Pkcs.SubjectIdentifierType signerIdentifierType, System.Security.Cryptography.X509Certificates.X509Certificate2? certificate, System.Security.Cryptography.AsymmetricAlgorithm? privateKey) { }
        public CmsSigner(System.Security.Cryptography.X509Certificates.X509Certificate2? certificate) { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2? Certificate { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Certificates { get { throw null; } }
        public System.Security.Cryptography.Oid DigestAlgorithm { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509IncludeOption IncludeOption { get { throw null; } set { } }
        public System.Security.Cryptography.AsymmetricAlgorithm? PrivateKey { get { throw null; } set { } }
        public System.Security.Cryptography.CryptographicAttributeObjectCollection SignedAttributes { get { throw null; } }
        public System.Security.Cryptography.Pkcs.SubjectIdentifierType SignerIdentifierType { get { throw null; } set { } }
        public System.Security.Cryptography.CryptographicAttributeObjectCollection UnsignedAttributes { get { throw null; } }
    }
    public sealed partial class ContentInfo
    {
        public ContentInfo(byte[] content) { }
        public ContentInfo(System.Security.Cryptography.Oid contentType, byte[] content) { }
        public byte[] Content { get { throw null; } }
        public System.Security.Cryptography.Oid ContentType { get { throw null; } }
        public static System.Security.Cryptography.Oid GetContentType(byte[] encodedMessage) { throw null; }
        public static System.Security.Cryptography.Oid GetContentType(System.ReadOnlySpan<byte> encodedMessage) { throw null; }
    }
    public sealed partial class EnvelopedCms
    {
        public EnvelopedCms() { }
        public EnvelopedCms(System.Security.Cryptography.Pkcs.ContentInfo contentInfo) { }
        public EnvelopedCms(System.Security.Cryptography.Pkcs.ContentInfo contentInfo, System.Security.Cryptography.Pkcs.AlgorithmIdentifier encryptionAlgorithm) { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Certificates { get { throw null; } }
        public System.Security.Cryptography.Pkcs.AlgorithmIdentifier ContentEncryptionAlgorithm { get { throw null; } }
        public System.Security.Cryptography.Pkcs.ContentInfo ContentInfo { get { throw null; } }
        public System.Security.Cryptography.Pkcs.RecipientInfoCollection RecipientInfos { get { throw null; } }
        public System.Security.Cryptography.CryptographicAttributeObjectCollection UnprotectedAttributes { get { throw null; } }
        public int Version { get { throw null; } }
        public void Decode(byte[] encodedMessage) { }
        public void Decode(System.ReadOnlySpan<byte> encodedMessage) { }
        public void Decrypt() { }
        public void Decrypt(System.Security.Cryptography.Pkcs.RecipientInfo recipientInfo) { }
        public void Decrypt(System.Security.Cryptography.Pkcs.RecipientInfo recipientInfo, System.Security.Cryptography.AsymmetricAlgorithm? privateKey) { }
        public void Decrypt(System.Security.Cryptography.Pkcs.RecipientInfo recipientInfo, System.Security.Cryptography.X509Certificates.X509Certificate2Collection extraStore) { }
        public void Decrypt(System.Security.Cryptography.X509Certificates.X509Certificate2Collection extraStore) { }
        public byte[] Encode() { throw null; }
        public void Encrypt(System.Security.Cryptography.Pkcs.CmsRecipient recipient) { }
        public void Encrypt(System.Security.Cryptography.Pkcs.CmsRecipientCollection recipients) { }
    }
    public sealed partial class KeyAgreeRecipientInfo : System.Security.Cryptography.Pkcs.RecipientInfo
    {
        internal KeyAgreeRecipientInfo() { }
        public System.DateTime Date { get { throw null; } }
        public override byte[] EncryptedKey { get { throw null; } }
        public override System.Security.Cryptography.Pkcs.AlgorithmIdentifier KeyEncryptionAlgorithm { get { throw null; } }
        public System.Security.Cryptography.Pkcs.SubjectIdentifierOrKey OriginatorIdentifierOrKey { get { throw null; } }
        public System.Security.Cryptography.CryptographicAttributeObject? OtherKeyAttribute { get { throw null; } }
        public override System.Security.Cryptography.Pkcs.SubjectIdentifier RecipientIdentifier { get { throw null; } }
        public override int Version { get { throw null; } }
    }
    public sealed partial class KeyTransRecipientInfo : System.Security.Cryptography.Pkcs.RecipientInfo
    {
        internal KeyTransRecipientInfo() { }
        public override byte[] EncryptedKey { get { throw null; } }
        public override System.Security.Cryptography.Pkcs.AlgorithmIdentifier KeyEncryptionAlgorithm { get { throw null; } }
        public override System.Security.Cryptography.Pkcs.SubjectIdentifier RecipientIdentifier { get { throw null; } }
        public override int Version { get { throw null; } }
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
        Unknown = 0,
        None = 1,
        Password = 2,
        PublicKey = 3,
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
        Unknown = 0,
        None = 1,
        Password = 2,
        PublicKey = 3,
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
    public partial class Pkcs9AttributeObject : System.Security.Cryptography.AsnEncodedData
    {
        public Pkcs9AttributeObject() { }
        public Pkcs9AttributeObject(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
        public Pkcs9AttributeObject(System.Security.Cryptography.Oid oid, byte[] encodedData) { }
        public Pkcs9AttributeObject(string oid, byte[] encodedData) { }
        public new System.Security.Cryptography.Oid? Oid { get { throw null; } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public sealed partial class Pkcs9ContentType : System.Security.Cryptography.Pkcs.Pkcs9AttributeObject
    {
        public Pkcs9ContentType() { }
        public System.Security.Cryptography.Oid ContentType { get { throw null; } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public sealed partial class Pkcs9DocumentDescription : System.Security.Cryptography.Pkcs.Pkcs9AttributeObject
    {
        public Pkcs9DocumentDescription() { }
        public Pkcs9DocumentDescription(byte[] encodedDocumentDescription) { }
        public Pkcs9DocumentDescription(string documentDescription) { }
        public string DocumentDescription { get { throw null; } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public sealed partial class Pkcs9DocumentName : System.Security.Cryptography.Pkcs.Pkcs9AttributeObject
    {
        public Pkcs9DocumentName() { }
        public Pkcs9DocumentName(byte[] encodedDocumentName) { }
        public Pkcs9DocumentName(string documentName) { }
        public string DocumentName { get { throw null; } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public sealed partial class Pkcs9LocalKeyId : System.Security.Cryptography.Pkcs.Pkcs9AttributeObject
    {
        public Pkcs9LocalKeyId() { }
        public Pkcs9LocalKeyId(byte[] keyId) { }
        public Pkcs9LocalKeyId(System.ReadOnlySpan<byte> keyId) { }
        public System.ReadOnlyMemory<byte> KeyId { get { throw null; } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public sealed partial class Pkcs9MessageDigest : System.Security.Cryptography.Pkcs.Pkcs9AttributeObject
    {
        public Pkcs9MessageDigest() { }
        public byte[] MessageDigest { get { throw null; } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public sealed partial class Pkcs9SigningTime : System.Security.Cryptography.Pkcs.Pkcs9AttributeObject
    {
        public Pkcs9SigningTime() { }
        public Pkcs9SigningTime(byte[] encodedSigningTime) { }
        public Pkcs9SigningTime(System.DateTime signingTime) { }
        public System.DateTime SigningTime { get { throw null; } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public sealed partial class PublicKeyInfo
    {
        internal PublicKeyInfo() { }
        public System.Security.Cryptography.Pkcs.AlgorithmIdentifier Algorithm { get { throw null; } }
        public byte[] KeyValue { get { throw null; } }
    }
    public abstract partial class RecipientInfo
    {
        internal RecipientInfo() { }
        public abstract byte[] EncryptedKey { get; }
        public abstract System.Security.Cryptography.Pkcs.AlgorithmIdentifier KeyEncryptionAlgorithm { get; }
        public abstract System.Security.Cryptography.Pkcs.SubjectIdentifier RecipientIdentifier { get; }
        public System.Security.Cryptography.Pkcs.RecipientInfoType Type { get { throw null; } }
        public abstract int Version { get; }
    }
    public sealed partial class RecipientInfoCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal RecipientInfoCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Security.Cryptography.Pkcs.RecipientInfo this[int index] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Security.Cryptography.Pkcs.RecipientInfo[] array, int index) { }
        public System.Security.Cryptography.Pkcs.RecipientInfoEnumerator GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class RecipientInfoEnumerator : System.Collections.IEnumerator
    {
        internal RecipientInfoEnumerator() { }
        public System.Security.Cryptography.Pkcs.RecipientInfo Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    public enum RecipientInfoType
    {
        Unknown = 0,
        KeyTransport = 1,
        KeyAgreement = 2,
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
        public System.Security.Cryptography.Pkcs.Rfc3161TimestampToken ProcessResponse(System.ReadOnlyMemory<byte> source, out int bytesConsumed) { throw null; }
        public static bool TryDecode(System.ReadOnlyMemory<byte> encodedBytes, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.Pkcs.Rfc3161TimestampRequest? request, out int bytesConsumed) { throw null; }
        public bool TryEncode(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public sealed partial class Rfc3161TimestampToken
    {
        internal Rfc3161TimestampToken() { }
        public System.Security.Cryptography.Pkcs.Rfc3161TimestampTokenInfo TokenInfo { get { throw null; } }
        public System.Security.Cryptography.Pkcs.SignedCms AsSignedCms() { throw null; }
        public static bool TryDecode(System.ReadOnlyMemory<byte> source, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.Pkcs.Rfc3161TimestampToken? token, out int bytesConsumed) { throw null; }
        public bool VerifySignatureForData(System.ReadOnlySpan<byte> data, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.X509Certificates.X509Certificate2? signerCertificate, System.Security.Cryptography.X509Certificates.X509Certificate2Collection? extraCandidates = null) { throw null; }
        public bool VerifySignatureForHash(System.ReadOnlySpan<byte> hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.X509Certificates.X509Certificate2? signerCertificate, System.Security.Cryptography.X509Certificates.X509Certificate2Collection? extraCandidates = null) { throw null; }
        public bool VerifySignatureForHash(System.ReadOnlySpan<byte> hash, System.Security.Cryptography.Oid hashAlgorithmId, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.X509Certificates.X509Certificate2? signerCertificate, System.Security.Cryptography.X509Certificates.X509Certificate2Collection? extraCandidates = null) { throw null; }
        public bool VerifySignatureForSignerInfo(System.Security.Cryptography.Pkcs.SignerInfo signerInfo, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.X509Certificates.X509Certificate2? signerCertificate, System.Security.Cryptography.X509Certificates.X509Certificate2Collection? extraCandidates = null) { throw null; }
    }
    public sealed partial class Rfc3161TimestampTokenInfo
    {
        public Rfc3161TimestampTokenInfo(System.Security.Cryptography.Oid policyId, System.Security.Cryptography.Oid hashAlgorithmId, System.ReadOnlyMemory<byte> messageHash, System.ReadOnlyMemory<byte> serialNumber, System.DateTimeOffset timestamp, long? accuracyInMicroseconds = default(long?), bool isOrdering = false, System.ReadOnlyMemory<byte>? nonce = default(System.ReadOnlyMemory<byte>?), System.ReadOnlyMemory<byte>? tsaName = default(System.ReadOnlyMemory<byte>?), System.Security.Cryptography.X509Certificates.X509ExtensionCollection? extensions = null) { }
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
        public static bool TryDecode(System.ReadOnlyMemory<byte> source, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Security.Cryptography.Pkcs.Rfc3161TimestampTokenInfo? timestampTokenInfo, out int bytesConsumed) { throw null; }
        public bool TryEncode(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public sealed partial class SignedCms
    {
        public SignedCms() { }
        public SignedCms(System.Security.Cryptography.Pkcs.ContentInfo contentInfo) { }
        public SignedCms(System.Security.Cryptography.Pkcs.ContentInfo contentInfo, bool detached) { }
        public SignedCms(System.Security.Cryptography.Pkcs.SubjectIdentifierType signerIdentifierType) { }
        public SignedCms(System.Security.Cryptography.Pkcs.SubjectIdentifierType signerIdentifierType, System.Security.Cryptography.Pkcs.ContentInfo contentInfo) { }
        public SignedCms(System.Security.Cryptography.Pkcs.SubjectIdentifierType signerIdentifierType, System.Security.Cryptography.Pkcs.ContentInfo contentInfo, bool detached) { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Certificates { get { throw null; } }
        public System.Security.Cryptography.Pkcs.ContentInfo ContentInfo { get { throw null; } }
        public bool Detached { get { throw null; } }
        public System.Security.Cryptography.Pkcs.SignerInfoCollection SignerInfos { get { throw null; } }
        public int Version { get { throw null; } }
        public void AddCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public void CheckHash() { }
        public void CheckSignature(bool verifySignatureOnly) { }
        public void CheckSignature(System.Security.Cryptography.X509Certificates.X509Certificate2Collection extraStore, bool verifySignatureOnly) { }
        public void ComputeSignature() { }
        public void ComputeSignature(System.Security.Cryptography.Pkcs.CmsSigner signer) { }
        public void ComputeSignature(System.Security.Cryptography.Pkcs.CmsSigner signer, bool silent) { }
        public void Decode(byte[] encodedMessage) { }
        public void Decode(System.ReadOnlySpan<byte> encodedMessage) { }
        public byte[] Encode() { throw null; }
        public void RemoveCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public void RemoveSignature(int index) { }
        public void RemoveSignature(System.Security.Cryptography.Pkcs.SignerInfo signerInfo) { }
    }
    public sealed partial class SignerInfo
    {
        internal SignerInfo() { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2? Certificate { get { throw null; } }
        public System.Security.Cryptography.Pkcs.SignerInfoCollection CounterSignerInfos { get { throw null; } }
        public System.Security.Cryptography.Oid DigestAlgorithm { get { throw null; } }
        public System.Security.Cryptography.Oid SignatureAlgorithm { get { throw null; } }
        public System.Security.Cryptography.CryptographicAttributeObjectCollection SignedAttributes { get { throw null; } }
        public System.Security.Cryptography.Pkcs.SubjectIdentifier SignerIdentifier { get { throw null; } }
        public System.Security.Cryptography.CryptographicAttributeObjectCollection UnsignedAttributes { get { throw null; } }
        public int Version { get { throw null; } }
        public void AddUnsignedAttribute(System.Security.Cryptography.AsnEncodedData unsignedAttribute) { }
        public void CheckHash() { }
        public void CheckSignature(bool verifySignatureOnly) { }
        public void CheckSignature(System.Security.Cryptography.X509Certificates.X509Certificate2Collection extraStore, bool verifySignatureOnly) { }
        public void ComputeCounterSignature() { }
        public void ComputeCounterSignature(System.Security.Cryptography.Pkcs.CmsSigner signer) { }
        public byte[] GetSignature() { throw null; }
        public void RemoveCounterSignature(int index) { }
        public void RemoveCounterSignature(System.Security.Cryptography.Pkcs.SignerInfo counterSignerInfo) { }
        public void RemoveUnsignedAttribute(System.Security.Cryptography.AsnEncodedData unsignedAttribute) { }
    }
    public sealed partial class SignerInfoCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal SignerInfoCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Security.Cryptography.Pkcs.SignerInfo this[int index] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Security.Cryptography.Pkcs.SignerInfo[] array, int index) { }
        public System.Security.Cryptography.Pkcs.SignerInfoEnumerator GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class SignerInfoEnumerator : System.Collections.IEnumerator
    {
        internal SignerInfoEnumerator() { }
        public System.Security.Cryptography.Pkcs.SignerInfo Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    public sealed partial class SubjectIdentifier
    {
        internal SubjectIdentifier() { }
        public System.Security.Cryptography.Pkcs.SubjectIdentifierType Type { get { throw null; } }
        public object? Value { get { throw null; } }
        public bool MatchesCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
    }
    public sealed partial class SubjectIdentifierOrKey
    {
        internal SubjectIdentifierOrKey() { }
        public System.Security.Cryptography.Pkcs.SubjectIdentifierOrKeyType Type { get { throw null; } }
        public object Value { get { throw null; } }
    }
    public enum SubjectIdentifierOrKeyType
    {
        Unknown = 0,
        IssuerAndSerialNumber = 1,
        SubjectKeyIdentifier = 2,
        PublicKeyInfo = 3,
    }
    public enum SubjectIdentifierType
    {
        Unknown = 0,
        IssuerAndSerialNumber = 1,
        SubjectKeyIdentifier = 2,
        NoSignature = 3,
    }
}
namespace System.Security.Cryptography.Xml
{
    public partial struct X509IssuerSerial
    {
        private object _dummy;
        private int _dummyPrimitive;
        public string IssuerName { readonly get { throw null; } set { } }
        public string SerialNumber { readonly get { throw null; } set { } }
    }
}
