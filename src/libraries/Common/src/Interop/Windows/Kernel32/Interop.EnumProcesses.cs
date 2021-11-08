// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "K32EnumProcesses", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial bool EnumProcesses(int[] processIds, int size, out int needed);
    }
}
