// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Test.Cryptography;

namespace System.Security.Cryptography.Rsa.Tests
{
    public sealed class RSACngProvider : RSAProvider
    {
        public static readonly RSACngProvider Instance = new RSACngProvider();

        private RSACngProvider() { }

        public override RSA Create() => new RSACng();

        public override RSA Create(int keySize) => new RSACng(keySize);

        public override bool Supports384PrivateKey => PlatformSupport.IsRSA384Supported;

        public override bool SupportsLargeExponent => true;

        public override bool SupportsSha2Oaep => true;

        public override bool SupportsPss => true;

        public override bool SupportsSha1Signatures => true;

        public override bool SupportsMd5Signatures => true;

        public override bool SupportsSha3 { get; } = SHA3_256.IsSupported; // If SHA3_256 is supported, assume 384 and 512 are, too.
    }
}
