// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public class SessionEndedTests : SystemEventsTest
    {
        private IntPtr SendMessage(bool ended, int lParam)
        {
            return SendMessage(User32.WM_ENDSESSION, ended ? (IntPtr)1 : (IntPtr)0, (IntPtr)lParam);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(User32.ENDSESSION_LOGOFF, SessionEndReasons.Logoff)]
        [InlineData(User32.ENDSESSION_LOGOFF | User32.ENDSESSION_CRITICAL, SessionEndReasons.Logoff)]
        [InlineData(User32.ENDSESSION_LOGOFF | User32.ENDSESSION_CLOSEAPP, SessionEndReasons.Logoff)]
        [InlineData(User32.ENDSESSION_LOGOFF | User32.ENDSESSION_CRITICAL | User32.ENDSESSION_CLOSEAPP, SessionEndReasons.Logoff)]
        [InlineData(0, SessionEndReasons.SystemShutdown)]
        [InlineData(User32.ENDSESSION_CRITICAL, SessionEndReasons.SystemShutdown)]
        [InlineData(User32.ENDSESSION_CLOSEAPP, SessionEndReasons.SystemShutdown)]
        [InlineData(User32.ENDSESSION_CRITICAL | User32.ENDSESSION_CLOSEAPP, SessionEndReasons.SystemShutdown)]
        public void SignalsSessionEnded(int lParam, SessionEndReasons reason)
        {
            bool signaled = false;
            SessionEndedEventArgs args = null;
            SessionEndedEventHandler endedHandler = (o, e) =>
            {
                signaled = true;
                args = e;
            };

            SystemEvents.SessionEnded += endedHandler;

            try
            {
                SendMessage(true, lParam);
                Assert.True(signaled);
                Assert.NotNull(args);
                Assert.Equal(reason, args.Reason);
            }
            finally
            {
                SystemEvents.SessionEnded -= endedHandler;
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void DoesNotSignalSessionEnded()
        {
            bool signaled = false;
            SessionEndedEventHandler endedHandler = (o, e) =>
            {
                signaled = true;
            };

            SystemEvents.SessionEnded += endedHandler;

            try
            {
                SendMessage(false, 0);
                Assert.False(signaled);
            }
            finally
            {
                SystemEvents.SessionEnded -= endedHandler;
            }
        }
    }
}
