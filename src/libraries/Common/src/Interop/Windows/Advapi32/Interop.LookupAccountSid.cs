// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [GeneratedDllImport(Interop.Libraries.Advapi32, EntryPoint = "LookupAccountSidW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial int LookupAccountSid(
            string lpSystemName,
            byte[] Sid,
            char* Name,
            ref int cchName,
            char* ReferencedDomainName,
            ref int cchReferencedDomainName,
            out int peUse);
    }
}
