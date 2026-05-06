// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security
{
#if SYSTEM_PRIVATE_CORELIB
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public partial interface IStackWalk
    {
        void Assert();
        void Demand();
        void Deny();
        void PermitOnly();
    }
}
