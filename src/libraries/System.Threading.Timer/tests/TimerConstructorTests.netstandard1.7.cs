// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Xunit;

public partial class TimerConstructorTests
{
    [Fact]
    public void Timer_Constructor_CallbackOnly_Negative()
    {
        Assert.Throws<ArgumentNullException>(() => new Timer(null));
    }

    [Fact]
    public void Timer_Constructor_Int64_Negative()
    {
        Assert.Throws<ArgumentNullException>(() => new Timer(null, null, (long)-1, (long)-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(EmptyTimerTarget, null, (long)-2, (long)-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(EmptyTimerTarget, null, (long)-1, (long)-2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(EmptyTimerTarget, null, (long)0xffffffff, (long)-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(EmptyTimerTarget, null, (long)-1, (long)0xffffffff));
    }
}
