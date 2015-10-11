// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public interface IECDsaProvider
    {
        ECDsa Create();
        ECDsa Create(int keySize);
    }

    public static partial class ECDsaFactory
    {
        public static ECDsa Create()
        {
            return s_provider.Create();
        }

        public static ECDsa Create(int keySize)
        {
            return s_provider.Create(keySize);
        }
    }
}
