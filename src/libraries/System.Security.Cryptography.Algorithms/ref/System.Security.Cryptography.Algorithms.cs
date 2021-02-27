// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography
{
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class Aes : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected Aes() { }
        public static new System.Security.Cryptography.Aes Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static new System.Security.Cryptography.Aes? Create(string algorithmName) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public sealed partial class AesCcm : System.IDisposable
    {
        public AesCcm(byte[] key) { }
        public AesCcm(System.ReadOnlySpan<byte> key) { }
        public static System.Security.Cryptography.KeySizes NonceByteSizes { get { throw null; } }
        public static System.Security.Cryptography.KeySizes TagByteSizes { get { throw null; } }
        public void Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext, byte[]? associatedData = null) { }
        public void Decrypt(System.ReadOnlySpan<byte> nonce, System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> tag, System.Span<byte> plaintext, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { }
        public void Dispose() { }
        public void Encrypt(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag, byte[]? associatedData = null) { }
        public void Encrypt(System.ReadOnlySpan<byte> nonce, System.ReadOnlySpan<byte> plaintext, System.Span<byte> ciphertext, System.Span<byte> tag, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public sealed partial class AesGcm : System.IDisposable
    {
        public AesGcm(byte[] key) { }
        public AesGcm(System.ReadOnlySpan<byte> key) { }
        public static System.Security.Cryptography.KeySizes NonceByteSizes { get { throw null; } }
        public static System.Security.Cryptography.KeySizes TagByteSizes { get { throw null; } }
        public void Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext, byte[]? associatedData = null) { }
        public void Decrypt(System.ReadOnlySpan<byte> nonce, System.ReadOnlySpan<byte> ciphertext, System.ReadOnlySpan<byte> tag, System.Span<byte> plaintext, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { }
        public void Dispose() { }
        public void Encrypt(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag, byte[]? associatedData = null) { }
        public void Encrypt(System.ReadOnlySpan<byte> nonce, System.ReadOnlySpan<byte> plaintext, System.Span<byte> ciphertext, System.Span<byte> tag, System.ReadOnlySpan<byte> associatedData = default(System.ReadOnlySpan<byte>)) { }
    }
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class AsymmetricKeyExchangeDeformatter
    {
        protected AsymmetricKeyExchangeDeformatter() { }
        public abstract string? Parameters { get; set; }
        public abstract byte[] DecryptKeyExchange(byte[] rgb);
        public abstract void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key);
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class AsymmetricSignatureDeformatter
    {
        protected AsymmetricSignatureDeformatter() { }
        public abstract void SetHashAlgorithm(string strName);
        public abstract void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key);
        public abstract bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);
        public virtual bool VerifySignature(System.Security.Cryptography.HashAlgorithm hash, byte[] rgbSignature) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class AsymmetricSignatureFormatter
    {
        protected AsymmetricSignatureFormatter() { }
        public abstract byte[] CreateSignature(byte[] rgbHash);
        public virtual byte[] CreateSignature(System.Security.Cryptography.HashAlgorithm hash) { throw null; }
        public abstract void SetHashAlgorithm(string strName);
        public abstract void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key);
    }
    public partial class CryptoConfig
    {
        public CryptoConfig() { }
        public static bool AllowOnlyFipsAlgorithms { get { throw null; } }
        public static void AddAlgorithm(System.Type algorithm, params string[] names) { }
        public static void AddOID(string oid, params string[] names) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static object? CreateFromName(string name) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static object? CreateFromName(string name, params object?[]? args) { throw null; }
        public static byte[] EncodeOID(string str) { throw null; }
        public static string? MapNameToOID(string name) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class DeriveBytes : System.IDisposable
    {
        protected DeriveBytes() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract byte[] GetBytes(int cb);
        public abstract void Reset();
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class DES : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected DES() { }
        public override byte[] Key { get { throw null; } set { } }
        public static new System.Security.Cryptography.DES Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static new System.Security.Cryptography.DES? Create(string algName) { throw null; }
        public static bool IsSemiWeakKey(byte[] rgbKey) { throw null; }
        public static bool IsWeakKey(byte[] rgbKey) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class DSA : System.Security.Cryptography.AsymmetricAlgorithm
    {
        protected DSA() { }
        public static new System.Security.Cryptography.DSA Create() { throw null; }
        public static System.Security.Cryptography.DSA Create(int keySizeInBits) { throw null; }
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class DSASignatureFormatter : System.Security.Cryptography.AsymmetricSignatureFormatter
    {
        public DSASignatureFormatter() { }
        public DSASignatureFormatter(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public override byte[] CreateSignature(byte[] rgbHash) { throw null; }
        public override void SetHashAlgorithm(string strName) { }
        public override void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key) { }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class ECDiffieHellman : System.Security.Cryptography.AsymmetricAlgorithm
    {
        protected ECDiffieHellman() { }
        public override string KeyExchangeAlgorithm { get { throw null; } }
        public abstract System.Security.Cryptography.ECDiffieHellmanPublicKey PublicKey { get; }
        public override string? SignatureAlgorithm { get { throw null; } }
        public static new System.Security.Cryptography.ECDiffieHellman Create() { throw null; }
        public static System.Security.Cryptography.ECDiffieHellman Create(System.Security.Cryptography.ECCurve curve) { throw null; }
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class ECDsa : System.Security.Cryptography.AsymmetricAlgorithm
    {
        protected ECDsa() { }
        public override string? KeyExchangeAlgorithm { get { throw null; } }
        public override string SignatureAlgorithm { get { throw null; } }
        public static new System.Security.Cryptography.ECDsa Create() { throw null; }
        public static System.Security.Cryptography.ECDsa Create(System.Security.Cryptography.ECCurve curve) { throw null; }
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class HMACMD5 : System.Security.Cryptography.HMAC
    {
        public HMACMD5() { }
        public HMACMD5(byte[] key) { }
        public override byte[] Key { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class HMACSHA1 : System.Security.Cryptography.HMAC
    {
        public HMACSHA1() { }
        public HMACSHA1(byte[] key) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public HMACSHA1(byte[] key, bool useManagedSha1) { }
        public override byte[] Key { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
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
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class HMACSHA384 : System.Security.Cryptography.HMAC
    {
        public HMACSHA384() { }
        public HMACSHA384(byte[] key) { }
        public override byte[] Key { get { throw null; } set { } }
        public bool ProduceLegacyHmacValues { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class HMACSHA512 : System.Security.Cryptography.HMAC
    {
        public HMACSHA512() { }
        public HMACSHA512(byte[] key) { }
        public override byte[] Key { get { throw null; } set { } }
        public bool ProduceLegacyHmacValues { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { }
        protected override byte[] HashFinal() { throw null; }
        public override void Initialize() { }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw null; }
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class RC2 : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected int EffectiveKeySizeValue;
        protected RC2() { }
        public virtual int EffectiveKeySize { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class Rijndael : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected Rijndael() { }
        public static new System.Security.Cryptography.Rijndael Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static new System.Security.Cryptography.Rijndael? Create(string algName) { throw null; }
    }
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class RSA : System.Security.Cryptography.AsymmetricAlgorithm
    {
        protected RSA() { }
        public override string? KeyExchangeAlgorithm { get { throw null; } }
        public override string SignatureAlgorithm { get { throw null; } }
        public static new System.Security.Cryptography.RSA Create() { throw null; }
        public static System.Security.Cryptography.RSA Create(int keySizeInBits) { throw null; }
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
        public override bool Equals(object? obj) { throw null; }
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class RSAOAEPKeyExchangeDeformatter : System.Security.Cryptography.AsymmetricKeyExchangeDeformatter
    {
        public RSAOAEPKeyExchangeDeformatter() { }
        public RSAOAEPKeyExchangeDeformatter(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public override string? Parameters { get { throw null; } set { } }
        public override byte[] DecryptKeyExchange(byte[] rgbData) { throw null; }
        public override void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key) { }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public partial class RSAPKCS1KeyExchangeDeformatter : System.Security.Cryptography.AsymmetricKeyExchangeDeformatter
    {
        public RSAPKCS1KeyExchangeDeformatter() { }
        public RSAPKCS1KeyExchangeDeformatter(System.Security.Cryptography.AsymmetricAlgorithm key) { }
        public override string? Parameters { get { throw null; } set { } }
        public System.Security.Cryptography.RandomNumberGenerator? RNG { get { throw null; } set { } }
        public override byte[] DecryptKeyExchange(byte[] rgbIn) { throw null; }
        public override void SetKey(System.Security.Cryptography.AsymmetricAlgorithm key) { }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public sealed partial class RSASignaturePadding : System.IEquatable<System.Security.Cryptography.RSASignaturePadding>
    {
        internal RSASignaturePadding() { }
        public System.Security.Cryptography.RSASignaturePaddingMode Mode { get { throw null; } }
        public static System.Security.Cryptography.RSASignaturePadding Pkcs1 { get { throw null; } }
        public static System.Security.Cryptography.RSASignaturePadding Pss { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Security.Cryptography.RSASignaturePadding? other) { throw null; }
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
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
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public abstract partial class TripleDES : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected TripleDES() { }
        public override byte[] Key { get { throw null; } set { } }
        public static new System.Security.Cryptography.TripleDES Create() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static new System.Security.Cryptography.TripleDES? Create(string str) { throw null; }
        public static bool IsWeakKey(byte[] rgbKey) { throw null; }
    }
}
