// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSACryptoServiceProviderProvider : IRSAProvider
    {
        public RSA Create() => new RSACryptoServiceProvider();

        public RSA Create(int keySize) => new RSACryptoServiceProvider(keySize);

        public bool Supports384PrivateKey => true;

        public bool SupportsSha2Oaep => false;

        public bool SupportsDecryptingIntoExactSpaceRequired => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new RSACryptoServiceProviderProvider();
    }
}
