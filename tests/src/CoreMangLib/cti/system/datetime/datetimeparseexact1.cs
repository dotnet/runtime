// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

public class DateTimeParseExact1
{
    private const  int c_MIN_STRING_LEN = 1;
    private const  int c_MAX_STRING_LEN = 2048;
    private const  int c_NUM_LOOPS      = 100;
    private static CultureInfo CurrentCulture = TestLibrary.Utilities.CurrentCulture;
    private static CultureInfo EnglishCulture = new CultureInfo("en-US");
    private static string[] c_MONTHS = EnglishCulture.DateTimeFormat.MonthGenitiveNames;
    private static string[] c_MONTHS_SH = EnglishCulture.DateTimeFormat.AbbreviatedMonthGenitiveNames;
    private static string[] c_DAYS_SH = EnglishCulture.DateTimeFormat.AbbreviatedDayNames;

    public static int Main()
    {
        DateTimeParseExact1 test = new DateTimeParseExact1();

        TestLibrary.TestFramework.BeginTestCase("DateTimeParseExact1");

        if (test.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest15() && retVal;

        TestLibrary.Utilities.CurrentCulture = EnglishCulture;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest12() && retVal;
        retVal = PosTest13() && retVal;
        retVal = PosTest14() && retVal;
        TestLibrary.Utilities.CurrentCulture = CurrentCulture;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        string   dateBefore = "";
        DateTime dateAfter;

        TestLibrary.TestFramework.BeginScenario("PosTest1: DateTime.ParseExact(G, DateTime.Now)");

        try
        {
            dateBefore = DateTime.Now.ToString();

            dateAfter = DateTime.ParseExact( dateBefore, "G", formater );

            if (!dateBefore.Equals(dateAfter.ToString()))
            {
                TestLibrary.TestFramework.LogError("001", "DateTime.ParseExact(" + dateBefore + ") did not equal " + dateAfter.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        DateTime dateAfter;
        string   dateBefore = "";
        int      day;    // 1 - 29
        int      year;   // 1900 - 2000
        int      month;  // 1 - 12

        TestLibrary.TestFramework.BeginScenario("PosTest2: DateTime.ParseExact(d, M/d/yyyy (ShortDatePattern ex: 1/3/2002))");

        // Skipping test because format 'd' on some platforms represents the year using two digits, 
        // this cause extrange results. Some dates are shifted 1 hour backward. See DDB 173277 - MAC bug
        // Culture could be customized and dateseparator may be different than / or MM is used in the format
        if (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.ShortDatePattern, "M/d/yyyy", StringComparison.Ordinal) != 0)
        {
            TestLibrary.TestFramework.LogInformation("Skipping test case, ShortDatePattern: " + TestLibrary.Utilities.CurrentCulture.DateTimeFormat.ShortDatePattern);
            return retVal;
        }

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day    = (TestLibrary.Generator.GetInt32(-55) % 28)  + 1;
                year   = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month  = (TestLibrary.Generator.GetInt32(-55) % 12)  + 1;

                dateBefore = month + "/" + day + "/" + year;
                dateAfter  = DateTime.ParseExact( dateBefore, "d", formater);

                if (month != dateAfter.Month || day != dateAfter.Day || year != dateAfter.Year)
                {
                    TestLibrary.TestFramework.LogError("003", "DateTime.ParseExact(" + dateBefore + ") did not equal " + dateAfter.ToString() + ".  Got M="+dateAfter.Month+", d="+dateAfter.Day+", y="+dateAfter.Year);
                    retVal = false;
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        DateTime dateAfter;
        string   dateBefore = "";
        int      dayOfWeek; // 0 - 6
        int      day;       // 1 - 28
        int      year;      // 1900 - 2000
        int      month;     // 0 - 11

        TestLibrary.TestFramework.BeginScenario("PosTest3: DateTime.ParseExact(D, dddd, MMMM dd, yyyy (LongDatePattern ex: Thursday, January 03, 2002))");
        // Culture could be customized
        if (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongDatePattern, "dddd, MMMM dd, yyyy", StringComparison.Ordinal) != 0 &&
            String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongDatePattern, "dddd, MMMM d, yyyy", StringComparison.Ordinal) != 0)
        {
            TestLibrary.TestFramework.LogInformation("Skipping test case, LongDatePattern: " + TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongDatePattern);
            return retVal;
        }

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12);

		        // cheat and get day of the week
                dateAfter = DateTime.Parse( (month+1) + "/" + day + "/" + year);
                dayOfWeek = (int)dateAfter.DayOfWeek;

                // parse the date
                dateBefore = (DayOfWeek)dayOfWeek + ", " + c_MONTHS[month] + " " +
                            (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongDatePattern, "dddd, MMMM dd, yyyy", StringComparison.Ordinal) == 0 && 10 > day ? "0" : "") + day + ", " + year;
                dateAfter  = DateTime.ParseExact( dateBefore, "D", formater );

                if ((month+1) != dateAfter.Month || day != dateAfter.Day || year != dateAfter.Year || (DayOfWeek)dayOfWeek != dateAfter.DayOfWeek)
                {
                    TestLibrary.TestFramework.LogError("005", "DateTime.ParseExact(" + dateBefore + ") did not equal (" + dateAfter.DayOfWeek + ", " + c_MONTHS[dateAfter.Month-1] + " " + dateAfter.Day + ", " + dateAfter.Year + ")");
                    retVal = false;
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        DateTime dateAfter;
        string   dateBefore = "";
        int      dayOfWeek; // 0 - 6
        int      day;       // 1 - 28
        int      year;      // 1900 - 2000
        int      month;     // 0 - 11
        int      hour;      // 0 - 11
        int      minute;    // 0 - 59
        int      second;    // 0 - 59
        int      timeOfDay; // 0 -1
        string[] twelveHour = new string[2] {"AM", "PM"};
        DateTime dateBeforeForValidity;

        TestLibrary.TestFramework.BeginScenario("PosTest5: DateTime.ParseExact(F, dddd, MMMM dd, yyyy h:mm:ss tt (FullDateTimePattern ex: Thursday, January 03, 2002 12:00:00 AM))");

        // Culture could be customized
        if (!((String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongDatePattern, "dddd, MMMM dd, yyyy", StringComparison.Ordinal) == 0 ||
               String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongDatePattern, "dddd, MMMM d, yyyy", StringComparison.Ordinal) == 0) && 
              (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongTimePattern, "hh:mm:ss tt", StringComparison.Ordinal) == 0 ||
               String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongTimePattern, "h:mm:ss tt", StringComparison.Ordinal) == 0)))

        {
            TestLibrary.TestFramework.LogInformation("Skipping test case, " +
                                                     " LongDatePattern: " + TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongDatePattern +
                                                     " LongTimePattern: " + TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongTimePattern);
            return retVal;
        }

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12);
                hour      = (TestLibrary.Generator.GetInt32(-55) % 12);
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                second    = (TestLibrary.Generator.GetInt32(-55) % 60);
                timeOfDay = (TestLibrary.Generator.GetInt32(-55) % 2);

                dateBeforeForValidity = new DateTime(year, month + 1, day, hour, minute, second);

        		// cheat and get day of the week
                dateAfter = DateTime.Parse( (month+1) + "/" + day + "/" + year);
                dayOfWeek = (int)dateAfter.DayOfWeek;

                // parse the date
                dateBefore = (DayOfWeek)dayOfWeek + ", " + 
                             c_MONTHS[month] + " " +
                             (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongDatePattern, "dddd, MMMM dd, yyyy", StringComparison.Ordinal) == 0 && 10 > day ? "0" : "") + day + ", " + 
                             year + " " +
                             (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongTimePattern, "hh:mm:ss tt", StringComparison.Ordinal) == 0 && 10 > hour ? "0" : "") + hour + ":" + 
                             (10 > minute ? "0" : "") + minute + ":" + 
                             (10 > second ? "0" : "") + second + " " + twelveHour[timeOfDay];

                if (!TestLibrary.Utilities.IsWindows)
                {
                    dateAfter = DateTime.Parse(dateBefore);
                    TimeSpan span = dateAfter - dateAfter.ToUniversalTime();
                    String strSpan = (span.Duration()==span ? "+" : "-") + 
                        (10 > span.Duration().Hours ? "0" : "") + span.Duration().Hours + 
                        ":" + (10 > span.Minutes ? "0" : "") + span.Minutes;
                    dateBefore += " " + strSpan;
                }
                
                dateAfter  = DateTime.ParseExact( dateBefore, "F", formater );
                
                //Dev10 Bug 686124: For mac, the ambiguous and invalid points in time on the Mac
                if (false == TimeZoneInfo.Local.IsInvalidTime(dateBeforeForValidity))
                {
                    if ((month + 1) != dateAfter.Month
                           || day != dateAfter.Day
                           || year != dateAfter.Year
                           || (DayOfWeek)dayOfWeek != dateAfter.DayOfWeek
                           || (hour + timeOfDay * 12) != dateAfter.Hour
                           || minute != dateAfter.Minute
                           || second != dateAfter.Second)
                    {
                        TestLibrary.TestFramework.LogError("009", "DateTime.ParseExact(" + dateBefore + ") did not equal (" + dateAfter.DayOfWeek + ", " + c_MONTHS[dateAfter.Month - 1] + " " + dateAfter.Day + ", " + dateAfter.Year + " " + dateAfter.Hour + ":" + dateAfter.Minute + ":" + dateAfter.Second + ")");
                        retVal = false;
                    }
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        DateTime dateAfter;
        string   dateBefore = "";
        int      day;       // 1 - 28
        int      month;     // 1 - 12
        int      year;      // 1900 - 2000
        int      hour;      // 0 - 11
        int      minute;    // 0 - 59
        int      second;    // 0 - 59
        int      timeOfDay; // 0 -1
        string[] twelveHour = new string[2] {"AM", "PM"};

        DateTime dateBeforeForValidity;

        TestLibrary.TestFramework.BeginScenario("PosTest7: DateTime.ParseExact(G, ex: 1/3/2002 12:00:00 AM)");

        // Culture could be customized
        if (!(String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.ShortDatePattern, "M/d/yyyy", StringComparison.Ordinal) == 0 &&
              (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongTimePattern, "hh:mm:ss tt", StringComparison.Ordinal) == 0 ||
               String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongTimePattern, "h:mm:ss tt", StringComparison.Ordinal) == 0)))
        {
            TestLibrary.TestFramework.LogInformation("Skipping test case, " +
                                                     " ShortDatePattern: " + TestLibrary.Utilities.CurrentCulture.DateTimeFormat.ShortDatePattern +
                                                     " LongTimePattern: " + TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongTimePattern);
            return retVal;
        }

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1930;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12) + 1;
                hour      = (TestLibrary.Generator.GetInt32(-55) % 12);
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                second    = (TestLibrary.Generator.GetInt32(-55) % 60);
                timeOfDay = (TestLibrary.Generator.GetInt32(-55) % 2);
                dateBeforeForValidity = new DateTime(year, month, day, hour, minute, second);

                // parse the date
                dateBefore =  month + "/" + + day + "/" + year + " " +
                              (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongTimePattern, "hh:mm:ss tt", StringComparison.Ordinal) == 0 && 10 > hour ? "0" : "") + hour + ":" + 
                              (10 > minute ? "0" : "") + minute + ":" + (10 > second ? "0" : "") + second + " " + twelveHour[timeOfDay];

                if (!TestLibrary.Utilities.IsWindows)
                {
                    dateAfter = DateTime.Parse(dateBefore);
                    TimeSpan span = dateAfter - dateAfter.ToUniversalTime();
                    String strSpan = (span.Duration()==span ? "+" : "-") + 
                        (10 > span.Duration().Hours ? "0" : "") + span.Duration().Hours + 
                        ":" + (10 > span.Minutes ? "0" : "") + span.Minutes;
                    dateBefore += " " + strSpan;
                }

                dateAfter = DateTime.ParseExact(dateBefore, "G", formater);

                //Dev10 Bug 686124: For mac, the ambiguous and invalid points in time on the Mac
                if (false == TimeZoneInfo.Local.IsInvalidTime(dateBeforeForValidity))
                {
                    if (month != dateAfter.Month
                           || day != dateAfter.Day
                           || year != dateAfter.Year
                           || (hour + timeOfDay * 12) != dateAfter.Hour
                           || minute != dateAfter.Minute
                           || second != dateAfter.Second)
                    {
                        TestLibrary.TestFramework.LogError("013", "DateTime.ParseExact(" + dateBefore + ") did not equal (" + dateAfter.Month + "/" + dateAfter.Day + "/" + dateAfter.Year + " " + dateAfter.Hour + ":" + dateAfter.Minute + ":" + dateAfter.Second + ")");
                        retVal = false;
                    }
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        DateTime dateAfter;
        string   dateBefore = "";
        int      day;       // 1 - 28
        int      month;     // 0 - 11

        TestLibrary.TestFramework.BeginScenario("PosTest8: DateTime.ParseExact(m, MMMM dd (MonthDayPattern ex: January 03))");

        // Culture could be customized
        if (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.MonthDayPattern, "MMMM dd", StringComparison.Ordinal) != 0)
        {
            TestLibrary.TestFramework.LogInformation("Skipping test case, MonthDayPattern: " + TestLibrary.Utilities.CurrentCulture.DateTimeFormat.MonthDayPattern);
            return retVal;
        }

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12);

                // parse the date
                dateBefore = c_MONTHS[month] + " " + (10>day?"0":"") + day;
                dateAfter  = DateTime.ParseExact( dateBefore, "m", formater );

                if ((month+1) != dateAfter.Month 
                       || day != dateAfter.Day)
                {
                    TestLibrary.TestFramework.LogError("015", "DateTime.ParseExact(" + dateBefore + ") did not equal (" + c_MONTHS[dateAfter.Month-1] + " " + dateAfter.Day + ")");
                    retVal = false;
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest9()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        DateTime dateAfter;
        string   dateBefore = "";
        int      dayOfWeek; // 0 - 6
        int      day;       // 1 - 28
        int      year;      // 1900 - 2000
        int      month;     // 0 - 11
        int      hour;      // 12 - 23
        int      minute;    // 0 - 59
        int      second;    // 0 - 59
        DateTime dateBeforeForValidity;

        TestLibrary.TestFramework.BeginScenario("PosTest9: DateTime.ParseExact(R, ddd, dd MMM yyyy HH':'mm':'ss 'GMT' (RFC1123Pattern ex: Thu, 03 Jan 2002 00:00:00 GMT))");

        // if there is any change in RFC1123Pattern, this test case would fail. The formatting should be updated!!!
        if (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.RFC1123Pattern, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", StringComparison.Ordinal) != 0)
        {
            TestLibrary.TestFramework.LogError("PosTest9", "Skipping test case, RFC1123Pattern: " + TestLibrary.Utilities.CurrentCulture.DateTimeFormat.RFC1123Pattern);
            return false;
        }

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12);
                hour      = (TestLibrary.Generator.GetInt32(-55) % 12) + 12;	// Parse will convert perform GMT -> PST conversion
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                second    = (TestLibrary.Generator.GetInt32(-55) % 60);
                dayOfWeek = (TestLibrary.Generator.GetInt32(-55) % 7);

                dateBeforeForValidity = new DateTime(year, month + 1, day, hour, minute, second);

		// cheat and get day of the week
                dateAfter = DateTime.Parse( (month+1) + "/" + day + "/" + year);
                dayOfWeek = (int)dateAfter.DayOfWeek;

                // parse the date
                dateBefore = c_DAYS_SH[dayOfWeek] + ", " + (10>day?"0":"") + day + " " + c_MONTHS_SH[month] + " " + year + " " + (10>hour?"0":"") + hour + ":" + (10>minute?"0":"") + minute + ":" + (10>second?"0":"") + second + " GMT";
                dateAfter  = DateTime.ParseExact( dateBefore, "R", formater );

                //Dev10 Bug 686124: For mac, the ambiguous and invalid points in time on the Mac
                if (false == TimeZoneInfo.Local.IsInvalidTime(dateBeforeForValidity))
                {
                    if ((month + 1) != dateAfter.Month
                           || day != dateAfter.Day
                           || year != dateAfter.Year
                           || (DayOfWeek)dayOfWeek != dateAfter.DayOfWeek
                           || minute != dateAfter.Minute
                           || second != dateAfter.Second)
                    {
                        TestLibrary.TestFramework.LogError("017", "DateTime.ParseExact(" + dateBefore + ") did not equal (" + c_DAYS_SH[(int)dateAfter.DayOfWeek] + ", " + dateAfter.Day + " " + c_MONTHS_SH[dateAfter.Month - 1] + " " + dateAfter.Year + " " + dateAfter.Hour + ":" + dateAfter.Minute + ":" + dateAfter.Second + " GMT)");
                        retVal = false;
                    }
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest10()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        DateTime dateAfter;
        string   dateBefore = "";
        int      day;       // 1 - 28
        int      month;     // 1 - 12
        int      year;      // 1900 - 2000
        int      hour;      // 0 - 23
        int      minute;    // 0 - 59
        int      second;    // 0 - 59
        DateTime dateBeforeForValidity;

        TestLibrary.TestFramework.BeginScenario("PosTest10: DateTime.ParseExact(s, yyyy'-'MM'-'dd'T'HH':'mm':'ss (SortableDateTimePattern ex: 2002-01-03T00:00:00))");
        // if there is any change in SortableDateTimePattern, this test case would fail, The formatting should be updated!!!
        if (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.SortableDateTimePattern, "yyyy'-'MM'-'dd'T'HH':'mm':'ss", StringComparison.Ordinal) != 0)
        {
            TestLibrary.TestFramework.LogError("PosTest10", "Skipping test case, SortableDateTimePattern: " + TestLibrary.Utilities.CurrentCulture.DateTimeFormat.SortableDateTimePattern);
            return false;
        }

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12) + 1;
                hour      = (TestLibrary.Generator.GetInt32(-55) % 24);
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                second    = (TestLibrary.Generator.GetInt32(-55) % 60);

                dateBeforeForValidity = new DateTime(year, month, day, hour, minute, second);

                // parse the date
                dateBefore = year + "-" + (10>month?"0":"") + month + "-" + (10>day?"0":"") + day + "T" + ((10 > hour) ? "0" : "") + hour + ":" + ((10 > minute) ? "0" : "") + minute + ":" + ((10 > second) ? "0" : "") + second;
                dateAfter  = DateTime.ParseExact( dateBefore, "s", formater );

                //Dev10 Bug 686124: For mac, the ambiguous and invalid points in time on the Mac
                if (false == TimeZoneInfo.Local.IsInvalidTime(dateBeforeForValidity))
                {
                    if (month != dateAfter.Month
                           || day != dateAfter.Day
                           || year != dateAfter.Year
                           || hour != dateAfter.Hour
                           || minute != dateAfter.Minute
                           || second != dateAfter.Second)
                    {
                        TestLibrary.TestFramework.LogError("019", "DateTime.ParseExact(" + dateBefore + ") did not equal (" + dateAfter.Year + "-" + dateAfter.Month + "-" + dateAfter.Day + "T" + dateAfter.Hour + ":" + dateAfter.Minute + ":" + dateAfter.Second + ")");
                        retVal = false;
                    }
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("020", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest12()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        DateTime dateAfter;
        string   dateBefore = "";
        int      hour;      // 0 - 11
        int      minute;    // 0 - 59
        int      second;    // 0 - 59
        int      timeOfDay; // 0 -1
        string[] twelveHour = new string[2] {"AM", "PM"};

        TestLibrary.TestFramework.BeginScenario("PosTest12: DateTime.ParseExact(T, h:mm:ss tt (LongTimePattern ex: 12:00:00 AM))");

        // Culture could be customized
        if (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongTimePattern, "hh:mm:ss tt", StringComparison.Ordinal) != 0 &&
            String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongTimePattern, "h:mm:ss tt", StringComparison.Ordinal) != 0)
        {
            TestLibrary.TestFramework.LogInformation("Skipping test case, " + " LongTimePattern: " + TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongTimePattern);
            return retVal;
        }

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                hour      = (TestLibrary.Generator.GetInt32(-55) % 12);
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                second    = (TestLibrary.Generator.GetInt32(-55) % 60);
                timeOfDay = (TestLibrary.Generator.GetInt32(-55) % 2);

                // parse the date
                int newHour = hour==0?12:hour;
                dateBefore = (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.LongTimePattern, "hh:mm:ss tt", StringComparison.Ordinal) == 0 && 10 > newHour ? "0" : "") + newHour + 
                             ":" + (10 > minute ? "0" : "") + minute + ":" + (10 > second ? "0" : "") + second + " " + twelveHour[timeOfDay];
                if (!TestLibrary.Utilities.IsWindows)
                {
                    dateAfter = DateTime.Parse(dateBefore);
                    TimeSpan span = dateAfter - dateAfter.ToUniversalTime();
                    String strSpan = (span.Duration()==span ? "+" : "-") + 
                        (10 > span.Duration().Hours ? "0" : "") + span.Duration().Hours + 
                        ":" + (10 > span.Minutes ? "0" : "") + span.Minutes;
                    dateBefore += " " + strSpan;
                }

                dateAfter = DateTime.ParseExact(dateBefore, "T", formater);

                if ((hour + timeOfDay*12) != dateAfter.Hour
                       || minute != dateAfter.Minute
                       || second != dateAfter.Second)
                {
                    TestLibrary.TestFramework.LogError("023", "DateTime.ParseExact(" + dateBefore + ") did not equal (" + dateAfter.Hour + ":" + dateAfter.Minute + ":" + dateAfter.Second + ")");
                    retVal = false;
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("024", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("024", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest13()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        DateTime dateAfter;
        string   dateBefore = "";
        int      day;       // 1 - 28
        int      month;     // 1 - 12
        int      year;      // 1900 - 2000
        int      hour;      // 12 - 23
        int      minute;    // 0 - 59
        int      second;    // 0 - 59
        DateTime dateBeforeForValidity;

        TestLibrary.TestFramework.BeginScenario("PosTest13: DateTime.ParseExact(u, yyyy'-'MM'-'dd HH':'mm':'ss'Z' (UniversalSortableDateTimePattern ex: 2002-01-03 00:00:00Z))");
        
        // if there is any change in SortableDateTimePattern, this test case would fail, The formatting should be updated!!!
        if (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern, "yyyy'-'MM'-'dd HH':'mm':'ss'Z'", StringComparison.Ordinal) != 0)
        {
            TestLibrary.TestFramework.LogError("PosTest13", "Skipping test case, UniversalSortableDateTimePattern: " + TestLibrary.Utilities.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern);
            return false;
        }

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12) + 1;
                hour      = (TestLibrary.Generator.GetInt32(-55) % 12) + 12;	// conversion
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                second    = (TestLibrary.Generator.GetInt32(-55) % 60);

                dateBeforeForValidity = new DateTime(year, month, day, hour, minute, second);

                // parse the date
                dateBefore = year + "-" + (10>month?"0":"") + month + "-" + (10>day?"0":"") + day + " " + ((10 > hour) ? "0" : "") + hour + ":" + ((10 > minute) ? "0" : "") + minute + ":" + ((10 > second) ? "0" : "") + second +"Z";
                dateAfter  = DateTime.ParseExact( dateBefore, "u", formater );

                //Dev10 Bug 686124: For mac, the ambiguous and invalid points in time on the Mac
                if (false == TimeZoneInfo.Local.IsInvalidTime(dateBeforeForValidity))
                {
                    if (month != dateAfter.Month
                           || day != dateAfter.Day
                           || year != dateAfter.Year
                           || minute != dateAfter.Minute
                           || second != dateAfter.Second)
                    {
                        TestLibrary.TestFramework.LogError("025", "DateTime.ParseExact(" + dateBefore + ") did not equal (" + dateAfter.Year + "-" + dateAfter.Month + "-" + dateAfter.Day + " " + dateAfter.Hour + ":" + dateAfter.Minute + ":" + dateAfter.Second + "Z)");
                        retVal = false;
                    }
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("026", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("026", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest14()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        DateTime dateAfter;
        string   dateBefore = "";
        int      year;      // 1900 - 2000
        int      month;     // 0 - 11

        TestLibrary.TestFramework.BeginScenario("PosTest14: DateTime.ParseExact(y, MMMM, yyyy (YearMonthPattern ex: January, 2002))");
        
        // Culture could be customized
        if (String.Compare(TestLibrary.Utilities.CurrentCulture.DateTimeFormat.YearMonthPattern, "MMMM, yyyy", StringComparison.Ordinal) != 0 )
        {
            TestLibrary.TestFramework.LogInformation("Skipping test case, YearMonthPattern: " + TestLibrary.Utilities.CurrentCulture.DateTimeFormat.YearMonthPattern);
            return retVal;
        }

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12);

                dateBefore = c_MONTHS[month] + ", " + year;
                dateAfter = DateTime.ParseExact(dateBefore, "y", formater);

                if ((month+1) != dateAfter.Month
                        || year != dateAfter.Year)
                {
                    TestLibrary.TestFramework.LogError("027", "DateTime.ParseExact(" + dateBefore + ") did not equal (" + c_MONTHS[dateAfter.Month-1] + ", " + dateAfter.Year + ")-("+dateAfter.ToString()+")-DST("+dateAfter.IsDaylightSavingTime()+")");
                    retVal = false;
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("028", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("028", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest15()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        string   dateBefore = "";
        DateTime dateAfter;

        TestLibrary.TestFramework.BeginScenario("PosTest15: DateTime.ParseExact(G, DateTime.Now, null)");

        try
        {
            dateBefore = DateTime.Now.ToString();

            dateAfter = DateTime.ParseExact( dateBefore, "G", null );

            if (!dateBefore.Equals(dateAfter.ToString()))
            {
                TestLibrary.TestFramework.LogError("101", "DateTime.ParseExact(" + dateBefore + ") did not equal " + dateAfter.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool      retVal = true;
        MyFormater formater = new MyFormater();

        TestLibrary.TestFramework.BeginScenario("NegTest1: DateTime.ParseExact(null)");

        try
        {
            DateTime.ParseExact(null, "d", formater);

            TestLibrary.TestFramework.LogError("029", "DateTime.ParseExact(null) should have thrown");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("030", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool      retVal = true;
        MyFormater formater = new MyFormater();

        TestLibrary.TestFramework.BeginScenario("NegTest2: DateTime.ParseExact(String.Empty)");

        try
        {
            DateTime.ParseExact(String.Empty, "d", formater);

            TestLibrary.TestFramework.LogError("031", "DateTime.ParseExact(String.Empty) should have thrown");
            retVal = false;
        }
        catch (FormatException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("032", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool   retVal = true;
        MyFormater formater = new MyFormater();
        string strDateTime = "";
        DateTime dateAfter;
        string[] formats = new string[17] {"d", "D", "f", "F", "g", "G", "m", "M", "r", "R", "s", "t", "T", "u", "U", "y", "Y"};
        string   format;
        int      formatIndex;
       

        TestLibrary.TestFramework.BeginScenario("NegTest3: DateTime.ParseExact(<garbage>)");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                try
                {
                    formatIndex = TestLibrary.Generator.GetInt32(-55) % 34;

                    if (0 <= formatIndex && formatIndex < 17)
                    {
                        format = formats[formatIndex];
                    }
                    else
                    {
                        format = TestLibrary.Generator.GetChar(-55) + "";
                    }

                    strDateTime = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                    dateAfter = DateTime.ParseExact(strDateTime, format, formater);

                    TestLibrary.TestFramework.LogError("033", "DateTime.ParseExact(" + strDateTime + ", "+ format + ") should have thrown (" + dateAfter + ")");
                    retVal = false;
                }
                catch (FormatException)
                {
                    // expected
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("034", "Failing date: " + strDateTime);
            TestLibrary.TestFramework.LogError("034", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        string   dateBefore = "";
        DateTime dateAfter;

        TestLibrary.TestFramework.BeginScenario("NegTest4: DateTime.ParseExact(\"\", DateTime.Now, formater)");

        try
        {
            dateBefore = DateTime.Now.ToString();

            dateAfter = DateTime.ParseExact( dateBefore, "", formater );

            TestLibrary.TestFramework.LogError("103", "DateTime.ParseExact(" + dateBefore + ") should have thrown " + dateAfter.ToString());
            retVal = false;
        }
        catch (System.FormatException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        string   dateBefore = "";
        DateTime dateAfter;

        TestLibrary.TestFramework.BeginScenario("NegTest5: DateTime.ParseExact(null, DateTime.Now, formater)");

        try
        {
            dateBefore = DateTime.Now.ToString();

            dateAfter = DateTime.ParseExact( dateBefore, null, formater );

            TestLibrary.TestFramework.LogError("105", "DateTime.ParseExact(" + dateBefore + ") should have thrown " + dateAfter.ToString());
            retVal = false;
        }
        catch (System.ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}

public class MyFormater : IFormatProvider
{
    public object GetFormat(Type formatType)
    {
        if (typeof(IFormatProvider) == formatType)
        {
            return this;
        }
        else
        {
            return null;
        }
    }
}

