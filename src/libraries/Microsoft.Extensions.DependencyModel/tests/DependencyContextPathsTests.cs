// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class DependencyContextPathsTests
    {
        [Fact]
        public void CreateWithNulls()
        {
            var paths = DependencyContextPaths.Create(null, null);

            paths.Application.Should().BeNull();
            paths.SharedRuntime.Should().BeNull();
            paths.NonApplicationPaths.Should().BeEmpty();
        }

        [Fact]
        public void CreateWithNullFxDeps()
        {
            var paths = DependencyContextPaths.Create("foo.deps.json", null);

            paths.Application.Should().Be("foo.deps.json");
            paths.SharedRuntime.Should().BeNull();
            paths.NonApplicationPaths.Should().BeEmpty();
        }

        [Fact]
        public void CreateWithDepsFilesContainingFxDeps()
        {
            var paths = DependencyContextPaths.Create("foo.deps.json;fx.deps.json", "fx.deps.json");

            paths.Application.Should().Be("foo.deps.json");
            paths.SharedRuntime.Should().Be("fx.deps.json");
            paths.NonApplicationPaths.Should().BeEquivalentTo("fx.deps.json");
        }

        [Fact]
        public void CreateWithExtraContainingFxDeps()
        {
            var paths = DependencyContextPaths.Create(
                "foo.deps.json;fx.deps.json;extra.deps.json;extra2.deps.json", 
                "fx.deps.json");

            paths.Application.Should().Be("foo.deps.json");
            paths.SharedRuntime.Should().Be("fx.deps.json");
            paths.NonApplicationPaths.Should().BeEquivalentTo("fx.deps.json", "extra.deps.json", "extra2.deps.json");
        }

        [Fact]
        public void CreateWithExtraNotContainingFxDeps()
        {
            var paths = DependencyContextPaths.Create(
                "foo.deps.json;extra.deps.json;extra2.deps.json", 
                "fx.deps.json");

            paths.Application.Should().Be("foo.deps.json");
            paths.SharedRuntime.Should().Be("fx.deps.json");
            paths.NonApplicationPaths.Should().BeEquivalentTo("extra.deps.json", "extra2.deps.json");
        }
    }
}
