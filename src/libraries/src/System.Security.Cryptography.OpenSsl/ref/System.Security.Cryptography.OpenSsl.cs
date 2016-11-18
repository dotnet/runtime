// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography
{
    public sealed partial class DSAOpenSsl : System.Security.Cryptography.DSA
    {
        public DSAOpenSsl() { }
        public DSAOpenSsl(int keySize) { }
        public DSAOpenSsl(System.IntPtr handle) { }
        public DSAOpenSsl(System.Security.Cryptography.DSAParameters parameters) { }
        public DSAOpenSsl(System.Security.Cryptography.SafeEvpPKeyHandle pkeyHandle) { }
        public override int KeySize { set { } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public override byte[] CreateSignature(byte[] rgbHash) { throw null; }
        protected override void Dispose(bool disposing) { }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateKeyHandle() { throw null; }
        public override System.Security.Cryptography.DSAParameters ExportParameters(bool includePrivateParameters) { throw null; }
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.DSAParameters parameters) { }
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) { throw null; }
    }
    public sealed partial class ECDsaOpenSsl : System.Security.Cryptography.ECDsa
    {
        public ECDsaOpenSsl() { }
        public ECDsaOpenSsl(int keySize) { }
        public ECDsaOpenSsl(System.IntPtr handle) { }
        public ECDsaOpenSsl(System.Security.Cryptography.ECCurve curve) { }
        public ECDsaOpenSsl(System.Security.Cryptography.SafeEvpPKeyHandle pkeyHandle) { }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateKeyHandle() { throw null; }
        public override System.Security.Cryptography.ECParameters ExportExplicitParameters(bool includePrivateParameters) { throw null; }
        public override System.Security.Cryptography.ECParameters ExportParameters(bool includePrivateParameters) { throw null; }
        public override void GenerateKey(System.Security.Cryptography.ECCurve curve) { }
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.ECParameters parameters) { }
        public override byte[] SignHash(byte[] hash) { throw null; }
        public override bool VerifyHash(byte[] hash, byte[] signature) { throw null; }
    }
    public sealed partial class RSAOpenSsl : System.Security.Cryptography.RSA
    {
        public RSAOpenSsl() { }
        public RSAOpenSsl(int keySize) { }
        public RSAOpenSsl(System.IntPtr handle) { }
        public RSAOpenSsl(System.Security.Cryptography.RSAParameters parameters) { }
        public RSAOpenSsl(System.Security.Cryptography.SafeEvpPKeyHandle pkeyHandle) { }
        public override int KeySize { set { } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw null; } }
        public override byte[] Decrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw null; }
        protected override void Dispose(bool disposing) { }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateKeyHandle() { throw null; }
        public override byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw null; }
        public override System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters) { throw null; }
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public override void ImportParameters(System.Security.Cryptography.RSAParameters parameters) { }
        public override byte[] SignHash(byte[] hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
        public override bool VerifyHash(byte[] hash, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw null; }
    }
    public sealed partial class SafeEvpPKeyHandle : System.Runtime.InteropServices.SafeHandle
    {
        public SafeEvpPKeyHandle(System.IntPtr handle, bool ownsHandle) : base (default(System.IntPtr), default(bool)) { }
        public override bool IsInvalid { get { throw null; } }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateHandle() { throw null; }
        protected override bool ReleaseHandle() { throw null; }
    }
}
