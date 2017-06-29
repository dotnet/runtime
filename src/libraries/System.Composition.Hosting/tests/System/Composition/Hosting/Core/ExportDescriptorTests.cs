// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Composition.Hosting.Core.Tests
{
    public class ExportDescriptorTests
    {
        public static IEnumerable<object[]> Create_TestData()
        {
            yield return new object[] { new CompositeActivator(Activator), new Dictionary<string, object>() };
            yield return new object[] { new CompositeActivator(Activator), new Dictionary<string, object> { { "key", "value" } } };
        }

        [Theory]
        [MemberData(nameof(Create_TestData))]
        public void Create_Valid_ReturnsExpected(CompositeActivator activator, Dictionary<string, object> metadata)
        {
            ExportDescriptor descriptor = ExportDescriptor.Create(activator, metadata);
            Assert.Same(activator, descriptor.Activator);
            Assert.Same(metadata, descriptor.Metadata);
        }

        [Fact]
        public void Create_NullActivator_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("activator", () => ExportDescriptor.Create(null, new Dictionary<string, object>()));
        }

        [Fact]
        public void Create_NullMetadata_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("metadata", () => ExportDescriptor.Create(Activator, null));
        }

        private static object Activator(LifetimeContext context, CompositionOperation operation) => null;
    }
}
