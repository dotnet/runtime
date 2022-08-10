// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public class AutoResetEventTests
    {
        [Fact]
        public void ConstructorAndDisposeTest()
        {
            var e = new AutoResetEvent(false);
            Assert.False(e.WaitOne(0));
            e.Dispose();
            Assert.Throws<ObjectDisposedException>(() => e.Reset());
            Assert.Throws<ObjectDisposedException>(() => e.Set());
            Assert.Throws<ObjectDisposedException>(() => e.WaitOne(0));

            e = new AutoResetEvent(true);
            Assert.True(e.WaitOne(0));
            e.Dispose();
        }

        [Fact]
        public void SetAndResetTest()
        {
            var e = new AutoResetEvent(true);
            e.Reset();
            Assert.False(e.WaitOne(0));
            Assert.False(e.WaitOne(0));
            e.Reset();
            Assert.False(e.WaitOne(0));
            e.Set();
            Assert.True(e.WaitOne(0));
            Assert.False(e.WaitOne(0));
            e.Set();
            e.Set();
            Assert.True(e.WaitOne(0));
        }

        [Fact]
        public void WaitTest()
        {
            var e = new AutoResetEvent(true);
            e.CheckedWait();
            Assert.False(e.WaitOne(0));
            e.Set();
            e.CheckedWait();
            Assert.False(e.WaitOne(0));

            e.Reset();
            Assert.False(e.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        }

        [Fact]
        public void MultiWaitWithAllIndexesSetTest()
        {
            var es =
                new AutoResetEvent[]
                {
                    new AutoResetEvent(true),
                    new AutoResetEvent(true),
                    new AutoResetEvent(true),
                    new AutoResetEvent(true)
                };
            Assert.Equal(0, WaitHandle.WaitAny(es, 0));
            for (int i = 0; i < es.Length; ++i)
            {
                Assert.Equal(i > 0, es[i].WaitOne(0));
                es[i].Set();
            }
            Assert.Equal(0, WaitHandle.WaitAny(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            for (int i = 0; i < es.Length; ++i)
            {
                Assert.Equal(i > 0, es[i].WaitOne(0));
                es[i].Set();
            }
            Assert.Equal(0, WaitHandle.WaitAny(es));
            for (int i = 0; i < es.Length; ++i)
            {
                Assert.Equal(i > 0, es[i].WaitOne(0));
                es[i].Set();
            }
            Assert.True(WaitHandle.WaitAll(es, 0));
            for (int i = 0; i < es.Length; ++i)
            {
                Assert.False(es[i].WaitOne(0));
                es[i].Set();
            }
            Assert.True(WaitHandle.WaitAll(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            for (int i = 0; i < es.Length; ++i)
            {
                Assert.False(es[i].WaitOne(0));
                es[i].Set();
            }
            Assert.True(WaitHandle.WaitAll(es));
            for (int i = 0; i < es.Length; ++i)
            {
                Assert.False(es[i].WaitOne(0));
            }
        }

        [Fact]
        public void MultiWaitWithInnerIndexesSetTest()
        {
            var es =
                new AutoResetEvent[]
                {
                    new AutoResetEvent(false),
                    new AutoResetEvent(true),
                    new AutoResetEvent(true),
                    new AutoResetEvent(false)
                };
            Assert.Equal(1, WaitHandle.WaitAny(es, 0));
            for (int i = 0; i < es.Length; ++i)
            {
                Assert.Equal(i == 2, es[i].WaitOne(0));
            }
            es[1].Set();
            es[2].Set();
            Assert.Equal(1, WaitHandle.WaitAny(es, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            for (int i = 0; i < es.Length; ++i)
            {
                Assert.Equal(i == 2, es[i].WaitOne(0));
            }
            es[1].Set();
            es[2].Set();
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
                new AutoResetEvent[]
                {
                    new AutoResetEvent(false),
                    new AutoResetEvent(false),
                    new AutoResetEvent(false),
                    new AutoResetEvent(false)
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

        [Fact]
        public void WaitHandleWaitAll_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => WaitHandle.WaitAll(null));
            Assert.Throws<ArgumentNullException>(() => WaitHandle.WaitAll(null, 100));
            Assert.Throws<ArgumentNullException>(() => WaitHandle.WaitAll(null, TimeSpan.Zero));
        }

        [Fact]
        public void WaitHandleWaitAny_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => WaitHandle.WaitAny(null));
            Assert.Throws<ArgumentNullException>(() => WaitHandle.WaitAny(null, 100));
            Assert.Throws<ArgumentNullException>(() => WaitHandle.WaitAny(null, TimeSpan.Zero));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void WaitHandleWaitAll()
        {
            AutoResetEvent[] handles = new AutoResetEvent[10];
            for (int i = 0; i < handles.Length; i++)
                handles[i] = new AutoResetEvent(false);

            Task<bool> t = Task.Run(() => WaitHandle.WaitAll(handles, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            for (int i = 0; i < handles.Length; i++)
            {
                Assert.False(t.IsCompleted);
                handles[i].Set();
            }
            Assert.True(t.Result);

            Assert.False(Task.Run(() => WaitHandle.WaitAll(handles, 0)).Result); // Task.Run used to ensure MTA thread (necessary for desktop)
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void WaitHandleWaitAny()
        {
            AutoResetEvent[] handles = new AutoResetEvent[10];
            for (int i = 0; i < handles.Length; i++)
                handles[i] = new AutoResetEvent(false);

            Task<int> t = Task.Run(() => WaitHandle.WaitAny(handles, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            handles[5].Set();
            Assert.Equal(5, t.Result);

            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(handles, 0));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void PingPong()
        {
            using (AutoResetEvent are1 = new AutoResetEvent(true), are2 = new AutoResetEvent(false))
            {
                const int Iters = 10;
                Task.WaitAll(
                    Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < Iters; i++)
                        {
                            are1.CheckedWait();
                            are2.Set();
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default),
                    Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < Iters; i++)
                        {
                            are2.CheckedWait();
                            are1.Set();
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
            }
        }
    }
}
