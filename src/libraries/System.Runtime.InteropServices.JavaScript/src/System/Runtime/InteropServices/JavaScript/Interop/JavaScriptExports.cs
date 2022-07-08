﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static unsafe partial class JavaScriptExports
    {
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

        public static IntPtr TryGetCSOwnedObjectJSHandleRef(in object rawObj, int shouldAddInflight)
        {
            JSObject? jsObject = rawObj as JSObject;
            if (jsObject != null && shouldAddInflight != 0)
            {
                jsObject.AddInFlight();
            }
            return jsObject?.JSHandle ?? IntPtr.Zero;
        }

        public static void CreateCSOwnedProxyRef(IntPtr jsHandle, JSHostImplementation.MappedType mappedType, int shouldAddInflight, out JSObject? jsObject)
        {
            jsObject = null;

            lock (JSHostImplementation.s_csOwnedObjects)
            {
                if (!JSHostImplementation.s_csOwnedObjects.TryGetValue((int)jsHandle, out WeakReference<JSObject>? reference) ||
                    !reference.TryGetTarget(out jsObject) ||
                    jsObject.IsDisposed)
                {
#pragma warning disable CS0612 // Type or member is obsolete
                    jsObject = mappedType switch
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
                    JSHostImplementation.s_csOwnedObjects[(int)jsHandle] = new WeakReference<JSObject>(jsObject, trackResurrection: true);
                }
            }
            if (shouldAddInflight != 0)
            {
                jsObject.AddInFlight();
            }
        }

        public static void GetJSOwnedObjectByGCHandleRef(int gcHandle, out object result)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            result = h.Target!;
        }

        public static IntPtr GetJSOwnedObjectGCHandleRef(in object obj)
        {
            return JSHostImplementation.GetJSOwnedObjectGCHandleRef(obj, GCHandleType.Normal);
        }

        // The JS layer invokes this method when the JS wrapper for a JS owned object
        //  has been collected by the JS garbage collector
        public static void ReleaseJSOwnedObjectByGCHandle(IntPtr gcHandle)
        {
            GCHandle handle = (GCHandle)gcHandle;
            lock (JSHostImplementation.s_gcHandleFromJSOwnedObject)
            {
                JSHostImplementation.s_gcHandleFromJSOwnedObject.Remove(handle.Target!);
                handle.Free();
            }
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

        public static void TaskFromResultRef(in object obj, out object result)
        {
            result = Task.FromResult(obj);
        }

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

        public static void CreateUriRef(string uri, out Uri result)
        {
            result = new Uri(uri);
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
                var mt = JSHostImplementation.GetMarshalTypeFromType(t);
                result[i] = JSHostImplementation.GetCallSignatureCharacterForMarshalType(mt, null);
            }

            return new string(result);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void StopProfile()
        {
        }

        // Called by the AOT profiler to save profile data into INTERNAL.aot_profile_data
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static unsafe void DumpAotProfileData(ref byte buf, int len, string extraArg)
        {
            if (len == 0)
                throw new JSException("Profile data length is 0");

            var arr = new byte[len];
            fixed (void* p = &buf)
            {
                var span = new ReadOnlySpan<byte>(p, len);
                // Send it to JS
#pragma warning disable CS0612 // Type or member is obsolete
                var internalJS = (JSObject)JavaScriptImports.GetGlobalObject("INTERNAL");
                if (internalJS == null) throw new InvalidOperationException();
                internalJS.SetObjectProperty("aot_profile_data", Uint8Array.From(span));
#pragma warning restore CS0612 // Type or member is obsolete
            }
        }
    }
}
