// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeX509ChainHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeX509ChainHandle() : base (default(bool)) { }
        protected override void Dispose(bool disposing) { }
        protected override bool ReleaseHandle() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
}
namespace System.Security.Cryptography.X509Certificates
{
    public sealed partial class CertificateRequest
    {
        public CertificateRequest(System.Security.Cryptography.X509Certificates.X500DistinguishedName subjectName, System.Security.Cryptography.ECDsa key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public CertificateRequest(System.Security.Cryptography.X509Certificates.X500DistinguishedName subjectName, System.Security.Cryptography.RSA key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public CertificateRequest(System.Security.Cryptography.X509Certificates.X500DistinguishedName subjectName, System.Security.Cryptography.X509Certificates.PublicKey publicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public CertificateRequest(string subjectName, System.Security.Cryptography.ECDsa key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public CertificateRequest(string subjectName, System.Security.Cryptography.RSA key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Collections.ObjectModel.Collection<System.Security.Cryptography.X509Certificates.X509Extension> CertificateExtensions { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.HashAlgorithmName HashAlgorithm { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.PublicKey PublicKey { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName SubjectName { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Create(System.Security.Cryptography.X509Certificates.X500DistinguishedName issuerName, System.Security.Cryptography.X509Certificates.X509SignatureGenerator generator, System.DateTimeOffset notBefore, System.DateTimeOffset notAfter, byte[] serialNumber) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Create(System.Security.Cryptography.X509Certificates.X509Certificate2 issuerCertificate, System.DateTimeOffset notBefore, System.DateTimeOffset notAfter, byte[] serialNumber) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 CreateSelfSigned(System.DateTimeOffset notBefore, System.DateTimeOffset notAfter) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[] CreateSigningRequest() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[] CreateSigningRequest(System.Security.Cryptography.X509Certificates.X509SignatureGenerator signatureGenerator) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public static partial class DSACertificateExtensions
    {
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CopyWithPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.Security.Cryptography.DSA privateKey) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.DSA? GetDSAPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.DSA? GetDSAPublicKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public static partial class ECDsaCertificateExtensions
    {
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CopyWithPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.Security.Cryptography.ECDsa privateKey) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.ECDsa? GetECDsaPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.ECDsa? GetECDsaPublicKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
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
        public PublicKey(System.Security.Cryptography.Oid oid, System.Security.Cryptography.AsnEncodedData parameters, System.Security.Cryptography.AsnEncodedData keyValue) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.AsnEncodedData EncodedKeyValue { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.AsnEncodedData EncodedParameters { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.AsymmetricAlgorithm Key { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.Oid Oid { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
    }
    public static partial class RSACertificateExtensions
    {
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CopyWithPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.Security.Cryptography.RSA privateKey) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.RSA? GetRSAPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.RSA? GetRSAPublicKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
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
        public SubjectAlternativeNameBuilder() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void AddDnsName(string dnsName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void AddEmailAddress(string emailAddress) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void AddIpAddress(System.Net.IPAddress ipAddress) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void AddUri(System.Uri uri) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void AddUserPrincipalName(string upn) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.X509Certificates.X509Extension Build(bool critical = false) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public sealed partial class X500DistinguishedName : System.Security.Cryptography.AsnEncodedData
    {
        public X500DistinguishedName(byte[] encodedDistinguishedName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X500DistinguishedName(System.Security.Cryptography.AsnEncodedData encodedDistinguishedName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X500DistinguishedName(System.Security.Cryptography.X509Certificates.X500DistinguishedName distinguishedName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X500DistinguishedName(string distinguishedName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X500DistinguishedName(string distinguishedName, System.Security.Cryptography.X509Certificates.X500DistinguishedNameFlags flag) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public string Name { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public string Decode(System.Security.Cryptography.X509Certificates.X500DistinguishedNameFlags flag) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override string Format(bool multiLine) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
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
        public X509BasicConstraintsExtension() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509BasicConstraintsExtension(bool certificateAuthority, bool hasPathLengthConstraint, int pathLengthConstraint, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509BasicConstraintsExtension(System.Security.Cryptography.AsnEncodedData encodedBasicConstraints, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public bool CertificateAuthority { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool HasPathLengthConstraint { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public int PathLengthConstraint { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public partial class X509Certificate : System.IDisposable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        public X509Certificate() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate(byte[] data) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate(byte[] rawData, System.Security.SecureString? password) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate(byte[] rawData, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate(byte[] rawData, string? password) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate(System.IntPtr handle) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate(System.Security.Cryptography.X509Certificates.X509Certificate cert) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate(string fileName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate(string fileName, System.Security.SecureString? password) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate(string fileName, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate(string fileName, string? password) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.IntPtr Handle { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public string Issuer { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public string Subject { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public static System.Security.Cryptography.X509Certificates.X509Certificate CreateFromCertFile(string filename) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.X509Certificates.X509Certificate CreateFromSignedFile(string filename) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public override bool Equals(object? obj) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual bool Equals(System.Security.Cryptography.X509Certificates.X509Certificate? other) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual byte[] Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public virtual byte[] Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType, System.Security.SecureString? password) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual byte[] Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType, string? password) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected static string FormatDate(System.DateTime date) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual byte[] GetCertHash() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual byte[] GetCertHash(System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual string GetCertHashString() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual string GetCertHashString(System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual string GetEffectiveDateString() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual string GetExpirationDateString() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual string GetFormat() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override int GetHashCode() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.ObsoleteAttribute("This method has been deprecated.  Please use the Issuer property instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual string GetIssuerName() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual string GetKeyAlgorithm() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual byte[] GetKeyAlgorithmParameters() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual string GetKeyAlgorithmParametersString() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.ObsoleteAttribute("This method has been deprecated.  Please use the Subject property instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual string GetName() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual byte[] GetPublicKey() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual string GetPublicKeyString() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual byte[] GetRawCertData() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual string GetRawCertDataString() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual byte[] GetSerialNumber() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual string GetSerialNumberString() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual void Import(byte[] rawData) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public virtual void Import(byte[] rawData, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual void Import(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual void Import(string fileName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public virtual void Import(string fileName, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual void Import(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual void Reset() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object? sender) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override string ToString() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual string ToString(bool fVerbose) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public virtual bool TryGetCertHash(System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Span<byte> destination, out int bytesWritten) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public partial class X509Certificate2 : System.Security.Cryptography.X509Certificates.X509Certificate
    {
        public X509Certificate2() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate2(byte[] rawData) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate2(byte[] rawData, System.Security.SecureString? password) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate2(byte[] rawData, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate2(byte[] rawData, string? password) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate2(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate2(System.IntPtr handle) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected X509Certificate2(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate2(System.Security.Cryptography.X509Certificates.X509Certificate certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate2(string fileName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate2(string fileName, System.Security.SecureString? password) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public X509Certificate2(string fileName, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate2(string fileName, string? password) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate2(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public bool Archived { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509ExtensionCollection Extensions { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public string FriendlyName { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool HasPrivateKey { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName IssuerName { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.DateTime NotAfter { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.DateTime NotBefore { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.AsymmetricAlgorithm? PrivateKey { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.PublicKey PublicKey { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public byte[] RawData { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public string SerialNumber { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.Oid SignatureAlgorithm { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName SubjectName { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public string Thumbprint { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public int Version { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromEncryptedPem(System.ReadOnlySpan<char> certPem, System.ReadOnlySpan<char> keyPem, System.ReadOnlySpan<char> password) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromEncryptedPemFile(string certPemFilePath, System.ReadOnlySpan<char> password, string? keyPemFilePath = null) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromPem(System.ReadOnlySpan<char> certPem, System.ReadOnlySpan<char> keyPem) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromPemFile(string certPemFilePath, string? keyPemFilePath = null) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.X509Certificates.X509ContentType GetCertContentType(byte[] rawData) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.X509Certificates.X509ContentType GetCertContentType(string fileName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public string GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType nameType, bool forIssuer) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void Import(byte[] rawData) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public override void Import(byte[] rawData, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void Import(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void Import(string fileName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        [System.CLSCompliantAttribute(false)]
        public override void Import(string fileName, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void Import(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void Reset() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override string ToString() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override string ToString(bool verbose) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public bool Verify() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public partial class X509Certificate2Collection : System.Security.Cryptography.X509Certificates.X509CertificateCollection
    {
        public X509Certificate2Collection() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate2Collection(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate2Collection(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Certificate2Collection(System.Security.Cryptography.X509Certificates.X509Certificate2[] certificates) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public new System.Security.Cryptography.X509Certificates.X509Certificate2 this[int index] { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public int Add(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509Certificate2[] certificates) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public bool Contains(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[]? Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[]? Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType, string? password) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Find(System.Security.Cryptography.X509Certificates.X509FindType findType, object findValue, bool validOnly) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public new System.Security.Cryptography.X509Certificates.X509Certificate2Enumerator GetEnumerator() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Import(byte[] rawData) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Import(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Import(string fileName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Import(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void ImportFromPem(System.ReadOnlySpan<char> certPem) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void ImportFromPemFile(string certPemFilePath) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Insert(int index, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Remove(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void RemoveRange(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void RemoveRange(System.Security.Cryptography.X509Certificates.X509Certificate2[] certificates) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public sealed partial class X509Certificate2Enumerator : System.Collections.IEnumerator
    {
        internal X509Certificate2Enumerator() { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Current { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        object System.Collections.IEnumerator.Current { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool MoveNext() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Reset() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        bool System.Collections.IEnumerator.MoveNext() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        void System.Collections.IEnumerator.Reset() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public partial class X509CertificateCollection : System.Collections.CollectionBase
    {
        public X509CertificateCollection() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509CertificateCollection(System.Security.Cryptography.X509Certificates.X509CertificateCollection value) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509CertificateCollection(System.Security.Cryptography.X509Certificates.X509Certificate[] value) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.X509Certificates.X509Certificate this[int index] { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public int Add(System.Security.Cryptography.X509Certificates.X509Certificate value) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509CertificateCollection value) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509Certificate[] value) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public bool Contains(System.Security.Cryptography.X509Certificates.X509Certificate value) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void CopyTo(System.Security.Cryptography.X509Certificates.X509Certificate[] array, int index) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public new System.Security.Cryptography.X509Certificates.X509CertificateCollection.X509CertificateEnumerator GetEnumerator() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override int GetHashCode() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public int IndexOf(System.Security.Cryptography.X509Certificates.X509Certificate value) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Insert(int index, System.Security.Cryptography.X509Certificates.X509Certificate value) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void OnValidate(object value) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Remove(System.Security.Cryptography.X509Certificates.X509Certificate value) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public partial class X509CertificateEnumerator : System.Collections.IEnumerator
        {
            public X509CertificateEnumerator(System.Security.Cryptography.X509Certificates.X509CertificateCollection mappings) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
            public System.Security.Cryptography.X509Certificates.X509Certificate Current { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
            object System.Collections.IEnumerator.Current { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
            public bool MoveNext() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
            public void Reset() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
            bool System.Collections.IEnumerator.MoveNext() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
            void System.Collections.IEnumerator.Reset() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        }
    }
    public partial class X509Chain : System.IDisposable
    {
        public X509Chain() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Chain(bool useMachineContext) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Chain(System.IntPtr chainContext) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.IntPtr ChainContext { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509ChainElementCollection ChainElements { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509ChainPolicy ChainPolicy { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509ChainStatus[] ChainStatus { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public Microsoft.Win32.SafeHandles.SafeX509ChainHandle? SafeHandle { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool Build(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.X509Certificates.X509Chain Create() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void Reset() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public partial class X509ChainElement
    {
        internal X509ChainElement() { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509ChainStatus[] ChainElementStatus { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public string Information { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
    }
    public sealed partial class X509ChainElementCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal X509ChainElementCollection() { }
        public int Count { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool IsSynchronized { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509ChainElement this[int index] { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public object SyncRoot { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public void CopyTo(System.Security.Cryptography.X509Certificates.X509ChainElement[] array, int index) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.X509Certificates.X509ChainElementEnumerator GetEnumerator() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public sealed partial class X509ChainElementEnumerator : System.Collections.IEnumerator
    {
        internal X509ChainElementEnumerator() { }
        public System.Security.Cryptography.X509Certificates.X509ChainElement Current { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        object System.Collections.IEnumerator.Current { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool MoveNext() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Reset() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public sealed partial class X509ChainPolicy
    {
        public X509ChainPolicy() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.OidCollection ApplicationPolicy { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.OidCollection CertificatePolicy { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection CustomTrustStore { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool DisableCertificateDownloads { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection ExtraStore { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509RevocationFlag RevocationFlag { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509RevocationMode RevocationMode { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509ChainTrustMode TrustMode { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.TimeSpan UrlRetrievalTimeout { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509VerificationFlags VerificationFlags { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.DateTime VerificationTime { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public void Reset() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct X509ChainStatus
    {
        private object _dummy;
        private int _dummyPrimitive;
        public System.Security.Cryptography.X509Certificates.X509ChainStatusFlags Status { readonly get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string StatusInformation { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
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
        public X509EnhancedKeyUsageExtension() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509EnhancedKeyUsageExtension(System.Security.Cryptography.AsnEncodedData encodedEnhancedKeyUsages, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509EnhancedKeyUsageExtension(System.Security.Cryptography.OidCollection enhancedKeyUsages, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.OidCollection EnhancedKeyUsages { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public partial class X509Extension : System.Security.Cryptography.AsnEncodedData
    {
        protected X509Extension() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Extension(System.Security.Cryptography.AsnEncodedData encodedExtension, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Extension(System.Security.Cryptography.Oid oid, byte[] rawData, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Extension(string oid, byte[] rawData, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public bool Critical { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public sealed partial class X509ExtensionCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public X509ExtensionCollection() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public int Count { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool IsSynchronized { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509Extension this[int index] { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.X509Extension? this[string oid] { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public object SyncRoot { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public int Add(System.Security.Cryptography.X509Certificates.X509Extension extension) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void CopyTo(System.Security.Cryptography.X509Certificates.X509Extension[] array, int index) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.X509Certificates.X509ExtensionEnumerator GetEnumerator() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public sealed partial class X509ExtensionEnumerator : System.Collections.IEnumerator
    {
        internal X509ExtensionEnumerator() { }
        public System.Security.Cryptography.X509Certificates.X509Extension Current { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        object System.Collections.IEnumerator.Current { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool MoveNext() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Reset() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
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
        public X509KeyUsageExtension() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509KeyUsageExtension(System.Security.Cryptography.AsnEncodedData encodedKeyUsage, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509KeyUsageExtension(System.Security.Cryptography.X509Certificates.X509KeyUsageFlags keyUsages, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.X509Certificates.X509KeyUsageFlags KeyUsages { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
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
        protected X509SignatureGenerator() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.X509Certificates.PublicKey PublicKey { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        protected abstract System.Security.Cryptography.X509Certificates.PublicKey BuildPublicKey();
        public static System.Security.Cryptography.X509Certificates.X509SignatureGenerator CreateForECDsa(System.Security.Cryptography.ECDsa key) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public static System.Security.Cryptography.X509Certificates.X509SignatureGenerator CreateForRSA(System.Security.Cryptography.RSA key, System.Security.Cryptography.RSASignaturePadding signaturePadding) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public abstract byte[] GetSignatureAlgorithmIdentifier(System.Security.Cryptography.HashAlgorithmName hashAlgorithm);
        public abstract byte[] SignData(byte[] data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm);
    }
    public sealed partial class X509Store : System.IDisposable
    {
        public X509Store() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Store(System.IntPtr storeHandle) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Store(System.Security.Cryptography.X509Certificates.StoreLocation storeLocation) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Store(System.Security.Cryptography.X509Certificates.StoreName storeName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Store(System.Security.Cryptography.X509Certificates.StoreName storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Store(System.Security.Cryptography.X509Certificates.StoreName storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation, System.Security.Cryptography.X509Certificates.OpenFlags flags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Store(string storeName) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Store(string storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509Store(string storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation, System.Security.Cryptography.X509Certificates.OpenFlags flags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Certificates { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool IsOpen { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.X509Certificates.StoreLocation Location { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public string? Name { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.IntPtr StoreHandle { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public void Add(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Close() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Dispose() { }
        public void Open(System.Security.Cryptography.X509Certificates.OpenFlags flags) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void Remove(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void RemoveRange(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public sealed partial class X509SubjectKeyIdentifierExtension : System.Security.Cryptography.X509Certificates.X509Extension
    {
        public X509SubjectKeyIdentifierExtension() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509SubjectKeyIdentifierExtension(byte[] subjectKeyIdentifier, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509SubjectKeyIdentifierExtension(System.Security.Cryptography.AsnEncodedData encodedSubjectKeyIdentifier, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509SubjectKeyIdentifierExtension(System.Security.Cryptography.X509Certificates.PublicKey key, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509SubjectKeyIdentifierExtension(System.Security.Cryptography.X509Certificates.PublicKey key, System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierHashAlgorithm algorithm, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public X509SubjectKeyIdentifierExtension(string subjectKeyIdentifier, bool critical) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public string? SubjectKeyIdentifier { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
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
