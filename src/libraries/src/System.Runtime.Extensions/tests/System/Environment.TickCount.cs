// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace System.Runtime.Extensions.Tests
{
    public class EnvironmentTickCount
    {
        [Fact]
        public void TickCountTest()
        {
            int start = Environment.TickCount;
            Assert.True(SpinWait.SpinUntil(() => Environment.TickCount > start, TimeSpan.FromSeconds(1)));
        }
    }
}
