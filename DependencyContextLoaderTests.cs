using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class DependencyContextTests
    {
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
                "Framework",
                "runtime",
                true,
                CompilationOptions.Default,
                compilationLibraries,
                runtimeLibraries,
                new RuntimeFallbacks[] { });

            var contextRedist = new DependencyContext(
                "Framework",
                "runtime",
                true,
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

        public void MergeMergesRuntimeGraph()
        {
            var context = new DependencyContext(
                "Framework",
                "runtime",
                true,
                CompilationOptions.Default,
                Enumerable.Empty<CompilationLibrary>(),
                Enumerable.Empty<RuntimeLibrary>(),
                new RuntimeFallbacks[]
                {
                    new RuntimeFallbacks("win8-x64", new [] { "win8" }),
                });

            var contextRedist = new DependencyContext(
                "Framework",
                "runtime",
                true,
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

        private CompilationLibrary CreateCompilation(string name)
        {
            return new CompilationLibrary(
                "project",
                name,
                "1.1.1",
                "HASH",
                new string[] { },
                new Dependency[] { },
                false);
        }

        private RuntimeLibrary CreateRuntime(string name)
        {
            return new RuntimeLibrary(
                "project",
                name,
                "1.1.1",
                "HASH",
                new RuntimeAssembly[] { },
                new string[] { },
                new ResourceAssembly[] { },
                new RuntimeTarget[] { },
                new Dependency[] {},
                false);
        }
    }
}
