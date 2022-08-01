// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Tests;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSACryptoServiceProviderProvider : IRSAProvider
    {
        private bool? _supportsSha1Signatures;

        public RSA Create() => new RSACryptoServiceProvider();

        public RSA Create(int keySize) => new RSACryptoServiceProvider(keySize);

        public bool Supports384PrivateKey => true;

        public bool SupportsLargeExponent => false;

        public bool SupportsSha2Oaep => false;

        public bool SupportsPss => false;

        public bool SupportsSha1Signatures => _supportsSha1Signatures ??= SignatureSupport.CanProduceSha1Signature(Create());
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new RSACryptoServiceProviderProvider();
    }
}
