// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class DependencyContextTests
    {
        [Theory]
        [InlineData("System.Collections.dll", "System.Collections")]
        [InlineData("System.Collections.ni.dll", "System.Collections")]
        [InlineData("mscorlib", "mscorlib")]
        public void GetRuntimeAssemblyNamesExtractsCorrectAssemblyName(string path, string expected)
        {
            var context = new DependencyContext(new TargetInfo(".NETStandard,Version=v1.3", string.Empty, string.Empty, true),
                compilationOptions: CompilationOptions.Default,
                compileLibraries: new CompilationLibrary[] { },
                runtimeLibraries: new[] {
                    new RuntimeLibrary("package", "System.Banana", "1.0.0", "hash",
                        new [] {
                            new RuntimeAssetGroup(string.Empty, Path.Combine("lib", path))
                        },
                        new RuntimeAssetGroup[] { },
                        new ResourceAssembly[] { },
                        new Dependency[] { },
                        serviceable: false,
                        path: "PackagePath",
                        hashPath: "PackageHashPath")
                },
                runtimeGraph: new RuntimeFallbacks[] { });

            var assets = context.GetDefaultAssemblyNames();
            assets.Should().OnlyContain(a => a.Name == expected);
        }

        [Fact]
        public void GetRuntimeAssemblyNamesReturnsRIDLessAssetsIfNoRIDSpecificAssetsInLibrary()
        {
            var context = BuildTestContext();

            var assets = context.GetRuntimeAssemblyNames("win7-x64");
            assets.Should().OnlyContain(a => a.Name == "System.Collections");
        }

        [Fact]
        public void GetRuntimeAssemblyNamesReturnsMostSpecificAssetIfRIDSpecificAssetInLibrary()
        {
            var context = BuildTestContext();

            var assets = context.GetRuntimeAssemblyNames("win81-x64");
            assets.Should().OnlyContain(a => a.Name == "System.Collections");
        }

        [Fact]
        public void GetRuntimeAssemblyNamesReturnsEmptyIfEmptyRuntimeGroupPresent()
        {
            var context = BuildTestContext();

            var assets = context.GetRuntimeAssemblyNames("win10-x64");
            assets.Should().BeEmpty();
        }

        [Fact]
        public void GetRuntimeNativeAssetsReturnsEmptyIfNoGroupsMatch()
        {
            var context = BuildTestContext();

            var assets = context.GetRuntimeNativeAssets("win7-x64");
            assets.Should().BeEmpty();
        }

        [Fact]
        public void GetRuntimeNativeAssetsReturnsMostSpecificAssetIfRIDSpecificAssetInLibrary()
        {
            var context = BuildTestContext();

            var assets = context.GetRuntimeNativeAssets("linux-x64");
            assets.Should().BeEquivalentTo(Path.Combine("runtimes", "linux-x64", "native", "System.Banana.Native.so"));
        }

        [Fact]
        public void GetRuntimeNativeAssetsReturnsEmptyIfEmptyRuntimeGroupPresent()
        {
            var context = BuildTestContext();

            var assets = context.GetRuntimeNativeAssets("rhel-x64");
            assets.Should().BeEmpty();
        }

        private DependencyContext BuildTestContext()
        {
            return new DependencyContext(new TargetInfo(".NETStandard,Version=v1.3", string.Empty, string.Empty, true),
                compilationOptions: CompilationOptions.Default,
                compileLibraries: new[]
                {
                    new CompilationLibrary("package", "System.Banana", "1.0.0", "hash",
                        new [] { Path.Combine("ref", "netstandard1.3", "System.Banana.dll") },
                        new Dependency[] { },
                        serviceable: false,
                        path: "PackagePath",
                        hashPath: "PackageHashPath")
                },
                runtimeLibraries: new[] {
                    new RuntimeLibrary("package", "System.Banana", "1.0.0", "hash",
                        new [] {
                            new RuntimeAssetGroup(string.Empty, Path.Combine("lib", "netstandard1.3", "System.Collections.dll")),
                            new RuntimeAssetGroup("win10"),
                            new RuntimeAssetGroup("win8", Path.Combine("runtimes", "win8", "lib", "netstandard1.3", "System.Collections.dll"))
                        },
                        new [] {
                            new RuntimeAssetGroup("rhel"),
                            new RuntimeAssetGroup("linux-x64", Path.Combine("runtimes", "linux-x64", "native", "System.Banana.Native.so")),
                            new RuntimeAssetGroup("osx-x64", Path.Combine("runtimes", "osx-x64", "native", "System.Banana.Native.dylib")),

                            // Just here to test we don't fall back through it for the other cases. There's
                            // no such thing as a "unix" native asset since there's no common executable format :)
                            new RuntimeAssetGroup("unix", Path.Combine("runtimes", "osx-x64", "native", "System.Banana.Native"))
                        },
                        new ResourceAssembly[] { },
                        new Dependency[] { },
                        serviceable: false,
                        path: "PackagePath",
                        hashPath: "PackageHashPath")
                },
                runtimeGraph: new[] {
                    new RuntimeFallbacks("win10-x64", "win10", "win81-x64", "win81", "win8-x64", "win8", "win7-x64", "win7", "win-x64", "win", "any", "base"),
                    new RuntimeFallbacks("win81-x64", "win81", "win8-x64", "win8", "win7-x64", "win7", "win-x64", "win", "any", "base"),
                    new RuntimeFallbacks("win8-x64", "win8", "win7-x64", "win7", "win-x64", "win", "any", "base"),
                    new RuntimeFallbacks("win7-x64", "win7", "win-x64", "win", "any", "base"),
                    new RuntimeFallbacks("ubuntu-x64", "ubuntu", "linux-x64", "linux", "unix", "any", "base"),
                    new RuntimeFallbacks("ubuntu.16.04-x64", "ubuntu", "linux-x64", "linux", "unix", "any", "base"),
                    new RuntimeFallbacks("rhel-x64", "rhel", "linux-x64", "linux", "unix", "any", "base"),
                    new RuntimeFallbacks("osx-x64", "osx", "unix", "any", "base"),
                });
        }

        [Fact]
        public void MergeMergesLibraries()
        {
            var compilationLibraries = new[]
            {
                CreateCompilation("PackageA"),
                CreateCompilation("PackageB"),
            };

            var runtimeLibraries = new[]
            {
                CreateRuntime("PackageA"),
                CreateRuntime("PackageB"),
            };

            var compilationLibrariesRedist = new[]
            {
                CreateCompilation("PackageB"),
                CreateCompilation("PackageC"),
            };

            var runtimeLibrariesRedist = new[]
            {
                CreateRuntime("PackageB"),
                CreateRuntime("PackageC"),
            };

            var context = new DependencyContext(
                CreateTargetInfo(),
                CompilationOptions.Default,
                compilationLibraries,
                runtimeLibraries,
                new RuntimeFallbacks[] { });

            var contextRedist = new DependencyContext(
                CreateTargetInfo(),
                CompilationOptions.Default,
                compilationLibrariesRedist,
                runtimeLibrariesRedist,
                new RuntimeFallbacks[] { });

            var result = context.Merge(contextRedist);

            result.CompileLibraries.Should().BeEquivalentTo(new[]
            {
                compilationLibraries[0],
                compilationLibraries[1],
                compilationLibrariesRedist[1],
            });

            result.RuntimeLibraries.Should().BeEquivalentTo(new[]
            {
                runtimeLibraries[0],
                runtimeLibraries[1],
                runtimeLibrariesRedist[1],
            });
        }

        [Fact]
        public void MergeMergesLibrariesWithDifferentCasing()
        {
            var compilationLibraries = new[]
            {
                CreateCompilation("PaCkAgEA"),
            };

            var runtimeLibraries = new[]
            {
                CreateRuntime("PaCkAgEA"),
            };

            var compilationLibrariesRedist = new[]
            {
                CreateCompilation("PackageA"),
            };

            var runtimeLibrariesRedist = new[]
            {
                CreateRuntime("PackageA"),
            };

            var context = new DependencyContext(
                CreateTargetInfo(),
                CompilationOptions.Default,
                compilationLibraries,
                runtimeLibraries,
                new RuntimeFallbacks[] { });

            var contextRedist = new DependencyContext(
                CreateTargetInfo(),
                CompilationOptions.Default,
                compilationLibrariesRedist,
                runtimeLibrariesRedist,
                new RuntimeFallbacks[] { });

            var result = context.Merge(contextRedist);

            result.CompileLibraries.Should().BeEquivalentTo(new[]
            {
                compilationLibraries[0]
            });

            result.RuntimeLibraries.Should().BeEquivalentTo(new[]
            {
                runtimeLibraries[0]
            });
        }

        [Fact]
        public void MergeMergesRuntimeGraph()
        {
            var context = new DependencyContext(
                CreateTargetInfo(),
                CompilationOptions.Default,
                Enumerable.Empty<CompilationLibrary>(),
                Enumerable.Empty<RuntimeLibrary>(),
                new RuntimeFallbacks[]
                {
                    new RuntimeFallbacks("win8-x64", new [] { "win8" }),
                });

            var contextRedist = new DependencyContext(
                CreateTargetInfo(),
                CompilationOptions.Default,
                Enumerable.Empty<CompilationLibrary>(),
                Enumerable.Empty<RuntimeLibrary>(),
                new RuntimeFallbacks[]
                {
                    new RuntimeFallbacks("win8", new [] { "win7-x64", "win7-x86" }),
                });

            var result = context.Merge(contextRedist);
            result.RuntimeGraph.Should().Contain(g => g.Runtime == "win8-x64").
                Subject.Fallbacks.Should().BeEquivalentTo("win8");
            result.RuntimeGraph.Should().Contain(g => g.Runtime == "win8").
                Subject.Fallbacks.Should().BeEquivalentTo("win7-x64", "win7-x86");
        }

        private TargetInfo CreateTargetInfo()
        {
            return new TargetInfo(
                "Framework",
                "runtime",
                "runtimeSignature",
                true);
        }

        private CompilationLibrary CreateCompilation(string name)
        {
            return new CompilationLibrary(
                "project",
                name,
                "1.1.1",
                "HASH",
                new string[] { },
                new Dependency[] { },
                false,
                "path",
                "hashPath");
        }

        private RuntimeLibrary CreateRuntime(string name)
        {
            return new RuntimeLibrary(
                "project",
                name,
                "1.1.1",
                "HASH",
                new RuntimeAssetGroup[] { },
                new RuntimeAssetGroup[] { },
                new ResourceAssembly[] { },
                new Dependency[] { },
                false,
                "path",
                "hashPath");
        }
    }
}
