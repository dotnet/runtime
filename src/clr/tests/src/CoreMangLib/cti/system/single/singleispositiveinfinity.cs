using System;
/// <summary>
/// IsPositiveInfinity(System.Single)
/// </summary>
public class SingleIsPositiveInfinity
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: check a random Single,IsPositiveInfinity return false.");

        try
        {
            Single i1 = TestLibrary.Generator.GetSingle(-55);
            if (Single.IsPositiveInfinity(i1))
            {
                TestLibrary.TestFramework.LogError("001.1", "IsPositiveInfinity should return false. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
  
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: check value  which is  not a number,   has a correct IsPositiveInfinity return  value.");

        try
        {
           Single i1 = Single.NaN;
           if (Single.IsPositiveInfinity(i1))
            {
                TestLibrary.TestFramework.LogError("002.1", "IsPositiveInfinity should return false. ");
                retVal = false;
            }
            i1 = Single.NegativeInfinity;
            if (Single.IsPositiveInfinity(i1))
            {
                TestLibrary.TestFramework.LogError("002.2", "IsPositiveInfinity should return false. ");
                retVal = false;
            }
            i1 = Single.PositiveInfinity;
            if (!Single.IsPositiveInfinity(i1))
            {
                TestLibrary.TestFramework.LogError("002.3", "IsPositiveInfinity should return true. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.4", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        SingleIsPositiveInfinity test = new SingleIsPositiveInfinity();

        TestLibrary.TestFramework.BeginTestCase("SingleIsPositiveInfinity");

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
