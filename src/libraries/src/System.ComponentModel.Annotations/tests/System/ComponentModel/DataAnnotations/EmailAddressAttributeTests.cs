// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class EmailAddressAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(new EmailAddressAttribute(), null);
            yield return new TestCase(new EmailAddressAttribute(), "someName@someDomain.com");
            yield return new TestCase(new EmailAddressAttribute(), "1234@someDomain.com");
            yield return new TestCase(new EmailAddressAttribute(), "firstName.lastName@someDomain.com");
            yield return new TestCase(new EmailAddressAttribute(), "\u00A0@someDomain.com");
            yield return new TestCase(new EmailAddressAttribute(), "!#$%&'*+-/=?^_`|~@someDomain.com");
            yield return new TestCase(new EmailAddressAttribute(), "\"firstName.lastName\"@someDomain.com");
            yield return new TestCase(new EmailAddressAttribute(), "someName@someDomain.com");
            yield return new TestCase(new EmailAddressAttribute(), "someName@some~domain.com");
            yield return new TestCase(new EmailAddressAttribute(), "someName@some_domain.com");
            yield return new TestCase(new EmailAddressAttribute(), "someName@1234.com");
            yield return new TestCase(new EmailAddressAttribute(), "someName@someDomain\uFFEF.com");
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new EmailAddressAttribute(), 0);
            yield return new TestCase(new EmailAddressAttribute(), "");
            yield return new TestCase(new EmailAddressAttribute(), " \r \t \n" );
            yield return new TestCase(new EmailAddressAttribute(), "@someDomain.com");
            yield return new TestCase(new EmailAddressAttribute(), "@someDomain@abc.com");
            yield return new TestCase(new EmailAddressAttribute(), "someName");
            yield return new TestCase(new EmailAddressAttribute(), "someName@");
            yield return new TestCase(new EmailAddressAttribute(), "someName@a@b.com");
        }

        [Fact]
        public static void DataType_CustomDataType_ReturnExpected()
        {
            var attribute = new EmailAddressAttribute();
            Assert.Equal(DataType.EmailAddress, attribute.DataType);
            Assert.Null(attribute.CustomDataType);
        }
    }
}
