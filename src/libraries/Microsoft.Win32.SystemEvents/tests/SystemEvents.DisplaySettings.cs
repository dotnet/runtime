// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Xunit;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public class DisplaySettingsTests : SystemEventsTest
    {
        private void SendMessage()
        {
            SendMessage(User32.WM_DISPLAYCHANGE, IntPtr.Zero, IntPtr.Zero);
        }
        private void SendReflectedMessage()
        {
            SendMessage(User32.WM_REFLECT + User32.WM_DISPLAYCHANGE, IntPtr.Zero, IntPtr.Zero);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void SignalsDisplayEventsAsynchronouslyOnDISPLAYCHANGE()
        {
            var changing = new AutoResetEvent(false);
            var changed = new AutoResetEvent(false);
            EventHandler changedHandler = (o, e) => changed.Set();
            EventHandler changingHandler = (o, e) => changing.Set();

            SystemEvents.DisplaySettingsChanged += changedHandler;
            SystemEvents.DisplaySettingsChanging += changingHandler;

            try
            {
                SendMessage();
                Assert.True(changing.WaitOne(PostMessageWait));
                Assert.True(changed.WaitOne(PostMessageWait));
            }
            finally
            {
                SystemEvents.DisplaySettingsChanged -= changedHandler;
                SystemEvents.DisplaySettingsChanging -= changingHandler;
                changing.Dispose();
                changed.Dispose();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void SignalsDisplayEventsSynchronouslyOnREFLECTDISPLAYCHANGE()
        {
            bool changing = false, changed = false;
            EventHandler changedHandler = (o, e) => changed = true;
            EventHandler changingHandler = (o, e) => changing = true;

            SystemEvents.DisplaySettingsChanged += changedHandler;
            SystemEvents.DisplaySettingsChanging += changingHandler;

            try
            {
                SendReflectedMessage();
                Assert.True(changing);
                Assert.True(changed);
            }
            finally
            {
                SystemEvents.DisplaySettingsChanged -= changedHandler;
                SystemEvents.DisplaySettingsChanging -= changingHandler;
            }
        }
    }
}
