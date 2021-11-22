// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [GeneratedDllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
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
