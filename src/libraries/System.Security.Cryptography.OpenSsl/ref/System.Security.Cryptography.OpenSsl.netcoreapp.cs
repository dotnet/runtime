// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography
{
    public sealed partial class DSAOpenSsl : System.Security.Cryptography.DSA
    {
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
    }
    public sealed partial class ECDiffieHellmanOpenSsl : System.Security.Cryptography.ECDiffieHellman
    {
        public ECDiffieHellmanOpenSsl() { }
        public ECDiffieHellmanOpenSsl(int keySize) { }
        public ECDiffieHellmanOpenSsl(System.IntPtr handle) { }
        public ECDiffieHellmanOpenSsl(System.Security.Cryptography.ECCurve curve) { }
        public ECDiffieHellmanOpenSsl(System.Security.Cryptography.SafeEvpPKeyHandle pkeyHandle) { }
        public override System.Security.Cryptography.ECDiffieHellmanPublicKey PublicKey { get { throw null; } }
        public override byte[] DeriveKeyFromHash(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[]? secretPrepend, byte[]? secretAppend) { throw null; }
        public override byte[] DeriveKeyFromHmac(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[]? hmacKey, byte[]? secretPrepend, byte[]? secretAppend) { throw null; }
        public override byte[] DeriveKeyMaterial(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey) { throw null; }
        public override byte[] DeriveKeyTls(System.Security.Cryptography.ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed) { throw null; }
        public System.Security.Cryptography.SafeEvpPKeyHandle DuplicateKeyHandle() { throw null; }
        public override System.Security.Cryptography.ECParameters ExportExplicitParameters(bool includePrivateParameters) { throw null; }
        public override System.Security.Cryptography.ECParameters ExportParameters(bool includePrivateParameters) { throw null; }
        public override void GenerateKey(System.Security.Cryptography.ECCurve curve) { }
        public override void ImportParameters(System.Security.Cryptography.ECParameters parameters) { }
    }
    public sealed partial class SafeEvpPKeyHandle : System.Runtime.InteropServices.SafeHandle
    {
        public static long OpenSslVersion { get { throw null; } }
    }
}
