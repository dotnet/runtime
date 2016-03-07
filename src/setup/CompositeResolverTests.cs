// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Moq;
using Xunit;
using FluentAssertions;

namespace StreamForwarderTests
{
    public class CompositeResolverTests
    {
        [Fact]
        public void ReturnsFirstSuccesfullResolve()
        {
            var fail = new Mock<ICompilationAssemblyResolver>();
            var success = new Mock<ICompilationAssemblyResolver>();
            success.Setup(r => r.TryResolveAssemblyPaths(It.IsAny<CompilationLibrary>(), It.IsAny<List<string>>()))
                .Returns(true);

            var failTwo = new Mock<ICompilationAssemblyResolver>();

            var resolvers = new[]
            {
                fail.Object,
                success.Object,
                failTwo.Object
            };

            var resolver = new CompositeCompilationAssemblyResolver(resolvers);
            var result = resolver.TryResolveAssemblyPaths(null, null);

            Assert.True(result);

            fail.Verify(r => r.TryResolveAssemblyPaths(It.IsAny<CompilationLibrary>(), It.IsAny<List<string>>()),
                Times.Once());
            success.Verify(r => r.TryResolveAssemblyPaths(It.IsAny<CompilationLibrary>(), It.IsAny<List<string>>()),
                Times.Once());
            failTwo.Verify(r => r.TryResolveAssemblyPaths(It.IsAny<CompilationLibrary>(), It.IsAny<List<string>>()),
                Times.Never());
        }

        [Fact]
        public void PassesLibraryToAllResolvers()
        {
            var fail = new Mock<ICompilationAssemblyResolver>();
            var failTwo = new Mock<ICompilationAssemblyResolver>();
            var resolvers = new[]
            {
                fail.Object,
                failTwo.Object
            };

            var library = new CompilationLibrary(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                Enumerable.Empty<string>(),
                Enumerable.Empty<Dependency>(),
                false);

            var resolver = new CompositeCompilationAssemblyResolver(resolvers);
            var result = resolver.TryResolveAssemblyPaths(library, null);

            fail.Verify(r => r.TryResolveAssemblyPaths(library, null), Times.Once());
            failTwo.Verify(r => r.TryResolveAssemblyPaths(library, null), Times.Once());
        }

        [Fact]
        public void PopulatedAssemblies()
        {
            var fail = new Mock<ICompilationAssemblyResolver>();
            var success = new Mock<ICompilationAssemblyResolver>();
            success.Setup(r => r.TryResolveAssemblyPaths(It.IsAny<CompilationLibrary>(), It.IsAny<List<string>>()))
                .Returns(true)
                .Callback((CompilationLibrary l, List<string> a) =>
                {
                    a.Add("Assembly");
                });

            var resolvers = new[]
            {
                fail.Object,
                success.Object
            };

            var assemblies = new List<string>();
            var library = new CompilationLibrary(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                Enumerable.Empty<string>(),
                Enumerable.Empty<Dependency>(),
                false);

            var resolver = new CompositeCompilationAssemblyResolver(resolvers);
            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            assemblies.Should().Contain("Assembly");
        }
    }
}
