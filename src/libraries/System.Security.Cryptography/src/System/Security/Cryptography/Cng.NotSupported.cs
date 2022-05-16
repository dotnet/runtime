// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Win32.SafeHandles
{
    public abstract partial class SafeNCryptHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SupportedOSPlatform("windows")]
        protected SafeNCryptHandle() : base(default(bool))
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        protected SafeNCryptHandle(IntPtr handle, SafeHandle parentHandle) : base(default(bool))
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        protected override bool ReleaseHandle() => false;

        protected abstract bool ReleaseNativeHandle();
    }
    public sealed partial class SafeNCryptKeyHandle : SafeNCryptHandle
    {
        [SupportedOSPlatform("windows")]
        public SafeNCryptKeyHandle()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public SafeNCryptKeyHandle(IntPtr handle, SafeHandle parentHandle)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        protected override bool ReleaseNativeHandle() => false;
    }
    public sealed partial class SafeNCryptProviderHandle : SafeNCryptHandle
    {
        [SupportedOSPlatform("windows")]
        public SafeNCryptProviderHandle()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        protected override bool ReleaseNativeHandle() => false;
    }
    public sealed partial class SafeNCryptSecretHandle : SafeNCryptHandle
    {
        [SupportedOSPlatform("windows")]
        public SafeNCryptSecretHandle()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        protected override bool ReleaseNativeHandle() => false;
    }
}

namespace System.Security.Cryptography
{
    public sealed partial class AesCng : Aes
    {
        [SupportedOSPlatform("windows")]
        public AesCng()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public AesCng(string keyName)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public AesCng(string keyName, CngProvider provider)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public AesCng(string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        public override void GenerateKey() { }
        public override void GenerateIV() { }
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) => null!;
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) => null!;
    }
    public sealed partial class CngKey : System.IDisposable
    {
        internal CngKey() { }
        public CngAlgorithm Algorithm => null!;
        public CngAlgorithmGroup? AlgorithmGroup => null;
        public CngExportPolicies ExportPolicy => default;
        public SafeNCryptKeyHandle Handle => null!;
        public bool IsEphemeral => false;
        public bool IsMachineKey => false;
        public string? KeyName => null;
        public int KeySize => default;
        public CngKeyUsages KeyUsage => default;
        public IntPtr ParentWindowHandle { get => default; set { } }
        public CngProvider? Provider => null;
        public SafeNCryptProviderHandle ProviderHandle => null!;
        public CngUIPolicy UIPolicy => null!;
        public string? UniqueName => null;
        public void Delete() { }
        public void Dispose() { }
        public byte[] Export(CngKeyBlobFormat format) => null!;
        public CngProperty GetProperty(string name, CngPropertyOptions options) => default;
        public bool HasProperty(string name, CngPropertyOptions options) => false;
        public void SetProperty(CngProperty property) { }

        [SupportedOSPlatform("windows")]
        public static CngKey Create(CngAlgorithm algorithm)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public static CngKey Create(CngAlgorithm algorithm, string? keyName)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public static CngKey Create(CngAlgorithm algorithm, string? keyName, CngKeyCreationParameters? creationParameters)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public static bool Exists(string keyName)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public static bool Exists(string keyName, CngProvider provider)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public static bool Exists(string keyName, CngProvider provider, CngKeyOpenOptions options)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public static CngKey Import(byte[] keyBlob, CngKeyBlobFormat format)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public static CngKey Import(byte[] keyBlob, CngKeyBlobFormat format, CngProvider provider)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public static CngKey Open(SafeNCryptKeyHandle keyHandle, CngKeyHandleOpenOptions keyHandleOpenOptions)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public static CngKey Open(string keyName)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public static CngKey Open(string keyName, CngProvider provider)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public static CngKey Open(string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }
    }
    public sealed partial class DSACng : DSA
    {
        [SupportedOSPlatform("windows")]
        public DSACng()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public DSACng(int keySize)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public DSACng(CngKey key)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        public CngKey Key => null!;
        public override byte[] CreateSignature(byte[] rgbHash) => null!;
        public override DSAParameters ExportParameters(bool includePrivateParameters) => default;
        public override void ImportParameters(DSAParameters parameters) { }
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) => false;
    }
    public sealed partial class ECDiffieHellmanCng : ECDiffieHellman
    {
        [SupportedOSPlatform("windows")]
        public ECDiffieHellmanCng()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public ECDiffieHellmanCng(int keySize)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public ECDiffieHellmanCng(CngKey key)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public ECDiffieHellmanCng(ECCurve curve)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        public CngAlgorithm HashAlgorithm { get => null!; set { } }
        public byte[]? HmacKey { get => null; set { } }
        public CngKey Key => null!;
        public ECDiffieHellmanKeyDerivationFunction KeyDerivationFunction { get => default; set { } }
        public byte[]? Label { get => null; set { } }
        public byte[]? SecretAppend { get => null; set { } }
        public byte[]? SecretPrepend { get => null; set { } }
        public byte[]? Seed { get => null; set { } }
        public bool UseSecretAgreementAsHmacKey => false;
        public SafeNCryptSecretHandle DeriveSecretAgreementHandle(CngKey otherPartyPublicKey) => null!;
        public SafeNCryptSecretHandle DeriveSecretAgreementHandle(ECDiffieHellmanPublicKey otherPartyPublicKey) => null!;
        public byte[] DeriveKeyMaterial(CngKey otherPartyPublicKey) => null!;
        [Obsolete(Obsoletions.EccXmlExportImportMessage, DiagnosticId = Obsoletions.EccXmlExportImportDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void FromXmlString(string xml, ECKeyXmlFormat format) { }
        [Obsolete(Obsoletions.EccXmlExportImportMessage, DiagnosticId = Obsoletions.EccXmlExportImportDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public string ToXmlString(ECKeyXmlFormat format) => null!;
        public override ECDiffieHellmanPublicKey PublicKey => null!;
    }
    public sealed partial class ECDiffieHellmanCngPublicKey : ECDiffieHellmanPublicKey
    {
        internal ECDiffieHellmanCngPublicKey() { }
        public CngKeyBlobFormat BlobFormat => null!;
        protected override void Dispose(bool disposing) { }
        public CngKey Import() => null!;

        [SupportedOSPlatform("windows")]
        public static ECDiffieHellmanPublicKey FromByteArray(byte[] publicKeyBlob, CngKeyBlobFormat format)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [Obsolete(Obsoletions.EccXmlExportImportMessage, DiagnosticId = Obsoletions.EccXmlExportImportDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static ECDiffieHellmanCngPublicKey FromXmlString(string xml)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [Obsolete(Obsoletions.EccXmlExportImportMessage, DiagnosticId = Obsoletions.EccXmlExportImportDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public override string ToXmlString()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }
    }
    public sealed partial class ECDsaCng : ECDsa
    {
        [SupportedOSPlatform("windows")]
        public ECDsaCng()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public ECDsaCng(int keySize)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public ECDsaCng(CngKey key)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public ECDsaCng(ECCurve curve)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        public CngAlgorithm HashAlgorithm { get => null!; set { } }
        public CngKey Key => null!;
        [Obsolete(Obsoletions.EccXmlExportImportMessage, DiagnosticId = Obsoletions.EccXmlExportImportDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void FromXmlString(string xml, ECKeyXmlFormat format) { }
        public byte[] SignData(byte[] data) => null!;
        public byte[] SignData(byte[] data, int offset, int count) => null!;
        public byte[] SignData(System.IO.Stream data) => null!;
        [Obsolete(Obsoletions.EccXmlExportImportMessage, DiagnosticId = Obsoletions.EccXmlExportImportDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public string ToXmlString(ECKeyXmlFormat format) => null!;
        public bool VerifyData(byte[] data, byte[] signature) => false;
        public bool VerifyData(byte[] data, int offset, int count, byte[] signature) => false;
        public bool VerifyData(System.IO.Stream data, byte[] signature) => false;
        public override byte[] SignHash(byte[] hash) => null!;
        public override bool VerifyHash(byte[] hash, byte[] signature) => false;
    }
    public sealed partial class RSACng : RSA
    {
        [SupportedOSPlatform("windows")]
        public RSACng()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public RSACng(int keySize)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public RSACng(CngKey key)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        public CngKey Key => null!;
        public override RSAParameters ExportParameters(bool includePrivateParameters) => default;
        public override void ImportParameters(RSAParameters parameters) { }
    }
    public sealed partial class TripleDESCng : TripleDES
    {
        [SupportedOSPlatform("windows")]
        public TripleDESCng()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public TripleDESCng(string keyName)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public TripleDESCng(string keyName, CngProvider provider)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        [SupportedOSPlatform("windows")]
        public TripleDESCng(string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyCng);
        }

        public override void GenerateKey() { }
        public override void GenerateIV() { }
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) => null!;
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) => null!;
    }
}
