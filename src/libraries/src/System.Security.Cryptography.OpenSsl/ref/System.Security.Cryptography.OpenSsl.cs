// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaOpenSsl : System.Security.Cryptography.ECDsa
    {
        public ECDsaOpenSsl() { }
        public ECDsaOpenSsl(int keySize) { }
        public ECDsaOpenSsl(System.IntPtr handle) { }
        public ECDsaOpenSsl(System.Security.Cryptography.SafeEvpPKeyHandle pkeyHandle) { }
        public override int KeySize { set { } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { return default(System.Security.Cryptography.KeySizes[]); } }
        protected override void Dispose(bool disposing) { }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateKeyHandle() { return default(System.Security.Cryptography.SafeEvpPKeyHandle); }
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { return default(byte[]); }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { return default(byte[]); }
        public override byte[] SignHash(byte[] hash) { return default(byte[]); }
        public override bool VerifyHash(byte[] hash, byte[] signature) { return default(bool); }
    }
    public sealed partial class RSAOpenSsl : System.Security.Cryptography.RSA
    {
        public RSAOpenSsl() { }
        public RSAOpenSsl(int keySize) { }
        public RSAOpenSsl(System.IntPtr handle) { }
        public RSAOpenSsl(System.Security.Cryptography.RSAParameters parameters) { }
        public RSAOpenSsl(System.Security.Cryptography.SafeEvpPKeyHandle pkeyHandle) { }
        public override int KeySize { set { } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { return default(System.Security.Cryptography.KeySizes[]); } }
        public override byte[] Decrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { return default(byte[]); }
        protected override void Dispose(bool disposing) { }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateKeyHandle() { return default(System.Security.Cryptography.SafeEvpPKeyHandle); }
        public override byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { return default(byte[]); }
        public override System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters) { return default(System.Security.Cryptography.RSAParameters); }
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { return default(byte[]); }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { return default(byte[]); }
        public override void ImportParameters(System.Security.Cryptography.RSAParameters parameters) { }
        public override byte[] SignHash(byte[] hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { return default(byte[]); }
        public override bool VerifyHash(byte[] hash, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { return default(bool); }
    }
    public sealed partial class SafeEvpPKeyHandle : System.Runtime.InteropServices.SafeHandle
    {
        public SafeEvpPKeyHandle(System.IntPtr handle, bool ownsHandle) : base (default(System.IntPtr), default(bool)) { }
        public override bool IsInvalid { get { return default(bool); } }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateHandle() { return default(System.Security.Cryptography.SafeEvpPKeyHandle); }
        protected override bool ReleaseHandle() { return default(bool); }
    }
}
