// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [System.FlagsAttribute]
    public enum ReflectionPermissionFlag
    {
        [System.ObsoleteAttribute("This permission has been deprecated. Use PermissionState.Unrestricted to get full access.")]
        AllFlags = 7,
        MemberAccess = 2,
        NoFlags = 0,
        [System.ObsoleteAttribute("This permission is no longer used by the CLR.")]
        ReflectionEmit = 4,
        RestrictedMemberAccess = 8,
        [System.ObsoleteAttribute("This API has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        TypeInformation = 1,
    }
}
