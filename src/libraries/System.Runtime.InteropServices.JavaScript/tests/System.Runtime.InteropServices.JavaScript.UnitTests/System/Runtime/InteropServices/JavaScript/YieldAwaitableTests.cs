// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public class YieldAwaitableTests : IAsyncLifetime
    {
        [Fact]
        public async Task TaskYieldsToBrowserLoop()
        {
            JavaScriptTestHelper.BeforeYield();
            await Task.Yield();
            Assert.True(JavaScriptTestHelper.IsSetTimeoutHit());
            Assert.True(JavaScriptTestHelper.IsPromiseThenHit());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public async Task TaskDelay0DoesNotYieldToBrowserLoop()
        {
            JavaScriptTestHelper.BeforeYield();
            bool managedContinuationRan = false;
            async Task MarkManagedYield()
            {
                await Task.Yield();
                managedContinuationRan = true;
            }
            Task queued = MarkManagedYield();
            Task delay = Task.Delay(0);
            Assert.True(delay.IsCompletedSuccessfully);
            await delay;
            Assert.False(managedContinuationRan);
            // The UI event loop runs independently when managed code is on a worker.
            if (Environment.GetEnvironmentVariable("DOTNET_TEST_WITH_WORKER") != "1")
            {
                Assert.False(JavaScriptTestHelper.IsSetTimeoutHit());
                Assert.False(JavaScriptTestHelper.IsPromiseThenHit());
            }
            await queued;
            Assert.True(managedContinuationRan);
        }

        [Fact]
        public async Task TaskDelay1YieldsToBrowserLoop()
        {
            JavaScriptTestHelper.BeforeYield();
            await Task.Delay(1);
            Assert.True(JavaScriptTestHelper.IsSetTimeoutHit());
            Assert.True(JavaScriptTestHelper.IsPromiseThenHit());
        }

        public async Task InitializeAsync()
        {
            await JavaScriptTestHelper.InitializeAsync();
            await Task.Delay(100);
        }

        public async Task DisposeAsync()
        {
            await JavaScriptTestHelper.DisposeAsync();
        }
    }
}
