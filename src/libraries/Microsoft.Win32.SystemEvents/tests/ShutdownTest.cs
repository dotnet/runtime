// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public abstract class ShutdownTest : SystemEventsTest
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void ShutdownThroughRestartManager()
        {
            RemoteExecutor.Invoke(() =>
            {
                // Register any event to ensure that SystemEvents get initialized
                SystemEvents.TimeChanged += (o, e) => { };

                // Fake Restart Manager behavior by sending external WM_CLOSE message
                SendMessage(Interop.User32.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

                // Emulate calling the Shutdown event
                var shutdownMethod = typeof(SystemEvents).GetMethod("Shutdown", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new Type[0], null);
                Assert.NotNull(shutdownMethod);
                shutdownMethod.Invoke(null, null);
            }).Dispose();
        }
    }
}
