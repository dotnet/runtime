// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public abstract partial class ECDiffieHellman : ECAlgorithm
    {
        public static new partial ECDiffieHellman Create()
        {
            return new ECDiffieHellmanWrapper(new ECDiffieHellmanOpenSsl());
        }

        public static partial ECDiffieHellman Create(ECCurve curve)
        {
            return new ECDiffieHellmanWrapper(new ECDiffieHellmanOpenSsl(curve));
        }

        public static partial ECDiffieHellman Create(ECParameters parameters)
        {
            ECDiffieHellman ecdh = new ECDiffieHellmanOpenSsl();

            try
            {
                ecdh.ImportParameters(parameters);
                return new ECDiffieHellmanWrapper(ecdh);
            }
            catch
            {
                ecdh.Dispose();
                throw;
            }
        }
    }
}
