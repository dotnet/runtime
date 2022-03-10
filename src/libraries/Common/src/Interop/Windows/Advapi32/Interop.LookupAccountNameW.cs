// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Libraries.Advapi32, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool LookupAccountNameW(
            string? lpSystemName,
            ref char lpAccountName,
            ref byte Sid,
            ref uint cbSid,
            ref char ReferencedDomainName,
            ref uint cchReferencedDomainName,
            out uint peUse);
    }
}
