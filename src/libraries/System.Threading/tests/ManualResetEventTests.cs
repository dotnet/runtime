// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public class ManualResetEventTests
    {
        private const int FailedWaitTimeout = 30000;

        [Fact]
        public void Ctor()
        {
            using (ManualResetEvent mre = new ManualResetEvent(false))
                Assert.False(mre.WaitOne(0));

            using (ManualResetEvent mre = new ManualResetEvent(true))
                Assert.True(mre.WaitOne(0));
        }

        [Fact]
        public void SetReset()
        {
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                Assert.False(mre.WaitOne(0));
                mre.Set();
                Assert.True(mre.WaitOne(0));
                Assert.True(mre.WaitOne(0));
                mre.Set();
                Assert.True(mre.WaitOne(0));
                mre.Reset();
                Assert.False(mre.WaitOne(0));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49890", TestPlatforms.Android)]
        public void WaitHandleWaitAll()
        {
            ManualResetEvent[] handles = new ManualResetEvent[10];
            for (int i = 0; i < handles.Length; i++)
                handles[i] = new ManualResetEvent(false);

            Task<bool> t = Task.Run(() => WaitHandle.WaitAll(handles, FailedWaitTimeout));
            for (int i = 0; i < handles.Length; i++)
            {
                Assert.False(t.IsCompleted);
                handles[i].Set();
            }
            Assert.True(t.Result);

            Assert.True(Task.Run(() => WaitHandle.WaitAll(handles, 0)).Result); // Task.Run used to ensure MTA thread (necessary for desktop)
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49890", TestPlatforms.Android)]
        public void WaitHandleWaitAny()
        {
            ManualResetEvent[] handles = new ManualResetEvent[10];
            for (int i = 0; i < handles.Length; i++)
                handles[i] = new ManualResetEvent(false);

            Task<int> t = Task.Run(() => WaitHandle.WaitAny(handles, FailedWaitTimeout));
            handles[5].Set();
            Assert.Equal(5, t.Result);

            Assert.Equal(5, WaitHandle.WaitAny(handles, 0));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49890", TestPlatforms.Android)]
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
                            Assert.True(mre1.WaitOne(FailedWaitTimeout));
                            mre1.Reset();
                            mre2.Set();
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default),
                    Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < Iters; i++)
                        {
                            Assert.True(mre2.WaitOne(FailedWaitTimeout));
                            mre2.Reset();
                            mre1.Set();
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
            }
        }
    }
}
