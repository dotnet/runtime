// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public abstract partial class ECDiffieHellman : AsymmetricAlgorithm
    {
        public static new ECDiffieHellman Create()
        {
            return new ECDiffieHellmanImplementation.ECDiffieHellmanSecurityTransforms();
        }

        public static ECDiffieHellman Create(ECCurve curve)
        {
            ECDiffieHellman ecdh = Create();

            try
            {
                ecdh.GenerateKey(curve);
                return ecdh;
            }
            catch
            {
                ecdh.Dispose();
                throw;
            }
        }

        public static ECDiffieHellman Create(ECParameters parameters)
        {
            ECDiffieHellman ecdh = Create();

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
