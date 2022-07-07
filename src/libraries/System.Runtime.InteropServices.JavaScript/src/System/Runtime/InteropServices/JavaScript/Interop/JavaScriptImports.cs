// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static unsafe partial class JavaScriptImports
    {

        public static string InvokeJS(string str)
        {
            string res = Interop.Runtime.InvokeJS(str, out int exception);
            if (exception != 0)
                throw new JSException(res);
            return res;
        }

        public static object GetGlobalObject(string? str = null)
        {
            int exception;
            Interop.Runtime.GetGlobalObjectRef(str, out exception, out object jsObj);

            if (exception != 0)
                throw new JSException($"Error obtaining a handle to global {str}");

            JSHostImplementation.ReleaseInFlight(jsObj);
            return jsObj;
        }

        public static IntPtr CreateCSOwnedObject(string typeName, object[] parms)
        {
            Interop.Runtime.CreateCSOwnedObjectRef(typeName, parms, out int exception, out object res);
            if (exception != 0)
                throw new JSException((string)res);

            return (IntPtr)(int)res;
        }

        public static void CancelPromise(IntPtr promiseJSHandle)
        {
            Interop.Runtime.CancelPromiseRef(promiseJSHandle, out int exception, out string res);
            if (exception != 0)
                throw new JSException(res);
        }

        public static Task<object> WebSocketOpen(string uri, object[]? subProtocols, Delegate onClosed, out JSObject webSocket, out IntPtr promiseJSHandle)
        {
            Interop.Runtime.WebSocketOpenRef(uri, subProtocols, onClosed, out IntPtr webSocketJSHandle, out promiseJSHandle, out int exception, out object res);
            if (exception != 0)
                throw new JSException((string)res);
            webSocket = new JSObject((IntPtr)webSocketJSHandle);

            return (Task<object>)res;
        }

        public static unsafe Task<object>? WebSocketSend(JSObject webSocket, ArraySegment<byte> buffer, int messageType, bool endOfMessage, out IntPtr promiseJSHandle)
        {
            fixed (byte* messagePtr = buffer.Array)
            {
                Interop.Runtime.WebSocketSend(webSocket.JSHandle, (IntPtr)messagePtr, buffer.Offset, buffer.Count, messageType, endOfMessage, out promiseJSHandle, out int exception, out object res);
                if (exception != 0)
                    throw new JSException((string)res);

                if (res == null)
                {
                    return null;
                }

                return (Task<object>)res;
            }
        }

        public static unsafe Task<object>? WebSocketReceive(JSObject webSocket, ArraySegment<byte> buffer, ReadOnlySpan<int> response, out IntPtr promiseJSHandle)
        {
            fixed (int* responsePtr = response)
            fixed (byte* bufferPtr = buffer.Array)
            {
                Interop.Runtime.WebSocketReceive(webSocket.JSHandle, (IntPtr)bufferPtr, buffer.Offset, buffer.Count, (IntPtr)responsePtr, out promiseJSHandle, out int exception, out object res);
                if (exception != 0)
                    throw new JSException((string)res);
                if (res == null)
                {
                    return null;
                }
                return (Task<object>)res;
            }
        }

        public static Task<object>? WebSocketClose(JSObject webSocket, int code, string? reason, bool waitForCloseReceived, out IntPtr promiseJSHandle)
        {
            Interop.Runtime.WebSocketCloseRef(webSocket.JSHandle, code, reason, waitForCloseReceived, out promiseJSHandle, out int exception, out object res);
            if (exception != 0)
                throw new JSException((string)res);

            if (res == null)
            {
                return null;
            }
            return (Task<object>)res;
        }

        public static void WebSocketAbort(JSObject webSocket)
        {
            Interop.Runtime.WebSocketAbort(webSocket.JSHandle, out int exception, out string res);
            if (exception != 0)
                throw new JSException(res);
        }
    }
}
