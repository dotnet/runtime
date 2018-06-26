// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DataObjectFieldAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_PrimaryKey(bool primaryKey)
        {
            var attribute = new DataObjectFieldAttribute(primaryKey);
            Assert.Equal(primaryKey, attribute.PrimaryKey);
            Assert.False(attribute.IsIdentity);
            Assert.False(attribute.IsNullable);
            Assert.Equal(-1, attribute.Length);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void Ctor_PrimaryKey_IsIdentity(bool primaryKey, bool isIdentity)
        {
            var attribute = new DataObjectFieldAttribute(primaryKey, isIdentity);
            Assert.Equal(primaryKey, attribute.PrimaryKey);
            Assert.Equal(isIdentity, attribute.IsIdentity);
            Assert.False(attribute.IsNullable);
            Assert.Equal(-1, attribute.Length);
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(false, false, false)]
        public void Ctor_PrimaryKey_IsIdentity_IsNullable(bool primaryKey, bool isIdentity, bool isNullable)
        {
            var attribute = new DataObjectFieldAttribute(primaryKey, isIdentity, isNullable);
            Assert.Equal(primaryKey, attribute.PrimaryKey);
            Assert.Equal(isIdentity, attribute.IsIdentity);
            Assert.Equal(isIdentity, attribute.IsNullable);
            Assert.Equal(-1, attribute.Length);
        }

        [Theory]
        [InlineData(true, true, true, 10)]
        [InlineData(false, false, false, -1)]
        public void Ctor_PrimaryKey_IsIdentity_IsNullable_Length(bool primaryKey, bool isIdentity, bool isNullable, int length)
        {
            var attribute = new DataObjectFieldAttribute(primaryKey, isIdentity, isNullable, length);
            Assert.Equal(primaryKey, attribute.PrimaryKey);
            Assert.Equal(isIdentity, attribute.IsIdentity);
            Assert.Equal(isIdentity, attribute.IsNullable);
            Assert.Equal(length, attribute.Length);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new DataObjectFieldAttribute(true, true, true, 10);

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new DataObjectFieldAttribute(true, true, true, 10), true };
            yield return new object[] { attribute, new DataObjectFieldAttribute(false, true, true, 10), false };
            yield return new object[] { attribute, new DataObjectFieldAttribute(true, false, true, 10), false };
            yield return new object[] { attribute, new DataObjectFieldAttribute(true, true, false, 10), false };
            yield return new object[] { attribute, new DataObjectFieldAttribute(true, true, true, 9), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(DataObjectFieldAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }
        
        [Fact]
        public void GetHashCode_InvokeMultipleTimes_ReturnsSame()
        {
            var attribute = new DataObjectFieldAttribute(false);
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }
    }
}
