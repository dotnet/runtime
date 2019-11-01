// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public class SessionEndingTests : SystemEventsTest
    {
        private IntPtr SendMessage(int lParam)
        {
            return SendMessage(User32.WM_QUERYENDSESSION, IntPtr.Zero, (IntPtr)lParam);
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
        public void SignalsSessionEnding(int lParam, SessionEndReasons reason)
        {
            bool signaled = false;
            SessionEndingEventArgs args = null;
            SessionEndingEventHandler endingHandler = (o, e) =>
            {
                signaled = true;
                args = e;
            };

            SystemEvents.SessionEnding += endingHandler;

            try
            {
                SendMessage(lParam);
                Assert.True(signaled);
                Assert.NotNull(args);
                Assert.Equal(reason, args.Reason);
            }
            finally
            {
                SystemEvents.SessionEnding -= endingHandler;
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void CancelSessionEnding()
        {
            bool shouldCancel = false;
            SessionEndingEventHandler endingHandler = (o, e) =>
            {
                e.Cancel = shouldCancel;
            };

            SystemEvents.SessionEnding += endingHandler;
            try
            {
                Assert.Equal((IntPtr)1, SendMessage(0));
                shouldCancel = true;
                Assert.Equal((IntPtr)0, SendMessage(0));
            }
            finally
            {
                SystemEvents.SessionEnding -= endingHandler;
            }
        }
    }
}
