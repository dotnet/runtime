// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Xunit;

public partial class TimerChangeTests
{
    [Fact]
    public void Timer_Change_Int64_Negative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(EmptyTimerTarget).Change((long)-2, (long)-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(EmptyTimerTarget).Change((long)-1, (long)-2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(EmptyTimerTarget).Change((long)0xffffffff, (long)-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(EmptyTimerTarget).Change((long)-1, (long)0xffffffff));
    }

    [Fact]
    public void Timer_Change_UInt32_Int64_AfterDispose_Throws()
    {
        var t = new Timer(EmptyTimerTarget);
        t.Dispose();
        Assert.Throws<ObjectDisposedException>(() => t.Change(0u, 0u));
        Assert.Throws<ObjectDisposedException>(() => t.Change(0L, 0L));
    }
}
