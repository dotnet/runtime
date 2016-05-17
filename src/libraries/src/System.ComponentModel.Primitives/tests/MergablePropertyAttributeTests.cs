// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class MergablePropertyAttributeTests
    {
        [Fact]
        public void Equals_DifferentValues()
        {
            Assert.False(MergablePropertyAttribute.Yes.Equals(MergablePropertyAttribute.No));
        }

        [Fact]
        public void Equals_SameValue()
        {
            Assert.True(MergablePropertyAttribute.Yes.Equals(MergablePropertyAttribute.Yes));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetAllowMerge(bool value)
        {
            var attribute = new MergablePropertyAttribute(value);

            Assert.Equal(value, attribute.AllowMerge);
        }

        [Theory]
        [MemberData(nameof(MergablePropertyAttributeData))]
        public void NameTests(MergablePropertyAttribute attribute, bool isMergable)
        {
            Assert.Equal(isMergable, attribute.AllowMerge);
        }

        private static IEnumerable<object[]> MergablePropertyAttributeData()
        {
            yield return new object[] { MergablePropertyAttribute.Default, true };
            yield return new object[] { new MergablePropertyAttribute(true), true };
            yield return new object[] { new MergablePropertyAttribute(false), false };
        }
    }
}
