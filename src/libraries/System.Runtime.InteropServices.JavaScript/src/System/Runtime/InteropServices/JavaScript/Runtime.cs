// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Console = System.Diagnostics.Debug;

namespace System.Runtime.InteropServices.JavaScript
{
    public static class Runtime
    {

        private static readonly Dictionary<int, WeakReference> _boundObjects = new Dictionary<int, WeakReference>();
        private static readonly Dictionary<object, JSObject?> _rawToJS = new Dictionary<object, JSObject?>();
        // _weakDelegateTable is a ConditionalWeakTable with the Delegate and associated JSObject:
        // Key Lifetime:
        //    Once the key dies, the dictionary automatically removes the key/value entry.
        // No need to lock as it is thread safe.
        private static readonly ConditionalWeakTable<Delegate, JSObject?> _weakDelegateTable = new ConditionalWeakTable<Delegate, JSObject?>();

        // <summary>
        // Execute the provided string in the JavaScript context
        // </summary>
        // <returns>The js.</returns>
        // <param name="str">String.</param>
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
            if (obj.GetType().IsSubclassOf(typeof(Delegate)))
            {
                return;
            }

            JSObject? jsobj;
            lock (_rawToJS)
            {
                if (!_rawToJS.Remove(obj, out jsobj))
                {
                    throw new JSException($"Error releasing object {obj}");
                }
            }
        }

        public static object GetGlobalObject(string? str = null)
        {
            return Interop.Runtime.GetGlobalObject(str);
        }

        public static int BindJSObject(int jsId, bool ownsHandle, int mappedType)
        {
            lock (_boundObjects)
            {
                if (!_boundObjects.TryGetValue(jsId, out WeakReference? obj))
                {
                    IntPtr jsIntPtr = (IntPtr)jsId;
                    obj = new WeakReference(mappedType > 0 ? BindJSType(jsIntPtr, ownsHandle, mappedType) : new JSObject(jsIntPtr, ownsHandle), true);
                    _boundObjects.Add(jsId, obj);
                }
                return obj.Target is JSObject target ? target.Int32Handle : 0;
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
                    _boundObjects.Add(jsId, new WeakReference(obj, true));
                }

                return obj?.Int32Handle ?? 0;
            }
        }

        private static JSObject BindJSType(IntPtr jsIntPtr, bool ownsHandle, int coreType) =>
            coreType switch
            {
                1 => new Array(jsIntPtr, ownsHandle),
                2 => new ArrayBuffer(jsIntPtr, ownsHandle),
                3 => new DataView(jsIntPtr, ownsHandle),
                4 => new Function(jsIntPtr, ownsHandle),
                5 => new Map(jsIntPtr, ownsHandle),
                6 => new SharedArrayBuffer(jsIntPtr, ownsHandle),
                10 => new Int8Array(jsIntPtr, ownsHandle),
                11 => new Uint8Array(jsIntPtr, ownsHandle),
                12 => new Uint8ClampedArray(jsIntPtr, ownsHandle),
                13 => new Int16Array(jsIntPtr, ownsHandle),
                14 => new Uint16Array(jsIntPtr, ownsHandle),
                15 => new Int32Array(jsIntPtr, ownsHandle),
                16 => new Uint32Array(jsIntPtr, ownsHandle),
                17 => new Float32Array(jsIntPtr, ownsHandle),
                18 => new Float64Array(jsIntPtr, ownsHandle),
                _ => throw new ArgumentOutOfRangeException(nameof(coreType))
            };

        public static bool ReleaseJSObject(JSObject objToRelease)
        {
            Interop.Runtime.ReleaseHandle(objToRelease.JSHandle, out int exception);
            if (exception != 0)
                throw new JSException($"Error releasing handle on (js-obj js '{objToRelease.JSHandle}' mono '{objToRelease.Int32Handle} raw '{objToRelease.RawObject != null}' weak raw '{objToRelease.WeakRawObject?.Target != null}'   )");

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
                        throw new JSException($"Error releasing handle on (js-obj js '{obj.JSHandle}' mono '{obj.Int32Handle} raw '{obj.RawObject != null})");

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
                    if (obj == null && dele != null && !_weakDelegateTable.TryGetValue(dele, out obj))
                    {
                        obj = new JSObject(jsId, true);
                        _boundObjects[jsId] = new WeakReference(obj);
                        _weakDelegateTable.Add(dele, obj);
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
                return obj == null ? 0 : obj.Int32Handle;
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
                        lock (_weakDelegateTable)
                            _weakDelegateTable.TryGetValue(dele, out obj);
                    }
                }
                if (_rawToJS.TryGetValue(rawObj, out JSObject? ojs))
                    return ojs?.JSHandle ?? -1;
            }
            return -1;
        }

        public static object? GetDotNetObject(int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;

            return h.Target is JSObject js ?
                (js.WeakRawObject is WeakReference weakRawObject ? weakRawObject.Target :
                    js.RawObject ?? h.Target) : h.Target;
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
                        else if (t == typeof(SafeHandle))
                        {
                            res[c] = 'h';
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

        public static bool SafeHandleAddRef(SafeHandle safeHandle)
        {
            bool _addRefSucceeded = false;
#if DEBUG_HANDLE
            var _anyref = safeHandle as AnyRef;
#endif
            try
            {
                safeHandle.DangerousAddRef(ref _addRefSucceeded);
#if DEBUG_HANDLE
                if (_addRefSucceeded && _anyref != null)
                    _anyref.AddRef();
#endif
            }
            catch
            {
                if (_addRefSucceeded)
                {
                    safeHandle.DangerousRelease();
#if DEBUG_HANDLE
                    if (_anyref != null)
                        _anyref.Release();
#endif
                    _addRefSucceeded = false;
                }
            }
#if DEBUG_HANDLE
            Console.WriteLine($"\tSafeHandleAddRef: {safeHandle.DangerousGetHandle()} / RefCount: {((_anyref == null) ? 0 : _anyref.RefCount)}");
#endif
            return _addRefSucceeded;
        }

        public static void SafeHandleRelease(SafeHandle safeHandle)
        {
            safeHandle.DangerousRelease();
#if DEBUG_HANDLE
            var _anyref = safeHandle as AnyRef;
            if (_anyref != null)
            {
                _anyref.Release();
                Console.WriteLine($"\tSafeHandleRelease: {safeHandle.DangerousGetHandle()} / RefCount: {_anyref.RefCount}");
            }
#endif
        }

        public static void SafeHandleReleaseByHandle(int jsId)
        {
#if DEBUG_HANDLE
            Console.WriteLine($"SafeHandleReleaseByHandle: {jsId}");
#endif
            lock (_boundObjects)
            {
                if (_boundObjects.TryGetValue(jsId, out WeakReference? reference))
                {
                    if (reference.Target != null)
                    {
                        SafeHandleRelease((AnyRef)reference.Target);
                    }
                    else
                    {
                        Console.WriteLine($"\tSafeHandleReleaseByHandle: did not find active target {jsId} / target: {reference.Target}");
                    }

                }
                else
                {
                    Console.WriteLine($"\tSafeHandleReleaseByHandle: did not find reference for {jsId}");
                }
            }

        }

        public static IntPtr SafeHandleGetHandle(SafeHandle safeHandle, bool addRef)
        {
#if DEBUG_HANDLE
            Console.WriteLine($"SafeHandleGetHandle: {safeHandle.DangerousGetHandle()} / addRef {addRef}");
#endif
            if (addRef)
                if (SafeHandleAddRef(safeHandle))
                    return safeHandle.DangerousGetHandle();
                else
                    return IntPtr.Zero;
            return safeHandle.DangerousGetHandle();
        }

    }
}
