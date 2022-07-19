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
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
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
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
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
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
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
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
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
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
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
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            string traceFilePath;
            dotnet.Exec(appDll)
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
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
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
            // Entry point projects
            public TestProjectFixture PortableAppFixture { get; }

            public RepoDirectoriesProvider RepoDirectories { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                // Entry point projects
                PortableAppFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
            }

            public void Dispose()
            {
                // Entry point projects
                PortableAppFixture.Dispose();
            }
        }
    }
}
