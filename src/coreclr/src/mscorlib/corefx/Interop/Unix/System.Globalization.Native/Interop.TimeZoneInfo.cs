// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class GlobalizationInterop
    {
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Ansi, EntryPoint = "GlobalizationNative_ReadLink")] // readlink requires char*
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReadLink(string filePath, [Out] StringBuilder result, uint resultCapacity);
    }
}
