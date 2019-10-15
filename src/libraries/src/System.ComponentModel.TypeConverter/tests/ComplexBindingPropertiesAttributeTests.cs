// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ComplexBindingPropertiesAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new ComplexBindingPropertiesAttribute();
            Assert.Null(attribute.DataSource);
            Assert.Null(attribute.DataMember);
        }

        [Theory]
        [InlineData("DataSource")]
        [InlineData(null)]
        public void Ctor_DataSource(string dataSource)
        {
            var attribute = new ComplexBindingPropertiesAttribute(dataSource);
            Assert.Same(dataSource, attribute.DataSource);
            Assert.Null(attribute.DataMember);
        }

        [Theory]
        [InlineData("DataSource", "DataMember")]
        [InlineData(null, null)]
        public void Ctor_DataSource_DataMember(string dataSource, string dataMember)
        {
            var attribute = new ComplexBindingPropertiesAttribute(dataSource, dataMember);
            Assert.Same(dataSource, attribute.DataSource);
            Assert.Same(dataMember, attribute.DataMember);
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            ComplexBindingPropertiesAttribute attribute = ComplexBindingPropertiesAttribute.Default;
            Assert.Same(attribute, ComplexBindingPropertiesAttribute.Default);

            Assert.Null(attribute.DataSource);
            Assert.Null(attribute.DataMember);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new ComplexBindingPropertiesAttribute("dataSource", "dataMember");
            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new ComplexBindingPropertiesAttribute("dataSource", "dataMember"), true };

            yield return new object[] { attribute, new ComplexBindingPropertiesAttribute("other", "dataMember"), false };
            yield return new object[] { attribute, new ComplexBindingPropertiesAttribute(null, "dataMember"), false };

            yield return new object[] { attribute, new ComplexBindingPropertiesAttribute("dataSource", "other"), false };
            yield return new object[] { attribute, new ComplexBindingPropertiesAttribute("dataSource", null), false };

            yield return new object[] { ComplexBindingPropertiesAttribute.Default, new ComplexBindingPropertiesAttribute(), true };

            yield return new object[] { new ComplexBindingPropertiesAttribute(), new object(), false };
            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(ComplexBindingPropertiesAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsConsistentValue()
        {
            var attribute = new ComplexBindingPropertiesAttribute();
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }
    }
}
