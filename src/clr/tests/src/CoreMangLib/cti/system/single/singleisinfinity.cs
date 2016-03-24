// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// IsInfinity(System.Single)
/// </summary>
public class SingleIsInfinity
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: check a random Single,isInfinity return false.");

        try
        {
            Single i1 = TestLibrary.Generator.GetSingle(-55);
            if (Single.IsInfinity(i1))
            {
                TestLibrary.TestFramework.LogError("001.1", "IsInfinity should return false. ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: check value  which is  not a number,   has a correct IsInfinity return  value.");

        try
        {
           Single i1 = Single.NaN;
           if (Single.IsInfinity(i1))
            {
                TestLibrary.TestFramework.LogError("002.1", "IsInfinity should return false. ");
                retVal = false;
            }
            i1 = Single.NegativeInfinity;
            if (!Single.IsInfinity(i1))
            {
                TestLibrary.TestFramework.LogError("002.2", "IsInfinity should return true. ");
                retVal = false;
            }
            i1 = Single.PositiveInfinity;
            if (!Single.IsInfinity(i1))
            {
                TestLibrary.TestFramework.LogError("002.3", "IsInfinity should return true. ");
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
        SingleIsInfinity test = new SingleIsInfinity();

        TestLibrary.TestFramework.BeginTestCase("SingleIsInfinity");

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
