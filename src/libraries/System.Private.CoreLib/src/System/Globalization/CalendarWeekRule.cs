// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    public enum CalendarWeekRule
    {
        FirstDay = 0,           // Week 1 begins on the first day of the year

        FirstFullWeek = 1,      // Week 1 begins on first FirstDayOfWeek not before the first day of the year

        FirstFourDayWeek = 2    // Week 1 begins on first FirstDayOfWeek such that FirstDayOfWeek+3 is not before the first day of the year
    }
}
