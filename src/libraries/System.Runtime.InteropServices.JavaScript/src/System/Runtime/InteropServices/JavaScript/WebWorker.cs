// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if FEATURE_WASM_THREADS

#pragma warning disable CA1416

using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// This is draft for possible public API of browser thread (web worker) dedicated to JS interop workloads
    /// </summary>
    internal static class WebWorker
    {
        public static Task<T> RunAsync<T>(Func<Task<T>> body)
        {
            var parentContext = SynchronizationContext.Current ?? new SynchronizationContext();
            var tcs = new TaskCompletionSource<T>();
            var capturedContext = SynchronizationContext.Current;
            var t = new Thread(() =>
            {
                try
                {
                    JSHostImplementation.InstallWebWorkerInterop(true);
                    var childScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                    Task<T> res = body();
                    // This code is exiting thread main() before all promises are resolved.
                    // the continuation is executed by setTimeout() callback of the thread.
                    res.ContinueWith(t =>
                    {
                        parentContext.Post((_) => {
                            if (res.IsFaulted)
                                tcs.SetException(res.Exception);
                            else if (t.IsCanceled)
                                tcs.SetCanceled();
                            else
                                tcs.SetResult(res.Result);
                        }, null);
                        JSHostImplementation.UninstallWebWorkerInterop();
                    }, childScheduler);
                }
                catch (Exception e)
                {
                    Environment.FailFast("WebWorker.RunAsync failed", e);
                }

            });
            JSHostImplementation.SetHasExternalEventLoop(t);
            t.Start();
            return tcs.Task;
        }

        public static Task RunAsyncVoid(Func<Task> body)
        {
            var parentContext = SynchronizationContext.Current ?? new SynchronizationContext();
            var tcs = new TaskCompletionSource();
            var capturedContext = SynchronizationContext.Current;
            var t = new Thread(() =>
            {
                try
                {
                    JSHostImplementation.InstallWebWorkerInterop(true);
                    var childScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                    Task res = body();
                    // This code is exiting thread main() before all promises are resolved.
                    // the continuation is executed by setTimeout() callback of the thread.
                    res.ContinueWith(t =>
                    {
                        parentContext.Post((_) => {
                            if (res.IsFaulted)
                                tcs.SetException(res.Exception);
                            else if (t.IsCanceled)
                                tcs.SetCanceled();
                            else
                                tcs.SetResult();
                        }, null);
                        JSHostImplementation.UninstallWebWorkerInterop();
                    }, childScheduler);
                }
                catch (Exception e)
                {
                    Environment.FailFast("WebWorker.RunAsync failed", e);
                }

            });
            JSHostImplementation.SetHasExternalEventLoop(t);
            t.Start();
            return tcs.Task;
        }

        public static Task Run(Action body)
        {
            var parentContext = SynchronizationContext.Current ?? new SynchronizationContext();
            var tcs = new TaskCompletionSource();
            var capturedContext = SynchronizationContext.Current;
            var t = new Thread(() =>
            {
                try
                {
                    JSHostImplementation.InstallWebWorkerInterop(false);
                    body();
                    parentContext.Post((_) => {
                        tcs.SetResult();
                    }, null);
                    JSHostImplementation.UninstallWebWorkerInterop();
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }

            });
            JSHostImplementation.SetHasExternalEventLoop(t);
            t.Start();
            return tcs.Task;
        }
    }
}

#endif
