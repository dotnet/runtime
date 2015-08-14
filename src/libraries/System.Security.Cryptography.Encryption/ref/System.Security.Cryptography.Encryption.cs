// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Security.Cryptography
{
    public abstract partial class AsymmetricAlgorithm : System.IDisposable
    {
        protected AsymmetricAlgorithm() { }
        public virtual int KeySize { get { return default(int); } set { } }
        public virtual System.Security.Cryptography.KeySizes[] LegalKeySizes { get { return default(System.Security.Cryptography.KeySizes[]); } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    public enum CipherMode
    {
        CBC = 1,
        CTS = 5,
        ECB = 2,
    }
    public partial class CryptographicException : System.Exception
    {
        public CryptographicException() { }
        public CryptographicException(int hr) { }
        public CryptographicException(string message) { }
        public CryptographicException(string message, System.Exception inner) { }
        public CryptographicException(string format, string insert) { }
    }
    public partial class CryptoStream : System.IO.Stream, System.IDisposable
    {
        public CryptoStream(System.IO.Stream stream, System.Security.Cryptography.ICryptoTransform transform, System.Security.Cryptography.CryptoStreamMode mode) { }
        public override bool CanRead { get { return default(bool); } }
        public override bool CanSeek { get { return default(bool); } }
        public override bool CanWrite { get { return default(bool); } }
        public bool HasFlushedFinalBlock { get { return default(bool); } }
        public override long Length { get { return default(long); } }
        public override long Position { get { return default(long); } set { } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public void FlushFinalBlock() { }
        public override int Read(byte[] buffer, int offset, int count) { buffer = default(byte[]); return default(int); }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { return default(long); }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
    }
    public enum CryptoStreamMode
    {
        Read = 0,
        Write = 1,
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
    public sealed partial class KeySizes
    {
        public KeySizes(int minSize, int maxSize, int skipSize) { }
        public int MaxSize { get { return default(int); } }
        public int MinSize { get { return default(int); } }
        public int SkipSize { get { return default(int); } }
    }
    public enum PaddingMode
    {
        None = 1,
        PKCS7 = 2,
        Zeros = 3,
    }
    public abstract partial class SymmetricAlgorithm : System.IDisposable
    {
        protected SymmetricAlgorithm() { }
        public virtual int BlockSize { get { return default(int); } set { } }
        public virtual byte[] IV { get { return default(byte[]); } set { } }
        public virtual byte[] Key { get { return default(byte[]); } set { } }
        public virtual int KeySize { get { return default(int); } set { } }
        public virtual System.Security.Cryptography.KeySizes[] LegalBlockSizes { get { return default(System.Security.Cryptography.KeySizes[]); } }
        public virtual System.Security.Cryptography.KeySizes[] LegalKeySizes { get { return default(System.Security.Cryptography.KeySizes[]); } }
        public virtual System.Security.Cryptography.CipherMode Mode { get { return default(System.Security.Cryptography.CipherMode); } set { } }
        public virtual System.Security.Cryptography.PaddingMode Padding { get { return default(System.Security.Cryptography.PaddingMode); } set { } }
        public virtual System.Security.Cryptography.ICryptoTransform CreateDecryptor() { return default(System.Security.Cryptography.ICryptoTransform); }
        public abstract System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV);
        public virtual System.Security.Cryptography.ICryptoTransform CreateEncryptor() { return default(System.Security.Cryptography.ICryptoTransform); }
        public abstract System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV);
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract void GenerateIV();
        public abstract void GenerateKey();
    }
}
