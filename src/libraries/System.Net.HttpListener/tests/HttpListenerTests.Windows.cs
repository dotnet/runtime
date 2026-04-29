// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // httpsys component missing in Nano.
    public class HttpListenerWindowsTests
    {
        [Fact]
        public void EnableKernelResponseBuffering_DefaultIsDisabled()
        {
            using (var listener = new HttpListener())
            {
                listener.Start();
                Assert.False(GetEnableKernelResponseBufferingValue());
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task EnableKernelResponseBuffering_Enabled()
        {
            await RemoteExecutor.Invoke(() =>
            {
                AppContext.SetSwitch("System.Net.HttpListener.EnableKernelResponseBuffering", true);

                using (var listener = new HttpListener())
                {
                    listener.Start();
                    Assert.True(GetEnableKernelResponseBufferingValue());
                }
            }).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task EnableKernelResponseBuffering_ImmutableAfterStart()
        {
            await RemoteExecutor.Invoke(() =>
            {
                AppContext.SetSwitch("System.Net.HttpListener.EnableKernelResponseBuffering", false);

                using (var listener = new HttpListener())
                {
                    listener.Start();
                    Assert.False(GetEnableKernelResponseBufferingValue());

                    AppContext.SetSwitch("System.Net.HttpListener.EnableKernelResponseBuffering", true);

                    // Assert internal value wasn't updated, despite updating the AppContext switch.
                    Assert.False(GetEnableKernelResponseBufferingValue());
                }
            }).DisposeAsync();
        }

        private bool GetEnableKernelResponseBufferingValue()
        {
            // We need EnableKernelResponseBuffering which is internal so we get it using reflection.
            var prop = typeof(HttpListener).GetProperty(
                "EnableKernelResponseBuffering",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(prop);

            object? value = prop!.GetValue(obj: null);
            Assert.NotNull(value);

            return Assert.IsType<bool>(value);
        }
    }
}
