// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class NotifyParentPropertyAttributeTests
    {
        [Fact]
        public void Equals_DifferentValues()
        {
            Assert.False(NotifyParentPropertyAttribute.Yes.Equals(NotifyParentPropertyAttribute.No));
        }

        [Fact]
        public void Equals_SameValue()
        {
            Assert.True(NotifyParentPropertyAttribute.Yes.Equals(NotifyParentPropertyAttribute.Yes));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetNotifyParent(bool value)
        {
            var attribute = new NotifyParentPropertyAttribute(value);

            Assert.Equal(value, attribute.NotifyParent);
        }

        [Theory]
        [MemberData(nameof(NotifyParentPropertyAttributeData))]
        public void NameTests(NotifyParentPropertyAttribute attribute, bool isNotifyParent)
        {
            Assert.Equal(isNotifyParent, attribute.NotifyParent);
        }

        private static IEnumerable<object[]> NotifyParentPropertyAttributeData()
        {
            yield return new object[] { NotifyParentPropertyAttribute.Default, false };
            yield return new object[] { new NotifyParentPropertyAttribute(true), true };
            yield return new object[] { new NotifyParentPropertyAttribute(false), false };
        }
    }
}
