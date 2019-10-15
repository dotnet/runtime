// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [Flags]
    public enum ReflectionPermissionFlag
    {
        [Obsolete("This permission has been deprecated. Use PermissionState.Unrestricted to get full access.")]
        AllFlags = 7,
        MemberAccess = 2,
        NoFlags = 0,
        [Obsolete("This permission is no longer used by the CLR.")]
        ReflectionEmit = 4,
        RestrictedMemberAccess = 8,
        [Obsolete("This API has been deprecated. https://go.microsoft.com/fwlink/?linkid=14202")]
        TypeInformation = 1,
    }
}
