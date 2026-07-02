// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;

namespace System.Security.Cryptography.Rsa.Tests
{
    public sealed class RSAOpenSslProvider : RSAProvider
    {
        public static readonly RSAOpenSslProvider Instance = new RSAOpenSslProvider();

        private bool? _supportsSha1Signatures;
        private bool? _supportsMd5Signatures;

        private RSAOpenSslProvider() { }

        public override RSA Create() => new RSAOpenSsl();

        public override RSA Create(int keySize) => new RSAOpenSsl(keySize);

        public override bool Supports384PrivateKey => true;

        public override bool SupportsLargeExponent => true;

        public override bool SupportsSha2Oaep => true;

        public override bool SupportsPss => true;

        public override bool SupportsSha1Signatures => _supportsSha1Signatures ??= SignatureSupport.CanProduceSha1Signature(Create());
        public override bool SupportsMd5Signatures => _supportsMd5Signatures ??= SignatureSupport.CanProduceMd5Signature(Create());

        public override bool SupportsSha3 => SHA3_256.IsSupported; // If SHA3_256 is supported, assume 384 and 512 are, too.
    }
}
