// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class PhoneAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(new PhoneAttribute(), null);
            yield return new TestCase(new PhoneAttribute(), "425-555-1212");
            yield return new TestCase(new PhoneAttribute(), "+1 425-555-1212");
            yield return new TestCase(new PhoneAttribute(), "(425)555-1212");
            yield return new TestCase(new PhoneAttribute(), "+44 (3456)987654");
            yield return new TestCase(new PhoneAttribute(), "+777.456.789.123");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 x123");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 x 123");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 ext123");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 ext 123");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 ext.123");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 ext. 123");
            yield return new TestCase(new PhoneAttribute(), "1");
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new PhoneAttribute(), new object());
            yield return new TestCase(new PhoneAttribute(), string.Empty);
            yield return new TestCase(new PhoneAttribute(), "abcdefghij");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 ext 123 ext 456");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 x");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 ext");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 ext.");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 x abc");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 ext def");
            yield return new TestCase(new PhoneAttribute(), "425-555-1212 ext. xyz");
            yield return new TestCase(new PhoneAttribute(), "-.()");
            yield return new TestCase(new PhoneAttribute(), "ext.123 1");

            // Certain invalid phone numbers are reported as valid with .NET core.
            // The full .NET framework considers them invalid. This is likely a bug
            // in .NET core. Seee https://github.com/dotnet/corefx/issues/17873.
            // [ActiveIssue(17873)]
            if (PlatformDetection.IsFullFramework)
            {
                yield return new TestCase(new PhoneAttribute(), "+4+2+5+-+5+5+5+-+1+2++1+2++");
                yield return new TestCase(new PhoneAttribute(), "425-555-1212    ");
                yield return new TestCase(new PhoneAttribute(), " \r \n 1  \t ");
                yield return new TestCase(new PhoneAttribute(), "1-.()");
                yield return new TestCase(new PhoneAttribute(), "(425555-1212");
                yield return new TestCase(new PhoneAttribute(), ")425555-1212");
            }
        }
        
        [Fact]
        public static void DataType_CustomDataType_ReturnsExpected()
        {
            var attribute = new PhoneAttribute();
            Assert.Equal(DataType.PhoneNumber, attribute.DataType);
            Assert.Null(attribute.CustomDataType);
        }
    }
}
