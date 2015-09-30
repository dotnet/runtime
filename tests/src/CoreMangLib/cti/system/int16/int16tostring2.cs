using System;
using System.Globalization;
using TestLibrary;

//System.Int16.ToString(System.IFormatProvider);
public class Int16ToString2
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
        if (Utilities.IsWindows)
        {
        //    retVal = NegTest1() && retVal; // Disabled until neutral cultures are available
        }

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert the int16MaxValue to String ");

        try
        {
            Int16 i1 = Int16.MaxValue;
            string s1 = i1.ToString(new CultureInfo("en-US"));
            if (s1 != GlobLocHelper.OSInt16ToString(i1, new CultureInfo("en-US")))
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert the int16MinValue to String ");

        try
        {
            Int16 i1 = Int16.MinValue;
            string s1 = i1.ToString(new CultureInfo("en-ZA"));
            if (s1 != GlobLocHelper.OSInt16ToString(i1, new CultureInfo("en-ZA")))
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
            Int16 i1 = -0;
            string s1 = i1.ToString(new CultureInfo("en-PH"));
            if (s1 != GlobLocHelper.OSInt16ToString(i1, new CultureInfo("en-PH")))
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Check the int16 in the beginning of which there are some zeros ");

        try
        {
            Int16 i1 = -00000765;
            string s1 = i1.ToString((IFormatProvider)null);
            if (s1 != GlobLocHelper.OSInt16ToString(i1))
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Test the notsupportedException");

        try
        {
            Int16 i1 = TestLibrary.Generator.GetInt16(-55);
            string s1 = i1.ToString(new CultureInfo("pl"));
            TestLibrary.TestFramework.LogError("101", "The notsupportedException was not thrown as expected ");
            retVal = false;
        }
        catch (System.NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        Int16ToString2 test = new Int16ToString2();

        TestLibrary.TestFramework.BeginTestCase("Int16ToString2");

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
