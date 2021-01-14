// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Tests;
using Xunit;

namespace System.Xml.Linq.Tests
{
    public class XAttributeTests
    {
        [Fact]
        public void FormattedDate()
        {
            // Ensure we are compatible with the .NET Framework
            Assert.Equal("CreatedTime=\"2018-01-01T12:13:14Z\"", new XAttribute("CreatedTime", new DateTime(2018, 1, 1, 12, 13, 14, DateTimeKind.Utc)).ToString());
        }

        public static IEnumerable<object[]> NumericValuesWithMinusSign()
        {
            yield return new object[] { -123 };
            yield return new object[] { -123f };
            yield return new object[] { -123L };
            yield return new object[] { (short)-123 };
            yield return new object[] { -12.3 };
            yield return new object[] { -12.3m };
            yield return new object[] { (sbyte)-123 };
        }

        [Theory]
        [MemberData(nameof(NumericValuesWithMinusSign))]
        public void MinusSignWithDifferentTypeSwedishCulture(object value)
        {
            CultureInfo newCulture = null;
            try
            {
                newCulture = new CultureInfo("sv-SE");
            }
            catch (CultureNotFoundException) { /* Do nothing */ }

            using (new ThreadCultureChange(newCulture))
            {
                Assert.Equal('-', (new XAttribute("a", value)).Value[0]);
            }
        }

        [Theory]
        [MemberData(nameof(NumericValuesWithMinusSign))]
        public void MinusSignWithDifferentTypeNoCulture(object value)
        {
            Assert.Equal('-', (new XAttribute("a", value)).Value[0]);
        }

        public static IEnumerable<object[]> NonNumericValues()
        {
            yield return new object[] { true, "true" };
            yield return new object[] { new DateTimeOffset(2018, 1, 1, 12, 13, 14, TimeSpan.Zero), "2018-01-01T12:13:14Z" };
            yield return new object[] { new TimeSpan(12, 13, 14), "PT12H13M14S" };
            yield return new object[] { "-123\n", "-123\n" };
        }

        [Theory]
        [MemberData(nameof(NonNumericValues))]
        public void NonNumericTypeSwedishCulture(object value, string expected)
        {
            CultureInfo newCulture = null;
            try
            {
                newCulture = new CultureInfo("sv-SE");
            }
            catch (CultureNotFoundException) { /* Do nothing */ }

            using (new ThreadCultureChange(newCulture))
            {
                Assert.Equal(expected, (new XAttribute("a", value)).Value);
            }
        }

        [Theory]
        [MemberData(nameof(NonNumericValues))]
        public void NonNumericTypesNoCulture(object value, string expected)
        {
            Assert.Equal(expected, (new XAttribute("a", value)).Value);
        }
    }
}
