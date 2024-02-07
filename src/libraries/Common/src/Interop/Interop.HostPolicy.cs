// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class HostPolicy
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void corehost_resolve_component_dependencies_result_fn(IntPtr assemblyPaths,
            IntPtr nativeSearchPaths, IntPtr resourceSearchPaths);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void corehost_error_writer_fn(IntPtr message);

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
#if TARGET_WINDOWS
        [LibraryImport(Libraries.HostPolicy, StringMarshalling = StringMarshalling.Utf16)]
#else
        [LibraryImport(Libraries.HostPolicy, StringMarshalling = StringMarshalling.Utf8)]
#endif
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static partial int corehost_resolve_component_dependencies(string componentMainAssemblyPath,
            corehost_resolve_component_dependencies_result_fn result);

        [LibraryImport(Libraries.HostPolicy)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static partial IntPtr corehost_set_error_writer(IntPtr errorWriter);
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
    }
}
