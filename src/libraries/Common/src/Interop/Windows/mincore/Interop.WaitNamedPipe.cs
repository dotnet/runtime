// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
