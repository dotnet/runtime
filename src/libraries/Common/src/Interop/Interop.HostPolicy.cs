// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static class HostPolicy
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        internal delegate void corehost_resolve_component_dependencies_result_fn(string assemblyPaths,
            string nativeSearchPaths, string resourceSearchPaths);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        internal delegate void corehost_error_writer_fn(string message);

        [DllImport(Libraries.HostPolicy, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        internal static extern int corehost_resolve_component_dependencies(string componentMainAssemblyPath,
            corehost_resolve_component_dependencies_result_fn result);

        [DllImport(Libraries.HostPolicy, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        internal static extern IntPtr corehost_set_error_writer(IntPtr errorWriter);
    }
}
