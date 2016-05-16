// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class DesignerSerializationVisibilityAttributeTests
    {
        [Fact]
        public void Equals_DifferentVisibilities()
        {
            Assert.False(DesignerSerializationVisibilityAttribute.Hidden.Equals(DesignerSerializationVisibilityAttribute.Visible));
        }

        [Fact]
        public void Equals_SameVisibility()
        {
            Assert.True(DesignerSerializationVisibilityAttribute.Visible.Equals(DesignerSerializationVisibilityAttribute.Visible));
        }

        [Theory]
        [InlineData(DesignerSerializationVisibility.Content)]
        [InlineData(DesignerSerializationVisibility.Hidden)]
        [InlineData(DesignerSerializationVisibility.Visible)]
        public static void Visibility(DesignerSerializationVisibility visibility)
        {
            var attribute = new DesignerSerializationVisibilityAttribute(visibility);

            Assert.Equal(visibility, attribute.Visibility);
        }
    }
}
