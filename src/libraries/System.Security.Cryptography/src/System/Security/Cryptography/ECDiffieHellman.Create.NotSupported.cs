// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public partial class ECDiffieHellman : AsymmetricAlgorithm
    {
        public static new partial ECDiffieHellman Create()
        {
            throw new PlatformNotSupportedException();
        }

        public static partial ECDiffieHellman Create(ECCurve curve)
        {
            throw new PlatformNotSupportedException();
        }

        public static partial ECDiffieHellman Create(ECParameters parameters)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
