// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Console = System.Diagnostics.Debug;

namespace System.Runtime.InteropServices.JavaScript
{
    public interface IJSObject
    {
        int JSHandle { get; }
        int Length { get; }
    }

    /// <summary>
    ///   JSObjects are wrappers for a native JavaScript object, and
    ///   they retain a reference to the JavaScript object for the lifetime of this C# object.
    /// </summary>
    public class JSObject : AnyRef, IJSObject, IDisposable
    {
        internal object? RawObject;

        private WeakReference<Delegate>? WeakRawObject;

        // to detect redundant calls
        public bool IsDisposed { get; private set; }

        public JSObject() : this(Interop.Runtime.New<object>(), true)
        {
            object result = Interop.Runtime.BindCoreObject(JSHandle, Int32Handle, out int exception);
            if (exception != 0)
                throw new JSException(SR.Format(SR.JSObjectErrorBinding, result));

        }

        internal JSObject(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }

        internal JSObject(int jsHandle, bool ownsHandle) : base((IntPtr)jsHandle, ownsHandle)
        { }

        internal JSObject(int jsHandle, object rawObj) : base(jsHandle, false)
        {
            RawObject = rawObj;
        }

        internal JSObject(int jsHandle, Delegate rawDelegate, bool ownsHandle = true) : base(jsHandle, ownsHandle)
        {
            WeakRawObject = new WeakReference<Delegate>(rawDelegate, trackResurrection: false);
        }

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
            object res = Interop.Runtime.InvokeJSWithArgs(JSHandle, method, args, out int exception);
            if (exception != 0)
                throw new JSException((string)res);
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
            object propertyValue = Interop.Runtime.GetObjectProperty(JSHandle, name, out int exception);
            if (exception != 0)
                throw new JSException((string)propertyValue);
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
            object setPropResult = Interop.Runtime.SetObjectProperty(JSHandle, name, value, createIfNotExists, hasOwnProperty, out int exception);
            if (exception != 0)
                throw new JSException($"Error setting {name} on (js-obj js '{JSHandle}' .NET '{Int32Handle} raw '{RawObject != null})");
        }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>The length.</value>
        public int Length
        {
            get => Convert.ToInt32(GetObjectProperty("length"));
            set => SetObjectProperty("length", value, false);
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

        internal bool IsWeakWrapper => WeakRawObject?.TryGetTarget(out _) == true;

        internal object? GetWrappedObject()
        {
            return RawObject ?? (WeakRawObject is WeakReference<Delegate> wr && wr.TryGetTarget(out Delegate? d) ? d : null);
        }
        internal void FreeHandle()
        {
            Runtime.ReleaseJSObject(this);
            SetHandleAsInvalid();
            IsDisposed = true;
            RawObject = null;
            WeakRawObject = null;
            FreeGCHandle();
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is JSObject other && JSHandle == other.JSHandle;

        public override int GetHashCode() => JSHandle;

        protected override bool ReleaseHandle()
        {
            bool ret = false;

#if DEBUG_HANDLE
            Console.WriteLine($"Release Handle handle:{handle}");
            try
            {
#endif
            FreeHandle();
            ret = true;

#if DEBUG_HANDLE
            }
            catch (Exception exception)
            {
                Console.WriteLine($"ReleaseHandle: {exception.Message}");
                ret = true;  // Avoid a second assert.
                throw;
            }
            finally
            {
                if (!ret)
                {
                    Console.WriteLine($"ReleaseHandle failed. handle:{handle}");
                }
            }
#endif
            return ret;
        }

        public override string ToString()
        {
            return $"(js-obj js '{Int32Handle}' raw '{RawObject != null}' weak_raw '{WeakRawObject != null}')";
        }
    }
}
