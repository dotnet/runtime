// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class LocalizableAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Localizable(bool localizable)
        {
            var attribute = new LocalizableAttribute(localizable);
            Assert.Equal(localizable, attribute.IsLocalizable);
            Assert.Equal(!localizable, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { LocalizableAttribute.Yes, LocalizableAttribute.Yes, true };
            yield return new object[] { LocalizableAttribute.No, new LocalizableAttribute(false), true };
            yield return new object[] { LocalizableAttribute.Yes, LocalizableAttribute.No, false };

            yield return new object[] { LocalizableAttribute.Yes, new object(), false };
            yield return new object[] { LocalizableAttribute.Yes, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(LocalizableAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is LocalizableAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        private static IEnumerable<object[]> DefaultProperties_TestData()
        {
            yield return new object[] { LocalizableAttribute.Yes, true };
            yield return new object[] { LocalizableAttribute.Default, false };
            yield return new object[] { LocalizableAttribute.No, false };
        }

        [Theory]
        [MemberData(nameof(DefaultProperties_TestData))]
        public void DefaultProperties_GetLocalizable_ReturnsExpected(LocalizableAttribute attribute, bool expectedLocalizable)
        {
            Assert.Equal(expectedLocalizable, attribute.IsLocalizable);
            Assert.Equal(!expectedLocalizable, attribute.IsDefaultAttribute());
        }
    }
}
