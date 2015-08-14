// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

public class MutexTests
{
    [Fact]
    public void Ctor_ConstructWaitRelease()
    {
        using (Mutex m = new Mutex())
        {
            Assert.True(m.WaitOne());
            m.ReleaseMutex();
        }

        using (Mutex m = new Mutex(false))
        {
            Assert.True(m.WaitOne());
            m.ReleaseMutex();
        }

        using (Mutex m = new Mutex(true))
        {
            Assert.True(m.WaitOne());
            m.ReleaseMutex();
            m.ReleaseMutex();
        }
    }

    [PlatformSpecific(PlatformID.AnyUnix)]
    [Fact]
    public void Ctor_NamesNotSupported_Unix()
    {
        Assert.Throws<PlatformNotSupportedException>(() => new Mutex(false, "anyname"));
    }

    [PlatformSpecific(PlatformID.Windows)]
    [Fact]
    public void Ctor_InvalidName()
    {
        Assert.Throws<ArgumentException>(() => new Mutex(false, new string('a', 1000)));
    }

    [PlatformSpecific(PlatformID.Windows)]
    [Fact]
    public void Ctor_ValidName_Windows()
    {
        string name = Guid.NewGuid().ToString("N");
        bool createdNew;
        using (Mutex m1 = new Mutex(false, name, out createdNew))
        {
            Assert.True(createdNew);
            using (Mutex m2 = new Mutex(false, name, out createdNew))
            {
                Assert.False(createdNew);
            }
        }
    }

    [PlatformSpecific(PlatformID.Windows)]
    [Fact]
    public void Ctor_NameUsedByOtherSynchronizationPrimitive_Windows()
    {
        string name = Guid.NewGuid().ToString("N");
        using (Semaphore s = new Semaphore(1, 1, name))
        {
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => new Mutex(false, name));
        }
    }

    [PlatformSpecific(PlatformID.Windows)]
    [Fact]
    public void OpenExisting_Windows()
    {
        string name = Guid.NewGuid().ToString("N");

        Mutex resultHandle;
        Assert.False(Mutex.TryOpenExisting(name, out resultHandle));

        using (Mutex m1 = new Mutex(false, name))
        {
            using (Mutex m2 = Mutex.OpenExisting(name))
            {
                Assert.True(m1.WaitOne());
                Assert.False(Task.Factory.StartNew(() => m2.WaitOne(0), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Result);
                m1.ReleaseMutex();

                Assert.True(m2.WaitOne());
                Assert.False(Task.Factory.StartNew(() => m1.WaitOne(0), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Result);
                m2.ReleaseMutex();
            }

            Assert.True(Mutex.TryOpenExisting(name, out resultHandle));
            Assert.NotNull(resultHandle);
            resultHandle.Dispose();
        }
    }

    [Fact]
    public void AbandonExisting()
    {
        using (Mutex m = new Mutex())
        {
            Task.Factory.StartNew(() => m.WaitOne(), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Wait();
            Assert.Throws<AbandonedMutexException>(() => m.WaitOne());
        }

        using (Mutex m = new Mutex())
        {
            Task.Factory.StartNew(() => m.WaitOne(), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Wait();
            AbandonedMutexException ame = Assert.Throws<AbandonedMutexException>(() => WaitHandle.WaitAny(new[] { m }));
            Assert.Equal(0, ame.MutexIndex);
        }
    }

}