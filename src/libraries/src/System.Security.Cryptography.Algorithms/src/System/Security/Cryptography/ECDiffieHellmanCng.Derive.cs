// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal static partial class ECDiffieHellmanImplementation
    {
        public sealed partial class ECDiffieHellmanCng : ECDiffieHellman
        {
            // For the public ECDiffieHellmanCng this is exposed as the HashAlgorithm property
            // which is a CngAlgorithm type. We're not doing that, but we do need the default value
            // for DeriveKeyMaterial.
            public override byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey)
            {
                if (otherPartyPublicKey == null)
                {
                    throw new ArgumentNullException(nameof(otherPartyPublicKey));
                }

                // ECDiffieHellmanCng on .NET Framework will throw an ArgumentException in this method
                // if otherPartyPublicKey is not an ECDiffieHellmanCngPublicKey.  All of the other methods
                // will use Import/Export to coerce the correct type for interop.

                // None of the other Core types will match that behavior, so the ECDiffieHellman.Create() on
                // Windows on .NET Core won't, either.
                
                // The default behavior for ECDiffieHellmanCng / ECDiffieHellman.Create() on .NET Framework was
                // to derive from hash, no prepend, no append, SHA-2-256.
                return DeriveKeyFromHash(otherPartyPublicKey, HashAlgorithmName.SHA256);
            }

            private SafeNCryptSecretHandle DeriveSecretAgreementHandle(ECDiffieHellmanPublicKey otherPartyPublicKey)
            {
                if (otherPartyPublicKey == null)
                {
                    throw new ArgumentNullException(nameof(otherPartyPublicKey));
                }

                ECParameters otherPartyParameters = otherPartyPublicKey.ExportParameters();

                using (ECDiffieHellmanCng otherPartyCng = (ECDiffieHellmanCng)Create(otherPartyParameters))
                using (SafeNCryptKeyHandle otherPartyHandle = otherPartyCng.GetDuplicatedKeyHandle())
                {
                    string importedKeyAlgorithmGroup =
                        CngKeyLite.GetPropertyAsString(
                            otherPartyHandle,
                            CngKeyLite.KeyPropertyName.AlgorithmGroup,
                            CngPropertyOptions.None);

                    if (importedKeyAlgorithmGroup != BCryptNative.AlgorithmName.ECDH)
                    {
                        throw new ArgumentException(SR.Cryptography_ArgECDHRequiresECDHKey, nameof(otherPartyPublicKey));
                    }

                    if (CngKeyLite.GetKeyLength(otherPartyHandle) != KeySize)
                    {
                        throw new ArgumentException(SR.Cryptography_ArgECDHKeySizeMismatch, nameof(otherPartyPublicKey));
                    }

                    using (SafeNCryptKeyHandle localHandle = GetDuplicatedKeyHandle())
                    {
                        return Interop.NCrypt.DeriveSecretAgreement(localHandle, otherPartyHandle);
                    }
                }
            }
        }
    }
}
