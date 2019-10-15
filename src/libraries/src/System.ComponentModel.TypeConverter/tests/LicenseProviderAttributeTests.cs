// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class LicenseProviderAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new LicenseProviderAttribute();
            Assert.Null(attribute.LicenseProvider);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("typeName")]
        [InlineData("System.Int32")]
        public void Ctor_String(string typeName)
        {
            var attribute = new LicenseProviderAttribute(typeName);
            Assert.Equal(typeName == null ? null : Type.GetType(typeName), attribute.LicenseProvider);
            Assert.Same(attribute.LicenseProvider, attribute.LicenseProvider);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        [InlineData(typeof(DesignerAttribute))]
        public void Ctor_Type(Type type)
        {
            var attribute = new LicenseProviderAttribute(type);
            Assert.Same(type, attribute.LicenseProvider);
            Assert.Same(attribute.LicenseProvider, attribute.LicenseProvider);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            LicenseProviderAttribute attribute = LicenseProviderAttribute.Default;
            Assert.Same(attribute, LicenseProviderAttribute.Default);
            Assert.Null(attribute.LicenseProvider);
            Assert.False(attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> TypeId_TestData()
        {
            yield return new object[] { new LicenseProviderAttribute((string)null), "System.ComponentModel.LicenseProviderAttribute" };
            yield return new object[] { new LicenseProviderAttribute("typeName"), "System.ComponentModel.LicenseProviderAttributetypeName" };
            yield return new object[] { new LicenseProviderAttribute(typeof(int)), "System.ComponentModel.LicenseProviderAttributeSystem.Int32" };
        }

        [Theory]
        [MemberData(nameof(TypeId_TestData))]
        public void TypeId_ValidDesignerBaseTypeName_ReturnsExcepted(LicenseProviderAttribute attribute, object expected)
        {
            Assert.Equal(expected, attribute.TypeId);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new LicenseProviderAttribute(typeof(int));

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new LicenseProviderAttribute(typeof(int)), true };
            yield return new object[] { attribute, new LicenseProviderAttribute(new TypeDelegator(typeof(int))), true };
            yield return new object[] { attribute, new LicenseProviderAttribute(typeof(bool)), false };
            yield return new object[] { attribute, new LicenseProviderAttribute(new TypeDelegator(typeof(bool))), false };
            yield return new object[] { attribute, new LicenseProviderAttribute((Type)null), false };
            yield return new object[] { attribute, new LicenseProviderAttribute("typeName"), false };
            yield return new object[] { attribute, new LicenseProviderAttribute((string)null), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(LicenseProviderAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is LicenseProviderAttribute otherAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }
    }
}
