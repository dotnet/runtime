// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if FEATURE_WASM_THREADS

#pragma warning disable CA1416

using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// This is draft for possible public API of browser thread (web worker) dedicated to JS interop workloads.
    /// The method names are unique to make it easy to call them via reflection for now. All of them should be just `RunAsync` probably.
    /// </summary>
    public static class JSWebWorker
    {
        // temporary, for easy reflection
        internal static Task RunAsyncVoid(Func<Task> body, CancellationToken cancellationToken) => RunAsync(body, cancellationToken);
        internal static Task<T> RunAsyncGeneric<T>(Func<Task<T>> body, CancellationToken cancellationToken) => RunAsync(body, cancellationToken);

        public static Task<T> RunAsync<T>(Func<Task<T>> body)
        {
            return RunAsync(body, CancellationToken.None);
        }

        public static Task RunAsync(Func<Task> body)
        {
            return RunAsync(body, CancellationToken.None);
        }

        public static async Task<T> RunAsync<T>(Func<Task<T>> body, CancellationToken cancellationToken)
        {
            if (JSProxyContext.MainThreadContext.IsCurrentThread())
            {
                await JavaScriptImports.ThreadAvailable().ConfigureAwait(false);
            }
            return await RunAsyncImpl(body, cancellationToken).ConfigureAwait(false);
        }

        public static async Task RunAsync(Func<Task> body, CancellationToken cancellationToken)
        {
            if (JSProxyContext.MainThreadContext.IsCurrentThread())
            {
                await JavaScriptImports.ThreadAvailable().ConfigureAwait(false);
            }
            await RunAsyncImpl(body, cancellationToken).ConfigureAwait(false);
        }

        private static Task<T> RunAsyncImpl<T>(Func<Task<T>> body, CancellationToken cancellationToken)
        {
            var parentContext = SynchronizationContext.Current ?? new SynchronizationContext();
            var tcs = new TaskCompletionSource<T>();
            var capturedContext = SynchronizationContext.Current;
            var t = new Thread(() =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        PostWhenCancellation(parentContext, tcs);
                        return;
                    }

                    JSHostImplementation.InstallWebWorkerInterop(false);
                    var childScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                    Task<T> res = body();
                    // This code is exiting thread main() before all promises are resolved.
                    // the continuation is executed by setTimeout() callback of the thread.
                    res.ContinueWith(t =>
                    {
                        SendWhenDone(parentContext, tcs, res);
                        JSHostImplementation.UninstallWebWorkerInterop();
                    }, childScheduler);
                }
                catch (Exception ex)
                {
                    SendWhenException(parentContext, tcs, ex);
                }

            });
            JSHostImplementation.SetHasExternalEventLoop(t);
            t.Start();
            return tcs.Task;
        }

        private static Task RunAsyncImpl(Func<Task> body, CancellationToken cancellationToken)
        {
            var parentContext = SynchronizationContext.Current ?? new SynchronizationContext();
            var tcs = new TaskCompletionSource();
            var capturedContext = SynchronizationContext.Current;
            var t = new Thread(() =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        PostWhenCancellation(parentContext, tcs);
                        return;
                    }

                    JSHostImplementation.InstallWebWorkerInterop(false);
                    var childScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                    Task res = body();
                    // This code is exiting thread main() before all promises are resolved.
                    // the continuation is executed by setTimeout() callback of the thread.
                    res.ContinueWith(t =>
                    {
                        SendWhenDone(parentContext, tcs, res);
                        JSHostImplementation.UninstallWebWorkerInterop();
                    }, childScheduler);
                }
                catch (Exception ex)
                {
                    SendWhenException(parentContext, tcs, ex);
                }

            });
            JSHostImplementation.SetHasExternalEventLoop(t);
            t.Start();
            return tcs.Task;
        }

        #region posting result to the original thread when handling exception

        private static void PostWhenCancellation(SynchronizationContext ctx, TaskCompletionSource tcs)
        {
            try
            {
                ctx.Send((_) => tcs.SetCanceled(), null);
            }
            catch (Exception e)
            {
                Environment.FailFast("JSWebWorker.RunAsync failed", e);
            }
        }

        private static void PostWhenCancellation<T>(SynchronizationContext ctx, TaskCompletionSource<T> tcs)
        {
            try
            {
                ctx.Send((_) => tcs.SetCanceled(), null);
            }
            catch (Exception e)
            {
                Environment.FailFast("JSWebWorker.RunAsync failed", e);
            }
        }

        private static void SendWhenDone(SynchronizationContext ctx, TaskCompletionSource tcs, Task done)
        {
            try
            {
                ctx.Send((_) =>
                {
                    PropagateCompletion(tcs, done);
                }, null);
            }
            catch (Exception e)
            {
                Environment.FailFast("JSWebWorker.RunAsync failed", e);
            }
        }

        private static void SendWhenException(SynchronizationContext ctx, TaskCompletionSource tcs, Exception ex)
        {
            try
            {
                ctx.Send((_) => tcs.SetException(ex), null);
            }
            catch (Exception e)
            {
                Environment.FailFast("JSWebWorker.RunAsync failed", e);
            }
        }

        private static void SendWhenException<T>(SynchronizationContext ctx, TaskCompletionSource<T> tcs, Exception ex)
        {
            try
            {
                ctx.Send((_) => tcs.SetException(ex), null);
            }
            catch (Exception e)
            {
                Environment.FailFast("JSWebWorker.RunAsync failed", e);
            }
        }

        private static void SendWhenDone<T>(SynchronizationContext ctx, TaskCompletionSource<T> tcs, Task<T> done)
        {
            try
            {
                ctx.Send((_) =>
                {
                    PropagateCompletion(tcs, done);
                }, null);
            }
            catch (Exception e)
            {
                Environment.FailFast("JSWebWorker.RunAsync failed", e);
            }
        }

        internal static void PropagateCompletion<T>(TaskCompletionSource<T> tcs, Task<T> done)
        {
            if (done.IsFaulted)
            {
                if (done.Exception is AggregateException ag && ag.InnerException != null)
                {
                    tcs.SetException(ag.InnerException);
                }
                else
                {
                    tcs.SetException(done.Exception);
                }
            }
            else if (done.IsCanceled)
                tcs.SetCanceled();
            else
                tcs.SetResult(done.Result);
        }

        internal static void PropagateCompletion(TaskCompletionSource tcs, Task done)
        {
            if (done.IsFaulted)
            {
                if (done.Exception is AggregateException ag && ag.InnerException != null)
                {
                    tcs.SetException(ag.InnerException);
                }
                else
                {
                    tcs.SetException(done.Exception);
                }
            }
            else if (done.IsCanceled)
                tcs.SetCanceled();
            else
                tcs.SetResult();
        }

        #endregion

    }
}

#endif
