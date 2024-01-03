﻿// Licensed to the .NET Foundation under one or more agreements.
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
        public static partial Task Delay(int ms);

        [JSImport("getTid", "WebWorkerTestHelper")]
        public static partial int GetTid();

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

        #region Execute

        public async static Task RunOnNewThread(Func<Task> job)
        {
            TaskCompletionSource tcs = new TaskCompletionSource();
            var t = new Thread(() =>
            {
                try
                {
                    var task = job();
                    task.Wait();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            t.Start();
            await tcs.Task;
            t.Join();
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:UnrecognizedReflectionPattern")]
        public async static Task RunOnMainAsync(Func<Task> job)
        {
            if (MainSynchronizationContext == null)
            {
                var jsProxyContext = typeof(JSObject).Assembly.GetType("System.Runtime.InteropServices.JavaScript.JSProxyContext");
                var mainThreadContext = jsProxyContext.GetField("_MainThreadContext", BindingFlags.NonPublic | BindingFlags.Static);
                var synchronizationContext = jsProxyContext.GetField("SynchronizationContext", BindingFlags.Public | BindingFlags.Instance);
                var mainCtx = mainThreadContext.GetValue(null);
                MainSynchronizationContext = (SynchronizationContext)synchronizationContext.GetValue(mainCtx);
            }
            await RunOnTargetAsync(MainSynchronizationContext, job);
        }

        public static Task RunOnTargetAsync(SynchronizationContext ctx, Func<Task> job)
        {
            TaskCompletionSource tcs = new TaskCompletionSource();
            ctx.Post(async _ =>
            {
                await InitializeAsync();
                try
                {
                    await job().ConfigureAwait(true);
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                finally
                {
                    await DisposeAsync();
                }
            }, null);
            return tcs.Task;
        }

        #endregion

        #region Setup

        [ThreadStatic]
        public static JSObject WebWorkerTestHelperModule;
        [ThreadStatic]
        public static JSObject InlineTestHelperModule;

        public static SynchronizationContext MainSynchronizationContext;

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
                await Delay(1).ConfigureAwait(false);
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

    #region

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

        public ExecutorType Type;

        public Executor(ExecutorType type)
        {
            Type = type;
        }

        public Task Execute(Func<Task> job)
        {
            Task wrapExecute()
            {
                ExecutorTID = Environment.CurrentManagedThreadId;
                return job();
            }

            switch (Type)
            {
                case ExecutorType.Main:
                    return WebWorkerTestHelper.RunOnMainAsync(wrapExecute);
                case ExecutorType.ThreadPool:
                    return Task.Run(wrapExecute);
                case ExecutorType.NewThread:
                    return WebWorkerTestHelper.RunOnNewThread(wrapExecute);
                case ExecutorType.JSWebWorker:
                    return JSWebWorker.RunAsync(wrapExecute);
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
                Assert.True(Thread.CurrentThread.IsThreadPoolThread);
            }
            else
            {
                Assert.False(Thread.CurrentThread.IsThreadPoolThread);
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
                    // the actual new thread is now blocked in .Wait() and so this is running on TP
                    Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                    Assert.NotEqual(ExecutorTID, Environment.CurrentManagedThreadId);
                    Assert.True(Thread.CurrentThread.IsThreadPoolThread);
                    break;
                case ExecutorType.ThreadPool:
                    // it could migrate to any TP thread
                    Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                    Assert.True(Thread.CurrentThread.IsThreadPoolThread);
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
        public async Task StickyAwait(Task task)
        {
            if (Type == ExecutorType.NewThread)
            {
                task.Wait();
            }
            else
            {
                await task.ConfigureAwait(true);
            }
            AssertTargetThread();
        }
    }

    #endregion
}
