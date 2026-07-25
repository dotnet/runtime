// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    // this maps to src\native\libs\System.Runtime.InteropServices.JavaScript.Native\interop\managed-exports.ts
    // the public methods are protected from trimming by DynamicDependency on JSFunctionBinding.BindJSFunction
    internal static unsafe partial class JavaScriptExports
    {
        [UnmanagedCallersOnly(EntryPoint = "SystemInteropJS_ReleaseJSOwnedObjectByGCHandle")]
        // The JS layer invokes this method when the JS wrapper for a JS owned object has been collected by the JS garbage collector
        // the marshaled signature is: void ReleaseJSOwnedObjectByGCHandle(GCHandle gcHandle)
        public static void ReleaseJSOwnedObjectByGCHandle(JSMarshalerArgument* argumentsBuffer)
        {
            ref JSMarshalerArgument argException = ref argumentsBuffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg1 = ref argumentsBuffer[2]; // initialized and set by caller

            try
            {
                // when we arrive here, we are on the thread which owns the proxies or on IO thread
                var ctx = argException.ToManagedContext;
                ctx.ReleaseJSOwnedObjectByGCHandle(arg1.slot.GCHandle);
            }
            catch (Exception ex)
            {
                Environment.FailFast($"ReleaseJSOwnedObjectByGCHandle: Unexpected synchronous failure (ManagedThreadId {Environment.CurrentManagedThreadId}): " + ex);
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "SystemInteropJS_CallDelegate")]
        // the marshaled signature is: TRes? CallDelegate<T1,T2,T3TRes>(GCHandle callback, T1? arg1, T2? arg2, T3? arg3)
        public static void CallDelegate(JSMarshalerArgument* argumentsBuffer)
        {
            ref JSMarshalerArgument argException = ref argumentsBuffer[0]; // initialized by JS caller in alloc_stack_frame()
            // argResult is initialized by JS caller
            ref JSMarshalerArgument arg1 = ref argumentsBuffer[2];// initialized and set by JS caller
            // arg_2 set by JS caller when there are arguments
            // arg_3 set by JS caller when there are arguments
            // arg_4 set by JS caller when there are arguments
            try
            {
                GCHandle callback_gc_handle = (GCHandle)arg1.slot.GCHandle;
                if (callback_gc_handle.Target is JSHostImplementation.ToManagedCallback callback)
                {
                    // arg_2, arg_3, arg_4, argResult are processed by the callback
                    callback(argumentsBuffer);
                }
                else
                {
                    throw new InvalidOperationException(SR.NullToManagedCallback);
                }
            }
            catch (Exception ex)
            {
                argException.ToJS(ex);
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "SystemInteropJS_CompleteTask")]
        // the marshaled signature is: void CompleteTask<T>(GCHandle holder, Exception? exceptionResult, T? result)
        public static void CompleteTask(JSMarshalerArgument* argumentsBuffer)
        {
            ref JSMarshalerArgument argException = ref argumentsBuffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument argResult = ref argumentsBuffer[1]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg1 = ref argumentsBuffer[2];// initialized and set by caller
            // arg_2 set by caller when this is SetException call
            // arg_3 set by caller when this is SetResult call

            try
            {
                // when we arrive here, we are on the thread which owns the proxies or on IO thread
                var ctx = argException.ToManagedContext;
                var holder = ctx.GetPromiseHolder(arg1.slot.GCHandle);
                JSHostImplementation.ToManagedCallback callback;

                callback = holder.Callback!;
                ctx.ReleasePromiseHolder(arg1.slot.GCHandle);

                // arg_2, arg_3 are processed by the callback
                // JSProxyContext.PopOperation() is called by the callback
                callback!(argumentsBuffer);
            }
            catch (Exception ex)
            {
                Environment.FailFast($"CompleteTask: Unexpected synchronous failure (ManagedThreadId {Environment.CurrentManagedThreadId}): " + ex);
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "SystemInteropJS_GetManagedStackTrace")]
        // the marshaled signature is: string GetManagedStackTrace(GCHandle exception)
        public static void GetManagedStackTrace(JSMarshalerArgument* argumentsBuffer)
        {
            ref JSMarshalerArgument argException = ref argumentsBuffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument argResult = ref argumentsBuffer[1]; // used as return value
            ref JSMarshalerArgument arg1 = ref argumentsBuffer[2];// initialized and set by caller
            try
            {
                // when we arrive here, we are on the thread which owns the proxies
                argException.AssertCurrentThreadContext();

                GCHandle exception_gc_handle = (GCHandle)arg1.slot.GCHandle;
                if (exception_gc_handle.Target is Exception exception)
                {
                    argResult.ToJS(exception.StackTrace);
                }
                else
                {
                    throw new InvalidOperationException(SR.UnableToResolveHandleAsException);
                }
            }
            catch (Exception ex)
            {
                argException.ToJS(ex);
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "SystemInteropJS_BindAssemblyExports")]
        // the marshaled signature is: Task BindAssemblyExports(string assemblyName)
        public static void BindAssemblyExports(JSMarshalerArgument* argumentsBuffer)
        {
            ref JSMarshalerArgument argException = ref argumentsBuffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument argResult = ref argumentsBuffer[1]; // used as return value
            ref JSMarshalerArgument arg1 = ref argumentsBuffer[2];// initialized and set by caller
            try
            {
                string? assemblyName;
                // when we arrive here, we are on the thread which owns the proxies
                argException.AssertCurrentThreadContext();
                arg1.ToManaged(out assemblyName);

                var result = JSHostImplementation.BindAssemblyExports(assemblyName);

                argResult.ToJS(result);
            }
            catch (Exception ex)
            {
                argException.ToJS(ex);
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "SystemInteropJS_CallJSExport")]
        public static void CallJSExport(int methodHandle, JSMarshalerArgument* argumentsBuffer)
        {
            ref JSMarshalerArgument argException = ref argumentsBuffer[0]; // initialized by caller in alloc_stack_frame()
            var ctx = argException.AssertCurrentThreadContext();
            if (!ctx.JSExportByHandle.TryGetValue(methodHandle, out var jsExport))
            {
                argException.ToJS(new InvalidOperationException("Unable to resolve JSExport by handle"));
                return;
            }
            jsExport(new IntPtr(argumentsBuffer));
        }
    }
}
