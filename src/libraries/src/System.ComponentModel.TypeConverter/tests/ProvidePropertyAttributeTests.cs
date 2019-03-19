// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel
{
    public class ProvidePropertyAttributeTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("propertyName", "invalidName")]
        [InlineData("propertyName", "System.Int32")]
        public void Ctor_String_String(string propertyName, string receiverTypeName)
        {
            var attribute = new ProvidePropertyAttribute(propertyName, receiverTypeName);
            Assert.Equal(propertyName, attribute.PropertyName);
            Assert.Equal(receiverTypeName, attribute.ReceiverTypeName);
        }

        [Theory]
        [InlineData(null, typeof(int))]
        [InlineData("", typeof(ProvidePropertyAttribute))]
        [InlineData("propertyName", typeof(ProvidePropertyAttributeTests))]
        public void Ctor_String_Type(string propertyName, Type receiverType)
        {
            var attribute = new ProvidePropertyAttribute(propertyName, receiverType);
            Assert.Equal(propertyName, attribute.PropertyName);
            Assert.Equal(receiverType.AssemblyQualifiedName, attribute.ReceiverTypeName);
        }

        [Fact]
        public void Ctor_NullReceiverType_ThrowsNullReferenceException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("receiverType", () => new ProvidePropertyAttribute("propertyName", (Type)null));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new ProvidePropertyAttribute("propertyName", "receiverTypeName");

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new ProvidePropertyAttribute("propertyName", "receiverTypeName"), true };
            yield return new object[] { attribute, new ProvidePropertyAttribute("propertyname", "receiverTypeName"), false };
            yield return new object[] { attribute, new ProvidePropertyAttribute(null, "receiverTypeName"), false };
            yield return new object[] { attribute, new ProvidePropertyAttribute("propertyName", "receivertypename"), false };
            yield return new object[] { attribute, new ProvidePropertyAttribute("propertyName", (string)null), false };

            yield return new object[] { new ProvidePropertyAttribute(null, (string)null), new ProvidePropertyAttribute(null, (string)null), true };
            yield return new object[] { new ProvidePropertyAttribute(null, (string)null), new ProvidePropertyAttribute("propertyName", (string)null), false };
            yield return new object[] { new ProvidePropertyAttribute(null, (string)null), new ProvidePropertyAttribute(null, "receiverTypeName"), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(ProvidePropertyAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_InvokeMultipleTimes_ReturnsEqual()
        {
            var attribute = new ProvidePropertyAttribute("propertyName", "receiverTypeName");
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }

        [Fact]
        public void GetHashCode_NullPropertyName_ReturnsEqual()
        {
            var attribute = new ProvidePropertyAttribute(null, "receiverTypeName");
            if (!PlatformDetection.IsFullFramework)
            {
                Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
            }
            else
            {
                Assert.Throws<NullReferenceException>(() => attribute.GetHashCode());
            }
        }

        [Fact]
        public void GetHashCode_NullReceiverTypeName_ReturnsEqual()
        {
            var attribute = new ProvidePropertyAttribute("propertyName", (string)null);

            if (!PlatformDetection.IsFullFramework)
            {
                Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
            }
            else
            {
                Assert.Throws<NullReferenceException>(() => attribute.GetHashCode());
            }
        }
    }
}
