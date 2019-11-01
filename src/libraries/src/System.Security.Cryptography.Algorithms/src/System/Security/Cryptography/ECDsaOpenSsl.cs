// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public partial class ECDsa : AsymmetricAlgorithm
    {
        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        public static new ECDsa Create()
        {
            return new ECDsaImplementation.ECDsaOpenSsl();
        }

        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        /// <param name="curve">
        /// The <see cref="ECCurve"/> representing the elliptic curve.
        /// </param>
        public static ECDsa Create(ECCurve curve)
        {
            return new ECDsaImplementation.ECDsaOpenSsl(curve);
        }

        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        /// <param name="parameters">
        /// The <see cref="ECParameters"/> representing the elliptic curve parameters.
        /// </param>
        public static ECDsa Create(ECParameters parameters)
        {
            ECDsa ec = new ECDsaImplementation.ECDsaOpenSsl();
            ec.ImportParameters(parameters);
            return ec;
        }
    }
}
