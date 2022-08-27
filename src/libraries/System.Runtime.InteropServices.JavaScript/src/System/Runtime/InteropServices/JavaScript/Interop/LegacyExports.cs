// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    // this maps to src\mono\wasm\runtime\legacy\corebindings.ts
    // the methods are protected from trimming by DynamicDependency on JSFunctionBinding.BindJSFunction
    internal static unsafe partial class LegacyExports
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void GetCSOwnedObjectByJSHandleRef(IntPtr jsHandle, int shouldAddInflight, out JSObject? result)
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

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static IntPtr GetCSOwnedObjectJSHandleRef(in JSObject jsObject, int shouldAddInflight)
        {
            jsObject.AssertNotDisposed();

            if (shouldAddInflight != 0)
            {
                jsObject.AddInFlight();
            }
            return jsObject.JSHandle;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static IntPtr TryGetCSOwnedObjectJSHandleRef(in object rawObj, int shouldAddInflight)
        {
            JSObject? jsObject = rawObj as JSObject;
            if (jsObject != null && shouldAddInflight != 0)
            {
                jsObject.AddInFlight();
            }
            return jsObject?.JSHandle ?? IntPtr.Zero;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void CreateCSOwnedProxyRef(IntPtr jsHandle, JSHostImplementation.MappedType mappedType, int shouldAddInflight, out JSObject jsObject)
        {
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
                        JSHostImplementation.MappedType.JSObject => new JSObject(jsHandle),
                        JSHostImplementation.MappedType.Array => new Array(jsHandle),
                        JSHostImplementation.MappedType.ArrayBuffer => new ArrayBuffer(jsHandle),
                        JSHostImplementation.MappedType.DataView => new DataView(jsHandle),
                        JSHostImplementation.MappedType.Function => new Function(jsHandle),
                        JSHostImplementation.MappedType.Uint8Array => new Uint8Array(jsHandle),
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

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void GetJSOwnedObjectByGCHandleRef(int gcHandle, out object result)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            result = h.Target!;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static IntPtr GetJSOwnedObjectGCHandleRef(in object obj)
        {
            return JSHostImplementation.GetJSOwnedObjectGCHandle(obj, GCHandleType.Normal);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static IntPtr CreateTaskSource()
        {
            var tcs = new TaskCompletionSource<object>();
            return GetJSOwnedObjectGCHandleRef(tcs);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void SetTaskSourceResultRef(int tcsGCHandle, in object result)
        {
            GCHandle handle = (GCHandle)(IntPtr)tcsGCHandle;
            // this is JS owned Normal handle. We always have a Target
            TaskCompletionSource<object> tcs = (TaskCompletionSource<object>)handle.Target!;
            tcs.SetResult(result);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void SetTaskSourceFailure(int tcsGCHandle, string reason)
        {
            GCHandle handle = (GCHandle)(IntPtr)tcsGCHandle;
            // this is JS owned Normal handle. We always have a Target
            TaskCompletionSource<object> tcs = (TaskCompletionSource<object>)handle.Target!;
            tcs.SetException(new JSException(reason));
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void GetTaskSourceTaskRef(int tcsGCHandle, out object result)
        {
            GCHandle handle = (GCHandle)(IntPtr)tcsGCHandle;
            // this is JS owned Normal handle. We always have a Target
            TaskCompletionSource<object> tcs = (TaskCompletionSource<object>)handle.Target!;
            result = tcs.Task;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void SetupJSContinuationRef(in Task _task, JSObject continuationObj)
        {
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

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static string ObjectToStringRef(ref object o)
        {
            return o.ToString() ?? string.Empty;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void CreateDateTimeRef(double ticks, out object result)
        {
            DateTimeOffset unixTime = DateTimeOffset.FromUnixTimeMilliseconds((long)ticks);
            result = unixTime.DateTime;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void CreateUriRef(string uri, out object? result)
        {
            // we do this via reflection to allow linker to trim dependency on URI and it's assembly
            // if the user code has methods with Uri signature, this should work too
            // System.Private.Uri is large assembly so it's worth trimming
            var uriType = Type.GetType("System.Uri, System.Private.Uri");
            if (uriType == null) throw new InvalidProgramException();
            result = Activator.CreateInstance(uriType, uri);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static bool IsSimpleArrayRef(ref object a)
        {
            return a is System.Array arr && arr.Rank == 1 && arr.GetLowerBound(0) == 0;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
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
                var mt = JSHostImplementation.GetMarshalTypeFromType(t);
                result[i] = JSHostImplementation.GetCallSignatureCharacterForMarshalType(mt, null);
            }

            return new string(result);
        }
    }
}
