// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaCng : ECDsa
    {
        private CngAlgorithmCore _core;

        /// <summary>
        ///     Creates a new ECDsaCng object that will use the specified key. The key's
        ///     <see cref="CngKey.AlgorithmGroup" /> must be ECDsa. This constructor
        ///     creates a copy of the key. Hence, the caller can safely dispose of the 
        ///     passed in key and continue using the ECDsaCng object. 
        /// </summary>
        /// <param name="key">Key to use for ECDsa operations</param>
        /// <exception cref="ArgumentException">if <paramref name="key" /> is not an ECDsa key</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="key" /> is null.</exception>
        public ECDsaCng(CngKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (!IsEccAlgorithmGroup(key.AlgorithmGroup))
                throw new ArgumentException(SR.Cryptography_ArgECDsaRequiresECDsaKey, nameof(key));

            Key = CngAlgorithmCore.Duplicate(key);
        }

        protected override void Dispose(bool disposing)
        {
            _core.Dispose();
        }

        private void DisposeKey()
        {
            _core.DisposeKey();
        }

        private static bool IsEccAlgorithmGroup(CngAlgorithmGroup algorithmGroup)
        {
            // Sometimes, when reading from certificates, ECDSA keys get identified as ECDH.
            // Windows allows the ECDH keys to perform both key exchange (ECDH) and signing (ECDSA),
            // so either value is acceptable for the ECDSA wrapper object.
            //
            // It is worth noting, however, that ECDSA-identified keys cannot be used for key exchange (ECDH) in CNG.
            return algorithmGroup == CngAlgorithmGroup.ECDsa || algorithmGroup == CngAlgorithmGroup.ECDiffieHellman;
        }

        internal string GetCurveName()
        {
            return Key.GetCurveName();
        }

        private void ImportFullKeyBlob(byte[] ecfullKeyBlob, bool includePrivateParameters)
        {
#if !uap
            Key = ECCng.ImportFullKeyBlob(ecfullKeyBlob, includePrivateParameters);
#endif // !uap
        }

        private void ImportKeyBlob(byte[] ecfullKeyBlob, string curveName, bool includePrivateParameters)
        {
#if !uap
            Key = ECCng.ImportKeyBlob(ecfullKeyBlob, curveName, includePrivateParameters);
#endif // !uap
        }

        private byte[] ExportKeyBlob(bool includePrivateParameters)
        {
#if uap
            return null;
#else
            return ECCng.ExportKeyBlob(Key, includePrivateParameters);
#endif // uap
        }

        private byte[] ExportFullKeyBlob(bool includePrivateParameters)
        {
#if uap
            return null;
#else
            return ECCng.ExportFullKeyBlob(Key, includePrivateParameters);
#endif // uap
        }
    }
}
