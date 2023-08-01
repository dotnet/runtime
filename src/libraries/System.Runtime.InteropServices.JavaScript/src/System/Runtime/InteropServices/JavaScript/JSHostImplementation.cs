// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
        private const string TaskGetResultName = "get_Result";
        private static MethodInfo? s_taskGetResultMethodInfo;
        // we use this to maintain identity of JSHandle for a JSObject proxy
#if FEATURE_WASM_THREADS
        [ThreadStatic]
#endif
        private static Dictionary<int, WeakReference<JSObject>>? s_csOwnedObjects;

        public static Dictionary<int, WeakReference<JSObject>> ThreadCsOwnedObjects
        {
            get
            {
                s_csOwnedObjects ??= new();
                return s_csOwnedObjects;
            }
        }

        // we use this to maintain identity of GCHandle for a managed object
#if FEATURE_WASM_THREADS
        [ThreadStatic]
#endif
        private static Dictionary<object, IntPtr>? s_jsOwnedObjects;

        public static Dictionary<object, IntPtr> ThreadJsOwnedObjects
        {
            get
            {
                s_jsOwnedObjects ??= new Dictionary<object, IntPtr>(ReferenceEqualityComparer.Instance);
                return s_jsOwnedObjects;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseCSOwnedObject(nint jsHandle)
        {
            if (jsHandle != IntPtr.Zero)
            {
#if FEATURE_WASM_THREADS
                JSSynchronizationContext.AssertWebWorkerContext();
#endif
                ThreadCsOwnedObjects.Remove((int)jsHandle);
                Interop.Runtime.ReleaseCSOwnedObject(jsHandle);
            }
        }

        public static object? GetTaskResultDynamic(Task task)
        {
            MethodInfo method = GetTaskResultMethodInfo(task.GetType());
            if (method != null)
            {
                return method.Invoke(task, null);
            }
            throw new InvalidOperationException();
        }

        // A JSOwnedObject is a managed object with its lifetime controlled by javascript.
        // The managed side maintains a strong reference to the object, while the JS side
        //  maintains a weak reference and notifies the managed side if the JS wrapper object
        //  has been reclaimed by the JS GC. At that point, the managed side will release its
        //  strong references, allowing the managed object to be collected.
        // This ensures that things like delegates and promises will never 'go away' while JS
        //  is expecting to be able to invoke or await them.
        public static IntPtr GetJSOwnedObjectGCHandle(object obj, GCHandleType handleType = GCHandleType.Normal)
        {
            if (obj == null)
            {
                return IntPtr.Zero;
            }

            IntPtr gcHandle;
            if (ThreadJsOwnedObjects.TryGetValue(obj, out gcHandle))
            {
                return gcHandle;
            }

            IntPtr result = (IntPtr)GCHandle.Alloc(obj, handleType);
            ThreadJsOwnedObjects[obj] = result;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RuntimeMethodHandle GetMethodHandleFromIntPtr(IntPtr ptr)
        {
            var temp = new IntPtrAndHandle { ptr = ptr };
            return temp.methodHandle;
        }

        /// <summary>
        /// Gets the MethodInfo for the Task{T}.Result property getter.
        /// </summary>
        /// <remarks>
        /// This ensures the returned MethodInfo is strictly for the Task{T} type, and not
        /// a "Result" property on some other class that derives from Task or a "new Result"
        /// property on a class that derives from Task{T}.
        ///
        /// The reason for this restriction is to make this use of Reflection trim-compatible,
        /// ensuring that trimming doesn't change the application's behavior.
        /// </remarks>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Task<T>.Result is preserved by the ILLinker because s_taskGetResultMethodInfo was initialized with it.")]
        public static MethodInfo GetTaskResultMethodInfo(Type taskType)
        {
            if (taskType != null)
            {
                if (s_taskGetResultMethodInfo == null)
                {
                    s_taskGetResultMethodInfo = typeof(Task<>).GetMethod(TaskGetResultName);
                }
                MethodInfo? getter = taskType.GetMethod(TaskGetResultName);
                if (getter != null && getter.HasSameMetadataDefinitionAs(s_taskGetResultMethodInfo!))
                {
                    return getter;
                }
            }

            throw new InvalidOperationException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowException(ref JSMarshalerArgument arg)
        {
            arg.ToManaged(out Exception? ex);

            if (ex != null)
            {
                throw ex;
            }
            throw new InvalidOperationException();
        }

        public static async Task<JSObject> ImportAsync(string moduleName, string moduleUrl, CancellationToken cancellationToken)
        {
            Task<JSObject> modulePromise = JavaScriptImports.DynamicImport(moduleName, moduleUrl);
            var wrappedTask = CancelationHelper(modulePromise, cancellationToken);
            return await wrappedTask.ConfigureAwait(
                ConfigureAwaitOptions.ContinueOnCapturedContext |
                ConfigureAwaitOptions.ForceYielding); // this helps to finish the import before we bind the module in [JSImport]
        }

        public static async Task<JSObject> CancelationHelper(Task<JSObject> jsTask, CancellationToken cancellationToken)
        {
            if (jsTask.IsCompletedSuccessfully)
            {
                return jsTask.Result;
            }
            using (var receiveRegistration = cancellationToken.Register(() =>
            {
                CancelablePromise.CancelPromise(jsTask);
            }))
            {
                return await jsTask.ConfigureAwait(true);
            }
        }

        // res type is first argument
        public static unsafe JSFunctionBinding GetMethodSignature(ReadOnlySpan<JSMarshalerType> types)
        {
            int argsCount = types.Length - 1;
            int size = JSFunctionBinding.JSBindingHeader.JSMarshalerSignatureHeaderSize + ((argsCount + 2) * sizeof(JSFunctionBinding.JSBindingType));
            // this is never unallocated
            IntPtr buffer = Marshal.AllocHGlobal(size);

            var signature = new JSFunctionBinding
            {
                Header = (JSFunctionBinding.JSBindingHeader*)buffer,
                Sigs = (JSFunctionBinding.JSBindingType*)(buffer + JSFunctionBinding.JSBindingHeader.JSMarshalerSignatureHeaderSize + (2 * sizeof(JSFunctionBinding.JSBindingType))),
            };

            signature.Version = 1;
            signature.ArgumentCount = argsCount;
            signature.Exception = JSMarshalerType.Exception._signatureType;
            signature.Result = types[0]._signatureType;
            for (int i = 0; i < argsCount; i++)
            {
                signature.Sigs[i] = types[i + 1]._signatureType;
            }

            return signature;
        }

        public static unsafe void FreeMethodSignatureBuffer(JSFunctionBinding signature)
        {
            Marshal.FreeHGlobal((nint)signature.Header);
            signature.Header = null;
            signature.Sigs = null;
        }

        public static JSObject CreateCSOwnedProxy(nint jsHandle)
        {
#if FEATURE_WASM_THREADS
            JSSynchronizationContext.AssertWebWorkerContext();
#endif
            JSObject? res;

            if (!ThreadCsOwnedObjects.TryGetValue((int)jsHandle, out WeakReference<JSObject>? reference) ||
                !reference.TryGetTarget(out res) ||
                res.IsDisposed)
            {
                res = new JSObject(jsHandle);
                ThreadCsOwnedObjects[(int)jsHandle] = new WeakReference<JSObject>(res, trackResurrection: true);
            }
            return res;
        }

        [Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "It's always part of the single compilation (and trimming) unit.")]
        public static void LoadLazyAssembly(byte[] dllBytes, byte[]? pdbBytes)
        {
            if (pdbBytes == null)
                AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(dllBytes));
            else
                AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(dllBytes), new MemoryStream(pdbBytes));
        }

        [Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "It's always part of the single compilation (and trimming) unit.")]
        public static void LoadSatelliteAssembly(byte[] dllBytes)
        {
            AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(dllBytes));
        }

#if FEATURE_WASM_THREADS
        public static void InstallWebWorkerInterop(bool installJSSynchronizationContext, bool isMainThread)
        {
            Interop.Runtime.InstallWebWorkerInterop(installJSSynchronizationContext);
            if (installJSSynchronizationContext)
            {
                var currentThreadId = GetNativeThreadId();
                var ctx = JSSynchronizationContext.CurrentJSSynchronizationContext;
                if (ctx == null)
                {
                    ctx = new JSSynchronizationContext(Thread.CurrentThread, currentThreadId);
                    ctx.previousSynchronizationContext = SynchronizationContext.Current;
                    JSSynchronizationContext.CurrentJSSynchronizationContext = ctx;
                    SynchronizationContext.SetSynchronizationContext(ctx);
                    if (isMainThread)
                    {
                        JSSynchronizationContext.MainJSSynchronizationContext = ctx;
                    }
                }
                else if (ctx.TargetThreadId != currentThreadId)
                {
                    Environment.FailFast($"JSSynchronizationContext.Install failed has wrong native thread id {ctx.TargetThreadId} != {currentThreadId}");
                }
                ctx.AwaitNewData();
            }
        }

        public static void UninstallWebWorkerInterop()
        {
            var ctx = JSSynchronizationContext.CurrentJSSynchronizationContext;
            var uninstallJSSynchronizationContext = ctx != null;
            if (uninstallJSSynchronizationContext)
            {
                try
                {
                    foreach (var jsObjectWeak in ThreadCsOwnedObjects.Values)
                    {
                        if (jsObjectWeak.TryGetTarget(out var jso))
                        {
                            jso.Dispose();
                        }
                    }
                    SynchronizationContext.SetSynchronizationContext(ctx!.previousSynchronizationContext);
                    JSSynchronizationContext.CurrentJSSynchronizationContext = null;
                    ctx.isDisposed = true;
                }
                catch(Exception ex)
                {
                    Environment.FailFast($"Unexpected error in UninstallWebWorkerInterop, ManagedThreadId: {Thread.CurrentThread.ManagedThreadId}. " + ex);
                }
            }
            else
            {
                if (ThreadCsOwnedObjects.Count > 0)
                {
                    Environment.FailFast($"There should be no JSObjects proxies on this thread, ManagedThreadId: {Thread.CurrentThread.ManagedThreadId}");
                }
                if (ThreadJsOwnedObjects.Count > 0)
                {
                    Environment.FailFast($"There should be no JS proxies of managed objects on this thread, ManagedThreadId: {Thread.CurrentThread.ManagedThreadId}");
                }
            }

            Interop.Runtime.UninstallWebWorkerInterop(uninstallJSSynchronizationContext);

            if (uninstallJSSynchronizationContext)
            {
                try
                {
                    foreach (var gch in ThreadJsOwnedObjects.Values)
                    {
                        GCHandle gcHandle = (GCHandle)gch;

                        // if this is pending promise we reject it
                        if (gcHandle.Target is TaskCallback holder)
                        {
                            unsafe
                            {
                                holder.Callback!.Invoke(null);
                            }
                        }
                        gcHandle.Free();
                    }
                }
                catch(Exception ex)
                {
                    Environment.FailFast($"Unexpected error in UninstallWebWorkerInterop, ManagedThreadId: {Thread.CurrentThread.ManagedThreadId}. " + ex);
                }
            }

            ThreadCsOwnedObjects.Clear();
            ThreadJsOwnedObjects.Clear();
        }

        private static FieldInfo? thread_id_Field;
        private static FieldInfo? external_eventloop_Field;

        // FIXME: after https://github.com/dotnet/runtime/issues/86040 replace with
        // [UnsafeAccessor(UnsafeAccessorKind.Field, Name="external_eventloop")]
        // static extern ref bool ThreadExternalEventloop(Thread @this);
        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, "System.Threading.Thread", "System.Private.CoreLib")]
        public static void SetHasExternalEventLoop(Thread thread)
        {
            if (external_eventloop_Field == null)
            {
                external_eventloop_Field = typeof(Thread).GetField("external_eventloop", BindingFlags.NonPublic | BindingFlags.Instance)!;
            }
            external_eventloop_Field.SetValue(thread, true);
        }

        // FIXME: after https://github.com/dotnet/runtime/issues/86040
        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicFields, "System.Threading.Thread", "System.Private.CoreLib")]
        public static IntPtr GetNativeThreadId()
        {
            if (thread_id_Field == null)
            {
                thread_id_Field = typeof(Thread).GetField("thread_id", BindingFlags.NonPublic | BindingFlags.Instance)!;
            }
            return (int)(long)thread_id_Field.GetValue(Thread.CurrentThread)!;
        }

#endif

    }
}
