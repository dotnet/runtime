// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Tests
{
    public partial class ConsoleTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Any & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.tvOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.Android)]
        public void OpenStandardInputHandle_ReturnsValidHandle()
        {
            using SafeFileHandle inputHandle = Console.OpenStandardInputHandle();
            Assert.NotNull(inputHandle);
            Assert.False(inputHandle.IsInvalid);
            Assert.False(inputHandle.IsClosed);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Any & ~TestPlatforms.iOS & ~TestPlatforms.tvOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.Android)]
        public void OpenStandardOutputHandle_ReturnsValidHandle()
        {
            using SafeFileHandle outputHandle = Console.OpenStandardOutputHandle();
            Assert.NotNull(outputHandle);
            Assert.False(outputHandle.IsInvalid);
            Assert.False(outputHandle.IsClosed);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Any & ~TestPlatforms.iOS & ~TestPlatforms.tvOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.Android)]
        public void OpenStandardErrorHandle_ReturnsValidHandle()
        {
            using SafeFileHandle errorHandle = Console.OpenStandardErrorHandle();
            Assert.NotNull(errorHandle);
            Assert.False(errorHandle.IsInvalid);
            Assert.False(errorHandle.IsClosed);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Any & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.tvOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.Android)]
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
        [PlatformSpecific(TestPlatforms.Any & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.tvOS & ~TestPlatforms.MacCatalyst & ~TestPlatforms.Android)]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CanCopyStandardInputToStandardOutput()
        {
            const string inputContent = "Hello from seekable stdin!";
            string testFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.WriteAllText(testFilePath, inputContent, Encoding.UTF8);

            try
            {
                using SafeFileHandle stdinHandle = File.OpenHandle(testFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using RemoteInvokeHandle handle = RemoteExecutor.Invoke(
                    static () =>
                    {
                        Console.OpenStandardInput().CopyTo(Console.OpenStandardOutput());
                        return RemoteExecutor.SuccessExitCode;
                    },
                    new RemoteInvokeOptions { StartInfo = new ProcessStartInfo { RedirectStandardOutput = true, StandardInputHandle = stdinHandle } });

                string output = handle.Process.StandardOutput.ReadToEnd();
                Assert.True(handle.Process.WaitForExit(30_000), "Process did not exit in time — possible infinite loop when reading seekable stdin.");
                Assert.Equal(inputContent, output);
            }
            finally
            {
                File.Delete(testFilePath);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void UnixConsoleStream_SeekableStdoutRedirection_WritesAllContent()
        {
            const string outputContentPart1 = "Hello seekable ";
            const string outputContentPart2 = "stdout!";
            string testFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                using SafeFileHandle stdoutHandle = File.OpenHandle(testFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using RemoteInvokeHandle handle = RemoteExecutor.Invoke(
                    static (part1, part2) =>
                    {
                        Stream stdout = Console.OpenStandardOutput();
                        stdout.Write(Encoding.UTF8.GetBytes(part1));
                        stdout.Write(Encoding.UTF8.GetBytes(part2));
                        return RemoteExecutor.SuccessExitCode;
                    },
                    outputContentPart1,
                    outputContentPart2,
                    new RemoteInvokeOptions { StartInfo = new ProcessStartInfo { StandardOutputHandle = stdoutHandle } });

                stdoutHandle.Dispose();
                Assert.True(handle.Process.WaitForExit(30_000), "Process did not exit in time.");

                string output = File.ReadAllText(testFilePath, Encoding.UTF8);
                Assert.Equal(outputContentPart1 + outputContentPart2, output);
            }
            finally
            {
                File.Delete(testFilePath);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst | TestPlatforms.Browser)]
        public void OpenStandardInputHandle_ThrowsOnUnsupportedPlatforms()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Console.OpenStandardInputHandle());
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void OpenStandardOutputHandle_ThrowsOnUnsupportedPlatforms()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Console.OpenStandardOutputHandle());
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void OpenStandardErrorHandle_ThrowsOnUnsupportedPlatforms()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Console.OpenStandardErrorHandle());
        }
    }
}
