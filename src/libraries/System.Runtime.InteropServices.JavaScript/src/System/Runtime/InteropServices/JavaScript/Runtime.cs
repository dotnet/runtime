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
            // We no longer need to free on delegates.
            // Leave this here for now so it does not break code.
            if (obj.GetType().IsSubclassOf(typeof(Delegate)))
            {
                return;
            }

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

                        jsobj.SetHandleAsInvalid();
                        jsobj.RawObject = null;
                        jsobj.IsDisposed = true;
                        jsobj.AnyRefHandle.Free();
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

        //private static readonly Dictionary<int, JSObject?> _boundObjects = new Dictionary<int, JSObject?>();
        private static readonly Dictionary<int, WeakReference> _boundObjects = new Dictionary<int, WeakReference>();
        // weak_delegate_table is a ConditionalWeakTable with the Delegate and associated JSObject:
        // Key Lifetime:
        //    Once the key dies, the dictionary automatically removes the key/value entry.
        // No need to lock as it is thread safe.
        private static readonly ConditionalWeakTable<Delegate, JSObject?> weak_delegate_table = new ConditionalWeakTable<Delegate, JSObject?>();

        private static readonly Dictionary<object, JSObject?> _rawToJS = new Dictionary<object, JSObject?>();

        public static int BindJSObject(int jsId, bool ownsHandle, Type mappedType)
        {
            lock (_boundObjects)
            {
                if (!_boundObjects.TryGetValue(jsId, out WeakReference? obj))
                {
                    if (mappedType != null)
                    {
                        return BindJSType(jsId, ownsHandle, mappedType);
                    }
                    else
                    {
                        _boundObjects[jsId] = obj = new WeakReference(new JSObject((IntPtr)jsId, ownsHandle), true);
                    }
                }
                JSObject? target = obj.Target as JSObject;
                return target == null ? 0 : (int)(IntPtr)target.AnyRefHandle;
            }
        }

        public static int BindCoreCLRObject(int jsId, int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            JSObject? obj;

            lock (_boundObjects)
            {
                if (_boundObjects.TryGetValue(jsId, out WeakReference? existingObj))
                {
                    var instance = existingObj?.Target as JSObject;
                    if (instance?.AnyRefHandle != h && h.IsAllocated)
                        throw new JSException($"Multiple handles pointing at jsId: {jsId}");

                    obj = instance;
                }
                else
                {
                    obj = h.Target as JSObject;
                }
                if (obj == null)
                    return 0;

                _boundObjects[jsId] = new WeakReference(obj, true);
                return (int)(IntPtr)obj.AnyRefHandle;
            }
        }

        public static int BindJSType(int jsId, bool ownsHandle, Type mappedType)
        {
            lock (_boundObjects)
            {
                if (!_boundObjects.TryGetValue(jsId, out WeakReference? reference))
                {
                    ConstructorInfo? jsobjectnew = mappedType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.ExactBinding,
                            null, new Type[] { typeof(IntPtr), typeof(bool) }, null);
                    JSObject? newJSType = jsobjectnew?.Invoke(new object[] { (IntPtr)jsId, ownsHandle }) as JSObject;
                    if (newJSType == null)
                        return -1;
                    _boundObjects[jsId] = reference = new WeakReference(newJSType, true);
                }
                JSObject? refTarget = reference?.Target as JSObject;
                return refTarget == null ? 0 : (int)(IntPtr)refTarget.AnyRefHandle;
            }
        }

        public static int UnBindJSObject(int jsId)
        {
            lock (_boundObjects)
            {
                if (_boundObjects.TryGetValue(jsId, out WeakReference? reference))
                {
                    _boundObjects.Remove(jsId);
                    JSObject? refTarget = reference?.Target as JSObject;
                    return refTarget == null ? 0 : (int)(IntPtr)refTarget.AnyRefHandle;
                }
                return 0;
            }
        }

        public static void UnBindJSObjectAndFree(int jsId)
        {
            lock (_boundObjects)
            {
                if (_boundObjects.TryGetValue(jsId, out WeakReference? reference))
                {
                    object? instance = reference.Target;
                    if (instance == null)
                    {
                        _boundObjects.Remove(jsId);
                    }
                    else
                    {

                        ((JSObject)instance).RawObject = null;
                        ((JSObject)instance).WeakRawObject = null;
                        _boundObjects.Remove(jsId);
                        if (reference?.Target is JSObject instanceJS)
                        {
                            instanceJS.SetHandleAsInvalid();
                            instanceJS.IsDisposed = true;
                            instanceJS.RawObject = null;
                            instanceJS.AnyRefHandle.Free();
                        }
                    }
                }
            }
        }
        public static bool ReleaseJSObject(JSObject objToRelease)
        {
            Interop.Runtime.ReleaseHandle(objToRelease.JSHandle, out int exception);
            if (exception != 0)
                throw new JSException($"Error releasing handle on (js-obj js '{objToRelease.JSHandle}' mono '{(IntPtr)objToRelease.AnyRefHandle} raw '{objToRelease.RawObject != null}' weak raw '{objToRelease.WeakRawObject?.Target != null}'   )");

            lock (_boundObjects)
            {
                _boundObjects.Remove(objToRelease.JSHandle);
                objToRelease.SetHandleAsInvalid();
                objToRelease.IsDisposed = true;
                objToRelease.RawObject = null;
                objToRelease.WeakRawObject = null;
                objToRelease.AnyRefHandle.Free();
            }
            return true;
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

                    Interop.Runtime.ReleaseHandle(obj.JSHandle, out int exception);
                    if (exception != 0)
                        throw new JSException($"Error releasing handle on (js-obj js '{obj.JSHandle}' mono '{(IntPtr)obj.AnyRefHandle} raw '{obj.RawObject != null})");

                    // Calling Release Handle above only removes the reference from the JavaScript side but does not
                    // release the bridged JSObject associated with the raw object so we have to do that ourselves.
                    obj.SetHandleAsInvalid();
                    obj.IsDisposed = true;
                    obj.RawObject = null;
                    obj.AnyRefHandle.Free();
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
                if (rawObj.GetType().IsSubclassOf(typeof(Delegate)))
                {
                    Delegate? dele = rawObj as Delegate;
                    if (obj == null && dele != null && !weak_delegate_table.TryGetValue(dele, out obj))
                    {
                        obj = new JSObject(jsId, true);
                        _boundObjects[jsId] = new WeakReference(obj);
                        weak_delegate_table.Add(dele, obj);
                        obj.WeakRawObject = new WeakReference(dele, false);
                    }
                }
                else
                {
                    if (obj == null && !_rawToJS.TryGetValue(rawObj, out obj))
                    {
                        _rawToJS[rawObj] = obj = new JSObject(jsId, rawObj);
                    }
                }
                return obj == null ? 0 : (int)(IntPtr)obj.AnyRefHandle;
            }
        }

        public static int GetJSObjectId(object rawObj)
        {
            JSObject? obj = rawObj as JSObject;
            lock (_rawToJS)
            {
                if (obj is null && rawObj.GetType().IsSubclassOf(typeof(Delegate)))
                {
                    Delegate? dele = rawObj as Delegate;
                    if (dele != null)
                    {
                        lock (weak_delegate_table)
                            weak_delegate_table.TryGetValue(dele, out obj);
                    }
                }
                if (obj is null && !_rawToJS.TryGetValue(rawObj, out obj))
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

        public static object? GetCoreType(string coreObj)
        {
            Assembly asm = typeof(Runtime).Assembly;
            Type? type = asm.GetType(coreObj);
            return type;

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
