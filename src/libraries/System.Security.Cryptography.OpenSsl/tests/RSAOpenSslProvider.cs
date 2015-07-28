// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSAOpenSslProvider : IRSAProvider
    {
        public RSA Create()
        {
            return new RSAOpenSsl();
        }

        public RSA Create(int keySize)
        {
            return new RSAOpenSsl(keySize);
        }
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new RSAOpenSslProvider();
    }
}
