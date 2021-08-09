// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    public static class Runtime
    {
        private static readonly Dictionary<int, WeakReference<JSObject>> _csOwnedObjects = new Dictionary<int, WeakReference<JSObject>>();
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

        public static object GetGlobalObject(string? str = null)
        {
            return Interop.Runtime.GetGlobalObject(str);
        }

        public static void DumpAotProfileData (ref byte buf, int len, string extraArg)
        {
            Interop.Runtime.DumpAotProfileData(ref buf, len, extraArg);
        }

        public static int CsOwnedObjectGetJsHandle(JSObject target, bool addRef)
        {
            if (addRef)
            {
                target.AddInFlight();
            }
            return target.JSHandle;
        }

        public static void ReleaseCsOwnedObjectByHandle(int jsHandle)
        {
            lock (_csOwnedObjects)
            {
                if (_csOwnedObjects.TryGetValue(jsHandle, out WeakReference<JSObject>? reference))
                {
                    reference.TryGetTarget(out JSObject? target);
                    Debug.Assert(target != null, $"\tSafeHandleReleaseByHandle: did not find active target {jsHandle}");

                    target.ReleaseInFlight();
                }
                else
                {
                    Debug.Fail($"\tSafeHandleReleaseByHandle: did not find reference for {jsHandle}");
                }
            }
        }

        internal static bool ReleaseCsOwnedObject(JSObject objToRelease)
        {
            lock (_csOwnedObjects)
            {
                Interop.Runtime.ReleaseCsOwnedObject(objToRelease.JSHandle);
                _csOwnedObjects.Remove(objToRelease.JSHandle);
            }
            return true;
        }

        public static int CreateCSOwnedObject(int jsHandle, int mappedType)
        {
            JSObject? target = null;

            lock (_csOwnedObjects)
            {
                if (!_csOwnedObjects.TryGetValue(jsHandle, out WeakReference<JSObject>? reference) ||
                    !reference.TryGetTarget(out target) ||
                    target.IsDisposed)
                {
                    target = mappedType > 0 ? BindJSType((IntPtr)jsHandle, mappedType) : new JSObject((IntPtr)jsHandle);
                    _csOwnedObjects[jsHandle] = new WeakReference<JSObject>(target, trackResurrection: true);
                }
            }

            target.AddInFlight();

            return target.GCHandleValue;
        }

        public static int TryGetCsOwnedObjectJsHandle(object rawObj)
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


        private static JSObject BindJSType(IntPtr jsHandle, int coreType) =>
            coreType switch
            {
                1 => new Array(jsHandle),
                2 => new ArrayBuffer(jsHandle),
                3 => new DataView(jsHandle),
                4 => new Function(jsHandle),
                5 => new Map(jsHandle),
                6 => new SharedArrayBuffer(jsHandle),
                10 => new Int8Array(jsHandle),
                11 => new Uint8Array(jsHandle),
                12 => new Uint8ClampedArray(jsHandle),
                13 => new Int16Array(jsHandle),
                14 => new Uint16Array(jsHandle),
                15 => new Int32Array(jsHandle),
                16 => new Uint32Array(jsHandle),
                17 => new Float32Array(jsHandle),
                18 => new Float64Array(jsHandle),
                _ => throw new ArgumentOutOfRangeException(nameof(coreType))
            };

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
    }
}
