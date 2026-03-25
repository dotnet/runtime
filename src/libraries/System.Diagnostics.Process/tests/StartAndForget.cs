// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class StartAndForgetTests : ProcessTestBase
    {
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void StartAndForget_StartsProcessAndReturnsValidPid(bool useProcessStartInfo)
        {
            Process template = CreateProcessLong();
            int pid = useProcessStartInfo
                ? Process.StartAndForget(template.StartInfo)
                : Process.StartAndForget(template.StartInfo.FileName, template.StartInfo.ArgumentList);

            Assert.True(pid > 0);

            using Process launched = Process.GetProcessById(pid);
            try
            {
                Assert.False(launched.HasExited);
            }
            finally
            {
                launched.Kill();
                launched.WaitForExit();
            }
        }

        [Fact]
        public void StartAndForget_WithNullArguments_StartsProcess()
        {
            // hostname is not available on Android or Azure Linux.
            // ls is available on every Unix.
            int pid = Process.StartAndForget(OperatingSystem.IsWindows() ? "hostname" : "ls", null);

            Assert.True(pid > 0);
        }

        [Fact]
        public void StartAndForget_WithStartInfo_NullStartInfo_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("startInfo", () => Process.StartAndForget((ProcessStartInfo)null!));
        }

        [Fact]
        public void StartAndForget_WithFileName_NullFileName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => Process.StartAndForget((string)null!));
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void StartAndForget_WithRedirectedStreams_ThrowsInvalidOperationException(
            bool redirectInput, bool redirectOutput, bool redirectError)
        {
            ProcessStartInfo startInfo = new("someprocess")
            {
                RedirectStandardInput = redirectInput,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectError,
            };

            Assert.Throws<InvalidOperationException>(() => Process.StartAndForget(startInfo));
        }
    }
}
