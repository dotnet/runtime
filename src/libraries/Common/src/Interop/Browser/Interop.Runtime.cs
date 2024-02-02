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
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void InvokeJSFunction(nint functionHandle, nint data);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern unsafe void BindCSFunction(in string fully_qualified_name, int signature_hash, void* signature, out int is_exception, out object result);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void ResolveOrRejectPromise(nint data);

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
        public static extern void InvokeJSImportAsync(nint data, nint signature);
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern unsafe void BindJSImport(void* signature, out int is_exception, out object result);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void InvokeJSImport(int importHandle, nint data);
#endif

        #region Legacy

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InvokeJSWithArgsRef(IntPtr jsHandle, in string method, in object?[] parms, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void GetObjectPropertyRef(IntPtr jsHandle, in string propertyName, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void SetObjectPropertyRef(IntPtr jsHandle, in string propertyName, in object? value, bool createIfNotExists, bool hasOwnProperty, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void GetByIndexRef(IntPtr jsHandle, int index, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void SetByIndexRef(IntPtr jsHandle, int index, in object? value, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void GetGlobalObjectRef(in string? globalName, out int exceptionalResult, out object result);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void TypedArrayToArrayRef(IntPtr jsHandle, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void CreateCSOwnedObjectRef(in string className, in object[] parms, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void TypedArrayFromRef(int arrayPtr, int begin, int end, int bytesPerElement, int type, out int exceptionalResult, out object result);

        #endregion

    }
}
