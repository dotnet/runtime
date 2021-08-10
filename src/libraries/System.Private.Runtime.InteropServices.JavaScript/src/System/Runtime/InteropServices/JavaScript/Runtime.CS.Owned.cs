// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Runtime.InteropServices.JavaScript
{
    public static partial class Runtime
    {
        private static readonly Dictionary<int, WeakReference<JSObject>> _csOwnedObjects = new Dictionary<int, WeakReference<JSObject>>();

        public static int GetCsOwnedObjectJsHandle(JSObject target, int shouldAddInflight)
        {
            if (shouldAddInflight != 0)
            {
                target.AddInFlight();
            }
            return target.JSHandle;
        }

        internal static bool ReleaseCsOwnedObject(JSObject objToRelease)
        {
            lock (_csOwnedObjects)
            {
                _csOwnedObjects.Remove(objToRelease.JSHandle);
                Interop.Runtime.ReleaseCsOwnedObject(objToRelease.JSHandle);
            }
            return true;
        }

        internal static IntPtr CreateCsOwnedObject(JSObject proxy, string typeName, params object[] parms)
        {
            object res = Interop.Runtime.CreateCsOwnedObject(typeName, parms, out int exception);
            if (exception != 0)
                throw new JSException((string)res);

            var jsHandle = (int)res;

            lock (_csOwnedObjects)
            {
                _csOwnedObjects[jsHandle] = new WeakReference<JSObject>(proxy, trackResurrection: true);
            }

            return (IntPtr)jsHandle;
        }

        public static JSObject CreateCsOwnedProxy(IntPtr jsHandle, MappedType mappedType)
        {
            JSObject? target = null;

            lock (_csOwnedObjects)
            {
                if (!_csOwnedObjects.TryGetValue((int)jsHandle, out WeakReference<JSObject>? reference) ||
                    !reference.TryGetTarget(out target) ||
                    target.IsDisposed)
                {
                    target = mappedType switch
                    {
                        MappedType.JSObject => new JSObject(jsHandle),
                        MappedType.Array => new Array(jsHandle),
                        MappedType.ArrayBuffer => new ArrayBuffer(jsHandle),
                        MappedType.DataView => new DataView(jsHandle),
                        MappedType.Function => new Function(jsHandle),
                        MappedType.Map => new Map(jsHandle),
                        MappedType.SharedArrayBuffer => new SharedArrayBuffer(jsHandle),
                        MappedType.Int8Array => new Int8Array(jsHandle),
                        MappedType.Uint8Array => new Uint8Array(jsHandle),
                        MappedType.Uint8ClampedArray => new Uint8ClampedArray(jsHandle),
                        MappedType.Int16Array => new Int16Array(jsHandle),
                        MappedType.Uint16Array => new Uint16Array(jsHandle),
                        MappedType.Int32Array => new Int32Array(jsHandle),
                        MappedType.Uint32Array => new Uint32Array(jsHandle),
                        MappedType.Float32Array => new Float32Array(jsHandle),
                        MappedType.Float64Array => new Float64Array(jsHandle),
                        _ => throw new ArgumentOutOfRangeException(nameof(mappedType))
                    };
                    _csOwnedObjects[(int)jsHandle] = new WeakReference<JSObject>(target, trackResurrection: true);
                }
            }

            target.AddInFlight();

            return target;
        }

        public static int TryGetCsOwnedObjectJsHandle(object rawObj)
        {
            JSObject? jsObject = rawObj as JSObject;
            return jsObject?.JSHandle ?? 0;
        }

        public static JSObject? GetCSOwnedObjectByJsHandle(int jsHandle, int shouldAddInflight)
        {
            lock (_csOwnedObjects)
            {
                if (_csOwnedObjects.TryGetValue(jsHandle, out WeakReference<JSObject>? reference))
                {
                    reference.TryGetTarget(out JSObject? jso);
                    if (shouldAddInflight != 0 && jso != null)
                    {
                        jso.AddInFlight();
                    }
                    return jso;
                }
            }
            return null;

        }

        // please keep BINDING wasm_type_symbol in sync
        public enum MappedType
        {
            JSObject = 0,
            Array = 1,
            ArrayBuffer = 2,
            DataView = 3,
            Function = 4,
            Map = 5,
            SharedArrayBuffer = 6,
            Int8Array = 10,
            Uint8Array = 11,
            Uint8ClampedArray = 12,
            Int16Array = 13,
            Uint16Array = 14,
            Int32Array = 15,
            Uint32Array = 16,
            Float32Array = 17,
            Float64Array = 18,
        }
    }
}
