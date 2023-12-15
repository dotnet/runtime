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
        private bool _isDisposed;

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

#if !FEATURE_WASM_THREADS
        private JSProxyContext()
        {
        }
#else
        public nint NativeTID;
        public int ManagedTID;
        public bool IsMainThread;
        public JSSynchronizationContext SynchronizationContext;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCurrentThread()
        {
            return ManagedTID == Thread.CurrentThread.ManagedThreadId;
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
            Interop.Runtime.InstallWebWorkerInterop();
            NativeTID = GetNativeThreadId();
            ManagedTID = Thread.CurrentThread.ManagedThreadId;
            IsMainThread = isMainThread;
        }
#endif

        #region Current operation context

#if !FEATURE_WASM_THREADS
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

        private sealed class PendingOperation
        {
            public JSProxyContext? CapturedContext;
            public bool Called;
            public bool Multiple;
        }

        [ThreadStatic]
        private static List<PendingOperation>? _OperationStack;
        private static List<PendingOperation> OperationStack
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _OperationStack ??= new();
            }
        }

        public static void PushOperationUnknownContext()
        {
            var stack = OperationStack;
            // for SchedulePopOperation()
            if (stack.Count > 0 && stack[stack.Count - 1].Called)
            {
                PopOperation();
            }
            // because this is called multiple times for each JSImport
            if (stack.Count > 0 && stack[stack.Count - 1].Multiple)
            {
                return;
            }

            stack.Add(new PendingOperation() { Multiple = true });
        }

        public static void PushOperationWithContext(JSProxyContext knownContext)
        {
            var stack = OperationStack;
            // for SchedulePopOperation()
            if (stack.Count > 0 && stack[stack.Count - 1].Called)
            {
                PopOperation();
            }
            // because this is called multiple times for each JSImport
            if (stack.Count > 0 && stack[stack.Count - 1].Multiple)
            {
                return;
            }

            stack.Add(new PendingOperation() { CapturedContext = knownContext, Multiple = true });
        }

        public static JSProxyContext PushOperationWithCurrentThreadContext()
        {
            var stack = OperationStack;
            // for SchedulePopOperation()
            if (stack.Count > 0 && stack[stack.Count - 1].Called)
            {
                PopOperation();
            }

            var current = AssertIsInteropThread();
            stack.Add(new PendingOperation { CapturedContext = current });
            return current;
        }

        public static void PopOperation()
        {
            var stack = OperationStack;
            if (stack.Count < 1) Environment.FailFast("Unbalanced PopOperation");// there is no recovery
            stack.RemoveAt(stack.Count - 1);
        }

        // this is here until we change the code generator and the API to Push/Pop the context
        // we have no way how to Pop from the after marshaling the result
        public static void SchedulePopOperation()
        {
            var stack = OperationStack;
            var op = stack[stack.Count - 1];
            if (op.Called) Environment.FailFast("SchedulePopOperation called twice");
            op.Called = true;
        }

        public static void AssertOperationStack(int expected)
        {
            var stack = OperationStack;
            if (stack.Count > 0 && stack[stack.Count - 1].Called)
            {
                PopOperation();
            }
            var actual = stack.Count;
            var multiple = stack.Count > 0 ? stack[stack.Count - 1].Multiple : false;
            var called = stack.Count > 0 ? stack[stack.Count - 1].Called : false;
            if (actual != expected) Environment.FailFast($"Unexpected OperationStack size expected: {expected} actual: {actual} called:{called} multiple:{multiple}");
        }

        // TODO: sort generated ToJS() calls to make the capture context before we need to use it
        public static void CaptureContextFromParameter(JSProxyContext parameterContext)
        {
            var stack = OperationStack;
            var pendingOperation = stack[stack.Count - 1];
            var capturedContext = pendingOperation.CapturedContext;
            if (capturedContext != null && parameterContext != capturedContext)
            {
                throw new InvalidOperationException("All JSObject proxies need to have same thread affinity");
            }
            pendingOperation.CapturedContext = capturedContext;
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var stack = OperationStack;
                if (stack.Count < 1) throw new Exception("CurrentOperationContext could be only used during pending operation.");
                var pendingOperation = stack[stack.Count - 1];
                if (pendingOperation.CapturedContext != null)
                {
                    return pendingOperation.CapturedContext;
                }
                // it could happen that we are in operation, in which we didn't capture target thread/context
                var executionContext = ExecutionContext;
                if (executionContext != null)
                {
                    // we could will call JS on the current thread (or child task), if it has the JS interop installed
                    return executionContext;
                }
                // otherwise we will call JS on the main thread, which always has JS interop
                return MainThreadContext;
            }
        }

#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSProxyContext AssertIsInteropThread()
        {
#if FEATURE_WASM_THREADS
            var ctx = CurrentThreadContext;
            if (ctx == null)
            {
                throw new InvalidOperationException($"Please use dedicated worker for working with JavaScript interop, ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}. See https://aka.ms/dotnet-JS-interop-threads");
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

        // TODO unregister and collect pending PromiseHolder also when no C# is awaiting ?
        public PromiseHolder GetPromiseHolder(nint gcHandle)
        {
            PromiseHolder holder;
            if (IsGCVHandle(gcHandle))
            {
                lock (this)
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

        public unsafe void ReleaseJSOwnedObjectByGCHandle(nint gcHandle)
        {
            PromiseHolder? holder = null;
            lock (this)
            {
                if (IsGCVHandle(gcHandle))
                {
                    if (ThreadJsOwnedHolders.Remove(gcHandle, out holder))
                    {
                        holder.GCHandle = IntPtr.Zero;
                    }
                    else
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
                        holder.GCHandle = IntPtr.Zero;
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
            }
            if (holder != null)
            {
                holder.Callback!(null);
            }
        }

        public PromiseHolder? ReleasePromiseHolder(nint holderGCHandle)
        {
            PromiseHolder? holder = null;
            lock (this)
            {
                if (IsGCVHandle(holderGCHandle))
                {
                    if (ThreadJsOwnedHolders.Remove(holderGCHandle, out holder))
                    {
                        holder.GCHandle = IntPtr.Zero;
                    }
                    else
                    {
                        throw new InvalidOperationException("ReleasePromiseHolder expected to find handle in ThreadJsOwnedHolders");
                    }
                }
                else
                {
                    GCHandle handle = (GCHandle)holderGCHandle;
                    var target = handle.Target!;
                    if (target is PromiseHolder holder2)
                    {
                        holder = holder2;
                        holder.GCHandle = IntPtr.Zero;
                    }
                    else
                    {
                        if (!ThreadJsOwnedObjects.Remove(target))
                        {
                            throw new InvalidOperationException("ReleasePromiseHolder expected to find handle in ThreadJsOwnedObjects");
                        }
                    }
                    handle.Free();
                }
            }
            return holder;
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

        public static void ReleaseCSOwnedObject(JSObject proxy, bool skipJS)
        {
            if (proxy.IsDisposed)
            {
                return;
            }
            var ctx = proxy.ProxyContext;
#if FEATURE_WASM_THREADS
            if (!ctx.IsCurrentThread())
            {
                throw new InvalidOperationException("ReleaseCSOwnedObject has to run on the thread with same affinity as the proxy");
            }
#endif
            lock (ctx)
            {
                if (proxy.IsDisposed)
                {
                    return;
                }
                proxy._isDisposed = true;
                GC.SuppressFinalize(proxy);
                var jsHandle = proxy.JSHandle;
                if (!ctx.ThreadCsOwnedObjects.Remove(jsHandle))
                {
                    throw new InvalidOperationException("ReleaseCSOwnedObject expected to find registration for" + jsHandle);
                };
                if (!skipJS)
                {
                    Interop.Runtime.ReleaseCSOwnedObject(jsHandle);
                }
                if (IsJSVHandle(jsHandle))
                {
                    ctx.FreeJSVHandle(jsHandle);
                }
            }
        }

#endregion

        #region Legacy

        // legacy
        public void RegisterCSOwnedObject(JSObject proxy)
        {
            lock (this)
            {
                ThreadCsOwnedObjects[(int)proxy.JSHandle] = new WeakReference<JSObject>(proxy, trackResurrection: true);
            }
        }

        // legacy
        public JSObject? GetCSOwnedObjectByJSHandle(nint jsHandle, int shouldAddInflight)
        {
            lock (this)
            {
                if (ThreadCsOwnedObjects.TryGetValue(jsHandle, out WeakReference<JSObject>? reference))
                {
                    reference.TryGetTarget(out JSObject? jsObject);
                    if (shouldAddInflight != 0)
                    {
                        jsObject?.AddInFlight();
                    }
                    return jsObject;
                }
            }
            return null;
        }

        // legacy
        public JSObject CreateCSOwnedProxy(nint jsHandle, LegacyHostImplementation.MappedType mappedType, int shouldAddInflight)
        {
            lock (this)
            {
                JSObject? res = null;
                if (!ThreadCsOwnedObjects.TryGetValue(jsHandle, out WeakReference<JSObject>? reference) ||
                !reference.TryGetTarget(out res) ||
                res.IsDisposed)
                {
#pragma warning disable CS0612 // Type or member is obsolete
                    res = mappedType switch
                    {
                        LegacyHostImplementation.MappedType.JSObject => new JSObject(jsHandle, JSProxyContext.MainThreadContext),
                        LegacyHostImplementation.MappedType.Array => new Array(jsHandle),
                        LegacyHostImplementation.MappedType.ArrayBuffer => new ArrayBuffer(jsHandle),
                        LegacyHostImplementation.MappedType.DataView => new DataView(jsHandle),
                        LegacyHostImplementation.MappedType.Function => new Function(jsHandle),
                        LegacyHostImplementation.MappedType.Uint8Array => new Uint8Array(jsHandle),
                        _ => throw new ArgumentOutOfRangeException(nameof(mappedType))
                    };
#pragma warning restore CS0612 // Type or member is obsolete
                    ThreadCsOwnedObjects[jsHandle] = new WeakReference<JSObject>(res, trackResurrection: true);
                }
                if (shouldAddInflight != 0)
                {
                    res.AddInFlight();
                }
                return res;
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
#if FEATURE_WASM_THREADS
                    if (!IsCurrentThread()) throw new InvalidOperationException("JSProxyContext must be disposed on the thread which owns it.");
                    AssertOperationStack(0);
                    _OperationStack = null;
#endif

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    foreach (var jsObjectWeak in ThreadCsOwnedObjects.Values)
                    {
                        if (jsObjectWeak.TryGetTarget(out var jso))
                        {
                            jso.Dispose();
                        }
                    }

#if FEATURE_WASM_THREADS
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

                    if (disposing)
                    {
                        ThreadCsOwnedObjects.Clear();
                        ThreadJsOwnedObjects.Clear();
                        JSVHandleFreeList.Clear();
                        NextJSVHandle = IntPtr.Zero;
#if FEATURE_WASM_THREADS
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
