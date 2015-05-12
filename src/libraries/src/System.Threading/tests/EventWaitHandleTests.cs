// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

public class EventWaitHandleTests
{
    [Fact]
    public void Ctor()
    {
        using (EventWaitHandle are = new EventWaitHandle(false, EventResetMode.AutoReset))
            Assert.False(are.WaitOne(0));
        using (EventWaitHandle are = new EventWaitHandle(true, EventResetMode.AutoReset))
            Assert.True(are.WaitOne(0));
        using (EventWaitHandle mre = new EventWaitHandle(false, EventResetMode.ManualReset))
            Assert.False(mre.WaitOne(0));
        using (EventWaitHandle mre = new EventWaitHandle(true, EventResetMode.ManualReset))
            Assert.True(mre.WaitOne(0));

        Assert.Throws<ArgumentException>(() => new EventWaitHandle(true, (EventResetMode)12345));
        Assert.Throws<ArgumentException>(() => new EventWaitHandle(true, EventResetMode.AutoReset, new string('a', 1000)));

        const string Name = "EventWaitHandleTestCtor";

        using (EventWaitHandle are = new EventWaitHandle(false, EventResetMode.AutoReset, Name))
            Assert.False(are.WaitOne(0));
        using (EventWaitHandle are = new EventWaitHandle(true, EventResetMode.AutoReset, Name))
            Assert.True(are.WaitOne(0));
        using (EventWaitHandle mre = new EventWaitHandle(false, EventResetMode.ManualReset, Name))
            Assert.False(mre.WaitOne(0));
        using (EventWaitHandle mre = new EventWaitHandle(true, EventResetMode.ManualReset, Name))
            Assert.True(mre.WaitOne(0));
        
        bool createdNew;
        using (EventWaitHandle are = new EventWaitHandle(false, EventResetMode.AutoReset, Name, out createdNew))
        {
            Assert.True(createdNew);
            using (EventWaitHandle are2 = new EventWaitHandle(false, EventResetMode.AutoReset, Name, out createdNew))
            {
                Assert.False(createdNew);
            }
        }
        using (EventWaitHandle mre = new EventWaitHandle(false, EventResetMode.ManualReset, Name, out createdNew))
        {
            Assert.True(createdNew);
            using (EventWaitHandle mre2 = new EventWaitHandle(false, EventResetMode.ManualReset, Name, out createdNew))
            {
                Assert.False(createdNew);
            }
        }

        using (Mutex m = new Mutex(false, Name))
        {
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => new EventWaitHandle(false, EventResetMode.AutoReset, Name));
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => new EventWaitHandle(false, EventResetMode.ManualReset, Name));
        }
    }

    [Fact]
    public void SetReset()
    {
        using (EventWaitHandle are = new EventWaitHandle(false, EventResetMode.AutoReset))
        {
            Assert.False(are.WaitOne(0));
            are.Set();
            Assert.True(are.WaitOne(0));
            Assert.False(are.WaitOne(0));
            are.Set();
            are.Reset();
            Assert.False(are.WaitOne(0));
        }

        using (EventWaitHandle mre = new EventWaitHandle(false, EventResetMode.ManualReset))
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

    [Fact]
    public void OpenExisting()
    {
        const string Name = "EventWaitHandleTestOpenExisting";

        EventWaitHandle resultHandle;
        Assert.False(EventWaitHandle.TryOpenExisting(Name, out resultHandle));
        Assert.Null(resultHandle);

        using (EventWaitHandle are1 = new EventWaitHandle(false, EventResetMode.AutoReset, Name))
        {
            using (EventWaitHandle are2 = EventWaitHandle.OpenExisting(Name))
            {
                are1.Set();
                Assert.True(are2.WaitOne(0));
                Assert.False(are1.WaitOne(0));
                Assert.False(are2.WaitOne(0));

                are2.Set();
                Assert.True(are1.WaitOne(0));
                Assert.False(are2.WaitOne(0));
                Assert.False(are1.WaitOne(0));
            }

            Assert.True(EventWaitHandle.TryOpenExisting(Name, out resultHandle));
            Assert.NotNull(resultHandle);
            resultHandle.Dispose();
        }
    }

}
