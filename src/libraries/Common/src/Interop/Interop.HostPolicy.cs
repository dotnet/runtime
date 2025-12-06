// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using unsafe ErrorWriterCallback = delegate* unmanaged[Cdecl]<nint, void>;

internal static partial class Interop
{
    internal static unsafe partial class HostPolicy
    {
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
#if TARGET_WINDOWS
        [LibraryImport(Libraries.HostPolicy, StringMarshalling = StringMarshalling.Utf16)]
#else
        [LibraryImport(Libraries.HostPolicy, StringMarshalling = StringMarshalling.Utf8)]
#endif
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static partial int corehost_resolve_component_dependencies(string componentMainAssemblyPath,
            delegate* unmanaged[Cdecl]<nint, nint, nint, void> result);

        [LibraryImport(Libraries.HostPolicy)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static partial ErrorWriterCallback corehost_set_error_writer(ErrorWriterCallback errorWriter);
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
    }
}
