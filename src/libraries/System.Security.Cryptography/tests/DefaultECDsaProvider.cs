// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public partial class DefaultECDsaProvider : ECDsaProvider
    {
        public static readonly DefaultECDsaProvider Instance = new DefaultECDsaProvider();

        private DefaultECDsaProvider() { }

        public override ECDsa Create()
        {
            return ECDsa.Create();
        }

        public override ECDsa Create(int keySize)
        {
            ECDsa ec = Create();
            ec.KeySize = keySize;
            return ec;
        }

        public override ECDsa Create(ECCurve curve)
        {
            return ECDsa.Create(curve);
        }
    }
}
