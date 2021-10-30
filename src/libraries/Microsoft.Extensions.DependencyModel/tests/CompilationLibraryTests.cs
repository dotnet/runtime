// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.Extensions.DependencyModel.Resolution;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class CompilationLibraryTests
    {
        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void ResolveReferencePathsAcceptsCustomResolvers()
        {
            var fail = new Mock<ICompilationAssemblyResolver>();
            var success = new Mock<ICompilationAssemblyResolver>();
            success.Setup(r => r.TryResolveAssemblyPaths(It.IsAny<CompilationLibrary>(), It.IsAny<List<string>>()))
                .Callback((CompilationLibrary l, List<string> a) =>
                {
                    a.Add("Assembly");
                })
                .Returns(true);

            var failTwo = new Mock<ICompilationAssemblyResolver>();

            var resolvers = new[]
            {
                fail.Object,
                success.Object,
                failTwo.Object
            };

            var library = TestLibraryFactory.Create();

            var result = library.ResolveReferencePaths(resolvers);

            result.ShouldBeEquivalentTo(new[] { "Assembly" });

            fail.Verify(r => r.TryResolveAssemblyPaths(It.IsAny<CompilationLibrary>(), It.IsAny<List<string>>()),
                Times.Once());
            success.Verify(r => r.TryResolveAssemblyPaths(It.IsAny<CompilationLibrary>(), It.IsAny<List<string>>()),
                Times.Once());
            failTwo.Verify(r => r.TryResolveAssemblyPaths(It.IsAny<CompilationLibrary>(), It.IsAny<List<string>>()),
                Times.Never());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60583", TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void ResolveReferencePathsAcceptsNullCustomResolvers()
        {
            var library = TestLibraryFactory.Create();
            var assemblyPath = Path.Combine(AppContext.BaseDirectory, "refs", library.Name + ".dll");
            Directory.CreateDirectory(Path.GetDirectoryName(assemblyPath));
            File.WriteAllText(assemblyPath, "hello");

            try
            {
                var result = library.ResolveReferencePaths(null);
                result.ShouldBeEquivalentTo(new[] { assemblyPath });
            }
            finally
            {
                File.Delete(assemblyPath);
            }
        }

        [Fact]
        public void ResolveReferencePathsThrowsOnNotFound()
        {
            var library = TestLibraryFactory.Create();
            Assert.Throws<InvalidOperationException>(() => library.ResolveReferencePaths(null));
        }
    }
}
