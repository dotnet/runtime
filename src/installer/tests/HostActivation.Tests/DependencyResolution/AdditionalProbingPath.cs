// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public class AdditionalProbingPath : DependencyResolutionBase, IClassFixture<AdditionalProbingPath.SharedTestState>
    {
        private readonly SharedTestState sharedState;

        public AdditionalProbingPath(SharedTestState sharedState)
        {
            this.sharedState = sharedState;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CommandLine(bool dependencyExists)
        {
            string probePath = dependencyExists ? sharedState.AdditionalProbingPath : sharedState.Location;
            TestApp app = sharedState.FrameworkReferenceApp;
            CommandResult result = sharedState.DotNetWithNetCoreApp.Exec(Constants.AdditionalProbingPath.CommandLineArgument, probePath, app.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute();

            result.Should().HaveUsedAdditionalProbingPath(probePath);
            if (dependencyExists)
            {
                result.Should().Pass()
                    .And.HaveResolvedAssembly(sharedState.DependencyPath)
                    .And.HaveResolvedNativeLibraryPath(sharedState.NativeDependencyDirectory);
            }
            else
            {
                // Specifying additional probing paths triggers file existence checking, so execution
                // should fail on the first dependency that doesn't exist.
                result.Should().Fail()
                    .And.ErrorWithMissingAssembly(Path.GetFileName(app.DepsJson), SharedTestState.DependencyName, SharedTestState.DependencyVersion);
            }
        }

        [Fact]
        public void PlaceholderArchTfm()
        {
            // Host should replace |arch| and |tfm| with actual architecture and TFM
            string probePath = Path.Combine(sharedState.AdditionalProbingPath, "|arch|", "|tfm|");
            TestApp app = sharedState.FrameworkReferenceApp;
            sharedState.DotNetWithNetCoreApp.Exec(Constants.AdditionalProbingPath.CommandLineArgument, probePath, app.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveUsedAdditionalProbingPath(sharedState.AdditionalProbingPath_ArchTfm)
                .And.HaveResolvedAssembly(sharedState.DependencyPath_ArchTfm)
                .And.HaveResolvedNativeLibraryPath(sharedState.NativeDependencyDirectory_ArchTfm);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RuntimeConfigSetting(bool dependencyExists)
        {
            string probePath = dependencyExists ? sharedState.AdditionalProbingPath : sharedState.Location;
            TestApp app = sharedState.FrameworkReferenceApp.Copy();
            RuntimeConfig.FromFile(app.RuntimeConfigJson)
                .WithAdditionalProbingPath(probePath)
                .Save();
            CommandResult result = sharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute();

            result.Should().HaveUsedAdditionalProbingPath(probePath);
            if (dependencyExists)
            {
                result.Should().Pass()
                    .And.HaveResolvedAssembly(sharedState.DependencyPath)
                    .And.HaveResolvedNativeLibraryPath(sharedState.NativeDependencyDirectory);
            }
            else
            {
                // Specifying additional probing paths triggers file existence checking, so execution
                // should fail on the first dependency that doesn't exist.
                result.Should().Fail()
                    .And.ErrorWithMissingAssembly(Path.GetFileName(app.DepsJson), SharedTestState.DependencyName, SharedTestState.DependencyVersion);
            }
        }

        public class SharedTestState : DependencyResolutionBase.SharedTestStateBase
        {
            public DotNetCli DotNetWithNetCoreApp { get; }

            public TestApp FrameworkReferenceApp { get; }

            public const string DependencyName = "Dependency";
            public const string DependencyVersion = "1.0.0";

            public string DependencyPath { get; }
            public string NativeDependencyDirectory { get; }

            public string DependencyPath_ArchTfm { get; }
            public string NativeDependencyDirectory_ArchTfm { get; }

            public string AdditionalProbingPath { get; }
            public string AdditionalProbingPath_ArchTfm { get; }

            public SharedTestState()
            {
                DotNetWithNetCoreApp = DotNet("WithNetCoreApp")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr(TestContext.MicrosoftNETCoreAppVersion)
                    .Build();

                string nativeDependencyRelPath = $"{TestContext.TargetRID}/{Binaries.GetSharedLibraryFileNameForCurrentPlatform("native")}";
                FrameworkReferenceApp = CreateFrameworkReferenceApp(Constants.MicrosoftNETCoreApp, TestContext.MicrosoftNETCoreAppVersion, b => b
                    .WithProject(DependencyName, DependencyVersion, p => p
                        .WithAssemblyGroup(null, g => g
                            .WithAsset($"{DependencyName}.dll", f => f.NotOnDisk()))
                        .WithNativeLibraryGroup(TestContext.TargetRID, g => g
                            .WithAsset(nativeDependencyRelPath, f => f.NotOnDisk()))));
                RuntimeConfig.FromFile(FrameworkReferenceApp.RuntimeConfigJson)
                    .WithTfm(TestContext.Tfm)
                    .Save();

                AdditionalProbingPath = Path.Combine(Location, "probe");
                (DependencyPath, NativeDependencyDirectory) = AddDependencies(AdditionalProbingPath);

                AdditionalProbingPath_ArchTfm = Path.Combine(AdditionalProbingPath, TestContext.BuildArchitecture, TestContext.Tfm);
                (DependencyPath_ArchTfm, NativeDependencyDirectory_ArchTfm) = AddDependencies(AdditionalProbingPath_ArchTfm);

                (string, string) AddDependencies(string probeDir)
                {
                    // Probing will look under <library_name>/<library_version>
                    string dependencyDir = Path.Combine(probeDir, DependencyName, DependencyVersion);
                    Directory.CreateDirectory(dependencyDir);

                    // Create the assembly dependency
                    string dependencyPath = Path.Combine(dependencyDir, $"{DependencyName}.dll");
                    File.WriteAllText(dependencyPath, string.Empty);

                    // Create the native dependency
                    string nativeDependencyPath = Path.Combine(dependencyDir, nativeDependencyRelPath);
                    string nativeDependencyDir = Path.GetDirectoryName(nativeDependencyPath);
                    Directory.CreateDirectory(nativeDependencyDir);
                    File.WriteAllText(nativeDependencyPath, string.Empty);

                    return (dependencyPath, nativeDependencyDir);
                }
            }
        }
    }
}
