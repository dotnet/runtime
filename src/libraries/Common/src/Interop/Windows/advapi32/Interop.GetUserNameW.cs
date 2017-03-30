// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class Advapi32
    {
#pragma warning disable BCL0015 // not available on Windows 7
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool GetUserNameW([Out] StringBuilder lpBuffer, ref int lpnSize);
#pragma warning restore BCL0015
    }
}
