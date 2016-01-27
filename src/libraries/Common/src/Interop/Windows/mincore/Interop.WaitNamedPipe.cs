// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Pipe, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false, EntryPoint = "WaitNamedPipeW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WaitNamedPipe(string name, int timeout);
    }
}
