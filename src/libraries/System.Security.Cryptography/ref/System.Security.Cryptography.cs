// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography
{
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
        public virtual byte[] ExportPkcs8PrivateKey() { throw null; }
        public virtual byte[] ExportSubjectPublicKeyInfo() { throw null; }
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
        public virtual bool TryExportPkcs8PrivateKey(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public virtual bool TryExportSubjectPublicKeyInfo(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class AsymmetricKeyExchangeFormatter
    {
        protected AsymmetricKeyExchangeFormatter() { }
        public abstract string? Parameters { get; }
        public abstract byte[] CreateKeyExchange(byte[] data);
        public abstract byte[] CreateKeyExchange(byte[] data, System.Type? symAlgType);
        public abstract void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key);
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
    public partial interface ICryptoTransform : System.IDisposable
    {
        bool CanReuseTransform { get; }
        bool CanTransformMultipleBlocks { get; }
        int InputBlockSize { get; }
        int OutputBlockSize { get; }
        int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset);
        byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);
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
}
