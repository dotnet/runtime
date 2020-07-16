// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NETCOREAPP // This file is compiled into both System.Private.CoreLib (NetCoreApp) and System.Security.Permission (netstandard2.0)
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public enum PermissionState
    {
        None = 0,
        Unrestricted = 1,
    }
}
