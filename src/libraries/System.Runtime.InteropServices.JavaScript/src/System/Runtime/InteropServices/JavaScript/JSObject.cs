// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        // to detect redundant calls
        public bool IsDisposed { get; private set; }

        public JSObject() : this(Interop.Runtime.New<object>())
        {
            object result = Interop.Runtime.BindCoreObject(JSHandle, (int)(IntPtr)Handle, out int exception);
            if (exception != 0)
                throw new JSException($"JSObject Error binding: {result}");

        }

        internal JSObject(IntPtr js_handle) : base(js_handle)
        { }

        internal JSObject(int js_handle) : base((IntPtr)js_handle)
        { }

        internal JSObject(int js_handle, object raw_obj) : base(js_handle)
        {
            RawObject = raw_obj;
        }

        public object Invoke(string method, params object?[] args)
        {
            object res = Interop.Runtime.InvokeJSWithArgs(JSHandle, method, args, out int exception);
            if (exception != 0)
                throw new JSException((string)res);
            return res;
        }

        public object GetObjectProperty(string name)
        {

            object propertyValue = Interop.Runtime.GetObjectProperty(JSHandle, name, out int exception);

            if (exception != 0)
                throw new JSException((string)propertyValue);

            return propertyValue;

        }

        public void SetObjectProperty(string name, object value, bool createIfNotExists = true, bool hasOwnProperty = false)
        {

            object setPropResult = Interop.Runtime.SetObjectProperty(JSHandle, name, value, createIfNotExists, hasOwnProperty, out int exception);
            if (exception != 0)
                throw new JSException($"Error setting {name} on (js-obj js '{JSHandle}' .NET '{(IntPtr)Handle} raw '{RawObject != null})");

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

        internal void ReleaseHandle()
        {
            FreeHandle();
            JSHandle = -1;
            IsDisposed = true;
            RawObject = null;
            Handle.Free();
        }

        private void FreeHandle()
        {
            Interop.Runtime.ReleaseHandle(JSHandle, out int exception);
            if (exception != 0)
                throw new JSException($"Error releasing handle on (js-obj js '{JSHandle}' .NET '{(IntPtr)Handle} raw '{RawObject != null})");
        }

        public override bool Equals(object? obj) => obj is JSObject other && JSHandle == other.JSHandle;

        public override int GetHashCode() => JSHandle;

        ~JSObject()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {

            if (!IsDisposed)
            {
                if (disposing)
                {

                    // Free any other managed objects here.
                    //
                    RawObject = null;
                }

                IsDisposed = true;

                // Free any unmanaged objects here.
                FreeHandle();

            }
        }

        public override string ToString()
        {
            return $"(js-obj js '{JSHandle}' .NET '{(IntPtr)Handle} raw '{RawObject != null})";
        }
    }
}
