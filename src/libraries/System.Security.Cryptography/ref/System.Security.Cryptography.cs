// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Win32.SafeHandles
{
    public abstract partial class SafeNCryptHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        protected SafeNCryptHandle() : base (default(bool)) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        protected SafeNCryptHandle(System.IntPtr handle, System.Runtime.InteropServices.SafeHandle parentHandle) : base (default(bool)) { }
        protected override bool ReleaseHandle() { throw null; }
        protected abstract bool ReleaseNativeHandle();
    }
    public sealed partial class SafeNCryptKeyHandle : Microsoft.Win32.SafeHandles.SafeNCryptHandle
    {
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public SafeNCryptKeyHandle() { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public SafeNCryptKeyHandle(System.IntPtr handle, System.Runtime.InteropServices.SafeHandle parentHandle) { }
        protected override bool ReleaseNativeHandle() { throw null; }
    }
    public sealed partial class SafeNCryptProviderHandle : Microsoft.Win32.SafeHandles.SafeNCryptHandle
    {
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public SafeNCryptProviderHandle() { }
        protected override bool ReleaseNativeHandle() { throw null; }
    }
    public sealed partial class SafeNCryptSecretHandle : Microsoft.Win32.SafeHandles.SafeNCryptHandle
    {
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public SafeNCryptSecretHandle() { }
        protected override bool ReleaseNativeHandle() { throw null; }
    }
    public sealed partial class SafeX509ChainHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeX509ChainHandle() : base (default(bool)) { }
        protected override void Dispose(bool disposing) { }
        protected override bool ReleaseHandle() { throw null; }
    }
}
namespace System.Security.Cryptography
{
    public abstract partial class Aes : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected Aes() { }
        public static new System.Security.Cryptography.Aes Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.Aes? Create(string algorithmName) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
    public sealed partial class AesCcm : System.IDisposable
    {
        public AesCcm(byte[] key) { }
        public AesCcm(System.ReadOnlySpan<byte> key) { }
        public static bool IsSupported { get { throw null; } }
        public static System.Security.Cryptography.KeySizes NonceByteSizes { get { throw null; } }
        public static System.Security.Cryptography.KeySizes TagByteSizes { get { throw null; } }
        public void Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext, byte[]? associatedData = null) { }
        public void Decrypt(System.ReadOnlySpan<byte> nonce, System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> tag, System.Span<byte> plaintext, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { }
        public void Dispose() { }
        public void Encrypt(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag, byte[]? associatedData = null) { }
        public void Encrypt(System.ReadOnlySpan<byte> nonce, System.ReadOnlySpan<byte> plaintext, System.Span<byte> ciphertext, System.Span<byte> tag, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { }
    }
    public sealed partial class AesCng : System.Security.Cryptography.Aes
    {
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public AesCng() { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public AesCng(string keyName) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public AesCng(string keyName, System.Security.Cryptography.CngProvider provider) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public AesCng(string keyName, System.Security.Cryptography.CngProvider provider, System.Security.Cryptography.CngKeyOpenOptions openOptions) { }
        public override byte[] Key { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void GenerateIV() { }
        public override void GenerateKey() { }
        protected override bool TryDecryptCbcCore(System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
        protected override bool TryDecryptCfbCore(System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten) { throw null; }
        protected override bool TryDecryptEcbCore(System.ReadOnlySpan<byte> ciphertext, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
        protected override bool TryEncryptCbcCore(System.ReadOnlySpan<byte> plaintext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
        protected override bool TryEncryptCfbCore(System.ReadOnlySpan<byte> plaintext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten) { throw null; }
        protected override bool TryEncryptEcbCore(System.ReadOnlySpan<byte> plaintext, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class AesCryptoServiceProvider : System.Security.Cryptography.Aes
    {
        public AesCryptoServiceProvider() { }
        public override int BlockSize { get { throw null; } set { } }
        public override int FeedbackSize { get { throw null; } set { } }
        public override byte[] IV { get { throw null; } set { } }
        public override byte[] Key { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.KeySizes[] LegalBlockSizes { get { throw null; } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public override System.Security.Cryptography.CipherMode Mode { get { throw null; } set { } }
        public override System.Security.Cryptography.PaddingMode Padding { get { throw null; } set { } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void GenerateIV() { }
        public override void GenerateKey() { }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
    public sealed partial class AesGcm : System.IDisposable
    {
        public AesGcm(byte[] key) { }
        public AesGcm(System.ReadOnlySpan<byte> key) { }
        public static bool IsSupported { get { throw null; } }
        public static System.Security.Cryptography.KeySizes NonceByteSizes { get { throw null; } }
        public static System.Security.Cryptography.KeySizes TagByteSizes { get { throw null; } }
        public void Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext, byte[]? associatedData = null) { }
        public void Decrypt(System.ReadOnlySpan<byte> nonce, System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> tag, System.Span<byte> plaintext, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { }
        public void Dispose() { }
        public void Encrypt(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag, byte[]? associatedData = null) { }
        public void Encrypt(System.ReadOnlySpan<byte> nonce, System.ReadOnlySpan<byte> plaintext, System.Span<byte> ciphertext, System.Span<byte> tag, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class AesManaged : System.Security.Cryptography.Aes
    {
        public AesManaged() { }
        public override int BlockSize { get { throw null; } set { } }
        public override int FeedbackSize { get { throw null; } set { } }
        public override byte[] IV { get { throw null; } set { } }
        public override byte[] Key { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.KeySizes[] LegalBlockSizes { get { throw null; } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public override System.Security.Cryptography.CipherMode Mode { get { throw null; } set { } }
        public override System.Security.Cryptography.PaddingMode Padding { get { throw null; } set { } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void GenerateIV() { }
        public override void GenerateKey() { }
    }
    public partial class AsnEncodedData
    {
        protected AsnEncodedData() { }
        public AsnEncodedData(byte[] rawData) { }
        public AsnEncodedData(System.ReadOnlySpan<byte> rawData) { }
        public AsnEncodedData(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
        public AsnEncodedData(System.Security.Cryptography.Oid? oid, byte[] rawData) { }
        public AsnEncodedData(System.Security.Cryptography.Oid? oid, System.ReadOnlySpan<byte> rawData) { }
        public AsnEncodedData(string oid, byte[] rawData) { }
        public AsnEncodedData(string oid, System.ReadOnlySpan<byte> rawData) { }
        public System.Security.Cryptography.Oid? Oid { get { throw null; } set { } }
        public byte[] RawData { get { throw null; } set { } }
        public virtual void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
        public virtual string Format(bool multiLine) { throw null; }
    }
    public sealed partial class AsnEncodedDataCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public AsnEncodedDataCollection() { }
        public AsnEncodedDataCollection(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Security.Cryptography.AsnEncodedData this[int index] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public int Add(System.Security.Cryptography.AsnEncodedData asnEncodedData) { throw null; }
        public void CopyTo(System.Security.Cryptography.AsnEncodedData[] array, int index) { }
        public System.Security.Cryptography.AsnEncodedDataEnumerator GetEnumerator() { throw null; }
        public void Remove(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class AsnEncodedDataEnumerator : System.Collections.IEnumerator
    {
        internal AsnEncodedDataEnumerator() { }
        public System.Security.Cryptography.AsnEncodedData Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    public abstract partial class AsymmetricAlgorithm : System.IDisposable
    {
        protected int KeySizeValue;
        [System.Diagnostics.CodeAnalysis.MaybeNullAttribute]
        protected System.Security.Cryptography.KeySizes[] LegalKeySizesValue;
        protected AsymmetricAlgorithm() { }
        public virtual string? KeyExchangeAlgorithm { get { throw null; } }
        public virtual int KeySize { get { throw null; } set { } }
        public virtual System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public virtual string? SignatureAlgorithm { get { throw null; } }
        public void Clear() { }
        [System.ObsoleteAttribute("The default implementation of this cryptography algorithm is not supported.", DiagnosticId="SYSLIB0007", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static System.Security.Cryptography.AsymmetricAlgorithm Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static System.Security.Cryptography.AsymmetricAlgorithm? Create(string algName) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual byte[] ExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public virtual byte[] ExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public string ExportEncryptedPkcs8PrivateKeyPem(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public virtual byte[] ExportPkcs8PrivateKey() { throw null; }
        public string ExportPkcs8PrivateKeyPem() { throw null; }
        public virtual byte[] ExportSubjectPublicKeyInfo() { throw null; }
        public string ExportSubjectPublicKeyInfoPem() { throw null; }
        public virtual void FromXmlString(string xmlString) { }
        public virtual void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public virtual void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public virtual void ImportFromEncryptedPem(System.ReadOnlySpan<char> input, System.ReadOnlySpan<byte> passwordBytes) { }
        public virtual void ImportFromEncryptedPem(System.ReadOnlySpan<char> input, System.ReadOnlySpan<char> password) { }
        public virtual void ImportFromPem(System.ReadOnlySpan<char> input) { }
        public virtual void ImportPkcs8PrivateKey(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public virtual void ImportSubjectPublicKeyInfo(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public virtual string ToXmlString(bool includePrivateParameters) { throw null; }
        public virtual bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public virtual bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TryExportEncryptedPkcs8PrivateKeyPem(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<char> destination, out int charsWritten) { throw null; }
        public virtual bool TryExportPkcs8PrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TryExportPkcs8PrivateKeyPem(System.Span<char> destination, out int charsWritten) { throw null; }
        public virtual bool TryExportSubjectPublicKeyInfo(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TryExportSubjectPublicKeyInfoPem(System.Span<char> destination, out int charsWritten) { throw null; }
    }
    public abstract partial class AsymmetricKeyExchangeDeformatter
    {
        protected AsymmetricKeyExchangeDeformatter() { }
        public abstract string? Parameters { get; set; }
        public abstract byte[] DecryptKeyExchange(byte[] rgb);
        public abstract void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key);
    }
    public abstract partial class AsymmetricKeyExchangeFormatter
    {
        protected AsymmetricKeyExchangeFormatter() { }
        public abstract string? Parameters { get; }
        public abstract byte[] CreateKeyExchange(byte[] data);
        public abstract byte[] CreateKeyExchange(byte[] data, System.Type? symAlgType);
        public abstract void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key);
    }
    public abstract partial class AsymmetricSignatureDeformatter
    {
        protected AsymmetricSignatureDeformatter() { }
        public abstract void SetHashAlgorithm(string strName);
        public abstract void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key);
        public abstract bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);
        public virtual bool VerifySignature(System.Security.Cryptography.HashAlgorithm hash, byte[] rgbSignature) { throw null; }
    }
    public abstract partial class AsymmetricSignatureFormatter
    {
        protected AsymmetricSignatureFormatter() { }
        public abstract byte[] CreateSignature(byte[] rgbHash);
        public virtual byte[] CreateSignature(System.Security.Cryptography.HashAlgorithm hash) { throw null; }
        public abstract void SetHashAlgorithm(string strName);
        public abstract void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key);
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
    public sealed partial class ChaCha20Poly1305 : System.IDisposable
    {
        public ChaCha20Poly1305(byte[] key) { }
        public ChaCha20Poly1305(System.ReadOnlySpan<byte> key) { }
        public static bool IsSupported { get { throw null; } }
        public void Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext, byte[]? associatedData = null) { }
        public void Decrypt(System.ReadOnlySpan<byte> nonce, System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> tag, System.Span<byte> plaintext, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { }
        public void Dispose() { }
        public void Encrypt(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag, byte[]? associatedData = null) { }
        public void Encrypt(System.ReadOnlySpan<byte> nonce, System.ReadOnlySpan<byte> plaintext, System.Span<byte> ciphertext, System.Span<byte> tag, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { }
    }
    public enum CipherMode
    {
        CBC = 1,
        ECB = 2,
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        OFB = 3,
        CFB = 4,
        CTS = 5,
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
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Security.Cryptography.CngAlgorithm? other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.CngAlgorithm? left, System.Security.Cryptography.CngAlgorithm? right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.CngAlgorithm? left, System.Security.Cryptography.CngAlgorithm? right) { throw null; }
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
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Security.Cryptography.CngAlgorithmGroup? other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.CngAlgorithmGroup? left, System.Security.Cryptography.CngAlgorithmGroup? right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.CngAlgorithmGroup? left, System.Security.Cryptography.CngAlgorithmGroup? right) { throw null; }
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
        public System.Security.Cryptography.CngAlgorithmGroup? AlgorithmGroup { get { throw null; } }
        public System.Security.Cryptography.CngExportPolicies ExportPolicy { get { throw null; } }
        public Microsoft.Win32.SafeHandles.SafeNCryptKeyHandle Handle { get { throw null; } }
        public bool IsEphemeral { get { throw null; } }
        public bool IsMachineKey { get { throw null; } }
        public string? KeyName { get { throw null; } }
        public int KeySize { get { throw null; } }
        public System.Security.Cryptography.CngKeyUsages KeyUsage { get { throw null; } }
        public System.IntPtr ParentWindowHandle { get { throw null; } set { } }
        public System.Security.Cryptography.CngProvider? Provider { get { throw null; } }
        public Microsoft.Win32.SafeHandles.SafeNCryptProviderHandle ProviderHandle { get { throw null; } }
        public System.Security.Cryptography.CngUIPolicy UIPolicy { get { throw null; } }
        public string? UniqueName { get { throw null; } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Security.Cryptography.CngKey Create(System.Security.Cryptography.CngAlgorithm algorithm) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Security.Cryptography.CngKey Create(System.Security.Cryptography.CngAlgorithm algorithm, string? keyName) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Security.Cryptography.CngKey Create(System.Security.Cryptography.CngAlgorithm algorithm, string? keyName, System.Security.Cryptography.CngKeyCreationParameters? creationParameters) { throw null; }
        public void Delete() { }
        public void Dispose() { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static bool Exists(string keyName) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static bool Exists(string keyName, System.Security.Cryptography.CngProvider provider) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static bool Exists(string keyName, System.Security.Cryptography.CngProvider provider, System.Security.Cryptography.CngKeyOpenOptions options) { throw null; }
        public byte[] Export(System.Security.Cryptography.CngKeyBlobFormat format) { throw null; }
        public System.Security.Cryptography.CngProperty GetProperty(string name, System.Security.Cryptography.CngPropertyOptions options) { throw null; }
        public bool HasProperty(string name, System.Security.Cryptography.CngPropertyOptions options) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Security.Cryptography.CngKey Import(byte[] keyBlob, System.Security.Cryptography.CngKeyBlobFormat format) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Security.Cryptography.CngKey Import(byte[] keyBlob, System.Security.Cryptography.CngKeyBlobFormat format, System.Security.Cryptography.CngProvider provider) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Security.Cryptography.CngKey Open(Microsoft.Win32.SafeHandles.SafeNCryptKeyHandle keyHandle, System.Security.Cryptography.CngKeyHandleOpenOptions keyHandleOpenOptions) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Security.Cryptography.CngKey Open(string keyName) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Security.Cryptography.CngKey Open(string keyName, System.Security.Cryptography.CngProvider provider) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
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
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Security.Cryptography.CngKeyBlobFormat? other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.CngKeyBlobFormat? left, System.Security.Cryptography.CngKeyBlobFormat? right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.CngKeyBlobFormat? left, System.Security.Cryptography.CngKeyBlobFormat? right) { throw null; }
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
        public System.Security.Cryptography.CngUIPolicy? UIPolicy { get { throw null; } set { } }
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
        private int _dummyPrimitive;
        public CngProperty(string name, byte[]? value, System.Security.Cryptography.CngPropertyOptions options) { throw null; }
        public readonly string Name { get { throw null; } }
        public readonly System.Security.Cryptography.CngPropertyOptions Options { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Security.Cryptography.CngProperty other) { throw null; }
        public override int GetHashCode() { throw null; }
        public byte[]? GetValue() { throw null; }
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
        public static System.Security.Cryptography.CngProvider MicrosoftPlatformCryptoProvider { get { throw null; } }
        public static System.Security.Cryptography.CngProvider MicrosoftSmartCardKeyStorageProvider { get { throw null; } }
        public static System.Security.Cryptography.CngProvider MicrosoftSoftwareKeyStorageProvider { get { throw null; } }
        public string Provider { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Security.Cryptography.CngProvider? other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.CngProvider? left, System.Security.Cryptography.CngProvider? right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.CngProvider? left, System.Security.Cryptography.CngProvider? right) { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class CngUIPolicy
    {
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel) { }
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel, string? friendlyName) { }
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel, string? friendlyName, string? description) { }
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel, string? friendlyName, string? description, string? useContext) { }
        public CngUIPolicy(System.Security.Cryptography.CngUIProtectionLevels protectionLevel, string? friendlyName, string? description, string? useContext, string? creationTitle) { }
        public string? CreationTitle { get { throw null; } }
        public string? Description { get { throw null; } }
        public string? FriendlyName { get { throw null; } }
        public System.Security.Cryptography.CngUIProtectionLevels ProtectionLevel { get { throw null; } }
        public string? UseContext { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum CngUIProtectionLevels
    {
        None = 0,
        ProtectKey = 1,
        ForceHighProtection = 2,
    }
    public partial class CryptoConfig
    {
        public CryptoConfig() { }
        public static bool AllowOnlyFipsAlgorithms { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static void AddAlgorithm(System.Type algorithm, params string[] names) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static void AddOID(string oid, params string[] names) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static object? CreateFromName(string name) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static object? CreateFromName(string name, params object?[]? args) { throw null; }
        [System.ObsoleteAttribute("EncodeOID is obsolete. Use the ASN.1 functionality provided in System.Formats.Asn1.", DiagnosticId="SYSLIB0031", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static byte[] EncodeOID(string str) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static string? MapNameToOID(string name) { throw null; }
    }
    public static partial class CryptographicOperations
    {
        public static bool FixedTimeEquals(System.ReadOnlySpan<byte> left, System.ReadOnlySpan<byte> right) { throw null; }
        public static void ZeroMemory(System.Span<byte> buffer) { }
    }
    public partial class CryptographicUnexpectedOperationException : System.Security.Cryptography.CryptographicException
    {
        public CryptographicUnexpectedOperationException() { }
        protected CryptographicUnexpectedOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CryptographicUnexpectedOperationException(string? message) { }
        public CryptographicUnexpectedOperationException(string? message, System.Exception? inner) { }
        public CryptographicUnexpectedOperationException(string format, string? insert) { }
    }
    public partial class CryptoStream : System.IO.Stream, System.IDisposable
    {
        public CryptoStream(System.IO.Stream stream, System.Security.Cryptography.ICryptoTransform transform, System.Security.Cryptography.CryptoStreamMode mode) { }
        public CryptoStream(System.IO.Stream stream, System.Security.Cryptography.ICryptoTransform transform, System.Security.Cryptography.CryptoStreamMode mode, bool leaveOpen) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public bool HasFlushedFinalBlock { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback? callback, object? state) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback? callback, object? state) { throw null; }
        public void Clear() { }
        public override void CopyTo(System.IO.Stream destination, int bufferSize) { }
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public void FlushFinalBlock() { }
        public System.Threading.Tasks.ValueTask FlushFinalBlockAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void WriteByte(byte value) { }
    }
    public enum CryptoStreamMode
    {
        Read = 0,
        Write = 1,
    }
    [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
    public sealed partial class CspKeyContainerInfo
    {
        public CspKeyContainerInfo(System.Security.Cryptography.CspParameters parameters) { }
        public bool Accessible { get { throw null; } }
        public bool Exportable { get { throw null; } }
        public bool HardwareDevice { get { throw null; } }
        public string? KeyContainerName { get { throw null; } }
        public System.Security.Cryptography.KeyNumber KeyNumber { get { throw null; } }
        public bool MachineKeyStore { get { throw null; } }
        public bool Protected { get { throw null; } }
        public string? ProviderName { get { throw null; } }
        public int ProviderType { get { throw null; } }
        public bool RandomlyGenerated { get { throw null; } }
        public bool Removable { get { throw null; } }
        public string UniqueKeyContainerName { get { throw null; } }
    }
    [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
    public sealed partial class CspParameters
    {
        public string? KeyContainerName;
        public int KeyNumber;
        public string? ProviderName;
        public int ProviderType;
        public CspParameters() { }
        public CspParameters(int dwTypeIn) { }
        public CspParameters(int dwTypeIn, string? strProviderNameIn) { }
        public CspParameters(int dwTypeIn, string? strProviderNameIn, string? strContainerNameIn) { }
        public System.Security.Cryptography.CspProviderFlags Flags { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        public System.Security.SecureString? KeyPassword { get { throw null; } set { } }
        public System.IntPtr ParentWindowHandle { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum CspProviderFlags
    {
        NoFlags = 0,
        UseMachineKeyStore = 1,
        UseDefaultKeyContainer = 2,
        UseNonExportableKey = 4,
        UseExistingKey = 8,
        UseArchivableKey = 16,
        UseUserProtectedKey = 32,
        NoPrompt = 64,
        CreateEphemeralKey = 128,
    }
    public abstract partial class DeriveBytes : System.IDisposable
    {
        protected DeriveBytes() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract byte[] GetBytes(int cb);
        public abstract void Reset();
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public abstract partial class DES : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected DES() { }
        public override byte[] Key { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static new System.Security.Cryptography.DES Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.DES? Create(string algName) { throw null; }
        public static bool IsSemiWeakKey(byte[] rgbKey) { throw null; }
        public static bool IsWeakKey(byte[] rgbKey) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class DESCryptoServiceProvider : System.Security.Cryptography.DES
    {
        public DESCryptoServiceProvider() { }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        public override void GenerateIV() { }
        public override void GenerateKey() { }
    }
    public abstract partial class DSA : System.Security.Cryptography.AsymmetricAlgorithm
    {
        protected DSA() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static new System.Security.Cryptography.DSA Create() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.Security.Cryptography.DSA Create(int keySizeInBits) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.Security.Cryptography.DSA Create(System.Security.Cryptography.DSAParameters parameters) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.DSA? Create(string algName) { throw null; }
        public abstract byte[] CreateSignature(byte[] rgbHash);
        public byte[] CreateSignature(byte[] rgbHash, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual byte[] CreateSignatureCore(System.ReadOnlySpan<byte> hash, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public abstract System.Security.Cryptography.DSAParameters ExportParameters(bool includePrivateParameters);
        public override void FromXmlString(string xmlString) { }
        public int GetMaxSignatureSize(System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected virtual byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportFromEncryptedPem(System.ReadOnlySpan<char> input, System.ReadOnlySpan<byte> passwordBytes) { }
        public override void ImportFromEncryptedPem(System.ReadOnlySpan<char> input, System.ReadOnlySpan<char> password) { }
        public override void ImportFromPem(System.ReadOnlySpan<char> input) { }
        public abstract void ImportParameters(System.Security.Cryptography.DSAParameters parameters);
        public override void ImportPkcs8PrivateKey(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportSubjectPublicKeyInfo(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public virtual byte[] SignData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public byte[] SignData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public byte[] SignData(byte[] data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public byte[] SignData(byte[] data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public virtual byte[] SignData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public byte[] SignData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual byte[] SignDataCore(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual byte[] SignDataCore(System.ReadOnlySpan<byte> data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public override string ToXmlString(bool includePrivateParameters) { throw null; }
        public virtual bool TryCreateSignature(System.ReadOnlySpan<byte> hash, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TryCreateSignature(System.ReadOnlySpan<byte> hash, System.Span<byte> destination, System.Security.Cryptography.DSASignatureFormat signatureFormat, out int bytesWritten) { throw null; }
        protected virtual bool TryCreateSignatureCore(System.ReadOnlySpan<byte> hash, System.Span<byte> destination, System.Security.Cryptography.DSASignatureFormat signatureFormat, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportPkcs8PrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportSubjectPublicKeyInfo(System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected virtual bool TryHashData(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, out int bytesWritten) { throw null; }
        public virtual bool TrySignData(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, out int bytesWritten) { throw null; }
        public bool TrySignData(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat, out int bytesWritten) { throw null; }
        protected virtual bool TrySignDataCore(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat, out int bytesWritten) { throw null; }
        public bool VerifyData(byte[] data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public bool VerifyData(byte[] data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public bool VerifyData(byte[] data, int offset, int count, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public virtual bool VerifyData(System.IO.Stream data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public bool VerifyData(System.IO.Stream data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public virtual bool VerifyData(System.ReadOnlySpan<byte> data, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public bool VerifyData(System.ReadOnlySpan<byte> data, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual bool VerifyDataCore(System.IO.Stream data, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual bool VerifyDataCore(System.ReadOnlySpan<byte> data, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public abstract bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);
        public bool VerifySignature(byte[] rgbHash, byte[] rgbSignature, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public virtual bool VerifySignature(System.ReadOnlySpan<byte> hash, System.ReadOnlySpan<byte> signature) { throw null; }
        public bool VerifySignature(System.ReadOnlySpan<byte> hash, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual bool VerifySignatureCore(System.ReadOnlySpan<byte> hash, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
    }
    public sealed partial class DSACng : System.Security.Cryptography.DSA
    {
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public DSACng() { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public DSACng(int keySize) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public DSACng(System.Security.Cryptography.CngKey key) { }
        public System.Security.Cryptography.CngKey Key { get { throw null; } }
        public override string? KeyExchangeAlgorithm { get { throw null; } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public override string SignatureAlgorithm { get { throw null; } }
        public override byte[] CreateSignature(byte[] rgbHash) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override byte[] ExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public override byte[] ExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public override System.Security.Cryptography.DSAParameters ExportParameters(bool includePrivateParameters) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.DSAParameters parameters) { }
        protected override bool TryCreateSignatureCore(System.ReadOnlySpan<byte> hash, System.Span<byte> destination, System.Security.Cryptography.DSASignatureFormat signatureFormat, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportPkcs8PrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) { throw null; }
        protected override bool VerifySignatureCore(System.ReadOnlySpan<byte> hash, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
    }
    public sealed partial class DSACryptoServiceProvider : System.Security.Cryptography.DSA, System.Security.Cryptography.ICspAsymmetricAlgorithm
    {
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public DSACryptoServiceProvider() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public DSACryptoServiceProvider(int dwKeySize) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public DSACryptoServiceProvider(int dwKeySize, System.Security.Cryptography.CspParameters? parameters) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public DSACryptoServiceProvider(System.Security.Cryptography.CspParameters? parameters) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public System.Security.Cryptography.CspKeyContainerInfo CspKeyContainerInfo { get { throw null; } }
        public override string? KeyExchangeAlgorithm { get { throw null; } }
        public override int KeySize { get { throw null; } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public bool PersistKeyInCsp { get { throw null; } set { } }
        public bool PublicOnly { get { throw null; } }
        public override string SignatureAlgorithm { get { throw null; } }
        public static bool UseMachineKeyStore { get { throw null; } set { } }
        public override byte[] CreateSignature(byte[] rgbHash) { throw null; }
        protected override void Dispose(bool disposing) { }
        public byte[] ExportCspBlob(bool includePrivateParameters) { throw null; }
        public override System.Security.Cryptography.DSAParameters ExportParameters(bool includePrivateParameters) { throw null; }
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public void ImportCspBlob(byte[] keyBlob) { }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.DSAParameters parameters) { }
        public byte[] SignData(byte[] buffer) { throw null; }
        public byte[] SignData(byte[] buffer, int offset, int count) { throw null; }
        public byte[] SignData(System.IO.Stream inputStream) { throw null; }
        public byte[] SignHash(byte[] rgbHash, string? str) { throw null; }
        public bool VerifyData(byte[] rgbData, byte[] rgbSignature) { throw null; }
        public bool VerifyHash(byte[] rgbHash, string? str, byte[] rgbSignature) { throw null; }
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) { throw null; }
    }
    public sealed partial class DSAOpenSsl : System.Security.Cryptography.DSA
    {
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public DSAOpenSsl() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public DSAOpenSsl(int keySize) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public DSAOpenSsl(System.IntPtr handle) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public DSAOpenSsl(System.Security.Cryptography.DSAParameters parameters) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public DSAOpenSsl(System.Security.Cryptography.SafeEvpPKeyHandle pkeyHandle) { }
        public override byte[] CreateSignature(byte[] rgbHash) { throw null; }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateKeyHandle() { throw null; }
        public override System.Security.Cryptography.DSAParameters ExportParameters(bool includePrivateParameters) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.DSAParameters parameters) { }
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) { throw null; }
    }
    public partial struct DSAParameters
    {
        public int Counter;
        public byte[]? G;
        public byte[]? J;
        public byte[]? P;
        public byte[]? Q;
        public byte[]? Seed;
        public byte[]? X;
        public byte[]? Y;
    }
    public partial class DSASignatureDeformatter : System.Security.Cryptography.AsymmetricSignatureDeformatter
    {
        public DSASignatureDeformatter() { }
        public DSASignatureDeformatter(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public override void SetHashAlgorithm(string strName) { }
        public override void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) { throw null; }
    }
    public enum DSASignatureFormat
    {
        IeeeP1363FixedFieldConcatenation = 0,
        Rfc3279DerSequence = 1,
    }
    public partial class DSASignatureFormatter : System.Security.Cryptography.AsymmetricSignatureFormatter
    {
        public DSASignatureFormatter() { }
        public DSASignatureFormatter(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public override byte[] CreateSignature(byte[] rgbHash) { throw null; }
        public override void SetHashAlgorithm(string strName) { }
        public override void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key) { }
    }
    public abstract partial class ECAlgorithm : System.Security.Cryptography.AsymmetricAlgorithm
    {
        protected ECAlgorithm() { }
        public virtual byte[] ExportECPrivateKey() { throw null; }
        public string ExportECPrivateKeyPem() { throw null; }
        public virtual System.Security.Cryptography.ECParameters ExportExplicitParameters(bool includePrivateParameters) { throw null; }
        public virtual System.Security.Cryptography.ECParameters ExportParameters(bool includePrivateParameters) { throw null; }
        public virtual void GenerateKey(System.Security.Cryptography.ECCurve curve) { }
        public virtual void ImportECPrivateKey(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportFromEncryptedPem(System.ReadOnlySpan<char> input, System.ReadOnlySpan<byte> passwordBytes) { }
        public override void ImportFromEncryptedPem(System.ReadOnlySpan<char> input, System.ReadOnlySpan<char> password) { }
        public override void ImportFromPem(System.ReadOnlySpan<char> input) { }
        public virtual void ImportParameters(System.Security.Cryptography.ECParameters parameters) { }
        public override void ImportPkcs8PrivateKey(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportSubjectPublicKeyInfo(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public virtual bool TryExportECPrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TryExportECPrivateKeyPem(System.Span<char> destination, out int charsWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportPkcs8PrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportSubjectPublicKeyInfo(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public partial struct ECCurve
    {
        private object _dummy;
        private int _dummyPrimitive;
        public byte[]? A;
        public byte[]? B;
        public byte[]? Cofactor;
        public System.Security.Cryptography.ECCurve.ECCurveType CurveType;
        public System.Security.Cryptography.ECPoint G;
        public System.Security.Cryptography.HashAlgorithmName? Hash;
        public byte[]? Order;
        public byte[]? Polynomial;
        public byte[]? Prime;
        public byte[]? Seed;
        public bool IsCharacteristic2 { get { throw null; } }
        public bool IsExplicit { get { throw null; } }
        public bool IsNamed { get { throw null; } }
        public bool IsPrime { get { throw null; } }
        public System.Security.Cryptography.Oid Oid { get { throw null; } }
        public static System.Security.Cryptography.ECCurve CreateFromFriendlyName(string oidFriendlyName) { throw null; }
        public static System.Security.Cryptography.ECCurve CreateFromOid(System.Security.Cryptography.Oid curveOid) { throw null; }
        public static System.Security.Cryptography.ECCurve CreateFromValue(string oidValue) { throw null; }
        public void Validate() { }
        public enum ECCurveType
        {
            Implicit = 0,
            PrimeShortWeierstrass = 1,
            PrimeTwistedEdwards = 2,
            PrimeMontgomery = 3,
            Characteristic2 = 4,
            Named = 5,
        }
        public static partial class NamedCurves
        {
            public static System.Security.Cryptography.ECCurve brainpoolP160r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP160t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP192r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP192t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP224r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP224t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP256r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP256t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP320r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP320t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP384r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP384t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP512r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP512t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve nistP256 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve nistP384 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve nistP521 { get { throw null; } }
        }
    }
    public abstract partial class ECDiffieHellman : System.Security.Cryptography.ECAlgorithm
    {
        protected ECDiffieHellman() { }
        public override string KeyExchangeAlgorithm { get { throw null; } }
        public abstract System.Security.Cryptography.ECDiffieHellmanPublicKey PublicKey { get; }
        public override string? SignatureAlgorithm { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static new System.Security.Cryptography.ECDiffieHellman Create() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.ECDiffieHellman Create(System.Security.Cryptography.ECCurve curve) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.ECDiffieHellman Create(System.Security.Cryptography.ECParameters parameters) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.ECDiffieHellman? Create(string algorithm) { throw null; }
        public byte[] DeriveKeyFromHash(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public virtual byte[] DeriveKeyFromHash(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[]? secretPrepend, byte[]? secretAppend) { throw null; }
        public byte[] DeriveKeyFromHmac(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[]? hmacKey) { throw null; }
        public virtual byte[] DeriveKeyFromHmac(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[]? hmacKey, byte[]? secretPrepend, byte[]? secretAppend) { throw null; }
        public virtual byte[] DeriveKeyMaterial(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey) { throw null; }
        public virtual byte[] DeriveKeyTls(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed) { throw null; }
        public override void FromXmlString(string xmlString) { }
        public override string ToXmlString(bool includePrivateParameters) { throw null; }
    }
    public sealed partial class ECDiffieHellmanCng : System.Security.Cryptography.ECDiffieHellman
    {
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public ECDiffieHellmanCng() { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public ECDiffieHellmanCng(int keySize) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public ECDiffieHellmanCng(System.Security.Cryptography.CngKey key) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public ECDiffieHellmanCng(System.Security.Cryptography.ECCurve curve) { }
        public System.Security.Cryptography.CngAlgorithm HashAlgorithm { get { throw null; } set { } }
        public byte[]? HmacKey { get { throw null; } set { } }
        public System.Security.Cryptography.CngKey Key { get { throw null; } }
        public System.Security.Cryptography.ECDiffieHellmanKeyDerivationFunction KeyDerivationFunction { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        public byte[]? Label { get { throw null; } set { } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public override System.Security.Cryptography.ECDiffieHellmanPublicKey PublicKey { get { throw null; } }
        public byte[]? SecretAppend { get { throw null; } set { } }
        public byte[]? SecretPrepend { get { throw null; } set { } }
        public byte[]? Seed { get { throw null; } set { } }
        public bool UseSecretAgreementAsHmacKey { get { throw null; } }
        public override byte[] DeriveKeyFromHash(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[]? secretPrepend, byte[]? secretAppend) { throw null; }
        public override byte[] DeriveKeyFromHmac(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[]? hmacKey, byte[]? secretPrepend, byte[]? secretAppend) { throw null; }
        public byte[] DeriveKeyMaterial(System.Security.Cryptography.CngKey otherPartyPublicKey) { throw null; }
        public override byte[] DeriveKeyMaterial(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey) { throw null; }
        public override byte[] DeriveKeyTls(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed) { throw null; }
        public Microsoft.Win32.SafeHandles.SafeNCryptSecretHandle DeriveSecretAgreementHandle(System.Security.Cryptography.CngKey otherPartyPublicKey) { throw null; }
        public Microsoft.Win32.SafeHandles.SafeNCryptSecretHandle DeriveSecretAgreementHandle(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override byte[] ExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public override byte[] ExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public override System.Security.Cryptography.ECParameters ExportExplicitParameters(bool includePrivateParameters) { throw null; }
        public override System.Security.Cryptography.ECParameters ExportParameters(bool includePrivateParameters) { throw null; }
        [System.ObsoleteAttribute("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId="SYSLIB0042", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public void FromXmlString(string xml, System.Security.Cryptography.ECKeyXmlFormat format) { }
        public override void GenerateKey(System.Security.Cryptography.ECCurve curve) { }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.ECParameters parameters) { }
        public override void ImportPkcs8PrivateKey(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        [System.ObsoleteAttribute("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId="SYSLIB0042", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public string ToXmlString(System.Security.Cryptography.ECKeyXmlFormat format) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportPkcs8PrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public sealed partial class ECDiffieHellmanCngPublicKey : System.Security.Cryptography.ECDiffieHellmanPublicKey
    {
        internal ECDiffieHellmanCngPublicKey() { }
        public System.Security.Cryptography.CngKeyBlobFormat BlobFormat { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override System.Security.Cryptography.ECParameters ExportExplicitParameters() { throw null; }
        public override System.Security.Cryptography.ECParameters ExportParameters() { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Security.Cryptography.ECDiffieHellmanPublicKey FromByteArray(byte[] publicKeyBlob, System.Security.Cryptography.CngKeyBlobFormat format) { throw null; }
        [System.ObsoleteAttribute("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId="SYSLIB0042", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static System.Security.Cryptography.ECDiffieHellmanCngPublicKey FromXmlString(string xml) { throw null; }
        public System.Security.Cryptography.CngKey Import() { throw null; }
        [System.ObsoleteAttribute("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId="SYSLIB0042", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public override string ToXmlString() { throw null; }
    }
    public enum ECDiffieHellmanKeyDerivationFunction
    {
        Hash = 0,
        Hmac = 1,
        Tls = 2,
    }
    public sealed partial class ECDiffieHellmanOpenSsl : System.Security.Cryptography.ECDiffieHellman
    {
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public ECDiffieHellmanOpenSsl() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public ECDiffieHellmanOpenSsl(int keySize) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public ECDiffieHellmanOpenSsl(System.IntPtr handle) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public ECDiffieHellmanOpenSsl(System.Security.Cryptography.ECCurve curve) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public ECDiffieHellmanOpenSsl(System.Security.Cryptography.SafeEvpPKeyHandle pkeyHandle) { }
        public override System.Security.Cryptography.ECDiffieHellmanPublicKey PublicKey { get { throw null; } }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateKeyHandle() { throw null; }
        public override System.Security.Cryptography.ECParameters ExportParameters(bool includePrivateParameters) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.ECParameters parameters) { }
    }
    public abstract partial class ECDiffieHellmanPublicKey : System.IDisposable
    {
        protected ECDiffieHellmanPublicKey() { }
        [System.ObsoleteAttribute("ECDiffieHellmanPublicKey.ToByteArray() and the associated constructor do not have a consistent and interoperable implementation on all platforms. Use ECDiffieHellmanPublicKey.ExportSubjectPublicKeyInfo() instead.", DiagnosticId="SYSLIB0043", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        protected ECDiffieHellmanPublicKey(byte[] keyBlob) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual System.Security.Cryptography.ECParameters ExportExplicitParameters() { throw null; }
        public virtual System.Security.Cryptography.ECParameters ExportParameters() { throw null; }
        public virtual byte[] ExportSubjectPublicKeyInfo() { throw null; }
        [System.ObsoleteAttribute("ECDiffieHellmanPublicKey.ToByteArray() and the associated constructor do not have a consistent and interoperable implementation on all platforms. Use ECDiffieHellmanPublicKey.ExportSubjectPublicKeyInfo() instead.", DiagnosticId="SYSLIB0043", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public virtual byte[] ToByteArray() { throw null; }
        [System.ObsoleteAttribute("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId="SYSLIB0042", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public virtual string ToXmlString() { throw null; }
        public virtual bool TryExportSubjectPublicKeyInfo(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public abstract partial class ECDsa : System.Security.Cryptography.ECAlgorithm
    {
        protected ECDsa() { }
        public override string? KeyExchangeAlgorithm { get { throw null; } }
        public override string SignatureAlgorithm { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static new System.Security.Cryptography.ECDsa Create() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.ECDsa Create(System.Security.Cryptography.ECCurve curve) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.ECDsa Create(System.Security.Cryptography.ECParameters parameters) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.ECDsa? Create(string algorithm) { throw null; }
        public override void FromXmlString(string xmlString) { }
        public int GetMaxSignatureSize(System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected virtual byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public virtual byte[] SignData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public byte[] SignData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public virtual byte[] SignData(byte[] data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public byte[] SignData(byte[] data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public virtual byte[] SignData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public byte[] SignData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual byte[] SignDataCore(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual byte[] SignDataCore(System.ReadOnlySpan<byte> data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public abstract byte[] SignHash(byte[] hash);
        public byte[] SignHash(byte[] hash, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual byte[] SignHashCore(System.ReadOnlySpan<byte> hash, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public override string ToXmlString(bool includePrivateParameters) { throw null; }
        protected virtual bool TryHashData(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, out int bytesWritten) { throw null; }
        public virtual bool TrySignData(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, out int bytesWritten) { throw null; }
        public bool TrySignData(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat, out int bytesWritten) { throw null; }
        protected virtual bool TrySignDataCore(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat, out int bytesWritten) { throw null; }
        public virtual bool TrySignHash(System.ReadOnlySpan<byte> hash, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TrySignHash(System.ReadOnlySpan<byte> hash, System.Span<byte> destination, System.Security.Cryptography.DSASignatureFormat signatureFormat, out int bytesWritten) { throw null; }
        protected virtual bool TrySignHashCore(System.ReadOnlySpan<byte> hash, System.Span<byte> destination, System.Security.Cryptography.DSASignatureFormat signatureFormat, out int bytesWritten) { throw null; }
        public bool VerifyData(byte[] data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public bool VerifyData(byte[] data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public bool VerifyData(byte[] data, int offset, int count, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public bool VerifyData(System.IO.Stream data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public bool VerifyData(System.IO.Stream data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public virtual bool VerifyData(System.ReadOnlySpan<byte> data, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public bool VerifyData(System.ReadOnlySpan<byte> data, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual bool VerifyDataCore(System.IO.Stream data, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual bool VerifyDataCore(System.ReadOnlySpan<byte> data, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public abstract bool VerifyHash(byte[] hash, byte[] signature);
        public bool VerifyHash(byte[] hash, byte[] signature, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        public virtual bool VerifyHash(System.ReadOnlySpan<byte> hash, System.ReadOnlySpan<byte> signature) { throw null; }
        public bool VerifyHash(System.ReadOnlySpan<byte> hash, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual bool VerifyHashCore(System.ReadOnlySpan<byte> hash, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
    }
    public sealed partial class ECDsaCng : System.Security.Cryptography.ECDsa
    {
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public ECDsaCng() { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public ECDsaCng(int keySize) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public ECDsaCng(System.Security.Cryptography.CngKey key) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public ECDsaCng(System.Security.Cryptography.ECCurve curve) { }
        public System.Security.Cryptography.CngAlgorithm HashAlgorithm { get { throw null; } set { } }
        public System.Security.Cryptography.CngKey Key { get { throw null; } }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override byte[] ExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public override byte[] ExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public override System.Security.Cryptography.ECParameters ExportExplicitParameters(bool includePrivateParameters) { throw null; }
        public override System.Security.Cryptography.ECParameters ExportParameters(bool includePrivateParameters) { throw null; }
        [System.ObsoleteAttribute("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId="SYSLIB0042", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public void FromXmlString(string xml, System.Security.Cryptography.ECKeyXmlFormat format) { }
        public override void GenerateKey(System.Security.Cryptography.ECCurve curve) { }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.ECParameters parameters) { }
        public override void ImportPkcs8PrivateKey(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public byte[] SignData(byte[] data) { throw null; }
        public byte[] SignData(byte[] data, int offset, int count) { throw null; }
        public byte[] SignData(System.IO.Stream data) { throw null; }
        public override byte[] SignHash(byte[] hash) { throw null; }
        [System.ObsoleteAttribute("ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys.", DiagnosticId="SYSLIB0042", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public string ToXmlString(System.Security.Cryptography.ECKeyXmlFormat format) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportPkcs8PrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TrySignHash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected override bool TrySignHashCore(System.ReadOnlySpan<byte> hash, System.Span<byte> destination, System.Security.Cryptography.DSASignatureFormat signatureFormat, out int bytesWritten) { throw null; }
        public bool VerifyData(byte[] data, byte[] signature) { throw null; }
        public bool VerifyData(byte[] data, int offset, int count, byte[] signature) { throw null; }
        public bool VerifyData(System.IO.Stream data, byte[] signature) { throw null; }
        public override bool VerifyHash(byte[] hash, byte[] signature) { throw null; }
        public override bool VerifyHash(System.ReadOnlySpan<byte> hash, System.ReadOnlySpan<byte> signature) { throw null; }
        protected override bool VerifyHashCore(System.ReadOnlySpan<byte> hash, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
    }
    public sealed partial class ECDsaOpenSsl : System.Security.Cryptography.ECDsa
    {
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public ECDsaOpenSsl() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public ECDsaOpenSsl(int keySize) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public ECDsaOpenSsl(System.IntPtr handle) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public ECDsaOpenSsl(System.Security.Cryptography.ECCurve curve) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public ECDsaOpenSsl(System.Security.Cryptography.SafeEvpPKeyHandle pkeyHandle) { }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateKeyHandle() { throw null; }
        public override byte[] SignHash(byte[] hash) { throw null; }
        public override bool VerifyHash(byte[] hash, byte[] signature) { throw null; }
    }
    public enum ECKeyXmlFormat
    {
        Rfc4050 = 0,
    }
    public partial struct ECParameters
    {
        public System.Security.Cryptography.ECCurve Curve;
        public byte[]? D;
        public System.Security.Cryptography.ECPoint Q;
        public void Validate() { }
    }
    public partial struct ECPoint
    {
        public byte[]? X;
        public byte[]? Y;
    }
    public partial class FromBase64Transform : System.IDisposable, System.Security.Cryptography.ICryptoTransform
    {
        public FromBase64Transform() { }
        public FromBase64Transform(System.Security.Cryptography.FromBase64TransformMode whitespaces) { }
        public virtual bool CanReuseTransform { get { throw null; } }
        public bool CanTransformMultipleBlocks { get { throw null; } }
        public int InputBlockSize { get { throw null; } }
        public int OutputBlockSize { get { throw null; } }
        public void Clear() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~FromBase64Transform() { }
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset) { throw null; }
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) { throw null; }
    }
    public enum FromBase64TransformMode
    {
        IgnoreWhiteSpaces = 0,
        DoNotIgnoreWhiteSpaces = 1,
    }
    public abstract partial class HashAlgorithm : System.IDisposable, System.Security.Cryptography.ICryptoTransform
    {
        protected int HashSizeValue;
        protected internal byte[]? HashValue;
        protected int State;
        protected HashAlgorithm() { }
        public virtual bool CanReuseTransform { get { throw null; } }
        public virtual bool CanTransformMultipleBlocks { get { throw null; } }
        public virtual byte[]? Hash { get { throw null; } }
        public virtual int HashSize { get { throw null; } }
        public virtual int InputBlockSize { get { throw null; } }
        public virtual int OutputBlockSize { get { throw null; } }
        public void Clear() { }
        public byte[] ComputeHash(byte[] buffer) { throw null; }
        public byte[] ComputeHash(byte[] buffer, int offset, int count) { throw null; }
        public byte[] ComputeHash(System.IO.Stream inputStream) { throw null; }
        public System.Threading.Tasks.Task<byte[]> ComputeHashAsync(System.IO.Stream inputStream, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.ObsoleteAttribute("The default implementation of this cryptography algorithm is not supported.", DiagnosticId="SYSLIB0007", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static System.Security.Cryptography.HashAlgorithm Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static System.Security.Cryptography.HashAlgorithm? Create(string hashName) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected abstract void HashCore(byte[] array, int ibStart, int cbSize);
        protected virtual void HashCore(System.ReadOnlySpan<byte> source) { }
        protected abstract byte[] HashFinal();
        public abstract void Initialize();
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[]? outputBuffer, int outputOffset) { throw null; }
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) { throw null; }
        public bool TryComputeHash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected virtual bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public readonly partial struct HashAlgorithmName : System.IEquatable<System.Security.Cryptography.HashAlgorithmName>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public HashAlgorithmName(string? name) { throw null; }
        public static System.Security.Cryptography.HashAlgorithmName MD5 { get { throw null; } }
        public string? Name { get { throw null; } }
        public static System.Security.Cryptography.HashAlgorithmName SHA1 { get { throw null; } }
        public static System.Security.Cryptography.HashAlgorithmName SHA256 { get { throw null; } }
        public static System.Security.Cryptography.HashAlgorithmName SHA384 { get { throw null; } }
        public static System.Security.Cryptography.HashAlgorithmName SHA512 { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Security.Cryptography.HashAlgorithmName other) { throw null; }
        public static System.Security.Cryptography.HashAlgorithmName FromOid(string oidValue) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.HashAlgorithmName left, System.Security.Cryptography.HashAlgorithmName right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.HashAlgorithmName left, System.Security.Cryptography.HashAlgorithmName right) { throw null; }
        public override string ToString() { throw null; }
        public static bool TryFromOid(string oidValue, out System.Security.Cryptography.HashAlgorithmName value) { throw null; }
    }
    public static partial class HKDF
    {
        public static byte[] DeriveKey(System.Security.Cryptography.HashAlgorithmName hashAlgorithmName, byte[] ikm, int outputLength, byte[]? salt = null, byte[]? info = null) { throw null; }
        public static void DeriveKey(System.Security.Cryptography.HashAlgorithmName hashAlgorithmName, System.ReadOnlySpan<byte> ikm, System.Span<byte> output, System.ReadOnlySpan<byte> salt, System.ReadOnlySpan<byte> info) { }
        public static byte[] Expand(System.Security.Cryptography.HashAlgorithmName hashAlgorithmName, byte[] prk, int outputLength, byte[]? info = null) { throw null; }
        public static void Expand(System.Security.Cryptography.HashAlgorithmName hashAlgorithmName, System.ReadOnlySpan<byte> prk, System.Span<byte> output, System.ReadOnlySpan<byte> info) { }
        public static byte[] Extract(System.Security.Cryptography.HashAlgorithmName hashAlgorithmName, byte[] ikm, byte[]? salt = null) { throw null; }
        public static int Extract(System.Security.Cryptography.HashAlgorithmName hashAlgorithmName, System.ReadOnlySpan<byte> ikm, System.ReadOnlySpan<byte> salt, System.Span<byte> prk) { throw null; }
    }
    public abstract partial class HMAC : System.Security.Cryptography.KeyedHashAlgorithm
    {
        protected HMAC() { }
        protected int BlockSizeValue { get { throw null; } set { } }
        public string HashName { get { throw null; } set { } }
        public override byte[] Key { get { throw null; } set { } }
        [System.ObsoleteAttribute("The default implementation of this cryptography algorithm is not supported.", DiagnosticId="SYSLIB0007", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.HMAC Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.HMAC? Create(string algorithmName) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public partial class HMACMD5 : System.Security.Cryptography.HMAC
    {
        public const int HashSizeInBits = 128;
        public const int HashSizeInBytes = 16;
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public HMACMD5() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public HMACMD5(byte[] key) { }
        public override byte[] Key { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static byte[] HashData(byte[] key, byte[] source) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static byte[] HashData(byte[] key, System.IO.Stream source) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.IO.Stream source) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static int HashData(System.ReadOnlySpan<byte> key, System.IO.Stream source, System.Span<byte> destination) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static int HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(byte[] key, System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Threading.Tasks.ValueTask<int> HashDataAsync(System.ReadOnlyMemory<byte> key, System.IO.Stream source, System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(System.ReadOnlyMemory<byte> key, System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static bool TryHashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public partial class HMACSHA1 : System.Security.Cryptography.HMAC
    {
        public const int HashSizeInBits = 160;
        public const int HashSizeInBytes = 20;
        public HMACSHA1() { }
        public HMACSHA1(byte[] key) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("HMACSHA1 always uses the algorithm implementation provided by the platform. Use a constructor without the useManagedSha1 parameter.", DiagnosticId="SYSLIB0030", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public HMACSHA1(byte[] key, bool useManagedSha1) { }
        public override byte[] Key { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        public static byte[] HashData(byte[] key, byte[] source) { throw null; }
        public static byte[] HashData(byte[] key, System.IO.Stream source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.IO.Stream source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.IO.Stream source, System.Span<byte> destination) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(byte[] key, System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<int> HashDataAsync(System.ReadOnlyMemory<byte> key, System.IO.Stream source, System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(System.ReadOnlyMemory<byte> key, System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        public static bool TryHashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public partial class HMACSHA256 : System.Security.Cryptography.HMAC
    {
        public const int HashSizeInBits = 256;
        public const int HashSizeInBytes = 32;
        public HMACSHA256() { }
        public HMACSHA256(byte[] key) { }
        public override byte[] Key { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        public static byte[] HashData(byte[] key, byte[] source) { throw null; }
        public static byte[] HashData(byte[] key, System.IO.Stream source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.IO.Stream source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.IO.Stream source, System.Span<byte> destination) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(byte[] key, System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<int> HashDataAsync(System.ReadOnlyMemory<byte> key, System.IO.Stream source, System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(System.ReadOnlyMemory<byte> key, System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        public static bool TryHashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public partial class HMACSHA384 : System.Security.Cryptography.HMAC
    {
        public const int HashSizeInBits = 384;
        public const int HashSizeInBytes = 48;
        public HMACSHA384() { }
        public HMACSHA384(byte[] key) { }
        public override byte[] Key { get { throw null; } set { } }
        [System.ObsoleteAttribute("ProduceLegacyHmacValues is obsolete. Producing legacy HMAC values is not supported.", DiagnosticId="SYSLIB0029", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public bool ProduceLegacyHmacValues { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        public static byte[] HashData(byte[] key, byte[] source) { throw null; }
        public static byte[] HashData(byte[] key, System.IO.Stream source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.IO.Stream source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.IO.Stream source, System.Span<byte> destination) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(byte[] key, System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<int> HashDataAsync(System.ReadOnlyMemory<byte> key, System.IO.Stream source, System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(System.ReadOnlyMemory<byte> key, System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        public static bool TryHashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public partial class HMACSHA512 : System.Security.Cryptography.HMAC
    {
        public const int HashSizeInBits = 512;
        public const int HashSizeInBytes = 64;
        public HMACSHA512() { }
        public HMACSHA512(byte[] key) { }
        public override byte[] Key { get { throw null; } set { } }
        [System.ObsoleteAttribute("ProduceLegacyHmacValues is obsolete. Producing legacy HMAC values is not supported.", DiagnosticId="SYSLIB0029", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public bool ProduceLegacyHmacValues { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        public static byte[] HashData(byte[] key, byte[] source) { throw null; }
        public static byte[] HashData(byte[] key, System.IO.Stream source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.IO.Stream source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.IO.Stream source, System.Span<byte> destination) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(byte[] key, System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<int> HashDataAsync(System.ReadOnlyMemory<byte> key, System.IO.Stream source, System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(System.ReadOnlyMemory<byte> key, System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        public static bool TryHashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public partial interface ICryptoTransform : System.IDisposable
    {
        bool CanReuseTransform { get; }
        bool CanTransformMultipleBlocks { get; }
        int InputBlockSize { get; }
        int OutputBlockSize { get; }
        int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset);
        byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);
    }
    public partial interface ICspAsymmetricAlgorithm
    {
        System.Security.Cryptography.CspKeyContainerInfo CspKeyContainerInfo { get; }
        byte[] ExportCspBlob(bool includePrivateParameters);
        void ImportCspBlob(byte[] rawData);
    }
    public sealed partial class IncrementalHash : System.IDisposable
    {
        internal IncrementalHash() { }
        public System.Security.Cryptography.HashAlgorithmName AlgorithmName { get { throw null; } }
        public int HashLengthInBytes { get { throw null; } }
        public void AppendData(byte[] data) { }
        public void AppendData(byte[] data, int offset, int count) { }
        public void AppendData(System.ReadOnlySpan<byte> data) { }
        public static System.Security.Cryptography.IncrementalHash CreateHash(System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public static System.Security.Cryptography.IncrementalHash CreateHMAC(System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[] key) { throw null; }
        public static System.Security.Cryptography.IncrementalHash CreateHMAC(System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.ReadOnlySpan<byte> key) { throw null; }
        public void Dispose() { }
        public byte[] GetCurrentHash() { throw null; }
        public int GetCurrentHash(System.Span<byte> destination) { throw null; }
        public byte[] GetHashAndReset() { throw null; }
        public int GetHashAndReset(System.Span<byte> destination) { throw null; }
        public bool TryGetCurrentHash(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TryGetHashAndReset(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public abstract partial class KeyedHashAlgorithm : System.Security.Cryptography.HashAlgorithm
    {
        protected byte[] KeyValue;
        protected KeyedHashAlgorithm() { }
        public virtual byte[] Key { get { throw null; } set { } }
        [System.ObsoleteAttribute("The default implementation of this cryptography algorithm is not supported.", DiagnosticId="SYSLIB0007", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.KeyedHashAlgorithm Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.KeyedHashAlgorithm? Create(string algName) { throw null; }
        protected override void Dispose(bool disposing) { }
    }
    public enum KeyNumber
    {
        Exchange = 1,
        Signature = 2,
    }
    public sealed partial class KeySizes
    {
        public KeySizes(int minSize, int maxSize, int skipSize) { }
        public int MaxSize { get { throw null; } }
        public int MinSize { get { throw null; } }
        public int SkipSize { get { throw null; } }
    }
    public abstract partial class MaskGenerationMethod
    {
        protected MaskGenerationMethod() { }
        public abstract byte[] GenerateMask(byte[] rgbSeed, int cbReturn);
    }
    public abstract partial class MD5 : System.Security.Cryptography.HashAlgorithm
    {
        public const int HashSizeInBits = 128;
        public const int HashSizeInBytes = 16;
        protected MD5() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static new System.Security.Cryptography.MD5 Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.MD5? Create(string algName) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static byte[] HashData(byte[] source) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static byte[] HashData(System.IO.Stream source) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static int HashData(System.IO.Stream source, System.Span<byte> destination) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static byte[] HashData(System.ReadOnlySpan<byte> source) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static int HashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Threading.Tasks.ValueTask<int> HashDataAsync(System.IO.Stream source, System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static bool TryHashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class MD5CryptoServiceProvider : System.Security.Cryptography.MD5
    {
        public MD5CryptoServiceProvider() { }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] array, int ibStart, int cbSize) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public sealed partial class Oid
    {
        public Oid() { }
        public Oid(System.Security.Cryptography.Oid oid) { }
        public Oid(string oid) { }
        public Oid(string? value, string? friendlyName) { }
        public string? FriendlyName { get { throw null; } set { } }
        public string? Value { get { throw null; } set { } }
        public static System.Security.Cryptography.Oid FromFriendlyName(string friendlyName, System.Security.Cryptography.OidGroup group) { throw null; }
        public static System.Security.Cryptography.Oid FromOidValue(string oidValue, System.Security.Cryptography.OidGroup group) { throw null; }
    }
    public sealed partial class OidCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public OidCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Security.Cryptography.Oid this[int index] { get { throw null; } }
        public System.Security.Cryptography.Oid? this[string oid] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public int Add(System.Security.Cryptography.Oid oid) { throw null; }
        public void CopyTo(System.Security.Cryptography.Oid[] array, int index) { }
        public System.Security.Cryptography.OidEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class OidEnumerator : System.Collections.IEnumerator
    {
        internal OidEnumerator() { }
        public System.Security.Cryptography.Oid Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    public enum OidGroup
    {
        All = 0,
        HashAlgorithm = 1,
        EncryptionAlgorithm = 2,
        PublicKeyAlgorithm = 3,
        SignatureAlgorithm = 4,
        Attribute = 5,
        ExtensionOrAttribute = 6,
        EnhancedKeyUsage = 7,
        Policy = 8,
        Template = 9,
        KeyDerivationFunction = 10,
    }
    public enum PaddingMode
    {
        None = 1,
        PKCS7 = 2,
        Zeros = 3,
        ANSIX923 = 4,
        ISO10126 = 5,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class PasswordDeriveBytes : System.Security.Cryptography.DeriveBytes
    {
        public PasswordDeriveBytes(byte[] password, byte[]? salt) { }
        public PasswordDeriveBytes(byte[] password, byte[]? salt, System.Security.Cryptography.CspParameters? cspParams) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The hash implementation might be removed. Ensure the referenced hash algorithm is not trimmed.")]
        public PasswordDeriveBytes(byte[] password, byte[]? salt, string hashName, int iterations) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The hash implementation might be removed. Ensure the referenced hash algorithm is not trimmed.")]
        public PasswordDeriveBytes(byte[] password, byte[]? salt, string hashName, int iterations, System.Security.Cryptography.CspParameters? cspParams) { }
        public PasswordDeriveBytes(string strPassword, byte[]? rgbSalt) { }
        public PasswordDeriveBytes(string strPassword, byte[]? rgbSalt, System.Security.Cryptography.CspParameters? cspParams) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The hash implementation might be removed. Ensure the referenced hash algorithm is not trimmed.")]
        public PasswordDeriveBytes(string strPassword, byte[]? rgbSalt, string strHashName, int iterations) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The hash implementation might be removed. Ensure the referenced hash algorithm is not trimmed.")]
        public PasswordDeriveBytes(string strPassword, byte[]? rgbSalt, string strHashName, int iterations, System.Security.Cryptography.CspParameters? cspParams) { }
        public string HashName { get { throw null; } [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The hash implementation might be removed. Ensure the referenced hash algorithm is not trimmed.")] set { } }
        public int IterationCount { get { throw null; } set { } }
        public byte[]? Salt { get { throw null; } set { } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public byte[] CryptDeriveKey(string? algname, string? alghashname, int keySize, byte[] rgbIV) { throw null; }
        protected override void Dispose(bool disposing) { }
        [System.ObsoleteAttribute("Rfc2898DeriveBytes replaces PasswordDeriveBytes for deriving key material from a password and is preferred in new applications.")]
        public override byte[] GetBytes(int cb) { throw null; }
        public override void Reset() { }
    }
    public enum PbeEncryptionAlgorithm
    {
        Unknown = 0,
        Aes128Cbc = 1,
        Aes192Cbc = 2,
        Aes256Cbc = 3,
        TripleDes3KeyPkcs12 = 4,
    }
    public sealed partial class PbeParameters
    {
        public PbeParameters(System.Security.Cryptography.PbeEncryptionAlgorithm encryptionAlgorithm, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, int iterationCount) { }
        public System.Security.Cryptography.PbeEncryptionAlgorithm EncryptionAlgorithm { get { throw null; } }
        public System.Security.Cryptography.HashAlgorithmName HashAlgorithm { get { throw null; } }
        public int IterationCount { get { throw null; } }
    }
    public static partial class PemEncoding
    {
        public static System.Security.Cryptography.PemFields Find(System.ReadOnlySpan<char> pemData) { throw null; }
        public static int GetEncodedSize(int labelLength, int dataLength) { throw null; }
        public static bool TryFind(System.ReadOnlySpan<char> pemData, out System.Security.Cryptography.PemFields fields) { throw null; }
        public static bool TryWrite(System.ReadOnlySpan<char> label, System.ReadOnlySpan<byte> data, System.Span<char> destination, out int charsWritten) { throw null; }
        public static char[] Write(System.ReadOnlySpan<char> label, System.ReadOnlySpan<byte> data) { throw null; }
        public static string WriteString(System.ReadOnlySpan<char> label, System.ReadOnlySpan<byte> data) { throw null; }
    }
    public readonly partial struct PemFields
    {
        private readonly int _dummyPrimitive;
        public System.Range Base64Data { get { throw null; } }
        public int DecodedDataLength { get { throw null; } }
        public System.Range Label { get { throw null; } }
        public System.Range Location { get { throw null; } }
    }
    public partial class PKCS1MaskGenerationMethod : System.Security.Cryptography.MaskGenerationMethod
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PKCS1MaskGenerationMethod is not trim compatible because the algorithm implementation referenced by HashName might be removed.")]
        public PKCS1MaskGenerationMethod() { }
        public string HashName { get { throw null; } set { } }
        public override byte[] GenerateMask(byte[] rgbSeed, int cbReturn) { throw null; }
    }
    public abstract partial class RandomNumberGenerator : System.IDisposable
    {
        protected RandomNumberGenerator() { }
        public static System.Security.Cryptography.RandomNumberGenerator Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static System.Security.Cryptography.RandomNumberGenerator? Create(string rngName) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static void Fill(System.Span<byte> data) { }
        public abstract void GetBytes(byte[] data);
        public virtual void GetBytes(byte[] data, int offset, int count) { }
        public static byte[] GetBytes(int count) { throw null; }
        public virtual void GetBytes(System.Span<byte> data) { }
        public static int GetInt32(int toExclusive) { throw null; }
        public static int GetInt32(int fromInclusive, int toExclusive) { throw null; }
        public virtual void GetNonZeroBytes(byte[] data) { }
        public virtual void GetNonZeroBytes(System.Span<byte> data) { }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public abstract partial class RC2 : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected int EffectiveKeySizeValue;
        protected RC2() { }
        public virtual int EffectiveKeySize { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static new System.Security.Cryptography.RC2 Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.RC2? Create(string AlgName) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class RC2CryptoServiceProvider : System.Security.Cryptography.RC2
    {
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public RC2CryptoServiceProvider() { }
        public override int EffectiveKeySize { get { throw null; } set { } }
        public bool UseSalt { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        public override void GenerateIV() { }
        public override void GenerateKey() { }
    }
    public partial class Rfc2898DeriveBytes : System.Security.Cryptography.DeriveBytes
    {
        [System.ObsoleteAttribute("The default hash algorithm and iteration counts in Rfc2898DeriveBytes constructors are outdated and insecure. Use a constructor that accepts the hash algorithm and the number of iterations.", DiagnosticId="SYSLIB0041", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations) { }
        public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        [System.ObsoleteAttribute("The default hash algorithm and iteration counts in Rfc2898DeriveBytes constructors are outdated and insecure. Use a constructor that accepts the hash algorithm and the number of iterations.", DiagnosticId="SYSLIB0041", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public Rfc2898DeriveBytes(string password, byte[] salt) { }
        [System.ObsoleteAttribute("The default hash algorithm and iteration counts in Rfc2898DeriveBytes constructors are outdated and insecure. Use a constructor that accepts the hash algorithm and the number of iterations.", DiagnosticId="SYSLIB0041", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public Rfc2898DeriveBytes(string password, byte[] salt, int iterations) { }
        public Rfc2898DeriveBytes(string password, byte[] salt, int iterations, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        [System.ObsoleteAttribute("The default hash algorithm and iteration counts in Rfc2898DeriveBytes constructors are outdated and insecure. Use a constructor that accepts the hash algorithm and the number of iterations.", DiagnosticId="SYSLIB0041", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public Rfc2898DeriveBytes(string password, int saltSize) { }
        [System.ObsoleteAttribute("The default hash algorithm and iteration counts in Rfc2898DeriveBytes constructors are outdated and insecure. Use a constructor that accepts the hash algorithm and the number of iterations.", DiagnosticId="SYSLIB0041", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public Rfc2898DeriveBytes(string password, int saltSize, int iterations) { }
        public Rfc2898DeriveBytes(string password, int saltSize, int iterations, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public System.Security.Cryptography.HashAlgorithmName HashAlgorithm { get { throw null; } }
        public int IterationCount { get { throw null; } set { } }
        public byte[] Salt { get { throw null; } set { } }
        [System.ObsoleteAttribute("Rfc2898DeriveBytes.CryptDeriveKey is obsolete and is not supported. Use PasswordDeriveBytes.CryptDeriveKey instead.", DiagnosticId="SYSLIB0033", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public byte[] CryptDeriveKey(string algname, string alghashname, int keySize, byte[] rgbIV) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override byte[] GetBytes(int cb) { throw null; }
        public static byte[] Pbkdf2(byte[] password, byte[] salt, int iterations, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, int outputLength) { throw null; }
        public static byte[] Pbkdf2(System.ReadOnlySpan<byte> password, System.ReadOnlySpan<byte> salt, int iterations, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, int outputLength) { throw null; }
        public static void Pbkdf2(System.ReadOnlySpan<byte> password, System.ReadOnlySpan<byte> salt, System.Span<byte> destination, int iterations, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public static byte[] Pbkdf2(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> salt, int iterations, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, int outputLength) { throw null; }
        public static void Pbkdf2(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> salt, System.Span<byte> destination, int iterations, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public static byte[] Pbkdf2(string password, byte[] salt, int iterations, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, int outputLength) { throw null; }
        public override void Reset() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("The Rijndael and RijndaelManaged types are obsolete. Use Aes instead.", DiagnosticId="SYSLIB0022", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public abstract partial class Rijndael : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected Rijndael() { }
        public static new System.Security.Cryptography.Rijndael Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.Rijndael? Create(string algName) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("The Rijndael and RijndaelManaged types are obsolete. Use Aes instead.", DiagnosticId="SYSLIB0022", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class RijndaelManaged : System.Security.Cryptography.Rijndael
    {
        public RijndaelManaged() { }
        public override int BlockSize { get { throw null; } set { } }
        public override int FeedbackSize { get { throw null; } set { } }
        public override byte[] IV { get { throw null; } set { } }
        public override byte[] Key { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public override System.Security.Cryptography.CipherMode Mode { get { throw null; } set { } }
        public override System.Security.Cryptography.PaddingMode Padding { get { throw null; } set { } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void GenerateIV() { }
        public override void GenerateKey() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("RNGCryptoServiceProvider is obsolete. To generate a random number, use one of the RandomNumberGenerator static methods instead.", DiagnosticId="SYSLIB0023", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class RNGCryptoServiceProvider : System.Security.Cryptography.RandomNumberGenerator
    {
        public RNGCryptoServiceProvider() { }
        public RNGCryptoServiceProvider(byte[] rgb) { }
        public RNGCryptoServiceProvider(System.Security.Cryptography.CspParameters? cspParams) { }
        public RNGCryptoServiceProvider(string str) { }
        protected override void Dispose(bool disposing) { }
        public override void GetBytes(byte[] data) { }
        public override void GetBytes(byte[] data, int offset, int count) { }
        public override void GetBytes(System.Span<byte> data) { }
        public override void GetNonZeroBytes(byte[] data) { }
        public override void GetNonZeroBytes(System.Span<byte> data) { }
    }
    public abstract partial class RSA : System.Security.Cryptography.AsymmetricAlgorithm
    {
        protected RSA() { }
        public override string? KeyExchangeAlgorithm { get { throw null; } }
        public override string SignatureAlgorithm { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static new System.Security.Cryptography.RSA Create() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.RSA Create(int keySizeInBits) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.RSA Create(System.Security.Cryptography.RSAParameters parameters) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.RSA? Create(string algName) { throw null; }
        public virtual byte[] Decrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw null; }
        public virtual byte[] DecryptValue(byte[] rgb) { throw null; }
        public virtual byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw null; }
        public virtual byte[] EncryptValue(byte[] rgb) { throw null; }
        public abstract System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters);
        public virtual byte[] ExportRSAPrivateKey() { throw null; }
        public string ExportRSAPrivateKeyPem() { throw null; }
        public virtual byte[] ExportRSAPublicKey() { throw null; }
        public string ExportRSAPublicKeyPem() { throw null; }
        public override void FromXmlString(string xmlString) { }
        protected virtual byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected virtual byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportFromEncryptedPem(System.ReadOnlySpan<char> input, System.ReadOnlySpan<byte> passwordBytes) { }
        public override void ImportFromEncryptedPem(System.ReadOnlySpan<char> input, System.ReadOnlySpan<char> password) { }
        public override void ImportFromPem(System.ReadOnlySpan<char> input) { }
        public abstract void ImportParameters(System.Security.Cryptography.RSAParameters parameters);
        public override void ImportPkcs8PrivateKey(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public virtual void ImportRSAPrivateKey(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public virtual void ImportRSAPublicKey(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportSubjectPublicKeyInfo(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public virtual byte[] SignData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public byte[] SignData(byte[] data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public virtual byte[] SignData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public virtual byte[] SignHash(byte[] hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public override string ToXmlString(bool includePrivateParameters) { throw null; }
        public virtual bool TryDecrypt(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.RSAEncryptionPadding padding, out int bytesWritten) { throw null; }
        public virtual bool TryEncrypt(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.RSAEncryptionPadding padding, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportPkcs8PrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public virtual bool TryExportRSAPrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TryExportRSAPrivateKeyPem(System.Span<char> destination, out int charsWritten) { throw null; }
        public virtual bool TryExportRSAPublicKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TryExportRSAPublicKeyPem(System.Span<char> destination, out int charsWritten) { throw null; }
        public override bool TryExportSubjectPublicKeyInfo(System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected virtual bool TryHashData(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, out int bytesWritten) { throw null; }
        public virtual bool TrySignData(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding, out int bytesWritten) { throw null; }
        public virtual bool TrySignHash(System.ReadOnlySpan<byte> hash, System.Span<byte> destination, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding, out int bytesWritten) { throw null; }
        public bool VerifyData(byte[] data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public bool VerifyData(System.IO.Stream data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public virtual bool VerifyData(System.ReadOnlySpan<byte> data, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public virtual bool VerifyHash(byte[] hash, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public virtual bool VerifyHash(System.ReadOnlySpan<byte> hash, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
    }
    public sealed partial class RSACng : System.Security.Cryptography.RSA
    {
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public RSACng() { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public RSACng(int keySize) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public RSACng(System.Security.Cryptography.CngKey key) { }
        public System.Security.Cryptography.CngKey Key { get { throw null; } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public override byte[] Decrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw null; }
        public override byte[] ExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public override byte[] ExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters) { throw null; }
        public override System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.RSAParameters parameters) { }
        public override void ImportPkcs8PrivateKey(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override byte[] SignHash(byte[] hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public override bool TryDecrypt(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.RSAEncryptionPadding padding, out int bytesWritten) { throw null; }
        public override bool TryEncrypt(System.ReadOnlySpan<byte> data, System.Span<byte> destination, System.Security.Cryptography.RSAEncryptionPadding padding, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportPkcs8PrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TrySignHash(System.ReadOnlySpan<byte> hash, System.Span<byte> destination, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding, out int bytesWritten) { throw null; }
        public override bool VerifyHash(byte[] hash, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public override bool VerifyHash(System.ReadOnlySpan<byte> hash, System.ReadOnlySpan<byte> signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
    }
    public sealed partial class RSACryptoServiceProvider : System.Security.Cryptography.RSA, System.Security.Cryptography.ICspAsymmetricAlgorithm
    {
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public RSACryptoServiceProvider() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public RSACryptoServiceProvider(int dwKeySize) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public RSACryptoServiceProvider(int dwKeySize, System.Security.Cryptography.CspParameters? parameters) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public RSACryptoServiceProvider(System.Security.Cryptography.CspParameters? parameters) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public System.Security.Cryptography.CspKeyContainerInfo CspKeyContainerInfo { get { throw null; } }
        public override string? KeyExchangeAlgorithm { get { throw null; } }
        public override int KeySize { get { throw null; } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public bool PersistKeyInCsp { get { throw null; } set { } }
        public bool PublicOnly { get { throw null; } }
        public override string SignatureAlgorithm { get { throw null; } }
        public static bool UseMachineKeyStore { get { throw null; } set { } }
        public byte[] Decrypt(byte[] rgb, bool fOAEP) { throw null; }
        public override byte[] Decrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw null; }
        public override byte[] DecryptValue(byte[] rgb) { throw null; }
        protected override void Dispose(bool disposing) { }
        public byte[] Encrypt(byte[] rgb, bool fOAEP) { throw null; }
        public override byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw null; }
        public override byte[] EncryptValue(byte[] rgb) { throw null; }
        public byte[] ExportCspBlob(bool includePrivateParameters) { throw null; }
        public override System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters) { throw null; }
        public void ImportCspBlob(byte[] keyBlob) { }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.RSAParameters parameters) { }
        public byte[] SignData(byte[] buffer, int offset, int count, object halg) { throw null; }
        public byte[] SignData(byte[] buffer, object halg) { throw null; }
        public byte[] SignData(System.IO.Stream inputStream, object halg) { throw null; }
        public override byte[] SignHash(byte[] hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public byte[] SignHash(byte[] rgbHash, string? str) { throw null; }
        public bool VerifyData(byte[] buffer, object halg, byte[] signature) { throw null; }
        public override bool VerifyHash(byte[] hash, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature) { throw null; }
    }
    public sealed partial class RSAEncryptionPadding : System.IEquatable<System.Security.Cryptography.RSAEncryptionPadding>
    {
        internal RSAEncryptionPadding() { }
        public System.Security.Cryptography.RSAEncryptionPaddingMode Mode { get { throw null; } }
        public System.Security.Cryptography.HashAlgorithmName OaepHashAlgorithm { get { throw null; } }
        public static System.Security.Cryptography.RSAEncryptionPadding OaepSHA1 { get { throw null; } }
        public static System.Security.Cryptography.RSAEncryptionPadding OaepSHA256 { get { throw null; } }
        public static System.Security.Cryptography.RSAEncryptionPadding OaepSHA384 { get { throw null; } }
        public static System.Security.Cryptography.RSAEncryptionPadding OaepSHA512 { get { throw null; } }
        public static System.Security.Cryptography.RSAEncryptionPadding Pkcs1 { get { throw null; } }
        public static System.Security.Cryptography.RSAEncryptionPadding CreateOaep(System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Security.Cryptography.RSAEncryptionPadding? other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.RSAEncryptionPadding? left, System.Security.Cryptography.RSAEncryptionPadding? right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.RSAEncryptionPadding? left, System.Security.Cryptography.RSAEncryptionPadding? right) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum RSAEncryptionPaddingMode
    {
        Pkcs1 = 0,
        Oaep = 1,
    }
    public partial class RSAOAEPKeyExchangeDeformatter : System.Security.Cryptography.AsymmetricKeyExchangeDeformatter
    {
        public RSAOAEPKeyExchangeDeformatter() { }
        public RSAOAEPKeyExchangeDeformatter(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public override string? Parameters { get { throw null; } set { } }
        public override byte[] DecryptKeyExchange(byte[] rgbData) { throw null; }
        public override void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key) { }
    }
    public partial class RSAOAEPKeyExchangeFormatter : System.Security.Cryptography.AsymmetricKeyExchangeFormatter
    {
        public RSAOAEPKeyExchangeFormatter() { }
        public RSAOAEPKeyExchangeFormatter(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public byte[]? Parameter { get { throw null; } set { } }
        public override string? Parameters { get { throw null; } }
        public System.Security.Cryptography.RandomNumberGenerator? Rng { get { throw null; } set { } }
        public override byte[] CreateKeyExchange(byte[] rgbData) { throw null; }
        public override byte[] CreateKeyExchange(byte[] rgbData, System.Type? symAlgType) { throw null; }
        public override void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key) { }
    }
    public sealed partial class RSAOpenSsl : System.Security.Cryptography.RSA
    {
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public RSAOpenSsl() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public RSAOpenSsl(int keySize) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public RSAOpenSsl(System.IntPtr handle) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public RSAOpenSsl(System.Security.Cryptography.RSAParameters parameters) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public RSAOpenSsl(System.Security.Cryptography.SafeEvpPKeyHandle pkeyHandle) { }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateKeyHandle() { throw null; }
        public override System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.RSAParameters parameters) { }
    }
    public partial struct RSAParameters
    {
        public byte[]? D;
        public byte[]? DP;
        public byte[]? DQ;
        public byte[]? Exponent;
        public byte[]? InverseQ;
        public byte[]? Modulus;
        public byte[]? P;
        public byte[]? Q;
    }
    public partial class RSAPKCS1KeyExchangeDeformatter : System.Security.Cryptography.AsymmetricKeyExchangeDeformatter
    {
        public RSAPKCS1KeyExchangeDeformatter() { }
        public RSAPKCS1KeyExchangeDeformatter(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public override string? Parameters { get { throw null; } set { } }
        public System.Security.Cryptography.RandomNumberGenerator? RNG { get { throw null; } set { } }
        public override byte[] DecryptKeyExchange(byte[] rgbIn) { throw null; }
        public override void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key) { }
    }
    public partial class RSAPKCS1KeyExchangeFormatter : System.Security.Cryptography.AsymmetricKeyExchangeFormatter
    {
        public RSAPKCS1KeyExchangeFormatter() { }
        public RSAPKCS1KeyExchangeFormatter(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public override string Parameters { get { throw null; } }
        public System.Security.Cryptography.RandomNumberGenerator? Rng { get { throw null; } set { } }
        public override byte[] CreateKeyExchange(byte[] rgbData) { throw null; }
        public override byte[] CreateKeyExchange(byte[] rgbData, System.Type? symAlgType) { throw null; }
        public override void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key) { }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class RSAPKCS1SignatureDeformatter : System.Security.Cryptography.AsymmetricSignatureDeformatter
    {
        public RSAPKCS1SignatureDeformatter() { }
        public RSAPKCS1SignatureDeformatter(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public override void SetHashAlgorithm(string strName) { }
        public override void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class RSAPKCS1SignatureFormatter : System.Security.Cryptography.AsymmetricSignatureFormatter
    {
        public RSAPKCS1SignatureFormatter() { }
        public RSAPKCS1SignatureFormatter(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public override byte[] CreateSignature(byte[] rgbHash) { throw null; }
        public override void SetHashAlgorithm(string strName) { }
        public override void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key) { }
    }
    public sealed partial class RSASignaturePadding : System.IEquatable<System.Security.Cryptography.RSASignaturePadding>
    {
        internal RSASignaturePadding() { }
        public System.Security.Cryptography.RSASignaturePaddingMode Mode { get { throw null; } }
        public static System.Security.Cryptography.RSASignaturePadding Pkcs1 { get { throw null; } }
        public static System.Security.Cryptography.RSASignaturePadding Pss { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Security.Cryptography.RSASignaturePadding? other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.RSASignaturePadding? left, System.Security.Cryptography.RSASignaturePadding? right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.RSASignaturePadding? left, System.Security.Cryptography.RSASignaturePadding? right) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum RSASignaturePaddingMode
    {
        Pkcs1 = 0,
        Pss = 1,
    }
    public sealed partial class SafeEvpPKeyHandle : System.Runtime.InteropServices.SafeHandle
    {
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public SafeEvpPKeyHandle() : base (default(System.IntPtr), default(bool)) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public SafeEvpPKeyHandle(System.IntPtr handle, bool ownsHandle) : base (default(System.IntPtr), default(bool)) { }
        public override bool IsInvalid { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public static long OpenSslVersion { get { throw null; } }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateHandle() { throw null; }
        protected override bool ReleaseHandle() { throw null; }
    }
    public abstract partial class SHA1 : System.Security.Cryptography.HashAlgorithm
    {
        public const int HashSizeInBits = 160;
        public const int HashSizeInBytes = 20;
        protected SHA1() { }
        public static new System.Security.Cryptography.SHA1 Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.SHA1? Create(string hashName) { throw null; }
        public static byte[] HashData(byte[] source) { throw null; }
        public static byte[] HashData(System.IO.Stream source) { throw null; }
        public static int HashData(System.IO.Stream source, System.Span<byte> destination) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static System.Threading.Tasks.ValueTask<int> HashDataAsync(System.IO.Stream source, System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static bool TryHashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class SHA1CryptoServiceProvider : System.Security.Cryptography.SHA1
    {
        public SHA1CryptoServiceProvider() { }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] array, int ibStart, int cbSize) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class SHA1Managed : System.Security.Cryptography.SHA1
    {
        public SHA1Managed() { }
        protected sealed override void Dispose(bool disposing) { }
        protected sealed override void HashCore(byte[] array, int ibStart, int cbSize) { }
        protected sealed override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected sealed override byte[] HashFinal() { throw null; }
        public sealed override void Initialize() { }
        protected sealed override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public abstract partial class SHA256 : System.Security.Cryptography.HashAlgorithm
    {
        public const int HashSizeInBits = 256;
        public const int HashSizeInBytes = 32;
        protected SHA256() { }
        public static new System.Security.Cryptography.SHA256 Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.SHA256? Create(string hashName) { throw null; }
        public static byte[] HashData(byte[] source) { throw null; }
        public static byte[] HashData(System.IO.Stream source) { throw null; }
        public static int HashData(System.IO.Stream source, System.Span<byte> destination) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static System.Threading.Tasks.ValueTask<int> HashDataAsync(System.IO.Stream source, System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static bool TryHashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class SHA256CryptoServiceProvider : System.Security.Cryptography.SHA256
    {
        public SHA256CryptoServiceProvider() { }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] array, int ibStart, int cbSize) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class SHA256Managed : System.Security.Cryptography.SHA256
    {
        public SHA256Managed() { }
        protected sealed override void Dispose(bool disposing) { }
        protected sealed override void HashCore(byte[] array, int ibStart, int cbSize) { }
        protected sealed override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected sealed override byte[] HashFinal() { throw null; }
        public sealed override void Initialize() { }
        protected sealed override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public abstract partial class SHA384 : System.Security.Cryptography.HashAlgorithm
    {
        public const int HashSizeInBits = 384;
        public const int HashSizeInBytes = 48;
        protected SHA384() { }
        public static new System.Security.Cryptography.SHA384 Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.SHA384? Create(string hashName) { throw null; }
        public static byte[] HashData(byte[] source) { throw null; }
        public static byte[] HashData(System.IO.Stream source) { throw null; }
        public static int HashData(System.IO.Stream source, System.Span<byte> destination) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static System.Threading.Tasks.ValueTask<int> HashDataAsync(System.IO.Stream source, System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static bool TryHashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class SHA384CryptoServiceProvider : System.Security.Cryptography.SHA384
    {
        public SHA384CryptoServiceProvider() { }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] array, int ibStart, int cbSize) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class SHA384Managed : System.Security.Cryptography.SHA384
    {
        public SHA384Managed() { }
        protected sealed override void Dispose(bool disposing) { }
        protected sealed override void HashCore(byte[] array, int ibStart, int cbSize) { }
        protected sealed override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected sealed override byte[] HashFinal() { throw null; }
        public sealed override void Initialize() { }
        protected sealed override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public abstract partial class SHA512 : System.Security.Cryptography.HashAlgorithm
    {
        public const int HashSizeInBits = 512;
        public const int HashSizeInBytes = 64;
        protected SHA512() { }
        public static new System.Security.Cryptography.SHA512 Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.SHA512? Create(string hashName) { throw null; }
        public static byte[] HashData(byte[] source) { throw null; }
        public static byte[] HashData(System.IO.Stream source) { throw null; }
        public static int HashData(System.IO.Stream source, System.Span<byte> destination) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static System.Threading.Tasks.ValueTask<int> HashDataAsync(System.IO.Stream source, System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<byte[]> HashDataAsync(System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static bool TryHashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class SHA512CryptoServiceProvider : System.Security.Cryptography.SHA512
    {
        public SHA512CryptoServiceProvider() { }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] array, int ibStart, int cbSize) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class SHA512Managed : System.Security.Cryptography.SHA512
    {
        public SHA512Managed() { }
        protected sealed override void Dispose(bool disposing) { }
        protected sealed override void HashCore(byte[] array, int ibStart, int cbSize) { }
        protected sealed override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected sealed override byte[] HashFinal() { throw null; }
        public sealed override void Initialize() { }
        protected sealed override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public partial class SignatureDescription
    {
        public SignatureDescription() { }
        public SignatureDescription(System.Security.SecurityElement el) { }
        public string? DeformatterAlgorithm { get { throw null; } set { } }
        public string? DigestAlgorithm { get { throw null; } set { } }
        public string? FormatterAlgorithm { get { throw null; } set { } }
        public string? KeyAlgorithm { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("CreateDeformatter is not trim compatible because the algorithm implementation referenced by DeformatterAlgorithm might be removed.")]
        public virtual System.Security.Cryptography.AsymmetricSignatureDeformatter CreateDeformatter(System.Security.Cryptography.AsymmetricAlgorithm key) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("CreateDigest is not trim compatible because the algorithm implementation referenced by DigestAlgorithm might be removed.")]
        public virtual System.Security.Cryptography.HashAlgorithm? CreateDigest() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("CreateFormatter is not trim compatible because the algorithm implementation referenced by FormatterAlgorithm might be removed.")]
        public virtual System.Security.Cryptography.AsymmetricSignatureFormatter CreateFormatter(System.Security.Cryptography.AsymmetricAlgorithm key) { throw null; }
    }
    public abstract partial class SymmetricAlgorithm : System.IDisposable
    {
        protected int BlockSizeValue;
        protected int FeedbackSizeValue;
        protected byte[]? IVValue;
        protected int KeySizeValue;
        protected byte[]? KeyValue;
        [System.Diagnostics.CodeAnalysis.MaybeNullAttribute]
        protected System.Security.Cryptography.KeySizes[] LegalBlockSizesValue;
        [System.Diagnostics.CodeAnalysis.MaybeNullAttribute]
        protected System.Security.Cryptography.KeySizes[] LegalKeySizesValue;
        protected System.Security.Cryptography.CipherMode ModeValue;
        protected System.Security.Cryptography.PaddingMode PaddingValue;
        protected SymmetricAlgorithm() { }
        public virtual int BlockSize { get { throw null; } set { } }
        public virtual int FeedbackSize { get { throw null; } set { } }
        public virtual byte[] IV { get { throw null; } set { } }
        public virtual byte[] Key { get { throw null; } set { } }
        public virtual int KeySize { get { throw null; } set { } }
        public virtual System.Security.Cryptography.KeySizes[] LegalBlockSizes { get { throw null; } }
        public virtual System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public virtual System.Security.Cryptography.CipherMode Mode { get { throw null; } set { } }
        public virtual System.Security.Cryptography.PaddingMode Padding { get { throw null; } set { } }
        public void Clear() { }
        [System.ObsoleteAttribute("The default implementation of this cryptography algorithm is not supported.", DiagnosticId="SYSLIB0007", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static System.Security.Cryptography.SymmetricAlgorithm Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static System.Security.Cryptography.SymmetricAlgorithm? Create(string algName) { throw null; }
        public virtual System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public abstract System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV);
        public virtual System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public abstract System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV);
        public byte[] DecryptCbc(byte[] ciphertext, byte[] iv, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.PKCS7) { throw null; }
        public byte[] DecryptCbc(System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> iv, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.PKCS7) { throw null; }
        public int DecryptCbc(System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.PKCS7) { throw null; }
        public byte[] DecryptCfb(byte[] ciphertext, byte[] iv, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.None, int feedbackSizeInBits = 8) { throw null; }
        public byte[] DecryptCfb(System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> iv, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.None, int feedbackSizeInBits = 8) { throw null; }
        public int DecryptCfb(System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.None, int feedbackSizeInBits = 8) { throw null; }
        public byte[] DecryptEcb(byte[] ciphertext, System.Security.Cryptography.PaddingMode paddingMode) { throw null; }
        public byte[] DecryptEcb(System.ReadOnlySpan<byte> ciphertext, System.Security.Cryptography.PaddingMode paddingMode) { throw null; }
        public int DecryptEcb(System.ReadOnlySpan<byte> ciphertext, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public byte[] EncryptCbc(byte[] plaintext, byte[] iv, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.PKCS7) { throw null; }
        public byte[] EncryptCbc(System.ReadOnlySpan<byte> plaintext, System.ReadOnlySpan<byte> iv, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.PKCS7) { throw null; }
        public int EncryptCbc(System.ReadOnlySpan<byte> plaintext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.PKCS7) { throw null; }
        public byte[] EncryptCfb(byte[] plaintext, byte[] iv, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.None, int feedbackSizeInBits = 8) { throw null; }
        public byte[] EncryptCfb(System.ReadOnlySpan<byte> plaintext, System.ReadOnlySpan<byte> iv, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.None, int feedbackSizeInBits = 8) { throw null; }
        public int EncryptCfb(System.ReadOnlySpan<byte> plaintext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.None, int feedbackSizeInBits = 8) { throw null; }
        public byte[] EncryptEcb(byte[] plaintext, System.Security.Cryptography.PaddingMode paddingMode) { throw null; }
        public byte[] EncryptEcb(System.ReadOnlySpan<byte> plaintext, System.Security.Cryptography.PaddingMode paddingMode) { throw null; }
        public int EncryptEcb(System.ReadOnlySpan<byte> plaintext, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode) { throw null; }
        public abstract void GenerateIV();
        public abstract void GenerateKey();
        public int GetCiphertextLengthCbc(int plaintextLength, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.PKCS7) { throw null; }
        public int GetCiphertextLengthCfb(int plaintextLength, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.None, int feedbackSizeInBits = 8) { throw null; }
        public int GetCiphertextLengthEcb(int plaintextLength, System.Security.Cryptography.PaddingMode paddingMode) { throw null; }
        public bool TryDecryptCbc(System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, out int bytesWritten, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.PKCS7) { throw null; }
        protected virtual bool TryDecryptCbcCore(System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
        public bool TryDecryptCfb(System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, out int bytesWritten, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.None, int feedbackSizeInBits = 8) { throw null; }
        protected virtual bool TryDecryptCfbCore(System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten) { throw null; }
        public bool TryDecryptEcb(System.ReadOnlySpan<byte> ciphertext, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
        protected virtual bool TryDecryptEcbCore(System.ReadOnlySpan<byte> ciphertext, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
        public bool TryEncryptCbc(System.ReadOnlySpan<byte> plaintext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, out int bytesWritten, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.PKCS7) { throw null; }
        protected virtual bool TryEncryptCbcCore(System.ReadOnlySpan<byte> plaintext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
        public bool TryEncryptCfb(System.ReadOnlySpan<byte> plaintext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, out int bytesWritten, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.None, int feedbackSizeInBits = 8) { throw null; }
        protected virtual bool TryEncryptCfbCore(System.ReadOnlySpan<byte> plaintext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten) { throw null; }
        public bool TryEncryptEcb(System.ReadOnlySpan<byte> plaintext, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
        protected virtual bool TryEncryptEcbCore(System.ReadOnlySpan<byte> plaintext, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
        public bool ValidKeySize(int bitLength) { throw null; }
    }
    public partial class ToBase64Transform : System.IDisposable, System.Security.Cryptography.ICryptoTransform
    {
        public ToBase64Transform() { }
        public virtual bool CanReuseTransform { get { throw null; } }
        public bool CanTransformMultipleBlocks { get { throw null; } }
        public int InputBlockSize { get { throw null; } }
        public int OutputBlockSize { get { throw null; } }
        public void Clear() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~ToBase64Transform() { }
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset) { throw null; }
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) { throw null; }
    }
    public abstract partial class TripleDES : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected TripleDES() { }
        public override byte[] Key { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static new System.Security.Cryptography.TripleDES Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        [System.ObsoleteAttribute("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId="SYSLIB0045", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.TripleDES? Create(string str) { throw null; }
        public static bool IsWeakKey(byte[] rgbKey) { throw null; }
    }
    public sealed partial class TripleDESCng : System.Security.Cryptography.TripleDES
    {
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public TripleDESCng() { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public TripleDESCng(string keyName) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public TripleDESCng(string keyName, System.Security.Cryptography.CngProvider provider) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public TripleDESCng(string keyName, System.Security.Cryptography.CngProvider provider, System.Security.Cryptography.CngKeyOpenOptions openOptions) { }
        public override byte[] Key { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void GenerateIV() { }
        public override void GenerateKey() { }
        protected override bool TryDecryptCbcCore(System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
        protected override bool TryDecryptCfbCore(System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten) { throw null; }
        protected override bool TryDecryptEcbCore(System.ReadOnlySpan<byte> ciphertext, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
        protected override bool TryEncryptCbcCore(System.ReadOnlySpan<byte> plaintext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
        protected override bool TryEncryptCfbCore(System.ReadOnlySpan<byte> plaintext, System.ReadOnlySpan<byte> iv, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten) { throw null; }
        protected override bool TryEncryptEcbCore(System.ReadOnlySpan<byte> plaintext, System.Span<byte> destination, System.Security.Cryptography.PaddingMode paddingMode, out int bytesWritten) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId="SYSLIB0021", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class TripleDESCryptoServiceProvider : System.Security.Cryptography.TripleDES
    {
        public TripleDESCryptoServiceProvider() { }
        public override int BlockSize { get { throw null; } set { } }
        public override int FeedbackSize { get { throw null; } set { } }
        public override byte[] IV { get { throw null; } set { } }
        public override byte[] Key { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.KeySizes[] LegalBlockSizes { get { throw null; } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public override System.Security.Cryptography.CipherMode Mode { get { throw null; } set { } }
        public override System.Security.Cryptography.PaddingMode Padding { get { throw null; } set { } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void GenerateIV() { }
        public override void GenerateKey() { }
    }
}
namespace System.Security.Cryptography.X509Certificates
{
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public sealed partial class CertificateRequest
    {
        public CertificateRequest(System.Security.Cryptography.X509Certificates.X500DistinguishedName subjectName, System.Security.Cryptography.ECDsa key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public CertificateRequest(System.Security.Cryptography.X509Certificates.X500DistinguishedName subjectName, System.Security.Cryptography.RSA key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { }
        public CertificateRequest(System.Security.Cryptography.X509Certificates.X500DistinguishedName subjectName, System.Security.Cryptography.X509Certificates.PublicKey publicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public CertificateRequest(System.Security.Cryptography.X509Certificates.X500DistinguishedName subjectName, System.Security.Cryptography.X509Certificates.PublicKey publicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding? rsaSignaturePadding = null) { }
        public CertificateRequest(string subjectName, System.Security.Cryptography.ECDsa key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public CertificateRequest(string subjectName, System.Security.Cryptography.RSA key, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { }
        public System.Collections.ObjectModel.Collection<System.Security.Cryptography.X509Certificates.X509Extension> CertificateExtensions { get { throw null; } }
        public System.Security.Cryptography.HashAlgorithmName HashAlgorithm { get { throw null; } }
        public System.Collections.ObjectModel.Collection<System.Security.Cryptography.AsnEncodedData> OtherRequestAttributes { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.PublicKey PublicKey { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName SubjectName { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Create(System.Security.Cryptography.X509Certificates.X500DistinguishedName issuerName, System.Security.Cryptography.X509Certificates.X509SignatureGenerator generator, System.DateTimeOffset notBefore, System.DateTimeOffset notAfter, byte[] serialNumber) { throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Create(System.Security.Cryptography.X509Certificates.X500DistinguishedName issuerName, System.Security.Cryptography.X509Certificates.X509SignatureGenerator generator, System.DateTimeOffset notBefore, System.DateTimeOffset notAfter, System.ReadOnlySpan<byte> serialNumber) { throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Create(System.Security.Cryptography.X509Certificates.X509Certificate2 issuerCertificate, System.DateTimeOffset notBefore, System.DateTimeOffset notAfter, byte[] serialNumber) { throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Create(System.Security.Cryptography.X509Certificates.X509Certificate2 issuerCertificate, System.DateTimeOffset notBefore, System.DateTimeOffset notAfter, System.ReadOnlySpan<byte> serialNumber) { throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 CreateSelfSigned(System.DateTimeOffset notBefore, System.DateTimeOffset notAfter) { throw null; }
        public byte[] CreateSigningRequest() { throw null; }
        public byte[] CreateSigningRequest(System.Security.Cryptography.X509Certificates.X509SignatureGenerator signatureGenerator) { throw null; }
        public string CreateSigningRequestPem() { throw null; }
        public string CreateSigningRequestPem(System.Security.Cryptography.X509Certificates.X509SignatureGenerator signatureGenerator) { throw null; }
        public static System.Security.Cryptography.X509Certificates.CertificateRequest LoadSigningRequest(byte[] pkcs10, System.Security.Cryptography.HashAlgorithmName signerHashAlgorithm, System.Security.Cryptography.X509Certificates.CertificateRequestLoadOptions options = System.Security.Cryptography.X509Certificates.CertificateRequestLoadOptions.Default, System.Security.Cryptography.RSASignaturePadding? signerSignaturePadding = null) { throw null; }
        public static System.Security.Cryptography.X509Certificates.CertificateRequest LoadSigningRequest(System.ReadOnlySpan<byte> pkcs10, System.Security.Cryptography.HashAlgorithmName signerHashAlgorithm, out int bytesConsumed, System.Security.Cryptography.X509Certificates.CertificateRequestLoadOptions options = System.Security.Cryptography.X509Certificates.CertificateRequestLoadOptions.Default, System.Security.Cryptography.RSASignaturePadding? signerSignaturePadding = null) { throw null; }
        public static System.Security.Cryptography.X509Certificates.CertificateRequest LoadSigningRequestPem(System.ReadOnlySpan<char> pkcs10Pem, System.Security.Cryptography.HashAlgorithmName signerHashAlgorithm, System.Security.Cryptography.X509Certificates.CertificateRequestLoadOptions options = System.Security.Cryptography.X509Certificates.CertificateRequestLoadOptions.Default, System.Security.Cryptography.RSASignaturePadding? signerSignaturePadding = null) { throw null; }
        public static System.Security.Cryptography.X509Certificates.CertificateRequest LoadSigningRequestPem(string pkcs10Pem, System.Security.Cryptography.HashAlgorithmName signerHashAlgorithm, System.Security.Cryptography.X509Certificates.CertificateRequestLoadOptions options = System.Security.Cryptography.X509Certificates.CertificateRequestLoadOptions.Default, System.Security.Cryptography.RSASignaturePadding? signerSignaturePadding = null) { throw null; }
    }
    [System.FlagsAttribute]
    public enum CertificateRequestLoadOptions
    {
        Default = 0,
        SkipSignatureValidation = 1,
        UnsafeLoadCertificateExtensions = 2,
    }
    public sealed partial class CertificateRevocationListBuilder
    {
        public CertificateRevocationListBuilder() { }
        public void AddEntry(byte[] serialNumber, System.DateTimeOffset? revocationTime = default(System.DateTimeOffset?), System.Security.Cryptography.X509Certificates.X509RevocationReason? reason = default(System.Security.Cryptography.X509Certificates.X509RevocationReason?)) { }
        public void AddEntry(System.ReadOnlySpan<byte> serialNumber, System.DateTimeOffset? revocationTime = default(System.DateTimeOffset?), System.Security.Cryptography.X509Certificates.X509RevocationReason? reason = default(System.Security.Cryptography.X509Certificates.X509RevocationReason?)) { }
        public void AddEntry(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.DateTimeOffset? revocationTime = default(System.DateTimeOffset?), System.Security.Cryptography.X509Certificates.X509RevocationReason? reason = default(System.Security.Cryptography.X509Certificates.X509RevocationReason?)) { }
        public byte[] Build(System.Security.Cryptography.X509Certificates.X500DistinguishedName issuerName, System.Security.Cryptography.X509Certificates.X509SignatureGenerator generator, System.Numerics.BigInteger crlNumber, System.DateTimeOffset nextUpdate, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.X509Certificates.X509AuthorityKeyIdentifierExtension authorityKeyIdentifier, System.DateTimeOffset? thisUpdate = default(System.DateTimeOffset?)) { throw null; }
        public byte[] Build(System.Security.Cryptography.X509Certificates.X509Certificate2 issuerCertificate, System.Numerics.BigInteger crlNumber, System.DateTimeOffset nextUpdate, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding? rsaSignaturePadding = null, System.DateTimeOffset? thisUpdate = default(System.DateTimeOffset?)) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Extension BuildCrlDistributionPointExtension(System.Collections.Generic.IEnumerable<string> uris, bool critical = false) { throw null; }
        public static System.Security.Cryptography.X509Certificates.CertificateRevocationListBuilder Load(byte[] currentCrl, out System.Numerics.BigInteger currentCrlNumber) { throw null; }
        public static System.Security.Cryptography.X509Certificates.CertificateRevocationListBuilder Load(System.ReadOnlySpan<byte> currentCrl, out System.Numerics.BigInteger currentCrlNumber, out int bytesConsumed) { throw null; }
        public static System.Security.Cryptography.X509Certificates.CertificateRevocationListBuilder LoadPem(System.ReadOnlySpan<char> currentCrl, out System.Numerics.BigInteger currentCrlNumber) { throw null; }
        public static System.Security.Cryptography.X509Certificates.CertificateRevocationListBuilder LoadPem(string currentCrl, out System.Numerics.BigInteger currentCrlNumber) { throw null; }
        public bool RemoveEntry(byte[] serialNumber) { throw null; }
        public bool RemoveEntry(System.ReadOnlySpan<byte> serialNumber) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
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
        [System.ObsoleteAttribute("PublicKey.Key is obsolete. Use the appropriate method to get the public key, such as GetRSAPublicKey.", DiagnosticId="SYSLIB0027", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public System.Security.Cryptography.AsymmetricAlgorithm Key { get { throw null; } }
        public System.Security.Cryptography.Oid Oid { get { throw null; } }
        public static System.Security.Cryptography.X509Certificates.PublicKey CreateFromSubjectPublicKeyInfo(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public byte[] ExportSubjectPublicKeyInfo() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public System.Security.Cryptography.DSA? GetDSAPublicKey() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Security.Cryptography.ECDiffieHellman? GetECDiffieHellmanPublicKey() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Security.Cryptography.ECDsa? GetECDsaPublicKey() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public System.Security.Cryptography.RSA? GetRSAPublicKey() { throw null; }
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
        public System.Collections.Generic.IEnumerable<System.Security.Cryptography.X509Certificates.X500RelativeDistinguishedName> EnumerateRelativeDistinguishedNames(bool reversed = true) { throw null; }
        public override string Format(bool multiLine) { throw null; }
    }
    public sealed partial class X500DistinguishedNameBuilder
    {
        public X500DistinguishedNameBuilder() { }
        public void Add(System.Security.Cryptography.Oid oid, string value, System.Formats.Asn1.UniversalTagNumber? stringEncodingType = default(System.Formats.Asn1.UniversalTagNumber?)) { }
        public void Add(string oidValue, string value, System.Formats.Asn1.UniversalTagNumber? stringEncodingType = default(System.Formats.Asn1.UniversalTagNumber?)) { }
        public void AddCommonName(string commonName) { }
        public void AddCountryOrRegion(string twoLetterCode) { }
        public void AddDomainComponent(string domainComponent) { }
        public void AddEmailAddress(string emailAddress) { }
        public void AddLocalityName(string localityName) { }
        public void AddOrganizationalUnitName(string organizationalUnitName) { }
        public void AddOrganizationName(string organizationName) { }
        public void AddStateOrProvinceName(string stateOrProvinceName) { }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName Build() { throw null; }
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
    public sealed partial class X500RelativeDistinguishedName
    {
        internal X500RelativeDistinguishedName() { }
        public bool HasMultipleElements { get { throw null; } }
        public System.ReadOnlyMemory<byte> RawData { get { throw null; } }
        public System.Security.Cryptography.Oid GetSingleElementType() { throw null; }
        public string? GetSingleElementValue() { throw null; }
    }
    public sealed partial class X509AuthorityInformationAccessExtension : System.Security.Cryptography.X509Certificates.X509Extension
    {
        public X509AuthorityInformationAccessExtension() { }
        public X509AuthorityInformationAccessExtension(byte[] rawData, bool critical = false) { }
        public X509AuthorityInformationAccessExtension(System.Collections.Generic.IEnumerable<string>? ocspUris, System.Collections.Generic.IEnumerable<string>? caIssuersUris, bool critical = false) { }
        public X509AuthorityInformationAccessExtension(System.ReadOnlySpan<byte> rawData, bool critical = false) { }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
        public System.Collections.Generic.IEnumerable<string> EnumerateCAIssuersUris() { throw null; }
        public System.Collections.Generic.IEnumerable<string> EnumerateOcspUris() { throw null; }
        public System.Collections.Generic.IEnumerable<string> EnumerateUris(System.Security.Cryptography.Oid accessMethodOid) { throw null; }
        public System.Collections.Generic.IEnumerable<string> EnumerateUris(string accessMethodOid) { throw null; }
    }
    public sealed partial class X509AuthorityKeyIdentifierExtension : System.Security.Cryptography.X509Certificates.X509Extension
    {
        public X509AuthorityKeyIdentifierExtension() { }
        public X509AuthorityKeyIdentifierExtension(byte[] rawData, bool critical = false) { }
        public X509AuthorityKeyIdentifierExtension(System.ReadOnlySpan<byte> rawData, bool critical = false) { }
        public System.ReadOnlyMemory<byte>? KeyIdentifier { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName? NamedIssuer { get { throw null; } }
        public System.ReadOnlyMemory<byte>? RawIssuer { get { throw null; } }
        public System.ReadOnlyMemory<byte>? SerialNumber { get { throw null; } }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
        public static System.Security.Cryptography.X509Certificates.X509AuthorityKeyIdentifierExtension Create(byte[] keyIdentifier, System.Security.Cryptography.X509Certificates.X500DistinguishedName issuerName, byte[] serialNumber) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509AuthorityKeyIdentifierExtension Create(System.ReadOnlySpan<byte> keyIdentifier, System.Security.Cryptography.X509Certificates.X500DistinguishedName issuerName, System.ReadOnlySpan<byte> serialNumber) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509AuthorityKeyIdentifierExtension CreateFromCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, bool includeKeyIdentifier, bool includeIssuerAndSerial) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509AuthorityKeyIdentifierExtension CreateFromIssuerNameAndSerialNumber(System.Security.Cryptography.X509Certificates.X500DistinguishedName issuerName, byte[] serialNumber) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509AuthorityKeyIdentifierExtension CreateFromIssuerNameAndSerialNumber(System.Security.Cryptography.X509Certificates.X500DistinguishedName issuerName, System.ReadOnlySpan<byte> serialNumber) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509AuthorityKeyIdentifierExtension CreateFromSubjectKeyIdentifier(byte[] subjectKeyIdentifier) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509AuthorityKeyIdentifierExtension CreateFromSubjectKeyIdentifier(System.ReadOnlySpan<byte> subjectKeyIdentifier) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509AuthorityKeyIdentifierExtension CreateFromSubjectKeyIdentifier(System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension subjectKeyIdentifier) { throw null; }
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
        public static System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension CreateForCertificateAuthority(int? pathLengthConstraint = default(int?)) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension CreateForEndEntity(bool critical = false) { throw null; }
    }
    public partial class X509Certificate : System.IDisposable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate(byte[] data) { }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate(byte[] rawData, System.Security.SecureString? password) { }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate(byte[] rawData, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate(byte[] rawData, string? password) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate(System.IntPtr handle) { }
        public X509Certificate(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate(System.Security.Cryptography.X509Certificates.X509Certificate cert) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate(string fileName) { }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate(string fileName, System.Security.SecureString? password) { }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate(string fileName, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate(string fileName, string? password) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public System.IntPtr Handle { get { throw null; } }
        public string Issuer { get { throw null; } }
        public System.ReadOnlyMemory<byte> SerialNumberBytes { get { throw null; } }
        public string Subject { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.X509Certificates.X509Certificate CreateFromCertFile(string filename) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
        [System.ObsoleteAttribute("X509Certificate.GetIssuerName has been deprecated. Use the Issuer property instead.")]
        public virtual string GetIssuerName() { throw null; }
        public virtual string GetKeyAlgorithm() { throw null; }
        public virtual byte[] GetKeyAlgorithmParameters() { throw null; }
        public virtual string GetKeyAlgorithmParametersString() { throw null; }
        [System.ObsoleteAttribute("X509Certificate.GetName has been deprecated. Use the Subject property instead.")]
        public virtual string GetName() { throw null; }
        public virtual byte[] GetPublicKey() { throw null; }
        public virtual string GetPublicKeyString() { throw null; }
        public virtual byte[] GetRawCertData() { throw null; }
        public virtual string GetRawCertDataString() { throw null; }
        public virtual byte[] GetSerialNumber() { throw null; }
        public virtual string GetSerialNumberString() { throw null; }
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public virtual void Import(byte[] rawData) { }
        [System.CLSCompliantAttribute(false)]
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public virtual void Import(byte[] rawData, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public virtual void Import(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public virtual void Import(string fileName) { }
        [System.CLSCompliantAttribute(false)]
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public virtual void Import(string fileName, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
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
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(byte[] rawData) { }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(byte[] rawData, System.Security.SecureString? password) { }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(byte[] rawData, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(byte[] rawData, string? password) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(System.IntPtr handle) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(System.ReadOnlySpan<byte> rawData) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(System.ReadOnlySpan<byte> rawData, System.ReadOnlySpan<char> password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet) { }
        protected X509Certificate2(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(System.Security.Cryptography.X509Certificates.X509Certificate certificate) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(string fileName) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(string fileName, System.ReadOnlySpan<char> password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.DefaultKeySet) { }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(string fileName, System.Security.SecureString? password) { }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(string fileName, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(string fileName, string? password) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public X509Certificate2(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public bool Archived { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        public System.Security.Cryptography.X509Certificates.X509ExtensionCollection Extensions { get { throw null; } }
        public string FriendlyName { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        public bool HasPrivateKey { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName IssuerName { get { throw null; } }
        public System.DateTime NotAfter { get { throw null; } }
        public System.DateTime NotBefore { get { throw null; } }
        [System.ObsoleteAttribute("X509Certificate2.PrivateKey is obsolete. Use the appropriate method to get the private key, such as GetRSAPrivateKey, or use the CopyWithPrivateKey method to create a new instance with a private key.", DiagnosticId="SYSLIB0028", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public System.Security.Cryptography.AsymmetricAlgorithm? PrivateKey { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.PublicKey PublicKey { get { throw null; } }
        public byte[] RawData { get { throw null; } }
        public System.ReadOnlyMemory<byte> RawDataMemory { get { throw null; } }
        public string SerialNumber { get { throw null; } }
        public System.Security.Cryptography.Oid SignatureAlgorithm { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X500DistinguishedName SubjectName { get { throw null; } }
        public string Thumbprint { get { throw null; } }
        public int Version { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 CopyWithPrivateKey(System.Security.Cryptography.ECDiffieHellman privateKey) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromEncryptedPem(System.ReadOnlySpan<char> certPem, System.ReadOnlySpan<char> keyPem, System.ReadOnlySpan<char> password) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromEncryptedPemFile(string certPemFilePath, System.ReadOnlySpan<char> password, string? keyPemFilePath = null) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromPem(System.ReadOnlySpan<char> certPem) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromPem(System.ReadOnlySpan<char> certPem, System.ReadOnlySpan<char> keyPem) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateFromPemFile(string certPemFilePath, string? keyPemFilePath = null) { throw null; }
        public string ExportCertificatePem() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.X509Certificates.X509ContentType GetCertContentType(byte[] rawData) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.X509Certificates.X509ContentType GetCertContentType(System.ReadOnlySpan<byte> rawData) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.X509Certificates.X509ContentType GetCertContentType(string fileName) { throw null; }
        public System.Security.Cryptography.ECDiffieHellman? GetECDiffieHellmanPrivateKey() { throw null; }
        public System.Security.Cryptography.ECDiffieHellman? GetECDiffieHellmanPublicKey() { throw null; }
        public string GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType nameType, bool forIssuer) { throw null; }
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public override void Import(byte[] rawData) { }
        [System.CLSCompliantAttribute(false)]
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public override void Import(byte[] rawData, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public override void Import(byte[] rawData, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public override void Import(string fileName) { }
        [System.CLSCompliantAttribute(false)]
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public override void Import(string fileName, System.Security.SecureString? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        [System.ObsoleteAttribute("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId="SYSLIB0026", UrlFormat="https://aka.ms/dotnet-warnings/{0}")]
        public override void Import(string fileName, string? password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags keyStorageFlags) { }
        public bool MatchesHostname(string hostname, bool allowWildcards = true, bool allowCommonName = true) { throw null; }
        public override void Reset() { }
        public override string ToString() { throw null; }
        public override string ToString(bool verbose) { throw null; }
        public bool TryExportCertificatePem(System.Span<char> destination, out int charsWritten) { throw null; }
        public bool Verify() { throw null; }
    }
    public partial class X509Certificate2Collection : System.Security.Cryptography.X509Certificates.X509CertificateCollection, System.Collections.Generic.IEnumerable<System.Security.Cryptography.X509Certificates.X509Certificate2>, System.Collections.IEnumerable
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
        public string ExportCertificatePems() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public string ExportPkcs7Pem() { throw null; }
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
        System.Collections.Generic.IEnumerator<System.Security.Cryptography.X509Certificates.X509Certificate2> System.Collections.Generic.IEnumerable<System.Security.Cryptography.X509Certificates.X509Certificate2>.GetEnumerator() { throw null; }
        public bool TryExportCertificatePems(System.Span<char> destination, out int charsWritten) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public bool TryExportPkcs7Pem(System.Span<char> destination, out int charsWritten) { throw null; }
    }
    public sealed partial class X509Certificate2Enumerator : System.Collections.Generic.IEnumerator<System.Security.Cryptography.X509Certificates.X509Certificate2>, System.Collections.IEnumerator, System.IDisposable
    {
        internal X509Certificate2Enumerator() { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
        bool System.Collections.IEnumerator.MoveNext() { throw null; }
        void System.Collections.IEnumerator.Reset() { }
        void System.IDisposable.Dispose() { }
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
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
    public sealed partial class X509ChainElementCollection : System.Collections.Generic.IEnumerable<System.Security.Cryptography.X509Certificates.X509ChainElement>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal X509ChainElementCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509ChainElement this[int index] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public void CopyTo(System.Security.Cryptography.X509Certificates.X509ChainElement[] array, int index) { }
        public System.Security.Cryptography.X509Certificates.X509ChainElementEnumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Security.Cryptography.X509Certificates.X509ChainElement> System.Collections.Generic.IEnumerable<System.Security.Cryptography.X509Certificates.X509ChainElement>.GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class X509ChainElementEnumerator : System.Collections.Generic.IEnumerator<System.Security.Cryptography.X509Certificates.X509ChainElement>, System.Collections.IEnumerator, System.IDisposable
    {
        internal X509ChainElementEnumerator() { }
        public System.Security.Cryptography.X509Certificates.X509ChainElement Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
        void System.IDisposable.Dispose() { }
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
        public bool VerificationTimeIgnored { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509ChainPolicy Clone() { throw null; }
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
    public sealed partial class X509ExtensionCollection : System.Collections.Generic.IEnumerable<System.Security.Cryptography.X509Certificates.X509Extension>, System.Collections.ICollection, System.Collections.IEnumerable
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
        System.Collections.Generic.IEnumerator<System.Security.Cryptography.X509Certificates.X509Extension> System.Collections.Generic.IEnumerable<System.Security.Cryptography.X509Certificates.X509Extension>.GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class X509ExtensionEnumerator : System.Collections.Generic.IEnumerator<System.Security.Cryptography.X509Certificates.X509Extension>, System.Collections.IEnumerator, System.IDisposable
    {
        internal X509ExtensionEnumerator() { }
        public System.Security.Cryptography.X509Certificates.X509Extension Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
        void System.IDisposable.Dispose() { }
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
    public enum X509RevocationReason
    {
        Unspecified = 0,
        KeyCompromise = 1,
        CACompromise = 2,
        AffiliationChanged = 3,
        Superseded = 4,
        CessationOfOperation = 5,
        CertificateHold = 6,
        RemoveFromCrl = 8,
        PrivilegeWithdrawn = 9,
        AACompromise = 10,
        WeakAlgorithmOrKey = 11,
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
    public sealed partial class X509SubjectAlternativeNameExtension : System.Security.Cryptography.X509Certificates.X509Extension
    {
        public X509SubjectAlternativeNameExtension() { }
        public X509SubjectAlternativeNameExtension(byte[] rawData, bool critical = false) { }
        public X509SubjectAlternativeNameExtension(System.ReadOnlySpan<byte> rawData, bool critical = false) { }
        public override void CopyFrom(System.Security.Cryptography.AsnEncodedData asnEncodedData) { }
        public System.Collections.Generic.IEnumerable<string> EnumerateDnsNames() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Net.IPAddress> EnumerateIPAddresses() { throw null; }
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
        public System.ReadOnlyMemory<byte> SubjectKeyIdentifierBytes { get { throw null; } }
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
