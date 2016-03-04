using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Graph;
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class DependencyContextBuilderTests
    {
        private string _referenceAssembliesPath = Path.Combine("reference", "assemblies");
        private NuGetFramework _defaultFramework;
        private string _defaultName = "Library.Name";
        private string _defaultHash = "Hash";
        private NuGetVersion _defaultVersion = new NuGetVersion(1, 2, 3, new []{"dev"}, string.Empty);

        public DependencyContext Build(CommonCompilerOptions compilerOptions = null,
            IEnumerable<LibraryExport> compilationExports = null,
            IEnumerable<LibraryExport> runtimeExports = null,
            NuGetFramework target = null,
            string runtime = null)
        {
            _defaultFramework = NuGetFramework.Parse("net451");
            return new DependencyContextBuilder(_referenceAssembliesPath).Build(
                compilerOptions ?? new CommonCompilerOptions(),
                compilationExports ?? new LibraryExport[] { },
                runtimeExports ?? new LibraryExport[] {},
                target ?? _defaultFramework,
                runtime ?? string.Empty);
        }

        [Fact]
        public void PreservesCompilationOptions()
        {
            var context = Build(new CommonCompilerOptions()
            {
                AllowUnsafe = true,
                Defines = new[] {"Define", "D"},
                DelaySign = true,
                EmitEntryPoint = true,
                GenerateXmlDocumentation = true,
                KeyFile = "Key.snk",
                LanguageVersion = "C#8",
                Optimize = true,
                Platform = "Platform",
                PublicSign = true,
                WarningsAsErrors = true
            });

            context.CompilationOptions.AllowUnsafe.Should().Be(true);
            context.CompilationOptions.DelaySign.Should().Be(true);
            context.CompilationOptions.EmitEntryPoint.Should().Be(true);
            context.CompilationOptions.GenerateXmlDocumentation.Should().Be(true);
            context.CompilationOptions.Optimize.Should().Be(true);
            context.CompilationOptions.PublicSign.Should().Be(true);
            context.CompilationOptions.WarningsAsErrors.Should().Be(true);

            context.CompilationOptions.Defines.Should().BeEquivalentTo(new[] { "Define", "D" });
            context.CompilationOptions.KeyFile.Should().Be("Key.snk");
            context.CompilationOptions.LanguageVersion.Should().Be("C#8");
            context.CompilationOptions.Platform.Should().Be("Platform");
        }


        private LibraryExport Export(
            LibraryDescription description,
            IEnumerable<LibraryAsset> compilationAssemblies = null,
            IEnumerable<LibraryAsset> runtimeAssemblies = null)
        {
            return new LibraryExport(
                description,
                compilationAssemblies ?? Enumerable.Empty<LibraryAsset>(),
                Enumerable.Empty<LibraryAsset>(),
                runtimeAssemblies ?? Enumerable.Empty<LibraryAsset>(),
                Enumerable.Empty<LibraryAsset>(),
                Enumerable.Empty<LibraryAsset>(),
                Enumerable.Empty<LibraryAsset>(),
                Enumerable.Empty<AnalyzerReference>()
            );
        }

        private PackageDescription PackageDescription(
            string name = null,
            NuGetVersion version = null,
            string hash = null,
            IEnumerable<LibraryRange> dependencies = null,
            bool? servicable = null)
        {
            return new PackageDescription(
                "PATH",
                new LockFilePackageLibrary()
                {
                    Files = new string[] { },
                    IsServiceable = servicable ?? false,
                    Name = name ?? _defaultName,
                    Version = version ?? _defaultVersion,
                    Sha512 = hash ?? _defaultHash
                },
                new LockFileTargetLibrary(),
                dependencies ?? Enumerable.Empty<LibraryRange>(),
                true);
        }

        private ProjectDescription ProjectDescription(
            string name = null,
            NuGetVersion version = null,
            IEnumerable<LibraryRange> dependencies = null)
        {
            return new ProjectDescription(
                new LibraryRange(
                    name ?? _defaultName,
                    new VersionRange(version ?? _defaultVersion),
                    LibraryType.Project,
                    LibraryDependencyType.Default
                    ),
                new Project(),
                dependencies ?? Enumerable.Empty<LibraryRange>(),
                new TargetFrameworkInformation(),
                true);
        }

        private LibraryDescription ReferenceAssemblyDescription(
           string name = null,
           NuGetVersion version = null)
        {
            return new LibraryDescription(
                new LibraryIdentity(
                    name ?? _defaultName,
                    version ?? _defaultVersion,
                    LibraryType.ReferenceAssembly),
                string.Empty, // Framework assemblies don't have hashes
                "PATH",
                Enumerable.Empty<LibraryRange>(),
                _defaultFramework,
                true,
                true);
        }

        [Fact]
        public void FillsRuntimeAndTarget()
        {
            var context = Build(target: new NuGetFramework("SomeFramework",new Version(1,2)), runtime: "win8-32");
            context.Runtime.Should().Be("win8-32");
            context.Target.Should().Be("SomeFramework,Version=v1.2");
        }

        [Fact]
        public void TakesServicableFromPackageDescription()
        {
            var context = Build(runtimeExports: new[]
            {
                Export(PackageDescription("Pack.Age", servicable: true))
            });

            var lib = context.RuntimeLibraries.Single();
            lib.Serviceable.Should().BeTrue();
        }

        [Fact]
        public void FillsRuntimeLibraryProperties()
        {
            var context = Build(runtimeExports: new[]
            {
                Export(PackageDescription("Pack.Age",
                    servicable: true,
                    hash: "Hash",
                    version: new NuGetVersion(1,2,3),
                    dependencies: new []
                    {
                        new LibraryRange()
                    }))
            });

            var lib = context.RuntimeLibraries.Single();
            lib.Serviceable.Should().BeTrue();
        }

    }
}
