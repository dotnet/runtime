// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd317762.aspx
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern int CompareStringOrdinal(
            ref char lpString1,
            int cchCount1,
            ref char lpString2,
            int cchCount2,
            bool bIgnoreCase);
    }
}
