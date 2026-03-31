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
        [Fact]
        [PlatformSpecific(TestPlatforms.Any & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.tvOS & ~TestPlatforms.Android)]
        public void OpenStandardInputHandle_ReturnsValidHandle()
        {
            using SafeFileHandle inputHandle = Console.OpenStandardInputHandle();
            Assert.NotNull(inputHandle);
            Assert.False(inputHandle.IsInvalid);
            Assert.False(inputHandle.IsClosed);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Any & ~TestPlatforms.iOS & ~TestPlatforms.tvOS & ~TestPlatforms.Android)]
        public void OpenStandardOutputHandle_ReturnsValidHandle()
        {
            using SafeFileHandle outputHandle = Console.OpenStandardOutputHandle();
            Assert.NotNull(outputHandle);
            Assert.False(outputHandle.IsInvalid);
            Assert.False(outputHandle.IsClosed);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Any & ~TestPlatforms.iOS & ~TestPlatforms.tvOS & ~TestPlatforms.Android)]
        public void OpenStandardErrorHandle_ReturnsValidHandle()
        {
            using SafeFileHandle errorHandle = Console.OpenStandardErrorHandle();
            Assert.NotNull(errorHandle);
            Assert.False(errorHandle.IsInvalid);
            Assert.False(errorHandle.IsClosed);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Any & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.tvOS & ~TestPlatforms.Android)]
        public void OpenStandardHandles_DoNotOwnHandle()
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
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Any & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.tvOS & ~TestPlatforms.Android)]
        public void OpenStandardHandles_CanBeUsedWithStream()
        {
            using RemoteInvokeHandle child = RemoteExecutor.Invoke(() =>
            {
                using SafeFileHandle outputHandle = Console.OpenStandardOutputHandle();
                using FileStream fs = new FileStream(outputHandle, FileAccess.Write);
                using StreamWriter writer = new StreamWriter(fs);
                writer.WriteLine("Test output");
            }, new RemoteInvokeOptions { StartInfo = new ProcessStartInfo() { RedirectStandardOutput = true } });

            // Verify the output was written
            string output = child.Process.StandardOutput.ReadLine();
            Assert.Equal("Test output", output);
            
            child.Process.WaitForExit();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.Browser)]
        public void OpenStandardInputHandle_ThrowsOnUnsupportedPlatforms()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Console.OpenStandardInputHandle());
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void OpenStandardOutputHandle_ThrowsOnUnsupportedPlatforms()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Console.OpenStandardOutputHandle());
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void OpenStandardErrorHandle_ThrowsOnUnsupportedPlatforms()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Console.OpenStandardErrorHandle());
        }
    }
}
