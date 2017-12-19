// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-10.2.2
    //
    // CertificateChoices ::= CHOICE {
    //   certificate Certificate,
    //   extendedCertificate[0] IMPLICIT ExtendedCertificate, -- Obsolete
    //   v1AttrCert[1] IMPLICIT AttributeCertificateV1,       -- Obsolete
    //   v2AttrCert[2] IMPLICIT AttributeCertificateV2,
    //   other[3] IMPLICIT OtherCertificateFormat }
    //
    // OtherCertificateFormat ::= SEQUENCE {
    //   otherCertFormat OBJECT IDENTIFIER,
    //   otherCert ANY DEFINED BY otherCertFormat }
    //
    // Except we only support public key certificates, so just trim the choice here.
    [StructLayout(LayoutKind.Sequential)]
    [Choice]
    internal struct CertificateChoiceAsn
    {
        [ExpectedTag(TagClass.Universal, (int)UniversalTagNumber.Sequence)]
        [AnyValue]
        public ReadOnlyMemory<byte>? Certificate;
    }
}
