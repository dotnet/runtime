// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.DependencyModel.Resolution;
using Xunit;
using F = Microsoft.Extensions.DependencyModel.Tests.TestLibraryFactory;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class PackageResolverTest
    {
        private static string PackagesPath = Path.Combine("package", "directory", "location");

        [Fact]
        public void ShouldUseEnvironmentVariableToGetDefaultLocation()
        {
            var environment = EnvironmentMockBuilder.Create()
                .AddVariable("NUGET_PACKAGES", PackagesPath)
                .Build();

            var result = PackageCompilationAssemblyResolver.GetDefaultProbeDirectories(environment);
            // The host for .NET Core 2.0 always sets the PROBING_DIRECTORIES property on the AppContext. Because of that,
            // no additional package directories should be returned from this, even if they are set as environment variables.
            result.Should().NotContain(PackagesPath);
        }


        [Fact]
        public void ShouldUseNugetUnderUserProfileOnWindows()
        {
            var environment = EnvironmentMockBuilder.Create()
                .SetIsWindows(true)
                .AddVariable("USERPROFILE", "User Profile")
                .Build();

            var result = PackageCompilationAssemblyResolver.GetDefaultProbeDirectories(environment);
            // The host for .NET Core 2.0 always sets the PROBING_DIRECTORIES property on the AppContext. Because of that,
            // no additional package directories should be returned from this, even if they are set as environment variables.
            result.Should().NotContain(Path.Combine("User Profile", ".nuget", "packages"));
        }

        [Fact]
        public void ShouldUseNugetUnderHomeOnNonWindows()
        {
            var environment = EnvironmentMockBuilder.Create()
                .SetIsWindows(false)
                .AddVariable("HOME", "User Home")
                .Build();

            var result = PackageCompilationAssemblyResolver.GetDefaultProbeDirectories(environment);
            // The host for .NET Core 2.0 always sets the PROBING_DIRECTORIES property on the AppContext. Because of that,
            // no additional package directories should be returned from this, even if they are set as environment variables.
            result.Should().NotContain(Path.Combine("User Home", ".nuget", "packages"));
        }

        [Fact]
        public void ResolvesAllAssemblies()
        {
            var packagePath = GetPackagesPath(F.DefaultPackageName, F.DefaultVersion);
            var fileSystem = FileSystemMockBuilder.Create()
                .AddFiles(packagePath, F.TwoAssemblies)
                .Build();
            var library = F.Create(assemblies: F.TwoAssemblies);

            var resolver = new PackageCompilationAssemblyResolver(fileSystem, new string[] { PackagesPath });
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            assemblies.Should().HaveCount(2);
            assemblies.Should().Contain(Path.Combine(packagePath, F.DefaultAssemblyPath));
            assemblies.Should().Contain(Path.Combine(packagePath, F.SecondAssemblyPath));
        }

        [Fact]
        public void FailsWhenOneOfAssembliesNotFound()
        {
            var packagePath = GetPackagesPath(F.DefaultPackageName, F.DefaultVersion);
            var fileSystem = FileSystemMockBuilder.Create()
                .AddFiles(packagePath, F.DefaultAssemblyPath)
                .Build();
            var library = F.Create(assemblies: F.TwoAssemblies);

            var resolver = new PackageCompilationAssemblyResolver(fileSystem,  new string[] { PackagesPath });
            var assemblies = new List<string>();

            resolver.TryResolveAssemblyPaths(library, assemblies)
                .Should().BeFalse();

            assemblies.Should().BeEmpty();
        }

        [Fact]
        public void KeepsLookingWhenOneOfAssembliesNotFound()
        {
            var packagePath1 = GetPackagesPath(F.DefaultPackageName, F.DefaultVersion);
            var secondPath = "secondPath";
            var packagePath2 = GetPackagesPath(secondPath, F.DefaultPackageName, F.DefaultVersion);
            var fileSystem = FileSystemMockBuilder.Create()
                .AddFiles(packagePath1, F.DefaultAssemblyPath)
                .AddFiles(packagePath2, F.DefaultAssemblyPath, F.SecondAssemblyPath)
                .Build();
            var library = F.Create(assemblies: F.TwoAssemblies);

            var resolver = new PackageCompilationAssemblyResolver(fileSystem, new string[] { PackagesPath, secondPath });
            var assemblies = new List<string>();

            resolver.TryResolveAssemblyPaths(library, assemblies)
                .Should().BeTrue();

            assemblies.Should().HaveCount(2);
            assemblies.Should().Contain(Path.Combine(packagePath2, F.DefaultAssemblyPath));
            assemblies.Should().Contain(Path.Combine(packagePath2, F.SecondAssemblyPath));
        }

        private static string GetPackagesPath(string id, string version)
        {
            return GetPackagesPath(PackagesPath, id, version);
        }

        internal static string GetPackagesPath(string basePath, string id, string version)
        {
            return Path.Combine(basePath, id, version);
        }
    }
}
