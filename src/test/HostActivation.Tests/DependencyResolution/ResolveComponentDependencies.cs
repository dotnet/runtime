// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public class ResolveComponentDependencies : 
        DependencyResolutionBase,
        IClassFixture<ResolveComponentDependencies.SharedTestState>
    {
        private readonly SharedTestState sharedTestState;
        private readonly ITestOutputHelper output;

        public ResolveComponentDependencies(SharedTestState fixture, ITestOutputHelper output)
        {
            sharedTestState = fixture;
            this.output = output;
        }

        private const string corehost_resolve_component_dependencies = "corehost_resolve_component_dependencies";
        private const string corehost_resolve_component_dependencies_multithreaded = "corehost_resolve_component_dependencies_multithreaded";

        [Fact]
        public void InvalidMainComponentAssemblyPathFails()
        {
            RunTest(sharedTestState.HostApiInvokerAppFixture.TestProject.AppDll + "_invalid")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Fail[0x80008092]")
                .And.HaveStdOutContaining("corehost reported errors:")
                .And.HaveStdOutContaining("Failed to locate managed application");
        }

        [Fact]
        public void ComponentWithNoDependenciesAndNoDeps()
        {
            var component = sharedTestState.ComponentWithNoDependencies.Copy();

            // Remove .deps.json
            File.Delete(component.DepsJson);

            RunTest(component)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{component.AppDll}{Path.PathSeparator}]")
                .And.HaveStdErrContaining($"app_root='{component.Location}{Path.DirectorySeparatorChar}'")
                .And.HaveStdErrContaining($"deps='{component.DepsJson}'")
                .And.HaveStdErrContaining($"mgd_app='{component.AppDll}'")
                .And.HaveStdErrContaining($"-- arguments_t: dotnet shared store: '{Path.Combine(sharedTestState.HostApiInvokerAppFixture.BuiltDotnet.BinPath, "store", sharedTestState.RepoDirectories.BuildArchitecture, sharedTestState.HostApiInvokerAppFixture.Framework)}'");
        }

        [Fact]
        public void ComponentWithNoDependencies()
        {
            RunTest(sharedTestState.ComponentWithNoDependencies)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{sharedTestState.ComponentWithNoDependencies.AppDll}{Path.PathSeparator}]");
        }

        private static readonly string[] SupportedOsList = new string[]
        {
            "ubuntu",
            "debian",
            "fedora",
            "opensuse",
            "osx",
            "rhel",
            "win"
        };

        private string GetExpectedLibuvRid(TestProjectFixture fixture)
        {
            // Simplified version of the RID fallback for libuv
            // Note that we have to take the architecture from the fixture (since this test may run on x64 but the fixture on x86)
            // but we can't use the OS part from the fixture RID as that may be too generic (like linux-x64).
            string currentRid = PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier();
            string fixtureRid = fixture.CurrentRid;
            string osName = currentRid.Split('-')[0];
            string architecture = fixtureRid.Split('-')[1];

            string supportedOsName = SupportedOsList.FirstOrDefault(a => osName.StartsWith(a));
            if (supportedOsName == null)
            {
                return null;
            }

            osName = supportedOsName;
            if (osName == "ubuntu") { osName = "debian"; }
            if (osName == "win") { osName = "win7"; }
            if (osName == "osx") { return osName; }

            return osName + "-" + architecture;
        }

        [Fact]
        public void ComponentWithDependencies()
        {
            string libuvRid = GetExpectedLibuvRid(sharedTestState.HostApiInvokerAppFixture);
            if (libuvRid == null)
            {
                output.WriteLine($"RID {PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier()} is not supported by libuv and thus we can't run this test on it.");
                return;
            }

            RunTest(sharedTestState.ComponentWithDependencies)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{Path.Combine(sharedTestState.ComponentWithDependencies.Location, "ComponentDependency.dll")}{Path.PathSeparator}" +
                    $"{sharedTestState.ComponentWithDependencies.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(sharedTestState.ComponentWithDependencies.Location, "Newtonsoft.Json.dll")}{Path.PathSeparator}]")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies native_search_paths:[" +
                    $"{ExpectedProbingPaths(Path.Combine(sharedTestState.ComponentWithDependencies.Location, "runtimes", libuvRid, "native"))}]");
        }

        [Fact]
        public void ComponentWithDependenciesAndDependencyRemoved()
        {
            var component = sharedTestState.ComponentWithDependencies.Copy();

            // Remove a dependency
            // This will cause the resolution to fail
            File.Delete(Path.Combine(component.Location, "ComponentDependency.dll"));

            RunTest(component)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{component.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(component.Location, "Newtonsoft.Json.dll")}{Path.PathSeparator}]");
        }

        [Fact]
        public void ComponentWithDependenciesAndNoDeps()
        {
            var component = sharedTestState.ComponentWithDependencies.Copy();

            // Remove .deps.json
            File.Delete(component.DepsJson);

            RunTest(component)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{Path.Combine(component.Location, "ComponentDependency.dll")}{Path.PathSeparator}" +
                    $"{component.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(component.Location, "Newtonsoft.Json.dll")}{Path.PathSeparator}]");
        }

        [Fact]
        public void ComponentWithDependenciesAndNoDepsAndDependencyRemoved()
        {
            var component = sharedTestState.ComponentWithDependencies.Copy();

            // Remove .deps.json
            File.Delete(component.DepsJson);

            // Remove a dependency
            // Since there's no .deps.json - there's no way for the system to know about this dependency and thus should not be reported.
            File.Delete(Path.Combine(component.Location, "ComponentDependency.dll"));

            RunTest(component)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{component.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(component.Location, "Newtonsoft.Json.dll")}{Path.PathSeparator}]");
        }

        [Fact]
        public void ComponentWithSameDependencyWithDifferentExtensionShouldFail()
        {
            // Add a reference to another package which has asset with the same name as the existing ComponentDependency
            // but with a different extension. This causes a failure.
            // Make sure the file exists so that we avoid failing due to missing file.
            var component = sharedTestState.CreateComponentWithDependencies(b => b
                .WithPackage("ComponentDependency_Dupe", "1.0.0", p => p
                    .WithAssemblyGroup(null, g => g
                        .WithAsset("ComponentDependency.notdll"))));

            RunTest(component)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Fail[0x8000808C]")
                .And.HaveStdOutContaining("corehost reported errors:")
                .And.HaveStdOutContaining("An assembly specified in the application dependencies manifest (ComponentWithDependencies.deps.json) has already been found but with a different file extension")
                .And.HaveStdOutContaining("package: 'ComponentDependency_Dupe', version: '1.0.0'")
                .And.HaveStdOutContaining("path: 'ComponentDependency.notdll'")
                .And.HaveStdOutContaining($"previously found assembly: '{Path.Combine(component.Location, "ComponentDependency.dll")}'");
        }

        // This test also validates that corehost_set_error_writer custom writer
        // correctly captures errors from hostpolicy.
        [Fact]
        public void ComponentWithCorruptedDepsJsonShouldFail()
        {
            var component = sharedTestState.ComponentWithDependencies.Copy();

            // Corrupt the .deps.json by appending } to it (malformed json)
            File.WriteAllText(
                component.DepsJson,
                File.ReadAllLines(component.DepsJson) + "}");

            RunTest(component)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Fail[0x8000808B]")
                .And.HaveStdOutContaining("corehost reported errors:")
                .And.HaveStdOutContaining($"A JSON parsing exception occurred in [{component.DepsJson}]: * Line 1, Column 2 Syntax error: Malformed token")
                .And.HaveStdOutContaining($"Error initializing the dependency resolver: An error occurred while parsing: {component.DepsJson}");
        }

        [Fact]
        public void ComponentWithResourcesShouldReportResourceSearchPaths()
        {
            RunTest(sharedTestState.ComponentWithResources)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies resource_search_paths:[" +
                    $"{ExpectedProbingPaths(sharedTestState.ComponentWithResources.Location)}]");
        }

        private string ExpectedProbingPaths(params string[] paths)
        {
            string result = string.Empty;
            foreach (string path in paths)
            {
                string expectedPath = path;
                if (expectedPath.EndsWith(Path.DirectorySeparatorChar))
                {
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // On non-windows the paths are normalized to not end with a /
                        expectedPath = expectedPath.Substring(0, expectedPath.Length - 1);
                    }
                }
                else
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // On windows all paths are normalized to end with a \
                        expectedPath += Path.DirectorySeparatorChar;
                    }
                }

                result += expectedPath + Path.PathSeparator;
            }

            return result;
        }

        [Fact]
        public void AdditionalDepsDontAffectComponentDependencyResolution()
        {
            var component = sharedTestState.ComponentWithNoDependencies.Copy();

            string additionalDepsPath = Path.Combine(Path.GetDirectoryName(component.DepsJson), "__duplicate.deps.json");
            File.Copy(sharedTestState.HostApiInvokerAppFixture.TestProject.DepsJson, additionalDepsPath);

            RunTest(component, command => command
                .EnvironmentVariable("DOTNET_ADDITIONAL_DEPS", additionalDepsPath))
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{component.AppDll}{Path.PathSeparator}]");
        }

        [Fact]
        public void MultiThreadedComponentDependencyResolutionWhichSucceeeds()
        {
            string componentWithNoDependenciesPrefix = Path.GetFileNameWithoutExtension(sharedTestState.ComponentWithNoDependencies.AppDll);
            string componentWithResourcesPrefix = Path.GetFileNameWithoutExtension(sharedTestState.ComponentWithResources.AppDll);

            RunMultiThreadedTest(sharedTestState.ComponentWithNoDependencies, sharedTestState.ComponentWithResources)
                .Should().Pass()
                .And.HaveStdOutContaining($"{componentWithNoDependenciesPrefix}: corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"{componentWithNoDependenciesPrefix}: corehost_resolve_component_dependencies assemblies:[{sharedTestState.ComponentWithNoDependencies.AppDll}{Path.PathSeparator}]")
                .And.HaveStdOutContaining($"{componentWithResourcesPrefix}: corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"{componentWithResourcesPrefix}: corehost_resolve_component_dependencies resource_search_paths:[" +
                    $"{ExpectedProbingPaths(sharedTestState.ComponentWithResources.Location)}]");
        }

        [Fact]
        public void MultiThreadedComponentDependencyResolutionWhithFailures()
        {
            var componentWithNoDependencies = sharedTestState.ComponentWithNoDependencies.Copy();

            string componentWithNoDependenciesPrefix = Path.GetFileNameWithoutExtension(componentWithNoDependencies.AppDll);
            string componentWithResourcesPrefix = Path.GetFileNameWithoutExtension(sharedTestState.ComponentWithResources.AppDll);

            // Corrupt the .deps.json by appending } to it (malformed json)
            File.WriteAllText(
                componentWithNoDependencies.DepsJson,
                File.ReadAllLines(componentWithNoDependencies.DepsJson) + "}");

            RunMultiThreadedTest(
                componentWithNoDependencies.AppDll,
                sharedTestState.ComponentWithResources.AppDll + "_invalid")
                .Should().Pass()
                .And.HaveStdOutContaining($"{componentWithNoDependenciesPrefix}: corehost_resolve_component_dependencies:Fail[0x8000808B]")
                .And.HaveStdOutContaining($"{componentWithNoDependenciesPrefix}: corehost reported errors:")
                .And.HaveStdOutContaining($"{componentWithNoDependenciesPrefix}: A JSON parsing exception occurred in [{componentWithNoDependencies.DepsJson}]: * Line 1, Column 2 Syntax error: Malformed token")
                .And.HaveStdOutContaining($"{componentWithNoDependenciesPrefix}: Error initializing the dependency resolver: An error occurred while parsing: {componentWithNoDependencies.DepsJson}")
                .And.HaveStdOutContaining($"{componentWithResourcesPrefix}: corehost_resolve_component_dependencies:Fail[0x80008092]")
                .And.HaveStdOutContaining($"{componentWithResourcesPrefix}: corehost reported errors:")
                .And.HaveStdOutContaining($"{componentWithResourcesPrefix}: Failed to locate managed application");
        }

        private CommandResult RunTest(TestApp component, Action<Command> commandCustomizer = null)
        {
            return RunTest(component.AppDll, commandCustomizer);
        }

        private CommandResult RunTest(string componentPath, Action<Command> commandCustomizer = null)
        {
            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentPath
            };

            Command command = sharedTestState.HostApiInvokerAppFixture.BuiltDotnet.Exec(sharedTestState.HostApiInvokerAppFixture.TestProject.AppDll, args)
                .EnableTracingAndCaptureOutputs();
            commandCustomizer?.Invoke(command);

            return command.Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {");
        }

        private CommandResult RunMultiThreadedTest(TestApp componentOne, TestApp componentTwo)
        {
            return RunMultiThreadedTest(componentOne.AppDll, componentTwo.AppDll);
        }

        private CommandResult RunMultiThreadedTest(string componentOnePath, string componentTwoPath)
        {
            string[] args =
            {
                corehost_resolve_component_dependencies_multithreaded,
                componentOnePath,
                componentTwoPath
            };
            return sharedTestState.HostApiInvokerAppFixture.BuiltDotnet.Exec(sharedTestState.HostApiInvokerAppFixture.TestProject.AppDll, args)
                .EnableTracingAndCaptureOutputs()
                .Execute();
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestProjectFixture HostApiInvokerAppFixture { get; }
            public TestApp ComponentWithNoDependencies { get; }
            public TestApp ComponentWithDependencies { get; }
            public TestApp ComponentWithResources { get; }
            public RepoDirectoriesProvider RepoDirectories { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                HostApiInvokerAppFixture = new TestProjectFixture("HostApiInvokerApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();

                ComponentWithNoDependencies = CreateComponentWithNoDependencies(null, Location);

                ComponentWithDependencies = CreateComponentWithDependencies(null, Location);

                ComponentWithResources = CreateComponentWithResources(null, Location);

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // On non-Windows, we can't just P/Invoke to already loaded hostpolicy, so copy it next to the app dll.
                    var fixture = HostApiInvokerAppFixture;
                    var hostpolicy = Path.Combine(
                        fixture.BuiltDotnet.GreatestVersionSharedFxPath,
                        RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy"));

                    FileUtils.CopyIntoDirectory(
                        hostpolicy,
                        Path.GetDirectoryName(fixture.TestProject.AppDll));
                }
            }

            private TestApp CreateTestApp(string location, string name)
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
                FileUtils.EnsureDirectoryExists(componentWithNoDependencies.Location);
                NetCoreAppBuilder builder = NetCoreAppBuilder.PortableForNETCoreApp(componentWithNoDependencies)
                    .WithProject(p => p.WithAssemblyGroup(null, g => g.WithMainAssembly()));
                customizer?.Invoke(builder);

                return builder.Build(componentWithNoDependencies);
            }

            public TestApp CreateComponentWithDependencies(Action<NetCoreAppBuilder> customizer = null, string location = null)
            {
                TestApp componentWithDependencies = CreateTestApp(location, "ComponentWithDependencies");
                FileUtils.EnsureDirectoryExists(componentWithDependencies.Location);
                NetCoreAppBuilder builder = NetCoreAppBuilder.PortableForNETCoreApp(componentWithDependencies)
                    .WithProject(p => p.WithAssemblyGroup(null, g => g.WithMainAssembly()))
                    .WithProject("ComponentDependency", "1.0.0", p => p.WithAssemblyGroup(null, g => g.WithAsset("ComponentDependency.dll")))
                    .WithPackage("Newtonsoft.Json", "9.0.1", p => p.WithAssemblyGroup(null, g => g
                        .WithAsset("lib/netstandard1.0/Newtonsoft.Json.dll", f => f
                            .WithVersion("9.0.0.0", "9.0.1.19813")
                            .WithFileOnDiskPath("Newtonsoft.Json.dll"))))
                    .WithPackage("Libuv", "1.9.1", p => p
                        .WithNativeLibraryGroup("debian-x64", g => g.WithAsset("runtimes/debian-x64/native/libuv.so"))
                        .WithNativeLibraryGroup("fedora-x64", g => g.WithAsset("runtimes/fedora-x64/native/libuv.so"))
                        .WithNativeLibraryGroup("opensuse-x64", g => g.WithAsset("runtimes/opensuse-x64/native/libuv.so"))
                        .WithNativeLibraryGroup("osx", g => g.WithAsset("runtimes/osx/native/libuv.dylib"))
                        .WithNativeLibraryGroup("rhel-x64", g => g.WithAsset("runtimes/rhel-x64/native/libuv.so"))
                        .WithNativeLibraryGroup("win7-arm", g => g.WithAsset("runtimes/win7-arm/native/libuv.dll"))
                        .WithNativeLibraryGroup("win7-x64", g => g.WithAsset("runtimes/win7-x64/native/libuv.dll"))
                        .WithNativeLibraryGroup("win7-x86", g => g.WithAsset("runtimes/win7-x86/native/libuv.dll")));
                customizer?.Invoke(builder);

                return builder.Build(componentWithDependencies);
            }

            public TestApp CreateComponentWithResources(Action<NetCoreAppBuilder> customizer = null, string location = null)
            {
                TestApp componentWithResources = CreateTestApp(location, "ComponentWithResources");
                NetCoreAppBuilder builder = NetCoreAppBuilder.PortableForNETCoreApp(componentWithResources)
                    .WithProject(p => p
                        .WithAssemblyGroup(null, g => g.WithMainAssembly())
                        .WithResourceAssembly("en/ComponentWithResources.resources.dll"));

                customizer?.Invoke(builder);

                return builder.Build(componentWithResources);
            }

            public override void Dispose()
            {
                base.Dispose();

                HostApiInvokerAppFixture.Dispose();
            }
        }
    }
}