// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// IsNaN(System.Single)
/// </summary>
public class SingleIsNaN
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: check a random Single,IsNaN return false.");

        try
        {
            Single i1 = TestLibrary.Generator.GetSingle(-55);
            if (Single.IsNaN(i1))
            {
                TestLibrary.TestFramework.LogError("001.1", "IsNaN should return false. ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: check value  which is  not a number,   has a correct IsNaN return  value.");

        try
        {
           Single i1 = Single.NaN;
           if (!Single.IsNaN(i1))
            {
                TestLibrary.TestFramework.LogError("002.1", "IsNaN should return true. ");
                retVal = false;
            }
            i1 = Single.NegativeInfinity;
            if (Single.IsNaN(i1))
            {
                TestLibrary.TestFramework.LogError("002.2", "IsNaN should return false. ");
                retVal = false;
            }
            i1 = Single.PositiveInfinity;
            if (Single.IsNaN(i1))
            {
                TestLibrary.TestFramework.LogError("002.3", "IsNaN should return false. ");
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
        SingleIsNaN test = new SingleIsNaN();

        TestLibrary.TestFramework.BeginTestCase("SingleIsNaN");

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
