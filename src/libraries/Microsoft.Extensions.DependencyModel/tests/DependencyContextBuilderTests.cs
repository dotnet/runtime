// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            bool portable = false,
            NuGetFramework target = null,
            string runtime = null)
        {
            _defaultFramework = NuGetFramework.Parse("net451");
            return new DependencyContextBuilder(_referenceAssembliesPath).Build(
                compilerOptions,
                compilationExports ?? new LibraryExport[] { },
                runtimeExports ?? new LibraryExport[] {},
                portable,
                target ?? _defaultFramework,
                runtime ?? string.Empty);
        }

        [Fact]
        public void PreservesCompilationOptions()
        {
            var context = Build(new CommonCompilerOptions()
            {
                AllowUnsafe = true,
                Defines = new[] { "Define", "D" },
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


        [Fact]
        public void AlowsNullCompilationOptions()
        {
            var context = Build(compilerOptions: null);

            context.CompilationOptions.Should().Be(CompilationOptions.Default);
        }

        [Fact]
        public void SetsPortableFlag()
        {
            var context = Build(portable: true);

            context.Target.IsPortable.Should().BeTrue();
        }

        [Fact]
        public void FillsRuntimeAndTarget()
        {
            var context = Build(target: new NuGetFramework("SomeFramework",new Version(1,2)), runtime: "win8-x86");
            context.Target.Runtime.Should().Be("win8-x86");
            context.Target.Framework.Should().Be("SomeFramework,Version=v1.2");
        }

        [Fact]
        public void SetsServiceableToTrueForPackageDescriptions()
        {
            var context = Build(runtimeExports: new[]
            {
                Export(PackageDescription("Pack.Age", servicable: false))
            });

            var lib = context.RuntimeLibraries.Single();
            lib.Serviceable.Should().BeTrue();
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
                Export(
                    PackageDescription(
                        "Pack.Age",
                        servicable: true,
                        hash: "Hash",
                        version: new NuGetVersion(1, 2, 3),
                        dependencies: new[]
                        {
                            new LibraryRange("System.Collections",
                                new VersionRange(new NuGetVersion(2, 1, 2)),
                                LibraryType.ReferenceAssembly,
                                LibraryDependencyType.Default)
                        },
                        path: "path/TO/package",
                        hashPath: "Pack.Age.1.2.3.nupkg.sha512"),
                    resourceAssemblies: new[]
                    {
                        new LibraryResourceAssembly(
                            new LibraryAsset("Dll", "en-US/Pack.Age.resources.dll", ""),
                            "en-US"
                            )
                    },
                    runtimeAssemblyGroups: new[]
                    {
                        new LibraryAssetGroup(
                            new LibraryAsset("Dll", "lib/Pack.Age.dll", "")),
                        new LibraryAssetGroup("win8-x64",
                            new LibraryAsset("Dll", "win8-x64/Pack.Age.dll", ""))
                    },
                    nativeLibraryGroups: new []
                    {
                        new LibraryAssetGroup("win8-x64",
                            new LibraryAsset("Dll", "win8-x64/Pack.Age.native.dll", ""))
                    }),
                Export(
                    ReferenceAssemblyDescription("System.Collections",
                        version: new NuGetVersion(3, 3, 3)),
                        runtimeAssemblyGroups: new[]
                        {
                            new LibraryAssetGroup(
                                new LibraryAsset("Dll", "System.Collections.dll", "System.Collections.dll"))
                        })
            });

            context.RuntimeLibraries.Should().HaveCount(2);

            var lib = context.RuntimeLibraries.Should().Contain(l => l.Name == "Pack.Age").Subject;
            lib.Type.Should().Be("package");
            lib.Serviceable.Should().BeTrue();
            lib.Hash.Should().Be("sha512-Hash");
            lib.Version.Should().Be("1.2.3");
            lib.Dependencies.Should().OnlyContain(l => l.Name == "System.Collections" && l.Version == "3.3.3");
            lib.ResourceAssemblies.Should().OnlyContain(l => l.Path == "en-US/Pack.Age.resources.dll" && l.Locale == "en-US");

            // When ProjectModel supports path and hashPath in the lock file library, this should assert the values
            // provided above.
            lib.Path.Should().BeNull();
            lib.HashPath.Should().BeNull();

            lib.RuntimeAssemblyGroups.GetDefaultAssets().Should().OnlyContain(l => l == "lib/Pack.Age.dll");
            lib.RuntimeAssemblyGroups.GetRuntimeAssets("win8-x64").Should().OnlyContain(l => l == "win8-x64/Pack.Age.dll");
            lib.NativeLibraryGroups.GetRuntimeAssets("win8-x64").Should().OnlyContain(l => l == "win8-x64/Pack.Age.native.dll");

            var asm = context.RuntimeLibraries.Should().Contain(l => l.Name == "System.Collections").Subject;
            asm.Type.Should().Be("referenceassembly");
            asm.Version.Should().Be("3.3.3");
            asm.Hash.Should().BeEmpty();
            asm.Dependencies.Should().BeEmpty();
            asm.RuntimeAssemblyGroups.GetDefaultAssets().Should().OnlyContain(l => l == "System.Collections.dll");
            asm.Path.Should().BeNull();
            asm.HashPath.Should().BeNull();
        }

        [Fact]
        public void FiltersDuplicatedDependencies()
        {
            var context = Build(runtimeExports: new[]
              {
                Export(PackageDescription("Pack.Age",
                    dependencies: new[]
                    {
                        new LibraryRange("System.Collections",
                            new VersionRange(new NuGetVersion(2, 0, 0)),
                            LibraryType.ReferenceAssembly,
                            LibraryDependencyType.Default),
                        new LibraryRange("System.Collections",
                            new VersionRange(new NuGetVersion(2, 1, 2)),
                            LibraryType.Package,
                            LibraryDependencyType.Default)
                    })
                    ),
                Export(ReferenceAssemblyDescription("System.Collections",
                    version: new NuGetVersion(2, 0, 0)))
            });

            context.RuntimeLibraries.Should().HaveCount(2);

            var lib = context.RuntimeLibraries.Should().Contain(l => l.Name == "Pack.Age").Subject;
            lib.Dependencies.Should().HaveCount(1);
            lib.Dependencies.Should().OnlyContain(l => l.Name == "System.Collections" && l.Version == "2.0.0");
        }

        [Fact]
        public void FillsCompileLibraryProperties()
        {
            var context = Build(compilationExports: new[]
            {
                Export(PackageDescription("Pack.Age",
                    servicable: true,
                    hash: "Hash",
                    version: new NuGetVersion(1, 2, 3),
                    dependencies: new[]
                    {
                        new LibraryRange("System.Collections",
                            new VersionRange(new NuGetVersion(2, 1, 2)),
                            LibraryType.ReferenceAssembly,
                            LibraryDependencyType.Default)
                    },
                    path: "path/TO/package",
                    hashPath: "Pack.Age.1.2.3.nupkg.sha512"),
                    compilationAssemblies: new[]
                    {
                        new LibraryAsset("Dll", "lib/Pack.Age.dll", ""),
                    }
                    ),
                Export(ReferenceAssemblyDescription("System.Collections",
                    version: new NuGetVersion(3, 3, 3)),
                    compilationAssemblies: new[]
                    {
                        new LibraryAsset("Dll", "", "System.Collections.dll"),
                    })
            });

            context.CompileLibraries.Should().HaveCount(2);

            var lib = context.CompileLibraries.Should().Contain(l => l.Name == "Pack.Age").Subject;
            lib.Type.Should().Be("package");
            lib.Serviceable.Should().BeTrue();
            lib.Hash.Should().Be("sha512-Hash");
            lib.Version.Should().Be("1.2.3");
            lib.Dependencies.Should().OnlyContain(l => l.Name == "System.Collections" && l.Version == "3.3.3");
            lib.Assemblies.Should().OnlyContain(a => a == "lib/Pack.Age.dll");

            // When ProjectModel supports path and hashPath in the lock file library, this should assert the values
            // provided above.
            lib.Path.Should().BeNull();
            lib.HashPath.Should().BeNull();

            var asm = context.CompileLibraries.Should().Contain(l => l.Name == "System.Collections").Subject;
            asm.Type.Should().Be("referenceassembly");
            asm.Version.Should().Be("3.3.3");
            asm.Hash.Should().BeEmpty();
            asm.Dependencies.Should().BeEmpty();
            asm.Assemblies.Should().OnlyContain(a => a == "System.Collections.dll");
            asm.Path.Should().BeNull();
            asm.HashPath.Should().BeNull();
        }

        [Fact]
        public void FillsResources()
        {
            var context = Build(runtimeExports: new[]
            {
                Export(PackageDescription("Pack.Age", version: new NuGetVersion(1, 2, 3)),
                    resourceAssemblies: new []
                    {
                        new LibraryResourceAssembly(new LibraryAsset("Dll", "resources/en-US/Pack.Age.dll", ""), "en-US")
                    })
            });

            context.RuntimeLibraries.Should().HaveCount(1);

            var lib = context.RuntimeLibraries.Should().Contain(l => l.Name == "Pack.Age").Subject;
            lib.ResourceAssemblies.Should().OnlyContain(l => l.Locale == "en-US" && l.Path == "resources/en-US/Pack.Age.dll");
        }

        [Fact]
        public void ReferenceAssembliesPathRelativeToDefaultRoot()
        {
            var context = Build(compilationExports: new[]
            {
                Export(ReferenceAssemblyDescription("System.Collections",
                    version: new NuGetVersion(3, 3, 3)),
                    compilationAssemblies: new[]
                    {
                        new LibraryAsset("Dll", "", Path.Combine(_referenceAssembliesPath, "sub", "System.Collections.dll"))
                    })
            });

            var asm = context.CompileLibraries.Should().Contain(l => l.Name == "System.Collections").Subject;
            asm.Assemblies.Should().OnlyContain(a => a == Path.Combine("sub", "System.Collections.dll"));
        }

        [Fact]
        public void SkipsBuildDependencies()
        {
            var context = Build(compilationExports: new[]
            {
                Export(PackageDescription("Pack.Age",
                    dependencies: new[]
                    {
                        new LibraryRange("System.Collections",
                            new VersionRange(new NuGetVersion(2, 1, 2)),
                            LibraryType.ReferenceAssembly,
                            LibraryDependencyType.Build)
                    })
                    ),
                Export(ReferenceAssemblyDescription("System.Collections",
                    version: new NuGetVersion(3, 3, 3)))
            });

            var lib = context.CompileLibraries.Should().Contain(l => l.Name == "Pack.Age").Subject;
            lib.Dependencies.Should().BeEmpty();
        }

        [Fact]
        public void GeneratesRuntimeSignatureOutOfPackageNamesAndVersions()
        {
            var context = Build(runtimeExports: new[]
            {
                Export(PackageDescription("Pack.Age", new NuGetVersion(1, 2, 3))),
                Export(PackageDescription("Pack.Age", new NuGetVersion(1, 2, 3))),
            });

            context.Target.RuntimeSignature.Should().Be("d0fc00006ed69e4aae80383dda08599a6892fd31");
        }


        private LibraryExport Export(
            LibraryDescription description,
            IEnumerable<LibraryAsset> compilationAssemblies = null,
            IEnumerable<LibraryAssetGroup> runtimeAssemblyGroups = null,
            IEnumerable<LibraryAssetGroup> nativeLibraryGroups = null,
            IEnumerable<LibraryResourceAssembly> resourceAssemblies = null)
        {
            return LibraryExportBuilder.Create(description)
                .WithCompilationAssemblies(compilationAssemblies)
                .WithRuntimeAssemblyGroups(runtimeAssemblyGroups)
                .WithNativeLibraryGroups(nativeLibraryGroups)
                .WithResourceAssemblies(resourceAssemblies)
                .Build();
        }

        private PackageDescription PackageDescription(
            string name = null,
            NuGetVersion version = null,
            string hash = null,
            IEnumerable<LibraryRange> dependencies = null,
            bool? servicable = null,
            string path = null,
            string hashPath = null)
        {
            // The LockFilePackageLibrary type in Microsoft.DotNet.ProjectModel currently does not
            // support the "path" property. Therefore, the path property to this method is ignored
            // and calling tests should assert that the value is not plumbed through.
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
                true,
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
    }
}
