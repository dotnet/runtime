// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

public class MutexTests
{
    [Fact]
    public void Ctor()
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

        Assert.Throws<ArgumentException>(() => new Mutex(false, new string('a', 1000)));

        const string Name = "MutexTestsCtor";
        bool createdNew;
        using (Mutex m1 = new Mutex(false, Name, out createdNew))
        {
            Assert.True(createdNew);
            using (Mutex m2 = new Mutex(false, Name, out createdNew))
            {
                Assert.False(createdNew);
            }
        }

        using (Semaphore s = new Semaphore(1, 1, Name))
        {
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => new Mutex(false, Name));
        }
    }

    [Fact]
    public void OpenExisting()
    {
        const string Name = "MutexTestsOpenExisting";

        Mutex resultHandle;
        Assert.False(Mutex.TryOpenExisting(Name, out resultHandle));

        using (Mutex m1 = new Mutex(false, Name))
        {
            using (Mutex m2 = Mutex.OpenExisting(Name))
            {
                Assert.True(m1.WaitOne());
                Assert.False(Task.Run(() => m2.WaitOne(0)).Result);
                m1.ReleaseMutex();

                Assert.True(m2.WaitOne());
                Assert.False(Task.Run(() => m1.WaitOne(0)).Result);
                m2.ReleaseMutex();
            }

            Assert.True(Mutex.TryOpenExisting(Name, out resultHandle));
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