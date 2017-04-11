// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyModel.Resolution;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class CompilationLibraryTests
    {
        [Fact]
        public void ResolveReferencePathsAcceptsAdditionalResolvers()
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
    }
}
