// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [Serializable]
    [Flags]
    public enum TypeDescriptorPermissionFlags
    {
        NoFlags = 0,
        RestrictedRegistrationAccess = 1,
    }
}
