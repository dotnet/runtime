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
        // the marshaled signature is: void LoadLazyAssembly(byte[] dll, byte[] pdb)
        [UnmanagedCallersOnly(EntryPoint = "SystemInteropJS_LoadLazyAssembly")]
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
        [UnmanagedCallersOnly(EntryPoint = "SystemInteropJS_LoadSatelliteAssembly")]
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

#if FEATURE_WASM_MANAGED_THREADS
        // this is here temporarily, until JSWebWorker becomes public API
        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, "System.Runtime.InteropServices.JavaScript.JSWebWorker", "System.Runtime.InteropServices.JavaScript")]
        [UnmanagedCallersOnly(EntryPoint = "SystemInteropJS_InstallMainSynchronizationContext")]
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

        [UnmanagedCallersOnly(EntryPoint = "SystemInteropJS_BeforeSyncJSExport")]
        // TODO ideally this would be public API callable from generated C# code for JSExport
        public static void BeforeSyncJSExport(JSMarshalerArgument* arguments_buffer)
        {
            ref JSMarshalerArgument arg_exc = ref arguments_buffer[0];
            try
            {
                var ctx = arg_exc.AssertCurrentThreadContext();
                // note that this method is only executed when the caller is on another thread, via SystemInteropJS_InstallWebWorkerInterop
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

        [UnmanagedCallersOnly(EntryPoint = "SystemInteropJS_AfterSyncJSExport")]
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

#endif // FEATURE_WASM_MANAGED_THREADS

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
