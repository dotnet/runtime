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
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object GetObjectProperty(IntPtr jsHandle, string propertyName, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object SetObjectProperty(IntPtr jsHandle, string propertyName, object value, bool createIfNotExists, bool hasOwnProperty, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object GetByIndex(IntPtr jsHandle, int index, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object SetByIndex(IntPtr jsHandle, int index, object? value, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object GetGlobalObject(string? globalName, out int exceptionalResult);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object TypedArrayToArray(IntPtr jsHandle, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object ReleaseCSOwnedObject(IntPtr jsHandle);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object CreateCSOwnedObject(string className, object[] parms, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object TypedArrayCopyTo(IntPtr jsHandle, int arrayPtr, int begin, int end, int bytesPerElement, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object TypedArrayFrom(int arrayPtr, int begin, int end, int bytesPerElement, int type, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object TypedArrayCopyFrom(IntPtr jsHandle, int arrayPtr, int begin, int end, int bytesPerElement, out int exceptionalResult);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object WebSocketSend(IntPtr webSocketJSHandle, IntPtr messagePtr, int offset, int length, int messageType, bool endOfMessage, out IntPtr promiseJSHandle, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object WebSocketReceive(IntPtr webSocketJSHandle, IntPtr bufferPtr, int offset, int length, IntPtr responsePtr, out IntPtr promiseJSHandle, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object WebSocketOpen(string uri, object[]? subProtocols, Delegate onClosed, out IntPtr webSocketJSHandle, out IntPtr promiseJSHandle, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string WebSocketAbort(IntPtr webSocketJSHandle, out int exceptionalResult);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object WebSocketClose(IntPtr webSocketJSHandle, int code, string? reason, bool waitForCloseReceived, out IntPtr promiseJSHandle, out int exceptionalResult);
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
            object jsObj = GetGlobalObject(str, out exception);

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
