// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    public static class Runtime
    {
        // / <summary>
        // / Execute the provided string in the JavaScript context
        // / </summary>
        // / <returns>The js.</returns>
        // / <param name="str">String.</param>
        public static string InvokeJS(string str)
        {
            return Interop.Runtime.InvokeJS(str);
        }

        public static System.Runtime.InteropServices.JavaScript.Function? CompileFunction(string snippet)
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
            lock (_rawToJS)
            {
                if (_rawToJS.TryGetValue(obj, out JSObject? jsobj))
                {
                    //raw_to_js [obj].RawObject = null;
                    _rawToJS.Remove(obj);
                    if (jsobj != null)
                    {
                        int exception;
                        Interop.Runtime.ReleaseObject(jsobj.JSHandle, out exception);
                        if (exception != 0)
                            throw new JSException($"Error releasing object on (raw-obj)");

                        jsobj.JSHandle = -1;
                        jsobj.RawObject = null;
                        jsobj.IsDisposed = true;
                        jsobj.Handle.Free();
                    }
                }
                else
                {
                    throw new JSException($"Error releasing object on (obj)");
                }
            }
        }
        public static object GetGlobalObject(string? str = null)
        {
            return Interop.Runtime.GetGlobalObject(str);
        }

        private static readonly Dictionary<int, JSObject?> _boundObjects = new Dictionary<int, JSObject?>();
        private static readonly Dictionary<object, JSObject?> _rawToJS = new Dictionary<object, JSObject?>();

        public static int BindJSObject(int jsId, int mappedType)
        {
            JSObject? obj;
            lock (_boundObjects)
            {
                if (!_boundObjects.TryGetValue(jsId, out obj))
                {
                  IntPtr jsIntPtr = (IntPtr)jsId;
                  obj = mappedType > 0 ? BindJSType(jsIntPtr, mappedType) : new JSObject(jsIntPtr);
                  _boundObjects.Add (jsId, obj);
                }
            }
            return obj == null ? 0 : (int)(IntPtr)obj.Handle;
        }

        public static int BindCoreCLRObject(int jsId, int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            JSObject? obj;

            lock (_boundObjects)
            {
                if (_boundObjects.TryGetValue(jsId, out JSObject? existingObj))
                {
                    if (existingObj?.Handle != h && h.IsAllocated)
                        throw new JSException($"Multiple handles pointing at js_id: {jsId}");

                    obj = existingObj;
                }
                else
                {
                    obj = h.Target as JSObject;
                }
                return obj == null ? 0 : (int)(IntPtr)obj.Handle;
            }
        }

        private static JSObject BindJSType(IntPtr jsIntPtr, int coreType)
        {
            CoreObject coreObject;
            switch (coreType)
            {
                case 1:
                    coreObject = new Array(jsIntPtr);
                    break;
                case 2:
                    coreObject = new ArrayBuffer(jsIntPtr);
                    break;
                case 3:
                    coreObject = new DataView(jsIntPtr);
                    break;
                case 4:
                    coreObject = new Function(jsIntPtr);
                    break;
                case 5:
                    coreObject = new Map(jsIntPtr);
                    break;
                case 6:
                    coreObject = new SharedArrayBuffer(jsIntPtr);
                    break;
                case 10:
                    coreObject = new Int8Array(jsIntPtr);
                    break;
                case 11:
                    coreObject = new Uint8Array(jsIntPtr);
                    break;
                case 12:
                    coreObject = new Uint8ClampedArray(jsIntPtr);
                    break;
                case 13:
                    coreObject = new Int16Array(jsIntPtr);
                    break;
                case 14:
                    coreObject = new Uint16Array(jsIntPtr);
                    break;
                case 15:
                    coreObject = new Int32Array(jsIntPtr);
                    break;
                case 16:
                    coreObject = new Uint32Array(jsIntPtr);
                    break;
                case 17:
                    coreObject = new Float32Array(jsIntPtr);
                    break;
                case 18:
                    coreObject = new Float64Array(jsIntPtr);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(coreType));
            }
            return coreObject;
        }

        public static int UnBindJSObject(int jsId)
        {
            lock (_boundObjects)
            {
                if (_boundObjects.TryGetValue(jsId, out JSObject? obj))
                {
                    _boundObjects.Remove(jsId);
                    return obj == null ? 0 : (int)(IntPtr)obj.Handle;
                }
                return 0;
            }
        }

        public static void UnBindJSObjectAndFree(int jsId)
        {
            lock (_boundObjects)
            {
                if (_boundObjects.TryGetValue(jsId, out JSObject? obj))
                {
                    if (_boundObjects[jsId] != null)
                    {
                        _boundObjects.Remove(jsId);
                    }
                    if (obj != null)
                    {
                        obj.JSHandle = -1;
                        obj.IsDisposed = true;
                        obj.RawObject = null;
                        obj.Handle.Free();
                    }
                }
            }
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

                    int exception;
                    Interop.Runtime.ReleaseHandle(obj.JSHandle, out exception);
                    if (exception != 0)
                        throw new JSException($"Error releasing handle on (js-obj js '{obj.JSHandle}' .NET '{(IntPtr)obj.Handle} raw '{obj.RawObject != null})");

                    // Calling Release Handle above only removes the reference from the JavaScript side but does not
                    // release the bridged JSObject associated with the raw object so we have to do that ourselves.
                    obj.JSHandle = -1;
                    obj.IsDisposed = true;
                    obj.RawObject = null;

                    obj.Handle.Free();
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
            JSObject? obj = rawObj as JSObject;
            lock (_rawToJS)
            {
                if (obj == null && !_rawToJS.TryGetValue(rawObj, out obj))
                    _rawToJS[rawObj] = obj = new JSObject(jsId, rawObj);

                return obj == null ? 0 : (int)(IntPtr)obj.Handle;
            }
        }

        public static int GetJSObjectId(object rawObj)
        {
            JSObject? obj = rawObj as JSObject;
            lock (_rawToJS)
            {
                if (obj == null && !_rawToJS.TryGetValue(rawObj, out obj))
                    return -1;

                return obj?.JSHandle ?? -1;
            }
        }

        public static object? GetDotNetObject(int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            JSObject? o = h.Target as JSObject;
            return o?.RawObject ?? null;
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
