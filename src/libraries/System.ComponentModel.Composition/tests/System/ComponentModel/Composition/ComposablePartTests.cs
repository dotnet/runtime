// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ComposablePartTests
    {
        [Fact]
        public void Constructor1_ShouldSetMetadataPropertyToEmptyDictionary()
        {
            var part = PartFactory.Create();

            Assert.Empty(part.Metadata);
        }

        [Fact]
        public void Constructor1_ShouldSetMetadataPropertyToReadOnlyDictionary()
        {
            var part = PartFactory.Create();

            Assert.Throws<NotSupportedException>(() =>
            {
                part.Metadata["Value"] = "Value";
            });
        }

        [Fact]
        public void OnComposed_DoesNotThrow()
        {
            var part = PartFactory.Create();
            part.Activate();
        }

    }
}
