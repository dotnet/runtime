using System;
using System.Globalization;
using TestLibrary;

//System.Int32.ToString(System.IFormatProvider);
public class Int32ToString2
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert the int32MaxValue to String ");

        try
        {
            Int32 i1 = Int32.MaxValue;
            string s1 = i1.ToString(new CultureInfo("en-US"));
            if (s1 != GlobLocHelper.OSInt32ToString(i1, new CultureInfo("en-US")))
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected. ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert the int32MinValue to String ");

        try
        {
            Int32 i1 = Int32.MinValue;
            string s1 = i1.ToString(new CultureInfo("pl-PL"));
            if (s1 != GlobLocHelper.OSInt32ToString(i1, new CultureInfo("pl-PL")))
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected. ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert the \"-0\" to String ");

        try
        {
            Int32 i1 = -0;
            string s1 = i1.ToString(new CultureInfo("pt-PT"));
            if (s1 != GlobLocHelper.OSInt32ToString(i1, new CultureInfo("pt-PT")))
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected. ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Check the int32 in the beginning of which there are some zeros ");

        try
        {
            Int32 i1 = -00000765;
            string s1 = i1.ToString((IFormatProvider)null);
            if (s1 != GlobLocHelper.OSInt32ToString(i1))
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected. ");
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
    #endregion

    public static int Main()
    {
        Int32ToString2 test = new Int32ToString2();

        TestLibrary.TestFramework.BeginTestCase("Int32ToString2");

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
