// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Moq;
using Xunit;
using FluentAssertions;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class CompositeResolverTests
    {
        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void ReturnsFirstSuccessfulResolve()
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

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void PassesLibraryToAllResolvers()
        {
            var fail = new Mock<ICompilationAssemblyResolver>();
            var failTwo = new Mock<ICompilationAssemblyResolver>();
            var resolvers = new[]
            {
                fail.Object,
                failTwo.Object
            };

            var library = TestLibraryFactory.Create();

            var resolver = new CompositeCompilationAssemblyResolver(resolvers);
            var result = resolver.TryResolveAssemblyPaths(library, null);

            fail.Verify(r => r.TryResolveAssemblyPaths(library, null), Times.Once());
            failTwo.Verify(r => r.TryResolveAssemblyPaths(library, null), Times.Once());
        }

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
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
            var library = TestLibraryFactory.Create();

            var resolver = new CompositeCompilationAssemblyResolver(resolvers);
            var result = resolver.TryResolveAssemblyPaths(library, assemblies);

            assemblies.Should().Contain("Assembly");
        }
    }
}
