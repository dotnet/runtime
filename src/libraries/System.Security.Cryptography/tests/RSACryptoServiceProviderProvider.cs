// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Test.Cryptography;

namespace System.Security.Cryptography.Rsa.Tests
{
    public sealed class RSACryptoServiceProviderProvider : RSAProvider
    {
        public static readonly RSACryptoServiceProviderProvider Instance = new RSACryptoServiceProviderProvider();

        private bool? _supportsSha1Signatures;
        private bool? _supportsMd5Signatures;

        private RSACryptoServiceProviderProvider() { }

        public override RSA Create() => new RSACryptoServiceProvider();

        public override RSA Create(int keySize) => new RSACryptoServiceProvider(keySize);

        public override bool Supports384PrivateKey => PlatformSupport.IsRSA384Supported;

        public override bool SupportsLargeExponent => false;

        public override bool SupportsSha2Oaep => false;

        public override bool SupportsPss => false;

        public override bool SupportsSha1Signatures => _supportsSha1Signatures ??= SignatureSupport.CanProduceSha1Signature(Create());
        public override bool SupportsMd5Signatures => _supportsMd5Signatures ??= SignatureSupport.CanProduceMd5Signature(Create());

        public override bool SupportsSha3 => false;
    }
}
