// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    public enum SecurityAction
    {
        Assert = 3,
        Demand = 2,
        Deny = 4,
        InheritanceDemand = 7,
        LinkDemand = 6,
        PermitOnly = 5,
        RequestMinimum = 8,
        RequestOptional = 9,
        RequestRefuse = 10,
    }
}
