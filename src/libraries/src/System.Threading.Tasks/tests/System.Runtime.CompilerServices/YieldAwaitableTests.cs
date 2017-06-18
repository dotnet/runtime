// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Tests
{
    public class YieldAwaitableTests
    {
        // awaiting Task.Yield
        [Fact]
        public static void RunAsyncYieldAwaiterTests()
        {
            // Test direct usage works even though it's not encouraged
            {
                for (int i = 0; i < 2; i++)
                {
                    SynchronizationContext.SetSynchronizationContext(new ValidateCorrectContextSynchronizationContext());
                    var ya = i == 0 ? new YieldAwaitable.YieldAwaiter() : new YieldAwaitable().GetAwaiter();
                    var mres = new ManualResetEventSlim();
                    Assert.False(ya.IsCompleted, "RunAsyncYieldAwaiterTests     > FAILURE. YieldAwaiter.IsCompleted should always be false.");
                    ya.OnCompleted(() =>
                    {
                        Assert.True(ValidateCorrectContextSynchronizationContext.t_isPostedInContext, "RunAsyncYieldAwaiterTests     > FAILURE. Expected to post in target context.");
                        mres.Set();
                    });
                    mres.Wait();
                    ya.GetResult();
                    SynchronizationContext.SetSynchronizationContext(null);
                }
            }

            {
                // Yield when there's a current sync context
                SynchronizationContext.SetSynchronizationContext(new ValidateCorrectContextSynchronizationContext());
                var ya = Task.Yield().GetAwaiter();
                try { ya.GetResult(); }
                catch
                {
                    Assert.True(false, string.Format("RunAsyncYieldAwaiterTests     > FAILURE. YieldAwaiter.GetResult threw inappropriately"));
                }
                var mres = new ManualResetEventSlim();
                Assert.False(ya.IsCompleted, "RunAsyncYieldAwaiterTests     > FAILURE. YieldAwaiter.IsCompleted should always be false.");
                ya.OnCompleted(() =>
                {
                    Assert.True(ValidateCorrectContextSynchronizationContext.t_isPostedInContext, "     > FAILURE. Expected to post in target context.");
                    mres.Set();
                });
                mres.Wait();
                ya.GetResult();
                SynchronizationContext.SetSynchronizationContext(null);
            }

            {
                // Yield when there's a current TaskScheduler
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var ya = Task.Yield().GetAwaiter();
                        try { ya.GetResult(); }
                        catch
                        {
                            Assert.True(false, string.Format("     > FAILURE. YieldAwaiter.GetResult threw inappropriately"));
                        }
                        var mres = new ManualResetEventSlim();
                        Assert.False(ya.IsCompleted, "     > FAILURE. YieldAwaiter.IsCompleted should always be false.");
                        ya.OnCompleted(() =>
                        {
                            Assert.True(TaskScheduler.Current is QUWITaskScheduler, "     > FAILURE. Expected to queue into target scheduler.");
                            mres.Set();
                        });
                        mres.Wait();
                        ya.GetResult();
                    }
                    catch { Assert.True(false, string.Format("     > FAILURE. Unexpected exception from Yield")); }
                }, CancellationToken.None, TaskCreationOptions.None, new QUWITaskScheduler()).Wait();
            }

            {
                // Yield when there's a current TaskScheduler and SynchronizationContext.Current is the base SynchronizationContext
                Task.Factory.StartNew(() =>
                {
                    SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                    try
                    {
                        var ya = Task.Yield().GetAwaiter();
                        try { ya.GetResult(); }
                        catch
                        {
                            Assert.True(false, string.Format("     > FAILURE. YieldAwaiter.GetResult threw inappropriately"));
                        }
                        var mres = new ManualResetEventSlim();
                        Assert.False(ya.IsCompleted, "     > FAILURE. YieldAwaiter.IsCompleted should always be false.");
                        ya.OnCompleted(() =>
                        {
                            Assert.True(TaskScheduler.Current is QUWITaskScheduler, "     > FAILURE. Expected to queue into target scheduler.");
                            mres.Set();
                        });
                        mres.Wait();
                        ya.GetResult();
                    }
                    catch { Assert.True(false, string.Format("     > FAILURE. Unexpected exception from Yield")); }
                    SynchronizationContext.SetSynchronizationContext(null);
                }, CancellationToken.None, TaskCreationOptions.None, new QUWITaskScheduler()).Wait();
            }

            {
                // OnCompleted grabs the current context, not Task.Yield nor GetAwaiter
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                var ya = Task.Yield().GetAwaiter();
                SynchronizationContext.SetSynchronizationContext(new ValidateCorrectContextSynchronizationContext());
                try { ya.GetResult(); }
                catch
                {
                    Assert.True(false, string.Format("     > FAILURE. YieldAwaiter.GetResult threw inappropriately"));
                }
                var mres = new ManualResetEventSlim();
                Assert.False(ya.IsCompleted, "     > FAILURE. YieldAwaiter.IsCompleted should always be false.");
                ya.OnCompleted(() =>
                {
                    Assert.True(ValidateCorrectContextSynchronizationContext.t_isPostedInContext, "     > FAILURE. Expected to post in target context.");
                    mres.Set();
                });
                mres.Wait();
                ya.GetResult();
                SynchronizationContext.SetSynchronizationContext(null);
            }
        }

        // awaiting Task.Yield
        [Fact]
        public static void RunAsyncYieldAwaiterTests_Negative()
        {
            // Yield when there's a current sync context
            SynchronizationContext.SetSynchronizationContext(new ValidateCorrectContextSynchronizationContext());
            var ya = Task.Yield().GetAwaiter();
            Assert.Throws<ArgumentNullException>(() => { ya.OnCompleted(null); });
        }

        #region Helper Methods / Classes

        private class ValidateCorrectContextSynchronizationContext : SynchronizationContext
        {
            [ThreadStatic]
            internal static bool t_isPostedInContext;

            internal int PostCount;
            internal int SendCount;

            public override void Post(SendOrPostCallback d, object state)
            {
                Interlocked.Increment(ref PostCount);
                Task.Run(() =>
                {
                    t_isPostedInContext = true;
                    d(state);
                    t_isPostedInContext = false;
                });
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                Interlocked.Increment(ref SendCount);
                d(state);
            }
        }

        /// <summary>A scheduler that queues to the TP and tracks the number of times QueueTask and TryExecuteTaskInline are invoked.</summary>
        private class QUWITaskScheduler : TaskScheduler
        {
            private int _queueTaskCount;
            private int _tryExecuteTaskInlineCount;

            public int QueueTaskCount { get { return _queueTaskCount; } }
            public int TryExecuteTaskInlineCount { get { return _tryExecuteTaskInlineCount; } }

            protected override IEnumerable<Task> GetScheduledTasks() { return null; }

            protected override void QueueTask(Task task)
            {
                Interlocked.Increment(ref _queueTaskCount);
                Task.Run(() => TryExecuteTask(task));
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                Interlocked.Increment(ref _tryExecuteTaskInlineCount);
                return TryExecuteTask(task);
            }
        }

        #endregion
    }
}
