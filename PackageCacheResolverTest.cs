// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.Extensions.DependencyModel.Resolution;
using Xunit;
using F = Microsoft.Extensions.DependencyModel.Tests.TestLibraryFactory;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class PackageCacheResolverTest
    {
        private static string CachePath = Path.Combine("cache", "directory", "location");

        [Fact]
        public void SholdUseEnvironmentVariableToGetDefaultLocation()
        {
            var result = PackageCacheCompilationAssemblyResolver.GetDefaultPackageCacheDirectory(GetDefaultEnviroment());

            result.Should().Be(CachePath);
        }

        [Fact]
        public void SkipsNonPackage()
        {
            var resolver = new PackageCacheCompilationAssemblyResolver();
            var library = F.Create(
               F.PackageType,
               assemblies: F.EmptyAssemblies);

            var result = resolver.TryResolveAssemblyPaths(library, null);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("INVALIDHASHVALUE")]
        [InlineData("INVALIDHASHVALUE-")]
        [InlineData("-INVALIDHASHVALUE")]
        public void FailsOnInvalidHash(string hash)
        {
            var resolver = new PackageCacheCompilationAssemblyResolver(FileSystemMockBuilder.Empty, CachePath);
            var library = F.Create(hash: hash);

            var exception = Assert.Throws<InvalidOperationException>(() => resolver.TryResolveAssemblyPaths(library, null));
            exception.Message.Should()
                .Contain(library.Hash)
                .And.Contain(library.Name);
        }

        [Fact]
        public void ChecksHashFile()
        {
            var packagePath = Path.Combine(CachePath, F.DefaultPackageName, F.DefaultVersion);
            var fileSystem = FileSystemMockBuilder.Create()
                .AddFile(
                    Path.Combine(packagePath, $"{F.DefaultPackageName}.{F.DefaultVersion}.nupkg.{F.DefaultHashAlgoritm}"),
                    "WRONGHASH"
                )
                .AddFiles(packagePath, F.DefaultAssemblies)
                .Build();

            var resolver = new PackageCacheCompilationAssemblyResolver(fileSystem, CachePath);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(F.Create(), assemblies);
            result.Should().BeFalse();
        }

        [Fact]
        public void ResolvesAllAssemblies()
        {
            var packagePath = Path.Combine(CachePath, F.DefaultPackageName, F.DefaultVersion);
            var fileSystem = FileSystemMockBuilder.Create()
                .AddFile(
                    Path.Combine(packagePath, $"{F.DefaultPackageName}.{F.DefaultVersion}.nupkg.{F.DefaultHashAlgoritm}"),
                    F.DefaultHashValue
                )
                .AddFiles(packagePath, F.TwoAssemblies)
                .Build();
            var library = F.Create(assemblies: F.TwoAssemblies);

            var resolver = new PackageCacheCompilationAssemblyResolver(fileSystem, CachePath);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            assemblies.Should().HaveCount(2);
            assemblies.Should().Contain(Path.Combine(packagePath, F.DefaultAssemblyPath));
            assemblies.Should().Contain(Path.Combine(packagePath, F.SecondAssemblyPath));
        }


        [Fact]
        public void FailsWhenOneOfAssembliesNotFound()
        {
            var packagePath = Path.Combine(CachePath, F.DefaultPackageName, F.DefaultVersion);
            var fileSystem = FileSystemMockBuilder.Create()
                .AddFile(
                    Path.Combine(packagePath, $"{F.DefaultPackageName}.{F.DefaultVersion}.nupkg.{F.DefaultHashAlgoritm}"),
                    F.DefaultHashValue
                )
                .AddFiles(packagePath, F.DefaultAssemblyPath)
                .Build();
            var library = F.Create(assemblies: F.TwoAssemblies);

            var resolver = new PackageCacheCompilationAssemblyResolver(fileSystem, CachePath);
            var assemblies = new List<string>();

            var exception = Assert.Throws<InvalidOperationException>(() => resolver.TryResolveAssemblyPaths(library, assemblies));
            exception.Message.Should()
                .Contain(F.SecondAssemblyPath)
                .And.Contain(library.Name);
        }

        private IEnvironment GetDefaultEnviroment()
        {
            return EnvironmentMockBuilder.Create()
                .AddVariable("DOTNET_PACKAGES_CACHE", CachePath)
                .Build();
        }


    }
}
