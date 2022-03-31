// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript.Private
{
    internal static unsafe partial class JavaScriptMarshalImpl
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static unsafe extern string _BindJSFunction(string function_name, void* signature, out IntPtr bound_function_js_handle, out int is_exception);
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void _InvokeBoundJSFunction(IntPtr bound_function_js_handle, void* data);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static unsafe extern string _BindCSFunction(string fully_qualified_name, int signature_hash, string export_as_name, void* signature, out int is_exception);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static unsafe extern string _RegisterCustomMarshaller(string factory_code, IntPtr type_handle, out IntPtr js_handle_out, out int is_exception);

        [JSImport("INTERNAL.mono_wasm_resolve_task")]
        internal static partial void _ResolveTask(IntPtr gc_task_handle, object data);

        [JSImport("INTERNAL.mono_wasm_reject_task")]
        internal static partial void _RejectTask(IntPtr gc_task_handle, Exception reason);


        internal static readonly Dictionary<IntPtr, TaskCompletionSource<object>> _jsHandleToTaskCompletionSource = new Dictionary<IntPtr, TaskCompletionSource<object>>();

        [JSExport("INTERNAL.mono_wasm_resolve_tcs")]
        private static void _ResolveTaskCompletionSource(IntPtr js_tcs_handle, object data)
        {
            TaskCompletionSource<object> tcs = null;
            lock (_jsHandleToTaskCompletionSource)
            {
                _jsHandleToTaskCompletionSource.Remove(js_tcs_handle, out tcs);
            }
            if (tcs!= null)
            {
                tcs.SetResult(data);
            }
        }

        [JSExport("INTERNAL.mono_wasm_reject_tcs")]
        private static void _RejectTaskCompletionSource(IntPtr js_tcs_handle, Exception reason)
        {
            TaskCompletionSource<object> tcs = null;
            lock (_jsHandleToTaskCompletionSource)
            {
                _jsHandleToTaskCompletionSource.Remove(js_tcs_handle, out tcs);
            }
            if (tcs != null)
            {
                tcs.SetException(reason);
            }
        }
    }
}
