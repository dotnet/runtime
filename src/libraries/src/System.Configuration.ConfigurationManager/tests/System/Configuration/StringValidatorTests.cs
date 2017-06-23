// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using Xunit;

namespace System.ConfigurationTests
{
    public class StringValidatorTests
    {
        [Fact]
        public void CanValidate_PassInStringType()
        {
            StringValidator validator = new StringValidator(5);
            Assert.True(validator.CanValidate(typeof(string)));
        }

        [Fact]
        public void CanValidate_PassInNotStringType()
        {
            StringValidator validator = new StringValidator(5);
            Assert.False(validator.CanValidate(typeof(Int32)));
        }

        [Fact]
        public void Validate_PassInNonString()
        {
            StringValidator validator = new StringValidator(5);
            AssertExtensions.Throws<ArgumentException>("", () => validator.Validate(5));
        }

        [Fact]
        public void Validate_StringTooSmall()
        {
            StringValidator validator = new StringValidator(5);
            AssertExtensions.Throws<ArgumentException>(null, () => validator.Validate("Hi"));
        }

        [Fact]
        public void Validate_StringTooBig()
        {
            StringValidator validator = new StringValidator(5, 10);
            ArgumentException thrownException = AssertExtensions.Throws<ArgumentException>(null, () => validator.Validate("This is more than ten"));
        }

        [Fact]
        public void Validate_EmptyString()
        {
            // this should complete with no errors.
            StringValidator validator = new StringValidator(0);
            validator.Validate(string.Empty);
        }

        [Theory]
        [MemberData(nameof(GetInvalidStrings))]
        public void Validate_UsinginvalidCharacters(string stringToValidate)
        {
            StringValidator validator = new StringValidator(1, 20, "_-");
            ArgumentException result = AssertExtensions.Throws<ArgumentException>(null, () => validator.Validate(stringToValidate));
        }

        [Fact]
        public void Validate_NoInvalidCharactersSpecified()
        {
            StringValidator validator = new StringValidator(5);
            validator.Validate("Hello");
        }


        public static IEnumerable<object[]> GetInvalidStrings()
        {
            yield return new object[] { "Hello-" };
            yield return new object[] { "Hello_" };
            yield return new object[] { "-Hello" };
            yield return new object[] { "_Hello" };
            yield return new object[] { "He-llo" };
            yield return new object[] { "He_llo" };
            yield return new object[] { "H-e-l-l-o" };
            yield return new object[] { "H_e_l_l_o_" };
            yield return new object[] { "H_e-l_l-o_" };
            yield return new object[] { "He__llo" };
            yield return new object[] { "He--llo" };
            yield return new object[] { "H_-el-l-o" };
            yield return new object[] { "_" };
            yield return new object[] { "-" };
        }
    }
}
