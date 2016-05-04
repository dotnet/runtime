// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    /// <summary>
    /// Represents the public and private key of the specified elliptic curve.
    /// </summary>
    public struct ECParameters
    {
        /// <summary>
        /// Public point.
        /// </summary>
        public ECPoint Q;

        /// <summary>
        /// Private Key. Not always present.
        /// </summary>
        public byte[] D;

        /// <summary>
        /// The Curve.
        /// </summary>
        public ECCurve Curve;

        /// <summary>
        /// Validate the current object.
        /// </summary>
        /// <exception cref="CryptographicException">
        ///     if the key or curve parameters are not valid for the current CurveType.
        /// </exception>
        public void Validate()
        {
            bool hasErrors = false;

            if (Q.X == null ||
                Q.Y == null ||
                Q.X.Length != Q.Y.Length)
            {
                hasErrors = true;
            }
            
            if (!hasErrors)
            {
                if (Curve.IsExplicit)
                {
                    // Explicit curves require D length to match Curve.Order
                    hasErrors = (D != null && (D.Length != Curve.Order.Length));
                }
                else if (Curve.IsNamed)
                {
                    // Named curves require D length to match Q.X and Q.Y
                    hasErrors = (D != null && (D.Length != Q.X.Length));
                }
            }

            if (hasErrors)
            {
                throw new CryptographicException(SR.Cryptography_InvalidCurveKeyParameters);
            }

            Curve.Validate();
        }
    }
}
