using System;

/// <summary>
/// Int16.IConvertilbe.ToDecimal
/// </summary>
public class Int16IConvertibleToDecimal
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check a random int16 to Decimal ");

        try
        {
            Int16 i1 = TestLibrary.Generator.GetInt16(-55);
            IConvertible Icon1 = (IConvertible)i1;
            decimal d1 = Icon1.ToDecimal(null);
            if (d1 != i1)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not correct as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert zero to decimal");

        try
        {
            Int16 i1 = 0;
            IConvertible Icon1 = (IConvertible)i1;
            decimal d1 = Icon1.ToDecimal(null);
            if (d1 != i1)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not correct as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Check the border value Int16MaxValue");

        try
        {
            Int16 i1 = Int16.MaxValue;
            IConvertible Icon1 = (IConvertible)i1;
            decimal d1 = Icon1.ToDecimal(null);
            if (d1 != i1)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not correct as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Check the border value Int16MinValue");

        try
        {
            Int16 i1 = Int16.MinValue;
            IConvertible Icon1 = (IConvertible)i1;
            decimal d1 = Icon1.ToDecimal(null);
            if (d1 != i1)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not correct as expected");
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
        Int16IConvertibleToDecimal test = new Int16IConvertibleToDecimal();

        TestLibrary.TestFramework.BeginTestCase("Int16IConvertibleToDecimal");

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
