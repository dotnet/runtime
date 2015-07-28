// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography.Rsa.Tests
{
    public interface IRSAProvider
    {
        RSA Create();
        RSA Create(int keySize);
    }

    public static partial class RSAFactory
    {
        public static RSA Create()
        {
            return s_provider.Create();
        }

        public static RSA Create(int keySize)
        {
            return s_provider.Create(keySize);
        }
    }
}
