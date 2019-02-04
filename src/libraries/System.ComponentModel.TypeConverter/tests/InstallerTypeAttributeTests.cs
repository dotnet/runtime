// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class InstallerTypeAttributeTests
    {
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(InstallerTypeAttribute))]
        public void Ctor_Type(Type installerType)
        {
            var attribute = new InstallerTypeAttribute(installerType);
            Assert.Equal(installerType, attribute.InstallerType);
        }

        [Fact]
        public void Ctor_NullInstallerType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("installerType", () => new InstallerTypeAttribute((Type)null));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(InstallerTypeAttribute))]
        public void Ctor_String(Type installerType)
        {
            var attribute = new InstallerTypeAttribute(installerType.AssemblyQualifiedName);
            Assert.Equal(installerType, attribute.InstallerType);
        }

        [Fact]
        public void Ctor_NullTypeName_InstallerTypeThrowsArgumentNullException()
        {
            var attribute = new InstallerTypeAttribute((string)null);
            AssertExtensions.Throws<ArgumentNullException>("typeName", () => attribute.InstallerType);
        }

        [Theory]
        [InlineData("")]
        [InlineData("NoSuchType")]
        public void Ctor_InvalidTypeName_InstallerTypeReturnsNull(string typeName)
        {
            var attribute = new InstallerTypeAttribute(typeName);
            Assert.Null(attribute.InstallerType);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new InstallerTypeAttribute("typeName");

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new InstallerTypeAttribute("typeName"), true };
            yield return new object[] { attribute, new InstallerTypeAttribute("typename"), false };
            yield return new object[] { attribute, new InstallerTypeAttribute((string)null), false };


            yield return new object[] { new InstallerTypeAttribute((string)null), new InstallerTypeAttribute((string)null), true };
            yield return new object[] { new InstallerTypeAttribute((string)null), new InstallerTypeAttribute("typeName"), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(InstallerTypeAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is InstallerTypeAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }
    }
}
