// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Composition.Hosting.Core.Tests
{
    public class CompositionDependencyTests
    {
        [Fact]
        public void Missing_Invoke_ReturnsExpected()
        {
            var contract = new CompositionContract(typeof(int));

            CompositionDependency dependency = CompositionDependency.Missing(contract, "Site");
            Assert.Same(contract, dependency.Contract);
            Assert.Equal("Site", dependency.Site);
            Assert.Null(dependency.Target);
            Assert.False(dependency.IsPrerequisite);

            Assert.Equal("Site", dependency.ToString());
        }

        [Fact]
        public void Missing_NullContract_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("contract", () => CompositionDependency.Missing(null, new object()));
        }

        [Fact]
        public void Missing_NullSite_ThrowsArgumentNullException()
        {
            var contract = new CompositionContract(typeof(int));
            AssertExtensions.Throws<ArgumentNullException>("site", () => CompositionDependency.Missing(contract, null));
        }

        [Fact]
        public void Satisfied_Invoke_ReturnsExpected()
        {
            var contract = new CompositionContract(typeof(int));
            var descriptor = ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            var target = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", false, () => Enumerable.Empty<CompositionDependency>(), dependencies =>
            {
                return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            });

            CompositionDependency dependency = CompositionDependency.Satisfied(contract, target, true, "Site");
            Assert.Same(contract, dependency.Contract);
            Assert.Equal("Site", dependency.Site);
            Assert.Same(target, dependency.Target);
            Assert.True(dependency.IsPrerequisite);

            Assert.Equal("'Site' on contract 'Int32' supplied by Origin", dependency.ToString());
        }

        [Fact]
        public void Satisfied_NullContract_ThrowsArgumentNullException()
        {
            var descriptor = ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            var target = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, () => Enumerable.Empty<CompositionDependency>(), dependencies =>
            {
                return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            });

            AssertExtensions.Throws<ArgumentNullException>("contract", () => CompositionDependency.Satisfied(null, target, false , new object()));
        }

        [Fact]
        public void Satisfied_NullTarget_ThrowsArgumentNullException()
        {
            var contract = new CompositionContract(typeof(int));
            AssertExtensions.Throws<ArgumentNullException>("target", () => CompositionDependency.Satisfied(contract, null, false, new object()));
        }

        [Fact]
        public void Satisfied_NullSite_ThrowsArgumentNullException()
        {
            var contract = new CompositionContract(typeof(int));
            var descriptor = ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            var target = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, () => Enumerable.Empty<CompositionDependency>(), dependencies =>
            {
                return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            });

            AssertExtensions.Throws<ArgumentNullException>("site", () => CompositionDependency.Satisfied(contract, target, false, null));
        }

        public static IEnumerable<object[]> Oversupplied_TestData()
        {
            yield return new object[] { Enumerable.Empty<ExportDescriptorPromise>() };
            yield return new object[] { new ExportDescriptorPromise[] { null } };

            var descriptor = ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            var target = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", false, () => Enumerable.Empty<CompositionDependency>(), dependencies =>
            {
                return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            });
            yield return new object[] { new ExportDescriptorPromise[] { target } };
        }

        [Theory]
        [MemberData(nameof(Oversupplied_TestData))]
        public void Oversupplied_Invoke_ReturnsExpected(IEnumerable<ExportDescriptorPromise> targets)
        {
            var contract = new CompositionContract(typeof(int));
    
            CompositionDependency dependency = CompositionDependency.Oversupplied(contract, targets, "Site");
            Assert.Same(contract, dependency.Contract);
            Assert.Equal("Site", dependency.Site);
            Assert.Null(dependency.Target);
            Assert.False(dependency.IsPrerequisite);

            Assert.Equal("Site", dependency.ToString());
        }

        [Fact]
        public void Oversupplied_NullContract_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("contract", () => CompositionDependency.Oversupplied(null, Enumerable.Empty<ExportDescriptorPromise>(), new object()));
        }

        [Fact]
        public void Oversupplied_NullTargets_ThrowsArgumentNullException()
        {
            var contract = new CompositionContract(typeof(int));
            AssertExtensions.Throws<ArgumentNullException>("targets", () => CompositionDependency.Oversupplied(contract, null, new object()));
        }

        [Fact]
        public void Oversupplied_NullSite_ThrowsArgumentNullException()
        {
            var contract = new CompositionContract(typeof(int));
            AssertExtensions.Throws<ArgumentNullException>("site", () => CompositionDependency.Oversupplied(contract, Enumerable.Empty<ExportDescriptorPromise>(), null));
        }

        private static object Activator(LifetimeContext context, CompositionOperation operation) => null;
    }
}
