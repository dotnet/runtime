// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public class AdditionalDeps : IClassFixture<AdditionalDeps.SharedTestState>
    {
        private SharedTestState SharedState { get; }

        // Shared state has a NetCoreApp with the two versions below
        public const string NetCoreAppVersion = "4.1.1";
        public const string NetCoreAppVersionPreview = "4.1.2-preview.2";

        public AdditionalDeps(SharedTestState sharedState)
        {
            SharedState = sharedState;
        }

        // Additional deps can point to a directory. The host looks for deps.json files in:
        //   <additional_deps_dir>/shared/<fx_name>/<version>
        // It uses the version closest to the framework version with a matching major/minor
        // and equal or lesser patch, release or pre-release.
        [Theory]
        // exact match
        [InlineData("4.1.1",            new string[] { "4.1.0", "4.1.1" },              "4.1.1")]
        [InlineData("4.1.2-preview.2",  new string[] { "4.1.1", "4.1.2-preview.2" },    "4.1.2-preview.2")]
        // lower patch version
        [InlineData("4.1.1",            new string[] { "4.1.0", "4.1.2-preview.1" },    "4.1.0")]
        [InlineData("4.1.2-preview.2",  new string[] { "4.1.1", "4.1.2" },              "4.1.1")]
        // lower prerelease
        [InlineData("4.1.1",            new string[] { "4.1.0", "4.1.1-preview.1" },    "4.1.1-preview.1")]
        [InlineData("4.1.2-preview.2",  new string[] { "4.1.1", "4.1.2-preview.1" },    "4.1.2-preview.1")]
        // no match
        [InlineData("4.1.1",            new string[] { "4.0.0", "4.1.2", "4.2.0" },     null)]
        [InlineData("4.1.2-preview.2",  new string[] { "4.0.0", "4.1.2", "4.2.0" },     null)]
        public void DepsDirectory(string fxVersion, string[] versions, string usedVersion)
        {
            using (TestArtifact additionalDeps = TestArtifact.Create("additionalDeps"))
            {
                string depsJsonName = Path.GetFileName(SharedState.AdditionalDepsComponent.DepsJson);
                foreach (string version in versions)
                {
                    string path = Path.Combine(additionalDeps.Location, "shared", Constants.MicrosoftNETCoreApp, version);
                    Directory.CreateDirectory(path);
                    File.Copy(
                        SharedState.AdditionalDepsComponent.DepsJson,
                        Path.Combine(path, depsJsonName),
                        true);
                }

                TestApp app = SharedState.FrameworkReferenceApp;
                if (fxVersion != NetCoreAppVersion)
                {
                    // Make a copy of the app and update its framework version
                    app = SharedState.FrameworkReferenceApp.Copy();
                    RuntimeConfig.FromFile(app.RuntimeConfigJson)
                        .RemoveFramework(Constants.MicrosoftNETCoreApp)
                        .WithFramework(Constants.MicrosoftNETCoreApp, fxVersion)
                        .Save();
                }

                CommandResult result = SharedState.DotNetWithNetCoreApp.Exec(Constants.AdditionalDeps.CommandLineArgument, additionalDeps.Location, app.AppDll)
                    .EnableTracingAndCaptureOutputs()
                    .Execute();

                result.Should().Pass();
                if (string.IsNullOrEmpty(usedVersion))
                {
                    result.Should().HaveStdErrContaining($"No additional deps directory less than or equal to [{fxVersion}] found with same major and minor version.");
                }
                else
                {
                    result.Should().HaveUsedAdditionalDeps(Path.Combine(additionalDeps.Location, "shared", Constants.MicrosoftNETCoreApp, usedVersion, depsJsonName));
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DepsFile(bool dependencyExists)
        {
            string additionalLibName = SharedState.AdditionalDepsComponent.AssemblyName;
            string additionalDepsFile = SharedState.AdditionalDepsComponent.DepsJson;

            TestApp app = SharedState.FrameworkReferenceApp;
            if (!dependencyExists)
            {
                // Make a copy of the app and delete the app-local dependency
                app = SharedState.FrameworkReferenceApp.Copy();
                File.Delete(Path.Combine(app.Location, $"{additionalLibName}.dll"));
            }

            CommandResult result = SharedState.DotNetWithNetCoreApp.Exec(Constants.AdditionalDeps.CommandLineArgument, additionalDepsFile, app.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute(expectedToFail: !dependencyExists);

            result.Should().HaveUsedAdditionalDeps(additionalDepsFile);
            if (dependencyExists)
            {
                result.Should().Pass()
                    .And.HaveResolvedAssembly(Path.Combine(app.Location, $"{additionalLibName}.dll"));
            }
            else
            {
                // Specifying an additional deps file triggers file existence checking, so execution
                // should fail when the dependency doesn't exist.
                result.Should().Fail()
                    .And.ErrorWithMissingAssembly($"{additionalLibName}.deps.json", additionalLibName, "1.0.0");
            }
        }

        [Fact]
        public void InvalidJson()
        {
            string invalidDepsFile = Path.Combine(SharedState.Location, "invalid.deps.json");
            try
            {
                File.WriteAllText(invalidDepsFile, "{");

                SharedState.DotNetWithNetCoreApp.Exec(Constants.AdditionalDeps.CommandLineArgument, invalidDepsFile, SharedState.FrameworkReferenceApp.AppDll)
                    .EnableTracingAndCaptureOutputs()
                    .Execute(expectedToFail: true)
                    .Should().Fail()
                    .And.HaveUsedAdditionalDeps(invalidDepsFile)
                    .And.HaveStdErrContaining($"Error initializing the dependency resolver: An error occurred while parsing: {invalidDepsFile}");
            }
            finally
            {
                FileUtils.DeleteFileIfPossible(invalidDepsFile);
            }
        }

        public class SharedTestState : SharedTestStateBase
        {
            public DotNetCli DotNetWithNetCoreApp { get; }

            public TestApp FrameworkReferenceApp { get; }

            public TestApp AdditionalDepsComponent { get; }

            public SharedTestState()
            {
                DotNetWithNetCoreApp = DotNet("WithNetCoreApp")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr(NetCoreAppVersion)
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr(NetCoreAppVersionPreview)
                    .Build();

                AdditionalDepsComponent = CreateComponentWithNoDependencies();

                FrameworkReferenceApp = CreateFrameworkReferenceApp(Constants.MicrosoftNETCoreApp, NetCoreAppVersion);

                // Copy dependency next to app
                File.Copy(AdditionalDepsComponent.AppDll, Path.Combine(FrameworkReferenceApp.Location, $"{AdditionalDepsComponent.AssemblyName}.dll"));
            }
        }
    }
}
