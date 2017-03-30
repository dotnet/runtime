// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [Flags]
    public enum StorePermissionFlags
    {
        NoFlags = 0x00,

        CreateStore = 0x01,
        DeleteStore = 0x02,
        EnumerateStores = 0x04,

        OpenStore = 0x10,
        AddToStore = 0x20,
        RemoveFromStore = 0x40,
        EnumerateCertificates = 0x80,

        AllFlags = 0xF7
    }
}
