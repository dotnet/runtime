// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Globalization
{
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum CalendarWeekRule
    {
        FirstDay = 0,           // Week 1 begins on the first day of the year

        FirstFullWeek = 1,      // Week 1 begins on first FirstDayOfWeek not before the first day of the year

        FirstFourDayWeek = 2    // Week 1 begins on first FirstDayOfWeek such that FirstDayOfWeek+3 is not before the first day of the year        
    };
}
