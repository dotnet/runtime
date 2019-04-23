// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Win32.SafeHandles
{
    public abstract partial class SafeNCryptHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        protected SafeNCryptHandle() : base (default(bool)) { }
        protected SafeNCryptHandle(System.IntPtr handle, System.Runtime.InteropServices.SafeHandle parentHandle) : base (default(bool)) { }
        public override bool IsInvalid { get { throw null; } }
        protected override bool ReleaseHandle() { throw null; }
        protected abstract bool ReleaseNativeHandle();
    }
    public sealed partial class SafeNCryptKeyHandle : Microsoft.Win32.SafeHandles.SafeNCryptHandle
    {
        public SafeNCryptKeyHandle() { }
        public SafeNCryptKeyHandle(System.IntPtr handle, System.Runtime.InteropServices.SafeHandle parentHandle) { }
        protected override bool ReleaseNativeHandle() { throw null; }
    }
    public sealed partial class SafeNCryptProviderHandle : Microsoft.Win32.SafeHandles.SafeNCryptHandle
    {
        public SafeNCryptProviderHandle() { }
        protected override bool ReleaseNativeHandle() { throw null; }
    }
    public sealed partial class SafeNCryptSecretHandle : Microsoft.Win32.SafeHandles.SafeNCryptHandle
    {
        public SafeNCryptSecretHandle() { }
        protected override bool ReleaseNativeHandle() { throw null; }
    }
}
namespace System.Security.Cryptography
{
    public sealed partial class AesCng : System.Security.Cryptography.Aes
    {
        public AesCng() { }
        public AesCng(string keyName) { }
        public AesCng(string keyName, System.Security.Cryptography.CngProvider provider) { }
        public AesCng(string keyName, System.Security.Cryptography.CngProvider provider, System.Security.Cryptography.CngKeyOpenOptions openOptions) { }
        public override byte[] Key { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV) { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void GenerateIV() { }
        public override void GenerateKey() { }
    }
    public sealed partial class CngAlgorithm : System.IEquatable<System.Security.Cryptography.CngAlgorithm>
    {
        public CngAlgorithm(string algorithm) { }
        public string Algorithm { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm ECDiffieHellman { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm ECDiffieHellmanP256 { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm ECDiffieHellmanP384 { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm ECDiffieHellmanP521 { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm ECDsa { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm ECDsaP256 { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm ECDsaP384 { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm ECDsaP521 { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm MD5 { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm Rsa { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm Sha1 { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm Sha256 { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm Sha384 { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithm Sha512 { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Security.Cryptography.CngAlgorithm other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.CngAlgorithm left, System.Security.Cryptography.CngAlgorithm right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.CngAlgorithm left, System.Security.Cryptography.CngAlgorithm right) { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class CngAlgorithmGroup : System.IEquatable<System.Security.Cryptography.CngAlgorithmGroup>
    {
        public CngAlgorithmGroup(string algorithmGroup) { }
        public string AlgorithmGroup { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithmGroup DiffieHellman { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithmGroup Dsa { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithmGroup ECDiffieHellman { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithmGroup ECDsa { get { throw null; } }
        public static System.Security.Cryptography.CngAlgorithmGroup Rsa { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Security.Cryptography.CngAlgorithmGroup other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.CngAlgorithmGroup left, System.Security.Cryptography.CngAlgorithmGroup right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.CngAlgorithmGroup left, System.Security.Cryptography.CngAlgorithmGroup right) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum CngExportPolicies
    {
        None = 0,
        AllowExport = 1,
        AllowPlaintextExport = 2,
        AllowArchiving = 4,
        AllowPlaintextArchiving = 8,
    }
    public sealed partial class CngKey : System.IDisposable
    {
        internal CngKey() { }
        public System.Security.Cryptography.CngAlgorithm Algorithm { get { throw null; } }
        public System.Security.Cryptography.CngAlgorithmGroup AlgorithmGroup { get { throw null; } }
        public System.Security.Cryptography.CngExportPolicies ExportPolicy { get { throw null; } }
        public Microsoft.Win32.SafeHandles.SafeNCryptKeyHandle Handle { get { throw null; } }
        public bool IsEphemeral { get { throw null; } }
        public bool IsMachineKey { get { throw null; } }
        public string KeyName { get { throw null; } }
        public int KeySize { get { throw null; } }
        public System.Security.Cryptography.CngKeyUsages KeyUsage { get { throw null; } }
        public System.IntPtr ParentWindowHandle { get { throw null; } set { } }
        public System.Security.Cryptography.CngProvider Provider { get { throw null; } }
        public Microsoft.Win32.SafeHandles.SafeNCryptProviderHandle ProviderHandle { get { throw null; } }
        public System.Security.Cryptography.CngUIPolicy UIPolicy { get { throw null; } }
        public string UniqueName { get { throw null; } }
        public static System.Security.Cryptography.CngKey Create(System.Security.Cryptography.CngAlgorithm algorithm) { throw null; }
        public static System.Security.Cryptography.CngKey Create(System.Security.Cryptography.CngAlgorithm algorithm, string keyName) { throw null; }
        public static System.Security.Cryptography.CngKey Create(System.Security.Cryptography.CngAlgorithm algorithm, string keyName, System.Security.Cryptography.CngKeyCreationParameters creationParameters) { throw null; }
        public void Delete() { }
        public void Dispose() { }
        public static bool Exists(string keyName) { throw null; }
        public static bool Exists(string keyName, System.Security.Cryptography.CngProvider provider) { throw null; }
        public static bool Exists(string keyName, System.Security.Cryptography.CngProvider provider, System.Security.Cryptography.CngKeyOpenOptions options) { throw null; }
        public byte[] Export(System.Security.Cryptography.CngKeyBlobFormat format) { throw null; }
        public System.Security.Cryptography.CngProperty GetProperty(string name, System.Security.Cryptography.CngPropertyOptions options) { throw null; }
        public bool HasProperty(string name, System.Security.Cryptography.CngPropertyOptions options) { throw null; }
        public static System.Security.Cryptography.CngKey Import(byte[] keyBlob, System.Security.Cryptography.CngKeyBlobFormat format) { throw null; }
        public static System.Security.Cryptography.CngKey Import(byte[] keyBlob, System.Security.Cryptography.CngKeyBlobFormat format, System.Security.Cryptography.CngProvider provider) { throw null; }
        public static System.Security.Cryptography.CngKey Open(Microsoft.Win32.SafeHandles.SafeNCryptKeyHandle keyHandle, System.Security.Cryptography.CngKeyHandleOpenOptions keyHandleOpenOptions) { throw null; }
        public static System.Security.Cryptography.CngKey Open(string keyName) { throw null; }
        public static System.Security.Cryptography.CngKey Open(string keyName, System.Security.Cryptography.CngProvider provider) { throw null; }
        public static System.Security.Cryptography.CngKey Open(string keyName, System.Security.Cryptography.CngProvider provider, System.Security.Cryptography.CngKeyOpenOptions openOptions) { throw null; }
        public void SetProperty(System.Security.Cryptography.CngProperty property) { }
    }
    public sealed partial class CngKeyBlobFormat : System.IEquatable<System.Security.Cryptography.CngKeyBlobFormat>
    {
        public CngKeyBlobFormat(string format) { }
        public static System.Security.Cryptography.CngKeyBlobFormat EccFullPrivateBlob { get { throw null; } }
        public static System.Security.Cryptography.CngKeyBlobFormat EccFullPublicBlob { get { throw null; } }
        public static System.Security.Cryptography.CngKeyBlobFormat EccPrivateBlob { get { throw null; } }
        public static System.Security.Cryptography.CngKeyBlobFormat EccPublicBlob { get { throw null; } }
        public string Format { get { throw null; } }
        public static System.Security.Cryptography.CngKeyBlobFormat GenericPrivateBlob { get { throw null; } }
        public static System.Security.Cryptography.CngKeyBlobFormat GenericPublicBlob { get { throw null; } }
        public static System.Security.Cryptography.CngKeyBlobFormat OpaqueTransportBlob { get { throw null; } }
        public static System.Security.Cryptography.CngKeyBlobFormat Pkcs8PrivateBlob { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Security.Cryptography.CngKeyBlobFormat other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.CngKeyBlobFormat left, System.Security.Cryptography.CngKeyBlobFormat right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.CngKeyBlobFormat left, System.Security.Cryptography.CngKeyBlobFormat right) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum CngKeyCreationOptions
    {
        None = 0,
        MachineKey = 32,
        OverwriteExistingKey = 128,
    }
    public sealed partial class CngKeyCreationParameters
    {
        public CngKeyCreationParameters() { }
        public System.Security.Cryptography.CngExportPolicies? ExportPolicy { get { throw null; } set { } }
        public System.Security.Cryptography.CngKeyCreationOptions KeyCreationOptions { get { throw null; } set { } }
        public System.Security.Cryptography.CngKeyUsages? KeyUsage { get { throw null; } set { } }
        public System.Security.Cryptography.CngPropertyCollection Parameters { get { throw null; } }
        public System.IntPtr ParentWindowHandle { get { throw null; } set { } }
        public System.Security.Cryptography.CngProvider Provider { get { throw null; } set { } }
        public System.Security.Cryptography.CngUIPolicy UIPolicy { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum CngKeyHandleOpenOptions
    {
        None = 0,
        EphemeralKey = 1,
    }
    [System.FlagsAttribute]
    public enum CngKeyOpenOptions
    {
        None = 0,
        UserKey = 0,
        MachineKey = 32,
        Silent = 64,
    }
    [System.FlagsAttribute]
    public enum CngKeyUsages
    {
        None = 0,
        Decryption = 1,
        Signing = 2,
        KeyAgreement = 4,
        AllUsages = 16777215,
    }
    public partial struct CngProperty : System.IEquatable<System.Security.Cryptography.CngProperty>
    {
        private object _dummy;
        public CngProperty(string name, byte[] value, System.Security.Cryptography.CngPropertyOptions options) { throw null; }
        public string Name { get { throw null; } }
        public System.Security.Cryptography.CngPropertyOptions Options { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Security.Cryptography.CngProperty other) { throw null; }
        public override int GetHashCode() { throw null; }
        public byte[] GetValue() { throw null; }
        public static bool operator ==(System.Security.Cryptography.CngProperty left, System.Security.Cryptography.CngProperty right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.CngProperty left, System.Security.Cryptography.CngProperty right) { throw null; }
    }
    public sealed partial class CngPropertyCollection : System.Collections.ObjectModel.Collection<System.Security.Cryptography.CngProperty>
    {
        public CngPropertyCollection() { }
    }
    [System.FlagsAttribute]
    public enum CngPropertyOptions
    {
        Persist = -2147483648,
        None = 0,
        CustomProperty = 1073741824,
    }
    public sealed partial class CngProvider : System.IEquatable<System.Security.Cryptography.CngProvider>
    {
        public CngProvider(string provider) { }
        public static System.Security.Cryptography.CngProvider MicrosoftSmartCardKeyStorageProvider { get { throw null; } }
        public static System.Security.Cryptography.CngProvider MicrosoftSoftwareKeyStorageProvider { get { throw null; } }
        public string Provider { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Security.Cryptography.CngProvider other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.CngProvider left, System.Security.Cryptography.CngProvider right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.CngProvider left, System.Security.Cryptography.CngProvider right) { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class CngUIPolicy
    {
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel) { }
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel, string friendlyName) { }
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel, string friendlyName, string description) { }
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel, string friendlyName, string description, string useContext) { }
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel, string friendlyName, string description, string useContext, string creationTitle) { }
        public string CreationTitle { get { throw null; } }
        public string Description { get { throw null; } }
        public string FriendlyName { get { throw null; } }
        public System.Security.Cryptography.CngUIProtectionLevels ProtectionLevel { get { throw null; } }
        public string UseContext { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum CngUIProtectionLevels
    {
        None = 0,
        ProtectKey = 1,
        ForceHighProtection = 2,
    }
    public sealed partial class DSACng : System.Security.Cryptography.DSA
    {
        public DSACng() { }
        public DSACng(int keySize) { }
        public DSACng(System.Security.Cryptography.CngKey key) { }
        public System.Security.Cryptography.CngKey Key { get { throw null; } }
        public override string KeyExchangeAlgorithm { get { throw null; } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public override string SignatureAlgorithm { get { throw null; } }
        public override byte[] CreateSignature(byte[] rgbHash) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override System.Security.Cryptography.DSAParameters ExportParameters(bool includePrivateParameters) { throw null; }
#if FEATURE_DSA_HASHDATA
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
#endif
        public override void ImportParameters(System.Security.Cryptography.DSAParameters parameters) { }
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) { throw null; }
    }
    public sealed partial class ECDiffieHellmanCng : System.Security.Cryptography.ECDiffieHellman
    {
        public ECDiffieHellmanCng() { }
        public ECDiffieHellmanCng(int keySize) { }
        public ECDiffieHellmanCng(System.Security.Cryptography.CngKey key) { }
#if FEATURE_ECPARAMETERS
        public ECDiffieHellmanCng(System.Security.Cryptography.ECCurve curve) { }
#endif
        public System.Security.Cryptography.CngAlgorithm HashAlgorithm { get { throw null; } set { } }
        public byte[] HmacKey { get { throw null; } set { } }
        public System.Security.Cryptography.CngKey Key { get { throw null; } }
        public System.Security.Cryptography.ECDiffieHellmanKeyDerivationFunction KeyDerivationFunction { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        public byte[] Label { get { throw null; } set { } }
        public override System.Security.Cryptography.ECDiffieHellmanPublicKey PublicKey { get { throw null; } }
        public byte[] SecretAppend { get { throw null; } set { } }
        public byte[] SecretPrepend { get { throw null; } set { } }
        public byte[] Seed { get { throw null; } set { } }
        public bool UseSecretAgreementAsHmacKey { get { throw null; } }
#if FEATURE_ECDH_DERIVEFROM
        public override byte[] DeriveKeyFromHash(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[] secretPrepend, byte[] secretAppend) { throw null; }
        public override byte[] DeriveKeyFromHmac(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[] hmacKey, byte[] secretPrepend, byte[] secretAppend) { throw null; }
#endif
        public byte[] DeriveKeyMaterial(System.Security.Cryptography.CngKey otherPartyPublicKey) { throw null; }
        public override byte[] DeriveKeyMaterial(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey) { throw null; }
#if FEATURE_ECDH_DERIVEFROM
        public override byte[] DeriveKeyTls(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed) { throw null; }
#endif
        public Microsoft.Win32.SafeHandles.SafeNCryptSecretHandle DeriveSecretAgreementHandle(System.Security.Cryptography.CngKey otherPartyPublicKey) { throw null; }
        public Microsoft.Win32.SafeHandles.SafeNCryptSecretHandle DeriveSecretAgreementHandle(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey) { throw null; }
        protected override void Dispose(bool disposing) { }
#if FEATURE_ECPARAMETERS
        public override System.Security.Cryptography.ECParameters ExportExplicitParameters(bool includePrivateParameters) { throw null; }
        public override System.Security.Cryptography.ECParameters ExportParameters(bool includePrivateParameters) { throw null; }
#endif
        public void FromXmlString(string xml, System.Security.Cryptography.ECKeyXmlFormat format) { }
#if FEATURE_ECPARAMETERS
        public override void GenerateKey(System.Security.Cryptography.ECCurve curve) { }
        public override void ImportParameters(System.Security.Cryptography.ECParameters parameters) { }
#endif
        public string ToXmlString(System.Security.Cryptography.ECKeyXmlFormat format) { throw null; }
    }
    public sealed partial class ECDiffieHellmanCngPublicKey : System.Security.Cryptography.ECDiffieHellmanPublicKey
    {
        // ECDiffieHellmanPublicKey parameter-less ctor only exist on netfx 4.7+
        private ECDiffieHellmanCngPublicKey() : base(null) { }
        public System.Security.Cryptography.CngKeyBlobFormat BlobFormat { get { throw null; } }
        protected override void Dispose(bool disposing) { }
#if FEATURE_ECPARAMETERS
        public override System.Security.Cryptography.ECParameters ExportExplicitParameters() { throw null; }
        public override System.Security.Cryptography.ECParameters ExportParameters() { throw null; }
#endif
        public static System.Security.Cryptography.ECDiffieHellmanPublicKey FromByteArray(byte[] publicKeyBlob, System.Security.Cryptography.CngKeyBlobFormat format) { throw null; }
        public static System.Security.Cryptography.ECDiffieHellmanCngPublicKey FromXmlString(string xml) { throw null; }
        public System.Security.Cryptography.CngKey Import() { throw null; }
        public override string ToXmlString() { throw null; }
    }
    public enum ECDiffieHellmanKeyDerivationFunction
    {
        Hash = 0,
        Hmac = 1,
        Tls = 2,
    }
    public sealed partial class ECDsaCng : System.Security.Cryptography.ECDsa
    {
        public ECDsaCng() { }
        public ECDsaCng(int keySize) { }
        public ECDsaCng(System.Security.Cryptography.CngKey key) { }
#if FEATURE_ECPARAMETERS // types missing from netfx and net462 targeting pack
        public ECDsaCng(System.Security.Cryptography.ECCurve curve) { }
#endif
        public System.Security.Cryptography.CngAlgorithm HashAlgorithm { get { throw null; } set { } }
        public System.Security.Cryptography.CngKey Key { get { throw null; } }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        protected override void Dispose(bool disposing) { }
#if FEATURE_ECPARAMETERS // types missing from netfx and net462 targeting pack
        public override System.Security.Cryptography.ECParameters ExportExplicitParameters(bool includePrivateParameters) { throw null; }
        public override System.Security.Cryptography.ECParameters ExportParameters(bool includePrivateParameters) { throw null; }
#endif
        public void FromXmlString(string xml, System.Security.Cryptography.ECKeyXmlFormat format) { }
#if FEATURE_ECPARAMETERS // types missing from netfx and net462 targeting pack
        public override void GenerateKey(System.Security.Cryptography.ECCurve curve) { }
#endif
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
#if FEATURE_ECPARAMETERS // types missing from netfx and net462 targeting pack
        public override void ImportParameters(System.Security.Cryptography.ECParameters parameters) { }
#endif
        public byte[] SignData(byte[] data) { throw null; }
        public byte[] SignData(byte[] data, int offset, int count) { throw null; }
        public byte[] SignData(System.IO.Stream data) { throw null; }
        public override byte[] SignHash(byte[] hash) { throw null; }
        public string ToXmlString(System.Security.Cryptography.ECKeyXmlFormat format) { throw null; }
        public bool VerifyData(byte[] data, byte[] signature) { throw null; }
        public bool VerifyData(byte[] data, int offset, int count, byte[] signature) { throw null; }
        public bool VerifyData(System.IO.Stream data, byte[] signature) { throw null; }
        public override bool VerifyHash(byte[] hash, byte[] signature) { throw null; }
    }
    public enum ECKeyXmlFormat
    {
        Rfc4050 = 0,
    }
    public sealed partial class RSACng : System.Security.Cryptography.RSA
    {
        public RSACng() { }
        public RSACng(int keySize) { }
        public RSACng(System.Security.Cryptography.CngKey key) { }
        public System.Security.Cryptography.CngKey Key { get { throw null; } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public override byte[] Decrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw null; }
        public override System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters) { throw null; }
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.RSAParameters parameters) { }
        public override byte[] SignHash(byte[] hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public override bool VerifyHash(byte[] hash, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
    }
    public sealed partial class TripleDESCng : System.Security.Cryptography.TripleDES
    {
        public TripleDESCng() { }
        public TripleDESCng(string keyName) { }
        public TripleDESCng(string keyName, System.Security.Cryptography.CngProvider provider) { }
        public TripleDESCng(string keyName, System.Security.Cryptography.CngProvider provider, System.Security.Cryptography.CngKeyOpenOptions openOptions) { }
        public override byte[] Key { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV) { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void GenerateIV() { }
        public override void GenerateKey() { }
    }
}
