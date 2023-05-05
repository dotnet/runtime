// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        /// <summary>
        /// The worker thread infastructure for the CLR thread pool.
        /// </summary>
        private static partial class WorkerThread
        {
            /// <summary>
            /// Semaphore for controlling how many threads are currently working.
            /// </summary>
            private static readonly LowLevelLifoAsyncWaitSemaphore s_semaphore =
                new LowLevelLifoAsyncWaitSemaphore(
                    0,
                    MaxPossibleThreadCount,
                    AppContextConfigHelper.GetInt32Config(
                        "System.Threading.ThreadPool.UnfairSemaphoreSpinLimit",
                        SemaphoreSpinCountDefault,
                        false),
                    onWait: () =>
                    {
                        if (NativeRuntimeEventSource.Log.IsEnabled())
                        {
                            NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadWait(
                                (uint)ThreadPoolInstance._separated.counts.VolatileRead().NumExistingThreads);
                        }
                    });

            private static readonly ThreadStart s_workerThreadStart = WorkerThreadStart;

            private sealed record SemaphoreWaitState(PortableThreadPool ThreadPoolInstance, LowLevelLock ThreadAdjustmentLock, WebWorkerEventLoop.KeepaliveToken KeepaliveToken)
            {
                public bool SpinWait = true;

                public void ResetIteration() {
                    SpinWait = true;
                }
            }

            private static void WorkerThreadStart()
            {
                Thread.CurrentThread.SetThreadPoolWorkerThreadName();

                PortableThreadPool threadPoolInstance = ThreadPoolInstance;

                if (NativeRuntimeEventSource.Log.IsEnabled())
                {
                    NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadStart(
                        (uint)threadPoolInstance._separated.counts.VolatileRead().NumExistingThreads);
                }

                LowLevelLock threadAdjustmentLock = threadPoolInstance._threadAdjustmentLock;
                var keepaliveToken = WebWorkerEventLoop.KeepalivePush();
                SemaphoreWaitState state = new(threadPoolInstance, threadAdjustmentLock, keepaliveToken) { SpinWait = true };
                // set up the callbacks for semaphore waits, tell
                // emscripten to keep the thread alive, and return to
                // the JS event loop.
                WaitForWorkLoop(s_semaphore, state);
                // return from thread start with keepalive - the thread will stay alive in the JS event loop
            }

            private static readonly Action<LowLevelLifoAsyncWaitSemaphore, object?> s_WorkLoopSemaphoreSuccess = new(WorkLoopSemaphoreSuccess);
            private static readonly Action<LowLevelLifoAsyncWaitSemaphore, object?> s_WorkLoopSemaphoreTimedOut = new(WorkLoopSemaphoreTimedOut);

            private static void WaitForWorkLoop(LowLevelLifoAsyncWaitSemaphore semaphore, SemaphoreWaitState state)
            {
                semaphore.PrepareAsyncWait(ThreadPoolThreadTimeoutMs, s_WorkLoopSemaphoreSuccess, s_WorkLoopSemaphoreTimedOut, state);
                // thread should still be kept alive
                Debug.Assert(state.KeepaliveToken.Valid);
            }

            private static void WorkLoopSemaphoreSuccess(LowLevelLifoAsyncWaitSemaphore semaphore, object? stateObject)
            {
                SemaphoreWaitState state = (SemaphoreWaitState)stateObject!;
                WorkerDoWork(state.ThreadPoolInstance, ref state.SpinWait);
                // Go around the loop one more time, keeping existing mutated state
                WaitForWorkLoop(semaphore, state);
            }

            private static void WorkLoopSemaphoreTimedOut(LowLevelLifoAsyncWaitSemaphore semaphore, object? stateObject)
            {
                SemaphoreWaitState state = (SemaphoreWaitState)stateObject!;
                if (ShouldExitWorker(state.ThreadPoolInstance, state.ThreadAdjustmentLock)) {
                    // we're done, kill the thread.

                    // we're wrapped in an emscripten eventloop handler which will consult the
                    // keepalive count, destroy the thread and run the TLS dtor which will
                    // unregister the thread from Mono
                    state.KeepaliveToken.Pop();
                    return;
                } else {
                    // more work showed up while we were shutting down, go around one more time
                    state.ResetIteration();
                    WaitForWorkLoop(semaphore, state);
                }
            }

            private static void CreateWorkerThread()
            {
                // Thread pool threads must start in the default execution context without transferring the context, so
                // using captureContext: false.
                Thread workerThread = new Thread(s_workerThreadStart);
                workerThread.IsThreadPoolThread = true;
                workerThread.IsBackground = true;
                // thread name will be set in thread proc

                // This thread will return to the JS event loop - tell the runtime not to cleanup
                // after the start function returns, if the Emscripten keepalive is non-zero.
                WebWorkerEventLoop.StartExitable(workerThread, captureContext: false);
            }
        }
    }
}
