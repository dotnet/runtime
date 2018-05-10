// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public class InvokeOnEventsThreadTests : SystemEventsTest
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void InvokeOnEventsThreadRunsAsynchronously()
        {
            var invoked = new AutoResetEvent(false);
            SystemEvents.InvokeOnEventsThread(new Action(() => invoked.Set()));
            Assert.True(invoked.WaitOne(PostMessageWait * SystemEventsTest.ExpectedEventMultiplier));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void InvokeOnEventsThreadRunsOnSameThreadAsOtherEvents()
        {
            int expectedThreadId = -1, actualThreadId = -1;
            var invoked = new AutoResetEvent(false);
            EventHandler handler = (sender, args) =>
            {
                expectedThreadId = Thread.CurrentThread.ManagedThreadId;
            };

            try
            {   
                // force a TimeChanged event to get the event thread ID
                SystemEvents.TimeChanged += handler;
                SendMessage(User32.WM_REFLECT + User32.WM_TIMECHANGE, IntPtr.Zero, IntPtr.Zero);
                Assert.NotEqual(-1, expectedThreadId);

                SystemEvents.InvokeOnEventsThread(new Action(() =>
                {
                    actualThreadId = Thread.CurrentThread.ManagedThreadId;
                    invoked.Set();
                }));
                Assert.True(invoked.WaitOne(PostMessageWait * SystemEventsTest.ExpectedEventMultiplier));
                Assert.Equal(expectedThreadId, actualThreadId);
            }
            finally
            {
                SystemEvents.TimeChanged -= handler;
                invoked.Dispose();
            }
        }
    }
}
