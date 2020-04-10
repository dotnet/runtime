// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#pragma warning disable BCL0015 // Disable Pinvoke analyzer errors.

        [DllImport(Libraries.HostPolicy, EntryPoint = "corehost_resolve_component_dependencies",
            CallingConvention = CallingConvention.Cdecl, CharSet = HostpolicyCharSet)]
        private static extern int ResolveComponentDependencies(string componentMainAssemblyPath,
            corehost_resolve_component_dependencies_result_fn result);

        [DllImport(Libraries.HostPolicy, EntryPoint = "corehost_set_error_writer",
            CallingConvention = CallingConvention.Cdecl, CharSet = HostpolicyCharSet)]
        private static extern IntPtr SetErrorWriter(IntPtr errorWriter);

#pragma warning restore
    }
}
