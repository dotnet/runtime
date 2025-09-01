// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public class DepsFile : IClassFixture<DepsFile.SharedTestState>
    {
        private readonly SharedTestState sharedState;

        public DepsFile(SharedTestState sharedState)
        {
            this.sharedState = sharedState;
        }

        [Fact]
        public void NoDepsJson()
        {
            // Without .deps.json, all assemblies in the app's directory are added to the TPA
            // and the app's directory is added to the native library search path
            TestApp app = sharedState.FrameworkReferenceApp;
            sharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveResolvedAssembly(Path.Combine(app.Location, $"{SharedTestState.DependencyName}.dll"))
                .And.HaveResolvedNativeLibraryPath(app.Location);
        }

        [Fact]
        public void SeparateDepsJson()
        {
            // For framework-dependent apps, the probing directories are:
            // - The directory where the .deps.json is
            // - Any framework directory
            // Dependency should resolve relative to the .deps.json directory without checking for file existence
            string dependencyPath = Path.Combine(Path.GetDirectoryName(sharedState.DepsJsonPath), $"{SharedTestState.DependencyName}.dll");
            sharedState.DotNetWithNetCoreApp.Exec("exec", Constants.DepsFile.CommandLineArgument, sharedState.DepsJsonPath, sharedState.FrameworkReferenceApp.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveResolvedAssembly(dependencyPath);
        }

        [Fact]
        public void DepsJsonWithUtf8Bom()
        {
            // Test that .deps.json files with UTF8 BOM are parsed correctly
            TestApp app = sharedState.FrameworkReferenceApp;

            // Create a copy of the existing deps.json with UTF8 BOM
            string depsJsonWithBom = Path.Combine(Path.GetDirectoryName(sharedState.DepsJsonPath),
                Path.GetFileNameWithoutExtension(sharedState.DepsJsonPath) + "_bom.deps.json");

            try
            {
                // Read the original deps.json content and write with UTF8 BOM
                string jsonContent = File.ReadAllText(sharedState.DepsJsonPath, Encoding.UTF8);
                FileUtils.WriteAllTextWithUtf8Bom(depsJsonWithBom, jsonContent);

                // Test that the app can be executed with the BOM deps.json
                string dependencyPath = Path.Combine(Path.GetDirectoryName(sharedState.DepsJsonPath), $"{SharedTestState.DependencyName}.dll");
                sharedState.DotNetWithNetCoreApp.Exec("exec", Constants.DepsFile.CommandLineArgument, depsJsonWithBom, app.AppDll)
                    .EnableTracingAndCaptureOutputs()
                    .Execute()
                    .Should().Pass()
                    .And.HaveResolvedAssembly(dependencyPath);
            }
            finally
            {
                // Clean up
                FileUtils.DeleteFileIfPossible(depsJsonWithBom);
            }
        }

        public class SharedTestState : SharedTestStateBase
        {
            public DotNetCli DotNetWithNetCoreApp { get; }

            public TestApp FrameworkReferenceApp { get; }

            public const string DependencyName = "Dependency";

            public string DepsJsonPath { get; }

            public SharedTestState()
            {
                DotNetWithNetCoreApp = DotNet("WithNetCoreApp")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr(TestContext.MicrosoftNETCoreAppVersion)
                    .Build();

                FrameworkReferenceApp = CreateFrameworkReferenceApp(Constants.MicrosoftNETCoreApp, TestContext.MicrosoftNETCoreAppVersion, b => b
                    .WithProject(DependencyName, "1.0.0", p => p
                        .WithAssemblyGroup(null, g => g.WithAsset($"{DependencyName}.dll"))));

                var depsDir = Path.Combine(Location, "deps");
                Directory.CreateDirectory(depsDir);
                DepsJsonPath = Path.Combine(depsDir, Path.GetFileName(FrameworkReferenceApp.DepsJson));
                File.Move(FrameworkReferenceApp.DepsJson, DepsJsonPath);
            }
        }
    }
}
