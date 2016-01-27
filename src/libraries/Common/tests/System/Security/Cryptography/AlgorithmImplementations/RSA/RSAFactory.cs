// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
