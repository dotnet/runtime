// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoClone
    {
        public static IEnumerable<object[]> Clone_TestData()
        {
            yield return new object[] { new CultureInfo(CultureInfo.InvariantCulture.Name) };
            yield return new object[] { CultureInfo.InvariantCulture };
            yield return new object[] { new CultureInfo("fr-FR") };
            yield return new object[] { new CultureInfo("en") };
        }

        [Theory]
        [MemberData(nameof(Clone_TestData))]
        public void Clone(CultureInfo culture)
        {
            CultureInfo clone = (CultureInfo)culture.Clone();
            Assert.Equal(culture, clone);
            Assert.NotSame(clone, culture);
        }

        [Fact]
        public void TestCalendarAfterCloning()
        {
            CultureInfo ci = new CultureInfo("en-US");
            Assert.Same(ci.Calendar, ci.DateTimeFormat.Calendar);
            CultureInfo ci1 = (CultureInfo) ci.Clone();
            Assert.Same(ci1.Calendar, ci1.DateTimeFormat.Calendar);
            Assert.NotSame(ci.Calendar, ci1.Calendar);
            Assert.NotSame(((CultureInfo)(ci.Clone())).Calendar, ((CultureInfo)(ci.Clone())).Calendar);
        }
    }
}
