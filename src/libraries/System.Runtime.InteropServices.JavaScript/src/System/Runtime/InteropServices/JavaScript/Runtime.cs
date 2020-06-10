// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    public static class Runtime
    {
        private static readonly Dictionary<int, JSObject> _boundObjects = new Dictionary<int, JSObject>();
        private static readonly Dictionary<object, JSObject> _rawToJS = new Dictionary<object, JSObject>();

        // / <summary>
        // / Execute the provided string in the JavaScript context
        // / </summary>
        // / <returns>The js.</returns>
        // / <param name="str">String.</param>
        public static string InvokeJS(string str)
        {
            return Interop.Runtime.InvokeJS(str);
        }

        public static Function? CompileFunction(string snippet)
        {
            return Interop.Runtime.CompileFunction(snippet);
        }

        public static int New<T>(params object[] parms)
        {
            return Interop.Runtime.New(typeof(T).Name, parms);
        }

        public static int New(string hostClassName, params object[] parms)
        {
            return Interop.Runtime.New(hostClassName, parms);
        }

        public static void FreeObject(object obj)
        {
            JSObject? jsobj;
            lock (_rawToJS)
            {
                if (!_rawToJS.Remove(obj, out jsobj))
                {
                    throw new JSException($"Error releasing object {obj}");
                }
            }

            jsobj.ReleaseHandle();
        }

        public static object GetGlobalObject(string? str = null)
        {
            return Interop.Runtime.GetGlobalObject(str);
        }

        public static int BindJSObject(int jsId, int mappedType)
        {
            JSObject? obj;
            lock (_boundObjects)
            {
                if (!_boundObjects.TryGetValue(jsId, out obj))
                {
                    IntPtr jsIntPtr = (IntPtr)jsId;
                    obj = mappedType > 0 ? BindJSType(jsIntPtr, mappedType) : new JSObject(jsIntPtr);
                    _boundObjects.Add(jsId, obj);
                }
            }

            return obj.Int32Handle;
        }

        public static int BindCoreCLRObject(int jsId, int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            JSObject? obj;

            lock (_boundObjects)
            {
                if (_boundObjects.TryGetValue(jsId, out JSObject? existingObj))
                {
                    if (existingObj.Handle != h && h.IsAllocated)
                        throw new JSException($"Multiple handles pointing at js_id: {jsId}");

                    obj = existingObj;
                }
                else
                {
                    obj = h.Target as JSObject;
                }
            }

            return obj?.Int32Handle ?? 0;
        }

        private static JSObject BindJSType(IntPtr jsIntPtr, int coreType) =>
            coreType switch
            {
                1 => new Array(jsIntPtr),
                2 => new ArrayBuffer(jsIntPtr),
                3 => new DataView(jsIntPtr),
                4 => new Function(jsIntPtr),
                5 => new Map(jsIntPtr),
                6 => new SharedArrayBuffer(jsIntPtr),
                10 => new Int8Array(jsIntPtr),
                11 => new Uint8Array(jsIntPtr),
                12 => new Uint8ClampedArray(jsIntPtr),
                13 => new Int16Array(jsIntPtr),
                14 => new Uint16Array(jsIntPtr),
                15 => new Int32Array(jsIntPtr),
                16 => new Uint32Array(jsIntPtr),
                17 => new Float32Array(jsIntPtr),
                18 => new Float64Array(jsIntPtr),
                _ => throw new ArgumentOutOfRangeException(nameof(coreType))
            };

        public static int UnBindJSObject(int jsId)
        {
            lock (_boundObjects)
            {
                return _boundObjects.Remove(jsId, out JSObject? obj) ? obj.Int32Handle : 0;
            }
        }

        public static void UnBindJSObjectAndFree(int jsId)
        {
            JSObject? obj;
            lock (_boundObjects)
            {
                _boundObjects.Remove(jsId, out obj);
            }

            obj?.ReleaseHandle();
        }

        public static void UnBindRawJSObjectAndFree(int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            JSObject? obj = h.Target as JSObject;
            lock (_rawToJS)
            {
                if (obj?.RawObject != null)
                {
                    _rawToJS.Remove(obj.RawObject);

                    obj.ReleaseHandle();
                }
            }
        }

        public static object CreateTaskSource(int jsId)
        {
            return new TaskCompletionSource<object>();
        }

        public static void SetTaskSourceResult(TaskCompletionSource<object> tcs, object result)
        {
            tcs.SetResult(result);
        }

        public static void SetTaskSourceFailure(TaskCompletionSource<object> tcs, string reason)
        {
            tcs.SetException(new JSException(reason));
        }

        public static int GetTaskAndBind(TaskCompletionSource<object> tcs, int jsId)
        {
            return BindExistingObject(tcs.Task, jsId);
        }

        public static int BindExistingObject(object rawObj, int jsId)
        {
            var obj = rawObj as JSObject;
            if (obj != null)
                return obj.Int32Handle;

            lock (_rawToJS)
            {
                if (!_rawToJS.TryGetValue(rawObj, out obj)) {
                    obj = new JSObject(jsId, rawObj);
                    _rawToJS.Add(rawObj, obj);
                }
            }

            return obj.Int32Handle;
        }

        public static int GetJSObjectId(object rawObj)
        {
            if (rawObj is JSObject js)
                return js.JSHandle;

            lock (_rawToJS)
            {
                if (_rawToJS.TryGetValue(rawObj, out JSObject? ojs))
                    return ojs.JSHandle;
            }

            return -1;
        }

        public static object? GetDotNetObject(int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            return h.Target is JSObject js ? js.RawObject : null;
        }

        public static object BoxInt(int i)
        {
            return i;
        }

        public static object BoxDouble(double d)
        {
            return d;
        }

        public static object BoxBool(int b)
        {
            return b == 0 ? false : true;
        }

        public static bool IsSimpleArray(object a)
        {
            return a is System.Array arr && arr.Rank == 1 && arr.GetLowerBound(0) == 0;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct IntPtrAndHandle
        {
            [FieldOffset(0)]
            internal IntPtr ptr;

            [FieldOffset(0)]
            internal RuntimeMethodHandle handle;
        }

        public static string GetCallSignature(IntPtr methodHandle)
        {
            IntPtrAndHandle tmp = default(IntPtrAndHandle);
            tmp.ptr = methodHandle;

            MethodBase? mb = MethodBase.GetMethodFromHandle(tmp.handle);
            if (mb == null)
                return string.Empty;

            ParameterInfo[] parms = mb.GetParameters();
            int parmsLength = parms.Length;
            if (parmsLength == 0)
                return string.Empty;

            char[] res = new char[parmsLength];

            for (int c = 0; c < parmsLength; c++)
            {
                Type t = parms[c].ParameterType;
                switch (Type.GetTypeCode(t))
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Boolean:
                        // Enums types have the same code as their underlying numeric types
                        if (t.IsEnum)
                            res[c] = 'j';
                        else
                            res[c] = 'i';
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        // Enums types have the same code as their underlying numeric types
                        if (t.IsEnum)
                            res[c] = 'k';
                        else
                            res[c] = 'l';
                        break;
                    case TypeCode.Single:
                        res[c] = 'f';
                        break;
                    case TypeCode.Double:
                        res[c] = 'd';
                        break;
                    case TypeCode.String:
                        res[c] = 's';
                        break;
                    default:
                        if (t == typeof(IntPtr))
                        {
                            res[c] = 'i';
                        }
                        else if (t == typeof(Uri))
                        {
                            res[c] = 'u';
                        }
                        else
                        {
                            if (t.IsValueType)
                                throw new NotSupportedException("ValueType arguments are not supported.");
                            res[c] = 'o';
                        }
                        break;
                }
            }
            return new string(res);
        }

        public static void SetupJSContinuation(Task task, JSObject continuationObj)
        {
            if (task.IsCompleted)
                Complete();
            else
                task.GetAwaiter().OnCompleted(Complete);

            void Complete()
            {
                try
                {
                    if (task.Exception == null)
                    {
                        object? result;
                        Type task_type = task.GetType();
                        if (task_type == typeof(Task))
                        {
                            result = System.Array.Empty<object>();
                        }
                        else
                        {
                            result = task_type.GetMethod("get_Result")?.Invoke(task, System.Array.Empty<object>());
                        }
                        continuationObj.Invoke("resolve", result);
                    }
                    else
                    {
                        continuationObj.Invoke("reject", task.Exception.ToString());
                    }
                }
                catch (Exception e)
                {
                    continuationObj.Invoke("reject", e.ToString());
                }
                finally
                {
                    continuationObj.Dispose();
                    FreeObject(task);
                }
            }
        }

        public static string ObjectToString(object o)
        {
            return o.ToString() ?? string.Empty;
        }

        public static double GetDateValue(object dtv)
        {
            if (dtv == null)
                throw new ArgumentNullException(nameof(dtv));
            if (!(dtv is DateTime dt))
                throw new InvalidCastException($"Unable to cast object of type {dtv.GetType()} to type DateTime.");
            if (dt.Kind == DateTimeKind.Local)
                dt = dt.ToUniversalTime();
            else if (dt.Kind == DateTimeKind.Unspecified)
                dt = new DateTime(dt.Ticks, DateTimeKind.Utc);
            return new DateTimeOffset(dt).ToUnixTimeMilliseconds();
        }

        public static DateTime CreateDateTime(double ticks)
        {
            DateTimeOffset unixTime = DateTimeOffset.FromUnixTimeMilliseconds((long)ticks);
            return unixTime.DateTime;
        }

        public static Uri CreateUri(string uri)
        {
            return new Uri(uri);
        }
    }
}
