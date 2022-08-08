// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static unsafe partial class Runtime
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ReleaseCSOwnedObject(IntPtr jsHandle);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern unsafe void BindJSFunction(in string function_name, in string module_name, void* signature, out IntPtr bound_function_js_handle, out int is_exception, out object result);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void InvokeJSFunction(IntPtr bound_function_js_handle, void* data);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern unsafe void BindCSFunction(in string fully_qualified_name, int signature_hash, void* signature, out int is_exception, out object result);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void MarshalPromise(void* data);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern IntPtr RegisterGCRoot(IntPtr start, int bytesSize, IntPtr name);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void DeregisterGCRoot(IntPtr handle);

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
