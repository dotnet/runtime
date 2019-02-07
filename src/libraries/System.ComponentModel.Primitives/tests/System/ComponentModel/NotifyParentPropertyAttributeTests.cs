// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class NotifyParentPropertyAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_NotifyParent(bool notifyParent)
        {
            var attribute = new NotifyParentPropertyAttribute(notifyParent);
            Assert.Equal(notifyParent, attribute.NotifyParent);
            Assert.Equal(!notifyParent, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { NotifyParentPropertyAttribute.Yes, NotifyParentPropertyAttribute.Yes, true };
            yield return new object[] { NotifyParentPropertyAttribute.No, new NotifyParentPropertyAttribute(false), true };
            yield return new object[] { NotifyParentPropertyAttribute.Yes, NotifyParentPropertyAttribute.No, false };

            yield return new object[] { NotifyParentPropertyAttribute.Yes, new object(), false };
            yield return new object[] { NotifyParentPropertyAttribute.Yes, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(NotifyParentPropertyAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is NotifyParentPropertyAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        private static IEnumerable<object[]> DefaultProperties_TestData()
        {
            yield return new object[] { NotifyParentPropertyAttribute.Yes, true };
            yield return new object[] { NotifyParentPropertyAttribute.Default, false };
            yield return new object[] { NotifyParentPropertyAttribute.No, false };
        }

        [Theory]
        [MemberData(nameof(DefaultProperties_TestData))]
        public void DefaultProperties_GetNotifyParent_ReturnsExpected(NotifyParentPropertyAttribute attribute, bool expectedNotifyParent)
        {
            Assert.Equal(expectedNotifyParent, attribute.NotifyParent);
            Assert.Equal(!expectedNotifyParent, attribute.IsDefaultAttribute());
        }
    }
}
