// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Activeds
    {
        [GeneratedDllImport(Interop.Libraries.Activeds, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static partial int ADsOpenObject(string path, string? userName, string? password, int flags, ref Guid iid, out IntPtr ppObject);
    }
}
