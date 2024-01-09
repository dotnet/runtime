// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if FEATURE_WASM_THREADS

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
        private readonly Action _DataIsAvailable;// don't allocate Action on each call to UnsafeOnCompleted
        private readonly WorkItemQueueType Queue;

        internal SynchronizationContext? previousSynchronizationContext;
        internal bool _isDisposed;
        internal bool _isCancellationRequested;
        private CancellationTokenRegistration _cancellationTokenRegistration;

        internal readonly struct WorkItem
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

        public JSSynchronizationContext(bool isMainThread, CancellationToken cancellationToken)
        {
            ProxyContext = new JSProxyContext(isMainThread, this);
            Queue = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = true, AllowSynchronousContinuations = true });
            _DataIsAvailable = DataIsAvailable;
            _cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                _isCancellationRequested=true;
                Queue.Writer.TryComplete();
            });
        }

        internal JSSynchronizationContext(JSProxyContext proxyContext, WorkItemQueueType queue, Action dataIsAvailable)
        {
            ProxyContext = proxyContext;
            Queue = queue;
            _DataIsAvailable = dataIsAvailable;
        }

        public override SynchronizationContext CreateCopy()
        {
            return new JSSynchronizationContext(ProxyContext, Queue, _DataIsAvailable);
        }

        internal void AwaitNewData()
        {
            if (_isDisposed || _isCancellationRequested)
            {
                // FIXME: there could be abandoned work, but here we have no way how to propagate the failure
                // ObjectDisposedException.ThrowIf(_isDisposed, this);
                return;
            }

            var vt = Queue.Reader.WaitToReadAsync();
            if (_isCancellationRequested)
            {
                return;
            }

            if (vt.IsCompleted)
            {
                DataIsAvailable();
                return;
            }

            // Once data is added to the queue, vt will be completed on the thread that added the data
            //  because we created the channel with AllowSynchronousContinuations = true. We want to
            //  fire a callback that will schedule a background job to pump the queue on the main thread.
            var awaiter = vt.AsTask().ConfigureAwait(false).GetAwaiter();
            // UnsafeOnCompleted avoids spending time flowing the execution context (we don't need it.)
            awaiter.UnsafeOnCompleted(_DataIsAvailable);
        }

        private unsafe void DataIsAvailable()
        {
            // While we COULD pump here, we don't want to. We want the pump to happen on the next event loop turn.
            // Otherwise we could get a chain where a pump generates a new work item and that makes us pump again, forever.
            TargetThreadScheduleBackgroundJob(ProxyContext.NativeTID, (void*)(delegate* unmanaged[Cdecl]<void>)&BackgroundJobHandler);
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            var workItem = new WorkItem(d, state, null);
            if (!Queue.Writer.TryWrite(workItem))
                Environment.FailFast($"JSSynchronizationContext.Post failed, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
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

            using (var signal = new ManualResetEventSlim(false))
            {
                var workItem = new WorkItem(d, state, signal);
                if (!Queue.Writer.TryWrite(workItem))
                    Environment.FailFast($"JSSynchronizationContext.Send failed, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");

                signal.Wait();
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
                // FIXME: there could be abandoned work, but here we have no way how to propagate the failure
                // ObjectDisposedException.ThrowIf(_isDisposed, this);
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
                }
            }
            catch (Exception e)
            {
                Environment.FailFast($"JSSynchronizationContext.BackgroundJobHandler failed, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {e.StackTrace}");
            }
            finally
            {
                // If an item throws, we want to ensure that the next pump gets scheduled appropriately regardless.
                if (!_isDisposed && !_isCancellationRequested) AwaitNewData();
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Queue.Writer.Complete();
                }
                previousSynchronizationContext = null;
                _isDisposed = true;
            }
        }

        internal void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

#endif
