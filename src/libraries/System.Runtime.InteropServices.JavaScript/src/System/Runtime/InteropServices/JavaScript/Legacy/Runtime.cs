// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    [Obsolete]
    public static class Runtime
    {
        /// <summary>
        /// Execute the provided string in the JavaScript context
        /// </summary>
        /// <returns>The js.</returns>
        /// <param name="str">String.</param>
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

        /// <summary>
        ///   Invoke a named method of the object, or throws a JSException on error.
        /// </summary>
        /// <param name="self">thisArg</param>
        /// <param name="method">The name of the method to invoke.</param>
        /// <param name="args">The argument list to pass to the invoke command.</param>
        /// <returns>
        ///   <para>
        ///     The return value can either be a primitive (string, int, double), a JSObject for JavaScript objects, a
        ///     System.Threading.Tasks.Task(object) for JavaScript promises, an array of
        ///     a byte, int or double (for Javascript objects typed as ArrayBuffer) or a
        ///     System.Func to represent JavaScript functions.  The specific version of
        ///     the Func that will be returned depends on the parameters of the Javascript function
        ///     and return value.
        ///   </para>
        ///   <para>
        ///     The value of a returned promise (The Task(object) return) can in turn be any of the above
        ///     valuews.
        ///   </para>
        /// </returns>
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

        /// <summary>
        ///   Returns the named property from the object, or throws a JSException on error.
        /// </summary>
        /// <param name="self">thisArg</param>
        /// <param name="name">The name of the property to lookup</param>
        /// <remarks>
        ///   This method can raise a JSException if fetching the property in Javascript raises an exception.
        /// </remarks>
        /// <returns>
        ///   <para>
        ///     The return value can either be a primitive (string, int, double), a
        ///     JSObject for JavaScript objects, a
        ///     System.Threading.Tasks.Task (object) for JavaScript promises, an array of
        ///     a byte, int or double (for Javascript objects typed as ArrayBuffer) or a
        ///     System.Func to represent JavaScript functions.  The specific version of
        ///     the Func that will be returned depends on the parameters of the Javascript function
        ///     and return value.
        ///   </para>
        ///   <para>
        ///     The value of a returned promise (The Task(object) return) can in turn be any of the above
        ///     valuews.
        ///   </para>
        /// </returns>
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

        /// <summary>
        ///   Sets the named property to the provided value.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="self">thisArg</param>
        /// <param name="name">The name of the property to lookup</param>
        /// <param name="value">The value can be a primitive type (int, double, string, bool), an
        /// array that will be surfaced as a typed ArrayBuffer (byte[], sbyte[], short[], ushort[],
        /// float[], double[]) </param>
        /// <param name="createIfNotExists">Defaults to <see langword="true"/> and creates the property on the javascript object if not found, if set to <see langword="false"/> it will not create the property if it does not exist.  If the property exists, the value is updated with the provided value.</param>
        /// <param name="hasOwnProperty"></param>
        public static void SetObjectProperty(this JSObject self, string name, object? value, bool createIfNotExists = true, bool hasOwnProperty = false)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (self.IsDisposed) throw new ObjectDisposedException($"Cannot access a disposed {self.GetType().Name}.");

            Interop.Runtime.SetObjectPropertyRef(self.JSHandle, name, in value, createIfNotExists, hasOwnProperty, out int exception, out object res);
            if (exception != 0)
                throw new JSException($"Error setting {name} on (js-obj js '{self.JSHandle}'): {res}");
        }

        public static void AssertNotDisposed(this JSObject self)
        {
            if (self.IsDisposed) throw new ObjectDisposedException($"Cannot access a disposed {self.GetType().Name}.");
        }

        public static void AssertInFlight(this JSObject self, int expectedInFlightCount)
        {
            if (self.InFlightCounter != expectedInFlightCount) throw new InvalidProgramException($"Invalid InFlightCounter for JSObject {self.JSHandle}, expected: {expectedInFlightCount}, actual: {self.InFlightCounter}");
        }
    }
}
