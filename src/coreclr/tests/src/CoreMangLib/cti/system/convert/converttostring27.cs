using System;
using System.Globalization;
using TestLibrary;

/// <summary>
/// Convert.ToString(Single,IFormatProvider)
/// </summary>
public class ConvertToString27
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random single to string ");

        try
        {
            Single i = TestLibrary.Generator.GetSingle(-55);
            IFormatProvider iFormatProvider = null;
            string str = Convert.ToString(i, iFormatProvider);
            if (str != i.ToString())
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,single is:" + i);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert SingleMaxValue to string");

        try
        {
            Single i = Single.MaxValue;
            IFormatProvider iFormatProvider = new CultureInfo("en-US");
            string str = Convert.ToString(i, iFormatProvider);
            if (str != i.ToString(iFormatProvider))
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert singleMinValue to string");

        try
        {
            Single i = Single.MinValue;
            IFormatProvider iFormatProvider = new CultureInfo("fr-FR");
            string str = Convert.ToString(i, iFormatProvider);
            string result = TestLibrary.GlobLocHelper.OSFloatToString(i, new CultureInfo("fr-FR"));
	    if(!Utilities.IsWindows) //Mac exponential looks a bit different
	    {
		result = "-3,402823E+38";
	    }
            if (str != result)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected.  Actual:" + str + " Result:" + result);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: the single is \"-0.01f\"");

        try
        {
            Single i = -0.01f;
            NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
            numberFormatInfo.NegativeSign = "&";
            numberFormatInfo.NumberDecimalSeparator = ",";
            string str = Convert.ToString(i, numberFormatInfo);
            if (str != "&0,01")
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        ConvertToString27 test = new ConvertToString27();

        TestLibrary.TestFramework.BeginTestCase("ConvertToString27");

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
