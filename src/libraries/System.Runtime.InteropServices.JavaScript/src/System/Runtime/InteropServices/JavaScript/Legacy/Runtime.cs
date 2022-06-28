// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    [Obsolete]
    public static class Runtime
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "System.Runtime.InteropServices.JavaScript.JavaScriptExports", "System.Runtime.InteropServices.JavaScript")]
        public static string InvokeJS(string str)
            => JavaScriptImports.InvokeJS(str);

        public static object GetGlobalObject(string str)
            => JavaScriptImports.GetGlobalObject(str);

        public static void CancelPromise(IntPtr promiseJSHandle)
            => JavaScriptImports.CancelPromise(promiseJSHandle);

        public static Task<object> WebSocketOpen(string uri, object[]? subProtocols, Delegate onClosed, out JSObject webSocket, out IntPtr promiseJSHandle)
            => JavaScriptImports.WebSocketOpen(uri, subProtocols, onClosed, out webSocket, out promiseJSHandle);

        public static unsafe Task<object>? WebSocketSend(JSObject webSocket, ArraySegment<byte> buffer, int messageType, bool endOfMessage, out IntPtr promiseJSHandle)
            => JavaScriptImports.WebSocketSend(webSocket, buffer, messageType, endOfMessage, out promiseJSHandle);

        public static unsafe Task<object>? WebSocketReceive(JSObject webSocket, ArraySegment<byte> buffer, ReadOnlySpan<int> response, out IntPtr promiseJSHandle)
            => JavaScriptImports.WebSocketReceive(webSocket, buffer, response, out promiseJSHandle);

        public static Task<object>? WebSocketClose(JSObject webSocket, int code, string? reason, bool waitForCloseReceived, out IntPtr promiseJSHandle)
            => JavaScriptImports.WebSocketClose(webSocket, code, reason, waitForCloseReceived, out promiseJSHandle);

        public static void WebSocketAbort(JSObject webSocket)
            => JavaScriptImports.WebSocketAbort(webSocket);

        public static object Invoke(this JSObject self, string method, params object?[] args)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (self.IsDisposed) throw new ObjectDisposedException($"Cannot access a disposed {self.GetType().Name}.");
            Interop.Runtime.InvokeJSWithArgsRef(self.JSHandle, method, args, out int exception, out object res);
            if (exception != 0)
                throw new JSException((string)res);
            JSHostImplementation.ReleaseInFlight(res);
            return res;
        }

        public static object GetObjectProperty(this JSObject self, string name)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (self.IsDisposed) throw new ObjectDisposedException($"Cannot access a disposed {self.GetType().Name}.");

            Interop.Runtime.GetObjectPropertyRef(self.JSHandle, name, out int exception, out object propertyValue);
            if (exception != 0)
                throw new JSException((string)propertyValue);
            JSHostImplementation.ReleaseInFlight(propertyValue);
            return propertyValue;
        }

        public static void SetObjectProperty(this JSObject self, string name, object? value, bool createIfNotExists = true, bool hasOwnProperty = false)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (self.IsDisposed) throw new ObjectDisposedException($"Cannot access a disposed {self.GetType().Name}.");

            Interop.Runtime.SetObjectPropertyRef(self.JSHandle, name, in value, createIfNotExists, hasOwnProperty, out int exception, out object res);
            if (exception != 0)
                throw new JSException($"Error setting {name} on (js-obj js '{self.JSHandle}'): {res}");
        }

#if DEBUG
        public static
#else
        internal static
#endif
            void AssertNotDisposed(this JSObject self)
        {
            if (self.IsDisposed) throw new ObjectDisposedException($"Cannot access a disposed {self.GetType().Name}.");
        }

#if DEBUG
        public static
#else
        internal static
#endif
            void AssertInFlight(this JSObject self, int expectedInFlightCount)
        {
            if (self.InFlightCounter != expectedInFlightCount) throw new InvalidProgramException($"Invalid InFlightCounter for JSObject {self.JSHandle}, expected: {expectedInFlightCount}, actual: {self.InFlightCounter}");
        }
    }
}
