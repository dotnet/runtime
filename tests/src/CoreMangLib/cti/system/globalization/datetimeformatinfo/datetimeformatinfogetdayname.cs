using System;
using System.Globalization;

/// <summary>
/// GetDayName(System.DayOfWeek)
/// </summary>
public class DateTimeFormatInfoGetDayName
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call GetAbbreviatedDayName on default invariant DateTimeFormatInfo instance");

        try
        {
            DateTimeFormatInfo info = CultureInfo.InvariantCulture.DateTimeFormat;
            string[] expected = new string[] {
                "Sunday",
                "Monday", 
                "Tuesday",
                "Wednesday",
                "Thursday",
                "Friday",
                "Saturday"
            };

            retVal = VerificationHelper(info, expected, "001.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call GetAbbreviatedDayName on en-us culture DateTimeFormatInfo instance");

        try
        {
            DateTimeFormatInfo info = new CultureInfo("en-us").DateTimeFormat;
            string[] expected = new string[] {
                "Sunday",
                "Monday", 
                "Tuesday",
                "Wednesday",
                "Thursday",
                "Friday",
                "Saturday"
            };

            retVal = VerificationHelper(info, expected, "002.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call GetAbbreviatedDayName on fr-FR culture DateTimeFormatInfo instance");

        try
        {
            DateTimeFormatInfo info = new CultureInfo("fr-FR").DateTimeFormat;
            string[] expected = new string[] {
                "dimanche",
                "lundi", 
                "mardi",
                "mercredi",
                "jeudi",
                "vendredi",
                "samedi"
            };

            retVal = VerificationHelper(info, expected, "003.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call GetAbbreviatedDayName on DateTimeFormatInfo instance created from ctor");

        try
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();
            string[] expected = new string[] {
                "Sunday",
                "Monday", 
                "Tuesday",
                "Wednesday",
                "Thursday",
                "Friday",
                "Saturday"
            };

            retVal = VerificationHelper(info, expected, "004.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException should be thrown when dayofweek is not a valid System.DayOfWeek value. ");

        try
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();

            info.GetAbbreviatedDayName((DayOfWeek)(-1));

            TestLibrary.TestFramework.LogError("101.1", "ArgumentOutOfRangeException is not thrown");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeFormatInfoGetDayName test = new DateTimeFormatInfoGetDayName();

        TestLibrary.TestFramework.BeginTestCase("DateTimeFormatInfoGetDayName");

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
    
    #region Private Methods
    private bool VerificationHelper(DateTimeFormatInfo info, string[] expected, string errorno)
    {
        bool retval = true;

        DayOfWeek[] values = new DayOfWeek[] {
            DayOfWeek.Sunday,
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday
        };

        for (int i = 0; i < values.Length; ++i)
        {
            string actual = info.GetDayName(values[i]);
            if (actual != expected[i])
            {
                TestLibrary.TestFramework.LogError(errorno, "GetDayName returns wrong value");
                TestLibrary.TestFramework.LogInformation("WARNING[LOCAL VARIABLES] values[i] = " + values[i] + ", expected[i] = " + expected[i] + ", actual = " + actual);
                retval = false;
            }
        }

        return retval;
    }
    #endregion
}
