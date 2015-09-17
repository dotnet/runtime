// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeX509ChainHandle : System.Runtime.InteropServices.SafeHandle
    {
        internal SafeX509ChainHandle() : base(default(System.IntPtr), default(bool)) { }
        protected override bool ReleaseHandle() { return default(bool); }
    }
}
namespace System.Security.Cryptography.X509Certificates
{
    public static partial class ECDsaCertificateExtensions
    {
        public static System.Security.Cryptography.ECDsa GetECDsaPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { return default(System.Security.Cryptography.ECDsa); }
        public static System.Security.Cryptography.ECDsa GetECDsaPublicKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { return default(System.Security.Cryptography.ECDsa); }
    }
    [System.FlagsAttribute]
    public enum OpenFlags
    {
        IncludeArchived = 8,
        MaxAllowed = 2,
        OpenExistingOnly = 4,
        ReadOnly = 0,
        ReadWrite = 1,
    }
    public sealed partial class PublicKey
    {
        public PublicKey(System.Security.Cryptography.Oid oid, System.Security.Cryptography.AsnEncodedData parameters, System.Security.Cryptography.AsnEncodedData keyValue) { }
        public System.Security.Cryptography.AsnEncodedData EncodedKeyValue { get { return default(System.Security.Cryptography.AsnEncodedData); } }
        public System.Security.Cryptography.AsnEncodedData EncodedParameters { get { return default(System.Security.Cryptography.AsnEncodedData); } }
        public System.Security.Cryptography.Oid Oid { get { return default(System.Security.Cryptography.Oid); } }
    }
    public static partial class RSACertificateExtensions
    {
        public static System.Security.Cryptography.RSA GetRSAPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { return default(System.Security.Cryptography.RSA); }
        public static System.Security.Cryptography.RSA GetRSAPublicKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { return default(System.Security.Cryptography.RSA); }
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
    public sealed partial class X500DistinguishedName : System.Security.Cryptography.AsnEncodedData
    {
        public X500DistinguishedName(byte[] encodedDistinguishedName) { }
        public X500DistinguishedName(System.Security.Cryptography.AsnEncodedData encodedDistinguishedName) { }
        public X500DistinguishedName(System.Security.Cryptography.X509Certificates.X500DistinguishedName distinguishedName) { }
        public X500DistinguishedName(string distinguishedName) { }
        public X500DistinguishedName(string distinguishedName, System.Security.Cryptography.X509Certificates.X500DistinguishedNameFlags flag) { }
        public string Name { get { return default(string); } }
        public string Decode(System.Security.Cryptography.X509Certificates.X500DistinguishedNameFlags flag) { return default(string); }
        public override string Format(bool multiLine) { return default(string); }
    }
    [System.FlagsAttribute]
    public enum X500DistinguishedNameFlags
    {
        DoNotUsePlusSign = 32,
        DoNotUseQuotes = 64,
        ForceUTF8Encoding = 16384,
        None = 0,
        Reversed = 1,
        UseCommas = 128,
        UseNewLines = 256,
        UseSemicolons = 16,
        UseT61Encoding = 8192,
        UseUTF8Encoding = 4096,
    }
    public sealed partial class X509BasicConstraintsExtension : System.Security.Cryptography.X509Certificates.X509Extension
    {
        public X509BasicConstraintsExtension() { }
        public X509BasicConstraintsExtension(bool certificateAuthority, bool hasPathLengthConstraint, int pathLengthConstraint, bool critical) { }
        public X509BasicConstraintsExtension(System.Security.Cryptography.AsnEncodedData encodedBasicConstraints, bool critical) { }
        public bool CertificateAuthority { get { return default(bool); } }
        public bool HasPathLengthConstraint { get { return default(bool); } }
        public int PathLengthConstraint { get { return default(int); } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public partial class X509Certificate : System.IDisposable
    {
        public X509Certificate() { }
        public X509Certificate(byte[] data) { }
        public X509Certificate(byte[] rawData, string password) { }
        public X509Certificate(byte[] rawData, string password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.Security.SecurityCriticalAttribute]
        public X509Certificate(System.IntPtr handle) { }
        public X509Certificate(string fileName) { }
        public X509Certificate(string fileName, string password) { }
        public X509Certificate(string fileName, string password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public System.IntPtr Handle {[System.Security.SecurityCriticalAttribute]get { return default(System.IntPtr); } }
        public string Issuer { get { return default(string); } }
        public string Subject { get { return default(string); } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public override bool Equals(object obj) { return default(bool); }
        public virtual bool Equals(System.Security.Cryptography.X509Certificates.X509Certificate other) { return default(bool); }
        public virtual byte[] Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType) { return default(byte[]); }
        public virtual byte[] Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType, string password) { return default(byte[]); }
        public virtual byte[] GetCertHash() { return default(byte[]); }
        public virtual string GetFormat() { return default(string); }
        public override int GetHashCode() { return default(int); }
        public virtual string GetKeyAlgorithm() { return default(string); }
        public virtual byte[] GetKeyAlgorithmParameters() { return default(byte[]); }
        public virtual string GetKeyAlgorithmParametersString() { return default(string); }
        public virtual byte[] GetPublicKey() { return default(byte[]); }
        public virtual byte[] GetSerialNumber() { return default(byte[]); }
        public override string ToString() { return default(string); }
        public virtual string ToString(bool fVerbose) { return default(string); }
    }
    public partial class X509Certificate2 : System.Security.Cryptography.X509Certificates.X509Certificate
    {
        public X509Certificate2() { }
        public X509Certificate2(byte[] rawData) { }
        public X509Certificate2(byte[] rawData, string password) { }
        public X509Certificate2(byte[] rawData, string password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public X509Certificate2(System.IntPtr handle) { }
        public X509Certificate2(string fileName) { }
        public X509Certificate2(string fileName, string password) { }
        public X509Certificate2(string fileName, string password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public bool Archived { get { return default(bool); } set { } }
        public System.Security.Cryptography.X509Certificates.X509ExtensionCollection Extensions { get { return default(System.Security.Cryptography.X509Certificates.X509ExtensionCollection); } }
        public string FriendlyName { get { return default(string); } set { } }
        public bool HasPrivateKey { get { return default(bool); } }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName IssuerName { get { return default(System.Security.Cryptography.X509Certificates.X500DistinguishedName); } }
        public System.DateTime NotAfter { get { return default(System.DateTime); } }
        public System.DateTime NotBefore { get { return default(System.DateTime); } }
        public System.Security.Cryptography.X509Certificates.PublicKey PublicKey { get { return default(System.Security.Cryptography.X509Certificates.PublicKey); } }
        public byte[] RawData { get { return default(byte[]); } }
        public string SerialNumber { get { return default(string); } }
        public System.Security.Cryptography.Oid SignatureAlgorithm { get { return default(System.Security.Cryptography.Oid); } }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName SubjectName { get { return default(System.Security.Cryptography.X509Certificates.X500DistinguishedName); } }
        public string Thumbprint { get { return default(string); } }
        public int Version { get { return default(int); } }
        public static System.Security.Cryptography.X509Certificates.X509ContentType GetCertContentType(byte[] rawData) { return default(System.Security.Cryptography.X509Certificates.X509ContentType); }
        public static System.Security.Cryptography.X509Certificates.X509ContentType GetCertContentType(string fileName) { return default(System.Security.Cryptography.X509Certificates.X509ContentType); }
        public string GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType nameType, bool forIssuer) { return default(string); }
        public override string ToString() { return default(string); }
        public override string ToString(bool verbose) { return default(string); }
    }
    public partial class X509Certificate2Collection : System.Security.Cryptography.X509Certificates.X509CertificateCollection
    {
        public X509Certificate2Collection() { }
        public X509Certificate2Collection(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public X509Certificate2Collection(System.Security.Cryptography.X509Certificates.X509Certificate2[] certificates) { }
        public X509Certificate2Collection(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { }
        public new System.Security.Cryptography.X509Certificates.X509Certificate2 this[int index] { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate2); } set { } }
        public int Add(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { return default(int); }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509Certificate2[] certificates) { }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { }
        public bool Contains(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { return default(bool); }
        public byte[] Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType) { return default(byte[]); }
        public byte[] Export(System.Security.Cryptography.X509Certificates.X509ContentType contentType, string password) { return default(byte[]); }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Find(System.Security.Cryptography.X509Certificates.X509FindType findType, object findValue, bool validOnly) { return default(System.Security.Cryptography.X509Certificates.X509Certificate2Collection); }
        public new System.Security.Cryptography.X509Certificates.X509Certificate2Enumerator GetEnumerator() { return default(System.Security.Cryptography.X509Certificates.X509Certificate2Enumerator); }
        public void Import(byte[] rawData) { }
        public void Import(byte[] rawData, string password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public void Import(string fileName) { }
        public void Import(string fileName, string password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public void Insert(int index, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public void Remove(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public void RemoveRange(System.Security.Cryptography.X509Certificates.X509Certificate2[] certificates) { }
        public void RemoveRange(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates) { }
    }
    public sealed partial class X509Certificate2Enumerator : System.Collections.IEnumerator
    {
        internal X509Certificate2Enumerator() { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Current { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate2); } }
        object System.Collections.IEnumerator.Current { get { return default(object); } }
        public bool MoveNext() { return default(bool); }
        public void Reset() { }
        bool System.Collections.IEnumerator.MoveNext() { return default(bool); }
        void System.Collections.IEnumerator.Reset() { }
    }
    public partial class X509CertificateCollection
    {
        public X509CertificateCollection() { }
        public X509CertificateCollection(System.Security.Cryptography.X509Certificates.X509Certificate[] value) { }
        public X509CertificateCollection(System.Security.Cryptography.X509Certificates.X509CertificateCollection value) { }
        public System.Security.Cryptography.X509Certificates.X509Certificate this[int index] { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate); } set { } }
        public int Add(System.Security.Cryptography.X509Certificates.X509Certificate value) { return default(int); }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509Certificate[] value) { }
        public void AddRange(System.Security.Cryptography.X509Certificates.X509CertificateCollection value) { }
        public bool Contains(System.Security.Cryptography.X509Certificates.X509Certificate value) { return default(bool); }
        public void CopyTo(System.Security.Cryptography.X509Certificates.X509Certificate[] array, int index) { }
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection.X509CertificateEnumerator GetEnumerator() { return default(System.Security.Cryptography.X509Certificates.X509CertificateCollection.X509CertificateEnumerator); }
        public override int GetHashCode() { return default(int); }
        public int IndexOf(System.Security.Cryptography.X509Certificates.X509Certificate value) { return default(int); }
        public void Insert(int index, System.Security.Cryptography.X509Certificates.X509Certificate value) { }
        public void Remove(System.Security.Cryptography.X509Certificates.X509Certificate value) { }
        public partial class X509CertificateEnumerator : System.Collections.IEnumerator
        {
            public X509CertificateEnumerator(System.Security.Cryptography.X509Certificates.X509CertificateCollection mappings) { }
            public System.Security.Cryptography.X509Certificates.X509Certificate Current { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate); } }
            object System.Collections.IEnumerator.Current { get { return default(object); } }
            public bool MoveNext() { return default(bool); }
            public void Reset() { }
            bool System.Collections.IEnumerator.MoveNext() { return default(bool); }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public partial class X509Chain : System.IDisposable
    {
        public X509Chain() { }
        public System.Security.Cryptography.X509Certificates.X509ChainElementCollection ChainElements { get { return default(System.Security.Cryptography.X509Certificates.X509ChainElementCollection); } }
        public System.Security.Cryptography.X509Certificates.X509ChainPolicy ChainPolicy { get { return default(System.Security.Cryptography.X509Certificates.X509ChainPolicy); } set { } }
        public System.Security.Cryptography.X509Certificates.X509ChainStatus[] ChainStatus { get { return default(System.Security.Cryptography.X509Certificates.X509ChainStatus[]); } }
        public Microsoft.Win32.SafeHandles.SafeX509ChainHandle SafeHandle { get { return default(Microsoft.Win32.SafeHandles.SafeX509ChainHandle); } }
        public bool Build(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { return default(bool); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    public partial class X509ChainElement
    {
        internal X509ChainElement() { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate2); } }
        public System.Security.Cryptography.X509Certificates.X509ChainStatus[] ChainElementStatus { get { return default(System.Security.Cryptography.X509Certificates.X509ChainStatus[]); } }
        public string Information { get { return default(string); } }
    }
    public sealed partial class X509ChainElementCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal X509ChainElementCollection() { }
        public int Count { get { return default(int); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        public System.Security.Cryptography.X509Certificates.X509ChainElement this[int index] { get { return default(System.Security.Cryptography.X509Certificates.X509ChainElement); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public void CopyTo(System.Security.Cryptography.X509Certificates.X509ChainElement[] array, int index) { }
        public System.Security.Cryptography.X509Certificates.X509ChainElementEnumerator GetEnumerator() { return default(System.Security.Cryptography.X509Certificates.X509ChainElementEnumerator); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public sealed partial class X509ChainElementEnumerator : System.Collections.IEnumerator
    {
        internal X509ChainElementEnumerator() { }
        public System.Security.Cryptography.X509Certificates.X509ChainElement Current { get { return default(System.Security.Cryptography.X509Certificates.X509ChainElement); } }
        object System.Collections.IEnumerator.Current { get { return default(object); } }
        public bool MoveNext() { return default(bool); }
        public void Reset() { }
    }
    public sealed partial class X509ChainPolicy
    {
        public X509ChainPolicy() { }
        public System.Security.Cryptography.OidCollection ApplicationPolicy { get { return default(System.Security.Cryptography.OidCollection); } }
        public System.Security.Cryptography.OidCollection CertificatePolicy { get { return default(System.Security.Cryptography.OidCollection); } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection ExtraStore { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate2Collection); } }
        public System.Security.Cryptography.X509Certificates.X509RevocationFlag RevocationFlag { get { return default(System.Security.Cryptography.X509Certificates.X509RevocationFlag); } set { } }
        public System.Security.Cryptography.X509Certificates.X509RevocationMode RevocationMode { get { return default(System.Security.Cryptography.X509Certificates.X509RevocationMode); } set { } }
        public System.TimeSpan UrlRetrievalTimeout { get { return default(System.TimeSpan); } set { } }
        public System.Security.Cryptography.X509Certificates.X509VerificationFlags VerificationFlags { get { return default(System.Security.Cryptography.X509Certificates.X509VerificationFlags); } set { } }
        public System.DateTime VerificationTime { get { return default(System.DateTime); } set { } }
        public void Reset() { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct X509ChainStatus
    {
        public System.Security.Cryptography.X509Certificates.X509ChainStatusFlags Status { get { return default(System.Security.Cryptography.X509Certificates.X509ChainStatusFlags); } set { } }
        public string StatusInformation { get { return default(string); } set { } }
    }
    [System.FlagsAttribute]
    public enum X509ChainStatusFlags
    {
        CtlNotSignatureValid = 262144,
        CtlNotTimeValid = 131072,
        CtlNotValidForUsage = 524288,
        Cyclic = 128,
        HasExcludedNameConstraint = 32768,
        HasNotDefinedNameConstraint = 8192,
        HasNotPermittedNameConstraint = 16384,
        HasNotSupportedNameConstraint = 4096,
        InvalidBasicConstraints = 1024,
        InvalidExtension = 256,
        InvalidNameConstraints = 2048,
        InvalidPolicyConstraints = 512,
        NoError = 0,
        NoIssuanceChainPolicy = 33554432,
        NotSignatureValid = 8,
        NotTimeNested = 2,
        NotTimeValid = 1,
        NotValidForUsage = 16,
        OfflineRevocation = 16777216,
        PartialChain = 65536,
        RevocationStatusUnknown = 64,
        Revoked = 4,
        UntrustedRoot = 32,
    }
    public enum X509ContentType
    {
        Authenticode = 6,
        Cert = 1,
        Pfx = 3,
        Pkcs12 = 3,
        Pkcs7 = 5,
        SerializedCert = 2,
        SerializedStore = 4,
        Unknown = 0,
    }
    public sealed partial class X509EnhancedKeyUsageExtension : System.Security.Cryptography.X509Certificates.X509Extension
    {
        public X509EnhancedKeyUsageExtension() { }
        public X509EnhancedKeyUsageExtension(System.Security.Cryptography.AsnEncodedData encodedEnhancedKeyUsages, bool critical) { }
        public X509EnhancedKeyUsageExtension(System.Security.Cryptography.OidCollection enhancedKeyUsages, bool critical) { }
        public System.Security.Cryptography.OidCollection EnhancedKeyUsages { get { return default(System.Security.Cryptography.OidCollection); } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public partial class X509Extension : System.Security.Cryptography.AsnEncodedData
    {
        protected X509Extension() { }
        public X509Extension(System.Security.Cryptography.AsnEncodedData encodedExtension, bool critical) { }
        public X509Extension(System.Security.Cryptography.Oid oid, byte[] rawData, bool critical) { }
        public X509Extension(string oid, byte[] rawData, bool critical) { }
        public bool Critical { get { return default(bool); } set { } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public sealed partial class X509ExtensionCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public X509ExtensionCollection() { }
        public int Count { get { return default(int); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        public System.Security.Cryptography.X509Certificates.X509Extension this[int index] { get { return default(System.Security.Cryptography.X509Certificates.X509Extension); } }
        public System.Security.Cryptography.X509Certificates.X509Extension this[string oid] { get { return default(System.Security.Cryptography.X509Certificates.X509Extension); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public int Add(System.Security.Cryptography.X509Certificates.X509Extension extension) { return default(int); }
        public void CopyTo(System.Security.Cryptography.X509Certificates.X509Extension[] array, int index) { }
        public System.Security.Cryptography.X509Certificates.X509ExtensionEnumerator GetEnumerator() { return default(System.Security.Cryptography.X509Certificates.X509ExtensionEnumerator); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public sealed partial class X509ExtensionEnumerator : System.Collections.IEnumerator
    {
        internal X509ExtensionEnumerator() { }
        public System.Security.Cryptography.X509Certificates.X509Extension Current { get { return default(System.Security.Cryptography.X509Certificates.X509Extension); } }
        object System.Collections.IEnumerator.Current { get { return default(object); } }
        public bool MoveNext() { return default(bool); }
        public void Reset() { }
    }
    public enum X509FindType
    {
        FindByApplicationPolicy = 10,
        FindByCertificatePolicy = 11,
        FindByExtension = 12,
        FindByIssuerDistinguishedName = 4,
        FindByIssuerName = 3,
        FindByKeyUsage = 13,
        FindBySerialNumber = 5,
        FindBySubjectDistinguishedName = 2,
        FindBySubjectKeyIdentifier = 14,
        FindBySubjectName = 1,
        FindByTemplateName = 9,
        FindByThumbprint = 0,
        FindByTimeExpired = 8,
        FindByTimeNotYetValid = 7,
        FindByTimeValid = 6,
    }
    [System.FlagsAttribute]
    public enum X509KeyStorageFlags
    {
        DefaultKeySet = 0,
        Exportable = 4,
        MachineKeySet = 2,
        PersistKeySet = 16,
        UserKeySet = 1,
        UserProtected = 8,
    }
    public sealed partial class X509KeyUsageExtension : System.Security.Cryptography.X509Certificates.X509Extension
    {
        public X509KeyUsageExtension() { }
        public X509KeyUsageExtension(System.Security.Cryptography.AsnEncodedData encodedKeyUsage, bool critical) { }
        public X509KeyUsageExtension(System.Security.Cryptography.X509Certificates.X509KeyUsageFlags keyUsages, bool critical) { }
        public System.Security.Cryptography.X509Certificates.X509KeyUsageFlags KeyUsages { get { return default(System.Security.Cryptography.X509Certificates.X509KeyUsageFlags); } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    [System.FlagsAttribute]
    public enum X509KeyUsageFlags
    {
        CrlSign = 2,
        DataEncipherment = 16,
        DecipherOnly = 32768,
        DigitalSignature = 128,
        EncipherOnly = 1,
        KeyAgreement = 8,
        KeyCertSign = 4,
        KeyEncipherment = 32,
        None = 0,
        NonRepudiation = 64,
    }
    public enum X509NameType
    {
        DnsFromAlternativeName = 4,
        DnsName = 3,
        EmailName = 1,
        SimpleName = 0,
        UpnName = 2,
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
        Offline = 2,
        Online = 1,
    }
    public sealed partial class X509Store : System.IDisposable
    {
        public X509Store() { }
        public X509Store(System.Security.Cryptography.X509Certificates.StoreName storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation) { }
        public X509Store(string storeName, System.Security.Cryptography.X509Certificates.StoreLocation storeLocation) { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Certificates { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate2Collection); } }
        public System.Security.Cryptography.X509Certificates.StoreLocation Location { get { return default(System.Security.Cryptography.X509Certificates.StoreLocation); } }
        public string Name { get { return default(string); } }
        public void Add(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public void Dispose() { }
        public void Open(System.Security.Cryptography.X509Certificates.OpenFlags flags) { }
        public void Remove(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
    }
    public sealed partial class X509SubjectKeyIdentifierExtension : System.Security.Cryptography.X509Certificates.X509Extension
    {
        public X509SubjectKeyIdentifierExtension() { }
        public X509SubjectKeyIdentifierExtension(byte[] subjectKeyIdentifier, bool critical) { }
        public X509SubjectKeyIdentifierExtension(System.Security.Cryptography.AsnEncodedData encodedSubjectKeyIdentifier, bool critical) { }
        public X509SubjectKeyIdentifierExtension(System.Security.Cryptography.X509Certificates.PublicKey key, bool critical) { }
        public X509SubjectKeyIdentifierExtension(System.Security.Cryptography.X509Certificates.PublicKey key, System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierHashAlgorithm algorithm, bool critical) { }
        public X509SubjectKeyIdentifierExtension(string subjectKeyIdentifier, bool critical) { }
        public string SubjectKeyIdentifier { get { return default(string); } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
    }
    public enum X509SubjectKeyIdentifierHashAlgorithm
    {
        CapiSha1 = 2,
        Sha1 = 0,
        ShortSha1 = 1,
    }
    [System.FlagsAttribute]
    public enum X509VerificationFlags
    {
        AllFlags = 4095,
        AllowUnknownCertificateAuthority = 16,
        IgnoreCertificateAuthorityRevocationUnknown = 1024,
        IgnoreCtlNotTimeValid = 2,
        IgnoreCtlSignerRevocationUnknown = 512,
        IgnoreEndRevocationUnknown = 256,
        IgnoreInvalidBasicConstraints = 8,
        IgnoreInvalidName = 64,
        IgnoreInvalidPolicy = 128,
        IgnoreNotTimeNested = 4,
        IgnoreNotTimeValid = 1,
        IgnoreRootRevocationUnknown = 2048,
        IgnoreWrongUsage = 32,
        NoFlag = 0,
    }
}
