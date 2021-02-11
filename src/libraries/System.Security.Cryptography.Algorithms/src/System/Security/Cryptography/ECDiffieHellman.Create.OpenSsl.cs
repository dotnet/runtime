// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public abstract partial class ECDiffieHellman : AsymmetricAlgorithm
    {
        public static new ECDiffieHellman Create()
        {
            return new ECDiffieHellmanImplementation.ECDiffieHellmanOpenSsl();
        }

        public static ECDiffieHellman Create(ECCurve curve)
        {
            return new ECDiffieHellmanImplementation.ECDiffieHellmanOpenSsl(curve);
        }

        public static ECDiffieHellman Create(ECParameters parameters)
        {
            ECDiffieHellman ecdh = new ECDiffieHellmanImplementation.ECDiffieHellmanOpenSsl();

            try
            {
                ecdh.ImportParameters(parameters);
                return ecdh;
            }
            catch
            {
                ecdh.Dispose();
                throw;
            }
        }
    }
}
