// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        
        public bool Supports384PrivateKey
        {
            get { return true; }
        }

        public bool SupportsSha2Oaep
        {
            get { return false; }
        }
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new RSACryptoServiceProviderProvider();
    }
}
