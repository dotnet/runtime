// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoReadOnly
    {
        public static IEnumerable<object[]> ReadOnly_TestData()
        {
            yield return new object[] { CultureInfo.InvariantCulture, true };
            yield return new object[] { new CultureInfo("en"), false };
            yield return new object[] { new CultureInfo("fr"), false };
            yield return new object[] { new CultureInfo("en-US"), false };
        }

        [Theory]
        [MemberData(nameof(ReadOnly_TestData))]
        public void ReadOnly(CultureInfo culture, bool expected)
        {
            Assert.Equal(expected, culture.IsReadOnly);

            CultureInfo readOnlyCulture = CultureInfo.ReadOnly(culture);
            Assert.True(readOnlyCulture.IsReadOnly);
        }

        [Fact]
        public void ReadOnly_ReadOnlyCulture_ReturnsSameReference()
        {
            CultureInfo readOnlyCulture = CultureInfo.ReadOnly(new CultureInfo("en-US"));
            Assert.Same(readOnlyCulture, CultureInfo.ReadOnly(readOnlyCulture));
        }

        [Fact]
        public void ReadOnly_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("ci", () => CultureInfo.ReadOnly(null));
        }
    }
}
