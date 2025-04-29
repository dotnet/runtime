// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class Tracing : IClassFixture<Tracing.SharedTestState>
    {
        private SharedTestState sharedTestState;

        // Trace messages currently expected for a passing app (somewhat randomly selected)
        private const string ExpectedVerboseMessage = "--- Begin breadcrumb write";
        private const string ExpectedInfoMessage = "Deps file:";
        private const string ExpectedBadPathMessage = "Unable to open COREHOST_TRACEFILE=";

        public Tracing(Tracing.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void TracingOff()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll)
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
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining(ExpectedInfoMessage)
                .And.HaveStdErrContaining(ExpectedVerboseMessage);
        }

        [Fact]
        public void TracingOnVerbose()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll)
                .EnableTracingAndCaptureOutputs()
                .EnvironmentVariable(Constants.HostTracing.VerbosityEnvironmentVariable, "4")
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining(ExpectedInfoMessage)
                .And.HaveStdErrContaining(ExpectedVerboseMessage);
        }

        [Fact]
        public void TracingOnInfo()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll)
                .EnableTracingAndCaptureOutputs()
                .EnvironmentVariable(Constants.HostTracing.VerbosityEnvironmentVariable, "3")
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining(ExpectedInfoMessage)
                .And.NotHaveStdErrContaining(ExpectedVerboseMessage);
        }

        [Fact]
        public void TracingOnWarning()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll)
                .EnableTracingAndCaptureOutputs()
                .EnvironmentVariable(Constants.HostTracing.VerbosityEnvironmentVariable, "2")
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.NotHaveStdErrContaining(ExpectedInfoMessage)
                .And.NotHaveStdErrContaining(ExpectedVerboseMessage);
        }

        [Fact]
        public void TracingOnToFileDefault()
        {

            string traceFilePath;
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll)
                .EnableHostTracingToFile(out traceFilePath)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.NotHaveStdErrContaining(ExpectedInfoMessage)
                .And.NotHaveStdErrContaining(ExpectedVerboseMessage)
                .And.FileExists(traceFilePath)
                .And.FileContains(traceFilePath, ExpectedVerboseMessage);

            FileUtils.DeleteFileIfPossible(traceFilePath);
        }

        [Fact]
        public void TracingOnToFileBadPathDefault()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll)
                .EnableTracingAndCaptureOutputs()
                .EnvironmentVariable(Constants.HostTracing.TraceFileEnvironmentVariable, "badpath/TracingOnToFileBadPathDefault.log")
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining(ExpectedInfoMessage)
                .And.HaveStdErrContaining(ExpectedVerboseMessage)
                .And.HaveStdErrContaining(ExpectedBadPathMessage);
        }

        public class SharedTestState : IDisposable
        {
            public TestApp App { get; }

            public SharedTestState()
            {
                App = TestApp.CreateFromBuiltAssets("HelloWorld");
                App.CreateAppHost();
            }

            public void Dispose()
            {
                App?.Dispose();
            }
        }
    }
}
