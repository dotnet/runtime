// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Gdi32
    {
#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support blittable structs defined in other assemblies.
        [DllImport(Libraries.Gdi32)]
        public static extern bool OffsetViewportOrgEx(IntPtr hdc, int x, int y, ref Point lppt);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        public static bool OffsetViewportOrgEx(HandleRef hdc, int x, int y, ref Point lppt)
        {
            bool result = OffsetViewportOrgEx(hdc.Handle, x, y, ref lppt);
            GC.KeepAlive(hdc.Wrapper);
            return result;
        }
    }
}
