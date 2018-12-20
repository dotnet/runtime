// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class Advapi32
    {
        [DllImport(Interop.Libraries.Advapi32, CharSet = CharSet.Unicode, EntryPoint = "LookupAccountSidW", SetLastError = true)]
        public extern static unsafe int LookupAccountSid(
            string lpSystemName,
            byte[] Sid,
            char* Name,
            ref int cchName,
            char* ReferencedDomainName,
            ref int cchReferencedDomainName,
            out int peUse);
    }
}
