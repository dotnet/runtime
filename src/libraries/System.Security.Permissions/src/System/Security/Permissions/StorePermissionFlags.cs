// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
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
