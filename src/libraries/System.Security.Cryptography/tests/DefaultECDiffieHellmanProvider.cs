// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public sealed partial class DefaultECDiffieHellmanProvider : ECDiffieHellmanProvider
    {
        public static readonly DefaultECDiffieHellmanProvider Instance = new DefaultECDiffieHellmanProvider();

        private DefaultECDiffieHellmanProvider() { }

        public override ECDiffieHellman Create()
        {
            return ECDiffieHellman.Create();
        }

        public override ECDiffieHellman Create(int keySize)
        {
            ECDiffieHellman ec = Create();
            ec.KeySize = keySize;
            return ec;
        }

        public override ECDiffieHellman Create(ECCurve curve)
        {
            return ECDiffieHellman.Create(curve);
        }
    }
}
