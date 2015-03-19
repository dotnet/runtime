// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Psapi, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "K32EnumProcesses")]
        internal static extern bool EnumProcesses(int[] processIds, int size, out int needed);
    }
}
