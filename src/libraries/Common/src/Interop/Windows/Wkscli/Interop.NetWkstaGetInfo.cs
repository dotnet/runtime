// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Wkscli
    {
        [GeneratedDllImport(Libraries.Wkscli, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static partial int NetWkstaGetInfo(string server, int level, ref IntPtr buffer);
    }
}
