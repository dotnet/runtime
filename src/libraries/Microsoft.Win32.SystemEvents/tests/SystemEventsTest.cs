// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public abstract class SystemEventsTest
    {
        IntPtr s_hwnd = IntPtr.Zero;

        public const int PostMessageWait = 10000;
        public const int ExpectedEventMultiplier = 1000;
        public const int UnexpectedEventMultiplier = 10;

        protected IntPtr SendMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            if (s_hwnd == IntPtr.Zero)
            {
                // locate the hwnd used by SystemEvents in this domain
                var windowClassNameField = typeof(SystemEvents).GetField("s_className", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (windowClassNameField == null)
                {
                    // desktop doesn't use the s_ prefix
                    windowClassNameField = typeof(SystemEvents).GetField("className", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                }

                Assert.NotNull(windowClassNameField);
                var windowClassName = windowClassNameField.GetValue(null) as string;
                Assert.NotNull(windowClassName);

                s_hwnd = User32.FindWindowW(windowClassName, null);
                Assert.NotEqual(s_hwnd, IntPtr.Zero);
            }

            return User32.SendMessageW(s_hwnd, msg, wParam, lParam);
        }
    }
}
