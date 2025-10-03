// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Test.Cryptography;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSACngProvider : IRSAProvider
    {
        public RSA Create() => new RSACng();

        public RSA Create(int keySize) => new RSACng(keySize);

        public bool Supports384PrivateKey => PlatformSupport.IsRSA384Supported;

        public bool SupportsLargeExponent => true;

        public bool SupportsSha2Oaep => true;

        public bool SupportsPss => true;

        public bool SupportsSha1Signatures => true;

        public bool SupportsMd5Signatures => true;

        public bool SupportsSha3 { get; } = SHA3_256.IsSupported; // If SHA3_256 is supported, assume 384 and 512 are, too.
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new RSACngProvider();
    }
}
