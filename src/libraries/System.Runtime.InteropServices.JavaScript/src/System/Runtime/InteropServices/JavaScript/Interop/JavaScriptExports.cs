// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSHostImplementation;

namespace System.Runtime.InteropServices.JavaScript
{
    // this maps to src\mono\browser\runtime\managed-exports.ts
    // the public methods are protected from trimming by DynamicDependency on JSFunctionBinding.BindJSFunction
    // TODO: all the calls here should be running on deputy or TP in MT, not in UI thread
    internal static unsafe partial class JavaScriptExports
    {
        // the marshaled signature is:
        // Task<int>? CallEntrypoint(MonoMethod* entrypointPtr, string[] args)
        public static void CallEntrypoint(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_result = ref arguments_buffer[1]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2]; // initialized and set by caller
            ref JSMarshalerArgument arg_2 = ref arguments_buffer[3]; // initialized and set by caller
            try
            {
#if FEATURE_WASM_MANAGED_THREADS
                // when we arrive here, we are on the thread which owns the proxies
                arg_exc.AssertCurrentThreadContext();
#endif

                arg_1.ToManaged(out IntPtr entrypointPtr);
                if (entrypointPtr == IntPtr.Zero)
                {
                    throw new MissingMethodException(SR.MissingManagedEntrypointHandle);
                }

                RuntimeMethodHandle methodHandle = GetMethodHandleFromIntPtr(entrypointPtr);
                // this would not work for generic types. But Main() could not be generic, so we are fine.
                MethodInfo? method = MethodBase.GetMethodFromHandle(methodHandle) as MethodInfo;
                if (method == null)
                {
                    throw new InvalidOperationException(SR.CannotResolveManagedEntrypointHandle);
                }

                arg_2.ToManaged(out string?[]? args);
                object[] argsToPass = System.Array.Empty<object>();
                Task<int>? result = null;
                var parameterInfos = method.GetParameters();
                if (parameterInfos.Length > 0 && parameterInfos[0].ParameterType == typeof(string[]))
                {
                    argsToPass = new object[] { args ?? System.Array.Empty<string>() };
                }
                if (method.ReturnType == typeof(void))
                {
                    method.Invoke(null, argsToPass);
                }
                else if (method.ReturnType == typeof(int))
                {
                    int intResult = (int)method.Invoke(null, argsToPass)!;
                    result = Task.FromResult(intResult);
                }
                else if (method.ReturnType == typeof(Task))
                {
                    Task methodResult = (Task)method.Invoke(null, argsToPass)!;
                    TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                    result = tcs.Task;
                    methodResult.ContinueWith((t) =>
                    {
                        if (t.IsFaulted)
                        {
                            tcs.SetException(t.Exception!);
                        }
                        else
                        {
                            tcs.SetResult(0);
                        }
                    }, TaskScheduler.Default);
                }
                else if (method.ReturnType == typeof(Task<int>))
                {
                    result = (Task<int>)method.Invoke(null, argsToPass)!;
                }
                else
                {
                    throw new InvalidOperationException(SR.Format(SR.ReturnTypeNotSupportedForMain, method.ReturnType.FullName));
                }
                arg_result.ToJS(result, (ref JSMarshalerArgument arg, int value) =>
                {
                    arg.ToJS(value);
                });
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException refEx && refEx.InnerException != null)
                    ex = refEx.InnerException;

                arg_exc.ToJS(ex);
            }
        }

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

        // The JS layer invokes this method when the JS wrapper for a JS owned object
        //  has been collected by the JS garbage collector
        // the marshaled signature is:
        // void ReleaseJSOwnedObjectByGCHandle(GCHandle gcHandle)
        public static void ReleaseJSOwnedObjectByGCHandle(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2]; // initialized and set by caller

            try
            {
                // when we arrive here, we are on the thread which owns the proxies
                var ctx = arg_exc.AssertCurrentThreadContext();
                ctx.ReleaseJSOwnedObjectByGCHandle(arg_1.slot.GCHandle);
            }
            catch (Exception ex)
            {
                arg_exc.ToJS(ex);
            }
        }

        // the marshaled signature is:
        // TRes? CallDelegate<T1,T2,T3TRes>(GCHandle callback, T1? arg1, T2? arg2, T3? arg3)
        public static void CallDelegate(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0]; // initialized by JS caller in alloc_stack_frame()
            // arg_res is initialized by JS caller
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2];// initialized and set by JS caller
            // arg_2 set by JS caller when there are arguments
            // arg_3 set by JS caller when there are arguments
            // arg_4 set by JS caller when there are arguments
            try
            {
#if FEATURE_WASM_MANAGED_THREADS
                // when we arrive here, we are on the thread which owns the proxies
                // if we need to dispatch the call to another thread in the future
                // we may need to consider how to solve blocking of the synchronous call
                // see also https://github.com/dotnet/runtime/issues/76958#issuecomment-1921418290
                arg_exc.AssertCurrentThreadContext();
#endif

                GCHandle callback_gc_handle = (GCHandle)arg_1.slot.GCHandle;
                if (callback_gc_handle.Target is ToManagedCallback callback)
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
        }

        // the marshaled signature is:
        // void CompleteTask<T>(GCHandle holder, Exception? exceptionResult, T? result)
        public static void CompleteTask(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_res = ref arguments_buffer[1]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2];// initialized and set by caller
            // arg_2 set by caller when this is SetException call
            // arg_3 set by caller when this is SetResult call

            try
            {
                // when we arrive here, we are on the thread which owns the proxies
                var ctx = arg_exc.AssertCurrentThreadContext();
                var holder = ctx.GetPromiseHolder(arg_1.slot.GCHandle);
                ToManagedCallback callback;

#if FEATURE_WASM_MANAGED_THREADS
                lock (ctx)
                {
                    // this means that CompleteTask is called before the ToManaged(out Task? value)
                    if (holder.Callback == null)
                    {
                        holder.CallbackReady = new ManualResetEventSlim(false);
                    }
                }

                if (holder.CallbackReady != null)
                {
                    var threadFlag = Monitor.ThrowOnBlockingWaitOnJSInteropThread;
                    try
                    {
                        Monitor.ThrowOnBlockingWaitOnJSInteropThread = false;
#pragma warning disable CA1416 // Validate platform compatibility
                        holder.CallbackReady?.Wait();
#pragma warning restore CA1416 // Validate platform compatibility
                    }
                    finally
                    {
                        Monitor.ThrowOnBlockingWaitOnJSInteropThread = threadFlag;
                    }
                }

                lock (ctx)
                {
                    callback = holder.Callback!;
                    // if Interop.Runtime.CancelPromisePost is in flight, we can't free the GCHandle, because it's needed in JS
                    var isOutOfOrderCancellation = holder.IsCanceling && arg_res.slot.Type != MarshalerType.Discard;
                    // FIXME: when it happens we are leaking GCHandle + holder
                    if (!isOutOfOrderCancellation)
                    {
                        ctx.ReleasePromiseHolder(arg_1.slot.GCHandle);
                    }
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
                arg_exc.ToJS(ex);
            }
        }

        // the marshaled signature is:
        // string GetManagedStackTrace(GCHandle exception)
        public static void GetManagedStackTrace(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0]; // initialized by caller in alloc_stack_frame()
            ref JSMarshalerArgument arg_return = ref arguments_buffer[1]; // used as return value
            ref JSMarshalerArgument arg_1 = ref arguments_buffer[2];// initialized and set by caller
            try
            {
                // when we arrive here, we are on the thread which owns the proxies
                arg_exc.AssertCurrentThreadContext();

                GCHandle exception_gc_handle = (GCHandle)arg_1.slot.GCHandle;
                if (exception_gc_handle.Target is Exception exception)
                {
                    arg_return.ToJS(exception.StackTrace);
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
        // the marshaled signature is:
        // void InstallMainSynchronizationContext()
        public static void InstallMainSynchronizationContext()
        {
            JSSynchronizationContext.InstallWebWorkerInterop(true, CancellationToken.None);
        }

#endif

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
