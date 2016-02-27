// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

public class DateTimeParse1
{
    private CultureInfo CurrentCulture = TestLibrary.Utilities.CurrentCulture;
    private const  int c_MIN_STRING_LEN = 1;
    private const  int c_MAX_STRING_LEN = 2048;
    private const  int c_NUM_LOOPS      = 100;
    //new string[12] {"January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"};
    private static string[] c_MONTHS    = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames;
    //new string[12] {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
    private static string[] c_MONTHS_SH = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthGenitiveNames;
    //new string[7]  {"Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"};
    private static string[] c_DAYS_SH   = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedDayNames;

    public static int Main()
    {
        DateTimeParse1 test = new DateTimeParse1();

        TestLibrary.TestFramework.BeginTestCase("DateTimeParse1");

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
        TestLibrary.Utilities.CurrentCulture = CultureInfo.InvariantCulture;

        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;
        retVal = PosTest12() && retVal;
        retVal = PosTest13() && retVal;
        retVal = PosTest14() && retVal;

        TestLibrary.Utilities.CurrentCulture = CurrentCulture;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool     retVal = true;
        string   dateBefore = "";
        DateTime dateAfter;

        TestLibrary.TestFramework.BeginScenario("PosTest1: DateTime.Parse(DateTime.Now)");

        try
        {
            dateBefore = DateTime.Now.ToString();

            dateAfter = DateTime.Parse( dateBefore );

            if (!dateBefore.Equals(dateAfter.ToString()))
            {
                TestLibrary.TestFramework.LogError("001", "DateTime.Parse(" + dateBefore + ") did not equal " + dateAfter.ToString());
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
        DateTime dateAfter;
        string   dateBefore = "";
        int      day;    // 1 - 29
        int      year;   // 1900 - 2000
        int      month;  // 1 - 12

        TestLibrary.TestFramework.BeginScenario("PosTest2: DateTime.Parse(M/d/yyyy (ShortDatePattern ex: 1/3/2002))");

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day    = (TestLibrary.Generator.GetInt32(-55) % 28)  + 1;
                year   = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month  = (TestLibrary.Generator.GetInt32(-55) % 12)  + 1;

                dateBefore = month + "/" + day + "/" + year;
                dateAfter  = DateTime.Parse( dateBefore );

                if (month != dateAfter.Month || day != dateAfter.Day || year != dateAfter.Year)
                {
                    TestLibrary.TestFramework.LogError("003", "DateTime.Parse(" + dateBefore + ") did not equal " + dateAfter.ToString());
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
        DateTime dateAfter;
        string   dateBefore = "";
        int      dayOfWeek; // 0 - 6
        int      day;       // 1 - 28
        int      year;      // 1900 - 2000
        int      month;     // 0 - 11

        TestLibrary.TestFramework.BeginScenario("PosTest3: DateTime.Parse(dddd, MMMM dd, yyyy (LongDatePattern ex: Thursday, January 03, 2002))");

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
                dateBefore = (DayOfWeek)dayOfWeek + ", " + c_MONTHS[month] + " " + day + ", " + year;
                dateAfter  = DateTime.Parse( dateBefore );

                if ((month+1) != dateAfter.Month || day != dateAfter.Day || year != dateAfter.Year || (DayOfWeek)dayOfWeek != dateAfter.DayOfWeek)
                {
                    TestLibrary.TestFramework.LogError("005", "DateTime.Parse(" + dateBefore + ") did not equal (" + dateAfter.DayOfWeek + ", " + c_MONTHS[dateAfter.Month-1] + " " + dateAfter.Day + ", " + dateAfter.Year + ")");
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

    public bool PosTest4()
    {
        bool     retVal = true;
        DateTime dateAfter;
        string   dateBefore = "";
        int      dayOfWeek; // 0 - 6
        int      day;       // 1 - 28
        int      year;      // 1900 - 2000
        int      month;     // 0 - 11
        int      hour;      // 0 - 11
        int      minute;    // 0 - 59
        int      timeOfDay; // 0 -1
        string[] twelveHour = new string[2] {"AM", "PM"};

        TestLibrary.TestFramework.BeginScenario("PosTest4: DateTime.Parse(ex: Thursday, January 03, 2002 12:00 AM)");

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12);
                hour      = (TestLibrary.Generator.GetInt32(-55) % 12);
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                timeOfDay = (TestLibrary.Generator.GetInt32(-55) % 2);

		// cheat and get day of the week
                dateAfter = DateTime.Parse( (month+1) + "/" + day + "/" + year);
                dayOfWeek = (int)dateAfter.DayOfWeek;

                // parse the date
                dateBefore = (DayOfWeek)dayOfWeek + ", " + c_MONTHS[month] + " " + day + ", " + year + " " + hour + ":" + minute + " " + twelveHour[timeOfDay];
                dateAfter  = DateTime.Parse( dateBefore );

                if ((month+1) != dateAfter.Month 
                       || day != dateAfter.Day 
                       || year != dateAfter.Year 
                       || (DayOfWeek)dayOfWeek != dateAfter.DayOfWeek 
                       || (hour + timeOfDay*12) != dateAfter.Hour
                       || minute != dateAfter.Minute)
                {
                    TestLibrary.TestFramework.LogError("007", "DateTime.Parse(" + dateBefore + ") did not equal (" + dateAfter.DayOfWeek + ", " + c_MONTHS[dateAfter.Month-1] + " " + dateAfter.Day + ", " + dateAfter.Year + " " + dateAfter.Hour + ":" + dateAfter.Minute + ")");
                    retVal = false;
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool     retVal = true;
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: DateTime.Parse(dddd, MMMM dd, yyyy h:mm:ss tt (FullDateTimePattern ex: Thursday, January 03, 2002 12:00:00 AM))");

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

		// cheat and get day of the week
                dateAfter = DateTime.Parse( (month+1) + "/" + day + "/" + year);
                dayOfWeek = (int)dateAfter.DayOfWeek;

                // parse the date
                dateBefore = (DayOfWeek)dayOfWeek + ", " + c_MONTHS[month] + " " + day + ", " + year + " " + hour + ":" + minute + ":" + second + " " + twelveHour[timeOfDay];
                dateAfter  = DateTime.Parse( dateBefore );

                if ((month+1) != dateAfter.Month 
                       || day != dateAfter.Day 
                       || year != dateAfter.Year 
                       || (DayOfWeek)dayOfWeek != dateAfter.DayOfWeek 
                       || (hour + timeOfDay*12) != dateAfter.Hour
                       || minute != dateAfter.Minute
                       || second != dateAfter.Second)
                {
                    TestLibrary.TestFramework.LogError("009", "DateTime.Parse(" + dateBefore + ") did not equal (" + dateAfter.DayOfWeek + ", " + c_MONTHS[dateAfter.Month-1] + " " + dateAfter.Day + ", " + dateAfter.Year + " " + dateAfter.Hour + ":" + dateAfter.Minute + ":" + dateAfter.Second + ")");
                    retVal = false;
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

    public bool PosTest6()
    {
        bool     retVal = true;
        DateTime dateAfter;
        string   dateBefore = "";
        int      day;       // 1 - 28
        int      month;     // 1 - 12
        int      year;      // 1900 - 2000
        int      hour;      // 0 - 11
        int      minute;    // 0 - 59
        int      timeOfDay; // 0 -1
        string[] twelveHour = new string[2] {"AM", "PM"};

        TestLibrary.TestFramework.BeginScenario("PosTest6: DateTime.Parse(ex: 1/3/2002 12:00 AM)");

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12) + 1;
                hour      = (TestLibrary.Generator.GetInt32(-55) % 12);
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                timeOfDay = (TestLibrary.Generator.GetInt32(-55) % 2);

                // parse the date
                dateBefore = month + "/" + day + "/" + year + " " + hour + ":" + minute + " " + twelveHour[timeOfDay];
                dateAfter  = DateTime.Parse( dateBefore );

                if (month != dateAfter.Month 
                       || day != dateAfter.Day 
                       || year != dateAfter.Year 
                       || (hour + timeOfDay*12) != dateAfter.Hour
                       || minute != dateAfter.Minute)
                {
                    TestLibrary.TestFramework.LogError("011", "DateTime.Parse(" + dateBefore + ") did not equal (" + dateAfter.Month + "/" + dateAfter.Day + "/" + dateAfter.Year + " " + dateAfter.Hour + ":" + dateAfter.Minute + ")");
                    retVal = false;
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool     retVal = true;
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

        TestLibrary.TestFramework.BeginScenario("PosTest7: DateTime.Parse(ex: 1/3/2002 12:00:00 AM)");

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12) + 1;
                hour      = (TestLibrary.Generator.GetInt32(-55) % 12);
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                second    = (TestLibrary.Generator.GetInt32(-55) % 60);
                timeOfDay = (TestLibrary.Generator.GetInt32(-55) % 2);

                // parse the date
                dateBefore = month + "/" + day + "/" + year + " " + hour + ":" + minute + ":" + second + " " + twelveHour[timeOfDay];
                dateAfter  = DateTime.Parse( dateBefore );

                if (month != dateAfter.Month 
                       || day != dateAfter.Day 
                       || year != dateAfter.Year 
                       || (hour + timeOfDay*12) != dateAfter.Hour
                       || minute != dateAfter.Minute
                       || second != dateAfter.Second)
                {
                    TestLibrary.TestFramework.LogError("013", "DateTime.Parse(" + dateBefore + ") did not equal (" + dateAfter.Month + "/" + dateAfter.Day + "/" + dateAfter.Year + " " + dateAfter.Hour + ":" + dateAfter.Minute + ":" + dateAfter.Second + ")");
                    retVal = false;
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
        DateTime dateAfter;
        string   dateBefore = "";
        int      day;       // 1 - 28
        int      month;     // 0 - 11

        TestLibrary.TestFramework.BeginScenario("PosTest8: DateTime.Parse(MMMM dd (MonthDayPattern ex: January 03))");

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12);

                // parse the date
                dateBefore = c_MONTHS[month] + " " + day;
                dateAfter  = DateTime.Parse( dateBefore );

                if ((month+1) != dateAfter.Month 
                       || day != dateAfter.Day)
                {
                    TestLibrary.TestFramework.LogError("015", "DateTime.Parse(" + dateBefore + ") did not equal (" + c_MONTHS[dateAfter.Month-1] + " " + dateAfter.Day + ")");
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
        DateTime dateAfter;
        string   dateBefore = "";
        int      dayOfWeek; // 0 - 6
        int      day;       // 1 - 28
        int      year;      // 1900 - 2000
        int      month;     // 0 - 11
        int      hour;      // 12 - 23
        int      minute;    // 0 - 59
        int      second;    // 0 - 59

        TestLibrary.TestFramework.BeginScenario("PosTest9: DateTime.Parse(ddd, dd MMM yyyy HH':'mm':'ss 'GMT' (RFC1123Pattern ex: Thu, 03 Jan 2002 00:00:00 GMT))");

        DateTime now = DateTime.Now;
        int hourshift;
        if (now - now.ToUniversalTime() < TimeSpan.Zero) // western hemisphere
            hourshift = +12;
        else
            hourshift = 0;
        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12);
                hour      = (TestLibrary.Generator.GetInt32(-55) % 12) + hourshift;	// Parse will convert perform GMT -> PST conversion
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                second    = (TestLibrary.Generator.GetInt32(-55) % 60);
                dayOfWeek = (TestLibrary.Generator.GetInt32(-55) % 7);

		// cheat and get day of the week
                dateAfter = DateTime.Parse( (month+1) + "/" + day + "/" + year);
                dayOfWeek = (int)dateAfter.DayOfWeek;

                // parse the date
                dateBefore = c_DAYS_SH[dayOfWeek] + ", " + day + " " + c_MONTHS_SH[month] + " " + year + " " + hour + ":" + minute + ":" + second + " GMT";
                dateAfter  = DateTime.Parse( dateBefore );

                if ((month+1) != dateAfter.Month
                       || day != dateAfter.Day
                       || year != dateAfter.Year 
                       || (DayOfWeek)dayOfWeek != dateAfter.DayOfWeek 
                       || minute != dateAfter.Minute
                       || second != dateAfter.Second)
                {
                    TestLibrary.TestFramework.LogError("017", "DateTime.Parse(" + dateBefore + ") did not equal (" + c_DAYS_SH[(int)dateAfter.DayOfWeek] + ", " + dateAfter.Day + " " + c_MONTHS_SH[dateAfter.Month-1] + " " + dateAfter.Year + " " + dateAfter.Hour + ":" + dateAfter.Minute + ":" + dateAfter.Second + " GMT)");
                    retVal = false;
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
        DateTime dateAfter;
        string   dateBefore = "";
        int      day;       // 1 - 28
        int      month;     // 1 - 12
        int      year;      // 1900 - 2000
        int      hour;      // 0 - 23
        int      minute;    // 0 - 59
        int      second;    // 0 - 59

        TestLibrary.TestFramework.BeginScenario("PosTest10: DateTime.Parse(yyyy'-'MM'-'dd'T'HH':'mm':'ss (SortableDateTimePattern ex: 2002-01-03T00:00:00))");

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

                // parse the date
                dateBefore = year + "-" + month + "-" + day + "T" + ((10 > hour) ? "0" : "") + hour + ":" + ((10 > minute) ? "0" : "") + minute + ":" + ((10 > second) ? "0" : "") + second;
                dateAfter  = DateTime.Parse( dateBefore );

                if (month != dateAfter.Month 
                       || day != dateAfter.Day 
                       || year != dateAfter.Year 
                       || hour != dateAfter.Hour
                       || minute != dateAfter.Minute
                       || second != dateAfter.Second)
                {
                    TestLibrary.TestFramework.LogError("019", "DateTime.Parse(" + dateBefore + ") did not equal (" + dateAfter.Year + "-" + dateAfter.Month + "-" + dateAfter.Day + "T" + dateAfter.Hour + ":" + dateAfter.Minute + ":" + dateAfter.Second + ")");
                    retVal = false;
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

    public bool PosTest11()
    {
        bool     retVal = true;
        DateTime dateAfter;
        string   dateBefore = "";
        int      hour;      // 0 - 11
        int      minute;    // 0 - 59
        int      timeOfDay; // 0 -1
        string[] twelveHour = new string[2] {"AM", "PM"};

        TestLibrary.TestFramework.BeginScenario("PosTest11: DateTime.Parse(h:mm tt (ShortTimePattern ex: 12:00 AM))");

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                hour      = (TestLibrary.Generator.GetInt32(-55) % 12);
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                timeOfDay = (TestLibrary.Generator.GetInt32(-55) % 2);

                // parse the date
                dateBefore = hour + ":" + minute + " " + twelveHour[timeOfDay];
                dateAfter  = DateTime.Parse( dateBefore );

                if ((hour + timeOfDay*12) != dateAfter.Hour
                       || minute != dateAfter.Minute)
                {
                    TestLibrary.TestFramework.LogError("021", "DateTime.Parse(" + dateBefore + ") did not equal (" + dateAfter.Hour + ":" + dateAfter.Minute + ")");
                    retVal = false;
                }
             }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("022", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest12()
    {
        bool     retVal = true;
        DateTime dateAfter;
        string   dateBefore = "";
        int      hour;      // 0 - 11
        int      minute;    // 0 - 59
        int      second;    // 0 - 59
        int      timeOfDay; // 0 -1
        string[] twelveHour = new string[2] {"AM", "PM"};

        TestLibrary.TestFramework.BeginScenario("PosTest12: DateTime.Parse(h:mm:ss tt (LongTimePattern ex: 12:00:00 AM))");

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                hour      = (TestLibrary.Generator.GetInt32(-55) % 12);
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                second    = (TestLibrary.Generator.GetInt32(-55) % 60);
                timeOfDay = (TestLibrary.Generator.GetInt32(-55) % 2);

                // parse the date
                dateBefore = hour + ":" + minute + ":" + second + " " + twelveHour[timeOfDay];
                dateAfter  = DateTime.Parse( dateBefore );

                if ((hour + timeOfDay*12) != dateAfter.Hour
                       || minute != dateAfter.Minute
                       || second != dateAfter.Second)
                {
                    TestLibrary.TestFramework.LogError("023", "DateTime.Parse(" + dateBefore + ") did not equal (" + dateAfter.Hour + ":" + dateAfter.Minute + ":" + dateAfter.Second + ")");
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
        DateTime dateAfter;
        string   dateBefore = "";
        int      day;       // 1 - 28
        int      month;     // 1 - 12
        int      year;      // 1900 - 2000
        int      hour;      // 12 - 23
        int      minute;    // 0 - 59
        int      second;    // 0 - 59

        TestLibrary.TestFramework.BeginScenario("PosTest13: DateTime.Parse(yyyy'-'MM'-'dd HH':'mm':'ss'Z' (UniversalSortableDateTimePattern ex: 2002-01-03 00:00:00Z))");

        DateTime now = DateTime.Now;
        int hourshift;
        if (now - now.ToUniversalTime() < TimeSpan.Zero) // western hemisphere
            hourshift = +12;
        else
            hourshift = 0;
        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                day       = (TestLibrary.Generator.GetInt32(-55) % 27) + 1;
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12) + 1;
                hour      = (TestLibrary.Generator.GetInt32(-55) % 12) + hourshift;	// conversion
                minute    = (TestLibrary.Generator.GetInt32(-55) % 60);
                second    = (TestLibrary.Generator.GetInt32(-55) % 60);

                // parse the date
                dateBefore = year + "-" + month + "-" + day + " " + ((10 > hour) ? "0" : "") + hour + ":" + ((10 > minute) ? "0" : "") + minute + ":" + ((10 > second) ? "0" : "") + second +"Z";
                dateAfter  = DateTime.Parse( dateBefore );

                if (month != dateAfter.Month 
                       || day != dateAfter.Day 
                       || year != dateAfter.Year 
                       || minute != dateAfter.Minute
                       || second != dateAfter.Second)
                {
                    TestLibrary.TestFramework.LogError("025", "DateTime.Parse(" + dateBefore + ") did not equal (" + dateAfter.Year + "-" + dateAfter.Month + "-" + dateAfter.Day + " " + dateAfter.Hour + ":" + dateAfter.Minute + ":" + dateAfter.Second + "Z)");
                    retVal = false;
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
        DateTime dateAfter;
        string   dateBefore = "";
        int      year;      // 1900 - 2000
        int      month;     // 0 - 11

        TestLibrary.TestFramework.BeginScenario("PosTest14: DateTime.Parse(MMMM, yyyy (YearMonthPattern ex: January, 2002))");

        try
        {
            for(int i=0; i<c_NUM_LOOPS; i++)
            {
                year      = (TestLibrary.Generator.GetInt32(-55) % 100) + 1900;
                month     = (TestLibrary.Generator.GetInt32(-55) % 12);

                // parse the date
                dateBefore = c_MONTHS[month] + ", " + year;
                dateAfter  = DateTime.Parse( dateBefore );

                if ((month+1) != dateAfter.Month
                        || year != dateAfter.Year)
                {
                    TestLibrary.TestFramework.LogError("027", "DateTime.Parse(" + dateBefore + ") did not equal (" + c_MONTHS[dateAfter.Month-1] + ", " + dateAfter.Year + ")");
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

    public bool NegTest1()
    {
        bool      retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: DateTime.Parse(null)");

        try
        {
            DateTime.Parse(null);

            TestLibrary.TestFramework.LogError("029", "DateTime.Parse(null) should have thrown");
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: DateTime.Parse(String.Empty)");

        try
        {
            DateTime.Parse(String.Empty);

            TestLibrary.TestFramework.LogError("031", "DateTime.Parse(String.Empty) should have thrown");
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
        bool     retVal = true;
        string   strDateTime = "";
        DateTime dateAfter;

        TestLibrary.TestFramework.BeginScenario("NegTest3: DateTime.Parse(<garbage>)");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                try
                {
                    strDateTime = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                    dateAfter = DateTime.Parse(strDateTime);

                    TestLibrary.TestFramework.LogError("033", "DateTime.Parse(" + strDateTime + ") should have thrown (" + dateAfter + ")");
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
}
