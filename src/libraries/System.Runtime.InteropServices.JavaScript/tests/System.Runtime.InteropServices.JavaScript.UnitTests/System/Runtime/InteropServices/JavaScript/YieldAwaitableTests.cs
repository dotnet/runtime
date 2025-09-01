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

        [Fact]
        public async Task TaskDelay0DoesNotYieldToBrowserLoop()
        {
            JavaScriptTestHelper.BeforeYield();
            await Task.Delay(0);
            Assert.False(JavaScriptTestHelper.IsSetTimeoutHit());
            Assert.False(JavaScriptTestHelper.IsPromiseThenHit());
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