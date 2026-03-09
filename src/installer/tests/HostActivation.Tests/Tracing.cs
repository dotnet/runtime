// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class Tracing
    {
        // Trace messages currently expected for running dotnet --list-runtimes
        private const string ExpectedVerboseMessage = "--- Executing in muxer mode";
        private const string ExpectedInfoMessage = "--- Invoked dotnet";

        private const string ExpectedBadPathMessage = "Unable to open specified trace file for writing:";

        [Fact]
        public void TracingOff()
        {
            HostTestContext.BuiltDotNet.Exec("--list-runtimes")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.NotHaveStdErrContaining(ExpectedInfoMessage)
                .And.NotHaveStdErrContaining(ExpectedVerboseMessage);
        }

        [Fact]
        public void TracingOnDefault()
        {
            HostTestContext.BuiltDotNet.Exec("--list-runtimes")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(ExpectedInfoMessage)
                .And.HaveStdErrContaining(ExpectedVerboseMessage);
        }

        [Fact]
        public void TracingOnVerbose()
        {
            HostTestContext.BuiltDotNet.Exec("--list-runtimes")
                .EnableTracingAndCaptureOutputs()
                .EnvironmentVariable(Constants.HostTracing.VerbosityEnvironmentVariable, "4")
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(ExpectedInfoMessage)
                .And.HaveStdErrContaining(ExpectedVerboseMessage);
        }

        [Fact]
        public void TracingOnInfo()
        {
            HostTestContext.BuiltDotNet.Exec("--list-runtimes")
                .EnableTracingAndCaptureOutputs()
                .EnvironmentVariable(Constants.HostTracing.VerbosityEnvironmentVariable, "3")
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(ExpectedInfoMessage)
                .And.NotHaveStdErrContaining(ExpectedVerboseMessage);
        }

        [Fact]
        public void TracingOnWarning()
        {
            HostTestContext.BuiltDotNet.Exec("--list-runtimes")
                .EnableTracingAndCaptureOutputs()
                .EnvironmentVariable(Constants.HostTracing.VerbosityEnvironmentVariable, "2")
                .Execute()
                .Should().Pass()
                .And.NotHaveStdErrContaining(ExpectedInfoMessage)
                .And.NotHaveStdErrContaining(ExpectedVerboseMessage);
        }

        [Fact]
        public void TracingOnToFileDefault()
        {
            string traceFilePath;
            HostTestContext.BuiltDotNet.Exec("--list-runtimes")
                .EnableHostTracingToFile(out traceFilePath)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.NotHaveStdErrContaining(ExpectedInfoMessage)
                .And.NotHaveStdErrContaining(ExpectedVerboseMessage)
                .And.FileExists(traceFilePath)
                .And.FileContains(traceFilePath, ExpectedVerboseMessage);

            FileUtils.DeleteFileIfPossible(traceFilePath);
        }

        [Fact]
        public void TracingOnToFileBadPathDefault()
        {
            HostTestContext.BuiltDotNet.Exec("--list-runtimes")
                .EnableTracingAndCaptureOutputs()
                .EnvironmentVariable(Constants.HostTracing.TraceFileEnvironmentVariable, "badpath/TracingOnToFileBadPathDefault.log")
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(ExpectedInfoMessage)
                .And.HaveStdErrContaining(ExpectedVerboseMessage)
                .And.HaveStdErrContaining(ExpectedBadPathMessage);
        }

        [Fact]
        public void TracingOnToDirectory()
        {
            using (TestArtifact directory = TestArtifact.Create("trace"))
            {
                var result = HostTestContext.BuiltDotNet.Exec("--list-runtimes")
                    .EnableHostTracingToPath(directory.Location)
                    .CaptureStdOut()
                    .CaptureStdErr()
                    .Execute();

                string traceFilePath = Path.Combine(directory.Location, $"{Path.GetFileNameWithoutExtension(Binaries.DotNet.FileName)}.{result.ProcessId}.log");
                result.Should().Pass()
                    .And.NotHaveStdErrContaining(ExpectedInfoMessage)
                    .And.NotHaveStdErrContaining(ExpectedVerboseMessage)
                    .And.FileExists(traceFilePath)
                    .And.FileContains(traceFilePath, ExpectedVerboseMessage);
            }
        }

        [Fact]
        public void LegacyVariableName()
        {
            HostTestContext.BuiltDotNet.Exec("--list-runtimes")
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(ExpectedInfoMessage)
                .And.HaveStdErrContaining(ExpectedVerboseMessage);
        }
    }
}
