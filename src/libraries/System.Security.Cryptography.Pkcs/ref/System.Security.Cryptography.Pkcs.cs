// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography
{
    public sealed partial class CryptographicAttributeObject
    {
        public CryptographicAttributeObject(System.Security.Cryptography.Oid oid) { }
        public CryptographicAttributeObject(System.Security.Cryptography.Oid oid, System.Security.Cryptography.AsnEncodedDataCollection values) { }
        public System.Security.Cryptography.Oid Oid { get { throw null; } }
        public System.Security.Cryptography.AsnEncodedDataCollection Values { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public sealed partial class CryptographicAttributeObjectCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public CryptographicAttributeObjectCollection() { }
        public CryptographicAttributeObjectCollection(System.Security.Cryptography.CryptographicAttributeObject attribute) { }
        public int Count { get { throw null; } }
        public System.Security.Cryptography.CryptographicAttributeObject this[int index] { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
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
        public int KeyLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Security.Cryptography.Oid Oid { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public sealed partial class CmsRecipient
    {
        public CmsRecipient(System.Security.Cryptography.Pkcs.SubjectIdentifierType recipientIdentifierType, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public CmsRecipient(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Security.Cryptography.Pkcs.SubjectIdentifierType RecipientIdentifierType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public sealed partial class CmsRecipientCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public CmsRecipientCollection() { }
        public CmsRecipientCollection(System.Security.Cryptography.Pkcs.CmsRecipient recipient) { }
        public CmsRecipientCollection(System.Security.Cryptography.Pkcs.SubjectIdentifierType recipientIdentifierType, System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { }
        public int Count { get { throw null; } }
        public System.Security.Cryptography.Pkcs.CmsRecipient this[int index] { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
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
        public CmsSigner() => throw null;
        public CmsSigner(SubjectIdentifierType signerIdentifierType) => throw null;
        public CmsSigner(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) => throw null;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CmsSigner(CspParameters parameters) => throw null;
        public CmsSigner(SubjectIdentifierType signerIdentifierType, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) => throw null;
        public SubjectIdentifierType SignerIdentifierType { get => throw null; set => throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate { get => throw null; set => throw null; }
        public Oid DigestAlgorithm { get => throw null; set => throw null; }
        public CryptographicAttributeObjectCollection SignedAttributes { get => throw null; set => throw null; }
        public CryptographicAttributeObjectCollection UnsignedAttributes { get => throw null; set => throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Certificates { get => throw null; set => throw null; }
        public System.Security.Cryptography.X509Certificates.X509IncludeOption IncludeOption { get => throw null; set => throw null; }
    }
    public sealed partial class ContentInfo
    {
        public ContentInfo(byte[] content) { }
        public ContentInfo(System.Security.Cryptography.Oid contentType, byte[] content) { }
        public byte[] Content { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Security.Cryptography.Oid ContentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static System.Security.Cryptography.Oid GetContentType(byte[] encodedMessage) { throw null; }
    }
    public sealed partial class EnvelopedCms
    {
        public EnvelopedCms() { }
        public EnvelopedCms(System.Security.Cryptography.Pkcs.ContentInfo contentInfo) { }
        public EnvelopedCms(System.Security.Cryptography.Pkcs.ContentInfo contentInfo, System.Security.Cryptography.Pkcs.AlgorithmIdentifier encryptionAlgorithm) { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Certificates { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Security.Cryptography.Pkcs.AlgorithmIdentifier ContentEncryptionAlgorithm { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Security.Cryptography.Pkcs.ContentInfo ContentInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Security.Cryptography.Pkcs.RecipientInfoCollection RecipientInfos { get { throw null; } }
        public System.Security.Cryptography.CryptographicAttributeObjectCollection UnprotectedAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Decode(byte[] encodedMessage) { }
        public void Decrypt() { }
        public void Decrypt(System.Security.Cryptography.Pkcs.RecipientInfo recipientInfo) { }
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
        public System.Security.Cryptography.CryptographicAttributeObject OtherKeyAttribute { get { throw null; } }
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
    public partial class Pkcs9AttributeObject : System.Security.Cryptography.AsnEncodedData
    {
        public Pkcs9AttributeObject() { }
        public Pkcs9AttributeObject(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
        public Pkcs9AttributeObject(System.Security.Cryptography.Oid oid, byte[] encodedData) { }
        public Pkcs9AttributeObject(string oid, byte[] encodedData) { }
        public new System.Security.Cryptography.Oid Oid { get { throw null; } }
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
        public System.Security.Cryptography.Pkcs.AlgorithmIdentifier Algorithm { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public byte[] KeyValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public abstract partial class RecipientInfo
    {
        internal RecipientInfo() { }
        public abstract byte[] EncryptedKey { get; }
        public abstract System.Security.Cryptography.Pkcs.AlgorithmIdentifier KeyEncryptionAlgorithm { get; }
        public abstract System.Security.Cryptography.Pkcs.SubjectIdentifier RecipientIdentifier { get; }
        public System.Security.Cryptography.Pkcs.RecipientInfoType Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public abstract int Version { get; }
    }
    public sealed partial class RecipientInfoCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal RecipientInfoCollection() { }
        public int Count { get { throw null; } }
        public System.Security.Cryptography.Pkcs.RecipientInfo this[int index] { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
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
        KeyAgreement = 2,
        KeyTransport = 1,
        Unknown = 0,
    }
    public sealed partial class SignedCms
    {
        public SignedCms() => throw null;
        public SignedCms(SubjectIdentifierType signerIdentifierType) => throw null;
        public SignedCms(ContentInfo contentInfo) => throw null;
        public SignedCms(SubjectIdentifierType signerIdentifierType, ContentInfo contentInfo) => throw null;
        public SignedCms(ContentInfo contentInfo, bool detached) => throw null;
        public SignedCms(SubjectIdentifierType signerIdentifierType, ContentInfo contentInfo, bool detached) => throw null;
        public int Version => throw null;
        public ContentInfo ContentInfo => throw null;
        public bool Detached => throw null;
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Certificates => throw null;
        public SignerInfoCollection SignerInfos => throw null;
        public byte[] Encode() => throw null;
        public void Decode(byte[] encodedMessage) => throw null;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void ComputeSignature() => throw null;
        public void ComputeSignature(CmsSigner signer) => throw null;
        public void ComputeSignature(CmsSigner signer, bool silent) => throw null;
        public void RemoveSignature(int index) => throw null;
        public void RemoveSignature(SignerInfo signerInfo) => throw null;
        public void CheckSignature(bool verifySignatureOnly) => throw null;
        public void CheckSignature(System.Security.Cryptography.X509Certificates.X509Certificate2Collection extraStore, bool verifySignatureOnly) => throw null;
        public void CheckHash() => throw null;
    }
    public sealed partial class SignerInfo
    {
        private SignerInfo() => throw null;
        public int Version => throw null;
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate => throw null;
        public SubjectIdentifier SignerIdentifier => throw null;
        public Oid DigestAlgorithm => throw null;
        public CryptographicAttributeObjectCollection SignedAttributes => throw null;
        public CryptographicAttributeObjectCollection UnsignedAttributes => throw null;
        public SignerInfoCollection CounterSignerInfos => throw null;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void ComputeCounterSignature() => throw null;
        public void ComputeCounterSignature(CmsSigner signer) => throw null;
        public void RemoveCounterSignature(int index) => throw null;
        public void RemoveCounterSignature(SignerInfo counterSignerInfo) => throw null;
        public void CheckSignature(bool verifySignatureOnly) => throw null;
        public void CheckSignature(System.Security.Cryptography.X509Certificates.X509Certificate2Collection extraStore, bool verifySignatureOnly) => throw null;
        public void CheckHash() => throw null;
    }
    public sealed partial class SignerInfoCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal SignerInfoCollection() => throw null;
        public SignerInfo this[int index] => throw null;
        public int Count => throw null;
        public SignerInfoEnumerator GetEnumerator() => throw null;
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw null;
        public void CopyTo(Array array, int index) => throw null;
        public void CopyTo(SignerInfo[] array, int index) => throw null;
        public bool IsSynchronized => throw null;
        public object SyncRoot => throw null;
    }
    public sealed partial class SignerInfoEnumerator : System.Collections.IEnumerator
    {
        private SignerInfoEnumerator() { }
        public SignerInfo Current => throw null;
        object System.Collections.IEnumerator.Current => throw null;
        public bool MoveNext() => throw null;
        public void Reset() => throw null;
    }
    public sealed partial class SubjectIdentifier
    {
        internal SubjectIdentifier() { }
        public System.Security.Cryptography.Pkcs.SubjectIdentifierType Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public sealed partial class SubjectIdentifierOrKey
    {
        internal SubjectIdentifierOrKey() { }
        public System.Security.Cryptography.Pkcs.SubjectIdentifierOrKeyType Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public enum SubjectIdentifierOrKeyType
    {
        IssuerAndSerialNumber = 1,
        PublicKeyInfo = 3,
        SubjectKeyIdentifier = 2,
        Unknown = 0,
    }
    public enum SubjectIdentifierType
    {
        IssuerAndSerialNumber = 1,
        NoSignature = 3,
        SubjectKeyIdentifier = 2,
        Unknown = 0,
    }
}
namespace System.Security.Cryptography.Xml
{
    public partial struct X509IssuerSerial
    {
        private object _dummy;
        public string IssuerName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SerialNumber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
