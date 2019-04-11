// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, EntryPoint = "ExpandEnvironmentStringsW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint ExpandEnvironmentStrings(string lpSrc, ref char lpDst, uint nSize);
    }
}
