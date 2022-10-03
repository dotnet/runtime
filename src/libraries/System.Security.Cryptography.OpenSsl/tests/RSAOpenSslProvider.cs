// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSAOpenSslProvider : IRSAProvider
    {
        private bool? _supportsSha1Signatures;

        public RSA Create() => new RSAOpenSsl();

        public RSA Create(int keySize) => new RSAOpenSsl(keySize);

        public bool Supports384PrivateKey => true;

        public bool SupportsLargeExponent => true;

        public bool SupportsSha2Oaep => true;

        public bool SupportsPss => true;

        public bool SupportsSha1Signatures => _supportsSha1Signatures ??= SignatureSupport.CanProduceSha1Signature(Create());
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new RSAOpenSslProvider();
    }
}
