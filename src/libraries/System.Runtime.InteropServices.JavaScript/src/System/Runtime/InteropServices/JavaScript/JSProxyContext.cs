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
        private bool _disposedValue;

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
        public static readonly JSProxyContext DefaultInstance = new();
        public static JSProxyContext CurrentInstance => DefaultInstance;
        public static JSProxyContext CapturedInstance
        {
            get => MainInstance!;
            set { }
        }
        public static JSProxyContext MainInstance => DefaultInstance;
#else
        public nint TargetTID;
        public int ThreadId;
        public bool IsMainThread;
        public JSSynchronizationContext SynchronizationContext;

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "thread_id")]
        private static extern ref long GetThreadNativeThreadId(Thread @this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr GetNativeThreadId()
        {
            return (int)GetThreadNativeThreadId(Thread.CurrentThread);
        }

        // Context of the main thread
        private static JSProxyContext? _MainInstance;
        public static JSProxyContext MainInstance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _MainInstance!;
            set => _MainInstance = value;
        }

        // Context captured from parameters of the current operation.
        // Most of the time, this matches the current thread.
        // When we pass `JSObject` instance to `JSImport`, it could have thread affinity different than the current thread.
        // We will use that parameter to capture the target thread to call into.
        [ThreadStatic]
        public static JSProxyContext? CapturedInstance;

        // Context of the current thread. Could be null on threads which don't have JS interop, like managed thread pool threads.
        [ThreadStatic]
        public static JSProxyContext? CurrentInstance;

        // This is context to dispatch into. In order of preference
        // - captured context by arguments of current/pending JSImport call
        // - current thread context, for calls from JSWebWorker threads with the interop installed
        // - main thread, for calls from any other thread, like managed thread pool or `new Thread`
        public static JSProxyContext DefaultInstance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // until we set CapturedInstance in the generated code of JSImport/JSExport, we need to fallback to CurrentInstance to find target thread
                // because maybe it was not captured by the other parameters yet
                // after capture is solid, we could drop DefaultInstance and use CapturedInstance instead
                // TODO: sort generated ToJS() calls to make the capture first
                return CapturedInstance ?? CurrentInstance ?? MainInstance;
            }
        }

        public JSProxyContext(bool isMainThread, JSSynchronizationContext synchronizationContext)
        {
            SynchronizationContext = synchronizationContext;
            Interop.Runtime.InstallWebWorkerInterop();
            TargetTID = GetNativeThreadId();
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            IsMainThread = isMainThread;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTargetThread()
        {
            return ThreadId == Thread.CurrentThread.ManagedThreadId;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSProxyContext AssertCurrentContext()
        {
#if FEATURE_WASM_THREADS
            var ctx = CurrentInstance;
            if (ctx == null || ctx._disposedValue)
            {
                throw new InvalidOperationException($"Please use dedicated worker for working with JavaScript interop, ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}. See https://aka.ms/dotnet-JS-interop-threads");
            }
            return ctx;
#else
            return MainInstance;
#endif
        }

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
        public static IntPtr GetJSOwnedObjectGCHandle(object obj, GCHandleType handleType = GCHandleType.Normal)
        {
            if (obj == null)
            {
                return IntPtr.Zero;
            }

            var ctx = JSProxyContext.DefaultInstance;
            lock (ctx)
            {
                if (ctx.ThreadJsOwnedObjects.TryGetValue(obj, out IntPtr gcHandle))
                {
                    return gcHandle;
                }

                IntPtr result = (IntPtr)GCHandle.Alloc(obj, handleType);
                ctx.ThreadJsOwnedObjects[obj] = result;
                return result;
            }
        }

        // TODO unregister and collect pending PromiseHolder also when no C# is awaiting ?
        public static PromiseHolder GetPromiseHolder(nint gcHandle)
        {
            PromiseHolder holder;
            if (IsGCVHandle(gcHandle))
            {
                var ctx = JSProxyContext.DefaultInstance;
                lock (ctx)
                {
                    holder = new PromiseHolder(ctx, gcHandle);
                    ctx.ThreadJsOwnedHolders.Add(gcHandle, holder);
                }
            }
            else
            {
                holder = (PromiseHolder)((GCHandle)gcHandle).Target!;
            }
            return holder;
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
                        ThreadJsOwnedObjects.Remove(target);
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

        public void RemoveCSOwnedObject(nint jsHandle)
        {
            lock (this)
            {
                ThreadCsOwnedObjects.Remove(jsHandle);
            }
        }

        public void ReleaseCSOwnedObject(nint jsHandle)
        {
            if (jsHandle != IntPtr.Zero)
            {
                // TODO could this be called from wrong thread ?
                lock (this)
                {
#if FEATURE_WASM_THREADS && DEBUG
                    if (ThreadCsOwnedObjects.Remove(jsHandle, out WeakReference<JSObject>? weak))
                    {
                        if (weak.TryGetTarget(out JSObject? obj) && obj.ProxyContext != this)
                        {
                            Environment.FailFast("ReleaseCSOwnedObject must be called on the thread that JSObject belongs into.");
                        }
                    };
#else
                    ThreadCsOwnedObjects.Remove(jsHandle);
#endif
                    Interop.Runtime.ReleaseCSOwnedObject(jsHandle);
                    if (IsJSVHandle(jsHandle))
                    {
                        FreeJSVHandle(jsHandle);
                    }
                }
            }
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
                        ThreadJsOwnedObjects.Remove(target);
                    }
                    handle.Free();
                }
            }
            if (holder != null)
            {
                holder.Callback!(null);
            }
        }

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
                        LegacyHostImplementation.MappedType.JSObject => new JSObject(jsHandle, JSProxyContext.MainInstance),
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
                if (!_disposedValue)
                {
#if FEATURE_WASM_THREADS
                    if (!IsTargetThread()) Environment.FailFast($"JSProxyContext must be disposed on the thread which owns it.");
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
                    _disposedValue = true;
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
