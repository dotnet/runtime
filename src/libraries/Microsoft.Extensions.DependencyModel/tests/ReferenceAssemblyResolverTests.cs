// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.DependencyModel.Resolution;
using Xunit;
using F = Microsoft.Extensions.DependencyModel.Tests.TestLibraryFactory;


namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class ReferenceAssemblyResolverTests
    {
        private static string ReferencePath = Path.Combine("reference", "assembly", "directory", "location");

        [Fact]
        public void SkipsNonReferenceAssembly()
        {
            var resolver = new ReferenceAssemblyPathResolver();
            var library = F.Create(
                F.PackageType);

            var result = resolver.TryResolveAssemblyPaths(library, null);

            result.Should().BeFalse();
        }

        [Fact]
        public void UsesEnvironmentVariableForDefaultPath()
        {
            var environment = EnvironmentMockBuilder.Create()
                .SetIsWindows(true)
                .AddVariable("DOTNET_REFERENCE_ASSEMBLIES_PATH", ReferencePath)
                .Build();

            var result = ReferenceAssemblyPathResolver.GetDefaultReferenceAssembliesPath(FileSystemMockBuilder.Empty, environment);
            result.Should().Be(ReferencePath);
        }

        [Fact]
        public void LooksOnlyOnEnvironmentVariableOnNonWindows()
        {
            var result = ReferenceAssemblyPathResolver.GetDefaultReferenceAssembliesPath(FileSystemMockBuilder.Empty, EnvironmentMockBuilder.Empty);
            result.Should().BeNull();
        }

        [Fact]
        public void ReturnsProgramFiles86AsDefaultLocationOnWin64()
        {
            var environment = EnvironmentMockBuilder.Create()
                .SetIsWindows(true)
                .AddVariable("ProgramFiles(x86)", "Program Files (x86)")
                .AddVariable("ProgramFiles", "Program Files")
                .Build();

            var result = ReferenceAssemblyPathResolver.GetDefaultReferenceAssembliesPath(FileSystemMockBuilder.Empty, environment);
            result.Should().Be(Path.Combine("Program Files (x86)", "Reference Assemblies", "Microsoft", "Framework"));
        }

        [Fact]
        public void ReturnsProgramFilesAsDefaultLocationOnWin32()
        {
            var environment = EnvironmentMockBuilder.Create()
                .SetIsWindows(true)
                .AddVariable("ProgramFiles", "Program Files")
                .Build();

            var result = ReferenceAssemblyPathResolver.GetDefaultReferenceAssembliesPath(FileSystemMockBuilder.Empty, environment);
            result.Should().Be(Path.Combine("Program Files", "Reference Assemblies", "Microsoft", "Framework"));
        }

        [Fact]
        public void ReturnNet20PathAsFallbackOnWindows()
        {
            var net20Path = Path.Combine("Windows", "Microsoft.NET", "Framework", "v2.0.50727");
            var fileSystem = FileSystemMockBuilder.Create()
                .AddFiles(net20Path, "some.dll")
                .Build();

            var environment = EnvironmentMockBuilder.Create()
                .SetIsWindows(true)
                .AddVariable("WINDIR", "Windows")
                .Build();

            var result = ReferenceAssemblyPathResolver.GetFallbackSearchPaths(fileSystem, environment);
            result.Should().Contain(net20Path);
        }

        [Fact]
        public void ChecksForRelativePathUnderDefaultLocation()
        {
            var fileSystem = FileSystemMockBuilder.Create()
                   .AddFiles(ReferencePath, F.DefaultAssemblyPath)
                   .Build();

            var library = F.Create(libraryType: F.ReferenceAssemblyType);
            var assemblies = new List<string>();

            var resolver = new ReferenceAssemblyPathResolver(fileSystem, ReferencePath, new string[] { });
            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            result.Should().BeTrue();
            assemblies.Should().Contain(Path.Combine(ReferencePath, F.DefaultAssemblyPath));
        }

        [Fact]
        public void ChecksForFileNameInFallbackLocation()
        {
            var fileSystem = FileSystemMockBuilder.Create()
                   .AddFiles(ReferencePath, F.DefaultAssembly)
                   .Build();

            var library = F.Create(libraryType: F.ReferenceAssemblyType);
            var assemblies = new List<string>();

            var resolver = new ReferenceAssemblyPathResolver(fileSystem, null, new string[] { ReferencePath });
            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            result.Should().BeTrue();
            assemblies.Should().Contain(Path.Combine(ReferencePath, F.DefaultAssembly));
        }

        [Fact]
        public void ShouldResolveAll()
        {
            var fileSystem = FileSystemMockBuilder.Create()
                   .AddFiles(ReferencePath, F.DefaultAssembly)
                   .Build();

            var library = F.Create(libraryType: F.ReferenceAssemblyType, assemblies: F.TwoAssemblies);
            var assemblies = new List<string>();

            var resolver = new ReferenceAssemblyPathResolver(fileSystem, null, new string[] { ReferencePath });

            var exception = Assert.Throws<InvalidOperationException>(() => resolver.TryResolveAssemblyPaths(library, assemblies));

            exception.Message.Should()
                .Contain(F.SecondAssemblyPath)
                .And.Contain(library.Name);
        }
    }
}
