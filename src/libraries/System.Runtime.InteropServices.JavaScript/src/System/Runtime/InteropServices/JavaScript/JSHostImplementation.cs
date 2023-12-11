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
        private static Dictionary<nint, WeakReference<JSObject>>? s_csOwnedObjects;

        public static Dictionary<nint, WeakReference<JSObject>> ThreadCsOwnedObjects
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
        private static Dictionary<object, nint>? s_jsOwnedObjects;

        public static Dictionary<object, nint> ThreadJsOwnedObjects
        {
            get
            {
                s_jsOwnedObjects ??= new Dictionary<object, nint>(ReferenceEqualityComparer.Instance);
                return s_jsOwnedObjects;
            }
        }

        // this is similar to GCHandle, but the GCVHandle is allocated on JS side and this keeps the C# proxy alive
#if FEATURE_WASM_THREADS
        [ThreadStatic]
#endif
        private static Dictionary<nint, PromiseHolder>? s_jsOwnedHolders;

        public static Dictionary<nint, PromiseHolder> ThreadJsOwnedHolders
        {
            get
            {
                s_jsOwnedHolders ??= new Dictionary<nint, PromiseHolder>();
                return s_jsOwnedHolders;
            }
        }

        // JSVHandle is like JSHandle, but it's not tracked and allocated by the JS side
        // It's used when we need to create JSHandle-like identity ahead of time, before calling JS.
        // they have negative values, so that they don't collide with JSHandles.
#if FEATURE_WASM_THREADS
        [ThreadStatic]
#endif
        public static nint NextJSVHandle;

#if FEATURE_WASM_THREADS
        [ThreadStatic]
#endif
        private static List<nint>? s_JSVHandleFreeList;
        public static List<nint> JSVHandleFreeList
        {
            get
            {
                s_JSVHandleFreeList ??= new();
                return s_JSVHandleFreeList;
            }
        }

        public static nint AllocJSVHandle()
        {
#if FEATURE_WASM_THREADS
            // TODO, when Task is passed to JSImport as parameter, it could be sent from another thread (in the future)
            // and so we need to use JSVHandleFreeList of the target thread
            JSSynchronizationContext.AssertWebWorkerContext();
#endif

            if (JSVHandleFreeList.Count > 0)
            {
                var jsvHandle = JSVHandleFreeList[JSVHandleFreeList.Count];
                JSVHandleFreeList.RemoveAt(JSVHandleFreeList.Count - 1);
                return jsvHandle;
            }
            if (NextJSVHandle == IntPtr.Zero)
            {
                NextJSVHandle = -2;
            }
            return NextJSVHandle--;
        }

        public static void FreeJSVHandle(nint jsvHandle)
        {
            JSVHandleFreeList.Add(jsvHandle);
        }

        public static bool IsGCVHandle(nint gcHandle)
        {
            return gcHandle < -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseCSOwnedObject(nint jsHandle)
        {
            if (jsHandle != IntPtr.Zero)
            {
#if FEATURE_WASM_THREADS
                JSSynchronizationContext.AssertWebWorkerContext();
#endif
                ThreadCsOwnedObjects.Remove(jsHandle);
                Interop.Runtime.ReleaseCSOwnedObject(jsHandle);
            }
        }

        public static bool GetTaskResultDynamic(Task task, out object? value)
        {
            var type = task.GetType();
            if (type == typeof(Task))
            {
                value = null;
                return false;
            }
            MethodInfo method = GetTaskResultMethodInfo(type);
            if (method != null)
            {
                value = method.Invoke(task, null);
                return true;
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
            using (var receiveRegistration = cancellationToken.Register(static s =>
            {
                CancelablePromise.CancelPromise((Task<JSObject>)s!);
            }, jsTask))
            {
                return await jsTask.ConfigureAwait(true);
            }
        }

        // res type is first argument
        public static unsafe JSFunctionBinding GetMethodSignature(ReadOnlySpan<JSMarshalerType> types, string? functionName, string? moduleName)
        {
            int argsCount = types.Length - 1;
            int size = JSFunctionBinding.JSBindingHeader.JSMarshalerSignatureHeaderSize + ((argsCount + 2) * sizeof(JSFunctionBinding.JSBindingType));

            int functionNameBytes = 0;
            int functionNameOffset = 0;
            if (functionName != null)
            {
                functionNameOffset = size;
                size += 4;
                functionNameBytes = functionName.Length * 2;
                size += functionNameBytes;
            }
            int moduleNameBytes = 0;
            int moduleNameOffset = 0;
            if (moduleName != null)
            {
                moduleNameOffset = size;
                size += 4;
                moduleNameBytes = moduleName.Length * 2;
                size += moduleNameBytes;
            }

            // this is never unallocated
            IntPtr buffer = Marshal.AllocHGlobal(size);

            var signature = new JSFunctionBinding
            {
                Header = (JSFunctionBinding.JSBindingHeader*)buffer,
                Sigs = (JSFunctionBinding.JSBindingType*)(buffer + JSFunctionBinding.JSBindingHeader.JSMarshalerSignatureHeaderSize + (2 * sizeof(JSFunctionBinding.JSBindingType))),
            };

            signature.Version = 2;
            signature.ArgumentCount = argsCount;
            signature.Exception = JSMarshalerType.Exception._signatureType;
            signature.Result = types[0]._signatureType;
#if FEATURE_WASM_THREADS
            signature.ImportHandle = (int)Interlocked.Increment(ref JSFunctionBinding.nextImportHandle);
            signature.IsThreadCaptured = false;
#else
            signature.ImportHandle = (int)JSFunctionBinding.nextImportHandle++;
#endif

            for (int i = 0; i < argsCount; i++)
            {
                var type = signature.Sigs[i] = types[i + 1]._signatureType;
#if FEATURE_WASM_THREADS
                if (i > 0 && (type.Type == MarshalerType.JSObject || type.Type == MarshalerType.JSException))
                {
                    signature.IsThreadCaptured = true;
                }
#endif
            }
            signature.IsAsync = types[0]._signatureType.Type == MarshalerType.Task;

            signature.Header[0].ImportHandle = signature.ImportHandle;
            signature.Header[0].FunctionNameLength = functionNameBytes;
            signature.Header[0].FunctionNameOffset = functionNameOffset;
            signature.Header[0].ModuleNameLength = moduleNameBytes;
            signature.Header[0].ModuleNameOffset = moduleNameOffset;
            if (functionNameBytes != 0)
            {
                fixed (void* fn = functionName)
                {
                    Unsafe.CopyBlock((byte*)buffer + functionNameOffset, fn, (uint)functionNameBytes);
                }
            }
            if (moduleNameBytes != 0)
            {
                fixed (void* mn = moduleName)
                {
                    Unsafe.CopyBlock((byte*)buffer + moduleNameOffset, mn, (uint)moduleNameBytes);
                }

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

            if (!ThreadCsOwnedObjects.TryGetValue(jsHandle, out WeakReference<JSObject>? reference) ||
                !reference.TryGetTarget(out res) ||
                res.IsDisposed)
            {
                res = new JSObject(jsHandle);
                ThreadCsOwnedObjects[jsHandle] = new WeakReference<JSObject>(res, trackResurrection: true);
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
        public static void InstallWebWorkerInterop(bool isMainThread)
        {
            Interop.Runtime.InstallWebWorkerInterop();
            var currentTID = GetNativeThreadId();
            var ctx = JSSynchronizationContext.CurrentJSSynchronizationContext;
            if (ctx == null)
            {
                ctx = new JSSynchronizationContext(Thread.CurrentThread, currentTID);
                ctx.previousSynchronizationContext = SynchronizationContext.Current;
                JSSynchronizationContext.CurrentJSSynchronizationContext = ctx;
                SynchronizationContext.SetSynchronizationContext(ctx);
                if (isMainThread)
                {
                    JSSynchronizationContext.MainJSSynchronizationContext = ctx;
                }
            }
            else if (ctx.TargetTID != currentTID)
            {
                Environment.FailFast($"JSSynchronizationContext.Install has wrong native thread id {ctx.TargetTID} != {currentTID}");
            }
            ctx.AwaitNewData();
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
                catch (Exception ex)
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

            Interop.Runtime.UninstallWebWorkerInterop();

            if (uninstallJSSynchronizationContext)
            {
                try
                {
                    foreach (var gch in ThreadJsOwnedObjects.Values)
                    {
                        GCHandle gcHandle = (GCHandle)gch;
                        gcHandle.Free();
                    }
                    foreach (var holder in ThreadJsOwnedHolders.Values)
                    {
                        unsafe
                        {
                            holder.Callback!.Invoke(null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Environment.FailFast($"Unexpected error in UninstallWebWorkerInterop, ManagedThreadId: {Thread.CurrentThread.ManagedThreadId}. " + ex);
                }
            }

            ThreadCsOwnedObjects.Clear();
            ThreadJsOwnedObjects.Clear();
            JSVHandleFreeList.Clear();
            NextJSVHandle = IntPtr.Zero;
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "external_eventloop")]
        private static extern ref bool GetThreadExternalEventloop(Thread @this);

        public static void SetHasExternalEventLoop(Thread thread)
        {
            GetThreadExternalEventloop(thread) = true;
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "thread_id")]
        private static extern ref long GetThreadNativeThreadId(Thread @this);

        public static IntPtr GetNativeThreadId()
        {
            return (int)GetThreadNativeThreadId(Thread.CurrentThread);
        }

#endif

    }
}
