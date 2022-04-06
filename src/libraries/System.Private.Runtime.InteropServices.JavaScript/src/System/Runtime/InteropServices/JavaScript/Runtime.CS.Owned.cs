// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Runtime.InteropServices.JavaScript
{
    public static partial class Runtime
    {
        private static readonly Dictionary<int, WeakReference<JSObject>> _csOwnedObjects = new Dictionary<int, WeakReference<JSObject>>();

        public static void GetCSOwnedObjectByJSHandleRef(IntPtr jsHandle, int shouldAddInflight, out JSObject? result)
        {
            lock (_csOwnedObjects)
            {
                if (_csOwnedObjects.TryGetValue((int)jsHandle, out WeakReference<JSObject>? reference))
                {
                    reference.TryGetTarget(out JSObject? jsObject);
                    if (shouldAddInflight != 0 && jsObject != null)
                    {
                        jsObject.AddInFlight();
                    }
                    result = jsObject;
                    return;
                }
            }
            result = null;
        }

        public static IntPtr TryGetCSOwnedObjectJSHandleRef(in object rawObj, int shouldAddInflight)
        {
            JSObject? jsObject = rawObj as JSObject;
            if (jsObject != null && shouldAddInflight != 0)
            {
                jsObject.AddInFlight();
            }
            return jsObject?.JSHandle ?? IntPtr.Zero;
        }

        public static IntPtr GetCSOwnedObjectJSHandleRef(in JSObject jsObject, int shouldAddInflight)
        {
            jsObject.AssertNotDisposed();

            if (shouldAddInflight != 0)
            {
                jsObject.AddInFlight();
            }
            return jsObject.JSHandle;
        }

        public static void CreateCSOwnedProxyRef(IntPtr jsHandle, MappedType mappedType, int shouldAddInflight, out JSObject? jsObject)
        {
            jsObject = null;

            lock (_csOwnedObjects)
            {
                if (!_csOwnedObjects.TryGetValue((int)jsHandle, out WeakReference<JSObject>? reference) ||
                    !reference.TryGetTarget(out jsObject) ||
                    jsObject.IsDisposed)
                {
                    jsObject = mappedType switch
                    {
                        MappedType.JSObject => new JSObject(jsHandle),
                        MappedType.Array => new Array(jsHandle),
                        MappedType.ArrayBuffer => new ArrayBuffer(jsHandle),
                        MappedType.DataView => new DataView(jsHandle),
                        MappedType.Function => new Function(jsHandle),
                        MappedType.Uint8Array => new Uint8Array(jsHandle),
                        _ => throw new ArgumentOutOfRangeException(nameof(mappedType))
                    };
                    _csOwnedObjects[(int)jsHandle] = new WeakReference<JSObject>(jsObject, trackResurrection: true);
                }
            }
            if (shouldAddInflight != 0)
            {
                jsObject.AddInFlight();
            }
        }

        #region used from C# side

        internal static bool ReleaseCSOwnedObject(JSObject objToRelease)
        {
            objToRelease.AssertNotDisposed();

            lock (_csOwnedObjects)
            {
                _csOwnedObjects.Remove((int)objToRelease.JSHandle);
                Interop.Runtime.ReleaseCSOwnedObject(objToRelease.JSHandle);
            }
            return true;
        }

        internal static IntPtr CreateCSOwnedObject(JSObject proxy, string typeName, params object[] parms)
        {
            Interop.Runtime.CreateCSOwnedObjectRef(typeName, parms, out int exception, out object res);
            if (exception != 0)
                throw new JSException((string)res);

            var jsHandle = (IntPtr)(int)res;

            lock (_csOwnedObjects)
            {
                _csOwnedObjects[(int)jsHandle] = new WeakReference<JSObject>(proxy, trackResurrection: true);
            }

            return (IntPtr)jsHandle;
        }

        #endregion


        // please keep BINDING wasm_type_symbol in sync
        public enum MappedType
        {
            JSObject = 0,
            Array = 1,
            ArrayBuffer = 2,
            DataView = 3,
            Function = 4,
            Uint8Array = 11,
        }
    }
}
