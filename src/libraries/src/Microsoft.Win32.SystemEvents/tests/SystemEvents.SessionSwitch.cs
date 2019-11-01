// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public class SessionSwitchTests : SystemEventsTest
    {
        private void SendMessage(SessionSwitchReason reason)
        {
            SendMessage(User32.WM_WTSSESSION_CHANGE, (IntPtr)reason, IntPtr.Zero);
        }

        public static IEnumerable<object[]> SessionSwitchReasons() => Enum.GetValues(typeof(SessionSwitchReason))
            .Cast<SessionSwitchReason>()
            .Select(x => new object[] { x });

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(SessionSwitchReasons))]
        public void SignalsSessionSwitch(SessionSwitchReason reason)
        {
            bool signaled = false;
            SessionSwitchEventArgs args = null;
            SessionSwitchEventHandler switchHandler = (o, e) =>
            {
                signaled = true;
                args = e;
            };

            SystemEvents.SessionSwitch += switchHandler;

            try
            {
                SendMessage(reason);
                Assert.True(signaled);
                Assert.NotNull(args);
                Assert.Equal(reason, args.Reason);
            }
            finally
            {
                SystemEvents.SessionSwitch -= switchHandler;
            }
        }
    }
}
