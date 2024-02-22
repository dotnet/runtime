// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSHostImplementation;

namespace System.Runtime.InteropServices.JavaScript
{
    internal sealed class JSProxyContext : IDisposable
    {
        internal bool _isDisposed;

        // we use this to maintain identity of JSHandle for a JSObject proxy
        private readonly Dictionary<nint, WeakReference<JSObject>> ThreadCsOwnedObjects = new();
        // we use this to maintain identity of GCHandle for a managed object
        private readonly Dictionary<object, nint> ThreadJsOwnedObjects = new(ReferenceEqualityComparer.Instance);
        // this is similar to GCHandle, but the GCVHandle is allocated on JS side and this keeps the C# proxy alive
        private readonly Dictionary<nint, PromiseHolder> ThreadJsOwnedHolders = new();
        // JSVHandle is like JSHandle, but it's not tracked and allocated by the JS side
        // It's used when we need to create JSHandle-like identity ahead of time, before calling JS.
        // they have negative values, so that they don't collide with JSHandles.
        private nint NextJSVHandle = -2;
        private readonly List<nint> JSVHandleFreeList = new();

#if !FEATURE_WASM_MANAGED_THREADS
        private JSProxyContext()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1822 // Mark members as static
        public bool IsCurrentThread() => true;
#pragma warning restore CA1822 // Mark members as static
#else
        public nint ContextHandle;
        public nint JSNativeTID; // target thread where JavaScript is running
        public nint NativeTID; // current pthread id
        public int ManagedTID; // current managed thread id
        public bool IsMainThread;
        public JSSynchronizationContext SynchronizationContext;

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool IsCurrentThread()
        {
            return ManagedTID == Environment.CurrentManagedThreadId;
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "thread_id")]
        private static extern ref long GetThreadNativeThreadId(Thread @this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr GetNativeThreadId()
        {
            return (int)GetThreadNativeThreadId(Thread.CurrentThread);
        }

        public JSProxyContext(bool isMainThread, JSSynchronizationContext synchronizationContext)
        {
            SynchronizationContext = synchronizationContext;
            NativeTID = JSNativeTID = GetNativeThreadId();
            ManagedTID = Environment.CurrentManagedThreadId;
            IsMainThread = isMainThread;
            ContextHandle = (nint)GCHandle.Alloc(this, GCHandleType.Normal);
        }
#endif

        #region Current operation context

#if !FEATURE_WASM_MANAGED_THREADS
        public static readonly JSProxyContext MainThreadContext = new();
        public static JSProxyContext CurrentThreadContext => MainThreadContext;
        public static JSProxyContext CurrentOperationContext => MainThreadContext;
        public static JSProxyContext PushOperationWithCurrentThreadContext()
        {
            // in single threaded build we don't have to keep stack of operations and the context/thread is always the same
            return MainThreadContext;
        }
#else

        // Context of the main thread
        private static JSProxyContext? _MainThreadContext;
        public static JSProxyContext MainThreadContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _MainThreadContext!;
            set => _MainThreadContext = value;
        }

        public enum JSImportOperationState
        {
            None,
            JSImportParams,
        }

        [ThreadStatic]
        private static JSProxyContext? _CapturedOperationContext;
        [ThreadStatic]
        private static JSImportOperationState _CapturingState;

        public static JSImportOperationState CapturingState => _CapturingState;

        // there will be call to JS from JSImport generated code, but we don't know which target thread yet
        public static void JSImportWithUnknownContext()
        {
            // it would be ideal to assert here, that we arrived here with JSImportOperationState.None
            // but any exception during JSImportOperationState.JSImportParams phase could make this state un-balanced
            // typically this would be exception which is validating the marshaled value
            // manually re-setting _CapturingState on each throw site would be possible, but fragile
            // luckily, we always reset it here before any new JSImport call
            // so the code which could interact with _CapturedOperationContext value will receive fresh values
            _CapturingState = JSImportOperationState.JSImportParams;
            _CapturedOperationContext = null;
        }

        // there will be no capture during following call to JS
        public static void JSImportNoCapture()
        {
            _CapturingState = JSImportOperationState.None;
            _CapturedOperationContext = null;
        }

        // we are at the end of marshaling of the JSImport parameters
        public static JSProxyContext SealJSImportCapturing()
        {
            if (_CapturingState != JSImportOperationState.JSImportParams)
            {
                Environment.FailFast($"Method only allowed during JSImport capturing phase, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
            }
            _CapturingState = JSImportOperationState.None;
            var capturedOperationContext = _CapturedOperationContext;
            _CapturedOperationContext = null;

            if (capturedOperationContext != null)
            {
                return capturedOperationContext;
            }
            // it could happen that we are in operation, in which we didn't capture target thread/context
            var executionContext = ExecutionContext;
            if (executionContext != null)
            {
                // we could will call JS on the task's AsyncLocal context, if it has the JS interop installed
                return executionContext;
            }

            var currentThreadContext = CurrentThreadContext;
            if (currentThreadContext != null)
            {
                // we could will call JS on the current thread (or child task), if it has the JS interop installed
                return currentThreadContext;
            }

            // otherwise we will call JS on the main thread, which always has JS interop
            return MainThreadContext;
        }

        // this is called only during marshaling (in) parameters of JSImport, which have existing ProxyContext (thread affinity)
        // together with CurrentOperationContext is will validate that all parameters of the call have same context/affinity
        public static void CaptureContextFromParameter(JSProxyContext parameterContext)
        {
            if (_CapturingState != JSImportOperationState.JSImportParams)
            {
                Environment.FailFast($"Method only allowed during JSImport capturing phase, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
            }

            var alreadyCapturedContext = _CapturedOperationContext;

            if (alreadyCapturedContext == null)
            {
                _CapturedOperationContext = parameterContext;
            }
            else if (parameterContext != alreadyCapturedContext)
            {
                _CapturedOperationContext = null;
                _CapturingState = JSImportOperationState.None;
                throw new InvalidOperationException("All JSObject proxies need to have same thread affinity. See https://aka.ms/dotnet-JS-interop-threads");
            }
        }

        // Context flowing from parent thread into child tasks.
        // Could be null on threads which don't have JS interop, like managed thread pool threads. Unless they inherit it from the current Task
        // TODO flow it also with ExecutionContext to child threads ?
        private static readonly AsyncLocal<JSProxyContext?> _currentThreadContext = new AsyncLocal<JSProxyContext?>();
        public static JSProxyContext? ExecutionContext
        {
            get => _currentThreadContext.Value;
            set => _currentThreadContext.Value = value;
        }

        [ThreadStatic]
        public static JSProxyContext? CurrentThreadContext;

        // This is context to dispatch into. In order of preference
        // - captured context by arguments of current/pending JSImport call
        // - current thread context, for calls from JSWebWorker threads with the interop installed
        // - main thread, for calls from any other thread, like managed thread pool or `new Thread`
        public static JSProxyContext CurrentOperationContext
        {
            get
            {
                if (_CapturingState != JSImportOperationState.JSImportParams)
                {
                    Environment.FailFast($"Method only allowed during JSImport capturing phase, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
                }
                var capturedOperationContext = _CapturedOperationContext;
                if (capturedOperationContext != null)
                {
                    return capturedOperationContext;
                }
                // it could happen that we are in operation, in which we didn't capture target thread/context
                var executionContext = ExecutionContext;
                if (executionContext != null)
                {
                    // capture this fallback for validation of all other parameters
                    _CapturedOperationContext = executionContext;

                    // we could will call JS on the current thread (or child task), if it has the JS interop installed
                    return executionContext;
                }

                // otherwise we will call JS on the main thread, which always has JS interop
                var mainThreadContext = MainThreadContext;

                // capture this fallback for validation of all other parameters
                // such validation could fail if Task is marshaled earlier than JSObject and uses different target context
                _CapturedOperationContext = mainThreadContext;

                return mainThreadContext;
            }
        }

#endif

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static JSProxyContext AssertIsInteropThread()
        {
#if FEATURE_WASM_MANAGED_THREADS
            var ctx = CurrentThreadContext;
            if (ctx == null)
            {
                throw new InvalidOperationException($"Please use dedicated worker for working with JavaScript interop, ManagedThreadId:{Environment.CurrentManagedThreadId}. See https://aka.ms/dotnet-JS-interop-threads");
            }
            if (ctx._isDisposed)
            {
                ObjectDisposedException.ThrowIf(ctx._isDisposed, ctx);
            }
            return ctx;
#else
            return MainThreadContext;
#endif
        }

        #endregion

        #region Handles

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsJSVHandle(nint jsHandle)
        {
            return jsHandle < -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGCVHandle(nint gcHandle)
        {
            return gcHandle < -1;
        }

        public nint AllocJSVHandle()
        {
            lock (this)
            {
                if (JSVHandleFreeList.Count > 0)
                {
                    var jsvHandle = JSVHandleFreeList[JSVHandleFreeList.Count - 1];
                    JSVHandleFreeList.RemoveAt(JSVHandleFreeList.Count - 1);
                    return jsvHandle;
                }
                if (NextJSVHandle == IntPtr.Zero)
                {
                    NextJSVHandle = -2;
                }
                return NextJSVHandle--;
            }
        }

        public void FreeJSVHandle(nint jsvHandle)
        {
            lock (this)
            {
                JSVHandleFreeList.Add(jsvHandle);
            }
        }

        // A JSOwnedObject is a managed object with its lifetime controlled by javascript.
        // The managed side maintains a strong reference to the object, while the JS side
        //  maintains a weak reference and notifies the managed side if the JS wrapper object
        //  has been reclaimed by the JS GC. At that point, the managed side will release its
        //  strong references, allowing the managed object to be collected.
        // This ensures that things like delegates and promises will never 'go away' while JS
        //  is expecting to be able to invoke or await them.
        public IntPtr GetJSOwnedObjectGCHandle(object obj, GCHandleType handleType = GCHandleType.Normal)
        {
            if (obj == null)
            {
                return IntPtr.Zero;
            }

            lock (this)
            {
                if (ThreadJsOwnedObjects.TryGetValue(obj, out IntPtr gcHandle))
                {
                    return gcHandle;
                }

                IntPtr result = (IntPtr)GCHandle.Alloc(obj, handleType);
                ThreadJsOwnedObjects[obj] = result;
                return result;
            }
        }

        public PromiseHolder CreatePromiseHolder()
        {
            lock (this)
            {
                return new PromiseHolder(this);
            }
        }

        public PromiseHolder GetPromiseHolder(nint gcHandle)
        {
            lock (this)
            {
                PromiseHolder? holder;
                if (IsGCVHandle(gcHandle))
                {
                    if (!ThreadJsOwnedHolders.TryGetValue(gcHandle, out holder))
                    {
                        holder = new PromiseHolder(this, gcHandle);
                        ThreadJsOwnedHolders.Add(gcHandle, holder);
                    }
                }
                else
                {
                    holder = (PromiseHolder)((GCHandle)gcHandle).Target!;
                }
                return holder;
            }
        }

        public unsafe void ReleasePromiseHolder(nint holderGCHandle)
        {
            lock (this)
            {
                PromiseHolder? holder;
                if (IsGCVHandle(holderGCHandle))
                {
                    if (!ThreadJsOwnedHolders.Remove(holderGCHandle, out holder))
                    {
                        throw new InvalidOperationException("ReleasePromiseHolder expected PromiseHolder " + holderGCHandle);
                    }
                    holder.IsDisposed = true;
                }
                else
                {
                    GCHandle handle = (GCHandle)holderGCHandle;
                    var target = handle.Target!;
                    if (target is PromiseHolder holder2)
                    {
                        holder = holder2;
                    }
                    else
                    {
                        throw new InvalidOperationException("ReleasePromiseHolder expected PromiseHolder" + holderGCHandle);
                    }
                    holder.IsDisposed = true;
                    handle.Free();
                }
            }
        }

        public unsafe void ReleaseJSOwnedObjectByGCHandle(nint gcHandle)
        {
            ToManagedCallback? holderCallback = null;
            lock (this)
            {
                PromiseHolder? holder = null;
                if (IsGCVHandle(gcHandle))
                {
                    if (!ThreadJsOwnedHolders.Remove(gcHandle, out holder))
                    {
                        throw new InvalidOperationException("ReleaseJSOwnedObjectByGCHandle expected in ThreadJsOwnedHolders");
                    }
                }
                else
                {
                    GCHandle handle = (GCHandle)gcHandle;
                    var target = handle.Target!;
                    if (target is PromiseHolder holder2)
                    {
                        holder = holder2;
                    }
                    else
                    {
                        if (!ThreadJsOwnedObjects.Remove(target))
                        {
                            throw new InvalidOperationException("ReleaseJSOwnedObjectByGCHandle expected in ThreadJsOwnedObjects");
                        }
                    }
                    handle.Free();
                }
                if (holder != null)
                {
                    holderCallback = holder.Callback;
                    holder.IsDisposed = true;
                }
            }
            holderCallback?.Invoke(null);
        }

        public JSObject CreateCSOwnedProxy(nint jsHandle)
        {
            lock (this)
            {
                JSObject? res;
                if (!ThreadCsOwnedObjects.TryGetValue(jsHandle, out WeakReference<JSObject>? reference) ||
                    !reference.TryGetTarget(out res) ||
                    res.IsDisposed)
                {
                    res = new JSObject(jsHandle, this);
                    ThreadCsOwnedObjects[jsHandle] = new WeakReference<JSObject>(res, trackResurrection: true);
                }
                return res;
            }
        }

        public static void ReleaseCSOwnedObject(JSObject jso, bool skipJS)
        {
            if (jso.IsDisposed)
            {
                return;
            }
            var ctx = jso.ProxyContext;

            lock (ctx)
            {
                if (jso.IsDisposed || ctx._isDisposed)
                {
                    return;
                }
                var jsHandle = jso.JSHandle;
                jso._isDisposed = true;
                jso.JSHandle = IntPtr.Zero;
                GC.SuppressFinalize(jso);
                if (!ctx.ThreadCsOwnedObjects.Remove(jsHandle))
                {
                    Environment.FailFast($"ReleaseCSOwnedObject expected to find registration for JSHandle: {jsHandle}, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
                };
                if (!skipJS)
                {
#if FEATURE_WASM_MANAGED_THREADS
                    if (ctx.IsCurrentThread())
                    {
                        Interop.Runtime.ReleaseCSOwnedObject(jsHandle);
                    }
                    else
                    {
                        if (IsJSVHandle(jsHandle))
                        {
                            Environment.FailFast("TODO implement blocking ReleaseCSOwnedObjectSend to make sure the order of FreeJSVHandle is correct.");
                        }

                        // this is async message, we need to call this as the last thing
                        // the same jsHandle would not be re-used until JS side considers it free
                        Interop.Runtime.ReleaseCSOwnedObjectPost(ctx.JSNativeTID, jsHandle);
                    }
#else
                    Interop.Runtime.ReleaseCSOwnedObject(jsHandle);
#endif
                }
                if (IsJSVHandle(jsHandle))
                {
                    ctx.FreeJSVHandle(jsHandle);
                }
            }
        }

#endregion

        #region Dispose

        private void Dispose(bool disposing)
        {
            lock (this)
            {
                if (!_isDisposed)
                {
#if FEATURE_WASM_MANAGED_THREADS
                    if (!IsCurrentThread())
                    {
                        Environment.FailFast($"JSProxyContext must be disposed on the thread which owns it, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
                    }
                    ((GCHandle)ContextHandle).Free();
#endif

                    List<WeakReference<JSObject>> copy = new(ThreadCsOwnedObjects.Values);
                    foreach (var jsObjectWeak in copy)
                    {
                        if (jsObjectWeak.TryGetTarget(out var jso))
                        {
                            jso.Dispose();
                        }
                    }

#if FEATURE_WASM_MANAGED_THREADS
                    Interop.Runtime.UninstallWebWorkerInterop();
#endif

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

                    ThreadCsOwnedObjects.Clear();
                    ThreadJsOwnedObjects.Clear();
                    JSVHandleFreeList.Clear();
                    NextJSVHandle = IntPtr.Zero;

                    if (disposing)
                    {
#if FEATURE_WASM_MANAGED_THREADS
                        SynchronizationContext.Dispose();
#endif
                    }
                    _isDisposed = true;
                }
            }
        }

        ~JSProxyContext()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
