// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Xunit;
using FluentAssertions;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class AppBaseResolverTests
    {
        private static string BasePath = Path.Combine("Base","Path");
        private static string BasePathRefs = Path.Combine(BasePath, "refs");

        [Fact]
        public void ResolvesProjectType()
        {
            var resolver = new AppBaseCompilationAssemblyResolver();
            var library = TestLibraryFactory.Create(
                TestLibraryFactory.ProjectType,
                assemblies: TestLibraryFactory.EmptyAssemblies);

            var result = resolver.TryResolveAssemblyPaths(library, null);

            Assert.True(result);
        }

        [Fact]
        public void ResolvesPackageType()
        {
            var resolver = new AppBaseCompilationAssemblyResolver();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.PackageType,
               assemblies: TestLibraryFactory.EmptyAssemblies);

            var result = resolver.TryResolveAssemblyPaths(library, null);

            Assert.True(result);
        }

        [Fact]
        public void ResolvesReferenceAssemblyType()
        {
            var resolver = new AppBaseCompilationAssemblyResolver();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.ReferenceAssemblyType,
               assemblies: TestLibraryFactory.EmptyAssemblies);

            var result = resolver.TryResolveAssemblyPaths(library, null);

            Assert.True(result);
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
            var resolver = new AppBaseCompilationAssemblyResolver(fileSystem, BasePath);
            var assemblies = new List<string>();

            var exception = Assert.Throws<InvalidOperationException>(() => resolver.TryResolveAssemblyPaths(library, assemblies));
            exception.Message.Should()
                .Contain(BasePath)
                .And.Contain(BasePathRefs)
                .And.Contain(TestLibraryFactory.SecondAssembly);
        }

        [Fact]
        public void ResolvesIfAllAreInBaseDir()
        {
            var fileSystem = FileSystemMockBuilder
                .Create()
                .AddFiles(BasePath, TestLibraryFactory.DefaultAssembly, TestLibraryFactory.SecondAssembly)
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.ReferenceAssemblyType,
               assemblies: TestLibraryFactory.TwoAssemblies);
            var resolver = new AppBaseCompilationAssemblyResolver(fileSystem, BasePath);
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

            var resolver = new AppBaseCompilationAssemblyResolver(fileSystem, BasePath);
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

            var resolver = new AppBaseCompilationAssemblyResolver(fileSystem, BasePath);
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
                .Build();
            var library = TestLibraryFactory.Create(
               TestLibraryFactory.ReferenceAssemblyType
               );

            var resolver = new AppBaseCompilationAssemblyResolver(fileSystem, BasePath);
            var assemblies = new List<string>();

            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            Assert.True(result);
            assemblies.Should().HaveCount(1);
            assemblies.Should().Contain(Path.Combine(BasePathRefs, TestLibraryFactory.DefaultAssembly));
        }


    }
}
