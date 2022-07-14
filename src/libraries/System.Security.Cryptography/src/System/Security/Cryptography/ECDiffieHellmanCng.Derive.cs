// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class ECDiffieHellmanCng : ECDiffieHellman
    {
        public override byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey)
        {
            ArgumentNullException.ThrowIfNull(otherPartyPublicKey);

            if (otherPartyPublicKey is ECDiffieHellmanCngPublicKey otherKey)
            {
                using (CngKey import = otherKey.Import())
                {
                    return DeriveKeyMaterial(import);
                }
            }

            // This deviates from the .NET Framework behavior.  .NET Framework can't handle unknown public
            // key types, but on .NET Core there are automatically two: the public class produced by
            // this class' PublicKey member, and the private class produced by ECDiffieHellman.Create().PublicKey
            //
            // So let's just work.
            ECParameters otherPartyParameters = otherPartyPublicKey.ExportParameters();

            using (ECDiffieHellmanCng otherPartyCng = new ECDiffieHellmanCng())
            {
                otherPartyCng.ImportParameters(otherPartyParameters);

                using (otherKey = (ECDiffieHellmanCngPublicKey)otherPartyCng.PublicKey)
                using (CngKey importedKey = otherKey.Import())
                {
                    return DeriveKeyMaterial(importedKey);
                }
            }
        }

        public byte[] DeriveKeyMaterial(CngKey otherPartyPublicKey)
        {
            ArgumentNullException.ThrowIfNull(otherPartyPublicKey);

            if (otherPartyPublicKey.AlgorithmGroup != CngAlgorithmGroup.ECDiffieHellman)
                throw new ArgumentException(SR.Cryptography_ArgECDHRequiresECDHKey, nameof(otherPartyPublicKey));
            if (otherPartyPublicKey.KeySize != KeySize)
                throw new ArgumentException(SR.Cryptography_ArgECDHKeySizeMismatch, nameof(otherPartyPublicKey));

            // Setting the flag to UseSecretAsHmacKey even when the KDF isn't HMAC, because that's what .NET Framework does.
            Interop.NCrypt.SecretAgreementFlags flags =
                UseSecretAgreementAsHmacKey
                    ? Interop.NCrypt.SecretAgreementFlags.UseSecretAsHmacKey
                    : Interop.NCrypt.SecretAgreementFlags.None;

            using (SafeNCryptSecretHandle handle = DeriveSecretAgreementHandle(otherPartyPublicKey))
            {
                switch (KeyDerivationFunction)
                {
                    case ECDiffieHellmanKeyDerivationFunction.Hash:
                        return Interop.NCrypt.DeriveKeyMaterialHash(
                            handle,
                            HashAlgorithm.Algorithm,
                            _secretPrepend,
                            _secretAppend,
                            flags);
                    case ECDiffieHellmanKeyDerivationFunction.Hmac:
                        return Interop.NCrypt.DeriveKeyMaterialHmac(
                            handle,
                            HashAlgorithm.Algorithm,
                            _hmacKey,
                            _secretPrepend,
                            _secretAppend,
                            flags);
                    case ECDiffieHellmanKeyDerivationFunction.Tls:
                        if (_label == null || _seed == null)
                        {
                            throw new InvalidOperationException(SR.Cryptography_TlsRequiresLabelAndSeed);
                        }

                        return Interop.NCrypt.DeriveKeyMaterialTls(
                            handle,
                            _label,
                            _seed,
                            flags);
                    default:
                        Debug.Fail($"Unknown KDF ({KeyDerivationFunction})");
                        // Match .NET Framework behavior
                        goto case ECDiffieHellmanKeyDerivationFunction.Tls;
                }
            }
        }

        /// <summary>
        ///     Get a handle to the secret agreement generated between two parties
        /// </summary>
        public SafeNCryptSecretHandle DeriveSecretAgreementHandle(ECDiffieHellmanPublicKey otherPartyPublicKey)
        {
            ArgumentNullException.ThrowIfNull(otherPartyPublicKey);

            if (otherPartyPublicKey is ECDiffieHellmanCngPublicKey otherKey)
            {
                using (CngKey importedKey = otherKey.Import())
                {
                    return DeriveSecretAgreementHandle(importedKey);
                }
            }

            ECParameters otherPartyParameters = otherPartyPublicKey.ExportParameters();

            using (ECDiffieHellmanCng otherPartyCng = new ECDiffieHellmanCng())
            {
                otherPartyCng.ImportParameters(otherPartyParameters);

                using (otherKey = (ECDiffieHellmanCngPublicKey)otherPartyCng.PublicKey)
                using (CngKey importedKey = otherKey.Import())
                {
                    return DeriveSecretAgreementHandle(importedKey);
                }
            }
        }

        /// <summary>
        ///     Get a handle to the secret agreement between two parties
        /// </summary>
        public SafeNCryptSecretHandle DeriveSecretAgreementHandle(CngKey otherPartyPublicKey)
        {
            ArgumentNullException.ThrowIfNull(otherPartyPublicKey);

            if (otherPartyPublicKey.AlgorithmGroup != CngAlgorithmGroup.ECDiffieHellman)
                throw new ArgumentException(SR.Cryptography_ArgECDHRequiresECDHKey, nameof(otherPartyPublicKey));
            if (otherPartyPublicKey.KeySize != KeySize)
                throw new ArgumentException(SR.Cryptography_ArgECDHKeySizeMismatch, nameof(otherPartyPublicKey));

            // This looks strange, but the Handle property returns a duplicate so we need to dispose of it when we're done
            using (SafeNCryptKeyHandle localHandle = Key.Handle)
            using (SafeNCryptKeyHandle otherPartyHandle = otherPartyPublicKey.Handle)
            {
                return Interop.NCrypt.DeriveSecretAgreement(localHandle, otherPartyHandle);
            }
        }
    }
}
