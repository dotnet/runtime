// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography.Xml
{
    public sealed partial class CipherData
    {
        public CipherData() { }
        public CipherData(byte[] cipherValue) { }
        public CipherData(System.Security.Cryptography.Xml.CipherReference cipherReference) { }
        public System.Security.Cryptography.Xml.CipherReference CipherReference { get { throw null; } set { } }
        public byte[] CipherValue { get { throw null; } set { } }
        public System.Xml.XmlElement GetXml() { throw null; }
        public void LoadXml(System.Xml.XmlElement value) { }
    }
    public sealed partial class CipherReference : System.Security.Cryptography.Xml.EncryptedReference
    {
        public CipherReference() { }
        public CipherReference(string uri) { }
        public CipherReference(string uri, System.Security.Cryptography.Xml.TransformChain transformChain) { }
        public override System.Xml.XmlElement GetXml() { throw null; }
        public override void LoadXml(System.Xml.XmlElement value) { }
    }
    public partial class DataObject
    {
        public DataObject() { }
        public DataObject(string id, string mimeType, string encoding, System.Xml.XmlElement data) { }
        public System.Xml.XmlNodeList Data { get { throw null; } set { } }
        public string Encoding { get { throw null; } set { } }
        public string Id { get { throw null; } set { } }
        public string MimeType { get { throw null; } set { } }
        public System.Xml.XmlElement GetXml() { throw null; }
        public void LoadXml(System.Xml.XmlElement value) { }
    }
    public sealed partial class DataReference : System.Security.Cryptography.Xml.EncryptedReference
    {
        public DataReference() { }
        public DataReference(string uri) { }
        public DataReference(string uri, System.Security.Cryptography.Xml.TransformChain transformChain) { }
    }
    public partial class DSAKeyValue : System.Security.Cryptography.Xml.KeyInfoClause
    {
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("maccatalyst")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public DSAKeyValue() { }
        public DSAKeyValue(System.Security.Cryptography.DSA key) { }
        public System.Security.Cryptography.DSA Key { get { throw null; } set { } }
        public override System.Xml.XmlElement GetXml() { throw null; }
        public override void LoadXml(System.Xml.XmlElement value) { }
    }
    public sealed partial class EncryptedData : System.Security.Cryptography.Xml.EncryptedType
    {
        public EncryptedData() { }
        public override System.Xml.XmlElement GetXml() { throw null; }
        public override void LoadXml(System.Xml.XmlElement value) { }
    }
    public sealed partial class EncryptedKey : System.Security.Cryptography.Xml.EncryptedType
    {
        public EncryptedKey() { }
        public string CarriedKeyName { get { throw null; } set { } }
        public string Recipient { get { throw null; } set { } }
        public System.Security.Cryptography.Xml.ReferenceList ReferenceList { get { throw null; } }
        public void AddReference(System.Security.Cryptography.Xml.DataReference dataReference) { }
        public void AddReference(System.Security.Cryptography.Xml.KeyReference keyReference) { }
        public override System.Xml.XmlElement GetXml() { throw null; }
        public override void LoadXml(System.Xml.XmlElement value) { }
    }
    public abstract partial class EncryptedReference
    {
        protected EncryptedReference() { }
        protected EncryptedReference(string uri) { }
        protected EncryptedReference(string uri, System.Security.Cryptography.Xml.TransformChain transformChain) { }
        protected internal bool CacheValid { get { throw null; } }
        protected string ReferenceType { get { throw null; } set { } }
        public System.Security.Cryptography.Xml.TransformChain TransformChain { get { throw null; } set { } }
        public string Uri { get { throw null; } set { } }
        public void AddTransform(System.Security.Cryptography.Xml.Transform transform) { }
        public virtual System.Xml.XmlElement GetXml() { throw null; }
        public virtual void LoadXml(System.Xml.XmlElement value) { }
    }
    public abstract partial class EncryptedType
    {
        protected EncryptedType() { }
        public virtual System.Security.Cryptography.Xml.CipherData CipherData { get { throw null; } set { } }
        public virtual string Encoding { get { throw null; } set { } }
        public virtual System.Security.Cryptography.Xml.EncryptionMethod EncryptionMethod { get { throw null; } set { } }
        public virtual System.Security.Cryptography.Xml.EncryptionPropertyCollection EncryptionProperties { get { throw null; } }
        public virtual string Id { get { throw null; } set { } }
        public System.Security.Cryptography.Xml.KeyInfo KeyInfo { get { throw null; } set { } }
        public virtual string MimeType { get { throw null; } set { } }
        public virtual string Type { get { throw null; } set { } }
        public void AddProperty(System.Security.Cryptography.Xml.EncryptionProperty ep) { }
        public abstract System.Xml.XmlElement GetXml();
        public abstract void LoadXml(System.Xml.XmlElement value);
    }
    public partial class EncryptedXml
    {
        public const string XmlEncAES128KeyWrapUrl = "http://www.w3.org/2001/04/xmlenc#kw-aes128";
        public const string XmlEncAES128Url = "http://www.w3.org/2001/04/xmlenc#aes128-cbc";
        public const string XmlEncAES192KeyWrapUrl = "http://www.w3.org/2001/04/xmlenc#kw-aes192";
        public const string XmlEncAES192Url = "http://www.w3.org/2001/04/xmlenc#aes192-cbc";
        public const string XmlEncAES256KeyWrapUrl = "http://www.w3.org/2001/04/xmlenc#kw-aes256";
        public const string XmlEncAES256Url = "http://www.w3.org/2001/04/xmlenc#aes256-cbc";
        public const string XmlEncDESUrl = "http://www.w3.org/2001/04/xmlenc#des-cbc";
        public const string XmlEncElementContentUrl = "http://www.w3.org/2001/04/xmlenc#Content";
        public const string XmlEncElementUrl = "http://www.w3.org/2001/04/xmlenc#Element";
        public const string XmlEncEncryptedKeyUrl = "http://www.w3.org/2001/04/xmlenc#EncryptedKey";
        public const string XmlEncNamespaceUrl = "http://www.w3.org/2001/04/xmlenc#";
        public const string XmlEncRSA15Url = "http://www.w3.org/2001/04/xmlenc#rsa-1_5";
        public const string XmlEncRSAOAEPUrl = "http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p";
        public const string XmlEncSHA256Url = "http://www.w3.org/2001/04/xmlenc#sha256";
        public const string XmlEncSHA512Url = "http://www.w3.org/2001/04/xmlenc#sha512";
        public const string XmlEncTripleDESKeyWrapUrl = "http://www.w3.org/2001/04/xmlenc#kw-tripledes";
        public const string XmlEncTripleDESUrl = "http://www.w3.org/2001/04/xmlenc#tripledes-cbc";
        public EncryptedXml() { }
        public EncryptedXml(System.Xml.XmlDocument document) { }
        public EncryptedXml(System.Xml.XmlDocument document, System.Security.Policy.Evidence evidence) { }
        public System.Security.Policy.Evidence DocumentEvidence { get { throw null; } set { } }
        public System.Text.Encoding Encoding { get { throw null; } set { } }
        public System.Security.Cryptography.CipherMode Mode { get { throw null; } set { } }
        public System.Security.Cryptography.PaddingMode Padding { get { throw null; } set { } }
        public string Recipient { get { throw null; } set { } }
        public System.Xml.XmlResolver Resolver { get { throw null; } set { } }
        public int XmlDSigSearchDepth { get { throw null; } set { } }
        public void AddKeyNameMapping(string keyName, object keyObject) { }
        public void ClearKeyNameMappings() { }
        public byte[] DecryptData(System.Security.Cryptography.Xml.EncryptedData encryptedData, System.Security.Cryptography.SymmetricAlgorithm symmetricAlgorithm) { throw null; }
        public void DecryptDocument() { }
        public virtual byte[] DecryptEncryptedKey(System.Security.Cryptography.Xml.EncryptedKey encryptedKey) { throw null; }
        public static byte[] DecryptKey(byte[] keyData, System.Security.Cryptography.RSA rsa, bool useOAEP) { throw null; }
        public static byte[] DecryptKey(byte[] keyData, System.Security.Cryptography.SymmetricAlgorithm symmetricAlgorithm) { throw null; }
        public System.Security.Cryptography.Xml.EncryptedData Encrypt(System.Xml.XmlElement inputElement, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        public System.Security.Cryptography.Xml.EncryptedData Encrypt(System.Xml.XmlElement inputElement, string keyName) { throw null; }
        public byte[] EncryptData(byte[] plaintext, System.Security.Cryptography.SymmetricAlgorithm symmetricAlgorithm) { throw null; }
        public byte[] EncryptData(System.Xml.XmlElement inputElement, System.Security.Cryptography.SymmetricAlgorithm symmetricAlgorithm, bool content) { throw null; }
        public static byte[] EncryptKey(byte[] keyData, System.Security.Cryptography.RSA rsa, bool useOAEP) { throw null; }
        public static byte[] EncryptKey(byte[] keyData, System.Security.Cryptography.SymmetricAlgorithm symmetricAlgorithm) { throw null; }
        public virtual byte[] GetDecryptionIV(System.Security.Cryptography.Xml.EncryptedData encryptedData, string symmetricAlgorithmUri) { throw null; }
        public virtual System.Security.Cryptography.SymmetricAlgorithm GetDecryptionKey(System.Security.Cryptography.Xml.EncryptedData encryptedData, string symmetricAlgorithmUri) { throw null; }
        public virtual System.Xml.XmlElement GetIdElement(System.Xml.XmlDocument document, string idValue) { throw null; }
        public void ReplaceData(System.Xml.XmlElement inputElement, byte[] decryptedData) { }
        public static void ReplaceElement(System.Xml.XmlElement inputElement, System.Security.Cryptography.Xml.EncryptedData encryptedData, bool content) { }
    }
    public partial class EncryptionMethod
    {
        public EncryptionMethod() { }
        public EncryptionMethod(string algorithm) { }
        public string KeyAlgorithm { get { throw null; } set { } }
        public int KeySize { get { throw null; } set { } }
        public System.Xml.XmlElement GetXml() { throw null; }
        public void LoadXml(System.Xml.XmlElement value) { }
    }
    public sealed partial class EncryptionProperty
    {
        public EncryptionProperty() { }
        public EncryptionProperty(System.Xml.XmlElement elementProperty) { }
        public string Id { get { throw null; } }
        public System.Xml.XmlElement PropertyElement { get { throw null; } set { } }
        public string Target { get { throw null; } }
        public System.Xml.XmlElement GetXml() { throw null; }
        public void LoadXml(System.Xml.XmlElement value) { }
    }
    public sealed partial class EncryptionPropertyCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public EncryptionPropertyCollection() { }
        public int Count { get { throw null; } }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public System.Security.Cryptography.Xml.EncryptionProperty this[int index] { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public int Add(System.Security.Cryptography.Xml.EncryptionProperty value) { throw null; }
        public void Clear() { }
        public bool Contains(System.Security.Cryptography.Xml.EncryptionProperty value) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Security.Cryptography.Xml.EncryptionProperty[] array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public int IndexOf(System.Security.Cryptography.Xml.EncryptionProperty value) { throw null; }
        public void Insert(int index, System.Security.Cryptography.Xml.EncryptionProperty value) { }
        public System.Security.Cryptography.Xml.EncryptionProperty Item(int index) { throw null; }
        public void Remove(System.Security.Cryptography.Xml.EncryptionProperty value) { }
        public void RemoveAt(int index) { }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    public partial interface IRelDecryptor
    {
        System.IO.Stream Decrypt(System.Security.Cryptography.Xml.EncryptionMethod encryptionMethod, System.Security.Cryptography.Xml.KeyInfo keyInfo, System.IO.Stream toDecrypt);
    }
    public partial class KeyInfo : System.Collections.IEnumerable
    {
        public KeyInfo() { }
        public int Count { get { throw null; } }
        public string Id { get { throw null; } set { } }
        public void AddClause(System.Security.Cryptography.Xml.KeyInfoClause clause) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public System.Collections.IEnumerator GetEnumerator(System.Type requestedObjectType) { throw null; }
        public System.Xml.XmlElement GetXml() { throw null; }
        public void LoadXml(System.Xml.XmlElement value) { }
    }
    public abstract partial class KeyInfoClause
    {
        protected KeyInfoClause() { }
        public abstract System.Xml.XmlElement GetXml();
        public abstract void LoadXml(System.Xml.XmlElement element);
    }
    public partial class KeyInfoEncryptedKey : System.Security.Cryptography.Xml.KeyInfoClause
    {
        public KeyInfoEncryptedKey() { }
        public KeyInfoEncryptedKey(System.Security.Cryptography.Xml.EncryptedKey encryptedKey) { }
        public System.Security.Cryptography.Xml.EncryptedKey EncryptedKey { get { throw null; } set { } }
        public override System.Xml.XmlElement GetXml() { throw null; }
        public override void LoadXml(System.Xml.XmlElement value) { }
    }
    public partial class KeyInfoName : System.Security.Cryptography.Xml.KeyInfoClause
    {
        public KeyInfoName() { }
        public KeyInfoName(string keyName) { }
        public string Value { get { throw null; } set { } }
        public override System.Xml.XmlElement GetXml() { throw null; }
        public override void LoadXml(System.Xml.XmlElement value) { }
    }
    public partial class KeyInfoNode : System.Security.Cryptography.Xml.KeyInfoClause
    {
        public KeyInfoNode() { }
        public KeyInfoNode(System.Xml.XmlElement node) { }
        public System.Xml.XmlElement Value { get { throw null; } set { } }
        public override System.Xml.XmlElement GetXml() { throw null; }
        public override void LoadXml(System.Xml.XmlElement value) { }
    }
    public partial class KeyInfoRetrievalMethod : System.Security.Cryptography.Xml.KeyInfoClause
    {
        public KeyInfoRetrievalMethod() { }
        public KeyInfoRetrievalMethod(string strUri) { }
        public KeyInfoRetrievalMethod(string strUri, string typeName) { }
        public string Type { get { throw null; } set { } }
        public string Uri { get { throw null; } set { } }
        public override System.Xml.XmlElement GetXml() { throw null; }
        public override void LoadXml(System.Xml.XmlElement value) { }
    }
    public partial class KeyInfoX509Data : System.Security.Cryptography.Xml.KeyInfoClause
    {
        public KeyInfoX509Data() { }
        public KeyInfoX509Data(byte[] rgbCert) { }
        public KeyInfoX509Data(System.Security.Cryptography.X509Certificates.X509Certificate cert) { }
        public KeyInfoX509Data(System.Security.Cryptography.X509Certificates.X509Certificate cert, System.Security.Cryptography.X509Certificates.X509IncludeOption includeOption) { }
        public System.Collections.ArrayList Certificates { get { throw null; } }
        public byte[] CRL { get { throw null; } set { } }
        public System.Collections.ArrayList IssuerSerials { get { throw null; } }
        public System.Collections.ArrayList SubjectKeyIds { get { throw null; } }
        public System.Collections.ArrayList SubjectNames { get { throw null; } }
        public void AddCertificate(System.Security.Cryptography.X509Certificates.X509Certificate certificate) { }
        public void AddIssuerSerial(string issuerName, string serialNumber) { }
        public void AddSubjectKeyId(byte[] subjectKeyId) { }
        public void AddSubjectKeyId(string subjectKeyId) { }
        public void AddSubjectName(string subjectName) { }
        public override System.Xml.XmlElement GetXml() { throw null; }
        public override void LoadXml(System.Xml.XmlElement element) { }
    }
    public sealed partial class KeyReference : System.Security.Cryptography.Xml.EncryptedReference
    {
        public KeyReference() { }
        public KeyReference(string uri) { }
        public KeyReference(string uri, System.Security.Cryptography.Xml.TransformChain transformChain) { }
    }
    public partial class Reference
    {
        public Reference() { }
        public Reference(System.IO.Stream stream) { }
        public Reference(string uri) { }
        public string DigestMethod { get { throw null; } set { } }
        public byte[] DigestValue { get { throw null; } set { } }
        public string Id { get { throw null; } set { } }
        public System.Security.Cryptography.Xml.TransformChain TransformChain { get { throw null; } set { } }
        public string Type { get { throw null; } set { } }
        public string Uri { get { throw null; } set { } }
        public void AddTransform(System.Security.Cryptography.Xml.Transform transform) { }
        public System.Xml.XmlElement GetXml() { throw null; }
        public void LoadXml(System.Xml.XmlElement value) { }
    }
    public sealed partial class ReferenceList : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public ReferenceList() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public System.Security.Cryptography.Xml.EncryptedReference this[int index] { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public int Add(object value) { throw null; }
        public void Clear() { }
        public bool Contains(object value) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public int IndexOf(object value) { throw null; }
        public void Insert(int index, object value) { }
        public System.Security.Cryptography.Xml.EncryptedReference Item(int index) { throw null; }
        public void Remove(object value) { }
        public void RemoveAt(int index) { }
    }
    public partial class RSAKeyValue : System.Security.Cryptography.Xml.KeyInfoClause
    {
        public RSAKeyValue() { }
        public RSAKeyValue(System.Security.Cryptography.RSA key) { }
        public System.Security.Cryptography.RSA Key { get { throw null; } set { } }
        public override System.Xml.XmlElement GetXml() { throw null; }
        public override void LoadXml(System.Xml.XmlElement value) { }
    }
    public partial class Signature
    {
        public Signature() { }
        public string Id { get { throw null; } set { } }
        public System.Security.Cryptography.Xml.KeyInfo KeyInfo { get { throw null; } set { } }
        public System.Collections.IList ObjectList { get { throw null; } set { } }
        public byte[] SignatureValue { get { throw null; } set { } }
        public System.Security.Cryptography.Xml.SignedInfo SignedInfo { get { throw null; } set { } }
        public void AddObject(System.Security.Cryptography.Xml.DataObject dataObject) { }
        public System.Xml.XmlElement GetXml() { throw null; }
        public void LoadXml(System.Xml.XmlElement value) { }
    }
    public partial class SignedInfo : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public SignedInfo() { }
        public string CanonicalizationMethod { get { throw null; } set { } }
        public System.Security.Cryptography.Xml.Transform CanonicalizationMethodObject { get { throw null; } }
        public int Count { get { throw null; } }
        public string Id { get { throw null; } set { } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Collections.ArrayList References { get { throw null; } }
        public string SignatureLength { get { throw null; } set { } }
        public string SignatureMethod { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        public void AddReference(System.Security.Cryptography.Xml.Reference reference) { }
        public void CopyTo(System.Array array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public System.Xml.XmlElement GetXml() { throw null; }
        public void LoadXml(System.Xml.XmlElement value) { }
    }
    public partial class SignedXml
    {
        protected System.Security.Cryptography.Xml.Signature m_signature;
        protected string m_strSigningKeyName;
        public const string XmlDecryptionTransformUrl = "http://www.w3.org/2002/07/decrypt#XML";
        public const string XmlDsigBase64TransformUrl = "http://www.w3.org/2000/09/xmldsig#base64";
        public const string XmlDsigC14NTransformUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
        public const string XmlDsigC14NWithCommentsTransformUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments";
        public const string XmlDsigCanonicalizationUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
        public const string XmlDsigCanonicalizationWithCommentsUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments";
        public const string XmlDsigDSAUrl = "http://www.w3.org/2000/09/xmldsig#dsa-sha1";
        public const string XmlDsigEnvelopedSignatureTransformUrl = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";
        public const string XmlDsigExcC14NTransformUrl = "http://www.w3.org/2001/10/xml-exc-c14n#";
        public const string XmlDsigExcC14NWithCommentsTransformUrl = "http://www.w3.org/2001/10/xml-exc-c14n#WithComments";
        public const string XmlDsigHMACSHA1Url = "http://www.w3.org/2000/09/xmldsig#hmac-sha1";
        public const string XmlDsigMinimalCanonicalizationUrl = "http://www.w3.org/2000/09/xmldsig#minimal";
        public const string XmlDsigNamespaceUrl = "http://www.w3.org/2000/09/xmldsig#";
        public const string XmlDsigRSASHA1Url = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
        public const string XmlDsigRSASHA256Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        public const string XmlDsigRSASHA384Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384";
        public const string XmlDsigRSASHA512Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";
        public const string XmlDsigSHA1Url = "http://www.w3.org/2000/09/xmldsig#sha1";
        public const string XmlDsigSHA256Url = "http://www.w3.org/2001/04/xmlenc#sha256";
        public const string XmlDsigSHA384Url = "http://www.w3.org/2001/04/xmldsig-more#sha384";
        public const string XmlDsigSHA512Url = "http://www.w3.org/2001/04/xmlenc#sha512";
        public const string XmlDsigXPathTransformUrl = "http://www.w3.org/TR/1999/REC-xpath-19991116";
        public const string XmlDsigXsltTransformUrl = "http://www.w3.org/TR/1999/REC-xslt-19991116";
        public const string XmlLicenseTransformUrl = "urn:mpeg:mpeg21:2003:01-REL-R-NS:licenseTransform";
        public SignedXml() { }
        public SignedXml(System.Xml.XmlDocument document) { }
        public SignedXml(System.Xml.XmlElement elem) { }
        public System.Security.Cryptography.Xml.EncryptedXml EncryptedXml { get { throw null; } set { } }
        public System.Security.Cryptography.Xml.KeyInfo KeyInfo { get { throw null; } set { } }
        public System.Xml.XmlResolver Resolver { set { } }
        public System.Collections.ObjectModel.Collection<string> SafeCanonicalizationMethods { get { throw null; } }
        public System.Security.Cryptography.Xml.Signature Signature { get { throw null; } }
        public System.Func<System.Security.Cryptography.Xml.SignedXml, bool> SignatureFormatValidator { get { throw null; } set { } }
        public string SignatureLength { get { throw null; } }
        public string SignatureMethod { get { throw null; } }
        public byte[] SignatureValue { get { throw null; } }
        public System.Security.Cryptography.Xml.SignedInfo SignedInfo { get { throw null; } }
        public System.Security.Cryptography.AsymmetricAlgorithm SigningKey { get { throw null; } set { } }
        public string SigningKeyName { get { throw null; } set { } }
        public void AddObject(System.Security.Cryptography.Xml.DataObject dataObject) { }
        public void AddReference(System.Security.Cryptography.Xml.Reference reference) { }
        public bool CheckSignature() { throw null; }
        public bool CheckSignature(System.Security.Cryptography.AsymmetricAlgorithm key) { throw null; }
        public bool CheckSignature(System.Security.Cryptography.KeyedHashAlgorithm macAlg) { throw null; }
        public bool CheckSignature(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, bool verifySignatureOnly) { throw null; }
        public bool CheckSignatureReturningKey(out System.Security.Cryptography.AsymmetricAlgorithm signingKey) { throw null; }
        public void ComputeSignature() { }
        public void ComputeSignature(System.Security.Cryptography.KeyedHashAlgorithm macAlg) { }
        public virtual System.Xml.XmlElement GetIdElement(System.Xml.XmlDocument document, string idValue) { throw null; }
        protected virtual System.Security.Cryptography.AsymmetricAlgorithm GetPublicKey() { throw null; }
        public System.Xml.XmlElement GetXml() { throw null; }
        public void LoadXml(System.Xml.XmlElement value) { }
    }
    public abstract partial class Transform
    {
        protected Transform() { }
        public string Algorithm { get { throw null; } set { } }
        public System.Xml.XmlElement Context { get { throw null; } set { } }
        public abstract System.Type[] InputTypes { get; }
        public abstract System.Type[] OutputTypes { get; }
        public System.Collections.Hashtable PropagatedNamespaces { get { throw null; } }
        public System.Xml.XmlResolver Resolver { set { } }
        public virtual byte[] GetDigestedOutput(System.Security.Cryptography.HashAlgorithm hash) { throw null; }
        protected abstract System.Xml.XmlNodeList GetInnerXml();
        public abstract object GetOutput();
        public abstract object GetOutput(System.Type type);
        public System.Xml.XmlElement GetXml() { throw null; }
        public abstract void LoadInnerXml(System.Xml.XmlNodeList nodeList);
        public abstract void LoadInput(object obj);
    }
    public partial class TransformChain
    {
        public TransformChain() { }
        public int Count { get { throw null; } }
        public System.Security.Cryptography.Xml.Transform this[int index] { get { throw null; } }
        public void Add(System.Security.Cryptography.Xml.Transform transform) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
    }
    public partial class XmlDecryptionTransform : System.Security.Cryptography.Xml.Transform
    {
        public XmlDecryptionTransform() { }
        public System.Security.Cryptography.Xml.EncryptedXml EncryptedXml { get { throw null; } set { } }
        public override System.Type[] InputTypes { get { throw null; } }
        public override System.Type[] OutputTypes { get { throw null; } }
        public void AddExceptUri(string uri) { }
        protected override System.Xml.XmlNodeList GetInnerXml() { throw null; }
        public override object GetOutput() { throw null; }
        public override object GetOutput(System.Type type) { throw null; }
        protected virtual bool IsTargetElement(System.Xml.XmlElement inputElement, string idValue) { throw null; }
        public override void LoadInnerXml(System.Xml.XmlNodeList nodeList) { }
        public override void LoadInput(object obj) { }
    }
    public partial class XmlDsigBase64Transform : System.Security.Cryptography.Xml.Transform
    {
        public XmlDsigBase64Transform() { }
        public override System.Type[] InputTypes { get { throw null; } }
        public override System.Type[] OutputTypes { get { throw null; } }
        protected override System.Xml.XmlNodeList GetInnerXml() { throw null; }
        public override object GetOutput() { throw null; }
        public override object GetOutput(System.Type type) { throw null; }
        public override void LoadInnerXml(System.Xml.XmlNodeList nodeList) { }
        public override void LoadInput(object obj) { }
    }
    public partial class XmlDsigC14NTransform : System.Security.Cryptography.Xml.Transform
    {
        public XmlDsigC14NTransform() { }
        public XmlDsigC14NTransform(bool includeComments) { }
        public override System.Type[] InputTypes { get { throw null; } }
        public override System.Type[] OutputTypes { get { throw null; } }
        public override byte[] GetDigestedOutput(System.Security.Cryptography.HashAlgorithm hash) { throw null; }
        protected override System.Xml.XmlNodeList GetInnerXml() { throw null; }
        public override object GetOutput() { throw null; }
        public override object GetOutput(System.Type type) { throw null; }
        public override void LoadInnerXml(System.Xml.XmlNodeList nodeList) { }
        public override void LoadInput(object obj) { }
    }
    public partial class XmlDsigC14NWithCommentsTransform : System.Security.Cryptography.Xml.XmlDsigC14NTransform
    {
        public XmlDsigC14NWithCommentsTransform() { }
    }
    public partial class XmlDsigEnvelopedSignatureTransform : System.Security.Cryptography.Xml.Transform
    {
        public XmlDsigEnvelopedSignatureTransform() { }
        public XmlDsigEnvelopedSignatureTransform(bool includeComments) { }
        public override System.Type[] InputTypes { get { throw null; } }
        public override System.Type[] OutputTypes { get { throw null; } }
        protected override System.Xml.XmlNodeList GetInnerXml() { throw null; }
        public override object GetOutput() { throw null; }
        public override object GetOutput(System.Type type) { throw null; }
        public override void LoadInnerXml(System.Xml.XmlNodeList nodeList) { }
        public override void LoadInput(object obj) { }
    }
    public partial class XmlDsigExcC14NTransform : System.Security.Cryptography.Xml.Transform
    {
        public XmlDsigExcC14NTransform() { }
        public XmlDsigExcC14NTransform(bool includeComments) { }
        public XmlDsigExcC14NTransform(bool includeComments, string inclusiveNamespacesPrefixList) { }
        public XmlDsigExcC14NTransform(string inclusiveNamespacesPrefixList) { }
        public string InclusiveNamespacesPrefixList { get { throw null; } set { } }
        public override System.Type[] InputTypes { get { throw null; } }
        public override System.Type[] OutputTypes { get { throw null; } }
        public override byte[] GetDigestedOutput(System.Security.Cryptography.HashAlgorithm hash) { throw null; }
        protected override System.Xml.XmlNodeList GetInnerXml() { throw null; }
        public override object GetOutput() { throw null; }
        public override object GetOutput(System.Type type) { throw null; }
        public override void LoadInnerXml(System.Xml.XmlNodeList nodeList) { }
        public override void LoadInput(object obj) { }
    }
    public partial class XmlDsigExcC14NWithCommentsTransform : System.Security.Cryptography.Xml.XmlDsigExcC14NTransform
    {
        public XmlDsigExcC14NWithCommentsTransform() { }
        public XmlDsigExcC14NWithCommentsTransform(string inclusiveNamespacesPrefixList) { }
    }
    public partial class XmlDsigXPathTransform : System.Security.Cryptography.Xml.Transform
    {
        public XmlDsigXPathTransform() { }
        public override System.Type[] InputTypes { get { throw null; } }
        public override System.Type[] OutputTypes { get { throw null; } }
        protected override System.Xml.XmlNodeList GetInnerXml() { throw null; }
        public override object GetOutput() { throw null; }
        public override object GetOutput(System.Type type) { throw null; }
        public override void LoadInnerXml(System.Xml.XmlNodeList nodeList) { }
        public override void LoadInput(object obj) { }
    }
    public partial class XmlDsigXsltTransform : System.Security.Cryptography.Xml.Transform
    {
        public XmlDsigXsltTransform() { }
        public XmlDsigXsltTransform(bool includeComments) { }
        public override System.Type[] InputTypes { get { throw null; } }
        public override System.Type[] OutputTypes { get { throw null; } }
        protected override System.Xml.XmlNodeList GetInnerXml() { throw null; }
        public override object GetOutput() { throw null; }
        public override object GetOutput(System.Type type) { throw null; }
        public override void LoadInnerXml(System.Xml.XmlNodeList nodeList) { }
        public override void LoadInput(object obj) { }
    }
    public partial class XmlLicenseTransform : System.Security.Cryptography.Xml.Transform
    {
        public XmlLicenseTransform() { }
        public System.Security.Cryptography.Xml.IRelDecryptor Decryptor { get { throw null; } set { } }
        public override System.Type[] InputTypes { get { throw null; } }
        public override System.Type[] OutputTypes { get { throw null; } }
        protected override System.Xml.XmlNodeList GetInnerXml() { throw null; }
        public override object GetOutput() { throw null; }
        public override object GetOutput(System.Type type) { throw null; }
        public override void LoadInnerXml(System.Xml.XmlNodeList nodeList) { }
        public override void LoadInput(object obj) { }
    }
}
