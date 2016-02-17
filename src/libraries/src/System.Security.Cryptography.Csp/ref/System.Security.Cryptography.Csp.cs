// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Security.Cryptography
{
    public sealed partial class CspKeyContainerInfo
    {
        public CspKeyContainerInfo(System.Security.Cryptography.CspParameters parameters) { }
        public bool Accessible { get { return default(bool); } }
        public bool Exportable { get { return default(bool); } }
        public bool HardwareDevice { get { return default(bool); } }
        public string KeyContainerName { get { return default(string); } }
        public System.Security.Cryptography.KeyNumber KeyNumber { get { return default(System.Security.Cryptography.KeyNumber); } }
        public bool MachineKeyStore { get { return default(bool); } }
        public bool Protected { get { return default(bool); } }
        public string ProviderName { get { return default(string); } }
        public int ProviderType { get { return default(int); } }
        public bool RandomlyGenerated { get { return default(bool); } }
        public bool Removable { get { return default(bool); } }
        public string UniqueKeyContainerName { get { return default(string); } }
    }
    public sealed partial class CspParameters
    {
        public string KeyContainerName;
        public int KeyNumber;
        public string ProviderName;
        public int ProviderType;
        public CspParameters() { }
        public CspParameters(int dwTypeIn) { }
        public CspParameters(int dwTypeIn, string strProviderNameIn) { }
        public CspParameters(int dwTypeIn, string strProviderNameIn, string strContainerNameIn) { }
        public System.Security.Cryptography.CspProviderFlags Flags { get { return default(System.Security.Cryptography.CspProviderFlags); } set { } }
        public System.IntPtr ParentWindowHandle { get { return default(System.IntPtr); } set { } }
    }
    [System.FlagsAttribute]
    public enum CspProviderFlags
    {
        CreateEphemeralKey = 128,
        NoFlags = 0,
        NoPrompt = 64,
        UseArchivableKey = 16,
        UseDefaultKeyContainer = 2,
        UseExistingKey = 8,
        UseMachineKeyStore = 1,
        UseNonExportableKey = 4,
        UseUserProtectedKey = 32,
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
    public sealed partial class RSACryptoServiceProvider : System.Security.Cryptography.RSA, System.Security.Cryptography.ICspAsymmetricAlgorithm
    {
        public RSACryptoServiceProvider() { }
        public RSACryptoServiceProvider(int dwKeySize) { }
        public RSACryptoServiceProvider(int dwKeySize, System.Security.Cryptography.CspParameters parameters) { }
        public RSACryptoServiceProvider(System.Security.Cryptography.CspParameters parameters) { }
        public System.Security.Cryptography.CspKeyContainerInfo CspKeyContainerInfo { get { return default(System.Security.Cryptography.CspKeyContainerInfo); } }
        public override int KeySize { get { return default(int); } }
        public bool PersistKeyInCsp { get { return default(bool); } set { } }
        public bool PublicOnly { get { return default(bool); } }
        public static bool UseMachineKeyStore { get { return default(bool); } set { } }
        public byte[] Decrypt(byte[] rgb, bool fOAEP) { return default(byte[]); }
        public override byte[] Decrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { return default(byte[]); }
        protected override void Dispose(bool disposing) { }
        public byte[] Encrypt(byte[] rgb, bool fOAEP) { return default(byte[]); }
        public override byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding) { return default(byte[]); }
        public byte[] ExportCspBlob(bool includePrivateParameters) { return default(byte[]); }
        public override System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters) { return default(System.Security.Cryptography.RSAParameters); }
        protected override byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { return default(byte[]); }
        protected override byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { return default(byte[]); }
        public void ImportCspBlob(byte[] keyBlob) { }
        public override void ImportParameters(System.Security.Cryptography.RSAParameters parameters) { }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { return default(System.Security.Cryptography.KeySizes[]); } }
        public byte[] SignData(byte[] buffer, int offset, int count, object halg) { return default(byte[]); }
        public byte[] SignData(byte[] buffer, object halg) { return default(byte[]); }
        public byte[] SignData(System.IO.Stream inputStream, object halg) { return default(byte[]); }
        public override byte[] SignHash(byte[] hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { return default(byte[]); }
        public byte[] SignHash(byte[] rgbHash, string str) { return default(byte[]); }
        public bool VerifyData(byte[] buffer, object halg, byte[] signature) { return default(bool); }
        public override bool VerifyHash(byte[] hash, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { return default(bool); }
        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature) { return default(bool); }
    }
}
