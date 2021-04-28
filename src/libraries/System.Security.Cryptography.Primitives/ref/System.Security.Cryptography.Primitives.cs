// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography
{
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
        public override void CopyTo(System.IO.Stream destination, int bufferSize) { throw null; }
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
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public override void WriteByte(byte value) { }
    }
    public enum CryptoStreamMode
    {
        Read = 0,
        Write = 1,
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
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract void GenerateIV();
        public abstract void GenerateKey();
        public int GetCiphertextLengthCbc(int plaintextLength, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.PKCS7) { throw null; }
        public int GetCiphertextLengthCfb(int plaintextLength, System.Security.Cryptography.PaddingMode paddingMode = System.Security.Cryptography.PaddingMode.None, int feedbackSizeInBits = 8) { throw null; }
        public int GetCiphertextLengthEcb(int plaintextLength, System.Security.Cryptography.PaddingMode paddingMode) { throw null; }
        public bool ValidKeySize(int bitLength) { throw null; }
    }
}
