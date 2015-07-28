// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSACryptoServiceProviderProvider : IRSAProvider
    {
        public RSA Create()
        {
            return new RSACryptoServiceProvider();
        }

        public RSA Create(int keySize)
        {
            return new RSACryptoServiceProvider(keySize);
        }
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new RSACryptoServiceProviderProvider();
    }
}
