// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Xunit;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public abstract class SystemEventsTest
    {
        IntPtr s_hwnd = IntPtr.Zero;

        public const int PostMessageWait = 30_000;
        public const int ExpectedEventMultiplier = 1000;
        public const int UnexpectedEventMultiplier = 10;

        protected IntPtr SendMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            EnsureHwnd();
            return User32.SendMessageW(s_hwnd, msg, wParam, lParam);
        }

        private void EnsureHwnd()
        {
            if (s_hwnd == IntPtr.Zero)
            {
                if (PlatformDetection.IsFullFramework)
                {
                    // desktop has a bug where it will allow EnsureSystemEvents to proceed without actually creating the HWND
                    WaitForSystemEventsWindow();
                }

                // locate the hwnd used by SystemEvents in this domain
                var windowClassNameField = typeof(SystemEvents).GetField("s_className", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic) ??  // corefx
                                           typeof(SystemEvents).GetField("className", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);      // desktop
                Assert.NotNull(windowClassNameField);
                var windowClassName = windowClassNameField.GetValue(null) as string;
                Assert.NotNull(windowClassName);

                s_hwnd = User32.FindWindowW(windowClassName, null);
                Assert.NotEqual(s_hwnd, IntPtr.Zero);
            }
        }

        internal static void WaitForSystemEventsWindow()
        {
            // wait for the window to be created
            var windowReadyField = typeof(SystemEvents).GetField("s_eventWindowReady", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic) ??  // corefx
                                   typeof(SystemEvents).GetField("eventWindowReady", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);      // desktop
            Assert.NotNull(windowReadyField);
            var windowReadyEvent = windowReadyField.GetValue(null) as ManualResetEvent;
            if (windowReadyEvent != null)
            {
                // on an STA thread the HWND is created in the same thread synchronously when attaching to an event handler
                // if we're on an MTA thread, a new thread is created to handle events and that thread creates the window, wait for it to complete.
                Assert.True(windowReadyEvent.WaitOne(PostMessageWait));
            }
        }
    }
}
