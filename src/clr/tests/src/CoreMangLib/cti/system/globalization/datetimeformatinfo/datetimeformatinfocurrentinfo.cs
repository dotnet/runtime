using System;
using System.Globalization;

/// <summary>
/// CurrentInfo
/// </summary>
public class DateTimeFormatInfoCurrentInfo
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call CurrentInfo to get a read only DateTimeFormatInfo based on the CultureInfo of the current thread");

        try
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.CurrentInfo;
            if (!info.IsReadOnly)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling CurrentInfo returns a writable instance");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeFormatInfoCurrentInfo test = new DateTimeFormatInfoCurrentInfo();

        TestLibrary.TestFramework.BeginTestCase("DateTimeFormatInfoCurrentInfo");

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
}
