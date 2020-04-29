// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal static partial class Interop
{
    internal static partial class JavaScript
    {
        public interface IJSObject
        {
            int JSHandle { get; }
            int Length { get; }
        }
    }
    internal static partial class JavaScript
    {
        /// <summary>
        ///   JSObjects are wrappers for a native JavaScript object, and
        ///   they retain a reference to the JavaScript object for the lifetime of this C# object.
        /// </summary>
        public class JSObject : Interop.Runtime.AnyRef, IJSObject, IDisposable
        {
            internal object? RawObject;

            // to detect redundant calls
            public bool IsDisposed { get; internal set; }

            public JSObject() : this(Runtime.New<object>())
            {
                var result = Runtime.BindCoreObject(JSHandle, (int)(IntPtr)Handle, out int exception);
                if (exception != 0)
                    throw new JSException($"JSObject Error binding: {result.ToString()}");

            }

            internal JSObject(IntPtr js_handle) : base(js_handle)
            {
                //Console.WriteLine ($"JSObject: {js_handle}");
            }

            internal JSObject(int js_handle) : base((IntPtr)js_handle)
            {
                //Console.WriteLine ($"JSObject: {js_handle}");
            }

            internal JSObject(int js_handle, object raw_obj) : base(js_handle)
            {
                RawObject = raw_obj;
            }

            public object Invoke(string method, params object?[] args)
            {
                var res = Runtime.InvokeJSWithArgs(JSHandle, method, args, out int exception);
                if (exception != 0)
                    throw new JSException((string)res);
                return res;
            }

            public object GetObjectProperty(string name)
            {

                var propertyValue = Runtime.GetObjectProperty(JSHandle, name, out int exception);

                if (exception != 0)
                    throw new JSException((string)propertyValue);

                return propertyValue;

            }

            public void SetObjectProperty(string name, object value, bool createIfNotExists = true, bool hasOwnProperty = false)
            {

                var setPropResult = Runtime.SetObjectProperty(JSHandle, name, value, createIfNotExists, hasOwnProperty, out int exception);
                if (exception != 0)
                    throw new JSException($"Error setting {name} on (js-obj js '{JSHandle}' mono '{(IntPtr)Handle} raw '{RawObject != null})");

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

            protected void FreeHandle()
            {
                Runtime.ReleaseHandle(JSHandle, out int exception);
                if (exception != 0)
                    throw new JSException($"Error releasing handle on (js-obj js '{JSHandle}' mono '{(IntPtr)Handle} raw '{RawObject != null})");
            }

            public override bool Equals(object? obj)
            {
                if (obj == null || GetType() != obj.GetType())
                {
                    return false;
                }
                return JSHandle == (obj as JSObject)?.JSHandle;
            }

            public override int GetHashCode()
            {
                return JSHandle;
            }

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
                return $"(js-obj js '{JSHandle}' mono '{(IntPtr)Handle} raw '{RawObject != null})";
            }

        }
    }
}
