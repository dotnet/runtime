// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    public static class Runtime
    {
        private static readonly Dictionary<int, WeakReference<JSObject>> _boundObjects = new Dictionary<int, WeakReference<JSObject>>();
        private static readonly Dictionary<object, JSObject> _rawToJS = new Dictionary<object, JSObject>();
        // _weakDelegateTable is a ConditionalWeakTable with the Delegate and associated JSObject:
        // Key Lifetime:
        //    Once the key dies, the dictionary automatically removes the key/value entry.
        // No need to lock as it is thread safe.
        private static readonly ConditionalWeakTable<Delegate, JSObject> _weakDelegateTable = new ConditionalWeakTable<Delegate, JSObject>();

        private const string TaskGetResultName = "get_Result";
        private static readonly MethodInfo _taskGetResultMethodInfo = typeof(Task<>).GetMethod(TaskGetResultName)!;

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
            if (obj is Delegate)
            {
                return;
            }

            JSObject? jsobj;
            lock (_rawToJS)
            {
                if (!_rawToJS.Remove(obj, out jsobj))
                {
                    throw new JSException(SR.Format(SR.ErrorReleasingObject, obj));
                }
            }
        }

        public static object GetGlobalObject(string? str = null)
        {
            return Interop.Runtime.GetGlobalObject(str);
        }

        public static void DumpAotProfileData (ref byte buf, int len, string extraArg)
        {
            Interop.Runtime.DumpAotProfileData(ref buf, len, extraArg);
        }

        public static int BindJSObject(int jsId, bool ownsHandle, int mappedType)
        {
            JSObject? target = null;

            lock (_boundObjects)
            {
                if (!_boundObjects.TryGetValue(jsId, out WeakReference<JSObject>? reference) ||
                    !reference.TryGetTarget(out target) ||
                    target.IsDisposed)
                {
                    IntPtr jsIntPtr = (IntPtr)jsId;
                    target = mappedType > 0 ? BindJSType(jsIntPtr, ownsHandle, mappedType) : new JSObject(jsIntPtr, ownsHandle);
                    _boundObjects[jsId] = new WeakReference<JSObject>(target, trackResurrection: true);
                }
            }

            return target.Int32Handle;
        }

        public static int BindCoreCLRObject(int jsId, int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            JSObject? obj = null;

            lock (_boundObjects)
            {
                if (_boundObjects.TryGetValue(jsId, out WeakReference<JSObject>? wr))
                {
                    if (!wr.TryGetTarget(out JSObject? instance) || (instance.Int32Handle != (int)(IntPtr)h && h.IsAllocated))
                    {
                        throw new JSException(SR.Format(SR.MultipleHandlesPointingJsId, jsId));
                    }

                    obj = instance;
                }
                else if (h.Target is JSObject instance)
                {
                    _boundObjects.Add(jsId, new WeakReference<JSObject>(instance, trackResurrection: true));
                    obj = instance;
                }
            }

            return obj?.Int32Handle ?? 0;
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

        internal static bool ReleaseJSObject(JSObject objToRelease)
        {
            Interop.Runtime.ReleaseHandle(objToRelease.JSHandle, out int exception);
            if (exception != 0)
                throw new JSException($"Error releasing handle on (js-obj js '{objToRelease.JSHandle}' mono '{objToRelease.Int32Handle} raw '{objToRelease.RawObject != null}' weak raw '{objToRelease.IsWeakWrapper}'   )");

            lock (_boundObjects)
            {
                _boundObjects.Remove(objToRelease.JSHandle);
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
                    obj.FreeHandle();
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
            JSObject? jsObject;
            if (rawObj is Delegate dele)
            {
                jsObject = new JSObject(jsId, dele);
                lock (_boundObjects)
                {
                    _boundObjects.Add(jsId, new WeakReference<JSObject>(jsObject));
                }
                lock (_weakDelegateTable)
                {
                    _weakDelegateTable.Add(dele, jsObject);
                }
            }
            else
            {
                lock (_rawToJS)
                {
                    if (!_rawToJS.TryGetValue(rawObj, out jsObject))
                    {
                        _rawToJS.Add(rawObj, jsObject = new JSObject(jsId, rawObj));
                    }
                }
            }
            return jsObject.Int32Handle;
        }

        public static int GetJSObjectId(object rawObj)
        {
            JSObject? jsObject;
            if (rawObj is Delegate dele)
            {
                lock (_weakDelegateTable)
                {
                    _weakDelegateTable.TryGetValue(dele, out jsObject);
                }
            }
            else
            {
                lock (_rawToJS)
                {
                    _rawToJS.TryGetValue(rawObj, out jsObject);
                }
            }
            return jsObject?.JSHandle ?? -1;
        }

        public static object? GetDotNetObject(int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;

            return h.Target is JSObject js ?
                js.GetWrappedObject() ?? h.Target : h.Target;
        }

        public static bool IsSimpleArray(object a)
        {
            return a is System.Array arr && arr.Rank == 1 && arr.GetLowerBound(0) == 0;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IntPtrAndHandle
        {
            [FieldOffset(0)]
            internal IntPtr ptr;

            [FieldOffset(0)]
            internal RuntimeMethodHandle handle;
        }

        public static string GetCallSignature(IntPtr methodHandle, object objForRuntimeType)
        {
            IntPtrAndHandle tmp = default(IntPtrAndHandle);
            tmp.ptr = methodHandle;

            MethodBase? mb = objForRuntimeType == null ? MethodBase.GetMethodFromHandle(tmp.handle) : MethodBase.GetMethodFromHandle(tmp.handle, Type.GetTypeHandle(objForRuntimeType));
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
                                throw new NotSupportedException(SR.ValueTypeNotSupported);
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
                            result = GetTaskResultMethodInfo(task_type)?.Invoke(task, null);
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

        /// <summary>
        /// Gets the MethodInfo for the Task{T}.Result property getter.
        /// </summary>
        /// <remarks>
        /// This ensures the returned MethodInfo is strictly for the Task{T} type, and not
        /// a "Result" property on some other class that derives from Task or a "new Result"
        /// property on a class that derives from Task{T}.
        ///
        /// The reason for this restriction is to make this use of Reflection trim-compatible,
        /// ensuring that trimming doesn't change the application's behavior.
        /// </remarks>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Task<T>.Result is preserved by the ILLinker because _taskGetResultMethodInfo was initialized with it.")]
        private static MethodInfo? GetTaskResultMethodInfo(Type taskType)
        {
            MethodInfo? result = taskType.GetMethod(TaskGetResultName);
            if (result != null && result.HasSameMetadataDefinitionAs(_taskGetResultMethodInfo))
            {
                return result;
            }

            return null;
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
                throw new InvalidCastException(SR.Format(SR.UnableCastObjectToType, dtv.GetType(), typeof(DateTime)));
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
            Debug.WriteLine($"\tSafeHandleAddRef: {safeHandle.DangerousGetHandle()} / RefCount: {((_anyref == null) ? 0 : _anyref.RefCount)}");
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
                Debug.WriteLine($"\tSafeHandleRelease: {safeHandle.DangerousGetHandle()} / RefCount: {_anyref.RefCount}");
            }
#endif
        }

        public static void SafeHandleReleaseByHandle(int jsId)
        {
#if DEBUG_HANDLE
            Debug.WriteLine($"SafeHandleReleaseByHandle: {jsId}");
#endif
            lock (_boundObjects)
            {
                if (_boundObjects.TryGetValue(jsId, out WeakReference<JSObject>? reference))
                {
                    reference.TryGetTarget(out JSObject? target);
                    Debug.Assert(target != null, $"\tSafeHandleReleaseByHandle: did not find active target {jsId}");
                    SafeHandleRelease(target);
                }
                else
                {
                    Debug.Fail($"\tSafeHandleReleaseByHandle: did not find reference for {jsId}");
                }
            }
        }

        public static IntPtr SafeHandleGetHandle(SafeHandle safeHandle, bool addRef)
        {
#if DEBUG_HANDLE
            Debug.WriteLine($"SafeHandleGetHandle: {safeHandle.DangerousGetHandle()} / addRef {addRef}");
#endif
            if (addRef && !SafeHandleAddRef(safeHandle)) return IntPtr.Zero;
            return safeHandle.DangerousGetHandle();
        }

    }
}
