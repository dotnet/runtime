// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        // https://learn.microsoft.com/windows/win32/api/sddl/nf-sddl-convertstringsidtosidw
        [LibraryImport(Interop.Libraries.Advapi32, EntryPoint = "ConvertStringSidToSidW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial BOOL ConvertStringSidToSid(
            string StringSid,
            out void* Sid);
    }
}
