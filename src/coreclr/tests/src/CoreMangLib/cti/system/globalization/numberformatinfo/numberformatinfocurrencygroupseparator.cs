using System;
using System.Globalization;

/// <summary>
/// CurrencyGroupSeparator
/// </summary>

public class NumberFormatInfoCurrencyGroupSeparator
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        
        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify default value of property CurrencyGroupSeparator .");

        try
        {
            NumberFormatInfo nfi = new NumberFormatInfo();

            if (nfi.CurrencyGroupSeparator != ",")
            {
                TestLibrary.TestFramework.LogError("001.1", "Property CurrencyGroupSeparator Err .");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify set value of property CurrencyGroupSeparator .");

        try
        {
            string testStr = "testStr";
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.CurrencyGroupSeparator = testStr;

            if (nfi.CurrencyGroupSeparator != testStr)
            {
                TestLibrary.TestFramework.LogError("002.1", "Property CurrencyGroupSeparator Err .");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown.");

        try
        {
            string testStr = null;
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.CurrencyGroupSeparator = testStr;

            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException is not thrown.");
            retVal = false;
        }
        catch (ArgumentNullException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: InvalidOperationException is not thrown.");

        try
        {
            string testStr = "testStr";
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            nfiReadOnly.CurrencyGroupSeparator = testStr;

            TestLibrary.TestFramework.LogError("102.1", "InvalidOperationException is not thrown.");
            retVal = false;
        }
        catch (InvalidOperationException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        NumberFormatInfoCurrencyGroupSeparator test = new NumberFormatInfoCurrencyGroupSeparator();

        TestLibrary.TestFramework.BeginTestCase("NumberFormatInfoCurrencyGroupSeparator");

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
