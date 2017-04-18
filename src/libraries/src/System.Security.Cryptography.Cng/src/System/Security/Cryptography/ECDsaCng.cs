// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaCng : ECDsa
    {
        private CngAlgorithmCore _core;
        private CngAlgorithm _hashAlgorithm;

        /// <summary>
        ///     Hash algorithm to use when generating a signature over arbitrary data
        /// </summary>
        public CngAlgorithm HashAlgorithm
        {
            get
            {
                return _hashAlgorithm;
            }
            set
            {
                _hashAlgorithm = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

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
            Key = ECCng.ImportFullKeyBlob(ecfullKeyBlob, includePrivateParameters);
        }

        private void ImportKeyBlob(byte[] ecfullKeyBlob, string curveName, bool includePrivateParameters)
        {
            Key = ECCng.ImportKeyBlob(ecfullKeyBlob, curveName, includePrivateParameters);
        }

        private byte[] ExportKeyBlob(bool includePrivateParameters)
        {
            return ECCng.ExportKeyBlob(Key, includePrivateParameters);
        }

        private byte[] ExportFullKeyBlob(bool includePrivateParameters)
        {
            return ECCng.ExportFullKeyBlob(Key, includePrivateParameters);
        }

        public void FromXmlString(string xml, ECKeyXmlFormat format)
            => throw new PlatformNotSupportedException();

        public byte[] SignData(byte[] data)
            => SignData(data, new HashAlgorithmName(HashAlgorithm.Algorithm));

        public byte[] SignData(byte[] data, int offset, int count) =>
            SignData(data, offset, count, new HashAlgorithmName(HashAlgorithm.Algorithm));

        public byte[] SignData(Stream data)
            => SignData(data, new HashAlgorithmName(HashAlgorithm.Algorithm));

        public string ToXmlString(ECKeyXmlFormat format)
            => throw new PlatformNotSupportedException();

        public bool VerifyData(byte[] data, byte[] signature)
            => VerifyData(data, signature, new HashAlgorithmName(HashAlgorithm.Algorithm));

        public bool VerifyData(byte[] data, int offset, int count, byte[] signature)
            => VerifyData(data, offset, count, signature, new HashAlgorithmName(HashAlgorithm.Algorithm));

        public bool VerifyData(Stream data, byte[] signature)
            => VerifyData(data, signature, new HashAlgorithmName(HashAlgorithm.Algorithm));
    }
}
