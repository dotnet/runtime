// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Dsrole
    {
        [GeneratedDllImport(Libraries.Dsrole)]
        public static partial int DsRoleFreeMemory(IntPtr buffer);
    }
}
