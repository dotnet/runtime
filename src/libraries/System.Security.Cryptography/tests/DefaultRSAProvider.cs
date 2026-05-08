// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Tests;
using Test.Cryptography;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class DefaultRSAProvider : IRSAProvider
    {
        private bool? _supportsSha1Signatures;
        private bool? _supportsMd5Signatures;

        public RSA Create() => RSA.Create();

        public RSA Create(int keySize)
        {
#if NET
            return RSA.Create(keySize);
#else
            RSA rsa = Create();

            rsa.KeySize = keySize;
            return rsa;
#endif
        }

        public bool Supports384PrivateKey => PlatformSupport.IsRSA384Supported;
        public bool SupportsSha1Signatures => _supportsSha1Signatures ??= SignatureSupport.CanProduceSha1Signature(Create());
        public bool SupportsMd5Signatures => _supportsMd5Signatures ??= SignatureSupport.CanProduceMd5Signature(Create());

        public bool SupportsLargeExponent => true;

        public bool SupportsSha2Oaep { get; } = true;

        public bool SupportsPss { get; } = true;

        public bool SupportsSha3 { get; } = SHA3_256.IsSupported; // If SHA3_256 is supported, assume 384 and 512 are, too.
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new DefaultRSAProvider();
    }
}
