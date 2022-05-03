// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public sealed partial class RSACryptoServiceProvider : RSA, ICspAsymmetricAlgorithm
    {
        [UnsupportedOSPlatform("browser")]
        public RSACryptoServiceProvider()
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public RSACryptoServiceProvider(int dwKeySize)
        {
            throw new PlatformNotSupportedException();
        }

        [SupportedOSPlatform("windows")]
        public RSACryptoServiceProvider(int dwKeySize, CspParameters parameters) =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CspParameters)));

        [SupportedOSPlatform("windows")]
        public RSACryptoServiceProvider(CspParameters parameters) =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CspParameters)));

        [SupportedOSPlatform("windows")]
        public CspKeyContainerInfo CspKeyContainerInfo => default!;

        public byte[] Decrypt(byte[] rgb, bool fOAEP) => default!;
        public byte[] Encrypt(byte[] rgb, bool fOAEP) => default!;
        public byte[] ExportCspBlob(bool includePrivateParameters) => default!;
        public override RSAParameters ExportParameters(bool includePrivateParameters) => default;
        public void ImportCspBlob(byte[] keyBlob) { }
        public override void ImportParameters(RSAParameters parameters) { }
        public bool PersistKeyInCsp { get; set; }
        public bool PublicOnly => default;
        public byte[] SignData(byte[] buffer, object halg) => default!;
        public byte[] SignData(byte[] buffer, int offset, int count, object halg) => default!;
        public byte[] SignData(Stream inputStream, object halg) => default!;
        public byte[] SignHash(byte[] rgbHash, string str) => default!;
        public bool VerifyData(byte[] buffer, object halg, byte[] signature) => default;
        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature) => default;

        // UseMachineKeyStore has no effect in non-Windows
        public static bool UseMachineKeyStore { get; set; }
    }
}
