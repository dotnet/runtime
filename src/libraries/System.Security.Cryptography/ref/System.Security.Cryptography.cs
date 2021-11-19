// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography
{
    public abstract partial class Aes : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected Aes() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static new System.Security.Cryptography.Aes Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
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
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId = "SYSLIB0021", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
        [System.ObsoleteAttribute("The default implementation of this cryptography algorithm is not supported", DiagnosticId = "SYSLIB0007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static System.Security.Cryptography.AsymmetricAlgorithm Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
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
        [System.ObsoleteAttribute("EncodeOID is obsolete. Use the ASN.1 functionality provided in System.Formats.Asn1.", DiagnosticId = "SYSLIB0031", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
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
        public static new System.Security.Cryptography.DES? Create(string algName) { throw null; }
        public static bool IsSemiWeakKey(byte[] rgbKey) { throw null; }
        public static bool IsWeakKey(byte[] rgbKey) { throw null; }
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
    public abstract partial class ECDiffieHellman : System.Security.Cryptography.AsymmetricAlgorithm
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
        public static new System.Security.Cryptography.ECDiffieHellman? Create(string algorithm) { throw null; }
        public byte[] DeriveKeyFromHash(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public virtual byte[] DeriveKeyFromHash(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[]? secretPrepend, byte[]? secretAppend) { throw null; }
        public byte[] DeriveKeyFromHmac(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[]? hmacKey) { throw null; }
        public virtual byte[] DeriveKeyFromHmac(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[]? hmacKey, byte[]? secretPrepend, byte[]? secretAppend) { throw null; }
        public virtual byte[] DeriveKeyMaterial(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey) { throw null; }
        public virtual byte[] DeriveKeyTls(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed) { throw null; }
        public virtual byte[] ExportECPrivateKey() { throw null; }
        public virtual System.Security.Cryptography.ECParameters ExportExplicitParameters(bool includePrivateParameters) { throw null; }
        public virtual System.Security.Cryptography.ECParameters ExportParameters(bool includePrivateParameters) { throw null; }
        public override void FromXmlString(string xmlString) { }
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
        public override string ToXmlString(bool includePrivateParameters) { throw null; }
        public virtual bool TryExportECPrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportPkcs8PrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportSubjectPublicKeyInfo(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public abstract partial class ECDiffieHellmanPublicKey : System.IDisposable
    {
        protected ECDiffieHellmanPublicKey() { }
        protected ECDiffieHellmanPublicKey(byte[] keyBlob) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual System.Security.Cryptography.ECParameters ExportExplicitParameters() { throw null; }
        public virtual System.Security.Cryptography.ECParameters ExportParameters() { throw null; }
        public virtual byte[] ExportSubjectPublicKeyInfo() { throw null; }
        public virtual byte[] ToByteArray() { throw null; }
        public virtual string ToXmlString() { throw null; }
        public virtual bool TryExportSubjectPublicKeyInfo(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public abstract partial class ECDsa : System.Security.Cryptography.AsymmetricAlgorithm
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
        public static new System.Security.Cryptography.ECDsa? Create(string algorithm) { throw null; }
        public virtual byte[] ExportECPrivateKey() { throw null; }
        public virtual System.Security.Cryptography.ECParameters ExportExplicitParameters(bool includePrivateParameters) { throw null; }
        public virtual System.Security.Cryptography.ECParameters ExportParameters(bool includePrivateParameters) { throw null; }
        public override void FromXmlString(string xmlString) { }
        public virtual void GenerateKey(System.Security.Cryptography.ECCurve curve) { }
        public int GetMaxSignatureSize(System.Security.Cryptography.DSASignatureFormat signatureFormat) { throw null; }
        protected virtual byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected virtual byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public virtual void ImportECPrivateKey(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportFromEncryptedPem(System.ReadOnlySpan<char> input, System.ReadOnlySpan<byte> passwordBytes) { }
        public override void ImportFromEncryptedPem(System.ReadOnlySpan<char> input, System.ReadOnlySpan<char> password) { }
        public override void ImportFromPem(System.ReadOnlySpan<char> input) { }
        public virtual void ImportParameters(System.Security.Cryptography.ECParameters parameters) { }
        public override void ImportPkcs8PrivateKey(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
        public override void ImportSubjectPublicKeyInfo(System.ReadOnlySpan<byte> source, out int bytesRead) { throw null; }
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
        public virtual bool TryExportECPrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<byte> passwordBytes, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportEncryptedPkcs8PrivateKey(System.ReadOnlySpan<char> password, System.Security.Cryptography.PbeParameters pbeParameters, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportPkcs8PrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public override bool TryExportSubjectPublicKeyInfo(System.Span<byte> destination, out int bytesWritten) { throw null; }
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
        [System.ObsoleteAttribute("The default implementation of this cryptography algorithm is not supported", DiagnosticId = "SYSLIB0007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static System.Security.Cryptography.HashAlgorithm Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
        [System.ObsoleteAttribute("The default implementation of this cryptography algorithm is not supported", DiagnosticId = "SYSLIB0007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.HMAC Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static new System.Security.Cryptography.HMAC? Create(string algorithmName) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class HMACMD5 : System.Security.Cryptography.HMAC
    {
        public HMACMD5() { }
        public HMACMD5(byte[] key) { }
        public override byte[] Key { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        public static byte[] HashData(byte[] key, byte[] source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        public static bool TryHashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class HMACSHA1 : System.Security.Cryptography.HMAC
    {
        public HMACSHA1() { }
        public HMACSHA1(byte[] key) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("HMACSHA1 always uses the algorithm implementation provided by the platform. Use a constructor without the useManagedSha1 parameter.", DiagnosticId = "SYSLIB0030", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public HMACSHA1(byte[] key, bool useManagedSha1) { }
        public override byte[] Key { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        public static byte[] HashData(byte[] key, byte[] source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        public static bool TryHashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class HMACSHA256 : System.Security.Cryptography.HMAC
    {
        public HMACSHA256() { }
        public HMACSHA256(byte[] key) { }
        public override byte[] Key { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        public static byte[] HashData(byte[] key, byte[] source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        public static bool TryHashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class HMACSHA384 : System.Security.Cryptography.HMAC
    {
        public HMACSHA384() { }
        public HMACSHA384(byte[] key) { }
        public override byte[] Key { get { throw null; } set { } }
        [System.ObsoleteAttribute("ProduceLegacyHmacValues is obsolete. Producing legacy HMAC values is not supported.", DiagnosticId = "SYSLIB0029", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public bool ProduceLegacyHmacValues { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        public static byte[] HashData(byte[] key, byte[] source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        public static bool TryHashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class HMACSHA512 : System.Security.Cryptography.HMAC
    {
        public HMACSHA512() { }
        public HMACSHA512(byte[] key) { }
        public override byte[] Key { get { throw null; } set { } }
        [System.ObsoleteAttribute("ProduceLegacyHmacValues is obsolete. Producing legacy HMAC values is not supported.", DiagnosticId = "SYSLIB0029", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public bool ProduceLegacyHmacValues { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        public static byte[] HashData(byte[] key, byte[] source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> key, System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
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
    public sealed partial class IncrementalHash : System.IDisposable
    {
        internal IncrementalHash() { }
        public System.Security.Cryptography.HashAlgorithmName AlgorithmName { get { throw null; } }
        public int HashLengthInBytes { get { throw null; } }
        public void AppendData(byte[] data) { }
        public void AppendData(byte[] data, int offset, int count) { }
        public void AppendData(System.ReadOnlySpan<byte> data) { }
        public static System.Security.Cryptography.IncrementalHash CreateHash(System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.Security.Cryptography.IncrementalHash CreateHMAC(System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[] key) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
        [System.ObsoleteAttribute("The default implementation of this cryptography algorithm is not supported", DiagnosticId = "SYSLIB0007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static new System.Security.Cryptography.KeyedHashAlgorithm Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static new System.Security.Cryptography.KeyedHashAlgorithm? Create(string algName) { throw null; }
        protected override void Dispose(bool disposing) { }
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class MD5 : System.Security.Cryptography.HashAlgorithm
    {
        protected MD5() { }
        public static new System.Security.Cryptography.MD5 Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static new System.Security.Cryptography.MD5? Create(string algName) { throw null; }
        public static byte[] HashData(byte[] source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static bool TryHashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
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
        public static new System.Security.Cryptography.RC2? Create(string AlgName) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class Rfc2898DeriveBytes : System.Security.Cryptography.DeriveBytes
    {
        public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations) { }
        public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public Rfc2898DeriveBytes(string password, byte[] salt) { }
        public Rfc2898DeriveBytes(string password, byte[] salt, int iterations) { }
        public Rfc2898DeriveBytes(string password, byte[] salt, int iterations, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public Rfc2898DeriveBytes(string password, int saltSize) { }
        public Rfc2898DeriveBytes(string password, int saltSize, int iterations) { }
        public Rfc2898DeriveBytes(string password, int saltSize, int iterations, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { }
        public System.Security.Cryptography.HashAlgorithmName HashAlgorithm { get { throw null; } }
        public int IterationCount { get { throw null; } set { } }
        public byte[] Salt { get { throw null; } set { } }
        [System.ObsoleteAttribute("Rfc2898DeriveBytes.CryptDeriveKey is obsolete and is not supported. Use PasswordDeriveBytes.CryptDeriveKey instead.", DiagnosticId = "SYSLIB0033", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
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
    [System.ObsoleteAttribute("The Rijndael and RijndaelManaged types are obsolete. Use Aes instead.", DiagnosticId = "SYSLIB0022", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public abstract partial class Rijndael : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected Rijndael() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static new System.Security.Cryptography.Rijndael Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static new System.Security.Cryptography.Rijndael? Create(string algName) { throw null; }
    }
    [System.ObsoleteAttribute("The Rijndael and RijndaelManaged types are obsolete. Use Aes instead.", DiagnosticId = "SYSLIB0022", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
        public static new System.Security.Cryptography.RSA? Create(string algName) { throw null; }
        public virtual byte[] Decrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw null; }
        public virtual byte[] DecryptValue(byte[] rgb) { throw null; }
        public virtual byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw null; }
        public virtual byte[] EncryptValue(byte[] rgb) { throw null; }
        public abstract System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters);
        public virtual byte[] ExportRSAPrivateKey() { throw null; }
        public virtual byte[] ExportRSAPublicKey() { throw null; }
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
        public virtual bool TryExportRSAPublicKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
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
    public abstract partial class SHA1 : System.Security.Cryptography.HashAlgorithm
    {
        protected SHA1() { }
        public static new System.Security.Cryptography.SHA1 Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static new System.Security.Cryptography.SHA1? Create(string hashName) { throw null; }
        public static byte[] HashData(byte[] source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static bool TryHashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId = "SYSLIB0021", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
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
        protected SHA256() { }
        public static new System.Security.Cryptography.SHA256 Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static new System.Security.Cryptography.SHA256? Create(string hashName) { throw null; }
        public static byte[] HashData(byte[] source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static bool TryHashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId = "SYSLIB0021", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
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
        protected SHA384() { }
        public static new System.Security.Cryptography.SHA384 Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static new System.Security.Cryptography.SHA384? Create(string hashName) { throw null; }
        public static byte[] HashData(byte[] source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static bool TryHashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId = "SYSLIB0021", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
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
        protected SHA512() { }
        public static new System.Security.Cryptography.SHA512 Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static new System.Security.Cryptography.SHA512? Create(string hashName) { throw null; }
        public static byte[] HashData(byte[] source) { throw null; }
        public static byte[] HashData(System.ReadOnlySpan<byte> source) { throw null; }
        public static int HashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static bool TryHashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.ObsoleteAttribute("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId = "SYSLIB0021", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
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
        [System.ObsoleteAttribute("The default implementation of this cryptography algorithm is not supported", DiagnosticId = "SYSLIB0007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static System.Security.Cryptography.SymmetricAlgorithm Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
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
        public static new System.Security.Cryptography.TripleDES? Create(string str) { throw null; }
        public static bool IsWeakKey(byte[] rgbKey) { throw null; }
    }
}
