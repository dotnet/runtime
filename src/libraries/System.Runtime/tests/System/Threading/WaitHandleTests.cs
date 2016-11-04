// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Xunit;

namespace System.Threading.Tests
{
    public static partial class WaitHandleTests
    {
        [Fact]
        public static void WaitOne()
        {
            ManualResetEvent h = new ManualResetEvent(true);

            Assert.True(h.WaitOne());
            Assert.True(h.WaitOne(1));
            Assert.True(h.WaitOne(TimeSpan.FromMilliseconds(1)));

            h.Reset();

            Assert.False(h.WaitOne(1));
            Assert.False(h.WaitOne(TimeSpan.FromMilliseconds(1)));
        }

        [Fact]
        public static void WaitAny()
        {
            var handles = new ManualResetEvent[] {
            new ManualResetEvent(false),
            new ManualResetEvent(false),
            new ManualResetEvent(true)
        };

            Assert.Equal(2, WaitHandle.WaitAny(handles));
            Assert.Equal(2, WaitHandle.WaitAny(handles, 1));
            Assert.Equal(2, WaitHandle.WaitAny(handles, TimeSpan.FromMilliseconds(1)));

            handles[2].Reset();

            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(handles, 1));
            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(handles, TimeSpan.FromMilliseconds(1)));
        }

        [Fact]
        public static void WaitAny_SameHandles()
        {
            ManualResetEvent[] wh = new ManualResetEvent[2];
            wh[0] = new ManualResetEvent(true);
            wh[1] = wh[0];

            Assert.Equal(0, WaitHandle.WaitAny(wh));
        }

        [Fact]
        public static void WaitAll()
        {
            var handles = new ManualResetEvent[] {
            new ManualResetEvent(true),
            new ManualResetEvent(true),
            new ManualResetEvent(true)
        };

            Assert.True(WaitHandle.WaitAll(handles));
            Assert.True(WaitHandle.WaitAll(handles, 1));
            Assert.True(WaitHandle.WaitAll(handles, TimeSpan.FromMilliseconds(1)));

            handles[2].Reset();

            Assert.False(WaitHandle.WaitAll(handles, 1));
            Assert.False(WaitHandle.WaitAll(handles, TimeSpan.FromMilliseconds(1)));
        }

        [Fact]
        public static void WaitAll_SameHandles()
        {
            ManualResetEvent[] wh = new ManualResetEvent[2];
            wh[0] = new ManualResetEvent(true);
            wh[1] = wh[0];

            Assert.ThrowsAny<ArgumentException>(() => WaitHandle.WaitAll(wh));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // names aren't supported on Unix
        public static void WaitAll_SameNames()
        {
            Mutex[] wh = new Mutex[2];
            wh[0] = new Mutex(false, "test");
            wh[1] = new Mutex(false, "test");

            Assert.Throws<ArgumentException>(() => WaitHandle.WaitAll(wh));
        }

        [Fact]
        public static void WaitTimeout()
        {
            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(new[] { new ManualResetEvent(false) }, 0));
        }

        [Fact]
        public static void Dispose()
        {
            var wh = new ManualResetEvent(false);
            wh.Dispose();
            Assert.Throws<ObjectDisposedException>(() => wh.WaitOne(0));
        }

        [Fact]
        public static void DisposeVirtual_ThroughDispose()
        {
            var wh = new TestWaitHandle();
            wh.Dispose();
            Assert.True(wh.WasExplicitlyDisposed);
        }

        private partial class TestWaitHandle : WaitHandle
        {
            public bool WasExplicitlyDisposed { get; private set; } = false;

            protected override void Dispose(bool explicitDisposing)
            {
                if (explicitDisposing)
                {
                    WasExplicitlyDisposed = true;
                }
                base.Dispose(explicitDisposing);
            }
        }
    }
}
