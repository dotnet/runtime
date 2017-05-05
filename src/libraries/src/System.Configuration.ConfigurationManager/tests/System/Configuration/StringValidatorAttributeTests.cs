// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class StringValidatorAttributeTests
    {
        [Fact]
        public void MinLengthNotSpecified_Get()
        {
            StringValidatorAttribute attribute = new StringValidatorAttribute();
            Assert.Equal(0, attribute.MinLength);
        }

        [Fact]
        public void MinLength_GetAfterSet()
        {
            StringValidatorAttribute attribute = new StringValidatorAttribute();
            attribute.MinLength = 5;
            Assert.Equal(5, attribute.MinLength);
        }

        [Fact]
        public void MaxLengthNotSpecified_Get()
        {
            StringValidatorAttribute attribute = new StringValidatorAttribute();
            Assert.Equal(int.MaxValue, attribute.MaxLength);
        }

        [Fact]
        public void MaxLength_GetAfterSet()
        {
            StringValidatorAttribute attribute = new StringValidatorAttribute();
            attribute.MaxLength = 10;
            Assert.Equal(10, attribute.MaxLength);
        }

        [Fact]
        public void MinLength_SmallerThanMaxLength()
        {
            StringValidatorAttribute attribute = new StringValidatorAttribute();
            attribute.MaxLength = 10;
            Assert.Throws<ArgumentOutOfRangeException>(() => attribute.MinLength = 11);
        }

        [Fact]
        public void MaxLength_BiggerThanMinLength()
        {
            StringValidatorAttribute attribute = new StringValidatorAttribute();
            attribute.MinLength = 5;
            Assert.Throws<ArgumentOutOfRangeException>(() => attribute.MaxLength = 4);
        }

        [Fact]
        public void InvalidCharacters_GetAndSet()
        {
            StringValidatorAttribute attribute = new StringValidatorAttribute();
            attribute.InvalidCharacters = "_-";
            Assert.Equal("_-", attribute.InvalidCharacters);
        }
    }
}