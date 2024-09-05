// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.CodeSign.Requirements
{
    public enum ExpressionOperation : int
    {
        False,
        True,
        Ident,
        AppleAnchor,
        AnchorHash,
        InfoKeyValue,
        And,
        Or,
        CDHash,
        Not,
        InfoKeyField,
        CertField,
        TrustedCert,
        TrustedCerts,
        CertGeneric,
        AppleGenericAnchor,
        EntitlementField,
        CertPolicy,
        NamedAnchor,
        NamedCode,
        Platform,
        Notarized,
        CertFieldDate,
        LegacyDevID,
    }
}
