// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Tests;
using Test.Cryptography;

namespace System.Security.Cryptography.Rsa.Tests
{
    public sealed class DefaultRSAProvider : RSAProvider
    {
        public static readonly DefaultRSAProvider Instance = new DefaultRSAProvider();

        private bool? _supportsSha1Signatures;
        private bool? _supportsMd5Signatures;

        private DefaultRSAProvider() { }

        public override RSA Create() => RSA.Create();

        public override RSA Create(int keySize)
        {
#if NET
            return RSA.Create(keySize);
#else
            RSA rsa = Create();

            rsa.KeySize = keySize;
            return rsa;
#endif
        }

        public override bool Supports384PrivateKey => PlatformSupport.IsRSA384Supported;
        public override bool SupportsSha1Signatures => _supportsSha1Signatures ??= SignatureSupport.CanProduceSha1Signature(Create());
        public override bool SupportsMd5Signatures => _supportsMd5Signatures ??= SignatureSupport.CanProduceMd5Signature(Create());

        public override bool SupportsLargeExponent => true;

        public override bool SupportsSha2Oaep { get; } = true;

        public override bool SupportsPss { get; } = true;

        public override bool SupportsSha3 { get; } = SHA3_256.IsSupported; // If SHA3_256 is supported, assume 384 and 512 are, too.
    }
}
