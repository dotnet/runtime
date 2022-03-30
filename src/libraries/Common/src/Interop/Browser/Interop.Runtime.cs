// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

using JSObject = System.Runtime.InteropServices.JavaScript.JSObject;
using JSException = System.Runtime.InteropServices.JavaScript.JSException;
using Uint8Array = System.Runtime.InteropServices.JavaScript.Uint8Array;

internal static partial class Interop
{
    internal static partial class Runtime
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string InvokeJS(string str, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object CompileFunction(string str, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object InvokeJSWithArgs(IntPtr jsHandle, string method, object?[] parms, out int exceptionalResult);
        // FIXME: All of these signatures need to be object? in various places and not object, but the nullability
        //  warnings will take me hours and hours to fix so I'm not doing that right now since they're already broken
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

        // FIXME: Why are these IntPtr?
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
        internal static extern string CancelPromise(IntPtr promiseJSHandle, out int exceptionalResult);

        // / <summary>
        // / Execute the provided string in the JavaScript context
        // / </summary>
        // / <returns>The js.</returns>
        // / <param name="str">String.</param>
        public static string InvokeJS(string str)
        {
            string res = InvokeJS(str, out int exception);
            if (exception != 0)
                throw new JSException(res);
            return res;
        }

        public static System.Runtime.InteropServices.JavaScript.Function? CompileFunction(string snippet)
        {
            object res = CompileFunction(snippet, out int exception);
            if (exception != 0)
                throw new JSException((string)res);
            ReleaseInFlight(res);
            return res as System.Runtime.InteropServices.JavaScript.Function;
        }

        public static object GetGlobalObject(string? str = null)
        {
            int exception;
            GetGlobalObjectRef(str, out exception, out object jsObj);

            if (exception != 0)
                throw new JSException($"Error obtaining a handle to global {str}");

            ReleaseInFlight(jsObj);
            return jsObj;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void StopProfile()
        {
        }

        // Called by the AOT profiler to save profile data into INTERNAL.aot_profile_data
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static unsafe void DumpAotProfileData(ref byte buf, int len, string extraArg)
        {
            if (len == 0)
                throw new JSException("Profile data length is 0");

            var arr = new byte[len];
            fixed (void *p = &buf)
            {
                var span = new ReadOnlySpan<byte>(p, len);
                // Send it to JS
                var module = (JSObject)Runtime.GetGlobalObject("Module");
                module.SetObjectProperty("aot_profile_data", Uint8Array.From(span));
            }
        }

        internal static void ReleaseInFlight(object? obj)
        {
            JSObject? jsObj = obj as JSObject;
            jsObj?.ReleaseInFlight();
        }
    }
}
