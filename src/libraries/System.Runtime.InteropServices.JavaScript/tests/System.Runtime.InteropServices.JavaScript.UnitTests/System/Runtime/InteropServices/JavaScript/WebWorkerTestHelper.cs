// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public partial class WebWorkerTestHelper
    {
        public static readonly string LocalHttpEcho = "http://" + Environment.GetEnvironmentVariable("DOTNET_TEST_HTTPHOST") + "/Echo.ashx";
        public static readonly string LocalWsEcho = "ws://" + Environment.GetEnvironmentVariable("DOTNET_TEST_WEBSOCKETHOST") + "/WebSocket/EchoWebSocket.ashx";

        [JSImport("globalThis.console.log")]
        public static partial void Log(string message);

        [JSImport("delay", "InlineTestHelper")]
        public static partial Task JSDelay(int ms);

        [JSImport("getTid", "WebWorkerTestHelper")]
        public static partial int GetTid();

        [JSImport("getState", "WebWorkerTestHelper")]
        public static partial JSObject GetState();

        [JSImport("promiseState", "WebWorkerTestHelper")]
        public static partial Task<JSObject> PromiseState();

        [JSImport("validateState", "WebWorkerTestHelper")]
        public static partial bool ValidateState(JSObject state);

        [JSImport("promiseValidateState", "WebWorkerTestHelper")]
        public static partial Task<bool> PromiseValidateState(JSObject state);

        public static string GetOriginUrl()
        {
            using var globalThis = JSHost.GlobalThis;
            using var document = globalThis.GetPropertyAsJSObject("document");
            using var location = globalThis.GetPropertyAsJSObject("location");
            return location.GetPropertyAsString("origin");
        }

        public static Task<JSObject> ImportModuleFromString(string jsModule)
        {
            var es6DataUrl = $"data:text/javascript,{jsModule.Replace('\r', ' ').Replace('\n', ' ')}";
            return JSHost.ImportAsync("InlineTestHelper", es6DataUrl);
        }

        #region Setup

        [ThreadStatic]
        public static JSObject WebWorkerTestHelperModule;
        [ThreadStatic]
        public static JSObject InlineTestHelperModule;

        [JSImport("setup", "WebWorkerTestHelper")]
        internal static partial Task Setup();

        [JSImport("INTERNAL.forceDisposeProxies")]
        internal static partial void ForceDisposeProxies(bool disposeMethods, bool verbose);

        public static async Task CreateDelay()
        {
            if (InlineTestHelperModule == null)
            {
                InlineTestHelperModule = await ImportModuleFromString(@"
                    export function delay(ms) {
                        return new Promise(resolve => setTimeout(resolve, ms))
                    }
                ").ConfigureAwait(false);
            }
            else
            {
                await JSDelay(1).ConfigureAwait(false);
            }
        }

        public static async Task InitializeAsync()
        {
            if (WebWorkerTestHelperModule != null)
            {
                await DisposeAsync();
            }

            WebWorkerTestHelperModule = await JSHost.ImportAsync("WebWorkerTestHelper", "../WebWorkerTestHelper.mjs?" + Guid.NewGuid());
            await CreateDelay();
            await Setup();
        }

        public static Task DisposeAsync()
        {
            WebWorkerTestHelperModule?.Dispose();
            WebWorkerTestHelperModule = null;
            return Task.CompletedTask;
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "thread_id")]
        private static extern ref long GetThreadNativeThreadId(Thread @this);

        public static IntPtr NativeThreadId => (int)GetThreadNativeThreadId(Thread.CurrentThread);

        #endregion
    }

    #region Executor

    public enum ExecutorType
    {
        Main,
        ThreadPool,
        NewThread,
        JSWebWorker,
    }

    public class Executor
    {
        public int ExecutorTID;
        public SynchronizationContext ExecutorSynchronizationContext;
        private static SynchronizationContext _mainSynchronizationContext;
        public static SynchronizationContext MainSynchronizationContext
        {

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern")]
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern")]
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:UnrecognizedReflectionPattern")]
            get
            {
                if (_mainSynchronizationContext != null)
                {
                    return _mainSynchronizationContext;
                }
                var jsProxyContext = typeof(JSObject).Assembly.GetType("System.Runtime.InteropServices.JavaScript.JSProxyContext");
                var mainThreadContext = jsProxyContext.GetField("_MainThreadContext", BindingFlags.NonPublic | BindingFlags.Static);
                var synchronizationContext = jsProxyContext.GetField("SynchronizationContext", BindingFlags.Public | BindingFlags.Instance);
                var mainCtx = mainThreadContext.GetValue(null);
                _mainSynchronizationContext = (SynchronizationContext)synchronizationContext.GetValue(mainCtx);
                return _mainSynchronizationContext;
            }
        }

        public ExecutorType Type;

        public Executor(ExecutorType type)
        {
            Type = type;
        }

        public Task Execute(Func<Task> job, CancellationToken cancellationToken)
        {
            Task wrapExecute()
            {
                ExecutorTID = Environment.CurrentManagedThreadId;
                ExecutorSynchronizationContext = SynchronizationContext.Current ?? MainSynchronizationContext;
                AssertTargetThread();
                return job();
            }

            switch (Type)
            {
                case ExecutorType.Main:
                    return RunOnTargetAsync(MainSynchronizationContext, wrapExecute, cancellationToken);
                case ExecutorType.ThreadPool:
                    return RunOnThreadPool(wrapExecute, cancellationToken);
                case ExecutorType.NewThread:
                    return RunOnNewThread(wrapExecute, cancellationToken);
                case ExecutorType.JSWebWorker:
                    return JSWebWorker.RunAsync(wrapExecute, cancellationToken);
                default:
                    throw new InvalidOperationException();
            }
        }

        public void AssertTargetThread()
        {
            if (Type == ExecutorType.Main)
            {
                Assert.Equal(1, Environment.CurrentManagedThreadId);
            }
            else
            {
                Assert.NotEqual(1, Environment.CurrentManagedThreadId);
            }
            if (Type == ExecutorType.ThreadPool)
            {
                Assert.True(Thread.CurrentThread.IsThreadPoolThread, "IsThreadPoolThread:" + Thread.CurrentThread.IsThreadPoolThread + " Type " + Type);
            }
            else
            {
                Assert.False(Thread.CurrentThread.IsThreadPoolThread, "IsThreadPoolThread:" + Thread.CurrentThread.IsThreadPoolThread + " Type " + Type);
            }
        }

        public void AssertAwaitCapturedContext()
        {
            switch (Type)
            {
                case ExecutorType.Main:
                    Assert.Equal(1, Environment.CurrentManagedThreadId);
                    Assert.Equal(ExecutorTID, Environment.CurrentManagedThreadId);
                    Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                    break;
                case ExecutorType.JSWebWorker:
                    Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                    Assert.Equal(ExecutorTID, Environment.CurrentManagedThreadId);
                    Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                    break;
                case ExecutorType.NewThread:
                    Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                    // sometimes this is TP and some times newThread, why ?
                    if (Thread.CurrentThread.IsThreadPoolThread)
                    {
                        Assert.NotEqual(ExecutorTID, Environment.CurrentManagedThreadId);
                    }
                    else
                    {
                        Assert.Equal(ExecutorTID, Environment.CurrentManagedThreadId);
                    }
                    break;
                case ExecutorType.ThreadPool:
                    // it could migrate to any TP thread
                    Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                    Assert.True(Thread.CurrentThread.IsThreadPoolThread);
                    break;
            }
        }

        public void AssertBlockingWait(Exception? exception)
        {
            switch (Type)
            {
                case ExecutorType.Main:
                case ExecutorType.JSWebWorker:
                    Assert.NotNull(exception);
                    Assert.IsType<PlatformNotSupportedException>(exception);
                    break;
                case ExecutorType.NewThread:
                case ExecutorType.ThreadPool:
                    Assert.Null(exception);
                    break;
            }
        }

        public void AssertInteropThread()
        {
            switch (Type)
            {
                case ExecutorType.Main:
                    Assert.Equal(1, Environment.CurrentManagedThreadId);
                    Assert.Equal(ExecutorTID, Environment.CurrentManagedThreadId);
                    Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                    break;
                case ExecutorType.JSWebWorker:
                    Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                    Assert.Equal(ExecutorTID, Environment.CurrentManagedThreadId);
                    Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                    break;
                case ExecutorType.NewThread:
                    // it will synchronously continue on the UI thread
                    Assert.Equal(1, Environment.CurrentManagedThreadId);
                    Assert.NotEqual(ExecutorTID, Environment.CurrentManagedThreadId);
                    Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                    break;
                case ExecutorType.ThreadPool:
                    // it will synchronously continue on the UI thread
                    Assert.Equal(1, Environment.CurrentManagedThreadId);
                    Assert.NotEqual(ExecutorTID, Environment.CurrentManagedThreadId);
                    Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                    break;
            }
        }

        public override string ToString() => Type.ToString();

        // make sure we stay on the executor
        public async Task StickyAwait(Task task, CancellationToken cancellationToken)
        {
            if (Type == ExecutorType.NewThread)
            {
                task.Wait(cancellationToken);
            }
            else
            {
                await task.ConfigureAwait(true);
            }
            AssertTargetThread();
        }

        public static Task RunOnThreadPool(Func<Task> job, CancellationToken cancellationToken)
        {
            TaskCompletionSource done = new TaskCompletionSource();
            var reg = cancellationToken.Register(() =>
            {
                done.TrySetException(new OperationCanceledException(cancellationToken));
            });
            Task.Run(job, cancellationToken).ContinueWith(result =>
            {
                if (result.IsFaulted)
                {
                    if (result.Exception is AggregateException ag && ag.InnerException != null)
                    {
                        done.TrySetException(ag.InnerException);
                    }
                    else
                    {
                        done.TrySetException(result.Exception);
                    }
                }
                else if (result.IsCanceled)
                {
                    done.TrySetCanceled(cancellationToken);
                }
                else
                {
                    done.TrySetResult();
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return done.Task;
        }

        public static Task RunOnNewThread(Func<Task> job, CancellationToken cancellationToken)
        {
            if( Environment.CurrentManagedThreadId == 1)
            {
                throw new Exception("This unit test should be executed with -backgroundExec otherwise it's prone to consume all threads too quickly");
            }
            TaskCompletionSource tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() =>
            {
                CancellationTokenRegistration? reg = null;
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetException(new OperationCanceledException(cancellationToken));
                        return;
                    }
                    reg = cancellationToken.Register(() =>
                    {
                        tcs.TrySetException(new OperationCanceledException(cancellationToken));
                    });
                    var task = job();
                    task.Wait(cancellationToken);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    if(ex is AggregateException agg)
                    {
                        tcs.TrySetException(agg.InnerException);
                    }
                    else
                    {
                        tcs.TrySetException(ex);
                    }
                }
                finally
                {
                    reg?.Dispose();
                }
            });
            thread.Start();
            tcs.Task.ContinueWith((t) => { thread.Join(); }, cancellationToken, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
            return tcs.Task;
        }

        public static Task RunOnTargetAsync(SynchronizationContext ctx, Func<Task> job, CancellationToken cancellationToken)
        {
            TaskCompletionSource tcs = new TaskCompletionSource();
            ctx.Post(async _ =>
            {
                CancellationTokenRegistration? reg = null;
                try
                {
                    reg = cancellationToken.Register(() =>
                    {
                        tcs.TrySetException(new OperationCanceledException(cancellationToken));
                    });
                    await job().ConfigureAwait(true);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                finally
                {
                    reg?.Dispose();
                }
            }, null);
            return tcs.Task;
        }
    }

    #endregion
}
