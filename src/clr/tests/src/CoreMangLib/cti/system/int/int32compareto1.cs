// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class Int32CompareTo1
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: set the parameter to int32MaxValue");

        try
        {
            Int32 intCase = Int32.MaxValue;
            if (intCase.CompareTo(Int32.MaxValue) != 0)
            {
                TestLibrary.TestFramework.LogError("001", " CompareTo method works incorrectly");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Compare to a less int");

        try
        {
            Int32 intCase = TestLibrary.Generator.GetInt32(-55);
            if(intCase == Int32.MinValue)
                intCase = intCase+1; // To avoid the intCase = Int32.MinValue

            if (intCase.CompareTo(Int32.MinValue) <= 0)
            {
                TestLibrary.TestFramework.LogError("003", " CompareTo method works incorrectly");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Compare to a big int");

        try
        {
            Int32 intCase1 = TestLibrary.Generator.GetInt32(-55); 
            while (intCase1 == Int32.MaxValue)
            {
                intCase1 = TestLibrary.Generator.GetInt32(-55);
            }
           
            int intCase2 = intCase1 + 1;
           
            if (intCase1.CompareTo(intCase2) >= 0)
            {
                TestLibrary.TestFramework.LogError("005", " CompareTo method works incorrectly");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        Int32CompareTo1 test = new Int32CompareTo1();

        TestLibrary.TestFramework.BeginTestCase("Int32CompareTo1");

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
