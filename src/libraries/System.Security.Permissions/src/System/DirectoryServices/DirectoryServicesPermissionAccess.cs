// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.DirectoryServices
{
    [Flags]
    public enum DirectoryServicesPermissionAccess
    {
        None = 0,
        Browse = 2,
        Write = 6
    }
}
