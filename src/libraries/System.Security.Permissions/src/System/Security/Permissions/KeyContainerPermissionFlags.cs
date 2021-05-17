// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public enum KeyContainerPermissionFlags
    {
        NoFlags = 0x0000,

        Create = 0x0001,
        Open = 0x0002,
        Delete = 0x0004,

        Import = 0x0010,
        Export = 0x0020,

        Sign = 0x0100,
        Decrypt = 0x0200,

        ViewAcl = 0x1000,
        ChangeAcl = 0x2000,

        AllFlags = 0x3337
    }
}
