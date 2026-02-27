// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Build;
using Xunit;
using static Microsoft.DotNet.CoreSetup.Test.NetCoreAppBuilder;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public class LocalPath : IClassFixture<LocalPath.SharedTestState>
    {
        private readonly SharedTestState sharedState;

        public LocalPath(SharedTestState sharedState)
        {
            this.sharedState = sharedState;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RuntimeAssemblies_FrameworkDependent(bool useLocalPath) => RuntimeAssemblies(isSelfContained: false, useLocalPath);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RuntimeAssemblies_SelfContained(bool useLocalPath) => RuntimeAssemblies(isSelfContained: true, useLocalPath);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NativeLibraries_FrameworkDependent(bool useLocalPath) => NativeLibraries(isSelfContained: false, useLocalPath);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NativeLibraries_SelfContained(bool useLocalPath) => NativeLibraries(isSelfContained: true, useLocalPath);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ResourceAssemblies_FrameworkDependent(bool useLocalPath) => ResourceAssemblies(isSelfContained: false, useLocalPath);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ResourceAssemblies_SelfContained(bool useLocalPath) => ResourceAssemblies(isSelfContained: true, useLocalPath);

        private void RuntimeAssemblies(bool isSelfContained, bool useLocalPath)
        {
            RuntimeLibraryType[] libraryTypes = [ RuntimeLibraryType.project, RuntimeLibraryType.package, RuntimeLibraryType.runtimepack ];

            Action<NetCoreAppBuilder> customizer = b =>
            {
                foreach (var libraryType in libraryTypes)
                {
                    string library = $"Test{libraryType}";
                    (string path, string localPath) = GetPaths(libraryType, false);
                    b.WithRuntimeLibrary(libraryType, library, "1.0.0", p => p
                        .WithAssemblyGroup(null, g => g
                            .WithAsset(path, useLocalPath ? f => f.WithLocalPath(localPath) : null)));

                    if (!isSelfContained)
                    {
                        // Add RID-specific assembly
                        (string ridPath, string localRidPath) = GetPaths(libraryType, true);
                        b.WithRuntimeLibrary(libraryType, $"{library}-{HostTestContext.BuildRID}", "1.0.0", p => p
                            .WithAssemblyGroup(HostTestContext.BuildRID, g => g
                                .WithAsset(ridPath, useLocalPath ? f => f.WithLocalPath(localRidPath) : null)));
                    }
                }

                b.WithLocalPathsInDepsJson(useLocalPath);
            };

            using TestApp app = CreateApp(isSelfContained, customizer);
            var result = sharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute();
            result.Should().Pass();

            // Check all library types
            foreach (var libraryType in libraryTypes)
            {
                // Check RID-agnostic assembly
                (string path, string localPath) = GetPaths(libraryType, false);

                // Without localPath, RID-agnostic non-runtimepack runtime assemblies are assumed to be in <app_directory>
                string relativePath = useLocalPath
                    ? localPath
                    : libraryType == RuntimeLibraryType.runtimepack ? path : Path.GetFileName(path);
                string expectedPath = Path.Join(app.Location, relativePath);
                result.Should().HaveResolvedAssembly(expectedPath);
                if (useLocalPath)
                {
                    result.Should().NotHaveResolvedAssembly(Path.Join(app.Location, path));
                }

                // Check RID-specific assembly
                if (!isSelfContained)
                {
                    (string ridPath, string localRidPath) = GetPaths(libraryType, true);
                    string expectedRidPath = Path.Join(app.Location, useLocalPath ? localRidPath : ridPath);
                    result.Should().HaveResolvedAssembly(expectedRidPath);
                    if (useLocalPath)
                    {
                        result.Should().NotHaveResolvedAssembly(Path.Join(app.Location, ridPath));
                    }
                }
            }

            static (string Path, string LocalPath) GetPaths(RuntimeLibraryType libraryType, bool useRid)
            {
                string library = $"Test{libraryType}";
                string path = useRid ? $"lib/{HostTestContext.BuildRID}/{library}-{HostTestContext.BuildRID}.dll" : $"lib/{library}.dll";
                return (path, $"{libraryType}/{path}");
            }
        }

        private void NativeLibraries(bool isSelfContained, bool useLocalPath)
        {
            NetCoreAppBuilder.RuntimeLibraryType[] libraryTypes = [NetCoreAppBuilder.RuntimeLibraryType.project, NetCoreAppBuilder.RuntimeLibraryType.package, NetCoreAppBuilder.RuntimeLibraryType.runtimepack];

            Action<NetCoreAppBuilder> customizer = b =>
            {
                foreach (var libraryType in libraryTypes)
                {
                    string library = $"Test{libraryType}";
                    (string path, string localPath) = GetPaths(libraryType, false);
                    b.WithRuntimeLibrary(libraryType, library, "1.0.0", p => p
                        .WithNativeLibraryGroup(null, g => g
                            .WithAsset($"{path}/{library}.native", useLocalPath ? f => f.WithLocalPath($"{localPath}/{library}.native") : null)));

                    if (!isSelfContained)
                    {
                        // Add RID-specific native library
                        (string ridPath, string localRidPath) = GetPaths(libraryType, true);
                        b.WithRuntimeLibrary(libraryType, $"{library}-{HostTestContext.BuildRID}", "1.0.0", p => p
                            .WithNativeLibraryGroup(HostTestContext.BuildRID, g => g
                                .WithAsset($"{ridPath}/{library}-{HostTestContext.BuildRID}.native", useLocalPath ? f => f.WithLocalPath($"{localRidPath}/{library}-{HostTestContext.BuildRID}.native") : null)));
                    }
                }

                b.WithLocalPathsInDepsJson(useLocalPath);
            };

            using TestApp app = CreateApp(isSelfContained, customizer);
            var result = sharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute();
            result.Should().Pass();

            // Check all library types
            foreach (NetCoreAppBuilder.RuntimeLibraryType libraryType in libraryTypes)
            {
                // Check RID-agnostic native library path
                (string path, string localPath) = GetPaths(libraryType, false);

                // Without localPath, RID-agnostic non-runtimepack native libraries are assumed to be in <app_directory>
                string relativePath = useLocalPath
                    ? localPath
                    : libraryType == RuntimeLibraryType.runtimepack ? path : string.Empty;
                string expectedPath = Path.Join(app.Location, relativePath);
                result.Should().HaveResolvedNativeLibraryPath(expectedPath);
                if (useLocalPath)
                {
                    result.Should().NotHaveResolvedNativeLibraryPath(Path.Join(app.Location, path));
                }

                // Check RID-specific native library path
                if (!isSelfContained)
                {
                    (string ridPath, string localRidPath) = GetPaths(libraryType, true);
                    string expectedRidPath = Path.Join(app.Location, useLocalPath ? localRidPath : ridPath);
                    result.Should().HaveResolvedNativeLibraryPath(expectedRidPath);
                    if (useLocalPath)
                    {
                        result.Should().NotHaveResolvedNativeLibraryPath(Path.Join(app.Location, ridPath));
                    }
                }
            }

            static (string Path, string LocalPath) GetPaths(NetCoreAppBuilder.RuntimeLibraryType libraryType, bool useRid)
            {
                string path = useRid ? $"native/{HostTestContext.BuildRID}" : "native";
                return (path, $"{libraryType}/{path}");
            }
        }

        private void ResourceAssemblies(bool isSelfContained, bool useLocalPath)
        {
            NetCoreAppBuilder.RuntimeLibraryType[] libraryTypes = [NetCoreAppBuilder.RuntimeLibraryType.project, NetCoreAppBuilder.RuntimeLibraryType.package, NetCoreAppBuilder.RuntimeLibraryType.runtimepack];

            Action<NetCoreAppBuilder> customizer = b =>
            {
                foreach (var libraryType in libraryTypes)
                {
                    string library = $"Test{libraryType}";
                    (string path, string localPath) = GetPaths(libraryType);
                    b.WithRuntimeLibrary(libraryType, library, "1.0.0", p => p
                        .WithResourceAssembly($"{path}/fr/{library}.resources.dll", useLocalPath ? f => f.WithLocalPath($"{localPath}/fr/{library}.resources.dll") : null));
                }

                b.WithLocalPathsInDepsJson(useLocalPath);
            };

            using TestApp app = CreateApp(isSelfContained, customizer);
            var result = sharedState.DotNetWithNetCoreApp.Exec(app.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute();
            result.Should().Pass();

            // Check all library types
            foreach (var libraryType in libraryTypes)
            {
                (string path, string localPath) = GetPaths(libraryType);

                // Without localPath, non-runtimepack resource assemblies are assumed to be in <app_directory>/<locale>/
                string relativePath = useLocalPath
                    ? localPath
                    : libraryType == RuntimeLibraryType.runtimepack ? path : string.Empty;
                string expectedPath = Path.Join(app.Location, relativePath);
                result.Should().HaveResolvedResourceRootPath(expectedPath);
                if (useLocalPath)
                {
                    result.Should().NotHaveResolvedResourceRootPath(Path.Join(app.Location, path));
                }
            }

            static (string Path, string LocalPath) GetPaths(NetCoreAppBuilder.RuntimeLibraryType libraryType)
            {
                string path = $"resources";
                return (path, $"{libraryType}/{path}");
            }
        }

        private static TestApp CreateApp(bool isSelfContained, Action<NetCoreAppBuilder> customizer)
        {
            TestApp app = TestApp.CreateEmpty("App");
            if (isSelfContained)
            {
                app.PopulateSelfContained(TestApp.MockedComponent.CoreClr, customizer);
            }
            else
            {
                app.PopulateFrameworkDependent(Constants.MicrosoftNETCoreApp, HostTestContext.MicrosoftNETCoreAppVersion, customizer);
            }
            return app;
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
