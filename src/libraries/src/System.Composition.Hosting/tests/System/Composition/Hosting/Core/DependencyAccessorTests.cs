// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Composition.Hosting.Core.Tests
{
    public class DependencyAccessorTests
    {
        [Fact]
        public void ResolveDependencies_PromisesReturnsEmpty_ReturnsEmpty()
        {
            var accessor = new SubAccessor { Result = new ExportDescriptorPromise[0] };

            var contract = new CompositionContract(typeof(int));
            Assert.Empty(accessor.ResolveDependencies("Site", contract, true));
            Assert.Same(contract, accessor.Contract);
        }

        [Fact]
        public void ResolveDependencies_PromisesReturnsNonEmpty_ReturnsExpected()
        {
            var descriptor = ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            var target = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", false, () => Enumerable.Empty<CompositionDependency>(), dependencies =>
            {
                return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            });
            var accessor = new SubAccessor { Result = new ExportDescriptorPromise[] { target } };

            var contract = new CompositionContract(typeof(int));
            CompositionDependency dependency = Assert.Single(accessor.ResolveDependencies("Site", contract, true));
            Assert.Same(contract, accessor.Contract);

            Assert.Same(contract,  dependency.Contract);
            Assert.Equal("Site", dependency.Site);
            Assert.True(dependency.IsPrerequisite);
            Assert.Same(target, dependency.Target);
        }

        [Fact]
        public void ResolveDependencies_PromisesReturnsNull_ThrowsArgumentNullException()
        {
            var accessor = new SubAccessor();

            var contract = new CompositionContract(typeof(int));
            AssertExtensions.Throws<ArgumentNullException>("source", () => accessor.ResolveDependencies("Site", contract, true));
            Assert.Same(contract, accessor.Contract);
        }

        [Fact]
        public void ResolveDependencies_PromisesReturnsNullObjectInArray_ThrowsArgumentNullException()
        {
            var accessor = new SubAccessor { Result = new ExportDescriptorPromise[] { null } };

            var contract = new CompositionContract(typeof(int));
            AssertExtensions.Throws<ArgumentNullException>("target", () => accessor.ResolveDependencies("Site", contract, true));
            Assert.Same(contract, accessor.Contract);
        }

        [Fact]
        public void ResolveRequiredDependency_PromisesReturnsEmpty_ReturnsMissing()
        {
            var accessor = new SubAccessor { Result = new ExportDescriptorPromise[0] };

            var contract = new CompositionContract(typeof(int));
            CompositionDependency dependency = accessor.ResolveRequiredDependency("Site", contract, true);
            Assert.Same(contract, accessor.Contract);

            Assert.Same(contract, dependency.Contract);
            Assert.Equal("Site", dependency.Site);
            Assert.False(dependency.IsPrerequisite);
            Assert.Null(dependency.Target);
        }

        [Fact]
        public void ResolveRequiredDependency_PromisesReturnsOneTarget_ReturnsStatisfied()
        {
            var descriptor = ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            var target = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", false, () => Enumerable.Empty<CompositionDependency>(), dependencies =>
            {
                return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            });
            var accessor = new SubAccessor { Result = new ExportDescriptorPromise[] { target } };

            var contract = new CompositionContract(typeof(int));
            CompositionDependency dependency = accessor.ResolveRequiredDependency("Site", contract, true);
            Assert.Same(contract, accessor.Contract);

            Assert.Same(contract, dependency.Contract);
            Assert.Equal("Site", dependency.Site);
            Assert.True(dependency.IsPrerequisite);
            Assert.Same(target, dependency.Target);
        }

        [Fact]
        public void ResolveRequiredDependency_PromisesReturnsMultipleTargets_ReturnsStatisfied()
        {
            var descriptor = ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            var target = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", false, () => Enumerable.Empty<CompositionDependency>(), dependencies =>
            {
                return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            });
            var accessor = new SubAccessor { Result = new ExportDescriptorPromise[] { target, target } };

            var contract = new CompositionContract(typeof(int));
            CompositionDependency dependency = accessor.ResolveRequiredDependency("Site", contract, true);
            Assert.Same(contract, accessor.Contract);

            Assert.Same(contract, dependency.Contract);
            Assert.Equal("Site", dependency.Site);
            Assert.False(dependency.IsPrerequisite);
            Assert.Null(dependency.Target);
        }

        [Fact]
        public void ResolveRequiredDependency_PromisesReturnsNull_ThrowsArgumentNullException()
        {
            var accessor = new SubAccessor();

            var contract = new CompositionContract(typeof(int));
            AssertExtensions.Throws<ArgumentNullException>("source", () => accessor.ResolveRequiredDependency("Site", contract, true));
            Assert.Same(contract, accessor.Contract);
        }

        [Fact]
        public void ResolveRequiredDependency_PromisesReturnsNullObjectInArray_ThrowsArgumentNullException()
        {
            var accessor = new SubAccessor { Result = new ExportDescriptorPromise[] { null } };

            var contract = new CompositionContract(typeof(int));
            AssertExtensions.Throws<ArgumentNullException>("target", () => accessor.ResolveRequiredDependency("Site", contract, true));
            Assert.Same(contract, accessor.Contract);
        }

        private class SubAccessor : DependencyAccessor
        {
            public CompositionContract Contract { get; set; }
            public IEnumerable<ExportDescriptorPromise> Result { get; set; }

            protected override IEnumerable<ExportDescriptorPromise> GetPromises(CompositionContract exportKey)
            {
                Contract = exportKey;
                return Result;
            }
        }

        private static object Activator(LifetimeContext context, CompositionOperation operation) => "hi";
    }
}
