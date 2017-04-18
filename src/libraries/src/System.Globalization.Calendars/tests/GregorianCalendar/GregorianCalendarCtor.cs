// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarCtor
    {
        [Fact]
        public void Ctor_Empty()
        {
            GregorianCalendar calendar = new GregorianCalendar();
            Assert.Equal(GregorianCalendarTypes.Localized, calendar.CalendarType);
            Assert.False(calendar.IsReadOnly);
        }

        public static IEnumerable<object[]> GregorianCalendarTypes_TestData()
        {
            yield return new object[] { GregorianCalendarTypes.Arabic };
            yield return new object[] { GregorianCalendarTypes.Localized };
            yield return new object[] { GregorianCalendarTypes.MiddleEastFrench };
            yield return new object[] { GregorianCalendarTypes.TransliteratedEnglish };
            yield return new object[] { GregorianCalendarTypes.TransliteratedFrench };
            yield return new object[] { GregorianCalendarTypes.USEnglish };
        }

        [Theory]
        [MemberData(nameof(GregorianCalendarTypes_TestData))]
        public void Ctor_GregorianCalendarType(GregorianCalendarTypes type)
        {
            GregorianCalendar calendar = new GregorianCalendar(type);
            Assert.Equal(type, calendar.CalendarType);
            Assert.False(calendar.IsReadOnly);
        }

        [Theory]
        [InlineData(GregorianCalendarTypes.Localized - 1)]
        [InlineData(GregorianCalendarTypes.TransliteratedFrench + 1)]
        public void Ctor_GregorianCalendarTypes_InvalidType_ThrowsArgumentOutOfRangeException(GregorianCalendarTypes type)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("type", () => new GregorianCalendar(type));
        }

        [Theory]
        [MemberData(nameof(GregorianCalendarTypes_TestData))]
        public void CalendarType_Set(GregorianCalendarTypes type)
        {
            GregorianCalendar calendar = new GregorianCalendar();
            calendar.CalendarType = type;
            Assert.Equal(type, calendar.CalendarType);
        }

        [Theory]
        [InlineData(GregorianCalendarTypes.Localized - 1)]
        [InlineData(GregorianCalendarTypes.TransliteratedFrench + 1)]
        public void CalendarType_Set_InvalidType_ThrowsArgumentOutOfRangeException(GregorianCalendarTypes type)
        {
            GregorianCalendar calendar = new GregorianCalendar();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("m_type", () => calendar.CalendarType = type);
        }
    }
}
