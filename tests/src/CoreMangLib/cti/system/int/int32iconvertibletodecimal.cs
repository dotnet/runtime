// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class Int32IConvertibleToDecimal
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check a random int32 to Decimal ");

        try
        {
            Int32 i1 = TestLibrary.Generator.GetInt32(-55);
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
            Int32 i1 = 0;
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Check the border value Int32MaxValue");

        try
        {
            Int32 i1 = Int32.MaxValue;
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Check the border value Int32MinValue");

        try
        {
            Int32 i1 = Int32.MinValue;
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
        Int32IConvertibleToDecimal test = new Int32IConvertibleToDecimal();

        TestLibrary.TestFramework.BeginTestCase("Int32IConvertibleToDecimal");

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
