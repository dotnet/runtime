// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public class PowerModeTests : SystemEventsTest
    {
        private void SendMessage(int pmEvent)
        {
            SendMessage(User32.WM_POWERBROADCAST, (IntPtr)pmEvent, IntPtr.Zero);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(User32.PBT_APMBATTERYLOW, PowerModes.StatusChange)]
        [InlineData(User32.PBT_APMOEMEVENT, PowerModes.StatusChange)]
        [InlineData(User32.PBT_APMPOWERSTATUSCHANGE, PowerModes.StatusChange)]
        [InlineData(User32.PBT_APMRESUMECRITICAL, PowerModes.Resume)]
        [InlineData(User32.PBT_APMRESUMESUSPEND, PowerModes.Resume)]
        [InlineData(User32.PBT_APMRESUMESTANDBY, PowerModes.Resume)]
        [InlineData(User32.PBT_APMSUSPEND, PowerModes.Suspend)]
        [InlineData(User32.PBT_APMSTANDBY, PowerModes.Suspend)]
        public void SignalsPowerModeChanged(int pmEvent, PowerModes powerMode)
        {
            bool changed = false;
            PowerModeChangedEventArgs args = null;
            PowerModeChangedEventHandler changedHandler = (o, e) =>
            {
                changed = true;
                args = e;
            };

            SystemEvents.PowerModeChanged += changedHandler;

            try
            {
                SendMessage(pmEvent);
                Assert.True(changed);
                Assert.NotNull(args);
                Assert.Equal(powerMode, args.Mode);
            }
            finally
            {
                SystemEvents.PowerModeChanged -= changedHandler;
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(User32.PBT_APMQUERYSTANDBY)]
        [InlineData(User32.PBT_APMQUERYSTANDBYFAILED)]
        [InlineData(User32.PBT_APMQUERYSUSPEND)]
        [InlineData(User32.PBT_APMQUERYSUSPENDFAILED)]
        [InlineData(int.MaxValue)]
        [InlineData(-1)]
        public void DoesNotSignalPowerModeChanged(int pmEvent)
        {
            bool changed = false;
            PowerModeChangedEventHandler changedHandler = (o, e) =>
            {
                changed = true;
            };

            SystemEvents.PowerModeChanged += changedHandler;

            try
            {
                SendMessage(pmEvent);
                Assert.False(changed);
            }
            finally
            {
                SystemEvents.PowerModeChanged -= changedHandler;
            }
        }
    }
}
