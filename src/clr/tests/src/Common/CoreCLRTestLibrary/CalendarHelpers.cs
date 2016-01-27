// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Globalization;

public static class CalendarGet
{
    public static Calendar GregorianCalendar(GregorianCalendarTypes Type)
    {
        CultureInfo ci = null;

        switch (Type)
        {
            case GregorianCalendarTypes.Arabic:
                ci = new CultureInfo("ar-SA");
                return ci.OptionalCalendars[4];
            case GregorianCalendarTypes.Localized:
                ci = new CultureInfo("ar-IQ");
                return ci.OptionalCalendars[0];
            case GregorianCalendarTypes.MiddleEastFrench:
                ci = new CultureInfo("ar-IQ");
                return ci.OptionalCalendars[4];
            case GregorianCalendarTypes.TransliteratedEnglish:
                ci = new CultureInfo("ar-IQ");
                return ci.OptionalCalendars[5];
            case GregorianCalendarTypes.TransliteratedFrench:
                ci = new CultureInfo("ar-IQ");
                return ci.OptionalCalendars[6];
            case GregorianCalendarTypes.USEnglish:
                ci = new CultureInfo("ar-IQ");
                return ci.OptionalCalendars[3];
            default:
                throw new NotImplementedException();
        }
    }

    public static Calendar JapaneseCalendar()
    {
        var ci = new CultureInfo("ja-JP");
        return ci.OptionalCalendars[1];
    }

    public static Calendar ThaiBuddhistCalendar()
    {
        var ci = new CultureInfo("th-TH");
        return ci.Calendar;
    }

    public static Calendar KoreanCalendar()
    {
        var ci = new System.Globalization.CultureInfo("ko-KR");
        return ci.OptionalCalendars[1];
    }

    public static Calendar GregorianCalendar()
    {
        var ci = new System.Globalization.CultureInfo("en-US");
        return ci.Calendar;
    }

    public static Calendar TaiwanCalendar()
    {
        var ci = new System.Globalization.CultureInfo("zh-TW");
        return ci.OptionalCalendars[1];
    }

    public static Calendar HebrewCalendar()
    {
        var ci = new System.Globalization.CultureInfo("he-IL");
        return ci.OptionalCalendars[1];
    }

    public static Calendar HijriCalendar()
    {
        var ci = new System.Globalization.CultureInfo("ar-SA");
        return ci.OptionalCalendars[1];
    }
}

public enum GregorianCalendarTypes
{
    Arabic,
    USEnglish,
    Localized,
    MiddleEastFrench,
    TransliteratedEnglish,
    TransliteratedFrench,
}
