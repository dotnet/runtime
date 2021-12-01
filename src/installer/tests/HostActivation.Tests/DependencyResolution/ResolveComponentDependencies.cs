// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public class ResolveComponentDependencies : 
        ComponentDependencyResolutionBase,
        IClassFixture<ResolveComponentDependencies.SharedTestState>
    {
        private readonly SharedTestState sharedTestState;

        public ResolveComponentDependencies(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void InvalidMainComponentAssemblyPathFails()
        {
            sharedTestState.RunComponentResolutionTest(
                sharedTestState.FrameworkReferenceApp.AppDll + "_invalid",
                sharedTestState.FrameworkReferenceApp,
                sharedTestState.DotNetWithNetCoreApp.GreatestVersionHostFxrPath)
                .Should().Fail()
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies:Fail[0x{Constants.ErrorCode.LibHostInvalidArgs.ToString("x")}]")
                .And.HaveStdOutContaining("corehost reported errors:")
                .And.HaveStdOutContaining("Failed to locate managed application");
        }

        [Fact]
        public void ComponentWithNoDependenciesAndNoDeps()
        {
            var component = sharedTestState.ComponentWithNoDependencies.Copy();

            // Remove .deps.json
            File.Delete(component.DepsJson);

            sharedTestState.RunComponentResolutionTest(component)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{component.AppDll}{Path.PathSeparator}]")
                .And.HaveStdErrContaining($"app_root='{component.Location}{Path.DirectorySeparatorChar}'")
                .And.HaveStdErrContaining($"deps='{component.DepsJson}'")
                .And.HaveStdErrContaining($"mgd_app='{component.AppDll}'");
        }

        [Fact]
        public void ComponentWithNoDependenciesCaseChangedOnAsm()
        {
            // Scenario: change the case of the first letter of component.AppDll file name

            // Changing the casing of the first letter of a dependent assembly have different behavior in the 3 platforms
            // Wisely the product code stays out of casing on dependent assemblies choosing the 1st assembly
            // Linux: we fail
            // Windows and Mac, probing succeeds but
            // Windows: probing returns the original name
            // Mac: probing return the new name including 2 assembly probing with the original and new name, and the changed deps file

            var component = sharedTestState.ComponentWithNoDependencies.Copy();

            // Change the case of the first letter of AppDll
            string fileName = component.AppDll;
            string nameWOExtension = Path.GetFileNameWithoutExtension(fileName);
            string nameWOExtensionCaseChanged = (Char.IsUpper(nameWOExtension[0]) ? nameWOExtension[0].ToString().ToLower() : nameWOExtension[0].ToString().ToUpper()) + nameWOExtension.Substring(1);
            string changeFile = Path.Combine(Path.GetDirectoryName(fileName), (nameWOExtensionCaseChanged + Path.GetExtension(fileName)));
            // on mac, hostpolicy returns the changed name for deps file as well
            string changeDepsFile = Path.Combine(Path.GetDirectoryName(component.DepsJson), (nameWOExtensionCaseChanged + ".deps" + Path.GetExtension(component.DepsJson)));

            // Rename
            File.Move(fileName, changeFile);

            if (OperatingSystem.IsWindows())
            {
                sharedTestState.RunComponentResolutionTest(component)
                    .Should().Pass()
                    .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                    .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{component.AppDll}{Path.PathSeparator}]")
                    .And.HaveStdErrContaining($"app_root='{component.Location}{Path.DirectorySeparatorChar}'")
                    .And.HaveStdErrContaining($"deps='{component.DepsJson}'")
                    .And.HaveStdErrContaining($"mgd_app='{component.AppDll}'");
            }
            else if(OperatingSystem.IsMacOS())
            {
                sharedTestState.RunComponentResolutionTest(component)
                    .Should().Pass()
                    .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                    .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{component.AppDll}{Path.PathSeparator}{changeFile}{Path.PathSeparator}]")
                    .And.HaveStdErrContaining($"app_root='{component.Location}{Path.DirectorySeparatorChar}'")
                    .And.HaveStdErrContaining($"deps='{changeDepsFile}'")
                    .And.HaveStdErrContaining($"mgd_app='{changeFile}'");
            }
            else
            {
                // OSPlatform.Linux
                // We expect the test to fail due to the the case change of AppDll
                sharedTestState.RunComponentResolutionTest(component)
                    .Should().Fail()
                    .And.HaveStdErrContaining($"Failed to locate managed application [{component.AppDll}]");
            }
        }

        [Fact]
        public void ComponentWithNoDependenciesCaseChangedOnDepsAndAsm()
        {

            // Scenario: change the case of the first letter of component.AppDll and component.DepsJson file names

            // Changing the casing of the first letter of a dependent assembly have different behavior in the 3 platforms
            // Wisely the product code stays out of casing on dependent assemblies choosing the 1st assembly
            // Linux: we fail
            // Windows and Mac, probing succeeds but
            // Windows: probing returns the original name
            // Mac: probing return the new name including 2 assembly probing with the original and new name, and the changed deps file

            var component = sharedTestState.ComponentWithNoDependencies.Copy();

            // Change the case of the first letter of AppDll
            string fileName = component.AppDll;
            string nameWOExtension = Path.GetFileNameWithoutExtension(fileName);
            string nameWOExtensionCaseChanged = (Char.IsUpper(nameWOExtension[0]) ? nameWOExtension[0].ToString().ToLower() : nameWOExtension[0].ToString().ToUpper()) + nameWOExtension.Substring(1);
            string changeFile = Path.Combine(Path.GetDirectoryName(fileName), (nameWOExtensionCaseChanged + Path.GetExtension(fileName)));

            string changeDepsFile = Path.Combine(Path.GetDirectoryName(component.DepsJson), (nameWOExtensionCaseChanged + ".deps" + Path.GetExtension(component.DepsJson)));

            // Rename
            File.Move(fileName, changeFile);
            File.Move(component.DepsJson, changeDepsFile);

            if (OperatingSystem.IsWindows())
            {
                sharedTestState.RunComponentResolutionTest(component)
                    .Should().Pass()
                    .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                    .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{component.AppDll}{Path.PathSeparator}]")
                    .And.HaveStdErrContaining($"app_root='{component.Location}{Path.DirectorySeparatorChar}'")
                    .And.HaveStdErrContaining($"deps='{component.DepsJson}'")
                    .And.HaveStdErrContaining($"mgd_app='{component.AppDll}'");
            }
            else if(OperatingSystem.IsMacOS())
            {
                sharedTestState.RunComponentResolutionTest(component)
                    .Should().Pass()
                    .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                    .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{component.AppDll}{Path.PathSeparator}{changeFile}{Path.PathSeparator}]")
                    .And.HaveStdErrContaining($"app_root='{component.Location}{Path.DirectorySeparatorChar}'")
                    .And.HaveStdErrContaining($"deps='{changeDepsFile}'")
                    .And.HaveStdErrContaining($"mgd_app='{changeFile}'");
            }
            else
            {
                // OSPlatform.Linux
                // We expect the test to fail due to the the case change of AppDll
                sharedTestState.RunComponentResolutionTest(component)
                    .Should().Fail()
                    .And.HaveStdErrContaining($"Failed to locate managed application [{component.AppDll}]");
            }
        }

        [Fact]
        public void ComponentWithNoDependenciesNoDepsCaseChangedOnAsm()
        {

            // Scenario: change the case of the first letter of component.AppDll file name and delete component.DepsJson file

            // Changing the casing of the first letter of a dependent assembly have different behavior in the 3 platforms
            // Wisely the product code stays out of casing on dependent assemblies choosing the 1st assembly
            // Linux: we fail
            // Windows and Mac, probing succeeds but
            // Windows: probing returns the original name
            // Mac: probing return the new name including assembly probing with the new name and the changed deps file

            var component = sharedTestState.ComponentWithNoDependencies.Copy();

            // Change the case of the first letter of AppDll
            string fileName = component.AppDll;
            string nameWOExtension = Path.GetFileNameWithoutExtension(fileName);
            string nameWOExtensionCaseChanged = (Char.IsUpper(nameWOExtension[0]) ? nameWOExtension[0].ToString().ToLower() : nameWOExtension[0].ToString().ToUpper()) + nameWOExtension.Substring(1);
            string changeFile = Path.Combine(Path.GetDirectoryName(fileName), (nameWOExtensionCaseChanged + Path.GetExtension(fileName)));
            // on mac, hostpolicy returns the changed name for deps file as well
            string changeDepsFile = Path.Combine(Path.GetDirectoryName(component.DepsJson), (nameWOExtensionCaseChanged + ".deps" + Path.GetExtension(component.DepsJson)));

            // Rename
            File.Move(fileName, changeFile);
            // Delete deps
            File.Delete(component.DepsJson);

            if (OperatingSystem.IsWindows())
            {
                sharedTestState.RunComponentResolutionTest(component)
                    .Should().Pass()
                    .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                    .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{component.AppDll}{Path.PathSeparator}{changeFile}{Path.PathSeparator}]")
                    .And.HaveStdErrContaining($"app_root='{component.Location}{Path.DirectorySeparatorChar}'")
                    .And.HaveStdErrContaining($"deps='{component.DepsJson}'")
                    .And.HaveStdErrContaining($"mgd_app='{component.AppDll}'");
            }
            else if(OperatingSystem.IsMacOS())
            {
                sharedTestState.RunComponentResolutionTest(component)
                    .Should().Pass()
                    .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                    .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{changeFile}{Path.PathSeparator}]")
                    .And.HaveStdErrContaining($"app_root='{component.Location}{Path.DirectorySeparatorChar}'")
                    .And.HaveStdErrContaining($"deps='{changeDepsFile}'")
                    .And.HaveStdErrContaining($"mgd_app='{changeFile}'");
            }
            else
            {
                // OSPlatform.Linux
                // We expect the test to fail due to the the case change of AppDll
                sharedTestState.RunComponentResolutionTest(component)
                    .Should().Fail()
                    .And.HaveStdErrContaining($"Failed to locate managed application [{component.AppDll}]");
            }
        }

        [Fact]
        public void ComponentWithNoDependencies()
        {
            sharedTestState.RunComponentResolutionTest(sharedTestState.ComponentWithNoDependencies)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{sharedTestState.ComponentWithNoDependencies.AppDll}{Path.PathSeparator}]");
        }

        [Fact]
        public void ComponentWithDependencies()
        {
            sharedTestState.RunComponentResolutionTest(sharedTestState.ComponentWithDependencies,
                command => command.RuntimeId("win10-x86"))
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{Path.Combine(sharedTestState.ComponentWithDependencies.Location, "ComponentDependency.dll")}{Path.PathSeparator}" +
                    $"{sharedTestState.ComponentWithDependencies.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(sharedTestState.ComponentWithDependencies.Location, "Newtonsoft.Json.dll")}{Path.PathSeparator}]")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies native_search_paths:[" +
                    $"{Path.Combine(sharedTestState.ComponentWithDependencies.Location, "runtimes", "win10-x86", "native")}" +
                    $"{Path.DirectorySeparatorChar}{Path.PathSeparator}]");
        }

        [Fact]
        public void ComponentWithDependenciesAndDependencyRemoved()
        {
            var component = sharedTestState.ComponentWithDependencies.Copy();

            // Remove a dependency
            File.Delete(Path.Combine(component.Location, "ComponentDependency.dll"));

            sharedTestState.RunComponentResolutionTest(component)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining(
                    $"corehost_resolve_component_dependencies assemblies:[" +
                    $"{Path.Combine(component.Location, "ComponentDependency.dll")}{Path.PathSeparator}" +
                    $"{component.AppDll}{Path.PathSeparator}" +
                    $"{Path.Combine(component.Location, "Newtonsoft.Json.dll")}{Path.PathSeparator}]");
        }

        [Fact]
        public void ComponentWithDependenciesAndNoDeps()
        {
            var component = sharedTestState.ComponentWithDependencies.Copy();

            // Remove .deps.json
            File.Delete(component.DepsJson);

            sharedTestState.RunComponentResolutionTest(component)
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

            sharedTestState.RunComponentResolutionTest(component)
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

            sharedTestState.RunComponentResolutionTest(component)
                .Should().Fail()
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies:Fail[0x{Constants.ErrorCode.ResolverResolveFailure.ToString("x")}]")
                .And.HaveStdOutContaining("corehost reported errors:")
                .And.HaveStdOutContaining("An assembly specified in the application dependencies manifest (ComponentWithDependencies.deps.json) has already been found but with a different file extension")
                .And.HaveStdOutContaining("package: 'ComponentDependency_Dupe', version: '1.0.0'")
                .And.HaveStdOutContaining("path: 'ComponentDependency.notdll'")
                .And.HaveStdOutContaining($"previously found assembly: '{Path.Combine(component.Location, "ComponentDependency.dll")}'");
        }

        [Fact]
        public void ComponentWithSameDependencyNativeImageShouldFail()
        {
            // Add a reference to a package which has asset of the native image of the existing ComponentDependency.
            var component = sharedTestState.CreateComponentWithDependencies(b => b
                .WithPackage("ComponentDependency_NI", "1.0.0", p => p
                    .WithAssemblyGroup(null, g => g
                        .WithAsset("ComponentDependency.ni.dll"))));

            sharedTestState.RunComponentResolutionTest(component)
                .Should().Fail()
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies:Fail[0x{Constants.ErrorCode.ResolverResolveFailure.ToString("x")}]")
                .And.HaveStdOutContaining("corehost reported errors:")
                .And.HaveStdOutContaining("An assembly specified in the application dependencies manifest (ComponentWithDependencies.deps.json) has already been found but with a different file extension")
                .And.HaveStdOutContaining("package: 'ComponentDependency_NI', version: '1.0.0'")
                .And.HaveStdOutContaining("path: 'ComponentDependency.ni.dll'")
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

            sharedTestState.RunComponentResolutionTest(component)
                .Should().Fail()
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies:Fail[0x{Constants.ErrorCode.ResolverInitFailure.ToString("x")}]")
                .And.HaveStdOutContaining("corehost reported errors:")
                .And.HaveStdOutContaining($"A JSON parsing exception occurred in [{component.DepsJson}], offset 0 (line 1, column 1): Invalid value.")
                .And.HaveStdOutContaining($"Error initializing the dependency resolver: An error occurred while parsing: {component.DepsJson}");
        }

        [Fact]
        public void ComponentWithResourcesShouldReportResourceSearchPaths()
        {
            sharedTestState.RunComponentResolutionTest(sharedTestState.ComponentWithResources)
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies resource_search_paths:[" +
                    $"{sharedTestState.ComponentWithResources.Location}" +
                    $"{Path.DirectorySeparatorChar}{Path.PathSeparator}]");
        }

        [Fact]
        public void AdditionalDepsDontAffectComponentDependencyResolution()
        {
            var component = sharedTestState.ComponentWithNoDependencies.Copy();

            string additionalDepsPath = Path.Combine(Path.GetDirectoryName(component.DepsJson), "__duplicate.deps.json");
            File.Copy(sharedTestState.FrameworkReferenceApp.DepsJson, additionalDepsPath);

            sharedTestState.RunComponentResolutionTest(component, command => command
                .EnvironmentVariable("DOTNET_ADDITIONAL_DEPS", additionalDepsPath))
                .Should().Pass()
                .And.HaveStdOutContaining("corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"corehost_resolve_component_dependencies assemblies:[{component.AppDll}{Path.PathSeparator}]");
        }

        [Fact]
        public void MultiThreadedComponentDependencyResolutionWhichSucceeeds()
        {
            sharedTestState.RunComponentResolutionMultiThreadedTest(sharedTestState.ComponentWithNoDependencies, sharedTestState.ComponentWithResources)
                .Should().Pass()
                .And.HaveStdOutContaining($"ComponentA: corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"ComponentA: corehost_resolve_component_dependencies assemblies:[{sharedTestState.ComponentWithNoDependencies.AppDll}{Path.PathSeparator}]")
                .And.HaveStdOutContaining($"ComponentB: corehost_resolve_component_dependencies:Success")
                .And.HaveStdOutContaining($"ComponentB: corehost_resolve_component_dependencies resource_search_paths:[" +
                    $"{sharedTestState.ComponentWithResources.Location}" +
                    $"{Path.DirectorySeparatorChar}{Path.PathSeparator}]");
        }

        [Fact]
        public void MultiThreadedComponentDependencyResolutionWhithFailures()
        {
            var componentWithNoDependencies = sharedTestState.ComponentWithNoDependencies.Copy();

            // Corrupt the .deps.json by appending } to it (malformed json)
            File.WriteAllText(
                componentWithNoDependencies.DepsJson,
                File.ReadAllLines(componentWithNoDependencies.DepsJson) + "}");

            sharedTestState.RunComponentResolutionMultiThreadedTest(
                componentWithNoDependencies.AppDll,
                sharedTestState.ComponentWithResources.AppDll + "_invalid",
                sharedTestState.FrameworkReferenceApp,
                sharedTestState.DotNetWithNetCoreApp.GreatestVersionHostFxrPath)
                .Should().Fail()
                .And.HaveStdOutContaining($"ComponentA: corehost_resolve_component_dependencies:Fail[0x{Constants.ErrorCode.ResolverInitFailure.ToString("x")}]")
                .And.HaveStdOutContaining($"ComponentA: corehost reported errors:")
                .And.HaveStdOutContaining($"ComponentA: A JSON parsing exception occurred in [{componentWithNoDependencies.DepsJson}], offset 0 (line 1, column 1): Invalid value.")
                .And.HaveStdOutContaining($"ComponentA: Error initializing the dependency resolver: An error occurred while parsing: {componentWithNoDependencies.DepsJson}")
                .And.HaveStdOutContaining($"ComponentB: corehost_resolve_component_dependencies:Fail[0x{Constants.ErrorCode.LibHostInvalidArgs.ToString("x")}]")
                .And.HaveStdOutContaining($"ComponentB: corehost reported errors:")
                .And.HaveStdOutContaining($"ComponentB: Failed to locate managed application");
        }

        public class SharedTestState : ComponentSharedTestStateBase
        {
            public TestApp ComponentWithNoDependencies { get; }
            public TestApp ComponentWithDependencies { get; }
            public TestApp ComponentWithResources { get; }

            public SharedTestState()
            {
                ComponentWithNoDependencies = CreateComponentWithNoDependencies(null, Location);

                ComponentWithDependencies = CreateComponentWithDependencies(null, Location);

                ComponentWithResources = CreateComponentWithResources(null, Location);
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
                        .WithNativeLibraryGroup("win10-arm", g => g.WithAsset("runtimes/win10-arm/native/libuv.dll"))
                        .WithNativeLibraryGroup("win10-x64", g => g.WithAsset("runtimes/win10-x64/native/libuv.dll"))
                        .WithNativeLibraryGroup("win10-x86", g => g.WithAsset("runtimes/win10-x86/native/libuv.dll")));
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
        }
    }
}
