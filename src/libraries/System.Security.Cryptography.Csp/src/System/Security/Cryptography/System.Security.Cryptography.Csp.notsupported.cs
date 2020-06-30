// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.Cryptography
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class AesCryptoServiceProvider : System.Security.Cryptography.Aes
    {
        public AesCryptoServiceProvider() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override int BlockSize { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override int FeedbackSize { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override byte[] IV { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override byte[] Key { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override int KeySize { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.KeySizes[] LegalBlockSizes { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.CipherMode Mode { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.PaddingMode Padding { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void Dispose(bool disposing) { }
        public override void GenerateIV() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void GenerateKey() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public sealed partial class CspKeyContainerInfo
    {
        public CspKeyContainerInfo(System.Security.Cryptography.CspParameters parameters) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public bool Accessible { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool Exportable { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool HardwareDevice { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public string? KeyContainerName { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.Security.Cryptography.KeyNumber KeyNumber { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool MachineKeyStore { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool Protected { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public string? ProviderName { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public int ProviderType { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool RandomlyGenerated { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool Removable { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public string UniqueKeyContainerName { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
    }
    public sealed partial class CspParameters
    {
        public string? KeyContainerName;
        public int KeyNumber;
        public string? ProviderName;
        public int ProviderType;
        public CspParameters() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public CspParameters(int dwTypeIn) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public CspParameters(int dwTypeIn, string? strProviderNameIn) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public CspParameters(int dwTypeIn, string? strProviderNameIn, string? strContainerNameIn) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.CspProviderFlags Flags { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        [System.CLSCompliantAttribute(false)]
        public System.Security.SecureString? KeyPassword { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public System.IntPtr ParentWindowHandle { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
    }
    [System.FlagsAttribute]
    public enum CspProviderFlags
    {
        NoFlags = 0,
        UseMachineKeyStore = 1,
        UseDefaultKeyContainer = 2,
        UseNonExportableKey = 4,
        UseExistingKey = 8,
        UseArchivableKey = 16,
        UseUserProtectedKey = 32,
        NoPrompt = 64,
        CreateEphemeralKey = 128,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class DESCryptoServiceProvider : System.Security.Cryptography.DES
    {
        public DESCryptoServiceProvider() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void GenerateIV() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void GenerateKey() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public sealed partial class DSACryptoServiceProvider : System.Security.Cryptography.DSA, System.Security.Cryptography.ICspAsymmetricAlgorithm
    {
        public DSACryptoServiceProvider() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public DSACryptoServiceProvider(int dwKeySize) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public DSACryptoServiceProvider(int dwKeySize, System.Security.Cryptography.CspParameters? parameters) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public DSACryptoServiceProvider(System.Security.Cryptography.CspParameters? parameters) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.CspKeyContainerInfo CspKeyContainerInfo { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override string? KeyExchangeAlgorithm { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override int KeySize { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool PersistKeyInCsp { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool PublicOnly { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override string SignatureAlgorithm { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public static bool UseMachineKeyStore { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override byte[] CreateSignature(byte[] rgbHash) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void Dispose(bool disposing) { }
        public byte[] ExportCspBlob(bool includePrivateParameters) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.DSAParameters ExportParameters(bool includePrivateParameters) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void ImportCspBlob(byte[] keyBlob) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void ImportParameters(System.Security.Cryptography.DSAParameters parameters) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[] SignData(byte[] buffer) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[] SignData(byte[] buffer, int offset, int count) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[] SignData(System.IO.Stream inputStream) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[] SignHash(byte[] rgbHash, string? str) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public bool VerifyData(byte[] rgbData, byte[] rgbSignature) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public bool VerifyHash(byte[] rgbHash, string? str, byte[] rgbSignature) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public partial interface ICspAsymmetricAlgorithm
    {
        System.Security.Cryptography.CspKeyContainerInfo CspKeyContainerInfo { get; }
        byte[] ExportCspBlob(bool includePrivateParameters);
        void ImportCspBlob(byte[] rawData);
    }
    public enum KeyNumber
    {
        Exchange = 1,
        Signature = 2,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class MD5CryptoServiceProvider : System.Security.Cryptography.MD5
    {
        public MD5CryptoServiceProvider() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] array, int ibStart, int cbSize) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override byte[] HashFinal() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void Initialize() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class PasswordDeriveBytes : System.Security.Cryptography.DeriveBytes
    {
        public PasswordDeriveBytes(byte[] password, byte[]? salt) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public PasswordDeriveBytes(byte[] password, byte[]? salt, System.Security.Cryptography.CspParameters? cspParams) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public PasswordDeriveBytes(byte[] password, byte[]? salt, string hashName, int iterations) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public PasswordDeriveBytes(byte[] password, byte[]? salt, string hashName, int iterations, System.Security.Cryptography.CspParameters? cspParams) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public PasswordDeriveBytes(string strPassword, byte[]? rgbSalt) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public PasswordDeriveBytes(string strPassword, byte[]? rgbSalt, System.Security.Cryptography.CspParameters? cspParams) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public PasswordDeriveBytes(string strPassword, byte[]? rgbSalt, string strHashName, int iterations) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public PasswordDeriveBytes(string strPassword, byte[]? rgbSalt, string strHashName, int iterations, System.Security.Cryptography.CspParameters? cspParams) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public string HashName { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public int IterationCount { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public byte[]? Salt { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public byte[] CryptDeriveKey(string? algname, string? alghashname, int keySize, byte[] rgbIV) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void Dispose(bool disposing) { }
#pragma warning disable 0809
        [System.ObsoleteAttribute("Rfc2898DeriveBytes replaces PasswordDeriveBytes for deriving key material from a password and is preferred in new applications.")]
        public override byte[] GetBytes(int cb) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
#pragma warning restore 0809
        public override void Reset() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class RC2CryptoServiceProvider : System.Security.Cryptography.RC2
    {
        public RC2CryptoServiceProvider() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override int EffectiveKeySize { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool UseSalt { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void GenerateIV() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void GenerateKey() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class RNGCryptoServiceProvider : System.Security.Cryptography.RandomNumberGenerator
    {
        public RNGCryptoServiceProvider() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public RNGCryptoServiceProvider(byte[] rgb) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public RNGCryptoServiceProvider(System.Security.Cryptography.CspParameters cspParams) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public RNGCryptoServiceProvider(string str) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void Dispose(bool disposing) { }
        public override void GetBytes(byte[] data) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void GetBytes(byte[] data, int offset, int count) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void GetBytes(System.Span<byte> data) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void GetNonZeroBytes(byte[] data) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void GetNonZeroBytes(System.Span<byte> data) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    public sealed partial class RSACryptoServiceProvider : System.Security.Cryptography.RSA, System.Security.Cryptography.ICspAsymmetricAlgorithm
    {
        public RSACryptoServiceProvider() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public RSACryptoServiceProvider(int dwKeySize) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public RSACryptoServiceProvider(int dwKeySize, System.Security.Cryptography.CspParameters? parameters) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public RSACryptoServiceProvider(System.Security.Cryptography.CspParameters? parameters) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public System.Security.Cryptography.CspKeyContainerInfo CspKeyContainerInfo { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override string? KeyExchangeAlgorithm { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override int KeySize { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool PersistKeyInCsp { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public bool PublicOnly { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override string SignatureAlgorithm { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public static bool UseMachineKeyStore { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public byte[] Decrypt(byte[] rgb, bool fOAEP) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override byte[] Decrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override byte[] DecryptValue(byte[] rgb) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void Dispose(bool disposing) { }
        public byte[] Encrypt(byte[] rgb, bool fOAEP) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override byte[] EncryptValue(byte[] rgb) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[] ExportCspBlob(bool includePrivateParameters) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public void ImportCspBlob(byte[] keyBlob) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void ImportParameters(System.Security.Cryptography.RSAParameters parameters) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[] SignData(byte[] buffer, int offset, int count, object halg) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[] SignData(byte[] buffer, object halg) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[] SignData(System.IO.Stream inputStream, object halg) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override byte[] SignHash(byte[] hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public byte[] SignHash(byte[] rgbHash, string? str) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public bool VerifyData(byte[] buffer, object halg, byte[] signature) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override bool VerifyHash(byte[] hash, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class SHA1CryptoServiceProvider : System.Security.Cryptography.SHA1
    {
        public SHA1CryptoServiceProvider() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] array, int ibStart, int cbSize) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override byte[] HashFinal() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void Initialize() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class SHA256CryptoServiceProvider : System.Security.Cryptography.SHA256
    {
        public SHA256CryptoServiceProvider() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] array, int ibStart, int cbSize) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override byte[] HashFinal() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void Initialize() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class SHA384CryptoServiceProvider : System.Security.Cryptography.SHA384
    {
        public SHA384CryptoServiceProvider() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] array, int ibStart, int cbSize) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override byte[] HashFinal() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void Initialize() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class SHA512CryptoServiceProvider : System.Security.Cryptography.SHA512
    {
        public SHA512CryptoServiceProvider() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] array, int ibStart, int cbSize) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void HashCore(System.ReadOnlySpan<byte> source) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override byte[] HashFinal() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void Initialize() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override bool TryHashFinal(System.Span<byte> destination, out int bytesWritten) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class TripleDESCryptoServiceProvider : System.Security.Cryptography.TripleDES
    {
        public TripleDESCryptoServiceProvider() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override int BlockSize { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override int FeedbackSize { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override byte[] IV { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override byte[] Key { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override int KeySize { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.KeySizes[] LegalBlockSizes { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.CipherMode Mode { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.PaddingMode Padding { get { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } set { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        protected override void Dispose(bool disposing) { }
        public override void GenerateIV() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
        public override void GenerateKey() { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported_Browser); }
    }
}
