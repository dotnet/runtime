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
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string InvokeJS(string str, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object CompileFunction(string str, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object InvokeJSWithArgs(int js_obj_handle, string method, object?[] _params, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object GetObjectProperty(int js_obj_handle, string propertyName, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object SetObjectProperty(int js_obj_handle, string propertyName, object value, bool createIfNotExists, bool hasOwnProperty, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object GetByIndex(int js_obj_handle, int index, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object SetByIndex(int js_obj_handle, int index, object? value, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object GetGlobalObject(string? globalName, out int exceptional_result);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object ReleaseHandle(int js_obj_handle, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object ReleaseObject(int js_obj_handle, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object NewObjectJS(int js_obj_handle, object[] _params, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object BindCoreObject(int js_obj_handle, int gc_handle, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object BindHostObject(int js_obj_handle, int gc_handle, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object New(string className, object[] _params, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object TypedArrayToArray(int js_obj_handle, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object TypedArrayCopyTo(int js_obj_handle, int array_ptr, int begin, int end, int bytes_per_element, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object TypedArrayFrom(int array_ptr, int begin, int end, int bytes_per_element, int type, out int exceptional_result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object TypedArrayCopyFrom(int js_obj_handle, int array_ptr, int begin, int end, int bytes_per_element, out int exceptional_result);

        // / <summary>
        // / Execute the provided string in the JavaScript context
        // / </summary>
        // / <returns>The js.</returns>
        // / <param name="str">String.</param>
        public static string InvokeJS(string str)
        {
            var res = InvokeJS(str, out int exception);
            if (exception != 0)
                throw new JSException(res);
            return res;
        }

        public static Interop.JavaScript.Function? CompileFunction(string snippet)
        {
            var res = CompileFunction(snippet, out int exception);
            if (exception != 0)
                throw new JSException((string)res);
            return res as Interop.JavaScript.Function;
        }

        private static Dictionary<int, JSObject?> bound_objects = new Dictionary<int, JSObject?>();
        private static Dictionary<object, JSObject?> raw_to_js = new Dictionary<object, JSObject?>();

        public static int New<T>(params object[] _params)
        {
            var res = New(typeof(T).Name, _params, out int exception);
            if (exception != 0)
                throw new JSException((string)res);
            return (int)res;
        }

        public static int New(string hostClassName, params object[] _params)
        {
            var res = New(hostClassName, _params, out int exception);
            if (exception != 0)
                throw new JSException((string)res);
            return (int)res;
        }

        public static JSObject? NewJSObject(JSObject? js_func_ptr = null, params object[] _params)
        {
            var res = NewObjectJS(js_func_ptr?.JSHandle ?? 0, _params, out int exception);
            if (exception != 0)
                throw new JSException((string)res);
            return res as JSObject;
        }

        private static int BindJSObject(int js_id, Type mappedType)
        {
            if (!bound_objects.TryGetValue(js_id, out JSObject? obj))
            {
                if (mappedType != null)
                {
                    return BindJSType(js_id, mappedType);
                }
                else
                {
                    bound_objects[js_id] = obj = new JSObject((IntPtr)js_id);
                }
            }
            return obj == null ? 0 : (int)(IntPtr)obj.Handle;
        }

        private static int BindCoreCLRObject(int js_id, int gcHandle)
        {
            //Console.WriteLine ($"Registering CLR Object {js_id} with handle {gcHandle}");
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            JSObject? obj = h.Target as JSObject;

            if (bound_objects.TryGetValue(js_id, out var existingObj))
            {
                if (existingObj?.Handle != h && h.IsAllocated)
                    throw new JSException($"Multiple handles pointing at js_id: {js_id}");

                obj = existingObj;
            }
            else
                bound_objects[js_id] = obj;

            return obj == null ? 0 : (int)(IntPtr)obj.Handle;
        }

        internal static int BindJSType(int js_id, Type mappedType)
        {
            if (!bound_objects.TryGetValue(js_id, out JSObject? obj))
            {
                var jsobjectnew = mappedType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.ExactBinding,
                        null, new Type[] { typeof(IntPtr) }, null);
                bound_objects[js_id] = obj = jsobjectnew == null ? null : (JSObject)jsobjectnew.Invoke(new object[] { (IntPtr)js_id });
            }
            return obj == null ? 0 : (int)(IntPtr)obj.Handle;
        }

        internal static int UnBindJSObject(int js_id)
        {
            if (bound_objects.TryGetValue(js_id, out var obj))
            {
                bound_objects.Remove(js_id);
                return obj == null ? 0 : (int)(IntPtr)obj.Handle;
            }

            return 0;
        }

        internal static void UnBindJSObjectAndFree(int js_id)
        {
            if (bound_objects.TryGetValue(js_id, out var obj))
            {
                if (bound_objects[js_id] != null)
                {
                    //bound_objects[js_id].RawObject = null;
                    bound_objects.Remove(js_id);
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
            if (obj != null && obj.RawObject != null)
            {
                raw_to_js.Remove(obj.RawObject);

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

        public static void FreeObject(object obj)
        {
            if (raw_to_js.TryGetValue(obj, out JSObject? jsobj))
            {
                //raw_to_js [obj].RawObject = null;
                raw_to_js.Remove(obj);
                if (jsobj != null)
                {
                    int exception;
                    Runtime.ReleaseObject(jsobj.JSHandle, out exception);
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

        internal static object CreateTaskSource(int js_id)
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

        internal static int GetTaskAndBind(TaskCompletionSource<object> tcs, int js_id)
        {
            return BindExistingObject(tcs.Task, js_id);
        }

        internal static int BindExistingObject(object raw_obj, int js_id)
        {
            JSObject? obj = raw_obj as JSObject;

            if (obj == null && !raw_to_js.TryGetValue(raw_obj, out obj))
                raw_to_js[raw_obj] = obj = new JSObject(js_id, raw_obj);

            return obj == null ? 0 : (int)(IntPtr)obj.Handle;
        }

        internal static int GetJSObjectId(object raw_obj)
        {
            JSObject? obj = raw_obj as JSObject;

            if (obj == null && !raw_to_js.TryGetValue(raw_obj, out obj))
                return -1;

            return obj != null ? obj.JSHandle : -1;
        }

        internal static object? GetMonoObject(int gc_handle)
        {
            GCHandle h = (GCHandle)(IntPtr)gc_handle;
            JSObject? o = h.Target as JSObject;
            if (o != null && o.RawObject != null)
                return o.RawObject;
            return o;
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

        internal static string GetCallSignature(IntPtr method_handle)
        {
            IntPtrAndHandle tmp = default(IntPtrAndHandle);
            tmp.ptr = method_handle;

            var mb = MethodBase.GetMethodFromHandle(tmp.handle);

            string res = "";
            var parms = mb?.GetParameters();
            if (parms == null)
                return res;
            foreach (var p in parms)
            {
                var t = p.ParameterType;

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
                            res += "j";
                        else
                            res += "i";
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        // Enums types have the same code as their underlying numeric types
                        if (t.IsEnum)
                            res += "k";
                        else
                            res += "l";
                        break;
                    case TypeCode.Single:
                        res += "f";
                        break;
                    case TypeCode.Double:
                        res += "d";
                        break;
                    case TypeCode.String:
                        res += "s";
                        break;
                    default:
                        if (t == typeof(IntPtr))
                        {
                            res += "i";
                        }
                        else if (t == typeof(Uri))
                        {
                            res += "u";
                        }
                        else
                        {
                            if (t.IsValueType)
                                throw new Exception("Can't handle VT arguments");
                            res += "o";
                        }
                        break;
                }
            }

            return res;
        }
        internal static void SetupJSContinuation(Task task, JSObject cont_obj)
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
                            cont_obj.Invoke("resolve", Array.Empty<object>());
                        else
                            cont_obj.Invoke("resolve", resultProperty.GetValue(task) ?? Array.Empty<object>());
                    }
                    else
                    {
                        cont_obj.Invoke("reject", task.Exception.ToString());
                    }
                }
                catch (Exception e)
                {
                    cont_obj.Invoke("reject", e.ToString());
                }
                finally
                {
                    cont_obj.Dispose();
                    FreeObject(task);
                }
            }
        }

        internal static object GetGlobalObject(string? str = null)
        {
            int exception;
            var globalHandle = Runtime.GetGlobalObject(str, out exception);

            if (exception != 0)
                throw new JSException($"Error obtaining a handle to global {str}");

            return globalHandle;
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
            if (!(dtv is DateTime))
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
                var js_dump = (JSObject)Runtime.GetGlobalObject("Module");
                //js_dump.SetObjectProperty("aot_profile_data", WebAssembly.Core.Uint8Array.From(span));
            }
        }

        // Called by the coverage profiler to save profile data into Module.coverage_profile_data
        internal static void DumpCoverageProfileData(string data, string s)
        {
            // Send it to JS
            var js_dump = (JSObject)Runtime.GetGlobalObject("Module");
            js_dump.SetObjectProperty("coverage_profile_data", data);
        }
    }
}
