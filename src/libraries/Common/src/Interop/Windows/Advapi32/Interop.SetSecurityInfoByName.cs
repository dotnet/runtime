// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Interop.Libraries.Advapi32, EntryPoint = "SetNamedSecurityInfoW",
            SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint SetSecurityInfoByName(
            string name,
            /*DWORD*/ uint objectType,
            /*DWORD*/ uint securityInformation,
            byte[]? owner,
            byte[]? group,
            byte[]? dacl,
            byte[]? sacl);
    }
}
