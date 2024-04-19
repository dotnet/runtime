// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarTests
    {
        [Theory]
        [InlineData(GregorianCalendarTypes.Localized - 1)]
        [InlineData(GregorianCalendarTypes.TransliteratedFrench + 1)]
        public void Ctor_InvalidType_ThrowsArgumentOutOfRangeException(GregorianCalendarTypes type)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("type", () => new GregorianCalendar(type));
        }
    }

    public abstract class GregorianCalendarTestBase : CalendarTestBase
    {
        public abstract GregorianCalendarTypes CalendarType { get; }

        public override Calendar Calendar => new GregorianCalendar(CalendarType);

        [Fact]
        public void CalendarType_Get_ReturnsExpected()
        {
            GregorianCalendar calendar = ((GregorianCalendar)Calendar);
            Assert.Equal(CalendarType, calendar.CalendarType);
        }

        [Theory]
        [InlineData(GregorianCalendarTypes.Arabic)]
        [InlineData(GregorianCalendarTypes.Localized)]
        [InlineData(GregorianCalendarTypes.MiddleEastFrench)]
        [InlineData(GregorianCalendarTypes.TransliteratedEnglish)]
        [InlineData(GregorianCalendarTypes.TransliteratedFrench)]
        [InlineData(GregorianCalendarTypes.USEnglish)]
        public void CalendarType_Set(GregorianCalendarTypes type)
        {
            GregorianCalendar calendar = ((GregorianCalendar)Calendar);
            calendar.CalendarType = type;
            Assert.Equal(type, calendar.CalendarType);
        }

        [Theory]
        [InlineData(GregorianCalendarTypes.Localized - 1)]
        [InlineData(GregorianCalendarTypes.TransliteratedFrench + 1)]
        public void CalendarType_SetInvalidValue_ThrowsArgumentOutOfRangeException(GregorianCalendarTypes type)
        {
            GregorianCalendar calendar = ((GregorianCalendar)Calendar);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", "m_type", () => calendar.CalendarType = type);
        }
    }

    public class GregorianCalendarDefaultTests : GregorianCalendarTestBase
    {
        public override Calendar Calendar => new GregorianCalendar();

        public override GregorianCalendarTypes CalendarType => GregorianCalendarTypes.Localized;
    }

    public class GregorianCalendarArabicTests : GregorianCalendarTestBase
    {
        public override GregorianCalendarTypes CalendarType => GregorianCalendarTypes.Arabic;
    }

    public class GregorianCalendarLocalizedTests : GregorianCalendarTestBase
    {
        public override GregorianCalendarTypes CalendarType => GregorianCalendarTypes.Localized;
    }

    public class GregorianCalendarMiddleEastFrenchTests : GregorianCalendarTestBase
    {
        public override GregorianCalendarTypes CalendarType => GregorianCalendarTypes.MiddleEastFrench;
    }

    public class GregorianCalendarTransliteratedEnglishTests : GregorianCalendarTestBase
    {
        public override GregorianCalendarTypes CalendarType => GregorianCalendarTypes.TransliteratedEnglish;
    }

    public class GregorianCalendarTransliteratedFrenchTests : GregorianCalendarTestBase
    {
        public override GregorianCalendarTypes CalendarType => GregorianCalendarTypes.TransliteratedFrench;
    }

    public class GregorianCalendarUSEnglishTests : GregorianCalendarTestBase
    {
        public override GregorianCalendarTypes CalendarType => GregorianCalendarTypes.USEnglish;
    }
}
