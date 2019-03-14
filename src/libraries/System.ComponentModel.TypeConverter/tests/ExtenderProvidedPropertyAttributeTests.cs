// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ExtenderProvidedPropertyAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new ExtenderProvidedPropertyAttribute();
            Assert.Null(attribute.ExtenderProperty);
            Assert.Null(attribute.Provider);
            Assert.Null(attribute.ReceiverType);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new ExtenderProvidedPropertyAttribute();

            yield return new object[] { attribute, attribute, true };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(ExtenderProvidedPropertyAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is ExtenderProvidedPropertyAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        [Fact]
        public void Equals_NullProperties_ThrowsNullReferenceException()
        {
            var attribute = new ExtenderProvidedPropertyAttribute();
            if (!PlatformDetection.IsFullFramework)
            {
                Assert.True(attribute.Equals(new ExtenderProvidedPropertyAttribute()));
            }
            else
            {
                Assert.Throws<NullReferenceException>(() => attribute.Equals(new ExtenderProvidedPropertyAttribute()));
            }
        }
    }
}
