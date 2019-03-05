// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHostApis
{
    public class GivenThatICareAboutComponentDependencyResolution : IClassFixture<GivenThatICareAboutComponentDependencyResolution.SharedTestState>
    {
        private SharedTestState sharedTestState;
        private readonly ITestOutputHelper output;

        public GivenThatICareAboutComponentDependencyResolution(SharedTestState fixture, ITestOutputHelper output)
        {
            sharedTestState = fixture;
            this.output = output;
        }

        private const string corehost_resolve_component_dependencies = "corehost_resolve_component_dependencies";
        private const string corehost_resolve_component_dependencies_multithreaded = "corehost_resolve_component_dependencies_multithreaded";

        [Fact]
        public void InvalidMainComponentAssemblyPathFails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();

            string[] args =
            {
                corehost_resolve_component_dependencies,
                fixture.TestProject.AppDll + "_invalid"
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Fail[0x80008092]")
                .And.HaveStdOutContaining("corehost reported errors:")
                .And.HaveStdOutContaining("Failed to locate managed application");
        }

        [Fact]
        public void ComponentWithNoDependenciesAndNoDeps()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture.Copy();

            // Remove .deps.json
            File.Delete(componentFixture.TestProject.DepsJson);

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{componentFixture.TestProject.AppDll}{Path.PathSeparator}]")
                .And.HaveStdErrContaining($"app_root='{componentFixture.TestProject.OutputDirectory}{Path.DirectorySeparatorChar}'")
                .And.HaveStdErrContaining($"deps='{componentFixture.TestProject.DepsJson}'")
                .And.HaveStdErrContaining($"mgd_app='{componentFixture.TestProject.AppDll}'")
                .And.HaveStdErrContaining($"-- arguments_t: dotnet shared store: '{Path.Combine(fixture.BuiltDotnet.BinPath, "store", sharedTestState.RepoDirectories.BuildArchitecture, fixture.Framework)}'");
        }

        [Fact]
        public void ComponentWithNoDependencies()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture.Copy();

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{componentFixture.TestProject.AppDll}{Path.PathSeparator}]");
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
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Copy();

            string libuvRid = GetExpectedLibuvRid(fixture);
            if (libuvRid == null)
            {
                output.WriteLine($"RID {PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier()} is not supported by libuv and thus we can't run this test on it.");
                return;
            }

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.dll")}{Path.PathSeparator}" +
                    $"{componentFixture.TestProject.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(componentFixture.TestProject.OutputDirectory, "Newtonsoft.Json.dll")}{Path.PathSeparator}]")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies native_search_paths:[" +
                    $"{ExpectedProbingPaths(Path.Combine(componentFixture.TestProject.OutputDirectory, "runtimes", libuvRid, "native"))}]");
        }

        [Fact]
        public void ComponentWithDependenciesAndDependencyRemoved()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Copy();

            // Remove a dependency
            // This will cause the resolution to fail
            File.Delete(Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.dll"));

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{componentFixture.TestProject.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(componentFixture.TestProject.OutputDirectory, "Newtonsoft.Json.dll")}{Path.PathSeparator}]");
        }

        [Fact]
        public void ComponentWithDependenciesAndNoDeps()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Copy();

            // Remove .deps.json
            File.Delete(componentFixture.TestProject.DepsJson);

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.dll")}{Path.PathSeparator}" +
                    $"{componentFixture.TestProject.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(componentFixture.TestProject.OutputDirectory, "Newtonsoft.Json.dll")}{Path.PathSeparator}]");
        }

        [Fact]
        public void ComponentWithDependenciesAndNoDepsAndDependencyRemoved()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Copy();

            // Remove .deps.json
            File.Delete(componentFixture.TestProject.DepsJson);

            // Remove a dependency
            // Since there's no .deps.json - there's no way for the system to know about this dependency and thus should not be reported.
            File.Delete(Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.dll"));

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{componentFixture.TestProject.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(componentFixture.TestProject.OutputDirectory, "Newtonsoft.Json.dll")}{Path.PathSeparator}]");
        }

        [Fact]
        public void ComponentWithSameDependencyWithDifferentExtensionShouldFail()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Copy();

            // Add a reference to another package which has asset with the same name as the existing ComponentDependency
            // but with a different extension. This causes a failure.
            SharedFramework.AddReferenceToDepsJson(
                componentFixture.TestProject.DepsJson,
                "ComponentWithDependencies/1.0.0",
                "ComponentDependency_Dupe",
                "1.0.0",
                testAssembly: "ComponentDependency.notdll");

            // Make sure the file exists so that we avoid failing due to missing file.
            File.Copy(
                Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.dll"),
                Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.notdll"));

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Fail[0x8000808C]")
                .And.HaveStdOutContaining("corehost reported errors:")
                .And.HaveStdOutContaining("An assembly specified in the application dependencies manifest (ComponentWithDependencies.deps.json) has already been found but with a different file extension")
                .And.HaveStdOutContaining("package: 'ComponentDependency_Dupe', version: '1.0.0'")
                .And.HaveStdOutContaining("path: 'ComponentDependency.notdll'")
                .And.HaveStdOutContaining($"previously found assembly: '{Path.Combine(componentFixture.TestProject.OutputDirectory, "ComponentDependency.dll")}'");
        }

        // This test also validates that corehost_set_error_writer custom writer
        // correctly captures errors from hostpolicy.
        [Fact]
        public void ComponentWithCorruptedDepsJsonShouldFail()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Copy();

            // Corrupt the .deps.json by appending } to it (malformed json)
            File.WriteAllText(
                componentFixture.TestProject.DepsJson,
                File.ReadAllLines(componentFixture.TestProject.DepsJson) + "}");

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Fail[0x8000808B]")
                .And.HaveStdOutContaining("corehost reported errors:")
                .And.HaveStdOutContaining($"A JSON parsing exception occurred in [{componentFixture.TestProject.DepsJson}]: * Line 1, Column 2 Syntax error: Malformed token")
                .And.HaveStdOutContaining($"Error initializing the dependency resolver: An error occurred while parsing: {componentFixture.TestProject.DepsJson}");
        }

        [Fact]
        public void ComponentWithResourcesShouldReportResourceSearchPaths()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithResourcesFixture.Copy();

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies resource_search_paths:[" +
                    $"{ExpectedProbingPaths(componentFixture.TestProject.OutputDirectory)}]");
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
                        expectedPath = expectedPath + Path.DirectorySeparatorChar;
                    }
                }

                result += expectedPath + Path.PathSeparator;
            }

            return result;
        }

        [Fact]
        public void AdditionalDepsDontAffectComponentDependencyResolution()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture.Copy();

            string additionalDepsPath = Path.Combine(Path.GetDirectoryName(fixture.TestProject.DepsJson), "__duplicate.deps.json");
            File.Copy(fixture.TestProject.DepsJson, additionalDepsPath);

            string[] args =
            {
                corehost_resolve_component_dependencies,
                componentFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1").EnvironmentVariable("DOTNET_ADDITIONAL_DEPS", additionalDepsPath)
                .Execute()
                .StdErrAfter("corehost_resolve_component_dependencies = {")
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{componentFixture.TestProject.AppDll}{Path.PathSeparator}]");
        }

        [Fact]
        public void MultiThreadedComponentDependencyResolutionWhichSucceeeds()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentWithNoDependenciesFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture.Copy();
            var componentWithResourcesFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithResourcesFixture.Copy();

            string componentWithNoDependenciesPrefix = Path.GetFileNameWithoutExtension(componentWithNoDependenciesFixture.TestProject.AppDll);
            string componentWithResourcesPrefix = Path.GetFileNameWithoutExtension(componentWithResourcesFixture.TestProject.AppDll);

            string[] args =
            {
                corehost_resolve_component_dependencies_multithreaded,
                componentWithNoDependenciesFixture.TestProject.AppDll,
                componentWithResourcesFixture.TestProject.AppDll
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"{componentWithNoDependenciesPrefix}: corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"{componentWithNoDependenciesPrefix}: corehost_resolve_component_dependencies assemblies:[{componentWithNoDependenciesFixture.TestProject.AppDll}{Path.PathSeparator}]")
                .And.HaveStdOutContaining($"{componentWithResourcesPrefix}: corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"{componentWithResourcesPrefix}: corehost_resolve_component_dependencies resource_search_paths:[" +
                    $"{ExpectedProbingPaths(componentWithResourcesFixture.TestProject.OutputDirectory)}]");
        }

        [Fact]
        public void MultiThreadedComponentDependencyResolutionWhichFailures()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var componentWithNoDependenciesFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture.Copy();
            var componentWithResourcesFixture = sharedTestState.PreviouslyPublishedAndRestoredComponentWithResourcesFixture.Copy();

            string componentWithNoDependenciesPrefix = Path.GetFileNameWithoutExtension(componentWithNoDependenciesFixture.TestProject.AppDll);
            string componentWithResourcesPrefix = Path.GetFileNameWithoutExtension(componentWithResourcesFixture.TestProject.AppDll);

            // Corrupt the .deps.json by appending } to it (malformed json)
            File.WriteAllText(
                componentWithNoDependenciesFixture.TestProject.DepsJson,
                File.ReadAllLines(componentWithNoDependenciesFixture.TestProject.DepsJson) + "}");

            string[] args =
            {
                corehost_resolve_component_dependencies_multithreaded,
                componentWithNoDependenciesFixture.TestProject.AppDll,
                componentWithResourcesFixture.TestProject.AppDll + "_invalid"
            };
            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut().CaptureStdErr().EnvironmentVariable("COREHOST_TRACE", "1")
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"{componentWithNoDependenciesPrefix}: corehost_resolve_component_dependencies:Fail[0x8000808B]")
                .And.HaveStdOutContaining($"{componentWithNoDependenciesPrefix}: corehost reported errors:")
                .And.HaveStdOutContaining($"{componentWithNoDependenciesPrefix}: A JSON parsing exception occurred in [{componentWithNoDependenciesFixture.TestProject.DepsJson}]: * Line 1, Column 2 Syntax error: Malformed token")
                .And.HaveStdOutContaining($"{componentWithNoDependenciesPrefix}: Error initializing the dependency resolver: An error occurred while parsing: {componentWithNoDependenciesFixture.TestProject.DepsJson}")
                .And.HaveStdOutContaining($"{componentWithResourcesPrefix}: corehost_resolve_component_dependencies:Fail[0x80008092]")
                .And.HaveStdOutContaining($"{componentWithResourcesPrefix}: corehost reported errors:")
                .And.HaveStdOutContaining($"{componentWithResourcesPrefix}: Failed to locate managed application");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture PreviouslyPublishedAndRestoredPortableApiTestProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredComponentWithDependenciesFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredComponentWithResourcesFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public string BreadcrumbLocation { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                PreviouslyPublishedAndRestoredPortableApiTestProjectFixture = new TestProjectFixture("HostApiInvokerApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();

                PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture = new TestProjectFixture("ComponentWithNoDependencies", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                PreviouslyPublishedAndRestoredComponentWithDependenciesFixture = new TestProjectFixture("ComponentWithDependencies", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                PreviouslyPublishedAndRestoredComponentWithResourcesFixture = new TestProjectFixture("ComponentWithResources", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // On non-Windows, we can't just P/Invoke to already loaded hostpolicy, so copy it next to the app dll.
                    var fixture = PreviouslyPublishedAndRestoredPortableApiTestProjectFixture;
                    var hostpolicy = Path.Combine(
                        fixture.BuiltDotnet.GreatestVersionSharedFxPath,
                        RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy"));

                    File.Copy(
                        hostpolicy,
                        Path.GetDirectoryName(fixture.TestProject.AppDll));
                }
            }

            public void Dispose()
            {
                PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredComponentWithNoDependenciesFixture.Dispose();
                PreviouslyPublishedAndRestoredComponentWithDependenciesFixture.Dispose();
                PreviouslyPublishedAndRestoredComponentWithResourcesFixture.Dispose();
            }
        }
    }
}