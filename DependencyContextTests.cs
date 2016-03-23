using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class DependencyContextTests
    {
        [Theory]
        [InlineData("System.Banana.dll", "System.Banana")]
        [InlineData("System.Banana.ni.dll", "System.Banana")]
        [InlineData("FlibbidyFlob", "FlibbidyFlob")]
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
                        serviceable: false)
                },
                runtimeGraph: new RuntimeFallbacks[] { });

            var assets = context.GetDefaultAssemblyNames();
            assets.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetRuntimeAssemblyNamesReturnsRIDLessAssetsIfNoRIDSpecificAssetsInLibrary()
        {
            var context = BuildTestContext();

            var assets = context.GetRuntimeAssemblyNames("win7-x64");
            assets.Should().BeEquivalentTo("System.Banana");
        }

        [Fact]
        public void GetRuntimeAssemblyNamesReturnsMostSpecificAssetIfRIDSpecificAssetInLibrary()
        {
            var context = BuildTestContext();

            var assets = context.GetRuntimeAssemblyNames("win81-x64");
            assets.Should().BeEquivalentTo("System.Banana");
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
                        serviceable: false)
                },
                runtimeLibraries: new[] {
                    new RuntimeLibrary("package", "System.Banana", "1.0.0", "hash",
                        new [] {
                            new RuntimeAssetGroup(string.Empty, Path.Combine("lib", "netstandard1.3", "System.Banana.dll")),
                            new RuntimeAssetGroup("win10"),
                            new RuntimeAssetGroup("win8", Path.Combine("runtimes", "win8", "lib", "netstandard1.3", "System.Banana.dll"))
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
                        serviceable: false)
                },
                runtimeGraph: new[] {
                    new RuntimeFallbacks("win10-x64", "win10", "win81-x64", "win81", "win8-x64", "win8", "win7-x64", "win7", "win-x64", "win", "any", "base"),
                    new RuntimeFallbacks("win81-x64", "win81", "win8-x64", "win8", "win7-x64", "win7", "win-x64", "win", "any", "base"),
                    new RuntimeFallbacks("win8-x64", "win8", "win7-x64", "win7", "win-x64", "win", "any", "base"),
                    new RuntimeFallbacks("win7-x64", "win7", "win-x64", "win", "any", "base"),
                    new RuntimeFallbacks("ubuntu-x64", "ubuntu", "linux-x64", "linux", "unix", "any", "base"),
                    new RuntimeFallbacks("rhel-x64", "rhel", "linux-x64", "linux", "unix", "any", "base"),
                    new RuntimeFallbacks("osx-x64", "osx", "unix", "any", "base"),
                });
        }
    }
}
