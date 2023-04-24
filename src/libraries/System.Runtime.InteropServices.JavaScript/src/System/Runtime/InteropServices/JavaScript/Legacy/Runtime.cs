// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    [Obsolete]
    public static class Runtime
    {
        public static object GetGlobalObject(string str)
            => JavaScriptImports.GetGlobalObject(str);

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
#if FEATURE_WASM_THREADS
            LegacyHostImplementation.ThrowIfLegacyWorkerThread();
#endif
            ArgumentNullException.ThrowIfNull(self);
            ObjectDisposedException.ThrowIf(self.IsDisposed, self);
            Interop.Runtime.InvokeJSWithArgsRef(self.JSHandle, method, args, out int exception, out object res);
            if (exception != 0)
                throw new JSException((string)res);
            LegacyHostImplementation.ReleaseInFlight(res);
            return res;
        }

        /// <summary>
        ///   Returns the named property from the object, or throws a JSException on error.
        /// </summary>
        /// <param name="self">thisArg</param>
        /// <param name="name">The name of the property to lookup.</param>
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
#if FEATURE_WASM_THREADS
            LegacyHostImplementation.ThrowIfLegacyWorkerThread();
#endif
            ArgumentNullException.ThrowIfNull(self);
            ObjectDisposedException.ThrowIf(self.IsDisposed, self);

            Interop.Runtime.GetObjectPropertyRef(self.JSHandle, name, out int exception, out object propertyValue);
            if (exception != 0)
                throw new JSException((string)propertyValue);
            LegacyHostImplementation.ReleaseInFlight(propertyValue);
            return propertyValue;
        }

        /// <summary>
        ///   Sets the named property to the provided value.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="self">thisArg</param>
        /// <param name="name">The name of the property to lookup.</param>
        /// <param name="value">The value can be a primitive type (int, double, string, bool), an
        /// array that will be surfaced as a typed ArrayBuffer (byte[], sbyte[], short[], ushort[],
        /// float[], double[]) </param>
        /// <param name="createIfNotExists">Defaults to <see langword="true"/> and creates the property on the javascript object if not found, if set to <see langword="false"/> it will not create the property if it does not exist.  If the property exists, the value is updated with the provided value.</param>
        /// <param name="hasOwnProperty"></param>
        public static void SetObjectProperty(this JSObject self, string name, object? value, bool createIfNotExists = true, bool hasOwnProperty = false)
        {
#if FEATURE_WASM_THREADS
            LegacyHostImplementation.ThrowIfLegacyWorkerThread();
#endif
            ArgumentNullException.ThrowIfNull(self);
            ObjectDisposedException.ThrowIf(self.IsDisposed, self);

            Interop.Runtime.SetObjectPropertyRef(self.JSHandle, name, in value, createIfNotExists, hasOwnProperty, out int exception, out object res);
            if (exception != 0)
                throw new JSException(SR.Format(SR.ErrorLegacySettingProperty, name, self.JSHandle, res));
        }

        public static void AssertNotDisposed(this JSObject self)
        {
            ObjectDisposedException.ThrowIf(self.IsDisposed, self);
        }

        public static void AssertInFlight(this JSObject self, int expectedInFlightCount)
        {
            if (self.InFlightCounter != expectedInFlightCount) throw new InvalidOperationException(SR.Format(SR.UnsupportedLegacyMarshlerType, self.JSHandle, expectedInFlightCount, self.InFlightCounter));
        }
    }
}
