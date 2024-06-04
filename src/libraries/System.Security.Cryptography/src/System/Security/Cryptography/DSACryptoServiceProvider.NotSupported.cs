// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed class DSACryptoServiceProvider : DSA, ICspAsymmetricAlgorithm
    {
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public DSACryptoServiceProvider()
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public DSACryptoServiceProvider(int dwKeySize) : base()
        {
            throw new PlatformNotSupportedException();
        }

        [SupportedOSPlatform("windows")]
        public DSACryptoServiceProvider(int dwKeySize, CspParameters parameters)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CspParameters)));
        }

        [SupportedOSPlatform("windows")]
        public DSACryptoServiceProvider(CspParameters parameters)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CspParameters)));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of DSACryptoServiceProvider")]
        public override byte[] CreateSignature(byte[] rgbHash) => default!;

        [SupportedOSPlatform("windows")]
        public CspKeyContainerInfo CspKeyContainerInfo
        {
            get { throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CAPI_Required, nameof(CspKeyContainerInfo))); }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of DSACryptoServiceProvider")]
        public byte[] SignHash(byte[] rgbHash, string str) => default!;

        public byte[] ExportCspBlob(bool includePrivateParameters) => default!;
        public override DSAParameters ExportParameters(bool includePrivateParameters) => default;
        public void ImportCspBlob(byte[] keyBlob) { }
        public override void ImportParameters(DSAParameters parameters) { }
        public bool PersistKeyInCsp { get; set; }
        public bool PublicOnly => default;
        public byte[] SignData(byte[] buffer) => default!;
        public byte[] SignData(byte[] buffer, int offset, int count) => default!;
        public byte[] SignData(Stream inputStream) => default!;
        public bool VerifyData(byte[] rgbData, byte[] rgbSignature) => default;
        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature) => default;
        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) => default;

        // UseMachineKeyStore has no effect in non-Windows
        public static bool UseMachineKeyStore { get; set; }
    }
}
