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
        private static object JSOwnedObjectLock = new object();
        // we use this to maintain identity of GCHandle for a managed object
        private static Dictionary<object, int> GCHandleFromJSOwnedObject = new Dictionary<object, int>();

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

        public static object GetGlobalObject(string? str = null)
        {
            return Interop.Runtime.GetGlobalObject(str);
        }

        public static void DumpAotProfileData (ref byte buf, int len, string extraArg)
        {
            Interop.Runtime.DumpAotProfileData(ref buf, len, extraArg);
        }

        public static int BindJSObject(int jsHandle, bool ownsHandle, int mappedType)
        {
            JSObject? target = null;

            lock (_boundObjects)
            {
                if (!_boundObjects.TryGetValue(jsHandle, out WeakReference<JSObject>? reference) ||
                    !reference.TryGetTarget(out target) ||
                    target.IsDisposed)
                {
                    IntPtr jsIntPtr = (IntPtr)jsHandle;
                    target = mappedType > 0 ? BindJSType(jsIntPtr, ownsHandle, mappedType) : new JSObject(jsIntPtr, ownsHandle);
                    _boundObjects[jsHandle] = new WeakReference<JSObject>(target, trackResurrection: true);
                }
            }

            target.AddInFlight();

            return target.GCHandleValue;
        }

        public static int BindCoreCLRObject(int jsHandle, int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            JSObject? obj = null;

            lock (_boundObjects)
            {
                if (_boundObjects.TryGetValue(jsHandle, out WeakReference<JSObject>? wr))
                {

                    if (!wr.TryGetTarget(out JSObject? instance) || (instance.GCHandleValue != (int)(IntPtr)h && h.IsAllocated))
                    {
                        throw new JSException(SR.Format(SR.MultipleHandlesPointingJsId, jsHandle));
                    }

                    obj = instance;
                }
                else if (h.Target is JSObject instance)
                {
                    _boundObjects.Add(jsHandle, new WeakReference<JSObject>(instance, trackResurrection: true));
                    obj = instance;
                }
            }

            return obj?.GCHandleValue ?? 0;
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
                throw new JSException($"Error releasing handle on (js-obj js '{objToRelease.JSHandle}' mono '{objToRelease.GCHandleValue})");

            lock (_boundObjects)
            {
                _boundObjects.Remove(objToRelease.JSHandle);
            }
            return true;
        }

        public static int CreateTaskSource()
        {
            var tcs= new TaskCompletionSource<object>();
            return GetJSOwnedObjectGCHandle(tcs);
        }

        public static void SetTaskSourceResult(int tcsGCHandle, object result)
        {
            GCHandle handle = (GCHandle)(IntPtr)tcsGCHandle;
            // this is JS owned Normal handle. We always have a Target
            TaskCompletionSource<object> tcs = (TaskCompletionSource<object>)handle.Target!;
            tcs.SetResult(result);
        }

        public static void SetTaskSourceFailure(int tcsGCHandle, string reason)
        {
            GCHandle handle = (GCHandle)(IntPtr)tcsGCHandle;
            // this is JS owned Normal handle. We always have a Target
            TaskCompletionSource<object> tcs = (TaskCompletionSource<object>)handle.Target!;
            tcs.SetException(new JSException(reason));
        }

        public static object GetTaskSourceTask(int tcsGCHandle)
        {
            GCHandle handle = (GCHandle)(IntPtr)tcsGCHandle;
            // this is JS owned Normal handle. We always have a Target
            TaskCompletionSource<object> tcs = (TaskCompletionSource<object>)handle.Target!;
            return tcs.Task;
        }

        // A JSOwnedObject is a managed object with its lifetime controlled by javascript.
        // The managed side maintains a strong reference to the object, while the JS side
        //  maintains a weak reference and notifies the managed side if the JS wrapper object
        //  has been reclaimed by the JS GC. At that point, the managed side will release its
        //  strong references, allowing the managed object to be collected.
        // This ensures that things like delegates and promises will never 'go away' while JS
        //  is expecting to be able to invoke or await them.
        public static int GetJSOwnedObjectGCHandle (object o) {
            if (o == null)
                return 0;

            int result;
            lock (JSOwnedObjectLock) {
                if (GCHandleFromJSOwnedObject.TryGetValue(o, out result))
                    return result;

                result = (int)(IntPtr)GCHandle.Alloc(o, GCHandleType.Normal);
                GCHandleFromJSOwnedObject[o] = result;
                return result;
            }
        }

        // The JS layer invokes this method when the JS wrapper for a JS owned object
        //  has been collected by the JS garbage collector
        public static void ReleaseJSOwnedObjectByHandle (int gcHandle) {
            GCHandle handle = (GCHandle)(IntPtr)gcHandle;
            lock (JSOwnedObjectLock) {
                GCHandleFromJSOwnedObject.Remove(handle.Target!);
                handle.Free();
            }
        }

        public static int GetJSObjectId(object rawObj)
        {
            JSObject? jsObject = rawObj as JSObject;
            return jsObject?.JSHandle ?? -1;
        }

        /// <param name="gcHandle"></param>
        /// <param name="shouldAddInflight">when true, we would create Normal GCHandle to the JSObject, so that it would not get collected before passing it back to managed code</param>
        public static object? GetDotNetObject(int gcHandle, int shouldAddInflight)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;

            if (h.Target is JSObject jso)
            {
                if (shouldAddInflight != 0)
                {
                    jso.AddInFlight();
                }
                return jso;
            }
            return h.Target;
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

        public static void SafeHandleReleaseByHandle(int jsHandle)
        {
#if DEBUG_HANDLE
            Debug.WriteLine($"SafeHandleReleaseByHandle: {jsHandle}");
#endif
            lock (_boundObjects)
            {
                if (_boundObjects.TryGetValue(jsHandle, out WeakReference<JSObject>? reference))
                {
                    reference.TryGetTarget(out JSObject? target);
                    Debug.Assert(target != null, $"\tSafeHandleReleaseByHandle: did not find active target {jsHandle}");
                    SafeHandleRelease(target);
                }
                else
                {
                    Debug.Fail($"\tSafeHandleReleaseByHandle: did not find reference for {jsHandle}");
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
