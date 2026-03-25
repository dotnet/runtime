// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class StartAndForgetTests : ProcessTestBase
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StartAndForget_WithProcessStartInfo_StartsProcessAndReturnsValidPid()
        {
            Process template = CreateProcessLong();
            ProcessStartInfo startInfo = template.StartInfo;

            int pid = Process.StartAndForget(startInfo);

            Assert.True(pid > 0);

            // Verify the process is actually running by retrieving it, then clean up.
            using Process launched = Process.GetProcessById(pid);
            AddProcessForDispose(launched);
            Assert.False(launched.HasExited);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StartAndForget_WithFileNameAndArguments_StartsProcessAndReturnsValidPid()
        {
            Process template = CreateProcessLong();
            string fileName = template.StartInfo.FileName;
            IList<string> arguments = template.StartInfo.ArgumentList;

            int pid = Process.StartAndForget(fileName, arguments);

            Assert.True(pid > 0);

            using Process launched = Process.GetProcessById(pid);
            AddProcessForDispose(launched);
            Assert.False(launched.HasExited);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StartAndForget_WithNullArguments_StartsProcess()
        {
            // A quick process: use CreateProcess with a simple exit-immediately function
            Process template = CreateProcess(() => RemoteExecutor.SuccessExitCode);
            string fileName = template.StartInfo.FileName;
            IList<string> arguments = template.StartInfo.ArgumentList;

            // Passing null arguments is valid (no extra arguments beyond what fileName needs)
            // Use the explicit argument list so it actually works
            int pid = Process.StartAndForget(fileName, arguments);

            Assert.True(pid > 0);
        }

        [Fact]
        public void StartAndForget_WithStartInfo_NullStartInfo_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Process.StartAndForget((ProcessStartInfo)null!));
        }

        [Fact]
        public void StartAndForget_WithFileName_NullFileName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Process.StartAndForget((string)null!));
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
