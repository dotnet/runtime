// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static class HostPolicy
    {
#if TARGET_WINDOWS
        private const CharSet HostpolicyCharSet = CharSet.Unicode;
#else
        private const CharSet HostpolicyCharSet = CharSet.Ansi;
#endif

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = HostpolicyCharSet)]
        internal delegate void ResolveComponentDependenciesResultFn(string assemblyPaths,
            string nativeSearchPaths, string resourceSearchPaths);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = HostpolicyCharSet)]
        internal delegate void ErrorWriterFn(string message);

#pragma warning disable BCL0015 // Disable Pinvoke analyzer errors.

        [DllImport(Libraries.HostPolicy, EntryPoint = "corehost_resolve_component_dependencies"
            CallingConvention = CallingConvention.Cdecl, CharSet = HostpolicyCharSet)]
        private static extern int ResolveComponentDependencies(string componentMainAssemblyPath,
            ResolveComponentDependenciesResultFn result);

        [DllImport(Libraries.HostPolicy, EntryPoint = "corehost_set_error_writer",
            CallingConvention = CallingConvention.Cdecl, CharSet = HostpolicyCharSet)]
        private static extern IntPtr SetErrorWriter(IntPtr errorWriter);

#pragma warning restore
    }
}
