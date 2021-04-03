// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public class AutoResetEventTests
    {
        private const int FailedWaitTimeout = 30000;

        [Fact]
        public void Ctor()
        {
            using (AutoResetEvent are = new AutoResetEvent(false))
                Assert.False(are.WaitOne(0));

            using (AutoResetEvent are = new AutoResetEvent(true))
                Assert.True(are.WaitOne(0));
        }

        [Fact]
        public void SetReset()
        {
            using (AutoResetEvent are = new AutoResetEvent(false))
            {
                Assert.False(are.WaitOne(0));
                are.Set();
                Assert.True(are.WaitOne(0));
                Assert.False(are.WaitOne(0));
                are.Set();
                are.Reset();
                Assert.False(are.WaitOne(0));
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49890", TestPlatforms.Android)]
        public void WaitHandleWaitAll()
        {
            AutoResetEvent[] handles = new AutoResetEvent[10];
            for (int i = 0; i < handles.Length; i++)
                handles[i] = new AutoResetEvent(false);

            Task<bool> t = Task.Run(() => WaitHandle.WaitAll(handles, FailedWaitTimeout));
            for (int i = 0; i < handles.Length; i++)
            {
                Assert.False(t.IsCompleted);
                handles[i].Set();
            }
            Assert.True(t.Result);

            Assert.False(Task.Run(() => WaitHandle.WaitAll(handles, 0)).Result); // Task.Run used to ensure MTA thread (necessary for desktop)
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49890", TestPlatforms.Android)]
        public void WaitHandleWaitAny()
        {
            AutoResetEvent[] handles = new AutoResetEvent[10];
            for (int i = 0; i < handles.Length; i++)
                handles[i] = new AutoResetEvent(false);

            Task<int> t = Task.Run(() => WaitHandle.WaitAny(handles, FailedWaitTimeout));
            handles[5].Set();
            Assert.Equal(5, t.Result);

            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(handles, 0));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49890", TestPlatforms.Android)]
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
                            Assert.True(are1.WaitOne(FailedWaitTimeout));
                            are2.Set();
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default),
                    Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < Iters; i++)
                        {
                            Assert.True(are2.WaitOne(FailedWaitTimeout));
                            are1.Set();
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
            }
        }
    }
}
