// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.EcDsa.Tests
{
    public partial class ECDsaProvider : IECDsaProvider
    {
        public ECDsa Create()
        {
            return ECDsa.Create();
        }

        public ECDsa Create(int keySize)
        {
            ECDsa ec = Create();
            ec.KeySize = keySize;
            return ec;
        }

#if netcoreapp
        public ECDsa Create(ECCurve curve)
        {
            return ECDsa.Create(curve);
        }
#endif
    }

    public partial class ECDsaFactory
    {
        private static readonly IECDsaProvider s_provider = new ECDsaProvider();
    }

}

