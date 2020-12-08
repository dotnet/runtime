// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public abstract class DependencyResolutionBase
    {
        protected const string MicrosoftNETCoreApp = "Microsoft.NETCore.App";

        public abstract class SharedTestStateBase : TestArtifact
        {
            protected string BuiltDotnetPath { get; }

            public RepoDirectoriesProvider RepoDirectories { get; }

            private static string GetBaseDir(string name)
            {
                string baseDir = Path.Combine(TestArtifactsPath, name);
                return SharedFramework.CalculateUniqueTestDirectory(baseDir);
            }

            public SharedTestStateBase()
                : base(GetBaseDir("dependencyResolution"), "dependencyResolution")
            {
                BuiltDotnetPath = Path.Combine(TestArtifactsPath, "sharedFrameworkPublish");
                RepoDirectories = new RepoDirectoriesProvider(builtDotnet: BuiltDotnetPath);
            }

            public DotNetBuilder DotNet(string name)
            {
                return new DotNetBuilder(Location, BuiltDotnetPath, name);
            }

            public TestApp CreateFrameworkReferenceApp(string fxName, string fxVersion)
            {
                // Prepare the app mock - we're not going to run anything really, so we just need the basic files
                TestApp testApp = CreateTestApp(Location, "FrameworkReferenceApp");
                RuntimeConfig.Path(testApp.RuntimeConfigJson)
                    .WithFramework(fxName, fxVersion)
                    .Save();

                return testApp;
            }

            protected TestApp CreateTestApp(string location, string name)
            {
                TestApp testApp;
                if (location == null)
                {
                    testApp = TestApp.CreateEmpty(name);
                }
                else
                {
                    string path = Path.Combine(location, name);
                    FileUtils.EnsureDirectoryExists(path);
                    testApp = new TestApp(path);
                }

                RegisterCopy(testApp);
                return testApp;
            }

            public TestApp CreateComponentWithNoDependencies(Action<NetCoreAppBuilder> customizer = null, string location = null)
            {
                TestApp componentWithNoDependencies = CreateTestApp(location, "ComponentWithNoDependencies");
                NetCoreAppBuilder builder = NetCoreAppBuilder.PortableForNETCoreApp(componentWithNoDependencies)
                    .WithProject(p => p.WithAssemblyGroup(null, g => g.WithMainAssembly()));
                customizer?.Invoke(builder);

                return builder.Build(componentWithNoDependencies);
            }

            public TestApp CreateSelfContainedAppWithMockCoreClr(string name, string version, Action<NetCoreAppBuilder> customizer = null, string location = null)
            {
                TestApp testApp = CreateTestApp(location, name);

                string hostFxrFileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr");
                string hostPolicyFileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy");
                string coreclrFileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("coreclr");
                string mockCoreclrFileName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("mockcoreclr");

                string currentRid = RepoDirectories.TargetRID;

                NetCoreAppBuilder.ForNETCoreApp(name, currentRid)
                    .WithProject(name, version, p => p
                        .WithAssemblyGroup(null, g => g.WithMainAssembly()))
                    .WithPackage("runtimePack.Microsoft.NETCore.App", "1.0.0", p => p
                        .WithNativeLibraryGroup(null, g => g
                            // ./coreclr.dll - this is a mock, will not actually run CoreClr
                            .WithAsset((new NetCoreAppBuilder.RuntimeFileBuilder(coreclrFileName))
                                .CopyFromFile(Path.Combine(RepoDirectories.Artifacts, "corehost_test", mockCoreclrFileName))
                                .WithFileOnDiskPath(coreclrFileName))))
                    .WithPackage("runtimePack.Microsoft.NETCore.DotNetHostResolver", "1.0.0", p => p
                        .WithNativeLibraryGroup(null, g => g
                            // ./hostfxr.dll - this is the real component and will load hostpolicy library
                            .WithAsset((new NetCoreAppBuilder.RuntimeFileBuilder(hostFxrFileName))
                                .CopyFromFile(Path.Combine(RepoDirectories.Artifacts, "corehost", hostFxrFileName))
                                .WithFileOnDiskPath(hostFxrFileName))))
                    .WithPackage("runtimePack.Microsoft.NETCore.DotNetHostPolicy", "1.0.0", p => p
                        .WithNativeLibraryGroup(null, g => g
                            // ./hostpolicy.dll - this is the real component and will load CoreClr library
                            .WithAsset((new NetCoreAppBuilder.RuntimeFileBuilder(hostPolicyFileName))
                                .CopyFromFile(Path.Combine(RepoDirectories.Artifacts, "corehost", hostPolicyFileName))
                                .WithFileOnDiskPath(hostPolicyFileName))))
                    .WithCustomizer(customizer)
                    .WithRuntimeConfig(config => { })
                    .Build(testApp);

                return testApp;
            }
        }
    }
}
