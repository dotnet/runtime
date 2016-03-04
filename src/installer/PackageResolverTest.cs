// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.DependencyModel.Resolution;
using Microsoft.Extensions.PlatformAbstractions;
using Moq;
using Xunit;

using F = Microsoft.Extensions.DependencyModel.Tests.TestLibraryFactory;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class PackageResolverTest
    {
        private static string PackagesPath = Path.Combine("package", "directory", "location");

        [Fact]
        public void SholdUseEnvironmentVariableToGetDefaultLocation()
        {
            var runtime = new Mock<IRuntimeEnvironment>();

            var environment = EnvironmentMockBuilder.Create()
                .AddVariable("NUGET_PACKAGES", PackagesPath)
                .Build();

            var result = PackageCompilationAssemblyResolver.GetDefaultPackageDirectory(runtime.Object, environment);
            result.Should().Be(PackagesPath);
        }


        [Fact]
        public void SholdUseNugetUnderUserProfileOnWindows()
        {
            var runtime = new Mock<IRuntimeEnvironment>();
            runtime.SetupGet(r => r.OperatingSystemPlatform).Returns(Platform.Windows);

            var environment = EnvironmentMockBuilder.Create()
                .AddVariable("USERPROFILE", "User Profile")
                .Build();

            var result = PackageCompilationAssemblyResolver.GetDefaultPackageDirectory(runtime.Object, environment);
            result.Should().Be(Path.Combine("User Profile", ".nuget", "packages"));
        }

        [Fact]
        public void SholdUseNugetUnderHomeOnNonWindows()
        {
            var runtime = new Mock<IRuntimeEnvironment>();
            runtime.SetupGet(r => r.OperatingSystemPlatform).Returns(Platform.Linux);

            var environment = EnvironmentMockBuilder.Create()
                .AddVariable("HOME", "User Home")
                .Build();

            var result = PackageCompilationAssemblyResolver.GetDefaultPackageDirectory(runtime.Object, environment);
            result.Should().Be(Path.Combine("User Home", ".nuget", "packages"));
        }

        [Fact]
        public void ResolvesAllAssemblies()
        {
            var packagePath = Path.Combine(PackagesPath, F.DefaultPackageName, F.DefaultVersion);
            var fileSystem = FileSystemMockBuilder.Create()
                .AddFiles(packagePath, F.TwoAssemblies)
                .Build();
            var library = F.Create(assemblies: F.TwoAssemblies);

            var resolver = new PackageCompilationAssemblyResolver(fileSystem, PackagesPath);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            assemblies.Should().HaveCount(2);
            assemblies.Should().Contain(Path.Combine(packagePath, F.DefaultAssemblyPath));
            assemblies.Should().Contain(Path.Combine(packagePath, F.SecondAssemblyPath));
        }


        [Fact]
        public void FailsWhenOneOfAssembliesNotFound()
        {
            var packagePath = Path.Combine(PackagesPath, F.DefaultPackageName, F.DefaultVersion);
            var fileSystem = FileSystemMockBuilder.Create()
                .AddFiles(packagePath, F.DefaultAssemblyPath)
                .Build();
            var library = F.Create(assemblies: F.TwoAssemblies);

            var resolver = new PackageCompilationAssemblyResolver(fileSystem, PackagesPath);
            var assemblies = new List<string>();

            var exception = Assert.Throws<InvalidOperationException>(() => resolver.TryResolveAssemblyPaths(library, assemblies));
            exception.Message.Should()
                .Contain(F.SecondAssemblyPath)
                .And.Contain(library.Name);
        }
    }
}
