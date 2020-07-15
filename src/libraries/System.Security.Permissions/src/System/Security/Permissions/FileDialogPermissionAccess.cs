// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
    [Flags]
    public enum FileDialogPermissionAccess
    {
        None = 0,
        Open = 1,
        OpenSave = 3,
        Save = 2,
    }
}
