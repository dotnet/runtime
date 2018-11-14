// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [Flags]
    public enum DataProtectionPermissionFlags
    {
        NoFlags         = 0x00,

        ProtectData     = 0x01,
        UnprotectData   = 0x02,

        ProtectMemory   = 0x04,
        UnprotectMemory = 0x08,

        AllFlags        = 0x0F
    }
}