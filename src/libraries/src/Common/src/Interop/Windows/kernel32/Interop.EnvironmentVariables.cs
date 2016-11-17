// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int ExpandEnvironmentStringsW(string lpSrc, [Out] StringBuilder lpDst, int nSize);

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetEnvironmentVariableW(string lpName, [Out] StringBuilder lpValue, int size);

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool SetEnvironmentVariableW(string lpName, string lpValue);

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode)]
        internal static extern unsafe char* GetEnvironmentStringsW();

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe bool FreeEnvironmentStringsW(char* pStrings);
    }
}
