// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    // https://tools.ietf.org/html/rfc5280#section-4.2.1.13
    //
    // ReasonFlags ::= BIT STRING {
    //     unused                  (0),
    //     keyCompromise           (1),
    //     cACompromise            (2),
    //     affiliationChanged      (3),
    //     superseded              (4),
    //     cessationOfOperation    (5),
    //     certificateHold         (6),
    //     privilegeWithdrawn      (7),
    //     aACompromise            (8)
    // }
    [Flags]
    internal enum ReasonFlagsAsn
    {
        Unused = 1 << 0,
        KeyCompromise = 1 << 1,
        CACompromise = 1 << 2,
        AffiliationChanged = 1 << 3,
        Superseded = 1 << 4,
        CessationOfOperation = 1 << 5,
        CertificateHold = 1 << 6,
        PrivilegeWithdrawn = 1 << 7,
        AACompromise = 1 << 8,
    }
}
