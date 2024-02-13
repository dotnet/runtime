// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

internal static partial class Interop
{
    // WARNING: until https://github.com/dotnet/runtime/issues/37955 is fixed
    // make sure that the native side always sets the out parameters
    // otherwise out parameters could stay un-initialized, when the method is used in inlined context
    internal static unsafe partial class Runtime
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ReleaseCSOwnedObject(nint jsHandle);
#if FEATURE_WASM_MANAGED_THREADS
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ReleaseCSOwnedObjectPost(nint targetNativeTID, nint jsHandle);
#endif

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void InvokeJSFunction(nint functionHandle, nint data);
#if FEATURE_WASM_MANAGED_THREADS
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void InvokeJSFunctionSend(nint targetNativeTID, nint functionHandle, nint data);
#endif

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void ResolveOrRejectPromise(nint data);
#if FEATURE_WASM_MANAGED_THREADS
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void ResolveOrRejectPromisePost(nint targetNativeTID, nint data);
#endif

#if !ENABLE_JS_INTEROP_BY_VALUE
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern nint RegisterGCRoot(void* start, int bytesSize, IntPtr name);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void DeregisterGCRoot(nint handle);
#endif

#if FEATURE_WASM_MANAGED_THREADS
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void InstallWebWorkerInterop(nint proxyContextGCHandle);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void UninstallWebWorkerInterop();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void InvokeJSImportSync(nint data, nint signature);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void InvokeJSImportSyncSend(nint targetNativeTID, nint data, nint signature);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void InvokeJSImportAsyncPost(nint targetNativeTID, nint data, nint signature);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void CancelPromise(nint taskHolderGCHandle);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void CancelPromisePost(nint targetNativeTID, nint taskHolderGCHandle);
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern unsafe void BindJSImport(void* signature, out int is_exception, out object result);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void InvokeJSImport(int importHandle, nint data);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void CancelPromise(nint gcHandle);
#endif
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void SetEntryPointBreakpoint(int entryPointMetadataToken);


    }
}
