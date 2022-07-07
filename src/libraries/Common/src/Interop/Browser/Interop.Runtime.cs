// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static unsafe partial class Runtime
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string InvokeJS(string str, out int exceptionalResult);
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
        internal static extern void ReleaseCSOwnedObject(IntPtr jsHandle);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void CreateCSOwnedObjectRef(in string className, in object[] parms, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void TypedArrayCopyToRef(IntPtr jsHandle, int arrayPtr, int begin, int end, int bytesPerElement, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void TypedArrayFromRef(int arrayPtr, int begin, int end, int bytesPerElement, int type, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void TypedArrayCopyFromRef(IntPtr jsHandle, int arrayPtr, int begin, int end, int bytesPerElement, out int exceptionalResult, out object result);


        #region Legacy
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void WebSocketSend(IntPtr webSocketJSHandle, IntPtr messagePtr, int offset, int length, int messageType, bool endOfMessage, out IntPtr promiseJSHandle, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void WebSocketReceive(IntPtr webSocketJSHandle, IntPtr bufferPtr, int offset, int length, IntPtr responsePtr, out IntPtr promiseJSHandle, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void WebSocketOpenRef(in string uri, in object[]? subProtocols, in Delegate onClosed, out IntPtr webSocketJSHandle, out IntPtr promiseJSHandle, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void WebSocketAbort(IntPtr webSocketJSHandle, out int exceptionalResult, out string result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void WebSocketCloseRef(IntPtr webSocketJSHandle, int code, in string? reason, bool waitForCloseReceived, out IntPtr promiseJSHandle, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void CancelPromiseRef(IntPtr promiseJSHandle, out int exceptionalResult, out string result);

        #endregion

    }
}
