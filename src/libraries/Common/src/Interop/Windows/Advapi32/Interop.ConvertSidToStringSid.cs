// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "ConvertSidToStringSidW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial BOOL ConvertSidToStringSid(IntPtr sid, ref string stringSid);
    }
}
