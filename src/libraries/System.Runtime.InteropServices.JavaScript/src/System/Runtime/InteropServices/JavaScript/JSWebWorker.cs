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

        public static Task<T> RunAsync<T>(Func<Task<T>> body, CancellationToken cancellationToken)
        {
            var instance = new JSWebWorkerInstance<T>(body, cancellationToken);
            return instance.Start();
        }

        public static Task RunAsync(Func<Task> body, CancellationToken cancellationToken)
        {
            var instance = new JSWebWorkerInstance<int>(async () =>
            {
                await body().ConfigureAwait(false);
                return 0;
            }, cancellationToken);
            return instance.Start();
        }

        internal sealed class JSWebWorkerInstance<T> : IDisposable
        {
            private JSSynchronizationContext? _jsSynchronizationContext;
            private TaskCompletionSource<T> _taskCompletionSource;
            private Thread _thread;
            private CancellationToken _cancellationToken;
            private CancellationTokenRegistration? _cancellationRegistration;
            private Func<Task<T>> _body;
            private Task<T>? _resultTask;
            private bool _isDisposed;

            public JSWebWorkerInstance(Func<Task<T>> bodyRes, CancellationToken cancellationToken)
            {
                _taskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.DenyChildAttach | TaskCreationOptions.RunContinuationsAsynchronously | TaskCreationOptions.HideScheduler);
                _thread = new Thread(ThreadMain);
                _resultTask = null;
                _cancellationToken = cancellationToken;
                _cancellationRegistration = null;
                _body = bodyRes;
                JSHostImplementation.SetHasExternalEventLoop(_thread);
            }

            public Task<T> Start()
            {
                if (JSProxyContext.MainThreadContext.IsCurrentThread())
                {
                    // give browser chance to load more threads
                    // until there at least one thread loaded, it doesn't make sense to `Start`
                    // because that would also hang, but in a way blocking the UI thread, much worse.
                    JavaScriptImports.ThreadAvailable().ContinueWith(static (t, o) =>
                    {
                        var self = (JSWebWorkerInstance<T>)o!;
                        if (t.IsCompletedSuccessfully)
                        {
                            self._thread.Start();
                        }
                        if (t.IsCanceled)
                        {
                            throw new OperationCanceledException(self._cancellationToken);
                        }
                        throw t.Exception!;
                    }, this, _cancellationToken, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    _thread.Start();
                }
                return _taskCompletionSource.Task;
            }

            private void ThreadMain()
            {
                try
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        PropagateCompletionAndDispose(Task.FromCanceled<T>(_cancellationToken));
                        return;
                    }

                    // receive callback when the cancellation is requested
                    _cancellationRegistration = _cancellationToken.Register(static (o) =>
                    {
                        var self = (JSWebWorkerInstance<T>)o!;
                        // this could be executing on any thread
                        self.PropagateCompletionAndDispose(Task.FromCanceled<T>(self._cancellationToken));
                    }, this);

                    // JSSynchronizationContext also registers to _cancellationToken
                    _jsSynchronizationContext = JSSynchronizationContext.InstallWebWorkerInterop(false, _cancellationToken);

                    var childScheduler = TaskScheduler.FromCurrentSynchronizationContext();

                    // This code is exiting thread ThreadMain() before all promises are resolved.
                    // the continuation is executed by setTimeout() callback of the WebWorker thread.
                    _body().ContinueWith(PropagateCompletionAndDispose, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, childScheduler);
                }
                catch (Exception ex)
                {
                    PropagateCompletionAndDispose(Task.FromException<T>(ex));
                }
            }

            // run actions on correct thread
            private void PropagateCompletionAndDispose(Task<T> result)
            {
                _resultTask = result;

                _cancellationRegistration?.Dispose();
                _cancellationRegistration = null;

                if (Thread.CurrentThread == _thread)
                {
                    // if cancelation was requested, the JSSynchronizationContext will stop the thread
                    if (_jsSynchronizationContext != null && !_cancellationToken.IsCancellationRequested)
                    {
                        // this will lead to __emscripten_thread_exit() later
                        _jsSynchronizationContext.UninstallWebWorkerInterop();
                    }
                    _jsSynchronizationContext = null;

                    // propagate the result on thread pool, rather than the JSWebWorker
                    Task.Run(PropagateCompletion);
                }
                else
                {
                    // if cancelation was requested, the JSSynchronizationContext will stop the thread
                    if (_jsSynchronizationContext != null && !_cancellationToken.IsCancellationRequested)
                    {
                        // we can only uninstall JS interop on it's own thread
                        _jsSynchronizationContext.Post(static o =>
                        {
                            var jsSynchronizationContext = (JSSynchronizationContext)o!;
                            // this will lead to __emscripten_thread_exit() later
                            jsSynchronizationContext?.UninstallWebWorkerInterop();
                        }, _jsSynchronizationContext);
                    }
                    _jsSynchronizationContext = null;

                    PropagateCompletion();
                }

                Dispose();
            }

            private void PropagateCompletion()
            {
                if (_resultTask!.IsFaulted)
                {
                    if (_resultTask.Exception is AggregateException ag && ag.InnerException != null)
                    {
                        _taskCompletionSource.TrySetException(ag.InnerException);
                    }
                    else
                    {
                        _taskCompletionSource.TrySetException(_resultTask.Exception);
                    }
                }
                else if (_resultTask.IsCanceled)
                {
                    _taskCompletionSource.TrySetCanceled();
                }
                else
                {
                    _taskCompletionSource.TrySetResult(_resultTask.Result);
                }
            }

            private void Dispose(bool disposing)
            {
                lock (this)
                {
                    if (_isDisposed)
                    {
                        return;
                    }
                    _isDisposed = true;
                }

                if (disposing)
                {
                    _cancellationRegistration?.Dispose();
                    _cancellationRegistration = null;
                }

                if (_jsSynchronizationContext != null)
                {
                    // this should not happen
                    Environment.FailFast($"JSWebWorker was disposed while running, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
                }
            }

            ~JSWebWorkerInstance()
            {
                Dispose(disposing: false);
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}

#endif
