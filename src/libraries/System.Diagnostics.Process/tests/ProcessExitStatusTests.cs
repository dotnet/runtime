// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessExitStatusTests
    {
        [Fact]
        public void Constructor_WithExitCodeOnly_SetsPropertiesCorrectly()
        {
            ProcessExitStatus status = new ProcessExitStatus(0, false);

            Assert.Equal(0, status.ExitCode);
            Assert.False(status.Canceled);
            Assert.Null(status.Signal);
        }

        [Fact]
        public void Constructor_WithNonZeroExitCode_SetsPropertiesCorrectly()
        {
            ProcessExitStatus status = new ProcessExitStatus(42, false);

            Assert.Equal(42, status.ExitCode);
            Assert.False(status.Canceled);
            Assert.Null(status.Signal);
        }

        [Fact]
        public void Constructor_WithCanceled_SetsPropertiesCorrectly()
        {
            ProcessExitStatus status = new ProcessExitStatus(1, true);

            Assert.Equal(1, status.ExitCode);
            Assert.True(status.Canceled);
            Assert.Null(status.Signal);
        }

        [Fact]
        public void Constructor_WithSignal_SetsPropertiesCorrectly()
        {
            ProcessExitStatus status = new ProcessExitStatus(137, false, PosixSignal.SIGTERM);

            Assert.Equal(137, status.ExitCode);
            Assert.False(status.Canceled);
            Assert.Equal(PosixSignal.SIGTERM, status.Signal);
        }

        [Fact]
        public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
        {
            ProcessExitStatus status = new ProcessExitStatus(130, true, PosixSignal.SIGINT);

            Assert.Equal(130, status.ExitCode);
            Assert.True(status.Canceled);
            Assert.Equal(PosixSignal.SIGINT, status.Signal);
        }
    }

    public class ProcessOutputLineTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var line = new ProcessOutputLine("hello", standardError: false);
            Assert.Equal("hello", line.Content);
            Assert.False(line.StandardError);
        }

        [Fact]
        public void Constructor_StandardError_SetsPropertiesCorrectly()
        {
            var line = new ProcessOutputLine("error msg", standardError: true);
            Assert.Equal("error msg", line.Content);
            Assert.True(line.StandardError);
        }

        [Fact]
        public void Constructor_ThrowsForNullContent()
        {
            Assert.Throws<ArgumentNullException>(() => new ProcessOutputLine(null!, standardError: false));
        }
    }

    public class ProcessTextOutputTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var exitStatus = new ProcessExitStatus(0, false);
            var output = new ProcessTextOutput(exitStatus, "stdout", "stderr", 42);

            Assert.Same(exitStatus, output.ExitStatus);
            Assert.Equal("stdout", output.StandardOutput);
            Assert.Equal("stderr", output.StandardError);
            Assert.Equal(42, output.ProcessId);
        }

        [Fact]
        public void Constructor_ThrowsForNullArguments()
        {
            var exitStatus = new ProcessExitStatus(0, false);
            Assert.Throws<ArgumentNullException>(() => new ProcessTextOutput(null!, "stdout", "stderr", 42));
            Assert.Throws<ArgumentNullException>(() => new ProcessTextOutput(exitStatus, null!, "stderr", 42));
            Assert.Throws<ArgumentNullException>(() => new ProcessTextOutput(exitStatus, "stdout", null!, 42));
        }
    }
}
