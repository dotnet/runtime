// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class HostPolicy
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        internal delegate void corehost_resolve_component_dependencies_result_fn(string assemblyPaths,
            string nativeSearchPaths, string resourceSearchPaths);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        internal delegate void corehost_error_writer_fn(string message);

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [GeneratedDllImport(Libraries.HostPolicy, CharSet = CharSet.Auto)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial int corehost_resolve_component_dependencies(string componentMainAssemblyPath,
            corehost_resolve_component_dependencies_result_fn result);

        [GeneratedDllImport(Libraries.HostPolicy, CharSet = CharSet.Auto)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial IntPtr corehost_set_error_writer(IntPtr errorWriter);
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
    }
}
