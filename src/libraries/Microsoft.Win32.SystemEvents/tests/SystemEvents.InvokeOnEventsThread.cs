// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public class InvokeOnEventsThreadTests : SystemEventsTest
    {
        [ActiveIssue("https://github.com/dotnet/runtime/issues/29941", TargetFrameworkMonikers.NetFramework)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))]
        public void InvokeOnEventsThreadRunsAsynchronously()
        {
            var invoked = new AutoResetEvent(false);
            SystemEvents.InvokeOnEventsThread(new Action(() => invoked.Set()));
            Assert.True(invoked.WaitOne(PostMessageWait));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))]
        public void InvokeOnEventsThreadRunsOnSameThreadAsOtherEvents()
        {
            int expectedThreadId = -1, actualThreadId = -1;
            var invoked = new AutoResetEvent(false);
            EventHandler handler = (sender, args) =>
            {
                expectedThreadId = Environment.CurrentManagedThreadId;
            };

            try
            {
                // force a TimeChanged event to get the event thread ID
                SystemEvents.TimeChanged += handler;
                SendMessage(User32.WM_REFLECT + User32.WM_TIMECHANGE, IntPtr.Zero, IntPtr.Zero);
                Assert.NotEqual(-1, expectedThreadId);

                SystemEvents.InvokeOnEventsThread(new Action(() =>
                {
                    actualThreadId = Environment.CurrentManagedThreadId;
                    invoked.Set();
                }));
                Assert.True(invoked.WaitOne(PostMessageWait));
                Assert.Equal(expectedThreadId, actualThreadId);
            }
            finally
            {
                SystemEvents.TimeChanged -= handler;
                invoked.Dispose();
            }
        }

        public static bool NotNanoNorServerCoreAndRemoteExecutorSupported => PlatformDetection.IsNotWindowsNanoNorServerCore && RemoteExecutor.IsSupported;

        [ConditionalFact(nameof(NotNanoNorServerCoreAndRemoteExecutorSupported))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34360", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void RegisterFromSTAThreadThatGoesAway_MessageStillDelivered()
        {
            RemoteExecutor.Invoke(() => // to ensure no one has registered for any events before
            {
                bool changing = false, changed = false;

                // Register for the events on an STA thread that then immediately exits
                var thread = new Thread(() =>
                {
                    SystemEvents.DisplaySettingsChanging += (o, e) => changing = true;
                    SystemEvents.DisplaySettingsChanged += (o, e) => changed = true;
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                SendMessage(User32.WM_REFLECT + User32.WM_DISPLAYCHANGE, IntPtr.Zero, IntPtr.Zero);

                Assert.True(changing);
                Assert.True(changed);
            }).Dispose();
        }
    }
}
