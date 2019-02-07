// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DesignerSerializationVisibilityAttributeTests
    {
        [Theory]
        [InlineData(DesignerSerializationVisibility.Hidden - 1, false)]
        [InlineData(DesignerSerializationVisibility.Content, false)]
        [InlineData(DesignerSerializationVisibility.Hidden, false)]
        [InlineData(DesignerSerializationVisibility.Visible, true)]
        public static void Ctor_Visibility(DesignerSerializationVisibility visibility, bool expectedIsDefaultAttribute)
        {
            var attribute = new DesignerSerializationVisibilityAttribute(visibility);
            Assert.Equal(visibility, attribute.Visibility);
            Assert.Equal(expectedIsDefaultAttribute, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { DesignerSerializationVisibilityAttribute.Visible, DesignerSerializationVisibilityAttribute.Visible, true };
            yield return new object[] { DesignerSerializationVisibilityAttribute.Visible, new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Visible), true };
            yield return new object[] { DesignerSerializationVisibilityAttribute.Visible, DesignerSerializationVisibilityAttribute.Hidden, false };

            yield return new object[] { DesignerSerializationVisibilityAttribute.Visible, new object(), false };
            yield return new object[] { DesignerSerializationVisibilityAttribute.Visible, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(DesignerSerializationVisibilityAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is DesignerSerializationVisibilityAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> DefaultProperties_TestData()
        {
            yield return new object[] { DesignerSerializationVisibilityAttribute.Content, DesignerSerializationVisibility.Content, false };
            yield return new object[] { DesignerSerializationVisibilityAttribute.Default, DesignerSerializationVisibility.Visible, true };
            yield return new object[] { DesignerSerializationVisibilityAttribute.Hidden, DesignerSerializationVisibility.Hidden, false };
            yield return new object[] { DesignerSerializationVisibilityAttribute.Visible, DesignerSerializationVisibility.Visible, true };
        }

        [Theory]
        [MemberData(nameof(DefaultProperties_TestData))]
        public void DefaultProperties_GetVisibility_ReturnsExpected(DesignerSerializationVisibilityAttribute attribute, DesignerSerializationVisibility expectedVisibility, bool expectedIsDefaultAttribute)
        {
            Assert.Equal(expectedVisibility, attribute.Visibility);
            Assert.Equal(expectedIsDefaultAttribute, attribute.IsDefaultAttribute());
        }
    }
}
