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
    // this maps to src\mono\browser\runtime\managed-exports.ts
    // the public methods are protected from trimming by DynamicDependency on JSFunctionBinding.BindJSFunction
    // TODO: change all of these to [UnmanagedCallersOnly] and drop the reflection in mono_wasm_invoke_jsexport
    internal static unsafe partial class JavaScriptExports
    {
        // the marshaled signature is: Task<int>? CallEntrypoint(char* assemblyNamePtr, string[] args)
        public static void CallEntrypoint(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_res = ref arguments_buffer[1]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2]; // initialized and set by caller
            ref JSMarshalerArgument arg_2 = ref arguments_buffer[3]; // initialized and set by caller
            ref JSMarshalerArgument arg_3 = ref arguments_buffer[4]; // initialized and set by caller
            try
            {
#if FEATURE_WASM_MANAGED_THREADS
                // when we arrive here, we are on the thread which owns the proxies
                arg_exc.AssertCurrentThreadContext();
                Debug.Assert(arg_res.slot.Type == MarshalerType.TaskPreCreated);
#endif

                arg_1.ToManaged(out IntPtr assemblyNamePtr);
                arg_2.ToManaged(out string?[]? args);
                arg_3.ToManaged(out bool waitForDebugger);

                Task<int>? result = JSHostImplementation.CallEntrypoint(assemblyNamePtr, args, waitForDebugger);

                arg_res.ToJS(result, (ref JSMarshalerArgument arg, int value) =>
                {
                    arg.ToJS(value);
                });
            }
            catch (Exception ex)
            {
                Environment.FailFast($"CallEntrypoint: Unexpected synchronous failure (ManagedThreadId {Environment.CurrentManagedThreadId}): " + ex);
            }
        }

        // the marshaled signature is: void LoadLazyAssembly(byte[] dll, byte[] pdb)
        public static void LoadLazyAssembly(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0];
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2];
            ref JSMarshalerArgument arg_2 = ref arguments_buffer[3];
            try
            {
#if FEATURE_WASM_MANAGED_THREADS
                // when we arrive here, we are on the thread which owns the proxies
                arg_exc.AssertCurrentThreadContext();
#endif
                arg_1.ToManaged(out byte[]? dllBytes);
                arg_2.ToManaged(out byte[]? pdbBytes);

                if (dllBytes != null)
                    JSHostImplementation.LoadLazyAssembly(dllBytes, pdbBytes);
            }
            catch (Exception ex)
            {
                arg_exc.ToJS(ex);
            }
        }

        // the marshaled signature is: void LoadSatelliteAssembly(byte[] dll)
        public static void LoadSatelliteAssembly(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0];
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2];
            try
            {
#if FEATURE_WASM_MANAGED_THREADS
                // when we arrive here, we are on the thread which owns the proxies
                arg_exc.AssertCurrentThreadContext();
#endif
                arg_1.ToManaged(out byte[]? dllBytes);

                if (dllBytes != null)
                    JSHostImplementation.LoadSatelliteAssembly(dllBytes);
            }
            catch (Exception ex)
            {
                arg_exc.ToJS(ex);
            }
        }

        // The JS layer invokes this method when the JS wrapper for a JS owned object has been collected by the JS garbage collector
        // the marshaled signature is: void ReleaseJSOwnedObjectByGCHandle(GCHandle gcHandle)
        public static void ReleaseJSOwnedObjectByGCHandle(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2]; // initialized and set by caller

            try
            {
                // when we arrive here, we are on the thread which owns the proxies or on IO thread
                var ctx = arg_exc.ToManagedContext;
                ctx.ReleaseJSOwnedObjectByGCHandle(arg_1.slot.GCHandle);
            }
            catch (Exception ex)
            {
                Environment.FailFast($"ReleaseJSOwnedObjectByGCHandle: Unexpected synchronous failure (ManagedThreadId {Environment.CurrentManagedThreadId}): " + ex);
            }
        }

        // the marshaled signature is: TRes? CallDelegate<T1,T2,T3TRes>(GCHandle callback, T1? arg1, T2? arg2, T3? arg3)
        public static void CallDelegate(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0]; // initialized by JS caller in alloc_stack_frame()
            // arg_res is initialized by JS caller
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2];// initialized and set by JS caller
            // arg_2 set by JS caller when there are arguments
            // arg_3 set by JS caller when there are arguments
            // arg_4 set by JS caller when there are arguments
#if !FEATURE_WASM_MANAGED_THREADS
            try
            {
#else
            // when we arrive here, we are on the thread which owns the proxies
            var ctx = arg_exc.AssertCurrentThreadContext();

            try
            {
                if (ctx.IsMainThread)
                {
                    if (JSProxyContext.ThreadBlockingMode == JSHostImplementation.JSThreadBlockingMode.ThrowWhenBlockingWait)
                    {
                        Thread.ThrowOnBlockingWaitOnJSInteropThread = true;
                    }
                    else if (JSProxyContext.ThreadBlockingMode == JSHostImplementation.JSThreadBlockingMode.WarnWhenBlockingWait)
                    {
                        Thread.WarnOnBlockingWaitOnJSInteropThread = true;
                    }
                }
#endif

                GCHandle callback_gc_handle = (GCHandle)arg_1.slot.GCHandle;
                if (callback_gc_handle.Target is JSHostImplementation.ToManagedCallback callback)
                {
                    // arg_2, arg_3, arg_4, arg_res are processed by the callback
                    callback(arguments_buffer);
                }
                else
                {
                    throw new InvalidOperationException(SR.NullToManagedCallback);
                }
            }
            catch (Exception ex)
            {
                arg_exc.ToJS(ex);
            }
#if FEATURE_WASM_MANAGED_THREADS
            finally
            {
                if (ctx.IsMainThread)
                {
                    if (JSProxyContext.ThreadBlockingMode == JSHostImplementation.JSThreadBlockingMode.ThrowWhenBlockingWait)
                    {
                        Thread.ThrowOnBlockingWaitOnJSInteropThread = false;
                    }
                    else if (JSProxyContext.ThreadBlockingMode == JSHostImplementation.JSThreadBlockingMode.WarnWhenBlockingWait)
                    {
                        Thread.WarnOnBlockingWaitOnJSInteropThread = false;
                    }
                }
            }
#endif
        }
        // the marshaled signature is: void CompleteTask<T>(GCHandle holder, Exception? exceptionResult, T? result)
        public static void CompleteTask(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_res = ref arguments_buffer[1]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2];// initialized and set by caller
            // arg_2 set by caller when this is SetException call
            // arg_3 set by caller when this is SetResult call

            try
            {
                // when we arrive here, we are on the thread which owns the proxies or on IO thread
                var ctx = arg_exc.ToManagedContext;
                var holder = ctx.GetPromiseHolder(arg_1.slot.GCHandle);
                JSHostImplementation.ToManagedCallback callback;

#if FEATURE_WASM_MANAGED_THREADS
                lock (ctx)
                {
                    // this means that CompleteTask is called before the ToManaged(out Task? value)
                    if (holder.Callback == null)
                    {
                        holder.CallbackReady = new ManualResetEventSlim(false);
                    }
                }

                // it's also OK to block here, because we know we will only block shortly, as this is just race with the other thread.
                if (holder.CallbackReady != null)
                {
                    Thread.ForceBlockingWait(static (b) => ((ManualResetEventSlim)b!).Wait(), holder.CallbackReady);
                }

                lock (ctx)
                {
                    callback = holder.Callback!;
                    ctx.ReleasePromiseHolder(arg_1.slot.GCHandle);
                }
#else
                callback = holder.Callback!;
                ctx.ReleasePromiseHolder(arg_1.slot.GCHandle);
#endif

                // arg_2, arg_3 are processed by the callback
                // JSProxyContext.PopOperation() is called by the callback
                callback!(arguments_buffer);
            }
            catch (Exception ex)
            {
                Environment.FailFast($"CompleteTask: Unexpected synchronous failure (ManagedThreadId {Environment.CurrentManagedThreadId}): " + ex);
            }
        }

        // the marshaled signature is: string GetManagedStackTrace(GCHandle exception)
        public static void GetManagedStackTrace(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_res = ref arguments_buffer[1]; // used as return value
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2];// initialized and set by caller
            try
            {
                // when we arrive here, we are on the thread which owns the proxies
                arg_exc.AssertCurrentThreadContext();

                GCHandle exception_gc_handle = (GCHandle)arg_1.slot.GCHandle;
                if (exception_gc_handle.Target is Exception exception)
                {
                    arg_res.ToJS(exception.StackTrace);
                }
                else
                {
                    throw new InvalidOperationException(SR.UnableToResolveHandleAsException);
                }
            }
            catch (Exception ex)
            {
                arg_exc.ToJS(ex);
            }
        }

#if FEATURE_WASM_MANAGED_THREADS

        // this is here temporarily, until JSWebWorker becomes public API
        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, "System.Runtime.InteropServices.JavaScript.JSWebWorker", "System.Runtime.InteropServices.JavaScript")]
        // the marshaled signature is: GCHandle InstallMainSynchronizationContext(nint jsNativeTID, JSThreadBlockingMode jsThreadBlockingMode)
        public static void InstallMainSynchronizationContext(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_res = ref arguments_buffer[1];// initialized and set by caller
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2];// initialized and set by caller
            ref JSMarshalerArgument arg_2 = ref arguments_buffer[3];// initialized and set by caller

            try
            {
                JSProxyContext.ThreadBlockingMode = (JSHostImplementation.JSThreadBlockingMode)arg_2.slot.Int32Value;
                var jsSynchronizationContext = JSSynchronizationContext.InstallWebWorkerInterop(true, CancellationToken.None);
                jsSynchronizationContext.ProxyContext.JSNativeTID = arg_1.slot.IntPtrValue;
                arg_res.slot.GCHandle = jsSynchronizationContext.ProxyContext.ContextHandle;
            }
            catch (Exception ex)
            {
                Environment.FailFast($"InstallMainSynchronizationContext: Unexpected failure (ManagedThreadId {Environment.CurrentManagedThreadId}): " + ex);
            }
        }

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
        // TODO ideally this would be public API callable from generated C# code for JSExport
        public static void BeforeSyncJSExport(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0];
            try
            {
                var ctx = arg_exc.AssertCurrentThreadContext();
                // note that this method is only executed when the caller is on another thread, via mono_wasm_invoke_jsexport_sync -> mono_wasm_install_js_worker_interop_wrapper
                ctx.IsPendingSynchronousCall = true;
                if (ctx.IsMainThread)
                {
                    if (JSProxyContext.ThreadBlockingMode == JSHostImplementation.JSThreadBlockingMode.ThrowWhenBlockingWait)
                    {
                        Thread.ThrowOnBlockingWaitOnJSInteropThread = true;
                    }
                    else if (JSProxyContext.ThreadBlockingMode == JSHostImplementation.JSThreadBlockingMode.WarnWhenBlockingWait)
                    {
                        Thread.WarnOnBlockingWaitOnJSInteropThread = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Environment.FailFast($"BeforeSyncJSExport: Unexpected synchronous failure (ManagedThreadId {Environment.CurrentManagedThreadId}): " + ex);
            }
        }

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
        // TODO ideally this would be public API callable from generated C# code for JSExport
        public static void AfterSyncJSExport(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0];
            try
            {
                var ctx = arg_exc.AssertCurrentThreadContext();
                ctx.IsPendingSynchronousCall = false;
                if (ctx.IsMainThread)
                {
                    if (JSProxyContext.ThreadBlockingMode == JSHostImplementation.JSThreadBlockingMode.ThrowWhenBlockingWait)
                    {
                        Thread.ThrowOnBlockingWaitOnJSInteropThread = false;
                    }
                    else if (JSProxyContext.ThreadBlockingMode == JSHostImplementation.JSThreadBlockingMode.WarnWhenBlockingWait)
                    {
                        Thread.WarnOnBlockingWaitOnJSInteropThread = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Environment.FailFast($"AfterSyncJSExport: Unexpected synchronous failure (ManagedThreadId {Environment.CurrentManagedThreadId}): " + ex);
            }
        }

#endif

        // the marshaled signature is: Task BindAssemblyExports(string assemblyName)
        public static void BindAssemblyExports(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_res = ref arguments_buffer[1]; // used as return value
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2];// initialized and set by caller
            try
            {
                string? assemblyName;
                // when we arrive here, we are on the thread which owns the proxies
                arg_exc.AssertCurrentThreadContext();
                arg_1.ToManaged(out assemblyName);

                var result = JSHostImplementation.BindAssemblyExports(assemblyName);

                arg_res.ToJS(result);
            }
            catch (Exception ex)
            {
                Environment.FailFast($"BindAssemblyExports: Unexpected synchronous failure (ManagedThreadId {Environment.CurrentManagedThreadId}): " + ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // profiler needs to find it executed under this name
        public static void StopProfile()
        {
        }

        // Called by the AOT profiler to save profile data into INTERNAL.aotProfileData
        [MethodImpl(MethodImplOptions.NoInlining)] // profiler needs to find it executed under this name
        public static unsafe void DumpAotProfileData(ref byte buf, int len, string extraArg)
        {
            if (len == 0)
                throw new InvalidOperationException(SR.EmptyProfileData);

            fixed (void* p = &buf)
            {
                var span = new ReadOnlySpan<byte>(p, len);
                // Send it to JS
                var module = JSHost.DotnetInstance.GetPropertyAsJSObject("INTERNAL");
                if (module == null)
                    throw new InvalidOperationException();

                module.SetProperty("aotProfileData", span.ToArray());
            }
        }
    }
}
