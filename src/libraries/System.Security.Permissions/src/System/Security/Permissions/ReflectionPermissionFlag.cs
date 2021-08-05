// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [Flags]
    public enum ReflectionPermissionFlag
    {
        [Obsolete("ReflectionPermissionFlag.AllFlags has been deprecated. Use PermissionState.Unrestricted to get full access.")]
        AllFlags = 7,
        MemberAccess = 2,
        NoFlags = 0,
        [Obsolete("ReflectionPermissionFlag.ReflectionEmit  has been deprecated and is not supported.")]
        ReflectionEmit = 4,
        RestrictedMemberAccess = 8,
        [Obsolete("ReflectionPermissionFlag.TypeInformation has been deprecated and is not supported.")]
        TypeInformation = 1,
    }
}
