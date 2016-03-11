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
            var runtime = new Mock<IRuntimeEnvironment>();
            runtime.SetupGet(r => r.OperatingSystemPlatform).Returns(Platform.Windows);
            
            var environment = EnvironmentMockBuilder.Create()
                .AddVariable("DOTNET_REFERENCE_ASSEMBLIES_PATH", ReferencePath)
                .Build();

            var result = ReferenceAssemblyPathResolver.GetDefaultReferenceAssembliesPath(runtime.Object, FileSystemMockBuilder.Empty, environment);
            result.Should().Be(ReferencePath);
        }

        [Fact]
        public void LooksOnlyOnEnvironmentVariableOnNonWindows()
        {
            var runtime = new Mock<IRuntimeEnvironment>();
            runtime.SetupGet(r => r.OperatingSystemPlatform).Returns(Platform.Linux);

            var result = ReferenceAssemblyPathResolver.GetDefaultReferenceAssembliesPath(runtime.Object, FileSystemMockBuilder.Empty, EnvironmentMockBuilder.Empty);
            result.Should().BeNull();
        }

        [Fact]
        public void ReturnsProgramFiles86AsDefaultLocationOnWin64()
        {
            var runtime = new Mock<IRuntimeEnvironment>();
            runtime.SetupGet(r => r.OperatingSystemPlatform).Returns(Platform.Windows);

            var environment = EnvironmentMockBuilder.Create()
                .AddVariable("ProgramFiles(x86)", "Program Files (x86)")
                .AddVariable("ProgramFiles", "Program Files")
                .Build();

            var result = ReferenceAssemblyPathResolver.GetDefaultReferenceAssembliesPath(runtime.Object, FileSystemMockBuilder.Empty, environment);
            result.Should().Be(Path.Combine("Program Files (x86)", "Reference Assemblies", "Microsoft", "Framework"));
        }

        [Fact]
        public void ReturnsProgramFilesAsDefaultLocationOnWin32()
        {
            var runtime = new Mock<IRuntimeEnvironment>();
            runtime.SetupGet(r => r.OperatingSystemPlatform).Returns(Platform.Windows);

            var environment = EnvironmentMockBuilder.Create()
                .AddVariable("ProgramFiles", "Program Files")
                .Build();

            var result = ReferenceAssemblyPathResolver.GetDefaultReferenceAssembliesPath(runtime.Object, FileSystemMockBuilder.Empty, environment);
            result.Should().Be(Path.Combine("Program Files", "Reference Assemblies", "Microsoft", "Framework"));
        }

        [Fact]
        public void ReturnNet20PathAsFallbackOnWindows()
        {
            var net20Path = Path.Combine("Windows", "Microsoft.NET", "Framework", "v2.0.50727");
            var fileSystem = FileSystemMockBuilder.Create()
                .AddFiles(net20Path, "some.dll")
                .Build();

            var runtime = new Mock<IRuntimeEnvironment>();
            runtime.SetupGet(r => r.OperatingSystemPlatform).Returns(Platform.Windows);

            var environment = EnvironmentMockBuilder.Create()
                .AddVariable("WINDIR", "Windows")
                .Build();

            var result = ReferenceAssemblyPathResolver.GetFallbackSearchPaths(fileSystem, runtime.Object, environment);
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
