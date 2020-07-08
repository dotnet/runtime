// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_LoadICU")]
        internal static extern int LoadICU();

        internal static void InitICUFunctions(IntPtr icuuc, IntPtr icuin, ReadOnlySpan<char> version, ReadOnlySpan<char> suffix)
        {
            Debug.Assert(icuuc != IntPtr.Zero);
            Debug.Assert(icuin != IntPtr.Zero);

            InitICUFunctions(icuuc, icuin, version.ToString(), suffix.Length > 0 ? suffix.ToString() : null);
        }

        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_InitICUFunctions")]
        internal static extern void InitICUFunctions(IntPtr icuuc, IntPtr icuin, string version, string? suffix);

        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetICUVersion")]
        internal static extern int GetICUVersion();
    }
}
