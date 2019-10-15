// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DataObjectMethodAttributeTests
    {
        [Theory]
        [InlineData(DataObjectMethodType.Delete)]
        [InlineData(DataObjectMethodType.Fill - 1)]
        [InlineData(DataObjectMethodType.Delete + 1)]
        public void Ctor_MethodType(DataObjectMethodType methodType)
        {
            var attribute = new DataObjectMethodAttribute(methodType);
            Assert.Equal(methodType, attribute.MethodType);
            Assert.False(attribute.IsDefault);
        }

        [Theory]
        [InlineData(DataObjectMethodType.Delete, true)]
        [InlineData(DataObjectMethodType.Fill - 1, false)]
        [InlineData(DataObjectMethodType.Delete + 1, false)]
        public void Ctor_MethodType_IsDefault(DataObjectMethodType methodType, bool isDefault)
        {
            var attribute = new DataObjectMethodAttribute(methodType, isDefault);
            Assert.Equal(methodType, attribute.MethodType);
            Assert.Equal(isDefault, attribute.IsDefault);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new DataObjectMethodAttribute(DataObjectMethodType.Fill, true);

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new DataObjectMethodAttribute(DataObjectMethodType.Fill, true), true };
            yield return new object[] { attribute, new DataObjectMethodAttribute(DataObjectMethodType.Delete, true), false };
            yield return new object[] { attribute, new DataObjectMethodAttribute(DataObjectMethodType.Fill, false), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(DataObjectMethodAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is DataObjectMethodAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> Match_TestData()
        {
            var attribute = new DataObjectMethodAttribute(DataObjectMethodType.Fill, true);

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new DataObjectMethodAttribute(DataObjectMethodType.Fill, true), true };
            yield return new object[] { attribute, new DataObjectMethodAttribute(DataObjectMethodType.Delete, true), false };
            yield return new object[] { attribute, new DataObjectMethodAttribute(DataObjectMethodType.Fill, false), true };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Match_TestData))]
        public void Match_Other_ReturnsExpected(DataObjectMethodAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Match(other));
        }
    }
}
