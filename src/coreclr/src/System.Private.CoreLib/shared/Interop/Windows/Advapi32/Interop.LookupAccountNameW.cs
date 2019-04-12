// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        internal static extern bool LookupAccountNameW(
            string? lpSystemName,
            ref char lpAccountName,
            ref byte Sid,
            ref uint cbSid,
            ref char ReferencedDomainName,
            ref uint cchReferencedDomainName,
            out uint peUse);
    }
}
