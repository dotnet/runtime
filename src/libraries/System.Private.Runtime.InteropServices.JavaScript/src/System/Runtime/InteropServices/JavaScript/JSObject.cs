// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    ///   JSObjects are wrappers for a native JavaScript object, and
    ///   they retain a reference to the JavaScript object for the lifetime of this C# object.
    /// </summary>
    public partial class JSObject : IDisposable
    {
        /// <summary>
        ///   Invoke a named method of the object, or throws a JSException on error.
        /// </summary>
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
        public object Invoke(string method, params object?[] args)
        {
            AssertNotDisposed();

            Interop.Runtime.InvokeJSWithArgsRef(JSHandle, method, args, out int exception, out object res);
            if (exception != 0)
                throw new JSException((string)res);
            Interop.Runtime.ReleaseInFlight(res);
            return res;
        }

        /// <summary>
        ///   Returns the named property from the object, or throws a JSException on error.
        /// </summary>
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
        public object GetObjectProperty(string name)
        {
            AssertNotDisposed();

            Interop.Runtime.GetObjectPropertyRef(JSHandle, name, out int exception, out object propertyValue);
            if (exception != 0)
                throw new JSException((string)propertyValue);
            Interop.Runtime.ReleaseInFlight(propertyValue);
            return propertyValue;
        }

        /// <summary>
        ///   Sets the named property to the provided value.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="name">The name of the property to lookup</param>
        /// <param name="value">The value can be a primitive type (int, double, string, bool), an
        /// array that will be surfaced as a typed ArrayBuffer (byte[], sbyte[], short[], ushort[],
        /// float[], double[]) </param>
        /// <param name="createIfNotExists">Defaults to <see langword="true"/> and creates the property on the javascript object if not found, if set to <see langword="false"/> it will not create the property if it does not exist.  If the property exists, the value is updated with the provided value.</param>
        /// <param name="hasOwnProperty"></param>
        public void SetObjectProperty(string name, object value, bool createIfNotExists = true, bool hasOwnProperty = false)
        {
            AssertNotDisposed();

            Interop.Runtime.SetObjectPropertyRef(JSHandle, name, in value, createIfNotExists, hasOwnProperty, out int exception, out object res);
            if (exception != 0)
                throw new JSException($"Error setting {name} on (js-obj js '{JSHandle}'): {res}");
        }

        /// <summary>
        /// Returns a boolean indicating whether the object has the specified property as its own property (as opposed to inheriting it).
        /// </summary>
        /// <returns><c>true</c>, if the object has the specified property as own property, <c>false</c> otherwise.</returns>
        /// <param name="prop">The String name or Symbol of the property to test.</param>
        public bool HasOwnProperty(string prop) => (bool)Invoke("hasOwnProperty", prop);

        /// <summary>
        /// Returns a boolean indicating whether the specified property is enumerable.
        /// </summary>
        /// <returns><c>true</c>, if the specified property is enumerable, <c>false</c> otherwise.</returns>
        /// <param name="prop">The String name or Symbol of the property to test.</param>
        public bool PropertyIsEnumerable(string prop) => (bool)Invoke("propertyIsEnumerable", prop);
    }
}
