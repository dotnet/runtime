// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Security.Cryptography
{
    public sealed partial class IncrementalHash : System.IDisposable
    {
        public void AppendData(System.ReadOnlySpan<byte> data) { }
        public bool TryGetHashAndReset(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public abstract partial class RandomNumberGenerator : System.IDisposable
    {
        public virtual void GetBytes(System.Span<byte> data) { }
        public virtual void GetNonZeroBytes(System.Span<byte> data) { }
    }
    public abstract partial class RSA : System.Security.Cryptography.AsymmetricAlgorithm
    {
        public virtual bool TryDecrypt(System.ReadOnlySpan<byte> source, System.Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten) { throw null; }
        public virtual bool TryEncrypt(System.ReadOnlySpan<byte> source, System.Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten) { throw null; }
        protected virtual bool TryHashData(System.ReadOnlySpan<byte> source, System.Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) { throw null; }
        public virtual bool TrySignData(System.ReadOnlySpan<byte> source, System.Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten) { throw null; }
        public virtual bool TrySignHash(System.ReadOnlySpan<byte> source, System.Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten) { throw null; }
        public virtual bool VerifyData(System.ReadOnlySpan<byte> data, System.ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) { throw null; }
        public virtual bool VerifyHash(System.ReadOnlySpan<byte> hash, System.ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) { throw null; }
    }
}
