// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Runtime
    {
        [LibraryImport(Libraries.JavaScriptNative, EntryPoint = "SystemInteropJS_BindJSImportST")]
        public static unsafe partial nint BindJSImportST(void* signature);

        [LibraryImport(Libraries.JavaScriptNative, EntryPoint = "SystemInteropJS_InvokeJSImportST")]
        public static partial void InvokeJSImportST(int importHandle, nint args);

        [LibraryImport(Libraries.JavaScriptNative, EntryPoint = "SystemInteropJS_ReleaseCSOwnedObject")]
        internal static partial void ReleaseCSOwnedObject(nint jsHandle);

        [LibraryImport(Libraries.JavaScriptNative, EntryPoint = "SystemInteropJS_InvokeJSFunction")]
        public static partial void InvokeJSFunction(nint functionHandle, nint data);

        [LibraryImport(Libraries.JavaScriptNative, EntryPoint = "SystemInteropJS_ResolveOrRejectPromise")]
        public static partial void ResolveOrRejectPromise(nint data);

        [LibraryImport(Libraries.JavaScriptNative, EntryPoint = "SystemInteropJS_RegisterGCRoot")]
        public static partial nint RegisterGCRoot(void* start, int bytesSize, IntPtr name);
        [LibraryImport(Libraries.JavaScriptNative, EntryPoint = "SystemInteropJS_DeregisterGCRoot")]
        public static partial void DeregisterGCRoot(nint handle);

        [LibraryImport(Libraries.JavaScriptNative, EntryPoint = "SystemInteropJS_CancelPromise")]
        public static partial void CancelPromise(nint gcHandle);

        [LibraryImport(Libraries.JavaScriptNative, EntryPoint = "SystemInteropJS_BindAssemblyExports")]
        public static partial void BindAssemblyExports(IntPtr assemblyNamePtr);
        [LibraryImport(Libraries.JavaScriptNative, EntryPoint = "SystemInteropJS_GetAssemblyExport")]
        public static partial void GetAssemblyExport(IntPtr assemblyNamePtr, IntPtr namespacePtr, IntPtr classnamePtr, IntPtr methodNamePtr, int signatureHash, IntPtr* methodHandlePtr);

        // TODO-WASM: delete once we switch to CoreCLR only
        [LibraryImport(Libraries.JavaScriptNative, EntryPoint = "SystemInteropJS_AssemblyGetEntryPoint")]
        public static partial void AssemblyGetEntryPoint(IntPtr assemblyNamePtr, int auto_insert_breakpoint, void** monoMethodPtrPtr);
    }
}
