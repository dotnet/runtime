// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DataObjectAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new DataObjectAttribute();
            Assert.True(attribute.IsDataObject);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_IsDataObject(bool isDataObject)
        {
            var attribute = new DataObjectAttribute(isDataObject);
            Assert.Equal(isDataObject, attribute.IsDataObject);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new DataObjectAttribute(true);

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new DataObjectAttribute(true), true };
            yield return new object[] { attribute, new DataObjectAttribute(false), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(DataObjectAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetHashCode_Invoke_ReturnsExpected(bool isDataObject)
        {
            var attribute = new DataObjectAttribute(isDataObject);
            Assert.Equal(isDataObject.GetHashCode(), attribute.GetHashCode());
        }

        [Fact]
        public void DataObject_Get_ReturnsExpected()
        {
            DataObjectAttribute attribute = DataObjectAttribute.DataObject;
            Assert.Same(attribute, DataObjectAttribute.DataObject);
            Assert.True(attribute.IsDataObject);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void NonDataObject_Get_ReturnsExpected()
        {
            DataObjectAttribute attribute = DataObjectAttribute.NonDataObject;
            Assert.Same(attribute, DataObjectAttribute.NonDataObject);
            Assert.False(attribute.IsDataObject);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            DataObjectAttribute attribute = DataObjectAttribute.Default;
            Assert.Same(attribute, DataObjectAttribute.NonDataObject);
            Assert.False(attribute.IsDataObject);
            Assert.True(attribute.IsDefaultAttribute());
        }
    }
}
