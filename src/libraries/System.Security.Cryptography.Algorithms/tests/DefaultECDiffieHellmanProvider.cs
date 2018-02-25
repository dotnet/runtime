// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanProvider : IECDiffieHellmanProvider
    {
        public ECDiffieHellman Create()
        {
            return ECDiffieHellman.Create();
        }

        public ECDiffieHellman Create(int keySize)
        {
            ECDiffieHellman ec = Create();
            ec.KeySize = keySize;
            return ec;
        }

        public ECDiffieHellman Create(ECCurve curve)
        {
            return ECDiffieHellman.Create(curve);
        }
    }

    public partial class ECDiffieHellmanFactory
    {
        private static readonly IECDiffieHellmanProvider s_provider = new ECDiffieHellmanProvider();
    }
}

