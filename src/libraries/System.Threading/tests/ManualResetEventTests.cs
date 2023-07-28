// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public class ManualResetEventTests
    {
        [Fact]
        public void ConstructorAndDisposeTest()
        {
            var e = new ManualResetEvent(false);
            Assert.False(e.WaitOne(0));
            e.Dispose();
            Assert.Throws<ObjectDisposedException>(() => e.Reset());
            Assert.Throws<ObjectDisposedException>(() => e.Set());
            Assert.Throws<ObjectDisposedException>(() => e.WaitOne(0));

            e = new ManualResetEvent(true);
            Assert.True(e.WaitOne(0));
            e.Dispose();
        }

        [Fact]
        public void SetAndResetTest()
        {
            var e = new ManualResetEvent(true);
            e.Reset();
            Assert.False(e.WaitOne(0));
            Assert.False(e.WaitOne(0));
            e.Reset();
            Assert.False(e.WaitOne(0));
            e.Set();
            Assert.True(e.WaitOne(0));
            Assert.True(e.WaitOne(0));
            e.Set();
            Assert.True(e.WaitOne(0));
        }

        [Fact]
        public void WaitTest()
        {
            var e = new ManualResetEvent(true);
            e.CheckedWait();
            e.CheckedWait();

            e.Reset();
            Assert.False(e.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        }

        [Fact]
        public void MultiWaitWithAllIndexesSetTest()
        {
            var es =
                new ManualResetEvent[]
                {
                    new ManualResetEvent(true),
                    new ManualResetEvent(true),
                    new ManualResetEvent(true),
                    new ManualResetEvent(true)
                };
            Assert.Equal(0, WaitHandle.WaitAny(es, 0));
            Assert.Equal(0, WaitHandle.WaitAny(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            Assert.Equal(0, WaitHandle.WaitAny(es));
            Assert.True(WaitHandle.WaitAll(es, 0));
            Assert.True(WaitHandle.WaitAll(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            Assert.True(WaitHandle.WaitAll(es));
            for (int i = 0; i < es.Length; ++i)
            {
                Assert.True(es[i].WaitOne(0));
            }
        }

        [Fact]
        public void MultiWaitWithInnerIndexesSetTest()
        {
            var es =
                new ManualResetEvent[]
                {
                    new ManualResetEvent(false),
                    new ManualResetEvent(true),
                    new ManualResetEvent(true),
                    new ManualResetEvent(false)
                };
            Assert.Equal(1, WaitHandle.WaitAny(es, 0));
            Assert.Equal(1, WaitHandle.WaitAny(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            Assert.False(WaitHandle.WaitAll(es, 0));
            Assert.False(WaitHandle.WaitAll(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
            for (int i = 0; i < es.Length; ++i)
            {
                Assert.Equal(i == 1 || i == 2, es[i].WaitOne(0));
            }
        }

        [Fact]
        public void MultiWaitWithAllIndexesResetTest()
        {
            var es =
                new ManualResetEvent[]
                {
                    new ManualResetEvent(false),
                    new ManualResetEvent(false),
                    new ManualResetEvent(false),
                    new ManualResetEvent(false)
                };
            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(es, 0));
            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
            Assert.False(WaitHandle.WaitAll(es, 0));
            Assert.False(WaitHandle.WaitAll(es, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
            for (int i = 0; i < es.Length; ++i)
            {
                Assert.False(es[i].WaitOne(0));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void WaitHandleWaitAll()
        {
            ManualResetEvent[] handles = new ManualResetEvent[10];
            for (int i = 0; i < handles.Length; i++)
                handles[i] = new ManualResetEvent(false);

            Task<bool> t = Task.Run(() => WaitHandle.WaitAll(handles, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            for (int i = 0; i < handles.Length; i++)
            {
                Assert.False(t.IsCompleted);
                handles[i].Set();
            }
            Assert.True(t.Result);

            Assert.True(Task.Run(() => WaitHandle.WaitAll(handles, 0)).Result); // Task.Run used to ensure MTA thread (necessary for desktop)
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void WaitHandleWaitAny()
        {
            ManualResetEvent[] handles = new ManualResetEvent[10];
            for (int i = 0; i < handles.Length; i++)
                handles[i] = new ManualResetEvent(false);

            Task<int> t = Task.Run(() => WaitHandle.WaitAny(handles, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            handles[5].Set();
            Assert.Equal(5, t.Result);

            Assert.Equal(5, WaitHandle.WaitAny(handles, 0));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void PingPong()
        {
            using (ManualResetEvent mre1 = new ManualResetEvent(true), mre2 = new ManualResetEvent(false))
            {
                const int Iters = 10;
                Task.WaitAll(
                    Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < Iters; i++)
                        {
                            mre1.CheckedWait();
                            mre1.Reset();
                            mre2.Set();
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default),
                    Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < Iters; i++)
                        {
                            mre2.CheckedWait();
                            mre2.Reset();
                            mre1.Set();
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
            }
        }
    }
}
