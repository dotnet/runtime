// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyModel.Resolution;
using Xunit;
using FluentAssertions;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class AppBaseResolverTests
    {
        private static string BasePath = Path.Combine("Base","Path");
        private static string BasePathRefs = Path.Combine(BasePath, "refs");

        private static string SharedFxPath = Path.Combine("shared", "fx");
        private static string SharedFxPathRefs = Path.Combine(SharedFxPath, "refs");

        private static DependencyContextPaths DependencyContextPaths =
            new DependencyContextPaths(null, Path.Combine(SharedFxPath, "deps.json"), null);

        [Fact]
        public void ResolvesProjectType()
        {
            var fileSystem = FileSystemMockBuilder
                 .Create()
                 .AddFiles(BasePathRefs, TestLibraryFactory.DefaultAssembly)
                 .Build();
            var resolver = CreateResolver(fileSystem);
            var library = TestLibraryFactory.Create(
                TestLibraryFactory.ProjectType,
                assemblies: TestLibraryFactory.EmptyAssemblies);

            var result = resolver.TryResolveAssemblyPaths(library, null);

            Assert.True(result);
        }

        [Fact]
        public void ResolvesMsBuildProjectType()
        {
            var fileSystem = FileSystemMockBuilder
                 .Create()
                 .AddFiles(BasePathRefs, TestLibraryFactory.DefaultAssembly)
                 .Build();
            var resolver = CreateResolver(fileSystem);
            var library = TestLibraryFactory.Create(
                TestLibraryFactory.MsBuildProjectType,
                assemblies: TestLibraryFactory.EmptyAssemblies);

            var result = resolver.TryResolveAssemblyPaths(library, null);

            Assert.True(result);
        }

        [Fact]
        public void ResolvesPackageType()
        {
            var fileSystem = FileSystemMockBuilder
                 .Create()
                 .AddFiles(BasePathRefs, TestLibraryFactory.DefaultAssembly)
                 .Build();
            var resolver = CreateResolver(fileSystem);
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.PackageType,
               assemblies: TestLibraryFactory.EmptyAssemblies);

            var result = resolver.TryResolveAssemblyPaths(library, null);

            Assert.True(result);
        }

        [Fact]
        public void ResolvesReferenceAssemblyType()
        {
            var fileSystem = FileSystemMockBuilder
                 .Create()
                 .AddFiles(BasePathRefs, TestLibraryFactory.DefaultAssembly)
                 .Build();
            var resolver = CreateResolver(fileSystem);
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.ReferenceAssemblyType,
               assemblies: TestLibraryFactory.EmptyAssemblies);

            var result = resolver.TryResolveAssemblyPaths(library, null);

            Assert.True(result);
        }

        [Fact]
        public void ResolvesReferenceType()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePathRefs, TestLibraryFactory.DefaultAssembly)
                .Build();
            var resolver = CreateResolver(fileSystem);
            var library = TestLibraryFactory.Create(
                TestLibraryFactory.ReferenceType,
                assemblies: TestLibraryFactory.EmptyAssemblies);

            var result = resolver.TryResolveAssemblyPaths(library, null);

            Assert.True(result);
        }

        [Fact]
        public void RequiresExistingRefsFolderForNonProjects()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePath, TestLibraryFactory.DefaultAssembly, TestLibraryFactory.SecondAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.ReferenceAssemblyType,
               assemblies: TestLibraryFactory.TwoAssemblies);
            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            Assert.False(result);
            assemblies.Should().HaveCount(0);
        }

        [Fact]
        public void ResolvesProjectWithoutRefsFolder()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePath, TestLibraryFactory.DefaultAssembly, TestLibraryFactory.SecondAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.ProjectType,
               assemblies: TestLibraryFactory.TwoAssemblies);
            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            Assert.True(result);
            assemblies.Should().HaveCount(2);
            assemblies.Should().Contain(Path.Combine(BasePath, TestLibraryFactory.DefaultAssembly));
            assemblies.Should().Contain(Path.Combine(BasePath, TestLibraryFactory.SecondAssembly));
        }

        [Fact]
        public void ResolvesDirectReferenceWithoutRefsFolder()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePath, TestLibraryFactory.DefaultAssembly, TestLibraryFactory.SecondAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
                TestLibraryFactory.ReferenceType,
                assemblies: TestLibraryFactory.TwoAssemblies);
            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            Assert.True(result);
            assemblies.Should().HaveCount(2);
            assemblies.Should().Contain(Path.Combine(BasePath, TestLibraryFactory.DefaultAssembly));
            assemblies.Should().Contain(Path.Combine(BasePath, TestLibraryFactory.SecondAssembly));
        }

        [Fact]
        public void RequiresAllLibrariesToExist()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePath, TestLibraryFactory.DefaultAssembly)
                .AddFiles(BasePathRefs, TestLibraryFactory.DefaultAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.ReferenceAssemblyType,
               assemblies: TestLibraryFactory.TwoAssemblies);
            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            resolver.TryResolveAssemblyPaths(library, assemblies).Should().Be(false);
            assemblies.Should().BeEmpty();
        }

        [Fact]
        public void ResolvesIfAllAreInBaseDir()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePath, TestLibraryFactory.DefaultAssembly, TestLibraryFactory.SecondAssembly)
                .AddFiles(BasePathRefs, "Dummy.dll")
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.ReferenceAssemblyType,
               assemblies: TestLibraryFactory.TwoAssemblies);
            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            Assert.True(result);
            assemblies.Should().HaveCount(2);
            assemblies.Should().Contain(Path.Combine(BasePath, TestLibraryFactory.DefaultAssembly));
            assemblies.Should().Contain(Path.Combine(BasePath, TestLibraryFactory.SecondAssembly));
        }


        [Fact]
        public void ResolvesIfAllAreInRefDir()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePathRefs, TestLibraryFactory.DefaultAssembly, TestLibraryFactory.SecondAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.ReferenceAssemblyType,
               assemblies: TestLibraryFactory.TwoAssemblies);

            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            Assert.True(result);
            assemblies.Should().HaveCount(2);
            assemblies.Should().Contain(Path.Combine(BasePathRefs, TestLibraryFactory.DefaultAssembly));
            assemblies.Should().Contain(Path.Combine(BasePathRefs, TestLibraryFactory.SecondAssembly));
        }

        [Fact]
        public void ResolvesIfOneInBaseOtherInRefs()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePath, TestLibraryFactory.DefaultAssembly)
                .AddFiles(BasePathRefs, TestLibraryFactory.SecondAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.ReferenceAssemblyType,
               assemblies: TestLibraryFactory.TwoAssemblies);

            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            Assert.True(result);
            assemblies.Should().HaveCount(2);
            assemblies.Should().Contain(Path.Combine(BasePath, TestLibraryFactory.DefaultAssembly));
            assemblies.Should().Contain(Path.Combine(BasePathRefs, TestLibraryFactory.SecondAssembly));
        }

        [Fact]
        public void PrefersRefs()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePath, TestLibraryFactory.DefaultAssembly)
                .AddFiles(BasePathRefs, TestLibraryFactory.DefaultAssembly)
                .AddFile(SharedFxPath, TestLibraryFactory.DefaultAssembly)
                .AddFile(SharedFxPathRefs, TestLibraryFactory.DefaultAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.PackageType
               );

            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            Assert.True(result);
            assemblies.Should().HaveCount(1);
            assemblies.Should().Contain(Path.Combine(BasePathRefs, TestLibraryFactory.DefaultAssembly));
        }

        [Fact]
        public void SearchesInSharedFxRefsPathForPublishedPortable()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePathRefs, TestLibraryFactory.SecondAssembly)
                .AddFiles(SharedFxPathRefs, TestLibraryFactory.DefaultAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.PackageType
               );

            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();
            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            Assert.True(result);
            assemblies.Should().HaveCount(1);
            assemblies.Should().Contain(Path.Combine(SharedFxPathRefs, TestLibraryFactory.DefaultAssembly));
        }

        [Fact]
        public void SearchesInSharedFxPathForPublishedPortable()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePathRefs, TestLibraryFactory.SecondAssembly)
                .AddFiles(SharedFxPath, TestLibraryFactory.DefaultAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.PackageType
               );

            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            Assert.True(result);
            assemblies.Should().HaveCount(1);
            assemblies.Should().Contain(Path.Combine(SharedFxPath, TestLibraryFactory.DefaultAssembly));
        }

        [Fact]
        public void PrefersSharedFxPathRefsPathPublishedPortable()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePathRefs, TestLibraryFactory.SecondAssembly)
                .AddFiles(SharedFxPath, TestLibraryFactory.DefaultAssembly)
                .AddFiles(SharedFxPathRefs, TestLibraryFactory.DefaultAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.PackageType
               );

            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            Assert.True(result);
            assemblies.Should().HaveCount(1);
            assemblies.Should().Contain(Path.Combine(SharedFxPathRefs, TestLibraryFactory.DefaultAssembly));
        }

        [Fact]
        public void SkipsSharedFxPathForNonPublishedPortable()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(SharedFxPath, TestLibraryFactory.DefaultAssembly)
                .AddFiles(SharedFxPathRefs, TestLibraryFactory.DefaultAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.PackageType
               );

            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);
            Assert.False(result);
        }

        [Fact]
        public void ShouldReturnFalseForNonResolvedInPublishedApps()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePathRefs, TestLibraryFactory.SecondAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.PackageType
               );

            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            resolver.TryResolveAssemblyPaths(library, assemblies).Should().Be(false);
            assemblies.Should().BeEmpty();
        }

        [Fact]
        public void ShouldSkipForNonResolvedInNonPublishedApps()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.PackageType
               );

            var resolver = CreateResolver(fileSystem);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);
            Assert.False(result);
        }

        private static AppBaseCompilationAssemblyResolver CreateResolver(IFileSystem fileSystem)
        {
            return new AppBaseCompilationAssemblyResolver(fileSystem, BasePath, DependencyContextPaths);
        }
    }
}
