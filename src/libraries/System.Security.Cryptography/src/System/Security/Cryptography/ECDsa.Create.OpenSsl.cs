// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public partial class ECDsa : ECAlgorithm
    {
        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        public static new partial ECDsa Create()
        {
            return new ECDsaWrapper(new ECDsaOpenSsl());
        }

        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        /// <param name="curve">
        /// The <see cref="ECCurve"/> representing the elliptic curve.
        /// </param>
        public static partial ECDsa Create(ECCurve curve)
        {
            return new ECDsaWrapper(new ECDsaOpenSsl(curve));
        }

        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        /// <param name="parameters">
        /// The <see cref="ECParameters"/> representing the elliptic curve parameters.
        /// </param>
        public static partial ECDsa Create(ECParameters parameters)
        {
            ECDsa ec = new ECDsaOpenSsl();

            try
            {
                ec.ImportParameters(parameters);
                return new ECDsaWrapper(ec);
            }
            catch
            {
                ec.Dispose();
                throw;
            }
        }
    }
}
