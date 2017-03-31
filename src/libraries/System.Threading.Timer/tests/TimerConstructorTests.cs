// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Xunit;

public partial class TimerConstructorTests
{
    private void EmptyTimerTarget(object o) { }

    [Fact]
    public void Timer_Constructor_NegativeTimeSpan_Period_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(new Action(() =>
        {
            using (var t = new Timer(new TimerCallback(EmptyTimerTarget)/* not relevant */, null /* not relevant */, new TimeSpan(1) /* not relevant */, TimeSpan.FromMilliseconds(-2))) { }
        }));
    }

    [Fact]
    public void Timer_Constructor_NegativeInt_Period_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(new Action(() =>
        {
            using (var t = new Timer(new TimerCallback(EmptyTimerTarget)/* not relevant */, null /* not relevant */, 1 /* not relevant */, -2)) { }
        }));
    }

    [Fact]
    public void Timer_Constructor_NegativeTimeSpan_DueTime_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(new Action(() =>
        {
            using (var t = new Timer(new TimerCallback(EmptyTimerTarget)/* not relevant */, null /* not relevant */, TimeSpan.FromMilliseconds(-2), new TimeSpan(1) /* not relevant */)) { }
        }));
    }

    [Fact]
    public void Timer_Constructor_NegativeInt_DueTime_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(new Action(() =>
        {
            using (var t = new Timer(new TimerCallback(EmptyTimerTarget)/* not relevant */, null /* not relevant */, -2, 1 /* not relevant */)) { }
        }));
    }

    [Fact]
    public void Timer_Constructor_TooLongTimeSpan_Period_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(new Action(() =>
        {
            using (var t = new Timer(new TimerCallback(EmptyTimerTarget)/* not relevant */, null /* not relevant */, new TimeSpan(1) /* not relevant */, TimeSpan.FromMilliseconds((long)0xFFFFFFFF))) { }
        }));
    }

    [Fact]
    public void Timer_Constructor_TooLongTimeSpan_DueTime_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(new Action(() =>
        {
            using (var t = new Timer(new TimerCallback(EmptyTimerTarget)/* not relevant */, null /* not relevant */, TimeSpan.FromMilliseconds((long)0xFFFFFFFF), new TimeSpan(1) /* not relevant */)) { }
        }));
    }

    [Fact]
    public void Timer_Constructor_Null_Callback_Throws()
    {
        Assert.Throws<ArgumentNullException>(new Action(() =>
        {
            using (var t = new Timer(null, null /* not relevant */, new TimeSpan(1) /* not relevant */, new TimeSpan(1) /* not relevant */)) { }
        }));
    }

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
