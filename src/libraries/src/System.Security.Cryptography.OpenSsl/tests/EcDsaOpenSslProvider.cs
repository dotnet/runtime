// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public class ECDsaProvider : IECDsaProvider
    {
        public ECDsa Create()
        {
            return new ECDsaOpenSsl();
        }

        public ECDsa Create(int keySize)
        {
            return new ECDsaOpenSsl(keySize);
        }
    }

    public partial class ECDsaFactory
    {
        private static readonly IECDsaProvider s_provider = new ECDsaProvider();
    }
}
