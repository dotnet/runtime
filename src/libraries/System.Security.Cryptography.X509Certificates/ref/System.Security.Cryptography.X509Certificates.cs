// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeX509ChainHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeX509ChainHandle() : base (default(bool)) { }
        protected override void Dispose(bool disposing) { }
        protected override bool ReleaseHandle() { throw null; }
    }
}
namespace System.Security.Cryptography.X509Certificates
{
    public sealed partial class CertificateRequest
    {
        public CertificateRequest(System.Security.Cryptography.X509Certificates.X500DistinguishedName subjectName, System.Security.Cryptography.ECDsa key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public CertificateRequest(System.Security.Cryptography.X509Certificates.X500DistinguishedName subjectName, System.Security.Cryptography.RSA key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { }
        public CertificateRequest(System.Security.Cryptography.X509Certificates.X500DistinguishedName subjectName, System.Security.Cryptography.X509Certificates.PublicKey publicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public CertificateRequest(string subjectName, System.Security.Cryptography.ECDsa key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public CertificateRequest(string subjectName, System.Security.Cryptography.RSA key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { }
        public System.Collections.ObjectModel.Collection<System.Security.Cryptography.X509Certificates.X509Extension> CertificateExtensions { get { throw null; } }
        public System.Security.Cryptography.HashAlgorithmName HashAlgorithm { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.PublicKey PublicKey { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName SubjectName { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Create(System.Security.Cryptography.X509Certificates.X500DistinguishedName issuerName, System.Security.Cryptography.X509Certificates.X509SignatureGenerator generator, System.DateTimeOffset notBefore, System.DateTimeOffset notAfter, byte[] serialNumber) { throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Create(System.Security.Cryptography.X509Certificates.X500DistinguishedName issuerName, System.Security.Cryptography.X509Certificates.X509SignatureGenerator generator, System.DateTimeOffset notBefore, System.DateTimeOffset notAfter, System.ReadOnlySpan<byte> serialNumber) { throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Create(System.Security.Cryptography.X509Certificates.X509Certificate2 issuerCertificate, System.DateTimeOffset notBefore, System.DateTimeOffset notAfter, byte[] serialNumber) { throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Create(System.Security.Cryptography.X509Certificates.X509Certificate2 issuerCertificate, System.DateTimeOffset notBefore, System.DateTimeOffset notAfter, System.ReadOnlySpan<byte> serialNumber) { throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 CreateSelfSigned(System.DateTimeOffset notBefore, System.DateTimeOffset notAfter) { throw null; }
        public byte[] CreateSigningRequest() { throw null; }
        public byte[] CreateSigningRequest(System.Security.Cryptography.X509Certificates.X509SignatureGenerator signatureGenerator) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("maccatalyst")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
    public static partial class DSACertificateExtensions
    {
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CopyWithPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.Security.Cryptography.DSA privateKey) { throw null; }
        public static System.Security.Cryptography.DSA? GetDSAPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        public static System.Security.Cryptography.DSA? GetDSAPublicKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
    }
    public static partial class ECDsaCertificateExtensions
    {
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CopyWithPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.Security.Cryptography.ECDsa privateKey) { throw null; }
        public static System.Security.Cryptography.ECDsa? GetECDsaPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        public static System.Security.Cryptography.ECDsa? GetECDsaPublicKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
    }
    [System.FlagsAttribute]
    public enum OpenFlags
    {
        ReadOnly = 0,
        ReadWrite = 1,
        MaxAllowed = 2,
        OpenExistingOnly = 4,
        IncludeArchived = 8,
    }
    public sealed partial class PublicKey
    {
        public PublicKey(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public PublicKey(System.Security.Cryptography.Oid oid, System.Security.Cryptography.AsnEncodedData parameters, System.Security.Cryptography.AsnEncodedData keyValue) { }
        public System.Security.Cryptography.AsnEncodedData EncodedKeyValue { get { throw null; } }
        public System.Security.Cryptography.AsnEncodedData EncodedParameters { get { throw null; } }
        public System.Security.Cryptography.AsymmetricAlgorithm Key { get { throw null; } }
        public System.Security.Cryptography.Oid Oid { get { throw null; } }
        public static System.Security.Cryptography.X509Certificates.PublicKey CreateFromSubjectPublicKeyInfo(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public byte[] ExportSubjectPublicKeyInfo() { throw null; }
        public bool TryExportSubjectPublicKeyInfo(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public static partial class RSACertificateExtensions
    {
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CopyWithPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.Security.Cryptography.RSA privateKey) { throw null; }
        public static System.Security.Cryptography.RSA? GetRSAPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        public static System.Security.Cryptography.RSA? GetRSAPublicKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
    }
    public enum StoreLocation
    {
        CurrentUser = 1,
        LocalMachine = 2,
    }
    public enum StoreName
    {
        AddressBook = 1,
        AuthRoot = 2,
        CertificateAuthority = 3,
        Disallowed = 4,
        My = 5,
        Root = 6,
        TrustedPeople = 7,
        TrustedPublisher = 8,
    }
    public sealed partial class SubjectAlternativeNameBuilder
    {
        public SubjectAlternativeNameBuilder() { }
        public void AddDnsName(string dnsName) { }
        public void AddEmailAddress(string emailAddress) { }
        public void AddIpAddress(System.Net.IPAddress ipAddress) { }
        public void AddUri(System.Uri uri) { }
        public void AddUserPrincipalName(string upn) { }
        public System.Security.Cryptography.X509Certificates.X509Extension Build(bool critical = false) { throw null; }
    }
    public sealed partial class X500DistinguishedName : System.Security.Cryptography.AsnEncodedData
    {
        public X500DistinguishedName(byte[] encodedDistinguishedName) { }
        public X500DistinguishedName(System.ReadOnlySpan<byte> encodedDistinguishedName) { }
        public X500DistinguishedName(System.Security.Cryptography.AsnEncodedData encodedDistinguishedName) { }
        public X500DistinguishedName(System.Security.Cryptography.X509Certificates.X500DistinguishedName distinguishedName) { }
        public X500DistinguishedName(string distinguishedName) { }
        public X500DistinguishedName(string distinguishedName, System.Security.Cryptography.X509Certificates.X500DistinguishedNameFlags flag) { }
        public string Name { get { throw null; } }
        public string Decode(System.Security.Cryptography.X509Certificates.X500DistinguishedNameFlags flag) { throw null; }
        public override string Format(bool multiLine) { throw null; }
    }
    [System.FlagsAttribute]
    public enum X500DistinguishedNameFlags
    {
        None = 0,
        Reversed = 1,
        UseSemicolons = 16,
        DoNotUsePlusSign = 32,
        DoNotUseQuotes = 64,
        UseCommas = 128,
        UseNewLines = 256,
        UseUTF8Encoding = 4096,
        UseT61Encoding = 8192,
        ForceUTF8Encoding = 16384,
    }
    public sealed partial class X509BasicConstraintsExtension : System.Security.Cryptography.X509Certificates.X509Extension
    {
        public X509BasicConstraintsExtension() { }
        public X509BasicConstraintsExtension(bool certificateAuthority, bool hasPathLengthConstraint, int pathLengthConstraint, bool critical) { }
        public X509BasicConstraintsExtension(System.Security.Cryptography.AsnEncodedData encodedBasicConstraints, bool critical) { }
        public bool CertificateAuthority { get { throw null; } }
        public bool HasPathLengthConstraint { get { throw null; } }
        public int PathLengthConstraint { get { throw null; } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public partial class X509Certificate : System.IDisposable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        public X509Certificate() { }
        public X509Certificate(byte[] data) { }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate(byte[] rawData, System.Security.SecureString? password) { }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate(byte[] rawData, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public X509Certificate(byte[] rawData, string? password) { }
        public X509Certificate(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public X509Certificate(System.IntPtr handle) { }
        public X509Certificate(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public X509Certificate(System.Security.Cryptography.X509Certificates.X509Certificate cert) { }
        public X509Certificate(string fileName) { }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate(string fileName, System.Security.SecureString? password) { }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate(string fileName, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public X509Certificate(string fileName, string? password) { }
        public X509Certificate(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public System.IntPtr Handle { get { throw null; } }
        public string Issuer { get { throw null; } }
        public string Subject { get { throw null; } }
        public static System.Security.Cryptography.X509Certificates.X509Certificate CreateFromCertFile(string filename) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate CreateFromSignedFile(string filename) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public virtual bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Security.Cryptography.X509Certificates.X509Certificate? other) { throw null; }
        public virtual byte[] Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public virtual byte[] Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType, System.Security.SecureString? password) { throw null; }
        public virtual byte[] Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType, string? password) { throw null; }
        protected static string FormatDate(System.DateTime date) { throw null; }
        public virtual byte[] GetCertHash() { throw null; }
        public virtual byte[] GetCertHash(System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public virtual string GetCertHashString() { throw null; }
        public virtual string GetCertHashString(System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public virtual string GetEffectiveDateString() { throw null; }
        public virtual string GetExpirationDateString() { throw null; }
        public virtual string GetFormat() { throw null; }
        public override int GetHashCode() { throw null; }
        [System.ObsoleteAttribute("This method has been deprecated.  Please use the Issuer property instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual string GetIssuerName() { throw null; }
        public virtual string GetKeyAlgorithm() { throw null; }
        public virtual byte[] GetKeyAlgorithmParameters() { throw null; }
        public virtual string GetKeyAlgorithmParametersString() { throw null; }
        [System.ObsoleteAttribute("This method has been deprecated.  Please use the Subject property instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual string GetName() { throw null; }
        public virtual byte[] GetPublicKey() { throw null; }
        public virtual string GetPublicKeyString() { throw null; }
        public virtual byte[] GetRawCertData() { throw null; }
        public virtual string GetRawCertDataString() { throw null; }
        public virtual byte[] GetSerialNumber() { throw null; }
        public virtual string GetSerialNumberString() { throw null; }
        public virtual void Import(byte[] rawData) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Import(byte[] rawData, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public virtual void Import(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public virtual void Import(string fileName) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Import(string fileName, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public virtual void Import(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public virtual void Reset() { }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object? sender) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
        public virtual string ToString(bool fVerbose) { throw null; }
        public virtual bool TryGetCertHash(System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public partial class X509Certificate2 : System.Security.Cryptography.X509Certificates.X509Certificate
    {
        public X509Certificate2() { }
        public X509Certificate2(byte[] rawData) { }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate2(byte[] rawData, System.Security.SecureString? password) { }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate2(byte[] rawData, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public X509Certificate2(byte[] rawData, string? password) { }
        public X509Certificate2(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public X509Certificate2(System.IntPtr handle) { }
        public X509Certificate2(System.ReadOnlySpan<byte> rawData) { }
        public X509Certificate2(System.ReadOnlySpan<byte> rawData, System.ReadOnlySpan<char> password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet) { }
        protected X509Certificate2(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public X509Certificate2(System.Security.Cryptography.X509Certificates.X509Certificate certificate) { }
        public X509Certificate2(string fileName) { }
        public X509Certificate2(string fileName, System.ReadOnlySpan<char> password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet) { }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate2(string fileName, System.Security.SecureString? password) { }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate2(string fileName, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public X509Certificate2(string fileName, string? password) { }
        public X509Certificate2(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public bool Archived { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509ExtensionCollection Extensions { get { throw null; } }
        public string FriendlyName { get { throw null; } set { } }
        public bool HasPrivateKey { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName IssuerName { get { throw null; } }
        public System.DateTime NotAfter { get { throw null; } }
        public System.DateTime NotBefore { get { throw null; } }
        public System.Security.Cryptography.AsymmetricAlgorithm? PrivateKey { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.PublicKey PublicKey { get { throw null; } }
        public byte[] RawData { get { throw null; } }
        public string SerialNumber { get { throw null; } }
        public System.Security.Cryptography.Oid SignatureAlgorithm { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName SubjectName { get { throw null; } }
        public string Thumbprint { get { throw null; } }
        public int Version { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 CopyWithPrivateKey(System.Security.Cryptography.ECDiffieHellman privateKey) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromEncryptedPem(System.ReadOnlySpan<char> certPem, System.ReadOnlySpan<char> keyPem, System.ReadOnlySpan<char> password) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromEncryptedPemFile(string certPemFilePath, System.ReadOnlySpan<char> password, string? keyPemFilePath = null) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromPem(System.ReadOnlySpan<char> certPem) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromPem(System.ReadOnlySpan<char> certPem, System.ReadOnlySpan<char> keyPem) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromPemFile(string certPemFilePath, string? keyPemFilePath = null) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509ContentType GetCertContentType(byte[] rawData) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509ContentType GetCertContentType(System.ReadOnlySpan<byte> rawData) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509ContentType GetCertContentType(string fileName) { throw null; }
        public System.Security.Cryptography.ECDiffieHellman? GetECDiffieHellmanPrivateKey() { throw null; }
        public System.Security.Cryptography.ECDiffieHellman? GetECDiffieHellmanPublicKey() { throw null; }
        public string GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType nameType, bool forIssuer) { throw null; }
        public override void Import(byte[] rawData) { }
        [System.CLSCompliantAttribute(false)]
        public override void Import(byte[] rawData, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public override void Import(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public override void Import(string fileName) { }
        [System.CLSCompliantAttribute(false)]
        public override void Import(string fileName, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public override void Import(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public override void Reset() { }
        public override string ToString() { throw null; }
        public override string ToString(bool verbose) { throw null; }
        public bool Verify() { throw null; }
    }
    public partial class X509Certificate2Collection : System.Security.Cryptography.X509Certificates.X509CertificateCollection
    {
        public X509Certificate2Collection() { }
        public X509Certificate2Collection(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public X509Certificate2Collection(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { }
        public X509Certificate2Collection(System.Security.Cryptography.X509Certificates.X509Certificate2[] certificates) { }
        public new System.Security.Cryptography.X509Certificates.X509Certificate2 this[int index] { get { throw null; } set { } }
        public int Add(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509Certificate2[] certificates) { }
        public bool Contains(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        public byte[]? Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType) { throw null; }
        public byte[]? Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType, string? password) { throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Find(System.Security.Cryptography.X509Certificates.X509FindType findType, object findValue, bool validOnly) { throw null; }
        public new System.Security.Cryptography.X509Certificates.X509Certificate2Enumerator GetEnumerator() { throw null; }
        public void Import(byte[] rawData) { }
        public void Import(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet) { }
        public void Import(System.ReadOnlySpan<byte> rawData) { }
        public void Import(System.ReadOnlySpan<byte> rawData, System.ReadOnlySpan<char> password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet) { }
        public void Import(System.ReadOnlySpan<byte> rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet) { }
        public void Import(string fileName) { }
        public void Import(string fileName, System.ReadOnlySpan<char> password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet) { }
        public void Import(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet) { }
        public void ImportFromPem(System.ReadOnlySpan<char> certPem) { }
        public void ImportFromPemFile(string certPemFilePath) { }
        public void Insert(int index, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public void Remove(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public void RemoveRange(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { }
        public void RemoveRange(System.Security.Cryptography.X509Certificates.X509Certificate2[] certificates) { }
    }
    public sealed partial class X509Certificate2Enumerator : System.Collections.IEnumerator
    {
        internal X509Certificate2Enumerator() { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
        bool System.Collections.IEnumerator.MoveNext() { throw null; }
        void System.Collections.IEnumerator.Reset() { }
    }
    public partial class X509CertificateCollection : System.Collections.CollectionBase
    {
        public X509CertificateCollection() { }
        public X509CertificateCollection(System.Security.Cryptography.X509Certificates.X509CertificateCollection value) { }
        public X509CertificateCollection(System.Security.Cryptography.X509Certificates.X509Certificate[] value) { }
        public System.Security.Cryptography.X509Certificates.X509Certificate this[int index] { get { throw null; } set { } }
        public int Add(System.Security.Cryptography.X509Certificates.X509Certificate value) { throw null; }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509CertificateCollection value) { }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509Certificate[] value) { }
        public bool Contains(System.Security.Cryptography.X509Certificates.X509Certificate value) { throw null; }
        public void CopyTo(System.Security.Cryptography.X509Certificates.X509Certificate[] array, int index) { }
        public new System.Security.Cryptography.X509Certificates.X509CertificateCollection.X509CertificateEnumerator GetEnumerator() { throw null; }
        public override int GetHashCode() { throw null; }
        public int IndexOf(System.Security.Cryptography.X509Certificates.X509Certificate value) { throw null; }
        public void Insert(int index, System.Security.Cryptography.X509Certificates.X509Certificate value) { }
        protected override void OnValidate(object value) { }
        public void Remove(System.Security.Cryptography.X509Certificates.X509Certificate value) { }
        public partial class X509CertificateEnumerator : System.Collections.IEnumerator
        {
            public X509CertificateEnumerator(System.Security.Cryptography.X509Certificates.X509CertificateCollection mappings) { }
            public System.Security.Cryptography.X509Certificates.X509Certificate Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            public void Reset() { }
            bool System.Collections.IEnumerator.MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public partial class X509Chain : System.IDisposable
    {
        public X509Chain() { }
        public X509Chain(bool useMachineContext) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public X509Chain(System.IntPtr chainContext) { }
        public System.IntPtr ChainContext { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509ChainElementCollection ChainElements { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509ChainPolicy ChainPolicy { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509ChainStatus[] ChainStatus { get { throw null; } }
        public Microsoft.Win32.SafeHandles.SafeX509ChainHandle? SafeHandle { get { throw null; } }
        public bool Build(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Chain Create() { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void Reset() { }
    }
    public partial class X509ChainElement
    {
        internal X509ChainElement() { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509ChainStatus[] ChainElementStatus { get { throw null; } }
        public string Information { get { throw null; } }
    }
    public sealed partial class X509ChainElementCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal X509ChainElementCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509ChainElement this[int index] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public void CopyTo(System.Security.Cryptography.X509Certificates.X509ChainElement[] array, int index) { }
        public System.Security.Cryptography.X509Certificates.X509ChainElementEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class X509ChainElementEnumerator : System.Collections.IEnumerator
    {
        internal X509ChainElementEnumerator() { }
        public System.Security.Cryptography.X509Certificates.X509ChainElement Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    public sealed partial class X509ChainPolicy
    {
        public X509ChainPolicy() { }
        public System.Security.Cryptography.OidCollection ApplicationPolicy { get { throw null; } }
        public System.Security.Cryptography.OidCollection CertificatePolicy { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection CustomTrustStore { get { throw null; } }
        public bool DisableCertificateDownloads { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection ExtraStore { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509RevocationFlag RevocationFlag { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509RevocationMode RevocationMode { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509ChainTrustMode TrustMode { get { throw null; } set { } }
        public System.TimeSpan UrlRetrievalTimeout { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509VerificationFlags VerificationFlags { get { throw null; } set { } }
        public System.DateTime VerificationTime { get { throw null; } set { } }
        public void Reset() { }
    }
    public partial struct X509ChainStatus
    {
        private object _dummy;
        private int _dummyPrimitive;
        public System.Security.Cryptography.X509Certificates.X509ChainStatusFlags Status { readonly get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string StatusInformation { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum X509ChainStatusFlags
    {
        NoError = 0,
        NotTimeValid = 1,
        NotTimeNested = 2,
        Revoked = 4,
        NotSignatureValid = 8,
        NotValidForUsage = 16,
        UntrustedRoot = 32,
        RevocationStatusUnknown = 64,
        Cyclic = 128,
        InvalidExtension = 256,
        InvalidPolicyConstraints = 512,
        InvalidBasicConstraints = 1024,
        InvalidNameConstraints = 2048,
        HasNotSupportedNameConstraint = 4096,
        HasNotDefinedNameConstraint = 8192,
        HasNotPermittedNameConstraint = 16384,
        HasExcludedNameConstraint = 32768,
        PartialChain = 65536,
        CtlNotTimeValid = 131072,
        CtlNotSignatureValid = 262144,
        CtlNotValidForUsage = 524288,
        HasWeakSignature = 1048576,
        OfflineRevocation = 16777216,
        NoIssuanceChainPolicy = 33554432,
        ExplicitDistrust = 67108864,
        HasNotSupportedCriticalExtension = 134217728,
    }
    public enum X509ChainTrustMode
    {
        System = 0,
        CustomRootTrust = 1,
    }
    public enum X509ContentType
    {
        Unknown = 0,
        Cert = 1,
        SerializedCert = 2,
        Pfx = 3,
        Pkcs12 = 3,
        SerializedStore = 4,
        Pkcs7 = 5,
        Authenticode = 6,
    }
    public sealed partial class X509EnhancedKeyUsageExtension : System.Security.Cryptography.X509Certificates.X509Extension
    {
        public X509EnhancedKeyUsageExtension() { }
        public X509EnhancedKeyUsageExtension(System.Security.Cryptography.AsnEncodedData encodedEnhancedKeyUsages, bool critical) { }
        public X509EnhancedKeyUsageExtension(System.Security.Cryptography.OidCollection enhancedKeyUsages, bool critical) { }
        public System.Security.Cryptography.OidCollection EnhancedKeyUsages { get { throw null; } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public partial class X509Extension : System.Security.Cryptography.AsnEncodedData
    {
        protected X509Extension() { }
        public X509Extension(System.Security.Cryptography.AsnEncodedData encodedExtension, bool critical) { }
        public X509Extension(System.Security.Cryptography.Oid oid, byte[] rawData, bool critical) { }
        public X509Extension(System.Security.Cryptography.Oid oid, System.ReadOnlySpan<byte> rawData, bool critical) { }
        public X509Extension(string oid, byte[] rawData, bool critical) { }
        public X509Extension(string oid, System.ReadOnlySpan<byte> rawData, bool critical) { }
        public bool Critical { get { throw null; } set { } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public sealed partial class X509ExtensionCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public X509ExtensionCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Extension this[int index] { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Extension? this[string oid] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public int Add(System.Security.Cryptography.X509Certificates.X509Extension extension) { throw null; }
        public void CopyTo(System.Security.Cryptography.X509Certificates.X509Extension[] array, int index) { }
        public System.Security.Cryptography.X509Certificates.X509ExtensionEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class X509ExtensionEnumerator : System.Collections.IEnumerator
    {
        internal X509ExtensionEnumerator() { }
        public System.Security.Cryptography.X509Certificates.X509Extension Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    public enum X509FindType
    {
        FindByThumbprint = 0,
        FindBySubjectName = 1,
        FindBySubjectDistinguishedName = 2,
        FindByIssuerName = 3,
        FindByIssuerDistinguishedName = 4,
        FindBySerialNumber = 5,
        FindByTimeValid = 6,
        FindByTimeNotYetValid = 7,
        FindByTimeExpired = 8,
        FindByTemplateName = 9,
        FindByApplicationPolicy = 10,
        FindByCertificatePolicy = 11,
        FindByExtension = 12,
        FindByKeyUsage = 13,
        FindBySubjectKeyIdentifier = 14,
    }
    public enum X509IncludeOption
    {
        None = 0,
        ExcludeRoot = 1,
        EndCertOnly = 2,
        WholeChain = 3,
    }
    [System.FlagsAttribute]
    public enum X509KeyStorageFlags
    {
        DefaultKeySet = 0,
        UserKeySet = 1,
        MachineKeySet = 2,
        Exportable = 4,
        UserProtected = 8,
        PersistKeySet = 16,
        EphemeralKeySet = 32,
    }
    public sealed partial class X509KeyUsageExtension : System.Security.Cryptography.X509Certificates.X509Extension
    {
        public X509KeyUsageExtension() { }
        public X509KeyUsageExtension(System.Security.Cryptography.AsnEncodedData encodedKeyUsage, bool critical) { }
        public X509KeyUsageExtension(System.Security.Cryptography.X509Certificates.X509KeyUsageFlags keyUsages, bool critical) { }
        public System.Security.Cryptography.X509Certificates.X509KeyUsageFlags KeyUsages { get { throw null; } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    [System.FlagsAttribute]
    public enum X509KeyUsageFlags
    {
        None = 0,
        EncipherOnly = 1,
        CrlSign = 2,
        KeyCertSign = 4,
        KeyAgreement = 8,
        DataEncipherment = 16,
        KeyEncipherment = 32,
        NonRepudiation = 64,
        DigitalSignature = 128,
        DecipherOnly = 32768,
    }
    public enum X509NameType
    {
        SimpleName = 0,
        EmailName = 1,
        UpnName = 2,
        DnsName = 3,
        DnsFromAlternativeName = 4,
        UrlName = 5,
    }
    public enum X509RevocationFlag
    {
        EndCertificateOnly = 0,
        EntireChain = 1,
        ExcludeRoot = 2,
    }
    public enum X509RevocationMode
    {
        NoCheck = 0,
        Online = 1,
        Offline = 2,
    }
    public abstract partial class X509SignatureGenerator
    {
        protected X509SignatureGenerator() { }
        public System.Security.Cryptography.X509Certificates.PublicKey PublicKey { get { throw null; } }
        protected abstract System.Security.Cryptography.X509Certificates.PublicKey BuildPublicKey();
        public static System.Security.Cryptography.X509Certificates.X509SignatureGenerator CreateForECDsa(System.Security.Cryptography.ECDsa key) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509SignatureGenerator CreateForRSA(System.Security.Cryptography.RSA key, System.Security.Cryptography.RSASignaturePadding signaturePadding) { throw null; }
        public abstract byte[] GetSignatureAlgorithmIdentifier(System.Security.Cryptography.HashAlgorithmName hashAlgorithm);
        public abstract byte[] SignData(byte[] data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm);
    }
    public sealed partial class X509Store : System.IDisposable
    {
        public X509Store() { }
        public X509Store(System.IntPtr storeHandle) { }
        public X509Store(System.Security.Cryptography.X509Certificates.StoreLocation storeLocation) { }
        public X509Store(System.Security.Cryptography.X509Certificates.StoreName storeName) { }
        public X509Store(System.Security.Cryptography.X509Certificates.StoreName storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation) { }
        public X509Store(System.Security.Cryptography.X509Certificates.StoreName storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation, System.Security.Cryptography.X509Certificates.OpenFlags flags) { }
        public X509Store(string storeName) { }
        public X509Store(string storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation) { }
        public X509Store(string storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation, System.Security.Cryptography.X509Certificates.OpenFlags flags) { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Certificates { get { throw null; } }
        public bool IsOpen { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.StoreLocation Location { get { throw null; } }
        public string? Name { get { throw null; } }
        public System.IntPtr StoreHandle { get { throw null; } }
        public void Add(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { }
        public void Close() { }
        public void Dispose() { }
        public void Open(System.Security.Cryptography.X509Certificates.OpenFlags flags) { }
        public void Remove(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public void RemoveRange(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { }
    }
    public sealed partial class X509SubjectKeyIdentifierExtension : System.Security.Cryptography.X509Certificates.X509Extension
    {
        public X509SubjectKeyIdentifierExtension() { }
        public X509SubjectKeyIdentifierExtension(byte[] subjectKeyIdentifier, bool critical) { }
        public X509SubjectKeyIdentifierExtension(System.ReadOnlySpan<byte> subjectKeyIdentifier, bool critical) { }
        public X509SubjectKeyIdentifierExtension(System.Security.Cryptography.AsnEncodedData encodedSubjectKeyIdentifier, bool critical) { }
        public X509SubjectKeyIdentifierExtension(System.Security.Cryptography.X509Certificates.PublicKey key, bool critical) { }
        public X509SubjectKeyIdentifierExtension(System.Security.Cryptography.X509Certificates.PublicKey key, System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierHashAlgorithm algorithm, bool critical) { }
        public X509SubjectKeyIdentifierExtension(string subjectKeyIdentifier, bool critical) { }
        public string? SubjectKeyIdentifier { get { throw null; } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public enum X509SubjectKeyIdentifierHashAlgorithm
    {
        Sha1 = 0,
        ShortSha1 = 1,
        CapiSha1 = 2,
    }
    [System.FlagsAttribute]
    public enum X509VerificationFlags
    {
        NoFlag = 0,
        IgnoreNotTimeValid = 1,
        IgnoreCtlNotTimeValid = 2,
        IgnoreNotTimeNested = 4,
        IgnoreInvalidBasicConstraints = 8,
        AllowUnknownCertificateAuthority = 16,
        IgnoreWrongUsage = 32,
        IgnoreInvalidName = 64,
        IgnoreInvalidPolicy = 128,
        IgnoreEndRevocationUnknown = 256,
        IgnoreCtlSignerRevocationUnknown = 512,
        IgnoreCertificateAuthorityRevocationUnknown = 1024,
        IgnoreRootRevocationUnknown = 2048,
        AllFlags = 4095,
    }
}
