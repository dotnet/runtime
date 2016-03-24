// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Convert.ToDecimal(System.UInt16)
/// </summary>
public class ConvertToDecimal1
{
    public static int Main()
    {
        ConvertToDecimal1 testObj = new ConvertToDecimal1();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToDecimal(System.UInt16)");
        if (testObj.RunTests())
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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string errorDesc;

        System.UInt16 b;

        Decimal actualValue;

        b = Convert.ToUInt16(TestLibrary.Generator.GetInt32(-55) % System.UInt16.MaxValue);

        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify the parameter is a Random System.UInt16 ");
        try
        {
            actualValue = Convert.ToDecimal(b);

            if (actualValue != b)
            {
                errorDesc = "The Decimal value " + b + " is not the value " + b +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\n The System.UInt16 value is " + b;
            TestLibrary.TestFramework.LogError("002", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string errorDesc;

        System.UInt16 b;
        
        Decimal actualValue;

        b = System.UInt16.MaxValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2: value is USystem.UInt16.MaxValue.");
        try
        {
            actualValue = Convert.ToDecimal(b);

            if (actualValue != b)
            {
                errorDesc = "The Decimal value " + b + " is not the value " + b +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("003", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe byte value is " + b;
            TestLibrary.TestFramework.LogError("004", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string errorDesc;

        System.UInt16 b;

        Decimal actualValue;

        b = System.UInt16.MinValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3: value is System.UInt16.MinValue.");
        try
        {
            actualValue = Convert.ToDecimal(b);

            if (actualValue != b)
            {
                errorDesc = "The  Decimal value " + b + " is not the value " + b +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("005", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe System.UInt16 value is " + b;
            TestLibrary.TestFramework.LogError("006", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
