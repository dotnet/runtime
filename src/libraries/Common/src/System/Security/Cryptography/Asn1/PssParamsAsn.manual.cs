// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.Asn1
{
    internal ref partial struct ValuePssParamsAsn
    {
        internal RSASignaturePadding GetSignaturePadding(int? digestValueLength = null)
        {
            if (TrailerField != 1)
            {
                throw new CryptographicException(SR.Cryptography_Pkcs_InvalidSignatureParameters);
            }

            if (MaskGenAlgorithm.Algorithm != Oids.Mgf1)
            {
                throw new CryptographicException(
                    SR.Cryptography_Pkcs_PssParametersMgfNotSupported,
                    MaskGenAlgorithm.Algorithm);
            }

            if (!MaskGenAlgorithm.HasParameters)
            {
                throw new CryptographicException(SR.Cryptography_Pkcs_InvalidSignatureParameters);
            }

            ValueAlgorithmIdentifierAsn.Decode(
                MaskGenAlgorithm.Parameters,
                AsnEncodingRules.DER,
                out ValueAlgorithmIdentifierAsn mgfParams);

            if (mgfParams.Algorithm != HashAlgorithm.Algorithm)
            {
                throw new CryptographicException(
                    SR.Format(
                        SR.Cryptography_Pkcs_PssParametersMgfHashMismatch,
                        mgfParams.Algorithm,
                        HashAlgorithm.Algorithm));
            }

            int saltSize = digestValueLength.GetValueOrDefault();

            if (!digestValueLength.HasValue)
            {
                saltSize = Helpers.HashOidToByteLength(HashAlgorithm.Algorithm);
            }

#if NET11_0_OR_GREATER
            return SaltLength == saltSize ?
                RSASignaturePadding.Pss :
                RSASignaturePadding.CreatePss(SaltLength);
#else
            if (SaltLength != saltSize)
            {
                throw new CryptographicException(
                    SR.Format(
                        SR.Cryptography_Pkcs_PssParametersSaltMismatch,
                        SaltLength,
                        HashAlgorithm.Algorithm));
            }

            return RSASignaturePadding.Pss;
#endif
        }
    }
}
