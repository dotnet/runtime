// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Tests
{
    public partial class ConsoleTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix | TestPlatforms.Windows)]
        public void OpenStandardInputHandle_ReturnsValidHandle()
        {
            RemoteExecutor.Invoke(() =>
            {
                using SafeFileHandle inputHandle = Console.OpenStandardInputHandle();
                Assert.NotNull(inputHandle);
                Assert.False(inputHandle.IsInvalid);
                Assert.False(inputHandle.IsClosed);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix | TestPlatforms.Windows)]
        public void OpenStandardOutputHandle_ReturnsValidHandle()
        {
            RemoteExecutor.Invoke(() =>
            {
                using SafeFileHandle outputHandle = Console.OpenStandardOutputHandle();
                Assert.NotNull(outputHandle);
                Assert.False(outputHandle.IsInvalid);
                Assert.False(outputHandle.IsClosed);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix | TestPlatforms.Windows)]
        public void OpenStandardErrorHandle_ReturnsValidHandle()
        {
            RemoteExecutor.Invoke(() =>
            {
                using SafeFileHandle errorHandle = Console.OpenStandardErrorHandle();
                Assert.NotNull(errorHandle);
                Assert.False(errorHandle.IsInvalid);
                Assert.False(errorHandle.IsClosed);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix | TestPlatforms.Windows)]
        public void OpenStandardHandles_DoNotOwnHandle()
        {
            RemoteExecutor.Invoke(() =>
            {
                SafeFileHandle inputHandle = Console.OpenStandardInputHandle();
                SafeFileHandle outputHandle = Console.OpenStandardOutputHandle();
                SafeFileHandle errorHandle = Console.OpenStandardErrorHandle();

                // Disposing should not close the underlying handle since ownsHandle is false
                inputHandle.Dispose();
                outputHandle.Dispose();
                errorHandle.Dispose();

                // Should still be able to get new handles
                using SafeFileHandle inputHandle2 = Console.OpenStandardInputHandle();
                using SafeFileHandle outputHandle2 = Console.OpenStandardOutputHandle();
                using SafeFileHandle errorHandle2 = Console.OpenStandardErrorHandle();

                Assert.NotNull(inputHandle2);
                Assert.NotNull(outputHandle2);
                Assert.NotNull(errorHandle2);
                Assert.False(inputHandle2.IsInvalid);
                Assert.False(outputHandle2.IsInvalid);
                Assert.False(errorHandle2.IsInvalid);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix | TestPlatforms.Windows)]
        public void OpenStandardHandles_CanBeUsedWithStream()
        {
            RemoteExecutor.Invoke(() =>
            {
                using SafeFileHandle outputHandle = Console.OpenStandardOutputHandle();
                using FileStream fs = new FileStream(outputHandle, FileAccess.Write);
                using StreamWriter writer = new StreamWriter(fs);
                writer.WriteLine("Test output");
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void OpenStandardInputHandle_ThrowsOnUnsupportedPlatforms()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Throws<PlatformNotSupportedException>(() => Console.OpenStandardInputHandle());
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void OpenStandardOutputHandle_ThrowsOnUnsupportedPlatforms()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Throws<PlatformNotSupportedException>(() => Console.OpenStandardOutputHandle());
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void OpenStandardErrorHandle_ThrowsOnUnsupportedPlatforms()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Throws<PlatformNotSupportedException>(() => Console.OpenStandardErrorHandle());
            }).Dispose();
        }
    }
}
