// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using Xunit;

namespace System.ComponentModel.Composition.Hosting
{
    public class CompositionElementTests
    {
        [Fact]
        public void Constructor_ValueAsUnderlyingObjectArgument_ShouldSetUnderlyingObjectProperty()
        {
            var expectations = Expectations.GetObjectsReferenceTypes();

            foreach (var e in expectations)
            {
                var element = new CompositionElement(e);

                Assert.Same(e, element.UnderlyingObject);
            }            
        }

        [Fact]
        public void Constructor_ValueAsUnderlyingObjectArgument_ShouldSetDisplayNamePropertyToUnderlyingObjectToString()
        {
            var expectations = Expectations.GetObjectsReferenceTypes();

            foreach (var e in expectations)
            {
                var element = new CompositionElement(e);

                Assert.Equal(e.ToString(), element.DisplayName);
            }
        }

        [Fact]
        public void Constructor_ValueAsUnderlyingObjectArgument_ShouldSetOriginToUnknown()
        {
            var expectations = Expectations.GetObjectsReferenceTypes();

            foreach (var e in expectations)
            {
                var element = new CompositionElement(e);

                Assert.NotNull(element.Origin);
                Assert.Null(element.Origin.Origin);
            }
        }

        [Fact]
        public void ToString_ShouldReturnDisplayNameProperty()
        {
            var expectations = Expectations.GetObjectsReferenceTypes();

            foreach (var e in expectations)
            {
                var element = CreateCompositionElement(e);

                Assert.Equal(element.DisplayName, element.ToString());
            }
        }

        private static CompositionElement CreateCompositionElement(object underlyingObject)
        {
            return new CompositionElement(underlyingObject);
        }
   }
}
