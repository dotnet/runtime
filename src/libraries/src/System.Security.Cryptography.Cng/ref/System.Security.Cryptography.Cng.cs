// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    public abstract partial class SafeNCryptHandle : System.Runtime.InteropServices.SafeHandle
    {
        protected SafeNCryptHandle() : base(default(System.IntPtr), default(bool)) { }
        protected override bool ReleaseHandle() { return default(bool); }
        protected abstract bool ReleaseNativeHandle();
    }
    public sealed partial class SafeNCryptKeyHandle : Microsoft.Win32.SafeHandles.SafeNCryptHandle
    {
        public SafeNCryptKeyHandle() { }
        protected override bool ReleaseNativeHandle() { return default(bool); }
    }
    public sealed partial class SafeNCryptProviderHandle : Microsoft.Win32.SafeHandles.SafeNCryptHandle
    {
        public SafeNCryptProviderHandle() { }
        protected override bool ReleaseNativeHandle() { return default(bool); }
    }
    public sealed partial class SafeNCryptSecretHandle : Microsoft.Win32.SafeHandles.SafeNCryptHandle
    {
        public SafeNCryptSecretHandle() { }
        protected override bool ReleaseNativeHandle() { return default(bool); }
    }
}
namespace System.Security.Cryptography
{
    public sealed partial class CngAlgorithm : System.IEquatable<System.Security.Cryptography.CngAlgorithm>
    {
        public CngAlgorithm(string algorithm) { }
        public string Algorithm { get { return default(string); } }
        public static System.Security.Cryptography.CngAlgorithm ECDiffieHellmanP256 { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public static System.Security.Cryptography.CngAlgorithm ECDiffieHellmanP384 { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public static System.Security.Cryptography.CngAlgorithm ECDiffieHellmanP521 { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public static System.Security.Cryptography.CngAlgorithm ECDsaP256 { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public static System.Security.Cryptography.CngAlgorithm ECDsaP384 { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public static System.Security.Cryptography.CngAlgorithm ECDsaP521 { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public static System.Security.Cryptography.CngAlgorithm MD5 { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public static System.Security.Cryptography.CngAlgorithm Rsa { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public static System.Security.Cryptography.CngAlgorithm Sha1 { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public static System.Security.Cryptography.CngAlgorithm Sha256 { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public static System.Security.Cryptography.CngAlgorithm Sha384 { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public static System.Security.Cryptography.CngAlgorithm Sha512 { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Security.Cryptography.CngAlgorithm other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Security.Cryptography.CngAlgorithm left, System.Security.Cryptography.CngAlgorithm right) { return default(bool); }
        public static bool operator !=(System.Security.Cryptography.CngAlgorithm left, System.Security.Cryptography.CngAlgorithm right) { return default(bool); }
        public override string ToString() { return default(string); }
    }
    public sealed partial class CngAlgorithmGroup : System.IEquatable<System.Security.Cryptography.CngAlgorithmGroup>
    {
        public CngAlgorithmGroup(string algorithmGroup) { }
        public string AlgorithmGroup { get { return default(string); } }
        public static System.Security.Cryptography.CngAlgorithmGroup DiffieHellman { get { return default(System.Security.Cryptography.CngAlgorithmGroup); } }
        public static System.Security.Cryptography.CngAlgorithmGroup Dsa { get { return default(System.Security.Cryptography.CngAlgorithmGroup); } }
        public static System.Security.Cryptography.CngAlgorithmGroup ECDiffieHellman { get { return default(System.Security.Cryptography.CngAlgorithmGroup); } }
        public static System.Security.Cryptography.CngAlgorithmGroup ECDsa { get { return default(System.Security.Cryptography.CngAlgorithmGroup); } }
        public static System.Security.Cryptography.CngAlgorithmGroup Rsa { get { return default(System.Security.Cryptography.CngAlgorithmGroup); } }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Security.Cryptography.CngAlgorithmGroup other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Security.Cryptography.CngAlgorithmGroup left, System.Security.Cryptography.CngAlgorithmGroup right) { return default(bool); }
        public static bool operator !=(System.Security.Cryptography.CngAlgorithmGroup left, System.Security.Cryptography.CngAlgorithmGroup right) { return default(bool); }
        public override string ToString() { return default(string); }
    }
    [System.FlagsAttribute]
    public enum CngExportPolicies
    {
        AllowArchiving = 4,
        AllowExport = 1,
        AllowPlaintextArchiving = 8,
        AllowPlaintextExport = 2,
        None = 0,
    }
    public sealed partial class CngKey : System.IDisposable
    {
        internal CngKey() { }
        public System.Security.Cryptography.CngAlgorithm Algorithm { get { return default(System.Security.Cryptography.CngAlgorithm); } }
        public System.Security.Cryptography.CngAlgorithmGroup AlgorithmGroup { get { return default(System.Security.Cryptography.CngAlgorithmGroup); } }
        public System.Security.Cryptography.CngExportPolicies ExportPolicy { get { return default(System.Security.Cryptography.CngExportPolicies); } }
        public Microsoft.Win32.SafeHandles.SafeNCryptKeyHandle Handle { get { return default(Microsoft.Win32.SafeHandles.SafeNCryptKeyHandle); } }
        public bool IsEphemeral { get { return default(bool); } }
        public bool IsMachineKey { get { return default(bool); } }
        public string KeyName { get { return default(string); } }
        public int KeySize { get { return default(int); } }
        public System.Security.Cryptography.CngKeyUsages KeyUsage { get { return default(System.Security.Cryptography.CngKeyUsages); } }
        public System.IntPtr ParentWindowHandle { get { return default(System.IntPtr); } set { } }
        public System.Security.Cryptography.CngProvider Provider { get { return default(System.Security.Cryptography.CngProvider); } }
        public Microsoft.Win32.SafeHandles.SafeNCryptProviderHandle ProviderHandle { get { return default(Microsoft.Win32.SafeHandles.SafeNCryptProviderHandle); } }
        public System.Security.Cryptography.CngUIPolicy UIPolicy { get { return default(System.Security.Cryptography.CngUIPolicy); } }
        public string UniqueName { get { return default(string); } }
        public static System.Security.Cryptography.CngKey Create(System.Security.Cryptography.CngAlgorithm algorithm) { return default(System.Security.Cryptography.CngKey); }
        public static System.Security.Cryptography.CngKey Create(System.Security.Cryptography.CngAlgorithm algorithm, string keyName) { return default(System.Security.Cryptography.CngKey); }
        public static System.Security.Cryptography.CngKey Create(System.Security.Cryptography.CngAlgorithm algorithm, string keyName, System.Security.Cryptography.CngKeyCreationParameters creationParameters) { return default(System.Security.Cryptography.CngKey); }
        public void Delete() { }
        public void Dispose() { }
        public static bool Exists(string keyName) { return default(bool); }
        public static bool Exists(string keyName, System.Security.Cryptography.CngProvider provider) { return default(bool); }
        public static bool Exists(string keyName, System.Security.Cryptography.CngProvider provider, System.Security.Cryptography.CngKeyOpenOptions options) { return default(bool); }
        public byte[] Export(System.Security.Cryptography.CngKeyBlobFormat format) { return default(byte[]); }
        public System.Security.Cryptography.CngProperty GetProperty(string name, System.Security.Cryptography.CngPropertyOptions options) { return default(System.Security.Cryptography.CngProperty); }
        public bool HasProperty(string name, System.Security.Cryptography.CngPropertyOptions options) { return default(bool); }
        public static System.Security.Cryptography.CngKey Import(byte[] keyBlob, System.Security.Cryptography.CngKeyBlobFormat format) { return default(System.Security.Cryptography.CngKey); }
        public static System.Security.Cryptography.CngKey Import(byte[] keyBlob, System.Security.Cryptography.CngKeyBlobFormat format, System.Security.Cryptography.CngProvider provider) { return default(System.Security.Cryptography.CngKey); }
        public static System.Security.Cryptography.CngKey Open(Microsoft.Win32.SafeHandles.SafeNCryptKeyHandle keyHandle, System.Security.Cryptography.CngKeyHandleOpenOptions keyHandleOpenOptions) { return default(System.Security.Cryptography.CngKey); }
        public static System.Security.Cryptography.CngKey Open(string keyName) { return default(System.Security.Cryptography.CngKey); }
        public static System.Security.Cryptography.CngKey Open(string keyName, System.Security.Cryptography.CngProvider provider) { return default(System.Security.Cryptography.CngKey); }
        public static System.Security.Cryptography.CngKey Open(string keyName, System.Security.Cryptography.CngProvider provider, System.Security.Cryptography.CngKeyOpenOptions openOptions) { return default(System.Security.Cryptography.CngKey); }
        public void SetProperty(System.Security.Cryptography.CngProperty property) { }
    }
    public sealed partial class CngKeyBlobFormat : System.IEquatable<System.Security.Cryptography.CngKeyBlobFormat>
    {
        public CngKeyBlobFormat(string format) { }
        public static System.Security.Cryptography.CngKeyBlobFormat EccPrivateBlob { get { return default(System.Security.Cryptography.CngKeyBlobFormat); } }
        public static System.Security.Cryptography.CngKeyBlobFormat EccPublicBlob { get { return default(System.Security.Cryptography.CngKeyBlobFormat); } }
        public string Format { get { return default(string); } }
        public static System.Security.Cryptography.CngKeyBlobFormat GenericPrivateBlob { get { return default(System.Security.Cryptography.CngKeyBlobFormat); } }
        public static System.Security.Cryptography.CngKeyBlobFormat GenericPublicBlob { get { return default(System.Security.Cryptography.CngKeyBlobFormat); } }
        public static System.Security.Cryptography.CngKeyBlobFormat OpaqueTransportBlob { get { return default(System.Security.Cryptography.CngKeyBlobFormat); } }
        public static System.Security.Cryptography.CngKeyBlobFormat Pkcs8PrivateBlob { get { return default(System.Security.Cryptography.CngKeyBlobFormat); } }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Security.Cryptography.CngKeyBlobFormat other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Security.Cryptography.CngKeyBlobFormat left, System.Security.Cryptography.CngKeyBlobFormat right) { return default(bool); }
        public static bool operator !=(System.Security.Cryptography.CngKeyBlobFormat left, System.Security.Cryptography.CngKeyBlobFormat right) { return default(bool); }
        public override string ToString() { return default(string); }
    }
    [System.FlagsAttribute]
    public enum CngKeyCreationOptions
    {
        MachineKey = 32,
        None = 0,
        OverwriteExistingKey = 128,
    }
    public sealed partial class CngKeyCreationParameters
    {
        public CngKeyCreationParameters() { }
        public System.Nullable<System.Security.Cryptography.CngExportPolicies> ExportPolicy { get { return default(System.Nullable<System.Security.Cryptography.CngExportPolicies>); } set { } }
        public System.Security.Cryptography.CngKeyCreationOptions KeyCreationOptions { get { return default(System.Security.Cryptography.CngKeyCreationOptions); } set { } }
        public System.Nullable<System.Security.Cryptography.CngKeyUsages> KeyUsage { get { return default(System.Nullable<System.Security.Cryptography.CngKeyUsages>); } set { } }
        public System.Security.Cryptography.CngPropertyCollection Parameters { get { return default(System.Security.Cryptography.CngPropertyCollection); } }
        public System.IntPtr ParentWindowHandle { get { return default(System.IntPtr); } set { } }
        public System.Security.Cryptography.CngProvider Provider { get { return default(System.Security.Cryptography.CngProvider); } set { } }
        public System.Security.Cryptography.CngUIPolicy UIPolicy { get { return default(System.Security.Cryptography.CngUIPolicy); } set { } }
    }
    [System.FlagsAttribute]
    public enum CngKeyHandleOpenOptions
    {
        EphemeralKey = 1,
        None = 0,
    }
    [System.FlagsAttribute]
    public enum CngKeyOpenOptions
    {
        MachineKey = 32,
        None = 0,
        Silent = 64,
        UserKey = 0,
    }
    [System.FlagsAttribute]
    public enum CngKeyUsages
    {
        AllUsages = 16777215,
        Decryption = 1,
        KeyAgreement = 4,
        None = 0,
        Signing = 2,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CngProperty : System.IEquatable<System.Security.Cryptography.CngProperty>
    {
        public CngProperty(string name, byte[] value, System.Security.Cryptography.CngPropertyOptions options) { throw new System.NotImplementedException(); }
        public string Name { get { return default(string); } }
        public System.Security.Cryptography.CngPropertyOptions Options { get { return default(System.Security.Cryptography.CngPropertyOptions); } }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Security.Cryptography.CngProperty other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public byte[] GetValue() { return default(byte[]); }
        public static bool operator ==(System.Security.Cryptography.CngProperty left, System.Security.Cryptography.CngProperty right) { return default(bool); }
        public static bool operator !=(System.Security.Cryptography.CngProperty left, System.Security.Cryptography.CngProperty right) { return default(bool); }
    }
    public sealed partial class CngPropertyCollection : System.Collections.ObjectModel.Collection<System.Security.Cryptography.CngProperty>
    {
        public CngPropertyCollection() { }
    }
    [System.FlagsAttribute]
    public enum CngPropertyOptions
    {
        CustomProperty = 1073741824,
        None = 0,
        Persist = -2147483648,
    }
    public sealed partial class CngProvider : System.IEquatable<System.Security.Cryptography.CngProvider>
    {
        public CngProvider(string provider) { }
        public static System.Security.Cryptography.CngProvider MicrosoftSmartCardKeyStorageProvider { get { return default(System.Security.Cryptography.CngProvider); } }
        public static System.Security.Cryptography.CngProvider MicrosoftSoftwareKeyStorageProvider { get { return default(System.Security.Cryptography.CngProvider); } }
        public string Provider { get { return default(string); } }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Security.Cryptography.CngProvider other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Security.Cryptography.CngProvider left, System.Security.Cryptography.CngProvider right) { return default(bool); }
        public static bool operator !=(System.Security.Cryptography.CngProvider left, System.Security.Cryptography.CngProvider right) { return default(bool); }
        public override string ToString() { return default(string); }
    }
    public sealed partial class CngUIPolicy
    {
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel) { }
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel, string friendlyName) { }
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel, string friendlyName, string description) { }
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel, string friendlyName, string description, string useContext) { }
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel, string friendlyName, string description, string useContext, string creationTitle) { }
        public string CreationTitle { get { return default(string); } }
        public string Description { get { return default(string); } }
        public string FriendlyName { get { return default(string); } }
        public System.Security.Cryptography.CngUIProtectionLevels ProtectionLevel { get { return default(System.Security.Cryptography.CngUIProtectionLevels); } }
        public string UseContext { get { return default(string); } }
    }
    [System.FlagsAttribute]
    public enum CngUIProtectionLevels
    {
        ForceHighProtection = 2,
        None = 0,
        ProtectKey = 1,
    }
    public sealed partial class ECDsaCng : System.Security.Cryptography.ECDsa
    {
        public ECDsaCng() {}
        public ECDsaCng(int keySize) {}
        public ECDsaCng(CngKey key) {}
        public System.Security.Cryptography.CngKey Key { get { return default(System.Security.Cryptography.CngKey); } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { return default(System.Security.Cryptography.KeySizes[]); } }
        protected override void Dispose(bool disposing) {}
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { return default(byte[]); }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { return default(byte[]); }
        public override byte[] SignHash(byte[] hash) { return default(byte[]); }
        public override bool VerifyHash(byte[] hash, byte[] signature) { return default(bool); }
    }
    public sealed partial class RSACng : System.Security.Cryptography.RSA
    {
        public RSACng() { }
        public RSACng(int keySize) { }
        public RSACng(System.Security.Cryptography.CngKey key) { }
        public System.Security.Cryptography.CngKey Key { get { return default(System.Security.Cryptography.CngKey); } }
        public override byte[] Decrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { return default(byte[]); }
        protected override void Dispose(bool disposing) { }
        public override byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { return default(byte[]); }
        public override System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters) { return default(System.Security.Cryptography.RSAParameters); }
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { return default(byte[]); }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { return default(byte[]); }
        public override void ImportParameters(System.Security.Cryptography.RSAParameters parameters) { }
        public override byte[] SignHash(byte[] hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { return default(byte[]); }
        public override bool VerifyHash(byte[] hash, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { return default(bool); }
    }
}
