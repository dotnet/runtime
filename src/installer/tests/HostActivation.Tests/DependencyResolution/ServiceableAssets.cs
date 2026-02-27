// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Build;
using Xunit;
using static Microsoft.DotNet.CoreSetup.Test.NetCoreAppBuilder;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    [SkipOnPlatform(TestPlatforms.Windows, "On Windows, the servicing location is %ProgramFiles%\\coreservicing. Writing to it would require admin privileges.")]
    public class ServiceableAssets : IClassFixture<ServiceableAssets.SharedTestState>
    {
        private readonly SharedTestState sharedState;

        public ServiceableAssets(SharedTestState fixture)
        {
            sharedState = fixture;
        }

        private class LibraryAsset
        {
            private const string LibraryVersion = "1.0.0";
            public LibraryAsset(string name, string path)
            {
                Name = name;
                Path = path;
                Version = LibraryVersion;
            }

            public string Name { get; }
            public string Version { get; }
            public string Path { get; }
            public string ServicedPath { get; set; }
        }

        [Fact]
        public void RuntimeAssemblies()
        {
            using TestArtifact artifact = TestArtifact.Create(nameof(RuntimeAssemblies));
            TestApp app = new(Path.Combine(artifact.Location, "app"));

            var serviceableLib = new LibraryAsset("ServiceableLib", $"lib/{HostTestContext.Tfm}/ServiceableLib.dll");
            var nonServiceableLib = new LibraryAsset("NonServiceableLib", $"lib/{HostTestContext.Tfm}/NonServiceableLib.dll");

            app.PopulateFrameworkDependent(Constants.MicrosoftNETCoreApp, HostTestContext.MicrosoftNETCoreAppVersion, b =>
            {
                b.WithPackage(serviceableLib.Name, serviceableLib.Version, lib => lib
                    .WithAssemblyGroup(string.Empty, asm => asm
                        .WithAsset(serviceableLib.Path, f => f
                            .WithLocalPath(Path.GetFileName(serviceableLib.Path))))
                    .WithServiceable(true));

                b.WithPackage(nonServiceableLib.Name, nonServiceableLib.Version, lib => lib
                    .WithAssemblyGroup(string.Empty, asm => asm
                        .WithAsset(nonServiceableLib.Path, f => f
                            .WithLocalPath(Path.GetFileName(nonServiceableLib.Path))))
                    .WithServiceable(false));
            });

            string servicingDir = SetUpServicingDirectory(artifact.Location, serviceableLib, nonServiceableLib);
            string serviceableAppPath = Path.Join(app.Location, Path.GetFileName(serviceableLib.Path));
            string nonServiceableAppPath = Path.Join(app.Location, Path.GetFileName(nonServiceableLib.Path));

            sharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                .EnvironmentVariable(Constants.CoreServicing.EnvironmentVariable, servicingDir)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                // Serviceable library should be resolved from servicing directory, not app directory
                .And.HaveResolvedAssembly(serviceableLib.ServicedPath)
                .And.NotHaveResolvedAssembly(serviceableAppPath)
                // Non-serviceable library should be resolved from app directory, not servicing directory
                .And.HaveResolvedAssembly(nonServiceableAppPath)
                .And.NotHaveResolvedAssembly(nonServiceableLib.ServicedPath);
        }

        [Fact]
        public void NativeLibraries()
        {
            using TestArtifact artifact = TestArtifact.Create(nameof(NativeLibraries));
            TestApp app = new(Path.Combine(artifact.Location, "app"));

            var serviceableLib = new LibraryAsset("ServiceableNativeLib", "serviceable/ServiceableNativeLib.native");
            var nonServiceableLib = new LibraryAsset("NonServiceableNativeLib", "nonServiceable/NonServiceableNativeLib.native");

            app.PopulateFrameworkDependent(Constants.MicrosoftNETCoreApp, HostTestContext.MicrosoftNETCoreAppVersion, b =>
            {
                b.WithPackage(serviceableLib.Name, serviceableLib.Version, lib => lib
                    .WithNativeLibraryGroup(HostTestContext.BuildRID, nativeGroup => nativeGroup
                        .WithAsset(serviceableLib.Path))
                    .WithServiceable(true));

                b.WithPackage(nonServiceableLib.Name, nonServiceableLib.Version, lib => lib
                    .WithNativeLibraryGroup(HostTestContext.BuildRID, nativeGroup => nativeGroup
                        .WithAsset(nonServiceableLib.Path))
                    .WithServiceable(false));
            });

            string servicingDir = SetUpServicingDirectory(artifact.Location, serviceableLib, nonServiceableLib);
            string serviceableAppPath = Path.Join(app.Location, serviceableLib.Path);
            string nonServiceableAppPath = Path.Join(app.Location, nonServiceableLib.Path);

            sharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                .EnvironmentVariable(Constants.CoreServicing.EnvironmentVariable, servicingDir)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                // Serviceable native library should be resolved from servicing directory, not app directory
                .And.HaveResolvedNativeLibraryPath(Path.GetDirectoryName(serviceableLib.ServicedPath))
                .And.NotHaveResolvedNativeLibraryPath(Path.GetDirectoryName(serviceableAppPath))
                // Non-serviceable native library should be resolved from app directory, not servicing directory
                .And.HaveResolvedNativeLibraryPath(Path.GetDirectoryName(nonServiceableAppPath))
                .And.NotHaveResolvedNativeLibraryPath(Path.GetDirectoryName(nonServiceableLib.ServicedPath));
        }

        [Fact]
        public void ResourceAssemblies()
        {
            using TestArtifact artifact = TestArtifact.Create(nameof(ResourceAssemblies));
            TestApp app = new(Path.Combine(artifact.Location, "app"));

            var serviceableLib = new LibraryAsset("ServiceableResourceLib", "serviceable/fr/ServiceableResourceLib.resources.dll");
            var nonServiceableLib = new LibraryAsset("NonServiceableResourceLib", "nonServiceable/fr/NonServiceableResourceLib.resources.dll");

            app.PopulateFrameworkDependent(Constants.MicrosoftNETCoreApp, HostTestContext.MicrosoftNETCoreAppVersion, b =>
            {
                b.WithPackage(serviceableLib.Name, serviceableLib.Version, lib => lib
                    .WithResourceAssembly(serviceableLib.Path, f => f
                        .WithLocalPath(serviceableLib.Path))
                    .WithServiceable(true));

                b.WithPackage(nonServiceableLib.Name, nonServiceableLib.Version, lib => lib
                    .WithResourceAssembly(nonServiceableLib.Path, f => f
                        .WithLocalPath(nonServiceableLib.Path))
                    .WithServiceable(false));
            });

            string servicingDir = SetUpServicingDirectory(artifact.Location, serviceableLib, nonServiceableLib);
            string serviceableAppPath = Path.Join(app.Location, serviceableLib.Path);
            string nonServiceableAppPath = Path.Join(app.Location, nonServiceableLib.Path);

            sharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                .EnvironmentVariable(Constants.CoreServicing.EnvironmentVariable, servicingDir)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                // Serviceable resource assembly should be resolved from servicing directory, not app directory
                .And.HaveResolvedResourceRootPath(GetResourceRoot(serviceableLib.ServicedPath))
                .And.NotHaveResolvedResourceRootPath(GetResourceRoot(serviceableAppPath))
                // Non-serviceable resource assembly should be resolved from app directory, not servicing directory
                .And.HaveResolvedResourceRootPath(GetResourceRoot(nonServiceableAppPath))
                .And.NotHaveResolvedResourceRootPath(GetResourceRoot(nonServiceableLib.ServicedPath));

            static string GetResourceRoot(string path)
            {
                // <root>/<locale>/<resource.dll>
                return Path.GetDirectoryName(Path.GetDirectoryName(path));
            }
        }

        private string SetUpServicingDirectory(string directory, params LibraryAsset[] libraryAssets)
        {
            string servicingDir = Path.Combine(directory, "servicing");
            foreach (var asset in libraryAssets)
            {
                string servicedAsset = Path.Combine(servicingDir, "pkgs", asset.Name, asset.Version, asset.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(servicedAsset));
                File.WriteAllText(servicedAsset, string.Empty);
                asset.ServicedPath = servicedAsset;
            }

            return servicingDir;
        }

        public class SharedTestState : SharedTestStateBase
        {
            public DotNetCli DotNetWithNetCoreApp { get; }

            public SharedTestState()
            {
                DotNetWithNetCoreApp = DotNet("WithNetCoreApp")
                    .AddMicrosoftNETCoreAppFrameworkMockCoreClr(HostTestContext.MicrosoftNETCoreAppVersion)
                    .Build();
            }
        }
    }
}
