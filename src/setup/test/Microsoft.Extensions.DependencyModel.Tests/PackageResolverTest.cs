﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.DependencyModel;
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

            var result = PackageCompilationAssemblyResolver.GetDefaultPackageDirectory(Platform.Unknown, environment);
            result.Should().Be(PackagesPath);
        }


        [Fact]
        public void ShouldUseNugetUnderUserProfileOnWindows()
        {
            var environment = EnvironmentMockBuilder.Create()
                .AddVariable("USERPROFILE", "User Profile")
                .Build();

            var result = PackageCompilationAssemblyResolver.GetDefaultPackageDirectory(Platform.Windows, environment);
            result.Should().Be(Path.Combine("User Profile", ".nuget", "packages"));
        }

        [Fact]
        public void ShouldUseNugetUnderHomeOnNonWindows()
        {
            var environment = EnvironmentMockBuilder.Create()
                .AddVariable("HOME", "User Home")
                .Build();

            var result = PackageCompilationAssemblyResolver.GetDefaultPackageDirectory(Platform.Linux, environment);
            result.Should().Be(Path.Combine("User Home", ".nuget", "packages"));
        }

        [Fact]
        public void ResolvesAllAssemblies()
        {
            var packagePath = GetPackagesPath(F.DefaultPackageName, F.DefaultVersion);
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
            var packagePath = GetPackagesPath(F.DefaultPackageName, F.DefaultVersion);
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

        private static string GetPackagesPath(string id, string version)
        {
            return GetPackagesPath(PackagesPath, id, version);
        }

        internal static string GetPackagesPath(string basePath, string id, string version)
        {
            return Path.Combine(basePath, id.ToLowerInvariant(), version.ToLowerInvariant());
        }
    }
}
