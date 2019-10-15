// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    // https://tools.ietf.org/html/rfc5280#section-4.1
    //
    // TBSCertificate  ::=  SEQUENCE  {
    //     version         [0]  Version DEFAULT v1,
    //     serialNumber         CertificateSerialNumber,
    //     signature            AlgorithmIdentifier,
    //     issuer               Name,
    //     validity             Validity,
    //     subject              Name,
    //     subjectPublicKeyInfo SubjectPublicKeyInfo,
    //     issuerUniqueID  [1]  IMPLICIT UniqueIdentifier OPTIONAL,
    //                          -- If present, version MUST be v2 or v3
    //     subjectUniqueID [2]  IMPLICIT UniqueIdentifier OPTIONAL,
    //                          -- If present, version MUST be v2 or v3
    //     extensions      [3]  Extensions OPTIONAL
    //                          -- If present, version MUST be v3 --
    // }
    internal partial struct TbsCertificateAsn
    {
        /// <summary>
        /// Validate semantics by the specified version.
        /// </summary>
        public void ValidateVersion()
        {
            if (Version < 0 || Version > 2)
            {
                throw new CryptographicException();
            }
            if (Version < 1)
            {
                if (IssuerUniqueId.HasValue || SubjectUniqueId.HasValue)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }
            if (Version < 2)
            {
                if (Extensions != null)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }
        }
    }
}
