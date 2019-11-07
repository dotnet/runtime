// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.NativeCrypto
{
    internal static partial class CapiHelper
    {
        // Provider type to use by default for DSS operations.
        internal const int DefaultDssProviderType = (int)ProviderType.PROV_DSS_DH;

        /// <summary>
        /// Check to see if a better CSP than the one requested is available
        /// DSS providers are supersets of each other in the following order:
        ///    1. MS_ENH_DSS_DH_PROV
        ///    2. MS_DEF_DSS_DH_PROV
        ///
        /// This will return the best provider which is a superset of wszProvider,
        /// or NULL if there is no upgrade available on the machine.
        /// </summary>
        /// <param name="dwProvType">provider type</param>
        /// <param name="wszProvider">provider name</param>
        /// <returns>Returns upgrade CSP name</returns>
        public static string UpgradeDSS(int dwProvType, string wszProvider)
        {
            string wszUpgrade = null;
            if (string.Equals(wszProvider, MS_DEF_DSS_DH_PROV, StringComparison.Ordinal))
            {
                SafeProvHandle safeProvHandle;
                // If this is the base DSS/DH provider, see if we can use the enhanced provider instead.
                if (S_OK == AcquireCryptContext(out safeProvHandle,
                    null,
                    MS_ENH_DSS_DH_PROV,
                    dwProvType,
                    (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT))
                {
                    wszUpgrade = MS_ENH_DSS_DH_PROV;
                }
                safeProvHandle.Dispose();
            }
            return wszUpgrade;
        }

        private static void ReverseDsaSignature(byte[] signature, int cbSignature)
        {
            // A DSA signature consists of two 20-byte components, each of which
            // must be reversed in place.
            if (cbSignature != 40)
                throw new CryptographicException(SR.Cryptography_InvalidDSASignatureSize);

            Array.Reverse(signature, 0, 20);
            Array.Reverse(signature, 20, 20);
        }
    }
}
