// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.InteropServices
{
    public static partial class Marshal
    {
        public static void FreeLibrary(System.IntPtr handle) { }
        public static System.IntPtr GetLibraryExport(System.IntPtr handle, string name) { throw null; }
        public static System.IntPtr LoadLibrary(string libraryPath) { throw null; }
        public static System.IntPtr LoadLibrary(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath) { throw null; }
        public static bool TryGetLibraryExport(System.IntPtr handle, string name, out System.IntPtr address) { throw null; }
        public static bool TryLoadLibrary(string libraryPath, out System.IntPtr handle) { throw null; }
        public static bool TryLoadLibrary(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath, out System.IntPtr handle) { throw null; }
    }
}
