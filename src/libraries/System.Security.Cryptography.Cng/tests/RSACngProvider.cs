// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSACngProvider : IRSAProvider
    {
        public RSA Create()
        {
            return new RSACng();
        }

        public RSA Create(int keySize)
        {
            return new RSACng(keySize);
        }
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new RSACngProvider();
    }
}
