// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if FEATURE_WASM_MANAGED_THREADS

using System.Threading;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using WorkItemQueueType = System.Threading.Channels.Channel<System.Runtime.InteropServices.JavaScript.JSSynchronizationContext.WorkItem>;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Provides a thread-safe default SynchronizationContext for the browser that will automatically
    ///  route callbacks to the original browser thread where they can interact with the DOM and other
    ///  thread-affinity-having APIs like WebSockets, fetch, WebGL, etc.
    /// Callbacks are processed during event loop turns via the runtime's background job system.
    /// See also https://github.com/dotnet/runtime/blob/main/src/mono/wasm/threads.md#JS-interop-on-dedicated-threads
    /// </summary>
    internal sealed class JSSynchronizationContext : SynchronizationContext
    {
        internal readonly JSProxyContext ProxyContext;
        private readonly Action _ScheduleJSPump;// don't allocate Action on each call to UnsafeOnCompleted
        private readonly WorkItemQueueType Queue;

        internal SynchronizationContext? previousSynchronizationContext;
        internal bool _isDisposed;
        internal bool _isCancellationRequested;
        internal bool _isRunning;
        private CancellationTokenRegistration _cancellationTokenRegistration;

        internal struct WorkItem
        {
            public readonly SendOrPostCallback Callback;
            public readonly object? Data;
            public readonly ManualResetEventSlim? Signal;

            public WorkItem(SendOrPostCallback callback, object? data, ManualResetEventSlim? signal)
            {
                Callback = callback;
                Data = data;
                Signal = signal;
            }
        }

        // this need to be called from JSWebWorker or UI thread
        public static JSSynchronizationContext InstallWebWorkerInterop(bool isMainThread, CancellationToken cancellationToken)
        {
            var ctx = new JSSynchronizationContext(isMainThread, cancellationToken);
            ctx.previousSynchronizationContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(ctx);

            // FIXME: make this configurable
            // we could have 3 different modes of this
            // 1) throwing on UI + JSWebWorker
            // 2) throwing only on UI - small risk, more convenient.
            // 3) not throwing at all - quite risky
            // deadlock scenarios are:
            // - .Wait for more than 5000ms and deadlock the GC suspend
            // - .Wait on the Task from HTTP client, on the same thread as the HTTP client needs to resolve the Task/Promise. This could be also be a chain of promises.
            // - try to create new pthread when UI thread is blocked and we run out of posix/emscripten pool of loaded workers.
            // Things which lead to it are
            // - Task.Wait, Signal.Wait etc
            // - Monitor.Enter etc, if the lock is held by another thread for long time
            // - synchronous [JSExport] into managed code, which would block
            // - synchronous [JSImport] to another thread, which would block
            // see also https://github.com/dotnet/runtime/issues/76958#issuecomment-1921418290
            Thread.ThrowOnBlockingWaitOnJSInteropThread = true;

            var proxyContext = ctx.ProxyContext;
            JSProxyContext.CurrentThreadContext = proxyContext;
            JSProxyContext.ExecutionContext = proxyContext;
            if (isMainThread)
            {
                JSProxyContext.MainThreadContext = proxyContext;
            }

            ctx.AwaitNewData();

            Interop.Runtime.InstallWebWorkerInterop(proxyContext.ContextHandle);

            return ctx;
        }

        // this need to be called from JSWebWorker thread
        internal void UninstallWebWorkerInterop()
        {
            if (_isDisposed)
            {
                return;
            }
            if (!_isRunning)
            {
                return;
            }

            var jsProxyContext = JSProxyContext.AssertIsInteropThread();
            if (jsProxyContext != ProxyContext)
            {
                Environment.FailFast($"UninstallWebWorkerInterop failed, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
            }
            if (SynchronizationContext.Current == this)
            {
                SynchronizationContext.SetSynchronizationContext(this.previousSynchronizationContext);
            }

            // this will runtimeKeepalivePop()
            // and later maybeExit() -> __emscripten_thread_exit()
            // this will also call JSSynchronizationContext.Dispose() on this instance
            jsProxyContext.Dispose();

            JSProxyContext.CurrentThreadContext = null;
            JSProxyContext.ExecutionContext = null;
            _isRunning = false;
        }

        public JSSynchronizationContext(bool isMainThread, CancellationToken cancellationToken)
        {
            ProxyContext = new JSProxyContext(isMainThread, this);
            Queue = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = true, AllowSynchronousContinuations = true });
            _ScheduleJSPump = ScheduleJSPump;

            // receive callback (on any thread) that cancelation is requested
            _cancellationTokenRegistration = cancellationToken.Register(() =>
            {

                _isCancellationRequested = true;
                Queue.Writer.TryComplete();

                while (Queue.Reader.TryRead(out var item))
                {
                    // the Post is checking _isCancellationRequested after .Wait()
                    item.Signal?.Set();
                }
            });
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

        // this must be called from the worker thread
        internal void AwaitNewData()
        {
            if (_isDisposed)
            {
                return;
            }
            if (_isCancellationRequested)
            {
                UninstallWebWorkerInterop();
                return;
            }
            _isRunning = true;

            var vt = Queue.Reader.WaitToReadAsync();
            if (_isCancellationRequested)
            {
                UninstallWebWorkerInterop();
                return;
            }

            if (vt.IsCompleted)
            {
                ScheduleJSPump();
                return;
            }

            // Once data is added to the queue, vt will be completed on the thread that added the data
            //  because we created the channel with AllowSynchronousContinuations = true. We want to
            //  fire a callback that will schedule a background job to pump the queue on the main thread.
            var awaiter = vt.AsTask().ConfigureAwait(false).GetAwaiter();
            // UnsafeOnCompleted avoids spending time flowing the execution context (we don't need it.)
            awaiter.UnsafeOnCompleted(_ScheduleJSPump);
        }

        private unsafe void ScheduleJSPump()
        {
            // While we COULD pump here, we don't want to. We want the pump to happen on the next event loop turn.
            // Otherwise we could get a chain where a pump generates a new work item and that makes us pump again, forever.
            TargetThreadScheduleBackgroundJob(ProxyContext.NativeTID, (delegate* unmanaged[Cdecl]<void>)&BackgroundJobHandler);
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            if (_isCancellationRequested)
            {
                // propagate the cancellation to the caller
                throw new OperationCanceledException(_cancellationTokenRegistration.Token);
            }

            var workItem = new WorkItem(d, state, null);
            if (!Queue.Writer.TryWrite(workItem))
            {
                if (_isCancellationRequested)
                {
                    // propagate the cancellation to the caller
                    throw new OperationCanceledException(_cancellationTokenRegistration.Token);
                }
                ObjectDisposedException.ThrowIf(_isDisposed, this);
                Environment.FailFast($"JSSynchronizationContext.Post failed, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
            }
        }

        // This path can only run when threading is enabled
#pragma warning disable CA1416

        public override void Send(SendOrPostCallback d, object? state)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (ProxyContext.IsCurrentThread())
            {
                d(state);
                return;
            }

            Thread.AssureBlockingPossible();

            using (var signal = new ManualResetEventSlim(false))
            {
                var workItem = new WorkItem(d, state, signal);
                if (!Queue.Writer.TryWrite(workItem))
                {
                    if (_isCancellationRequested)
                    {
                        // propagate the cancellation to the caller
                        throw new OperationCanceledException(_cancellationTokenRegistration.Token);
                    }
                    ObjectDisposedException.ThrowIf(_isDisposed, this);

                    Environment.FailFast($"JSSynchronizationContext.Send failed, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
                }

                signal.Wait();

                if (_isCancellationRequested)
                {
                    // propagate the cancellation to the caller
                    throw new OperationCanceledException(_cancellationTokenRegistration.Token);
                }
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void TargetThreadScheduleBackgroundJob(IntPtr targetTID, void* callback);

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
        // this callback will arrive on the target thread, called from mono_background_exec
        private static void BackgroundJobHandler()
        {
            var ctx = JSProxyContext.AssertIsInteropThread();
            ctx.SynchronizationContext.Pump();
        }

        private void Pump()
        {
            if (_isDisposed || _isCancellationRequested)
            {
                UninstallWebWorkerInterop();
                return;
            }
            try
            {
                while (Queue.Reader.TryRead(out var item))
                {
                    try
                    {
                        item.Callback(item.Data);
                        // While we would ideally have a catch block here and do something to dispatch/forward unhandled
                        //  exceptions, the standard threadpool (and thus standard synchronizationcontext) have zero
                        //  error handling, so for consistency with them we do nothing. Don't throw in SyncContext callbacks.
                    }
                    finally
                    {
                        item.Signal?.Set();
                    }
                    if (_isDisposed || _isCancellationRequested)
                    {
                        UninstallWebWorkerInterop();
                        return;
                    }
                }
                // if anything throws unhandled exception, we will abort the program
                // otherwise, we could schedule another round
                AwaitNewData();
            }
            catch (Exception e)
            {
                Environment.FailFast($"JSSynchronizationContext.BackgroundJobHandler failed, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {e.StackTrace}");
            }
        }


        internal void Dispose()
        {
            if (!_isDisposed)
            {
                _isCancellationRequested = true;
                Queue.Writer.TryComplete();
                while (Queue.Reader.TryRead(out var item))
                {
                    // the Post is checking _isCancellationRequested after .Wait()
                    item.Signal?.Set();
                }
                _isDisposed = true;
                _cancellationTokenRegistration.Dispose();
                previousSynchronizationContext = null;
            }
        }
    }
}

#endif
