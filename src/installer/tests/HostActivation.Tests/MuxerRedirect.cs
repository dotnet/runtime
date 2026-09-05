// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution;
using Xunit;

namespace HostActivation.Tests
{
    public class MuxerRedirect : IDisposable
    {
        private const string RedirectTargetEnvironmentVariable = "DOTNET_ROOT_REDIRECT_TARGET";
        private const string SourceVersion = "9999.0.1";
        private const string TargetVersion = "9999.0.2";

        private readonly TestArtifact _artifact;
        private readonly DotNetCli _sourceDotNet;
        private readonly DotNetCli _targetDotNet;

        public MuxerRedirect()
        {
            _artifact = TestArtifact.Create(nameof(MuxerRedirect));

            _sourceDotNet = new DotNetBuilder(_artifact.Location, HostTestContext.BuiltDotNet.BinPath, "source")
                .AddMockSDK(SourceVersion, SourceVersion)
                .AddMicrosoftNETCoreAppFrameworkMockHostPolicy(SourceVersion)
                .Build();

            _targetDotNet = new DotNetBuilder(_artifact.Location, HostTestContext.BuiltDotNet.BinPath, "target")
                .AddMockSDK(TargetVersion, TargetVersion)
                .AddMicrosoftNETCoreAppFrameworkMockHostPolicy(TargetVersion)
                .Build();
        }

        [Fact]
        public void RedirectTargetWinsForSdkResolution()
        {
            _sourceDotNet.Exec("--list-sdks")
                .EnvironmentVariable(RedirectTargetEnvironmentVariable, _targetDotNet.BinPath)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"{TargetVersion} [{Path.Combine(_targetDotNet.BinPath, "sdk")}")
                .And.NotHaveStdOutContaining(SourceVersion)
                .And.HaveStdErrContaining($"Redirecting dotnet root to [{_targetDotNet.BinPath}]");
        }

        [Fact]
        public void RedirectTargetWinsForRuntimeResolution()
        {
            _sourceDotNet.Exec("help")
                .EnvironmentVariable(RedirectTargetEnvironmentVariable, _targetDotNet.BinPath)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .ShouldHaveResolvedFramework(Constants.MicrosoftNETCoreApp, TargetVersion, _targetDotNet.BinPath);
        }

        [Fact]
        public void RedirectTargetDoesNotFallBackToSourceHive()
        {
            string emptyTarget = Path.Combine(_artifact.Location, "empty-target");
            Directory.CreateDirectory(emptyTarget);

            _sourceDotNet.Exec("--list-sdks")
                .EnvironmentVariable(RedirectTargetEnvironmentVariable, emptyTarget)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"Redirecting dotnet root to [{emptyTarget}]")
                .And.NotHaveStdOutContaining(SourceVersion);
        }

        public void Dispose()
        {
            _artifact.Dispose();
        }
    }
}