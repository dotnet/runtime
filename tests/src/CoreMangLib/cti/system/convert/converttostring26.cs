using System;
using System.Globalization;
using TestLibrary;
/// <summary>
/// Convert.ToString(Single)
/// </summary>
public class ConvertToString26
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random single to string");

        try
        {
            Single i = TestLibrary.Generator.GetSingle(-55);
            string str = Convert.ToString(i);
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
            string str = Convert.ToString(i);
            if (str != i.ToString())
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
            string str = Convert.ToString(i);
            if (str != i.ToString())
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: the single is \"-0.0000\"");

        try
        {
            Single i = -0.0000f;
            string str = Convert.ToString(i);
	    if(Utilities.IsWindows) 
	    {
		    if (str != TestLibrary.GlobLocHelper.OSFloatToString(i))
		    {
			TestLibrary.TestFramework.LogError("007", "The result is not the value as expected. Actual:" + str + " Expected:" + TestLibrary.GlobLocHelper.OSFloatToString(i));
			retVal = false;
		    }
	    }
	    else //Special case for Mac see DDB166526, behavior is consistent with Windows in this case
	    {
		    if (str != "0")
		    {
			TestLibrary.TestFramework.LogError("007", "The result is not the value as expected. Actual:" + str + " Expected: 0"); 
			retVal = false;
		    }
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
        ConvertToString26 test = new ConvertToString26();

        TestLibrary.TestFramework.BeginTestCase("ConvertToString26");

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
