// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Security.Cryptography
{
    public abstract partial class AsymmetricAlgorithm : System.IDisposable
    {
        protected int KeySizeValue;
        protected System.Security.Cryptography.KeySizes[] LegalKeySizesValue;
        protected AsymmetricAlgorithm() { }
        public virtual int KeySize { get { throw null; } set { } }
        public virtual System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    public enum CipherMode
    {
        CBC = 1,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        CFB = 4,
        CTS = 5,
        ECB = 2,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        OFB = 3,
    }
    public partial class CryptographicException : System.SystemException
    {
        public CryptographicException() { }
        public CryptographicException(int hr) { }
        protected CryptographicException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CryptographicException(string message) { }
        public CryptographicException(string message, System.Exception inner) { }
        public CryptographicException(string format, string insert) { }
    }
    public partial class CryptographicUnexpectedOperationException : System.Security.Cryptography.CryptographicException
    {
        public CryptographicUnexpectedOperationException() { }
        protected CryptographicUnexpectedOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CryptographicUnexpectedOperationException(string message) { }
        public CryptographicUnexpectedOperationException(string message, System.Exception inner) { }
        public CryptographicUnexpectedOperationException(string format, string insert) { }
    }
    public partial class CryptoStream : System.IO.Stream, System.IDisposable
    {
        public CryptoStream(System.IO.Stream stream, System.Security.Cryptography.ICryptoTransform transform, System.Security.Cryptography.CryptoStreamMode mode) { }
#if netcoreapp11
        public CryptoStream(System.IO.Stream stream, System.Security.Cryptography.ICryptoTransform transform, System.Security.Cryptography.CryptoStreamMode mode, bool leaveOpen) { }
#endif
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public bool HasFlushedFinalBlock { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public void FlushFinalBlock() { }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override void WriteByte(byte value) { }
    }
    public enum CryptoStreamMode
    {
        Read = 0,
        Write = 1,
    }
    public abstract partial class HashAlgorithm : System.IDisposable, System.Security.Cryptography.ICryptoTransform
    {
        protected internal byte[] HashValue;
        protected int State;
        protected HashAlgorithm() { }
        public virtual bool CanReuseTransform { get { throw null; } }
        public virtual bool CanTransformMultipleBlocks { get { throw null; } }
        public virtual byte[] Hash { get { throw null; } }
        public virtual int HashSize { get { throw null; } }
        public virtual int InputBlockSize { get { throw null; } }
        public virtual int OutputBlockSize { get { throw null; } }
        public byte[] ComputeHash(byte[] buffer) { throw null; }
        public byte[] ComputeHash(byte[] buffer, int offset, int count) { throw null; }
        public byte[] ComputeHash(System.IO.Stream inputStream) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected abstract void HashCore(byte[] array, int ibStart, int cbSize);
        protected abstract byte[] HashFinal();
        public abstract void Initialize();
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset) { throw null; }
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct HashAlgorithmName : System.IEquatable<System.Security.Cryptography.HashAlgorithmName>
    {
        public HashAlgorithmName(string name) { throw null; }
        public static System.Security.Cryptography.HashAlgorithmName MD5 { get { throw null; } }
        public string Name { get { throw null; } }
        public static System.Security.Cryptography.HashAlgorithmName SHA1 { get { throw null; } }
        public static System.Security.Cryptography.HashAlgorithmName SHA256 { get { throw null; } }
        public static System.Security.Cryptography.HashAlgorithmName SHA384 { get { throw null; } }
        public static System.Security.Cryptography.HashAlgorithmName SHA512 { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Security.Cryptography.HashAlgorithmName other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.Cryptography.HashAlgorithmName left, System.Security.Cryptography.HashAlgorithmName right) { throw null; }
        public static bool operator !=(System.Security.Cryptography.HashAlgorithmName left, System.Security.Cryptography.HashAlgorithmName right) { throw null; }
        public override string ToString() { throw null; }
    }
    public abstract partial class HMAC : System.Security.Cryptography.KeyedHashAlgorithm
    {
        protected HMAC() { }
        public string HashName { get { throw null; } set { } }
        public override byte[] Key { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
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
    }
    public abstract partial class SymmetricAlgorithm : System.IDisposable
    {
        protected int BlockSizeValue;
        protected byte[] IVValue;
        protected int KeySizeValue;
        protected byte[] KeyValue;
        protected System.Security.Cryptography.KeySizes[] LegalBlockSizesValue;
        protected System.Security.Cryptography.KeySizes[] LegalKeySizesValue;
        protected System.Security.Cryptography.CipherMode ModeValue;
        protected System.Security.Cryptography.PaddingMode PaddingValue;
        protected SymmetricAlgorithm() { }
        public virtual int BlockSize { get { throw null; } set { } }
        public virtual byte[] IV { get { throw null; } set { } }
        public virtual byte[] Key { get { throw null; } set { } }
        public virtual int KeySize { get { throw null; } set { } }
        public virtual System.Security.Cryptography.KeySizes[] LegalBlockSizes { get { throw null; } }
        public virtual System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public virtual System.Security.Cryptography.CipherMode Mode { get { throw null; } set { } }
        public virtual System.Security.Cryptography.PaddingMode Padding { get { throw null; } set { } }
        public virtual System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public abstract System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV);
        public virtual System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public abstract System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV);
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract void GenerateIV();
        public abstract void GenerateKey();
    }
}
