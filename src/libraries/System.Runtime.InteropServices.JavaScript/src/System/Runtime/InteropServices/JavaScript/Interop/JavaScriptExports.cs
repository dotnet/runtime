// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    // this maps to src\mono\wasm\runtime\corebindings.ts
    // the methods are protected from trimming by DynamicDependency on JSFunctionBinding.BindJSFunction
    internal static unsafe partial class JavaScriptExports
    {

        // The JS layer invokes this method when the JS wrapper for a JS owned object
        //  has been collected by the JS garbage collector
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void ReleaseJSOwnedObjectByGCHandle(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0];
            ref JSMarshalerArgument arg_return = ref arguments_buffer[1];
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2];
            try
            {
                GCHandle handle = (GCHandle)arg_1.slot.GCHandle;

                lock (JSHostImplementation.s_gcHandleFromJSOwnedObject)
                {
                    JSHostImplementation.s_gcHandleFromJSOwnedObject.Remove(handle.Target!);
                    handle.Free();
                }
                arg_exc.Initialize();
            }
            catch (Exception ex)
            {
                arg_exc.ToJS(ex);
            }
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void CreateTaskCallback(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0];
            ref JSMarshalerArgument arg_return = ref arguments_buffer[1];
            try
            {
                JSHostImplementation.TaskCallback holder = new JSHostImplementation.TaskCallback();
                arg_return.slot.Type = MarshalerType.Object;
                arg_return.slot.GCHandle = JSHostImplementation.GetJSOwnedObjectGCHandle(holder);
            }
            catch (Exception ex)
            {
                arg_exc.ToJS(ex);
            }
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void CallDelegate(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0];
            ref JSMarshalerArgument arg_return = ref arguments_buffer[1];
            try
            {
                GCHandle callback_gc_handle = (GCHandle)arg_return.slot.GCHandle;

                JSHostImplementation.ToManagedCallback? cb = (JSHostImplementation.ToManagedCallback?)callback_gc_handle.Target;
                if (cb == null)
                {
                    throw new InvalidOperationException("ToManagedCallback is null");
                }

                cb(arguments_buffer);
            }
            catch (Exception ex)
            {
                arg_exc.ToJS(ex);
            }
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void CompleteTask(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0];
            ref JSMarshalerArgument arg_return = ref arguments_buffer[1];
            try
            {
                GCHandle callback_gc_handle = (GCHandle)arg_return.slot.GCHandle;

                JSHostImplementation.TaskCallback? holder = (JSHostImplementation.TaskCallback?)callback_gc_handle.Target;
                if (holder == null || holder.Callback == null)
                {
                    throw new InvalidOperationException("TaskCallback is null");
                }

                holder.Callback(arguments_buffer);
            }
            catch (Exception ex)
            {
                arg_exc.ToJS(ex);
            }
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static void StopProfile()
        {
        }

        // Called by the AOT profiler to save profile data into INTERNAL.aot_profile_data
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
        public static unsafe void DumpAotProfileData(ref byte buf, int len, string extraArg)
        {
            if (len == 0)
                throw new JSException("Profile data length is 0");

            var arr = new byte[len];
            fixed (void* p = &buf)
            {
                var span = new ReadOnlySpan<byte>(p, len);
                // Send it to JS
                var module = JSHost.DotnetInstance.GetPropertyAsJSObject("INTERNAL");
                if (module == null)
                    throw new InvalidOperationException();

                module.SetProperty("aot_profile_data", span.ToArray());
            }
        }
    }
}
