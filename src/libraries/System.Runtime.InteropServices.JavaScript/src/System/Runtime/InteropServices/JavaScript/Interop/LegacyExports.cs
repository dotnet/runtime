// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static unsafe partial class LegacyExportsTrimmingRoot
    {
        // the public methods are used from JavaScript, but the trimmer doesn't know about it.
        // It's protected by DynamicDependencyAttribute on JSFunctionBinding.BindJSFunction.
        public static void TrimWhenNotWasmEnableLegacyJsInterop()
        {
            // if MSBuild property WasmEnableLegacyJsInterop==false this call would be substituted away and LegacyExports would be trimmed.
            LegacyExports.PreventTrimming();
        }
    }

    internal static unsafe partial class LegacyExports
    {
        // the public methods of this class are used from JavaScript, but the trimmer doesn't know about it.
        // They are protected by LegacyExportsTrimmingRoot.PreventTrimming and JSFunctionBinding.BindJSFunction.
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LegacyExports))]
        internal static void PreventTrimming()
        {
        }

        public static void GetCSOwnedObjectByJSHandleRef(nint jsHandle, int shouldAddInflight, out JSObject? result)
        {
            lock (JSHostImplementation.s_csOwnedObjects)
            {
                if (JSHostImplementation.s_csOwnedObjects.TryGetValue((int)jsHandle, out WeakReference<JSObject>? reference))
                {
                    reference.TryGetTarget(out JSObject? jsObject);
                    if (shouldAddInflight != 0)
                    {
                        jsObject?.AddInFlight();
                    }
                    result = jsObject;
                    return;
                }
            }
            result = null;
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

        public static IntPtr TryGetCSOwnedObjectJSHandleRef(in object rawObj, int shouldAddInflight)
        {
            JSObject? jsObject = rawObj as JSObject;
            if (jsObject != null && shouldAddInflight != 0)
            {
                jsObject.AddInFlight();
            }
            return jsObject?.JSHandle ?? IntPtr.Zero;
        }

        public static void CreateCSOwnedProxyRef(nint jsHandle, LegacyHostImplementation.MappedType mappedType, int shouldAddInflight, out JSObject jsObject)
        {
#if FEATURE_WASM_THREADS
            LegacyHostImplementation.ThrowIfLegacyWorkerThread();
#endif

            JSObject? res = null;

            lock (JSHostImplementation.s_csOwnedObjects)
            {
                if (!JSHostImplementation.s_csOwnedObjects.TryGetValue((int)jsHandle, out WeakReference<JSObject>? reference) ||
                    !reference.TryGetTarget(out res) ||
                    res.IsDisposed)
                {
#pragma warning disable CS0612 // Type or member is obsolete
                    res = mappedType switch
                    {
                        LegacyHostImplementation.MappedType.JSObject => new JSObject(jsHandle),
                        LegacyHostImplementation.MappedType.Array => new Array(jsHandle),
                        LegacyHostImplementation.MappedType.ArrayBuffer => new ArrayBuffer(jsHandle),
                        LegacyHostImplementation.MappedType.DataView => new DataView(jsHandle),
                        LegacyHostImplementation.MappedType.Function => new Function(jsHandle),
                        LegacyHostImplementation.MappedType.Uint8Array => new Uint8Array(jsHandle),
                        _ => throw new ArgumentOutOfRangeException(nameof(mappedType))
                    };
#pragma warning restore CS0612 // Type or member is obsolete
                    JSHostImplementation.s_csOwnedObjects[(int)jsHandle] = new WeakReference<JSObject>(res, trackResurrection: true);
                }
            }
            if (shouldAddInflight != 0)
            {
                res.AddInFlight();
            }
            jsObject = res;
        }

        public static void GetJSOwnedObjectByGCHandleRef(int gcHandle, out object result)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            result = h.Target!;
        }

        public static IntPtr GetJSOwnedObjectGCHandleRef(in object obj)
        {
            return JSHostImplementation.GetJSOwnedObjectGCHandle(obj, GCHandleType.Normal);
        }

        public static IntPtr CreateTaskSource()
        {
            var tcs = new TaskCompletionSource<object>();
            return GetJSOwnedObjectGCHandleRef(tcs);
        }

        public static void SetTaskSourceResultRef(int tcsGCHandle, in object result)
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

        public static void GetTaskSourceTaskRef(int tcsGCHandle, out object result)
        {
            GCHandle handle = (GCHandle)(IntPtr)tcsGCHandle;
            // this is JS owned Normal handle. We always have a Target
            TaskCompletionSource<object> tcs = (TaskCompletionSource<object>)handle.Target!;
            result = tcs.Task;
        }

        public static void SetupJSContinuationRef(in Task _task, JSObject continuationObj)
        {
#if FEATURE_WASM_THREADS
            LegacyHostImplementation.ThrowIfLegacyWorkerThread();
#endif
            // HACK: Attempting to use the in-param will produce CS1628, so we make a temporary copy
            //  on the stack that can be captured by our local functions below
            var task = _task;

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
                            result = JSHostImplementation.GetTaskResultMethodInfo(task_type)?.Invoke(task, null);
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

        public static string ObjectToStringRef(ref object o)
        {
            return o.ToString() ?? string.Empty;
        }

        public static double GetDateValueRef(ref object dtv)
        {
            ArgumentNullException.ThrowIfNull(dtv);

            if (!(dtv is DateTime dt))
                throw new InvalidCastException(SR.Format(SR.UnableCastObjectToType, dtv.GetType(), typeof(DateTime)));
            if (dt.Kind == DateTimeKind.Local)
                dt = dt.ToUniversalTime();
            else if (dt.Kind == DateTimeKind.Unspecified)
                dt = new DateTime(dt.Ticks, DateTimeKind.Utc);
            return new DateTimeOffset(dt).ToUnixTimeMilliseconds();
        }

        // HACK: We need to implicitly box by using an 'object' out-param.
        // Note that the return value would have been boxed on the C#->JS transition anyway.
        public static void CreateDateTimeRef(double ticks, out object result)
        {
            DateTimeOffset unixTime = DateTimeOffset.FromUnixTimeMilliseconds((long)ticks);
            result = unixTime.DateTime;
        }

        // we do this via reflection to allow trimming tools to trim dependency on Uri class and it's assembly
        // if the user code has methods with Uri signature, they probably also have the Uri constructor
        // if they don't have it, they could configure ILLing to protect it after they enabled trimming
        // We believe that this code path is probably not even used in the wild
        // System.Private.Uri is ~80KB large assembly so it's worth trimming
        private static Type? uriType;

        [Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2077", Justification = "Done on purpose, see comment above.")]
        [Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Done on purpose, see comment above.")]
        public static void CreateUriRef(string uri, out object? result)
        {
#if FEATURE_WASM_THREADS
            LegacyHostImplementation.ThrowIfLegacyWorkerThread();
#endif
            if (uriType == null)
            {
                // StringBuilder to confuse ILLink, which is too smart otherwise
                StringBuilder sb = new StringBuilder("System.Uri, System.Private.Uri");
                uriType = Type.GetType(sb.ToString());
            }
            // See: https://devblogs.microsoft.com/dotnet/customizing-trimming-in-net-core-5/
            if (uriType == null) throw new InvalidOperationException(SR.UriTypeMissing);
            try
            {
                result = Activator.CreateInstance(uriType, uri);
            }
            catch (MissingMethodException ex)
            {
                throw new MissingMethodException(SR.UriConstructorMissing, ex);
            }
        }

        public static bool IsSimpleArrayRef(ref object a)
        {
            return a is System.Array arr && arr.Rank == 1 && arr.GetLowerBound(0) == 0;
        }

        public static string GetCallSignatureRef(IntPtr _methodHandle, in object objForRuntimeType)
        {
            var methodHandle = JSHostImplementation.GetMethodHandleFromIntPtr(_methodHandle);

            MethodBase? mb = objForRuntimeType is null ? MethodBase.GetMethodFromHandle(methodHandle) : MethodBase.GetMethodFromHandle(methodHandle, Type.GetTypeHandle(objForRuntimeType));
            if (mb is null)
                return string.Empty;

            ParameterInfo[] parms = mb.GetParameters();
            int parmsLength = parms.Length;
            if (parmsLength == 0)
                return string.Empty;

            var result = new char[parmsLength];
            for (int i = 0; i < parmsLength; i++)
            {
                Type t = parms[i].ParameterType;
                var mt = LegacyHostImplementation.GetMarshalTypeFromType(t);
                result[i] = LegacyHostImplementation.GetCallSignatureCharacterForMarshalType(mt, null);
            }

            return new string(result);
        }
    }
}
