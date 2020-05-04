// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using JSObject = Interop.JavaScript.JSObject;
using JSException = Interop.JavaScript.JSException;

internal static partial class Interop
{
    internal static partial class Runtime
    {
        private static Dictionary<int, JSObject?> _boundObjects = new Dictionary<int, JSObject?>();
        private static Dictionary<object, JSObject?> _rawToJS = new Dictionary<object, JSObject?>();

        internal static int BindJSObject(int jsId, Type mappedType)
        {
            if (!_boundObjects.TryGetValue(jsId, out JSObject? obj))
            {
                if (mappedType != null)
                {
                    return BindJSType(jsId, mappedType);
                }
                else
                {
                    _boundObjects[jsId] = obj = new JSObject((IntPtr)jsId);
                }
            }
            return obj == null ? 0 : (int)(IntPtr)obj.Handle;
        }

        internal static int BindCoreCLRObject(int jsId, int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            JSObject? obj = h.Target as JSObject;

            if (_boundObjects.TryGetValue(jsId, out var existingObj))
            {
                if (existingObj?.Handle != h && h.IsAllocated)
                    throw new JSException($"Multiple handles pointing at js_id: {jsId}");

                obj = existingObj;
            }
            else
                _boundObjects[jsId] = obj;

            return obj == null ? 0 : (int)(IntPtr)obj.Handle;
        }

        internal static int BindJSType(int jsId, Type mappedType)
        {
            if (!_boundObjects.TryGetValue(jsId, out JSObject? obj))
            {
                var jsobjectnew = mappedType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.ExactBinding,
                        null, new Type[] { typeof(IntPtr) }, null);
                _boundObjects[jsId] = obj = jsobjectnew == null ? null : (JSObject)jsobjectnew.Invoke(new object[] { (IntPtr)jsId });
            }
            return obj == null ? 0 : (int)(IntPtr)obj.Handle;
        }

        internal static int UnBindJSObject(int jsId)
        {
            if (_boundObjects.TryGetValue(jsId, out var obj))
            {
                _boundObjects.Remove(jsId);
                return obj == null ? 0 : (int)(IntPtr)obj.Handle;
            }

            return 0;
        }

        internal static void UnBindJSObjectAndFree(int jsId)
        {
            if (_boundObjects.TryGetValue(jsId, out var obj))
            {
                if (_boundObjects[jsId] != null)
                {
                    //bound_objects[jsIs].RawObject = null;
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
        internal static void UnBindRawJSObjectAndFree(int gcHandle)
        {

            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            JSObject? obj = h.Target as JSObject;
            if (obj?.RawObject != null)
            {
                _rawToJS.Remove(obj.RawObject);

                int exception;
                ReleaseHandle(obj.JSHandle, out exception);
                if (exception != 0)
                    throw new JSException($"Error releasing handle on (js-obj js '{obj.JSHandle}' mono '{(IntPtr)obj.Handle} raw '{obj.RawObject != null})");

                // Calling Release Handle above only removes the reference from the JavaScript side but does not
                // release the bridged JSObject associated with the raw object so we have to do that ourselves.
                obj.JSHandle = -1;
                obj.IsDisposed = true;
                obj.RawObject = null;

                obj.Handle.Free();
            }

        }
        internal static object CreateTaskSource(int jsId)
        {
            return new TaskCompletionSource<object>();
        }

        internal static void SetTaskSourceResult(TaskCompletionSource<object> tcs, object result)
        {
            tcs.SetResult(result);
        }

        internal static void SetTaskSourceFailure(TaskCompletionSource<object> tcs, string reason)
        {
            tcs.SetException(new JSException(reason));
        }

        internal static int GetTaskAndBind(TaskCompletionSource<object> tcs, int jsId)
        {
            return BindExistingObject(tcs.Task, jsId);
        }

        internal static int BindExistingObject(object rawObj, int jsId)
        {
            JSObject? obj = rawObj as JSObject;

            if (obj == null && !_rawToJS.TryGetValue(rawObj, out obj))
                _rawToJS[rawObj] = obj = new JSObject(jsId, rawObj);

            return obj == null ? 0 : (int)(IntPtr)obj.Handle;
        }
        internal static int GetJSObjectId(object rawObj)
        {
            JSObject? obj = rawObj as JSObject;

            if (obj == null && !_rawToJS.TryGetValue(rawObj, out obj))
                return -1;

            return obj?.JSHandle ?? -1;
        }

        internal static object? GetMonoObject(int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            JSObject? o = h.Target as JSObject;
            return o?.RawObject ?? null;
        }

        internal static object BoxInt(int i)
        {
            return i;
        }
        internal static object BoxDouble(double d)
        {
            return d;
        }

        internal static object BoxBool(int b)
        {
            return b == 0 ? false : true;
        }

        internal static bool IsSimpleArray(object a)
        {
            if (a is Array arr)
            {
                if (arr.Rank == 1 && arr.GetLowerBound(0) == 0)
                    return true;
            }
            return false;

        }
        internal static object? GetCoreType(string coreObj)
        {
            Assembly asm = typeof(Runtime).Assembly;
            Type? type = asm.GetType(coreObj);
            return type;

        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct IntPtrAndHandle
        {
            [FieldOffset(0)]
            internal IntPtr ptr;

            [FieldOffset(0)]
            internal RuntimeMethodHandle handle;
        }

        internal static string GetCallSignature(IntPtr methodHandle)
        {
            IntPtrAndHandle tmp = default(IntPtrAndHandle);
            tmp.ptr = methodHandle;

            var mb = MethodBase.GetMethodFromHandle(tmp.handle);
            if (mb == null)
                return string.Empty;

            ParameterInfo[] parms = mb.GetParameters();
            var parmsLength = parms.Length;
            if (parmsLength == 0)
                return string.Empty;

            var res = new char[parmsLength];

            for (int c = 0; c < parmsLength; c++)
            {
                var t = parms[c].ParameterType;
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
        internal static void SetupJSContinuation(Task task, JSObject continuationObj)
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
                        var resultProperty = task.GetType().GetProperty("Result");

                        if (resultProperty == null)
                            continuationObj.Invoke("resolve", Array.Empty<object>());
                        else
                            continuationObj.Invoke("resolve", resultProperty.GetValue(task) ?? Array.Empty<object>());
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

        internal static string ObjectToString(object o)
        {

            //if (o == null)
            //    return null;
            //if (o is Enum)
            //    return EnumToExportContract((Enum)o).ToString();

            return o.ToString() ?? string.Empty;
        }

        internal static double GetDateValue(object dtv)
        {
            if (dtv == null)
                throw new ArgumentNullException(nameof(dtv), "Value can not be null");
            if (!(dtv is DateTime dt))
                throw new InvalidCastException($"Unable to cast object of type {dtv.GetType()} to type DateTime.");
            {
                throw new InvalidCastException($"Unable to cast object of type {dtv.GetType()} to type DateTime.");
            }
            var dt = (DateTime)dtv;
            if (dt.Kind == DateTimeKind.Local)
                dt = dt.ToUniversalTime();
            else if (dt.Kind == DateTimeKind.Unspecified)
                dt = new DateTime(dt.Ticks, DateTimeKind.Utc);
            return new DateTimeOffset(dt).ToUnixTimeMilliseconds();
        }

        internal static DateTime CreateDateTime(double ticks)
        {
            var unixTime = DateTimeOffset.FromUnixTimeMilliseconds((long)ticks);
            return unixTime.DateTime;
        }
        internal static Uri CreateUri(string uri)
        {
            return new Uri(uri);
        }

        //
        // Can be called by the app to stop profiling
        //
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void StopProfile()
        {
        }

        // Called by the AOT profiler to save profile data into Module.aot_profile_data
        internal static unsafe void DumpAotProfileData(ref byte buf, int len, string s)
        {
            var arr = new byte[len];
            fixed (void* p = &buf)
            {
                var span = new ReadOnlySpan<byte>(p, len);

                // Send it to JS
                var jsDump = (JSObject)Runtime.GetGlobalObject("Module");
                //jsDump.SetObjectProperty("aot_profile_data", WebAssembly.Core.Uint8Array.From(span));
            }
        }

        // Called by the coverage profiler to save profile data into Module.coverage_profile_data
        internal static void DumpCoverageProfileData(string data, string s)
        {
            // Send it to JS
            var jsDump = (JSObject)Runtime.GetGlobalObject("Module");
            jsDump.SetObjectProperty("coverage_profile_data", data);
        }
    }
}
