// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public static partial class MonitorTests
    {
        [Fact]
        public static void Enter_HasToWait_LockContentionCountTest()
        {
            long initialLockContentionCount = Monitor.LockContentionCount;
            Enter_HasToWait();
            Assert.True(Monitor.LockContentionCount - initialLockContentionCount >= 2);
        }
    }
}
